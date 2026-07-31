// <copyright file="ValoriaThronePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.PlugIns.ValoriaThrone;

using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging.Abstractions;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.GameLogic.PlayerActions;
using MUnique.OpenMU.GameLogic.PlayerActions.Craftings;
using MUnique.OpenMU.GameLogic.PlayerActions.Items;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;
using MUnique.OpenMU.GameLogic.Views.Character;
using MUnique.OpenMU.PlugIns;
using MUnique.OpenMU.PlugIns.ValoriaThrone.Configuration;
using MUnique.OpenMU.PlugIns.ValoriaThrone.Domain;
using MUnique.OpenMU.PlugIns.ValoriaThrone.Services;

/// <summary>
/// Provides the Valoria Throne event plugin points and administrative command.
/// </summary>
[PlugIn]
[Guid("D066FCD8-6B7E-4A6E-8D77-F7BFB4096C1E")]
public sealed class ValoriaThronePlugIn : IPeriodicTaskPlugIn, IChatCommandPlugIn, IMapEntryValidationPlugIn, IAttackableGotKilledPlugIn, IObjectRemovedFromMapPlugIn, IItemPickupPlugIn, IPlayerTalkToNpcPlugIn, IPlayerStateChangedPlugIn, IExperienceRateModifierPlugIn, ICommonDropRateModifierPlugIn, IPlayerKillerWarpPolicyPlugIn, IPvpPenaltyPolicyPlugIn, IChaosSuccessRateModifierPlugIn, IJewelSuccessRateModifierPlugIn, ISupportCustomConfiguration<ValoriaThroneOptions>, ISupportDefaultCustomConfiguration
{
    private const string Command = "/valoriathrone";

    private static readonly ValoriaThroneRuntimeRegistry RuntimeRegistry = new();
    private static readonly IValoriaThroneStateStore StateStore = new InMemoryValoriaThroneStateStore();
    private static readonly IValoriaThroneMessenger Messenger = new ValoriaThroneMessenger();
    private static readonly IValoriaThroneMapOperations SharedMapOperations = new ValoriaThroneMapOperations(RuntimeRegistry, NullLogger<ValoriaThroneMapOperations>.Instance);
    private static readonly IValoriaThroneEventController SharedController = new ValoriaThroneEventController(RuntimeRegistry, SharedMapOperations, Messenger, StateStore, TimeProvider.System, NullLogger<ValoriaThroneEventController>.Instance);
    private static readonly ValoriaThroneAdmissionPolicy SharedAdmissionPolicy = new(SharedController, NullLogger<ValoriaThroneAdmissionPolicy>.Instance);
    private static readonly ValoriaThroneScheduler SharedScheduler = new(TimeProvider.System);

    private readonly IValoriaThroneEventController _controller;
    private readonly IValoriaThroneMapOperations _mapOperations;
    private readonly ValoriaThroneAdmissionPolicy _admissionPolicy;
    private readonly ValoriaThroneScheduler _scheduler;
    private ValoriaThroneOptions _options = ValoriaThroneOptions.Default;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValoriaThronePlugIn"/> class.
    /// </summary>
    public ValoriaThronePlugIn()
        : this(SharedController, SharedMapOperations, SharedAdmissionPolicy, SharedScheduler)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValoriaThronePlugIn"/> class.
    /// </summary>
    public ValoriaThronePlugIn(
        IValoriaThroneEventController controller,
        IValoriaThroneMapOperations mapOperations,
        ValoriaThroneAdmissionPolicy admissionPolicy,
        ValoriaThroneScheduler scheduler)
    {
        this._controller = controller;
        this._mapOperations = mapOperations;
        this._admissionPolicy = admissionPolicy;
        this._scheduler = scheduler;
    }

    /// <summary>Gets the shared controller used by public Valoria commands.</summary>
    internal static IValoriaThroneEventController DefaultController => SharedController;

