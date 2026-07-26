// <copyright file="ValoriaThronePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.PlugIns.ValoriaThrone;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;
using MUnique.OpenMU.PlugIns;
using MUnique.OpenMU.PlugIns.ValoriaThrone.Configuration;
using MUnique.OpenMU.PlugIns.ValoriaThrone.Domain;
using MUnique.OpenMU.PlugIns.ValoriaThrone.Services;

/// <summary>
/// Provides the Valoria Throne event plugin points and administrative command.
/// </summary>
[PlugIn]
[Guid("D066FCD8-6B7E-4A6E-8D77-F7BFB4096C1E")]
public sealed class ValoriaThronePlugIn : IPeriodicTaskPlugIn, IChatCommandPlugIn, IMapEntryValidationPlugIn, IAttackableGotKilledPlugIn, IObjectRemovedFromMapPlugIn, ISupportCustomConfiguration<ValoriaThroneOptions>, ISupportDefaultCustomConfiguration
{
    private const string Command = "/valoriathrone";

    private readonly IValoriaThroneEventController _controller;
    private readonly IValoriaThroneMapOperations _mapOperations;
    private readonly ValoriaThroneAdmissionPolicy _admissionPolicy;
    private readonly ValoriaThroneScheduler _scheduler;

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

    /// <summary>Gets or sets the plugin configuration.</summary>
    public ValoriaThroneOptions? Configuration
    {
        get;
        set
        {
            field = value;
            var options = value ?? ValoriaThroneOptions.Default;
            ValoriaThroneOptionsValidator.Validate(options);
            this._controller.Configure(options);
            this._admissionPolicy.Configure(options);
            this._scheduler.Configure(options);
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
                await this.SendResultAsync(player, "Uso: /valoriathrone <start|stop|status|evacuate|spawn|reset>").ConfigureAwait(false);
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
    }

    /// <inheritdoc />
    public async ValueTask ObjectRemovedFromMapAsync(GameMap map, ILocateable removedObject)
    {
        if (removedObject is not IAttackable attackable || this._controller.GetSnapshot().EventInstanceId is not { } eventInstanceId)
        {
            return;
        }

        if (this._controller.State == ValoriaThroneEventState.InProgress && this._mapOperations.IsCurrentGuardian(attackable, eventInstanceId))
        {
            await this._controller.StopAsync(ValoriaThroneStopReason.Failure, CancellationToken.None).ConfigureAwait(false);
        }
    }

    private ValueTask SendResultAsync(Player player, string message)
    {
        return player.ShowBlueMessageAsync(message);
    }
}
