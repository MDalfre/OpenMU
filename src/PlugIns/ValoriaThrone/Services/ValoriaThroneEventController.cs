// <copyright file="ValoriaThroneEventController.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.PlugIns.ValoriaThrone.Services;

using Microsoft.Extensions.Logging;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.PlugIns.ValoriaThrone.Configuration;
using MUnique.OpenMU.PlugIns.ValoriaThrone.Domain;
using Nito.AsyncEx;

/// <summary>
/// Coordinates the state transitions of one event execution.
/// </summary>
public sealed class ValoriaThroneEventController : IValoriaThroneEventController
{
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
    private int _administratorStartRequested;

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
    public void Configure(ValoriaThroneOptions options)
    {
        ValoriaThroneOptionsValidator.Validate(options);
        this._options = options;
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
        this._runtimeRegistry.Register(context);
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
        using (await this._lock.LockAsync(cancellationToken).ConfigureAwait(false))
        {
            if (!this._options.Enabled || this.State != ValoriaThroneEventState.Idle)
            {
                return false;
            }

            eventInstanceId = Guid.NewGuid();
            this._eventInstanceId = eventInstanceId;
            this._guardianId = null;
            this.State = ValoriaThroneEventState.Announcing;
            this._nextTransitionAt = this._timeProvider.GetUtcNow().Add(this._options.AnnouncementDuration);
            await this.SaveSnapshotAsync(cancellationToken).ConfigureAwait(false);
        }

        this._logger.LogInformation("Valoria Throne {EventInstanceId} started by {Reason}.", eventInstanceId, reason);
        await this.BroadcastAsync("O Trono de Valoria começará em breve.", cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <inheritdoc />
    public async ValueTask StopAsync(ValoriaThroneStopReason reason, CancellationToken cancellationToken)
    {
        Guid? eventInstanceId;
        using (await this._lock.LockAsync(cancellationToken).ConfigureAwait(false))
        {
            eventInstanceId = this._eventInstanceId;
            if (eventInstanceId is null && this.State is ValoriaThroneEventState.Idle or ValoriaThroneEventState.Disabled)
            {
                return;
            }

            this.State = ValoriaThroneEventState.Finishing;
            this._nextTransitionAt = null;
        }

        try
        {
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
                    this.State = this._options.Enabled ? ValoriaThroneEventState.Cooldown : ValoriaThroneEventState.Disabled;
                    this._nextTransitionAt = this.State == ValoriaThroneEventState.Cooldown
                        ? this._timeProvider.GetUtcNow().Add(this._options.CooldownDuration)
                        : null;
                    await this.SaveSnapshotAsync(cancellationToken).ConfigureAwait(false);
                    stopped = true;
                }
            }

            if (stopped)
            {
                this._logger.LogInformation("Valoria Throne {EventInstanceId} stopped by {Reason}.", eventInstanceId, reason);
            }
        }
    }

    /// <inheritdoc />
    public async ValueTask TickAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref this._administratorStartRequested, 0) == 1)
        {
            await this.StartAsync(ValoriaThroneStartReason.Administrator, cancellationToken).ConfigureAwait(false);
            return;
        }

        ValoriaThroneEventState state;
        Guid? eventInstanceId;
        using (await this._lock.LockAsync(cancellationToken).ConfigureAwait(false))
        {
            if (this._nextTransitionAt is { } nextTransitionAt && nextTransitionAt > this._timeProvider.GetUtcNow())
            {
                return;
            }

            state = this.State;
            eventInstanceId = this._eventInstanceId;
            if (state == ValoriaThroneEventState.Cooldown)
            {
                this.State = ValoriaThroneEventState.Idle;
                this._nextTransitionAt = null;
                await this.SaveSnapshotAsync(cancellationToken).ConfigureAwait(false);
                return;
            }
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
            case ValoriaThroneEventState.InProgress:
            case ValoriaThroneEventState.CrownAvailable:
                await this.StopAsync(ValoriaThroneStopReason.Timeout, cancellationToken).ConfigureAwait(false);
                break;
        }
    }

    /// <inheritdoc />
    public ValoriaThroneSnapshot GetSnapshot() => new(this._eventInstanceId, this.State, this._nextTransitionAt, this._guardianId);

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
        return this.State == ValoriaThroneEventState.InProgress;
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
                || this.State != ValoriaThroneEventState.InProgress
                || !this._mapOperations.IsCurrentGuardian(killed, currentEventInstanceId))
            {
                return false;
            }

            eventInstanceId = currentEventInstanceId;
            this.State = ValoriaThroneEventState.CrownAvailable;
            this._nextTransitionAt = this._timeProvider.GetUtcNow().Add(this._options.CrownPhaseDuration);
            await this.SaveSnapshotAsync(cancellationToken).ConfigureAwait(false);
        }

        var killerName = killed.LastDeath?.KillerName ?? "Um aventureiro";
        this._logger.LogInformation("Valoria guardian {GuardianId} was defeated during {EventInstanceId} by {KillerName}.", killed.Id, eventInstanceId, killerName);
        await this.BroadcastAsync($"{killer?.GetName() ?? "Um aventureiro"} derrotou o Guardião do Trono.", cancellationToken).ConfigureAwait(false);
        return true;
    }

    private async ValueTask PrepareAsync(Guid eventInstanceId, CancellationToken cancellationToken)
    {
        await this._mapOperations.PrepareAsync(eventInstanceId, cancellationToken).ConfigureAwait(false);
        await this.TransitionAsync(eventInstanceId, ValoriaThroneEventState.Announcing, ValoriaThroneEventState.Preparing, this._options.PreparationDuration, cancellationToken).ConfigureAwait(false);
        await this.BroadcastAsync("Valley of Loren foi fechado para preparação.", cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask OpenRegistrationAsync(Guid eventInstanceId, CancellationToken cancellationToken)
    {
        await this.TransitionAsync(eventInstanceId, ValoriaThroneEventState.Preparing, ValoriaThroneEventState.RegistrationOpen, this._options.RegistrationDuration, cancellationToken).ConfigureAwait(false);
        await this.BroadcastAsync("As entradas para o Trono de Valoria estão abertas no servidor PvP.", cancellationToken).ConfigureAwait(false);
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
            this.State = ValoriaThroneEventState.InProgress;
            this._nextTransitionAt = this._timeProvider.GetUtcNow().Add(this._options.BattleDuration);
            await this.SaveSnapshotAsync(cancellationToken).ConfigureAwait(false);
        }

        await this.BroadcastAsync("O Guardião do Trono despertou.", cancellationToken).ConfigureAwait(false);
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
            this._nextTransitionAt = this._timeProvider.GetUtcNow().Add(duration);
            await this.SaveSnapshotAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async ValueTask BroadcastAsync(string message, CancellationToken cancellationToken)
    {
        foreach (var context in this._runtimeRegistry.Contexts)
        {
            await this._messenger.SendGlobalAsync(context, message, cancellationToken).ConfigureAwait(false);
        }
    }

    private ValueTask SaveSnapshotAsync(CancellationToken cancellationToken)
    {
        return this._stateStore.SaveAsync(this.GetSnapshot(), cancellationToken);
    }
}
