// <copyright file="HuntingZoneEnterRequestHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.MessageHandler;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.Network.Packets.ClientToServer;
using MUnique.OpenMU.PlugIns;

/// <summary>Handles the client's request to enter the castle hunting zone.</summary>
[PlugIn]
[Guid("E4CF3F05-E176-48BB-B995-7EACD8272833")]
[BelongsToGroup(CastleSiegeGroupHandlerPlugIn.GroupKey)]
internal sealed class HuntingZoneEnterRequestHandlerPlugIn : ISubPacketHandlerPlugIn
{
    /// <inheritdoc />
    public byte Key => CastleSiegeHuntingZoneEnterRequest.SubCode;

    /// <inheritdoc />
    public bool IsEncryptionExpected => false;

    /// <inheritdoc />
    public async ValueTask HandlePacketAsync(Player player, Memory<byte> packet)
    {
        if (packet.Length < CastleSiegeHuntingZoneEnterRequest.Length)
        {
            return;
        }

        CastleSiegeHuntingZoneEnterRequest request = packet;
        var arguments = new IHuntingZoneEnterRequestPlugIn.HuntingZoneEnterRequestArguments(request.Money);
        if (player.GameContext.PlugInManager.GetPlugInPoint<IHuntingZoneEnterRequestPlugIn>() is { } plugInPoint)
        {
            await plugInPoint.HandleHuntingZoneEnterRequestAsync(player, arguments).ConfigureAwait(false);
        }
    }
}
