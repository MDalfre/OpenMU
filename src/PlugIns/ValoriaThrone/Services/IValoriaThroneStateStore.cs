// <copyright file="IValoriaThroneStateStore.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.PlugIns.ValoriaThrone.Services;

using MUnique.OpenMU.PlugIns.ValoriaThrone.Domain;

/// <summary>
/// Stores event state for a future persistent implementation.
/// </summary>
public interface IValoriaThroneStateStore
{
    /// <summary>Loads the latest snapshot.</summary>
    ValueTask<ValoriaThroneSnapshot?> LoadAsync(CancellationToken cancellationToken);

    /// <summary>Saves a snapshot.</summary>
    ValueTask SaveAsync(ValoriaThroneSnapshot snapshot, CancellationToken cancellationToken);

    /// <summary>Clears the stored state.</summary>
    ValueTask ClearAsync(CancellationToken cancellationToken);
}
