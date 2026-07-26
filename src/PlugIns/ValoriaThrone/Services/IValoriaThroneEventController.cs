// <copyright file="IValoriaThroneEventController.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.PlugIns.ValoriaThrone.Services;

using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.PlugIns.ValoriaThrone.Configuration;
using MUnique.OpenMU.PlugIns.ValoriaThrone.Domain;

/// <summary>
/// Coordinates the event lifecycle.
/// </summary>
public interface IValoriaThroneEventController
{
    /// <summary>Gets the current event state.</summary>
    ValoriaThroneEventState State { get; }

    /// <summary>Configures the controller.</summary>
    void Configure(ValoriaThroneOptions options);

    /// <summary>Registers a game-server context.</summary>
    void Register(IGameServerContext context);

    /// <summary>Requests an administrative event start on the next periodic execution.</summary>
    void RequestStart();

    /// <summary>Starts an event execution.</summary>
    ValueTask<bool> StartAsync(ValoriaThroneStartReason reason, CancellationToken cancellationToken);

    /// <summary>Stops and cleans an event execution.</summary>
    ValueTask StopAsync(ValoriaThroneStopReason reason, CancellationToken cancellationToken);

    /// <summary>Executes a periodic lifecycle check.</summary>
    ValueTask TickAsync(CancellationToken cancellationToken);

    /// <summary>Handles a possible guardian death.</summary>
    ValueTask<bool> HandleGuardianKilledAsync(IAttackable killed, IAttacker? killer, CancellationToken cancellationToken);

    /// <summary>Spawns the guardian while registration is open.</summary>
    ValueTask<bool> SpawnGuardianAsync(CancellationToken cancellationToken);

    /// <summary>Gets the current snapshot.</summary>
    ValoriaThroneSnapshot GetSnapshot();
}
