// <copyright file="IValoriaThroneMessenger.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.PlugIns.ValoriaThrone.Services;

using MUnique.OpenMU.GameLogic;

/// <summary>
/// Sends event messages through the game-server presentation layer.
/// </summary>
public interface IValoriaThroneMessenger
{
    /// <summary>Sends a message to all players of the specified context.</summary>
    ValueTask SendGlobalAsync(IGameContext context, string message, CancellationToken cancellationToken);

    /// <summary>Sends a message to a player.</summary>
    ValueTask SendToPlayerAsync(Player player, string message, CancellationToken cancellationToken);
}
