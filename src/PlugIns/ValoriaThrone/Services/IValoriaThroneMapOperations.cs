// <copyright file="IValoriaThroneMapOperations.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.PlugIns.ValoriaThrone.Services;

using MUnique.OpenMU.GameLogic;
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

    /// <summary>Creates the guardian in the authorized game server.</summary>
    ValueTask<ushort?> SpawnGuardianAsync(Guid eventInstanceId, CancellationToken cancellationToken);

    /// <summary>Removes temporary event entities and evacuates remaining local players.</summary>
    ValueTask CleanupAsync(Guid? eventInstanceId, CancellationToken cancellationToken);

    /// <summary>Determines whether an attackable is the active guardian.</summary>
    bool IsCurrentGuardian(IAttackable attackable, Guid eventInstanceId);
}