    /// <summary>Gets or sets the plugin configuration.</summary>
    public ValoriaThroneOptions? Configuration
    {
        get;
        set
        {
            field = value;
            var options = value ?? ValoriaThroneOptions.Default;
            try
            {
                ValoriaThroneOptionsValidator.Validate(options);
            }
            catch (Exception exception) when (exception is InvalidOperationException or TimeZoneNotFoundException or InvalidTimeZoneException)
            {
                options = ValoriaThroneOptions.Default;
            }

            this._controller.Configure(options);
            this._admissionPolicy.Configure(options);
            this._scheduler.Configure(options);
            this._options = options;
        }
    }

    /// <inheritdoc />
    public string Key => Command;

    /// <inheritdoc />
    public CharacterStatus MinCharacterStatusRequirement => CharacterStatus.GameMaster;

    /// <inheritdoc />
    public object CreateDefaultConfig() => ValoriaThroneOptions.Default;

    /// <inheritdoc />
    public void ForceStart()
    {
        this._controller.RequestStart();
    }

    /// <inheritdoc />
    public async ValueTask ExecuteTaskAsync(GameContext gameContext)
    {
        if (gameContext is not IGameServerContext serverContext)
        {
            return;
        }

        this._controller.Register(serverContext);
        if (this._scheduler.IsDue())
        {
            await this._controller.StartAsync(ValoriaThroneStartReason.Schedule, CancellationToken.None).ConfigureAwait(false);
        }

        await this._controller.TickAsync(CancellationToken.None).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask HandleCommandAsync(Player player, string command)
    {
        var subCommand = command.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1).FirstOrDefault()?.ToLowerInvariant();
        switch (subCommand)
        {
            case "start":
                await this.SendResultAsync(player, await this._controller.StartAsync(ValoriaThroneStartReason.Administrator, CancellationToken.None).ConfigureAwait(false) ? "O evento foi iniciado." : "O evento não pode ser iniciado no estado atual.").ConfigureAwait(false);
                break;
            case "stop":
                await this._controller.StopAsync(ValoriaThroneStopReason.Administrator, CancellationToken.None).ConfigureAwait(false);
                await this.SendResultAsync(player, "O encerramento do evento foi solicitado.").ConfigureAwait(false);
                break;
            case "status":
                var snapshot = this._controller.GetSnapshot();
                await this.SendResultAsync(player, $"Estado: {snapshot.State}; execução: {snapshot.EventInstanceId?.ToString() ?? "nenhuma"}; guardião: {snapshot.GuardianId?.ToString() ?? "nenhum"}.").ConfigureAwait(false);
                break;
            case "era":
                var eraText = command.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(2).FirstOrDefault();
                var era = Enum.TryParse<ImperialEra>(eraText, true, out var selectedEra) ? selectedEra : ImperialEra.None;
                await this.SendResultAsync(player, await this._controller.SelectEraAsync(player, era, CancellationToken.None).ConfigureAwait(false) ? "A Era Imperial foi proclamada." : "A Era não pode ser escolhida por este personagem ou reinado.").ConfigureAwait(false);
                break;
            case "evacuate":
                await this._mapOperations.EvacuateAsync(CancellationToken.None).ConfigureAwait(false);
                await this.SendResultAsync(player, "Os jogadores em Valley of Loren foram evacuados no próprio servidor.").ConfigureAwait(false);
                break;
            case "spawn":
                await this.SendResultAsync(player, await this._controller.SpawnGuardianAsync(CancellationToken.None).ConfigureAwait(false) ? "O Guardião do Trono foi criado." : "O Guardião só pode ser criado durante a inscrição.").ConfigureAwait(false);
                break;
            case "reset":
                await this._controller.StopAsync(ValoriaThroneStopReason.Recovery, CancellationToken.None).ConfigureAwait(false);
                await this.SendResultAsync(player, "O estado do evento foi recuperado.").ConfigureAwait(false);
                break;
            default:
                await this.SendResultAsync(player, "Uso: /valoriathrone <start|stop|status|evacuate|spawn|reset|era Ascension|Fortune|Freedom|Luck>").ConfigureAwait(false);
                break;
        }
    }

    /// <inheritdoc />
    public ValueTask ValidateMapEntryAsync(Player player, GameMapDefinition targetMap, MapEntrySource source, MapEntryValidationEventArgs eventArgs)
    {
        return this._admissionPolicy.ValidateAsync(player, targetMap, source, eventArgs);
    }

