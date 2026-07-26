// <copyright file="ValoriaThroneRuntimeRegistry.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.PlugIns.ValoriaThrone.Services;

using System.Collections.Concurrent;
using MUnique.OpenMU.GameLogic;

/// <summary>
/// Tracks the game-server contexts hosted by the current process.
/// </summary>
public sealed class ValoriaThroneRuntimeRegistry
{
    private readonly ConcurrentDictionary<byte, IGameServerContext> _contexts = new();

    /// <summary>
    /// Registers a local game-server context.
    /// </summary>
    /// <param name="context">The context to register.</param>
    public void Register(IGameServerContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        this._contexts[context.Id] = context;
    }

    /// <summary>
    /// Gets all known contexts.
    /// </summary>
    public IReadOnlyCollection<IGameServerContext> Contexts => this._contexts.Values.ToArray();
}
