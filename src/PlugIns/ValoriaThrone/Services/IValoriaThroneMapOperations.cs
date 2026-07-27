// <copyright file="IValoriaThroneMapOperations.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.PlugIns.ValoriaThrone.Services;

using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.Pathfinding;
using MUnique.OpenMU.PlugIns.ValoriaThrone.Configuration;
using MUnique.OpenMU.PlugIns.ValoriaThrone.Domain;

/// <summary>
/// Applies local map effects for an event execution.
/// </summary>
public interface IValoriaThroneMapOperations
{
    /// <summary>Configures map operations.</summary>
    void Configure(ValoriaThroneOptions options);

    /// <summary>Prepares all local event maps and evacuates their players.</summary>
    ValueTask PrepareAsync(Guid eventInstanceId, CancellationToken cancellationToken);

    /// <summary>Evacuates the event map on all local game servers.</summary>
    ValueTask EvacuateAsync(CancellationToken cancellationToken);

    /// <summary>Evacuates local players from Lands of Trials.</summary>
    ValueTask EvacuateLandsOfTrialsAsync(CancellationToken cancellationToken);

    /// <summary>Creates the guardian in the authorized game server.</summary>
    ValueTask<ushort?> SpawnGuardianAsync(Guid eventInstanceId, CancellationToken cancellationToken);

    /// <summary>Creates the crown representation at the specified event position.</summary>
    ValueTask<ushort?> SpawnCrownAsync(Guid eventInstanceId, Point position, CancellationToken cancellationToken);

    /// <summary>Removes the crown representation of the specified event execution.</summary>
    ValueTask RemoveCrownAsync(Guid eventInstanceId, CancellationToken cancellationToken);

    /// <summary>Removes temporary event entities and evacuates remaining local players.</summary>
    ValueTask CleanupAsync(Guid? eventInstanceId, CancellationToken cancellationToken);

    /// <summary>Determines whether an attackable is the active guardian.</summary>
    bool IsCurrentGuardian(IAttackable attackable, Guid eventInstanceId);

    /// <summary>Determines whether a dropped item is the crown of the specified execution.</summary>
    bool IsCurrentCrown(DroppedItem droppedItem, Guid eventInstanceId);
}