    /// <inheritdoc />
    public async ValueTask AttackableGotKilledAsync(IAttackable killed, IAttacker? killer)
    {
        await this._controller.HandleGuardianKilledAsync(killed, killer, CancellationToken.None).ConfigureAwait(false);
        if (killed is Player player)
        {
            await this._controller.HandleCrownHolderLostAsync(player, "O Portador da Coroa foi derrotado!", CancellationToken.None).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async ValueTask HandleItemPickupAsync(Player player, DroppedItem droppedItem, IItemPickupPlugIn.ItemPickupArguments pickupArguments)
    {
        var result = await this._controller.HandleCrownPickupAsync(player, droppedItem, CancellationToken.None).ConfigureAwait(false);
        pickupArguments.WasHandled = result != ValoriaCrownPickupResult.NotCrown;
        pickupArguments.Success = result == ValoriaCrownPickupResult.PickedUp;
    }

    /// <inheritdoc />
    public async ValueTask PlayerTalksToNpcAsync(Player player, NonPlayerCharacter npc, NpcTalkEventArgs eventArgs)
    {
        var result = await this._controller.HandleSeniorInteractionAsync(player, npc, CancellationToken.None).ConfigureAwait(false);
        eventArgs.HasBeenHandled = result != ValoriaSeniorInteractionResult.NotSenior;
        if (result == ValoriaSeniorInteractionResult.EraSelectionRequested)
        {
            await player.InvokeViewPlugInAsync<IShowDialogPlugIn>(
                plugIn => plugIn.ShowDialogAsync(ValoriaEraDialogProtocol.CommandCategory, ValoriaEraDialogProtocol.Open)).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async ValueTask PlayerStateChangedAsync(Player player, State previousState, State currentState)
    {
        if (previousState == PlayerState.EnteredWorld && currentState == PlayerState.CharacterSelection)
        {
            await this._controller.HandleCrownHolderLostAsync(player, "O Portador da Coroa saiu do campo de batalha.", CancellationToken.None).ConfigureAwait(false);
        }

        if (previousState == PlayerState.CharacterSelection && currentState == PlayerState.EnteredWorld)
        {
            await this._controller.SynchronizeEmperorStatusAsync(player, CancellationToken.None).ConfigureAwait(false);
        }

        if (previousState != PlayerState.CharacterSelection || currentState != PlayerState.EnteredWorld || this._controller.ActiveReign is not { SelectedEra: not ImperialEra.None } reign)
        {
            return;
        }

        await player.ShowBlueMessageAsync($"O Imperador {reign.EmperorCharacterName}, da guild {reign.ImperialGuildName}, definiu a {ImperialEraPresentation.GetName(reign.SelectedEra)}: {this._controller.GetEraDescription(reign.SelectedEra)}").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void ModifyExperienceRate(Player player, IExperienceRateModifierPlugIn.ExperienceRateArguments arguments)
    {
        if (this._controller.ActiveReign?.SelectedEra == ImperialEra.Ascension)
        {
            arguments.Multiplier *= this._options.ImperialEras.AscensionExperienceMultiplier;
        }
    }

    /// <inheritdoc />
    public void ModifyCommonDropRate(Player player, MonsterDefinition monster, ICommonDropRateModifierPlugIn.CommonDropRateArguments arguments)
    {
        if (this._controller.ActiveReign?.SelectedEra == ImperialEra.Fortune)
        {
            arguments.Multiplier *= this._options.ImperialEras.FortuneDropMultiplier;
        }
    }

    /// <inheritdoc />
    public void EvaluatePlayerKillerWarp(Player player, IPlayerKillerWarpPolicyPlugIn.PlayerKillerWarpArguments arguments)
    {
        arguments.Allowed |= this._controller.ActiveReign?.SelectedEra == ImperialEra.Freedom;
    }

    /// <inheritdoc />
    public void EvaluatePvpPenalty(Player attacker, Player defender, IPvpPenaltyPolicyPlugIn.PvpPenaltyArguments arguments)
    {
        arguments.IsPenaltySuppressed |= this._options.Enabled
            && IsPenaltyFreePvpState(this._controller.State)
            && this._options.EventServerId is { } eventServerId
            && attacker.GameContext is IGameServerContext serverContext
            && serverContext.Id == eventServerId
            && ReferenceEquals(attacker.GameContext, defender.GameContext)
            && ReferenceEquals(attacker.CurrentMap, defender.CurrentMap)
            && attacker.CurrentMap?.Definition.Number == this._options.EventMapId;
    }

    /// <inheritdoc />
    public void ModifyChaosSuccessRate(Player player, IItemCraftingHandler handler, IChaosSuccessRateModifierPlugIn.ChaosSuccessRateArguments arguments)
    {
        if (this._controller.ActiveReign?.SelectedEra != ImperialEra.Luck || !this.IsLuckEnabledFor(handler))
        {
            return;
        }

        var multiplier = this._options.ImperialEras.LuckChaosMachineMultiplier;
        arguments.EffectiveRate = Math.Min(this._options.ImperialEras.MaximumChaosMachineSuccessRate, arguments.EffectiveRate * multiplier);
        arguments.Modifiers.Add(new SuccessRateModifier("ImperialEra.Luck", multiplier));
    }

    /// <inheritdoc />
    public void ModifyJewelSuccessRate(Player player, Item jewel, Item targetItem, IJewelSuccessRateModifierPlugIn.JewelSuccessRateArguments arguments)
    {
        if (this._controller.ActiveReign?.SelectedEra != ImperialEra.Luck || !this.IsAffectedJewel(jewel))
        {
            return;
        }

        var maximumChance = this._options.ImperialEras.MaximumJewelSuccessRate / 100.0;
        arguments.EffectiveChance = Math.Min(maximumChance, arguments.EffectiveChance * this._options.ImperialEras.LuckJewelMultiplier);
    }

    /// <inheritdoc />
    public async ValueTask ObjectRemovedFromMapAsync(GameMap map, ILocateable removedObject)
    {
        if (removedObject is NonPlayerCharacter npc)
        {
            await this._controller.HandleSeniorRemovedAsync(npc, CancellationToken.None).ConfigureAwait(false);
            return;
        }

        if (removedObject is not IAttackable attackable || this._controller.GetSnapshot().EventInstanceId is not { } eventInstanceId)
        {
            return;
        }

        if (this._controller.State == ValoriaThroneEventState.GuardianBattle && this._mapOperations.IsCurrentGuardian(attackable, eventInstanceId))
        {
            await this._controller.StopAsync(ValoriaThroneStopReason.Failure, CancellationToken.None).ConfigureAwait(false);
        }
    }

    internal static bool IsPenaltyFreePvpState(ValoriaThroneEventState state)
    {
        return state is ValoriaThroneEventState.GuardianBattle
            or ValoriaThroneEventState.CrownOnGround
            or ValoriaThroneEventState.CrownCarried
            or ValoriaThroneEventState.CoronationInProgress;
    }

    private bool IsLuckEnabledFor(IItemCraftingHandler handler)
    {
        if (handler is BaseEventTicketCrafting)
        {
            return this._options.ImperialEras.ApplyLuckToEventCombinations;
        }

        if (handler is SimpleItemCraftingHandler || handler.GetType().Namespace == typeof(BaseEventTicketCrafting).Namespace)
        {
            return this._options.ImperialEras.ApplyLuckToRegularCombinations;
        }

        return this._options.ImperialEras.ApplyLuckToCustomCombinations;
    }

    private bool IsAffectedJewel(Item jewel)
    {
        if (jewel.Definition is null)
        {
            return false;
        }

        var identifier = new ItemIdentifier(jewel.Definition.Number, jewel.Definition.Group);
        ValoriaJewelKind? kind = identifier == ItemConstants.JewelOfSoul
            ? ValoriaJewelKind.Soul
            : identifier == ItemConstants.JewelOfLife
                ? ValoriaJewelKind.Life
                : identifier == ItemConstants.JewelOfHarmony
                    ? ValoriaJewelKind.Harmony
                    : null;
        return kind is { } affectedKind && this._options.ImperialEras.AffectedJewels.Contains(affectedKind);
    }

    private ValueTask SendResultAsync(Player player, string message)
    {
        return player.ShowBlueMessageAsync(message);
    }
}
