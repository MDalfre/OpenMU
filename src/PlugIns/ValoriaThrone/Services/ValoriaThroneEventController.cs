// <copyright file="ValoriaThroneEventController.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.PlugIns.ValoriaThrone.Services;

using Microsoft.Extensions.Logging;
using MUnique.OpenMU.DataModel.Attributes;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.GameLogic.Views.World;
using MUnique.OpenMU.Pathfinding;
using MUnique.OpenMU.PlugIns.ValoriaThrone.Configuration;
using MUnique.OpenMU.PlugIns.ValoriaThrone.Domain;
using Nito.AsyncEx;

/// <summary>
/// Coordinates the state transitions of one event execution.
/// </summary>
public sealed class ValoriaThroneEventController : IValoriaThroneEventController
{
    /// <summary>The client status id reserved for the Valoria crown carrier.</summary>
    public const short CrownCarrierStatusId = 20;

    /// <summary>The client status id reserved for the Valoria emperor.</summary>
    public const short ValoriaEmperorStatusId = 173;

    private static readonly MagicEffectDefinition CrownCarrierStatusDefinition = new ValoriaStatusMagicEffectDefinition
    {
        Number = CrownCarrierStatusId,
        InformObservers = true,
        StopByDeath = false,
        SendDuration = false,
    };

    private static readonly MagicEffectDefinition ValoriaEmperorStatusDefinition = new ValoriaStatusMagicEffectDefinition
    {
        Number = ValoriaEmperorStatusId,
        InformObservers = true,
        StopByDeath = false,
        SendDuration = false,
    };

    private readonly AsyncLock _lock = new();
    private readonly ValoriaThroneRuntimeRegistry _runtimeRegistry;
    private readonly IValoriaThroneMapOperations _mapOperations;
    private readonly IValoriaThroneMessenger _messenger;
    private readonly IValoriaThroneStateStore _stateStore;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ValoriaThroneEventController> _logger;

    private ValoriaThroneOptions _options = ValoriaThroneOptions.Default;
    private Guid? _eventInstanceId;
    private DateTimeOffset? _nextTransitionAt;
    private ushort? _guardianId;
    private ValoriaCrownContext? _crown;
    private Player? _crownCarrier;
    private DateTimeOffset? _crownRespawnAt;
    private Player? _coronationCandidate;
    private NonPlayerCharacter? _coronationSenior;
    private ImperialReign? _activeReign;
    private int _administratorStartRequested;
    private int? _lastCountdownMinute;
    private int _currentStageDurationSeconds;
    private bool _announcedThirtySeconds;
    private bool _announcedTenSeconds;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValoriaThroneEventController"/> class.
    /// </summary>
    public ValoriaThroneEventController(
        ValoriaThroneRuntimeRegistry runtimeRegistry,
        IValoriaThroneMapOperations mapOperations,
        IValoriaThroneMessenger messenger,
        IValoriaThroneStateStore stateStore,
        TimeProvider timeProvider,
        ILogger<ValoriaThroneEventController> logger)
    {
        this._runtimeRegistry = runtimeRegistry;
        this._mapOperations = mapOperations;
        this._messenger = messenger;
        this._stateStore = stateStore;
        this._timeProvider = timeProvider;
        this._logger = logger;
        this.State = ValoriaThroneEventState.Disabled;
    }

    /// <inheritdoc />
    public ValoriaThroneEventState State { get; private set; }

    /// <inheritdoc />
    public ImperialReign? ActiveReign => this._activeReign;

    /// <inheritdoc />
    public void Configure(ValoriaThroneOptions options)
    {
        ValoriaThroneOptionsValidator.Validate(options);
        var configuredReign = options.ImperialReign;
        this._options = options;
        // A process hosts one global Valoria event. Configurations are loaded once per
        // game configuration, so an empty or older entry must never erase the reign
        // already accepted from another configuration.
        this._activeReign = SelectAuthoritativeReign(this._activeReign, configuredReign);
        this._options.ImperialReign = CloneReign(this._activeReign);
        this._logger.LogInformation(
            "Valoria Configure | ServerId: {ServerId} | EmperorCharacterId: {EmperorCharacterId} | ImperialGuildId: {ImperialGuildId} | SelectedEra: {SelectedEra} | StartedAt: {StartedAt}",
            this._options.EventServerId,
            this._activeReign?.EmperorCharacterId,
            this._activeReign?.ImperialGuildId,
            this._activeReign?.SelectedEra,
            this._activeReign?.StartedAt);
        this._mapOperations.Configure(options);
        if (!options.Enabled)
        {
            this.State = ValoriaThroneEventState.Disabled;
        }
        else if (this.State == ValoriaThroneEventState.Disabled)
        {
            this.State = ValoriaThroneEventState.Idle;
        }
    }

    /// <inheritdoc />
    public void Register(IGameServerContext context)
    {
        if (this._runtimeRegistry.Register(context))
        {
            this._logger.LogInformation("Valoria context registered | GameConfigurationId: {GameConfigurationId} | GameConfigurationName: {GameConfigurationName} | ServerId: {ServerId}", GetPersistentId(context.Configuration), context.Configuration.Name, context.Id);
        }
    }

    /// <inheritdoc />
    public void RequestStart()
    {
        Interlocked.Exchange(ref this._administratorStartRequested, 1);
    }

