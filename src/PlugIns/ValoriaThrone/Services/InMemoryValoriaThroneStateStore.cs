// <copyright file="InMemoryValoriaThroneStateStore.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.PlugIns.ValoriaThrone.Services;

using MUnique.OpenMU.PlugIns.ValoriaThrone.Domain;

/// <summary>
/// Keeps state only for the process lifetime.
/// </summary>
public sealed class InMemoryValoriaThroneStateStore : IValoriaThroneStateStore
{
    private ValoriaThroneSnapshot? _snapshot;

    /// <inheritdoc />
    public ValueTask<ValoriaThroneSnapshot?> LoadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(this._snapshot);
    }

    /// <inheritdoc />
    public ValueTask SaveAsync(ValoriaThroneSnapshot snapshot, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        this._snapshot = snapshot;
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask ClearAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        this._snapshot = null;
        return ValueTask.CompletedTask;
    }
}
