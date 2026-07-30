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
    public bool Register(IGameServerContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (this._contexts.TryGetValue(context.Id, out var registeredContext) && ReferenceEquals(registeredContext, context))
        {
            return false;
        }

        this._contexts[context.Id] = context;
        return true;
    }

    /// <summary>
    /// Gets all known contexts.
    /// </summary>
    public IReadOnlyCollection<IGameServerContext> Contexts => this._contexts.Values.ToArray();
}
