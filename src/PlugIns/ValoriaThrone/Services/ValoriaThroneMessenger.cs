// <copyright file="ValoriaThroneMessenger.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.PlugIns.ValoriaThrone.Services;

using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.Interfaces;

/// <summary>
/// Default event messenger.
/// </summary>
public sealed class ValoriaThroneMessenger : IValoriaThroneMessenger
{
    /// <inheritdoc />
    public async ValueTask SendGlobalAsync(IGameContext context, string message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await context.SendGlobalMessageAsync(message, MessageType.GoldenCenter).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask SendToPlayerAsync(Player player, string message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await player.ShowBlueMessageAsync(message).ConfigureAwait(false);
    }
}