    /// <inheritdoc />
    public async ValueTask<bool> StartAsync(ValoriaThroneStartReason reason, CancellationToken cancellationToken)
    {
        Guid eventInstanceId;
        ImperialReign? previousReign;
        using (await this._lock.LockAsync(cancellationToken).ConfigureAwait(false))
        {
            if (!this._options.Enabled || this.State != ValoriaThroneEventState.Idle)
            {
                return false;
            }

            eventInstanceId = Guid.NewGuid();
            previousReign = this._activeReign;
            this._activeReign = null;
            this._options.ImperialReign = null;
            this._eventInstanceId = eventInstanceId;
            this._guardianId = null;
            this._crown = null;
            this._crownCarrier = null;
            this._crownRespawnAt = null;
            this._coronationCandidate = null;
            this._coronationSenior = null;
            this.State = ValoriaThroneEventState.Announcing;
            this.SetNextTransition(this._options.AnnouncementDuration);
            await this.SaveSnapshotAsync(cancellationToken).ConfigureAwait(false);
        }

        if (!await this.PersistConfigurationAsync(cancellationToken).ConfigureAwait(false))
        {
            using (await this._lock.LockAsync(cancellationToken).ConfigureAwait(false))
            {
                this._activeReign = previousReign;
                this._options.ImperialReign = CloneReign(previousReign);
            }

            this._logger.LogError("Valoria event {EventInstanceId} started, but the previous reign could not be cleared persistently.", eventInstanceId);
            await this.StopAsync(ValoriaThroneStopReason.Failure, cancellationToken).ConfigureAwait(false);
            return false;
        }
        if (previousReign is not null)
        {
            await this.SynchronizeConnectedEmperorStatusAsync(previousReign.EmperorCharacterId, cancellationToken).ConfigureAwait(false);
        }

        this._logger.LogInformation("Valoria Throne {EventInstanceId} started by {Reason}.", eventInstanceId, reason);
        await this.BroadcastAsync($"O Trono de Valoria começará em {FormatDuration(this._options.AnnouncementDuration)}.", cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <inheritdoc />
    public async ValueTask StopAsync(ValoriaThroneStopReason reason, CancellationToken cancellationToken)
    {
        Guid? eventInstanceId;
        Player? crownCarrier;
        using (await this._lock.LockAsync(cancellationToken).ConfigureAwait(false))
        {
            eventInstanceId = this._eventInstanceId;
            if (eventInstanceId is null && this.State is ValoriaThroneEventState.Idle or ValoriaThroneEventState.Disabled)
            {
                return;
            }

            this.State = ValoriaThroneEventState.Finishing;
            this._nextTransitionAt = null;
            this._crownRespawnAt = null;
            this._coronationCandidate = null;
            this._coronationSenior = null;
            crownCarrier = this._crownCarrier;
            this._crownCarrier = null;
        }

        try
        {
            await this.SetCrownCarrierMarkerAsync(crownCarrier, false).ConfigureAwait(false);
            if (crownCarrier is not null)
            {
                await this.SynchronizeCrownCarrierStatusAsync(crownCarrier, false).ConfigureAwait(false);
            }
            await this._mapOperations.CleanupAsync(eventInstanceId, cancellationToken).ConfigureAwait(false);
            await this.BroadcastAsync("A batalha terminou.", cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Valoria Throne cleanup failed for {EventInstanceId}.", eventInstanceId);
        }
        finally
        {
            var stopped = false;
            using (await this._lock.LockAsync(cancellationToken).ConfigureAwait(false))
            {
                if (this._eventInstanceId == eventInstanceId)
                {
                    this._eventInstanceId = null;
                    this._guardianId = null;
                    this._crown = null;
                    this._coronationCandidate = null;
                    this._coronationSenior = null;
                    this.State = this._options.Enabled ? ValoriaThroneEventState.Cooldown : ValoriaThroneEventState.Disabled;
                    if (this.State == ValoriaThroneEventState.Cooldown)
                    {
                        this.SetNextTransition(this._options.CooldownDuration);
                    }
                    else
                    {
                        this._nextTransitionAt = null;
                    }

                    await this.SaveSnapshotAsync(cancellationToken).ConfigureAwait(false);
                    stopped = true;
                }
            }

            if (stopped)
            {
                this._logger.LogInformation("Valoria Throne {EventInstanceId} stopped by {Reason}.", eventInstanceId, reason);
                await this.BroadcastAsync($"Novo evento disponível em {FormatDuration(this._options.CooldownDuration)}.", cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <inheritdoc />
    public async ValueTask TickAsync(CancellationToken cancellationToken)
    {
        await this.SelectDefaultEraIfDueAsync(cancellationToken).ConfigureAwait(false);
        if (Interlocked.Exchange(ref this._administratorStartRequested, 0) == 1)
        {
            await this.StartAsync(ValoriaThroneStartReason.Administrator, cancellationToken).ConfigureAwait(false);
            return;
        }

        ValoriaThroneEventState state;
        Guid? eventInstanceId;
        string? countdownMessage = null;
        var isWaitingForTransition = false;
        var crownDeliveryExpired = false;
        var crownRespawnDue = false;
        var coronationCompleted = false;
        var coronationInterrupted = false;
        using (await this._lock.LockAsync(cancellationToken).ConfigureAwait(false))
        {
            var now = this._timeProvider.GetUtcNow();
            if ((this.State == ValoriaThroneEventState.CrownCarried && this._crown?.DeliveryDeadline <= now)
                || (this.State == ValoriaThroneEventState.CoronationInProgress && this._crown?.CoronationEndsAt <= now)
                || (this._nextTransitionAt is { } nextTransitionAt && nextTransitionAt > now))
            {
                state = this.State;
                eventInstanceId = this._eventInstanceId;
                if (state == ValoriaThroneEventState.CrownCarried && this._crown?.DeliveryDeadline is { } deliveryDeadline && deliveryDeadline <= now)
                {
                    crownDeliveryExpired = true;
                }
                else if (state == ValoriaThroneEventState.CoronationInProgress && this._crown?.CoronationEndsAt is { } coronationEndsAt && coronationEndsAt <= now)
                {
                    coronationCompleted = true;
                }
                else if (state == ValoriaThroneEventState.CoronationInProgress && !this.IsCoronationCandidateValid())
                {
                    coronationInterrupted = true;
                }
                else if (state == ValoriaThroneEventState.CrownOnGround && this._crownRespawnAt is { } respawnAt && respawnAt <= now)
                {
                    this._crownRespawnAt = null;
                    crownRespawnDue = true;
                }
                else
                {
                    countdownMessage = this.GetCountdownMessage(this.State, this._nextTransitionAt!.Value - now);
                    isWaitingForTransition = true;
                }
            }
            else
            {
                state = this.State;
                eventInstanceId = this._eventInstanceId;
                if (state == ValoriaThroneEventState.Cooldown)
                {
                    this.State = ValoriaThroneEventState.Idle;
                    this._nextTransitionAt = null;
                    await this.SaveSnapshotAsync(cancellationToken).ConfigureAwait(false);
                    countdownMessage = "O Trono de Valoria está disponível para um novo evento.";
                }
            }
        }

        if (countdownMessage is not null)
        {
            await this.BroadcastAsync(countdownMessage, cancellationToken).ConfigureAwait(false);
        }

        if (crownDeliveryExpired && eventInstanceId is { } expiredEventInstanceId)
        {
            await this.ReturnCrownAsync(expiredEventInstanceId, "O Portador nÃ£o conseguiu entregar a Coroa de Valoria a tempo.", cancellationToken).ConfigureAwait(false);
            return;
        }

        if (coronationCompleted && eventInstanceId is { } completedEventInstanceId)
        {
            await this.CompleteCoronationAsync(completedEventInstanceId, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (coronationInterrupted && eventInstanceId is { } interruptedEventInstanceId)
        {
            await this.ReturnCrownAsync(interruptedEventInstanceId, "A coroacao foi interrompida porque o candidato deixou a area do Senior.", cancellationToken).ConfigureAwait(false);
            return;
        }

        if (crownRespawnDue && eventInstanceId is { } respawnEventInstanceId)
        {
            await this.SpawnCrownAsync(respawnEventInstanceId, "A Coroa de Valoria retornou ao local da queda do GuardiÃ£o.", cancellationToken).ConfigureAwait(false);
            return;
        }

        if (isWaitingForTransition)
        {
            return;
        }

        if (eventInstanceId is null)
        {
            return;
        }

        switch (state)
        {
            case ValoriaThroneEventState.Announcing:
                await this.PrepareAsync(eventInstanceId.Value, cancellationToken).ConfigureAwait(false);
                break;
            case ValoriaThroneEventState.Preparing:
                await this.OpenRegistrationAsync(eventInstanceId.Value, cancellationToken).ConfigureAwait(false);
                break;
            case ValoriaThroneEventState.RegistrationOpen:
                await this.StartBattleAsync(eventInstanceId.Value, cancellationToken).ConfigureAwait(false);
                break;
            case ValoriaThroneEventState.GuardianBattle:
            case ValoriaThroneEventState.CrownOnGround:
            case ValoriaThroneEventState.CrownCarried:
            case ValoriaThroneEventState.CoronationInProgress:
                await this.StopAsync(ValoriaThroneStopReason.Timeout, cancellationToken).ConfigureAwait(false);
                break;
        }
    }

    /// <inheritdoc />
    public ValoriaThroneSnapshot GetSnapshot() => new(
        this._eventInstanceId,
        this.State,
        this._nextTransitionAt,
        this._guardianId,
        this._crown?.GroundItemId,
        this._crown?.HolderCharacterId,
        this._crown?.HolderGuildId,
        this._crown?.CoronationEndsAt);

    /// <inheritdoc />
    public async ValueTask<bool> SpawnGuardianAsync(CancellationToken cancellationToken)
    {
        Guid eventInstanceId;
        using (await this._lock.LockAsync(cancellationToken).ConfigureAwait(false))
        {
            if (this._eventInstanceId is not { } currentEventInstanceId || this.State != ValoriaThroneEventState.RegistrationOpen)
            {
                return false;
            }

            eventInstanceId = currentEventInstanceId;
        }

        await this.StartBattleAsync(eventInstanceId, cancellationToken).ConfigureAwait(false);
        return this.State == ValoriaThroneEventState.GuardianBattle;
    }

    /// <summary>
    /// Handles the death of an attackable object when it is the current guardian.
    /// </summary>
    /// <param name="killed">The killed object.</param>
    /// <param name="killer">The attacker, if available.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A value indicating whether the death belonged to the current guardian.</returns>
    public async ValueTask<bool> HandleGuardianKilledAsync(IAttackable killed, IAttacker? killer, CancellationToken cancellationToken)
    {
        Guid eventInstanceId;
        using (await this._lock.LockAsync(cancellationToken).ConfigureAwait(false))
        {
            if (this._eventInstanceId is not { } currentEventInstanceId
                || this.State != ValoriaThroneEventState.GuardianBattle
                || !this._mapOperations.IsCurrentGuardian(killed, currentEventInstanceId))
            {
                return false;
            }

            eventInstanceId = currentEventInstanceId;
            this.State = ValoriaThroneEventState.CrownOnGround;
            this._crown = new ValoriaCrownContext
            {
                EventInstanceId = currentEventInstanceId,
                OriginalDropPosition = killed.Position,
            };
            this._crownRespawnAt = null;
            this.SetNextTransition(this._options.CrownPhaseDuration);
            await this.SaveSnapshotAsync(cancellationToken).ConfigureAwait(false);
        }

        var killerName = killed.LastDeath?.KillerName ?? "Um aventureiro";
        this._logger.LogInformation("Valoria guardian {GuardianId} was defeated during {EventInstanceId} by {KillerName}.", killed.Id, eventInstanceId, killerName);
        await this.SpawnCrownAsync(eventInstanceId, $"{killer?.GetName() ?? "Um aventureiro"} derrotou o Guardiao do Trono. A Coroa de Valoria caiu no campo de batalha e estara disponivel por {FormatDuration(this._options.CrownPhaseDuration)}.", cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <inheritdoc />
    public async ValueTask<ValoriaCrownPickupResult> HandleCrownPickupAsync(Player player, DroppedItem droppedItem, CancellationToken cancellationToken)
    {
        string? rejectionMessage = null;
        Guid eventInstanceId = Guid.Empty;
        string? characterName = null;
        uint guildId = 0;

        // First pass: validate the pickup conditions and collect the guild id.
        using (await this._lock.LockAsync(cancellationToken).ConfigureAwait(false))
        {
            if (this._eventInstanceId is not { } currentEventInstanceId || !this._mapOperations.IsCurrentCrown(droppedItem, currentEventInstanceId))
            {
                return ValoriaCrownPickupResult.NotCrown;
            }

            if (this.State != ValoriaThroneEventState.CrownOnGround || this._crown is null || this._crown.GroundItemId != droppedItem.Id)
            {
                rejectionMessage = "A Coroa de Valoria ja nao esta disponivel.";
            }
            else if (!player.IsAlive)
            {
                rejectionMessage = "Voce precisa estar vivo para reivindicar a Coroa de Valoria.";
            }
            else if (player.IsTeleporting || player.CurrentMap?.Definition.Number != this._options.EventMapId)
            {
                rejectionMessage = "Voce precisa estar em Valley of Loren para reivindicar a Coroa de Valoria.";
            }
            else if (player.SelectedCharacter is null || player.GuildStatus is null)
            {
                rejectionMessage = "Voce precisa pertencer a uma guild para reivindicar a Coroa de Valoria.";
            }
            else if (this._nextTransitionAt is not { } deadline || deadline <= this._timeProvider.GetUtcNow())
            {
                rejectionMessage = "O prazo para reivindicar a Coroa de Valoria terminou.";
            }
            else
            {
                eventInstanceId = currentEventInstanceId;
                characterName = player.SelectedCharacter.Name;
                guildId = player.GuildStatus.GuildId;
            }
        }

        if (rejectionMessage is not null)
        {
            await this._messenger.SendToPlayerAsync(player, rejectionMessage, cancellationToken).ConfigureAwait(false);
            return ValoriaCrownPickupResult.Rejected;
        }

        // Resolve the guild name outside the lock to avoid async calls inside it.
        var guildName = player.GameContext is IGameServerContext serverContext
            ? (await serverContext.GuildServer.GetGuildAsync(guildId).ConfigureAwait(false))?.Name
            : null;
        guildName ??= guildId.ToString();

        // Second pass: apply state changes now that all data is ready.
        using (await this._lock.LockAsync(cancellationToken).ConfigureAwait(false))
        {
            if (this._eventInstanceId != eventInstanceId
                || this.State != ValoriaThroneEventState.CrownOnGround
                || this._crown is null
                || this._crown.GroundItemId != droppedItem.Id)
            {
                // State changed while we were resolving the guild name; treat as rejected.
                await this._messenger.SendToPlayerAsync(player, "A Coroa de Valoria ja nao esta disponivel.", cancellationToken).ConfigureAwait(false);
                return ValoriaCrownPickupResult.Rejected;
            }

            this._crown.GroundItemId = null;
            this._crown.HolderCharacterId = player.SelectedCharacter!.Id;
            this._crown.HolderCharacterName = characterName;
            this._crown.HolderGuildId = guildId;
            this._crown.HolderGuildName = guildName;
            this._crown.PickedUpAt = this._timeProvider.GetUtcNow();
            this._crown.DeliveryDeadline = this._crown.PickedUpAt.Value.Add(this._options.CrownDeliveryDuration);
            this._crownCarrier = player;
            this.State = ValoriaThroneEventState.CrownCarried;
            // The crown-on-ground timeout only applies until it is collected. From
            // this point the carrier gets the independently configured delivery time.
            this.SetNextTransition(this._options.CrownDeliveryDuration);
            await this.SaveSnapshotAsync(cancellationToken).ConfigureAwait(false);
        }

        await this._mapOperations.RemoveCrownAsync(eventInstanceId, cancellationToken).ConfigureAwait(false);
        await this.SynchronizeCrownCarrierStatusAsync(player, true).ConfigureAwait(false);
        await this.SetCrownCarrierMarkerAsync(player, true).ConfigureAwait(false);
        this._logger.LogInformation("Valoria crown of {EventInstanceId} was claimed by {HolderCharacterId} from guild {HolderGuildName} (runtime id {HolderGuildId}).", eventInstanceId, player.SelectedCharacter!.Id, guildName, guildId);
        await this.BroadcastAsync($"{characterName}, da guild {guildName}, tomou a Coroa de Valoria! O portador possui {FormatDuration(this._options.CrownDeliveryDuration)} para alcancar o Senior.", cancellationToken).ConfigureAwait(false);
        return ValoriaCrownPickupResult.PickedUp;
    }

    /// <inheritdoc />
    public async ValueTask<bool> HandleCrownHolderLostAsync(Player player, string reason, CancellationToken cancellationToken)
    {
        Guid eventInstanceId;
        using (await this._lock.LockAsync(cancellationToken).ConfigureAwait(false))
        {
            var crown = this._crown;
            if (this._eventInstanceId is not { } currentEventInstanceId
                || this.State is not (ValoriaThroneEventState.CrownCarried or ValoriaThroneEventState.CoronationInProgress)
                || crown is null
                || crown.HolderCharacterId != player.SelectedCharacter?.Id)
            {
                return false;
            }

            // The winner identity was captured when the coronation started. A disconnect
            // during the final stage must neither erase it nor open a second coronation.
            if (this.State == ValoriaThroneEventState.CoronationInProgress)
            {
                return true;
            }

            eventInstanceId = currentEventInstanceId;
            crown.ClearHolder();
            this._crownCarrier = null;
            this.State = ValoriaThroneEventState.CrownOnGround;
            this._crownRespawnAt = this._timeProvider.GetUtcNow().Add(this._options.CrownRespawnDelay);
            this._coronationCandidate = null;
            this._coronationSenior = null;
            await this.SaveSnapshotAsync(cancellationToken).ConfigureAwait(false);
        }

        await this.SetCrownCarrierMarkerAsync(player, false).ConfigureAwait(false);
        await this.SynchronizeCrownCarrierStatusAsync(player, false).ConfigureAwait(false);
        this._logger.LogInformation("Valoria crown holder {CharacterId} lost the crown during {EventInstanceId}: {Reason}.", player.SelectedCharacter?.Id, eventInstanceId, reason);
        await this.BroadcastAsync($"{reason} A Coroa de Valoria retornara ao local da queda do Guardiao em {FormatDuration(this._options.CrownRespawnDelay)}.", cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <inheritdoc />
    public bool IsCrownHolder(Player player)
    {
        return (this.State is ValoriaThroneEventState.CrownCarried or ValoriaThroneEventState.CoronationInProgress)
               && this._crown?.HolderCharacterId == player.SelectedCharacter?.Id;
    }

    /// <inheritdoc />
    public async ValueTask SynchronizeEmperorStatusAsync(Player player, CancellationToken cancellationToken)
    {
        var isCurrentEmperor = this._activeReign?.EmperorCharacterId == player.SelectedCharacter?.Id;
        if (isCurrentEmperor)
        {
            if (!player.MagicEffectList.ActiveEffects.ContainsKey(ValoriaEmperorStatusId))
            {
                await player.MagicEffectList.AddEffectAsync(new MagicEffect(Timeout.InfiniteTimeSpan, ValoriaEmperorStatusDefinition)).ConfigureAwait(false);
            }

            return;
        }

        if (player.MagicEffectList.ActiveEffects.TryGetValue(ValoriaEmperorStatusId, out var status))
        {
            await status.DisposeAsync().ConfigureAwait(false);
        }
    }

    private async ValueTask SynchronizeCrownCarrierStatusAsync(Player player, bool isCarrier)
    {
        if (isCarrier)
        {
            if (!player.MagicEffectList.ActiveEffects.ContainsKey(CrownCarrierStatusId))
            {
                await player.MagicEffectList.AddEffectAsync(new MagicEffect(Timeout.InfiniteTimeSpan, CrownCarrierStatusDefinition)).ConfigureAwait(false);
            }

            this._logger.LogInformation("Valoria crown status synchronized | CharacterId: {CharacterId} | StatusId: {StatusId} | Active: true", player.SelectedCharacter?.Id, CrownCarrierStatusId);
            return;
        }

        if (player.MagicEffectList.ActiveEffects.TryGetValue(CrownCarrierStatusId, out var status))
        {
            await status.DisposeAsync().ConfigureAwait(false);
        }

        this._logger.LogInformation("Valoria crown status synchronized | CharacterId: {CharacterId} | StatusId: {StatusId} | Active: false", player.SelectedCharacter?.Id, CrownCarrierStatusId);
    }

    /// <inheritdoc />
    public async ValueTask<bool> CanEnterLandsOfTrialsAsync(Player player, CancellationToken cancellationToken)
    {
        var reign = this._activeReign;
        if (!this._options.LandsOfTrials.Enabled || reign is null || player.GuildStatus is not { } guildStatus)
        {
            return false;
        }

        if (this._options.LandsOfTrials.AllowImperialGuild && guildStatus.GuildId == reign.ImperialGuildId)
        {
            return true;
        }

        return this._options.LandsOfTrials.AllowAlliances
               && player.GameContext is IGameServerContext context
               && await context.GuildServer.GetGuildRelationshipAsync(guildStatus.GuildId, reign.ImperialGuildId).ConfigureAwait(false) == MUnique.OpenMU.Interfaces.GuildRelationship.Union;
    }

    /// <inheritdoc />
    public async ValueTask<bool> HandleLandsOfTrialsEntryAsync(Player player, CancellationToken cancellationToken)
    {
        if (!this._options.LandsOfTrials.Enabled
            || player.CurrentMap is not { } currentMap
            || currentMap.Definition.Number != this._options.EventMapId
            || !currentMap.GetNpcsInRange(player.Position, 5).Any(npc => npc.Definition.Number == this._options.LandsOfTrials.GatekeeperNpcId))
        {
            return false;
        }

        if (this.State is not (ValoriaThroneEventState.Idle or ValoriaThroneEventState.Cooldown))
        {
            await this._messenger.SendToPlayerAsync(player, "Lands of Trials não está disponível durante a batalha pelo Trono de Valoria.", cancellationToken).ConfigureAwait(false);
            return true;
        }

        if (!await this.CanEnterLandsOfTrialsAsync(player, cancellationToken).ConfigureAwait(false))
        {
            await this._messenger.SendToPlayerAsync(player, "Somente a Guild Imperial e suas alianças podem entrar em Lands of Trials.", cancellationToken).ConfigureAwait(false);
            return true;
        }

        var map = await player.GameContext.GetMapAsync(this._options.LandsOfTrials.MapId).ConfigureAwait(false);
        if (map is null)
        {
            await this._messenger.SendToPlayerAsync(player, "Lands of Trials não está disponível neste servidor.", cancellationToken).ConfigureAwait(false);
            return true;
        }

        player.OpenedNpc = null;
        if (player.PlayerState.CurrentState == PlayerState.NpcDialogOpened)
        {
            await player.PlayerState.TryAdvanceToAsync(PlayerState.EnteredWorld).ConfigureAwait(false);
        }

        await player.TeleportToMapAsync(map, new Point(this._options.LandsOfTrials.EntryPositionX, this._options.LandsOfTrials.EntryPositionY)).ConfigureAwait(false);
        await this._messenger.SendToPlayerAsync(player, "A Guild Imperial controla Lands of Trials. Você recebeu permissão para entrar.", cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <inheritdoc />
    public async ValueTask<bool> SelectEraAsync(Player player, ImperialEra era, CancellationToken cancellationToken)
    {
        string announcement;
        using (await this._lock.LockAsync(cancellationToken).ConfigureAwait(false))
        {
            var reign = this._activeReign;
            if (!this.CanSelectEra(player, era, reign))
            {
                return false;
            }

            var previousEra = reign!.SelectedEra;
            var previousSelectedAt = reign.EraSelectedAt;
            reign.SelectedEra = era;
            reign.EraSelectedAt = this._timeProvider.GetUtcNow();
            if (!await this.PersistConfigurationAsync(cancellationToken).ConfigureAwait(false))
            {
                reign.SelectedEra = previousEra;
                reign.EraSelectedAt = previousSelectedAt;
                await this._messenger.SendToPlayerAsync(player, "A Era não foi proclamada porque não foi possível persistir o reinado.", cancellationToken).ConfigureAwait(false);
                return false;
            }

            announcement = $"O Imperador {reign.EmperorCharacterName}, da guild {reign.ImperialGuildName}, proclamou a {ImperialEraPresentation.GetName(era)}! {this.GetEraDescription(era)}";
        }

        await this.BroadcastAsync(announcement, cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <inheritdoc />
    public string GetEraDescription(ImperialEra era) => ImperialEraPresentation.GetDescription(era, this._options.ImperialEras);

    /// <inheritdoc />
    public async ValueTask<ValoriaSeniorInteractionResult> HandleSeniorInteractionAsync(Player player, NonPlayerCharacter npc, CancellationToken cancellationToken)
    {
        var isEventSenior = npc.Definition.Number == this._options.SeniorNpcId
                            && npc.CurrentMap.Definition.Number == this._options.EventMapId;
        if (isEventSenior
            && this.State is ValoriaThroneEventState.Idle or ValoriaThroneEventState.Cooldown
            && this._activeReign is { } reign
            && player.SelectedCharacter?.Id == reign.EmperorCharacterId)
        {
            if (reign.SelectedEra != ImperialEra.None)
            {
                await this._messenger.SendToPlayerAsync(
                    player,
                    $"{ImperialEraPresentation.GetName(reign.SelectedEra)} está ativa neste reinado. A escolha é definitiva.",
                    cancellationToken).ConfigureAwait(false);
                return ValoriaSeniorInteractionResult.Rejected;
            }

            if (this.IsEraSelectionWindowOpen(reign))
            {
                return ValoriaSeniorInteractionResult.EraSelectionRequested;
            }
        }

        if (npc.Definition.Number == this._options.LandsOfTrials.GatekeeperNpcId
            && npc.CurrentMap.Definition.Number == this._options.EventMapId)
        {
            return await this.HandleLandsOfTrialsEntryAsync(player, cancellationToken).ConfigureAwait(false)
                ? ValoriaSeniorInteractionResult.CoronationStarted
                : ValoriaSeniorInteractionResult.Rejected;
        }

        if (npc.Definition.Number != this._options.SeniorNpcId || npc.CurrentMap.Definition.Number != this._options.EventMapId)
        {
            return ValoriaSeniorInteractionResult.NotSenior;
        }

        string message;
        var startedCoronation = false;
        using (await this._lock.LockAsync(cancellationToken).ConfigureAwait(false))
        {
            var now = this._timeProvider.GetUtcNow();
            if (this._eventInstanceId is null || this.State is ValoriaThroneEventState.Idle or ValoriaThroneEventState.Cooldown or ValoriaThroneEventState.Disabled)
            {
                message = "O Trono de Valoria aguarda um novo soberano.";
            }
            else if (this.State == ValoriaThroneEventState.GuardianBattle)
            {
                message = "Derrote o Guardiao e recupere a Coroa de Valoria.";
            }
            else if (this.State == ValoriaThroneEventState.CrownOnGround)
            {
                message = "A Coroa de Valoria aguarda um novo portador.";
            }
            else if (this.State == ValoriaThroneEventState.CoronationInProgress)
            {
                message = "A coroacao ja esta em andamento.";
            }
            else if (this.State != ValoriaThroneEventState.CrownCarried
                     || this._crown is null
                     || this._crown.HolderCharacterId != player.SelectedCharacter?.Id
                     || this._crown.HolderGuildId != player.GuildStatus?.GuildId)
            {
                message = "Somente o Portador da Coroa pode reivindicar o Trono.";
            }
            else if (!this.IsValidCoronationPosition(player, npc))
            {
                message = "Voce precisa estar proximo ao Senior para entregar a Coroa de Valoria.";
            }
            else if (this._crown.ConfirmationRequestedAt is null
                     || this._crown.SeniorObjectId != npc.Id
                     || now - this._crown.ConfirmationRequestedAt > this._options.CoronationConfirmationDuration)
            {
                this._crown.SeniorObjectId = npc.Id;
                this._crown.ConfirmationRequestedAt = now;
                await this.SaveSnapshotAsync(cancellationToken).ConfigureAwait(false);
                message = "Fale novamente com o Senior para confirmar a entrega da Coroa e iniciar a coroacao.";
            }
            else
            {
                this._crown.CoronationStartedAt = now;
                this._crown.CoronationEndsAt = now.Add(this._options.CoronationDuration);
                this._crown.ConfirmationRequestedAt = null;
                this.State = ValoriaThroneEventState.CoronationInProgress;
                this._coronationCandidate = player;
                this._coronationSenior = npc;
                // Do not retain the carrier deadline. TickAsync uses this transition
                // deadline to decide whether the event has timed out.
                this.SetNextTransition(this._options.CoronationDuration);
                await this.SaveSnapshotAsync(cancellationToken).ConfigureAwait(false);
                this._logger.LogInformation("Valoria coronation started | EventInstanceId: {EventInstanceId} | EmperorCharacterId: {EmperorCharacterId} | ImperialGuildId: {ImperialGuildId} | EndsAt: {EndsAt}", this._eventInstanceId, this._crown.HolderCharacterId, this._crown.HolderGuildId, this._crown.CoronationEndsAt);
                message = $"A coroacao de {this._crown.HolderCharacterName}, da guild {this._crown.HolderGuildName}, comecou. Protejam o candidato por {FormatDuration(this._options.CoronationDuration)}.";
                startedCoronation = true;
            }
        }

        if (startedCoronation)
        {
            await this.BroadcastAsync(message, cancellationToken).ConfigureAwait(false);
            return ValoriaSeniorInteractionResult.CoronationStarted;
        }

        await this._messenger.SendToPlayerAsync(player, message, cancellationToken).ConfigureAwait(false);
        return message.StartsWith("Fale", StringComparison.Ordinal)
            ? ValoriaSeniorInteractionResult.ConfirmationRequested
            : ValoriaSeniorInteractionResult.Rejected;
    }

    private bool CanSelectEra(Player player, ImperialEra era, ImperialReign? reign)
    {
        return ImperialEraSelectionPolicy.CanSelect(
            reign,
            player.SelectedCharacter?.Id,
            era,
            this.State,
            this._timeProvider.GetUtcNow(),
            this._options.ImperialEras.SelectionDuration);
    }

    private bool IsEraSelectionWindowOpen(ImperialReign reign)
    {
        var now = this._timeProvider.GetUtcNow();
        return reign.StartedAt <= now
               && reign.ExpiresAt > now
               && reign.StartedAt.Add(this._options.ImperialEras.SelectionDuration) >= now;
    }

    /// <inheritdoc />
    public async ValueTask<bool> HandleSeniorRemovedAsync(NonPlayerCharacter npc, CancellationToken cancellationToken)
    {
        Guid? eventInstanceId;
        using (await this._lock.LockAsync(cancellationToken).ConfigureAwait(false))
        {
            eventInstanceId = this.State == ValoriaThroneEventState.CoronationInProgress && ReferenceEquals(this._coronationSenior, npc)
                ? this._eventInstanceId
                : null;
        }

        if (eventInstanceId is not { } currentEventInstanceId)
        {
            return false;
        }

        await this.ReturnCrownAsync(currentEventInstanceId, "A coroacao foi interrompida porque o Senior desapareceu.", cancellationToken).ConfigureAwait(false);
        return true;
    }

    private async ValueTask PrepareAsync(Guid eventInstanceId, CancellationToken cancellationToken)
    {
        await this._mapOperations.PrepareAsync(eventInstanceId, cancellationToken).ConfigureAwait(false);
        await this.TransitionAsync(eventInstanceId, ValoriaThroneEventState.Announcing, ValoriaThroneEventState.Preparing, this._options.PreparationDuration, cancellationToken).ConfigureAwait(false);
        await this.BroadcastAsync($"Valley of Loren foi fechado para preparação. Duração: {FormatDuration(this._options.PreparationDuration)}.", cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask OpenRegistrationAsync(Guid eventInstanceId, CancellationToken cancellationToken)
    {
        await this.TransitionAsync(eventInstanceId, ValoriaThroneEventState.Preparing, ValoriaThroneEventState.RegistrationOpen, this._options.RegistrationDuration, cancellationToken).ConfigureAwait(false);
        await this.BroadcastAsync($"As entradas para o Trono de Valoria estão abertas no server {(this._options.EventServerId ?? 0) + 1}. Duração: {FormatDuration(this._options.RegistrationDuration)}.", cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask StartBattleAsync(Guid eventInstanceId, CancellationToken cancellationToken)
    {
        var guardianId = await this._mapOperations.SpawnGuardianAsync(eventInstanceId, cancellationToken).ConfigureAwait(false);
        using (await this._lock.LockAsync(cancellationToken).ConfigureAwait(false))
        {
            if (this._eventInstanceId != eventInstanceId || this.State != ValoriaThroneEventState.RegistrationOpen)
            {
                return;
            }

            this._guardianId = guardianId;
            this.State = ValoriaThroneEventState.GuardianBattle;
            this.SetNextTransition(this._options.BattleDuration);
            await this.SaveSnapshotAsync(cancellationToken).ConfigureAwait(false);
        }

        await this.BroadcastAsync($"O Guardião do Trono despertou com {this._options.SupportMonsters.Count} mobs de apoio. Duração da batalha: {FormatDuration(this._options.BattleDuration)}.", cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask CompleteCoronationAsync(Guid eventInstanceId, CancellationToken cancellationToken)
    {
        string coronationCompletedMessage;
        string newEmperorMessage;
        Guid winnerCharacterId;
        using (await this._lock.LockAsync(cancellationToken).ConfigureAwait(false))
        {
            if (this._eventInstanceId != eventInstanceId
                || this.State != ValoriaThroneEventState.CoronationInProgress
                || this._crown is null)
            {
                return;
            }

            this._activeReign = new ImperialReign { Id = Guid.NewGuid(), EmperorCharacterId = this._crown.HolderCharacterId!.Value, EmperorCharacterName = this._crown.HolderCharacterName ?? string.Empty, ImperialGuildId = this._crown.HolderGuildId!.Value, ImperialGuildName = this._crown.HolderGuildName ?? string.Empty, StartedAt = this._timeProvider.GetUtcNow(), ExpiresAt = this._timeProvider.GetUtcNow().Add(this._options.ReignDuration) };
            this._options.ImperialReign = this._activeReign;
            winnerCharacterId = this._activeReign.EmperorCharacterId;
            coronationCompletedMessage = $"A coroação de {this._activeReign.EmperorCharacterName} foi concluída";
            newEmperorMessage = $"{this._activeReign.EmperorCharacterName} é o novo imperador de Valoria";
            this.State = ValoriaThroneEventState.Finishing;
            this._nextTransitionAt = null;
            await this.SaveSnapshotAsync(cancellationToken).ConfigureAwait(false);
        }

        if (!await this.PersistConfigurationAsync(cancellationToken).ConfigureAwait(false))
        {
            // Keep the candidate and crown context intact. The next tick can safely
            // retry persistence; do not announce an emperor which was not saved.
            using (await this._lock.LockAsync(cancellationToken).ConfigureAwait(false))
            {
                if (this._eventInstanceId == eventInstanceId && this.State == ValoriaThroneEventState.Finishing)
                {
                    this.State = ValoriaThroneEventState.CoronationInProgress;
                    this.SetNextTransition(TimeSpan.FromSeconds(30));
                    await this.SaveSnapshotAsync(cancellationToken).ConfigureAwait(false);
                }
            }

            await this.BroadcastAsync("A coroação aguarda a confirmação de persistência. Administradores devem verificar os logs do Valoria.", cancellationToken).ConfigureAwait(false);
            return;
        }
        await this.SynchronizeConnectedEmperorStatusAsync(winnerCharacterId, cancellationToken).ConfigureAwait(false);
        await this._mapOperations.EvacuateLandsOfTrialsAsync(cancellationToken).ConfigureAwait(false);

        this._logger.LogInformation("Valoria coronation completed for {EventInstanceId}, character {HolderCharacterId}, runtime guild {HolderGuildId}.", eventInstanceId, this._crown?.HolderCharacterId, this._crown?.HolderGuildId);
        await this.BroadcastAsync(coronationCompletedMessage, cancellationToken).ConfigureAwait(false);
        await this.BroadcastAsync(newEmperorMessage, cancellationToken).ConfigureAwait(false);
        await this.StopAsync(ValoriaThroneStopReason.Completed, cancellationToken).ConfigureAwait(false);
    }

    private bool IsCoronationCandidateValid()
    {
        return this._crown is not null
               && this._coronationCandidate is { } candidate
               && this._coronationSenior is { } senior
               && this.IsValidCoronationPosition(candidate, senior)
               && candidate.SelectedCharacter?.Id == this._crown.HolderCharacterId
               && candidate.GuildStatus?.GuildId == this._crown.HolderGuildId;
    }

    private bool IsValidCoronationPosition(Player player, NonPlayerCharacter senior)
    {
        return player.IsAlive
               && player.CurrentMap?.Definition.Number == this._options.EventMapId
               && ReferenceEquals(player.CurrentMap, senior.CurrentMap)
               && player.GetDistanceTo(senior) <= this._options.CoronationRadius;
    }

    private async ValueTask SpawnCrownAsync(Guid eventInstanceId, string announcement, CancellationToken cancellationToken)
    {
        Point position;
        using (await this._lock.LockAsync(cancellationToken).ConfigureAwait(false))
        {
            if (this._eventInstanceId != eventInstanceId || this.State != ValoriaThroneEventState.CrownOnGround || this._crown is null)
            {
                return;
            }

            position = this._crown.OriginalDropPosition;
        }

        try
        {
            var crownGroundItemId = await this._mapOperations.SpawnCrownAsync(eventInstanceId, position, cancellationToken).ConfigureAwait(false);
            using (await this._lock.LockAsync(cancellationToken).ConfigureAwait(false))
            {
                if (this._eventInstanceId != eventInstanceId || this.State != ValoriaThroneEventState.CrownOnGround || this._crown is null)
                {
                    await this._mapOperations.RemoveCrownAsync(eventInstanceId, cancellationToken).ConfigureAwait(false);
                    return;
                }

                this._crown.GroundItemId = crownGroundItemId;
                await this.SaveSnapshotAsync(cancellationToken).ConfigureAwait(false);
            }

            this._logger.LogInformation("Valoria crown {CrownGroundItemId} is available for {EventInstanceId} at {Position}.", crownGroundItemId, eventInstanceId, position);
            await this.BroadcastAsync(announcement, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            this._logger.LogError(exception, "Could not create the Valoria crown for {EventInstanceId}.", eventInstanceId);
            await this.StopAsync(ValoriaThroneStopReason.Failure, cancellationToken).ConfigureAwait(false);
        }
    }

    private async ValueTask ReturnCrownAsync(Guid eventInstanceId, string announcement, CancellationToken cancellationToken)
    {
        Player? crownCarrier;
        using (await this._lock.LockAsync(cancellationToken).ConfigureAwait(false))
        {
            if (this._eventInstanceId != eventInstanceId
                || this.State is not (ValoriaThroneEventState.CrownCarried or ValoriaThroneEventState.CoronationInProgress)
                || this._crown is null)
            {
                return;
            }

            this._crown.ClearHolder();
            crownCarrier = this._crownCarrier;
            this._crownCarrier = null;
            this.State = ValoriaThroneEventState.CrownOnGround;
            this._crownRespawnAt = this._timeProvider.GetUtcNow().Add(this._options.CrownRespawnDelay);
            this._coronationCandidate = null;
            this._coronationSenior = null;
            await this.SaveSnapshotAsync(cancellationToken).ConfigureAwait(false);
        }

        await this.SetCrownCarrierMarkerAsync(crownCarrier, false).ConfigureAwait(false);
        if (crownCarrier is not null)
        {
            await this.SynchronizeCrownCarrierStatusAsync(crownCarrier, false).ConfigureAwait(false);
        }
        await this.BroadcastAsync($"{announcement} A Coroa retornara ao local da queda do Guardiao em {FormatDuration(this._options.CrownRespawnDelay)}.", cancellationToken).ConfigureAwait(false);
    }

    private ValueTask SetCrownCarrierMarkerAsync(Player? player, bool isActive)
    {
        return player is null
            ? ValueTask.CompletedTask
            : player.ForEachWorldObserverAsync<IWorldObjectMarkerPlugIn>(
                view => view.SetMarkerAsync(player, this._options.CrownCarrierMarkerId, isActive),
                true);
    }

    private async ValueTask TransitionAsync(Guid eventInstanceId, ValoriaThroneEventState expectedState, ValoriaThroneEventState nextState, TimeSpan duration, CancellationToken cancellationToken)
    {
        using (await this._lock.LockAsync(cancellationToken).ConfigureAwait(false))
        {
            if (this._eventInstanceId != eventInstanceId || this.State != expectedState)
            {
                return;
            }

            this.State = nextState;
            this.SetNextTransition(duration);
            await this.SaveSnapshotAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private void SetNextTransition(TimeSpan duration)
    {
        this._nextTransitionAt = this._timeProvider.GetUtcNow().Add(duration);
        this._currentStageDurationSeconds = (int)Math.Ceiling(duration.TotalSeconds);
        this._lastCountdownMinute = this._currentStageDurationSeconds > 0
            ? (int)Math.Ceiling(duration.TotalMinutes)
            : null;
        this._announcedThirtySeconds = false;
        this._announcedTenSeconds = false;
    }

    private string? GetCountdownMessage(ValoriaThroneEventState state, TimeSpan remaining)
    {
        var remainingSeconds = Math.Max(0, (int)Math.Ceiling(remaining.TotalSeconds));
        var remainingMinutes = (int)Math.Ceiling(remaining.TotalMinutes);
        if (remainingMinutes > 0 && remainingMinutes < this._lastCountdownMinute)
        {
            this._lastCountdownMinute = remainingMinutes;
            return this.CreateCountdownMessage(state, TimeSpan.FromSeconds(remainingSeconds));
        }

        if (remainingSeconds <= 10 && this._currentStageDurationSeconds > 10 && !this._announcedTenSeconds)
        {
            this._announcedTenSeconds = true;
            return this.CreateCountdownMessage(state, TimeSpan.FromSeconds(remainingSeconds));
        }

        if (remainingSeconds is > 10 and <= 30 && this._currentStageDurationSeconds > 30 && !this._announcedThirtySeconds)
        {
            this._announcedThirtySeconds = true;
            return this.CreateCountdownMessage(state, TimeSpan.FromSeconds(remainingSeconds));
        }

        return null;
    }

    private string CreateCountdownMessage(ValoriaThroneEventState state, TimeSpan remaining)
    {
        if (state == ValoriaThroneEventState.CoronationInProgress && this._crown?.HolderCharacterName is { Length: > 0 } playerName)
        {
            return $"{playerName} será coroado imperador em {FormatDuration(remaining)}";
        }

        var phase = state switch
        {
            ValoriaThroneEventState.Announcing => "O Trono de Valoria começará",
            ValoriaThroneEventState.Preparing => "A preparação terminará",
            ValoriaThroneEventState.RegistrationOpen => "As inscrições encerrarão",
            ValoriaThroneEventState.GuardianBattle => "A batalha terminará",
            ValoriaThroneEventState.CrownOnGround => "A Coroa de Valoria deixará de estar disponível",
            ValoriaThroneEventState.CrownCarried => "A fase da Coroa de Valoria terminará",
            ValoriaThroneEventState.Cooldown => "O novo evento estará disponível",
            _ => "A próxima etapa começará",
        };
        return $"{phase} em {FormatDuration(remaining)}.";
    }

    private static string FormatDuration(TimeSpan duration)
    {
        var seconds = Math.Max(0, (int)Math.Ceiling(duration.TotalSeconds));
        var roundedDuration = TimeSpan.FromSeconds(seconds);
        return roundedDuration.Hours > 0
            ? $"{(int)roundedDuration.TotalHours}h {roundedDuration.Minutes:D2}min"
            : roundedDuration.Minutes > 0
                ? $"{roundedDuration.Minutes}min {roundedDuration.Seconds:D2}s"
                : $"{roundedDuration.Seconds}s";
    }

    private async ValueTask BroadcastAsync(string message, CancellationToken cancellationToken)
    {
        foreach (var context in this._runtimeRegistry.Contexts)
        {
            await this._messenger.SendGlobalAsync(context, message, cancellationToken).ConfigureAwait(false);
        }
    }

    private async ValueTask SynchronizeConnectedEmperorStatusAsync(Guid emperorCharacterId, CancellationToken cancellationToken)
    {
        foreach (var context in this._runtimeRegistry.Contexts)
        {
            foreach (var player in await context.GetPlayersAsync().ConfigureAwait(false))
            {
                if (player.SelectedCharacter?.Id == emperorCharacterId || player.MagicEffectList.ActiveEffects.ContainsKey(ValoriaEmperorStatusId))
                {
                    await this.SynchronizeEmperorStatusAsync(player, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }

    private ValueTask SaveSnapshotAsync(CancellationToken cancellationToken)
    {
        return this._stateStore.SaveAsync(this.GetSnapshot(), cancellationToken);
    }

    private async ValueTask<bool> PersistConfigurationAsync(CancellationToken cancellationToken)
    {
        var context = this._runtimeRegistry.Contexts.FirstOrDefault();
        if (context is null)
        {
            this._logger.LogError("Valoria persistence failed because no game-server context is registered.");
            return false;
        }

        try
        {
            using var persistenceContext = context.PersistenceContextProvider.CreateNewContext();
            var configurations = await persistenceContext.GetAsync<GameConfiguration>(cancellationToken).ConfigureAwait(false);
            var targets = configurations
                .SelectMany(configuration => configuration.PlugInConfigurations
                    .Where(plugInConfiguration => plugInConfiguration.TypeId == typeof(ValoriaThronePlugIn).GUID)
                    .Select(plugInConfiguration => (configuration, plugInConfiguration)))
                .ToArray();
            if (targets.Length == 0)
            {
                this._logger.LogError("Valoria persistence failed because no ValoriaThrone PlugInConfiguration was found.");
                return false;
            }

            foreach (var (configuration, plugInConfiguration) in targets)
            {
                var persistedOptions = plugInConfiguration.GetConfiguration<ValoriaThroneOptions>(context.PlugInManager.CustomConfigReferenceHandler) ?? ValoriaThroneOptions.Default;
                persistedOptions.ImperialReign = CloneReign(this._activeReign);
                plugInConfiguration.SetConfiguration(persistedOptions, context.PlugInManager.CustomConfigReferenceHandler);
                this._logger.LogInformation("Valoria persistence target | GameConfigurationId: {GameConfigurationId} | GameConfigurationName: {GameConfigurationName} | PlugInConfigurationId: {PlugInConfigurationId} | ServerId: {ServerId} | EmperorCharacterId: {EmperorCharacterId} | ImperialGuildId: {ImperialGuildId} | SelectedEra: {SelectedEra}", GetPersistentId(configuration), configuration.Name, GetPersistentId(plugInConfiguration), context.Id, this._activeReign?.EmperorCharacterId, this._activeReign?.ImperialGuildId, this._activeReign?.SelectedEra);
            }

            await persistenceContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (Exception exception)
        {
            this._logger.LogError(exception, "Valoria persistence failed | EmperorCharacterId: {EmperorCharacterId} | ImperialGuildId: {ImperialGuildId} | SelectedEra: {SelectedEra}", this._activeReign?.EmperorCharacterId, this._activeReign?.ImperialGuildId, this._activeReign?.SelectedEra);
            return false;
        }
    }

    private static ImperialReign? SelectAuthoritativeReign(ImperialReign? current, ImperialReign? candidate)
    {
        if (candidate is null)
        {
            return current;
        }

        return current is null || candidate.StartedAt > current.StartedAt || (candidate.StartedAt == current.StartedAt && candidate.Id.CompareTo(current.Id) > 0)
            ? CloneReign(candidate)
            : current;
    }

    private static ImperialReign? CloneReign(ImperialReign? reign) => reign is null
        ? null
        : new ImperialReign
        {
            Id = reign.Id,
            EmperorCharacterId = reign.EmperorCharacterId,
            EmperorCharacterName = reign.EmperorCharacterName,
            ImperialGuildId = reign.ImperialGuildId,
            ImperialGuildName = reign.ImperialGuildName,
            StartedAt = reign.StartedAt,
            ExpiresAt = reign.ExpiresAt,
            SelectedEra = reign.SelectedEra,
            EraSelectedAt = reign.EraSelectedAt,
        };

    private static Guid? GetPersistentId(object value) => (value as MUnique.OpenMU.Persistence.IIdentifiable)?.Id;

    private async ValueTask SelectDefaultEraIfDueAsync(CancellationToken cancellationToken)
    {
        var reign = this._activeReign;
        if (reign is null || reign.SelectedEra != ImperialEra.None || reign.StartedAt.Add(this._options.ImperialEras.SelectionDuration) > this._timeProvider.GetUtcNow())
        {
            return;
        }

        reign.SelectedEra = this._options.ImperialEras.DefaultEra;
        reign.EraSelectedAt = this._timeProvider.GetUtcNow();
        if (!await this.PersistConfigurationAsync(cancellationToken).ConfigureAwait(false))
        {
            reign.SelectedEra = ImperialEra.None;
            reign.EraSelectedAt = null;
            this._logger.LogError("Valoria default era was not announced because persistence failed | EmperorCharacterId: {EmperorCharacterId}", reign.EmperorCharacterId);
            return;
        }
        await this.BroadcastAsync($"O prazo de escolha terminou. {ImperialEraPresentation.GetName(reign.SelectedEra)} foi proclamada automaticamente. {this.GetEraDescription(reign.SelectedEra)}", cancellationToken).ConfigureAwait(false);
    }

    private sealed class ValoriaStatusMagicEffectDefinition : MagicEffectDefinition
    {
        public ValoriaStatusMagicEffectDefinition()
        {
            this.PowerUpDefinitions = new List<PowerUpDefinition>(0);
        }
    }
}
