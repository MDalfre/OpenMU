// <copyright file="ChaosSuccessRateRequestHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.MessageHandler.Items;

using System.Buffers.Binary;
using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.PlayerActions.Items;
using MUnique.OpenMU.GameServer.RemoteView;
using MUnique.OpenMU.Network;
using MUnique.OpenMU.Network.Packets.ServerToClient;
using MUnique.OpenMU.PlugIns;

/// <summary>Handles authoritative Chaos Machine success rate requests from the open-source client.</summary>
[PlugIn]
[Guid("CC808CC4-FF42-4D80-B6CF-76FD62C71374")]
internal sealed class ChaosSuccessRateRequestHandlerPlugIn : IPacketHandlerPlugIn
{
    private const byte PacketCode = 0xFA;
    private const int RequestLength = 5;
    private const int ResponseLength = 7;
    private readonly ItemCraftAction _craftAction = new();

    /// <inheritdoc />
    public bool IsEncryptionExpected => false;

    /// <inheritdoc />
    public byte Key => PacketCode;

    /// <inheritdoc />
    public async ValueTask HandlePacketAsync(Player player, Memory<byte> packet)
    {
        if (player is not RemotePlayer remotePlayer || packet.Length != RequestLength || remotePlayer.Connection is not { } connection)
        {
            return;
        }

        var revision = BinaryPrimitives.ReadUInt16LittleEndian(packet.Span[3..]);
        var result = this._craftAction.CalculateSuccessRate(player);
        var effectiveRateBasisPoints = (ushort)Math.Clamp(Math.Round((result?.EffectiveRate ?? 0.0) * 100), ushort.MinValue, 10000);

        await connection.SendAsync(WriteResponse).ConfigureAwait(false);
        int WriteResponse()
        {
            var response = connection.Output.GetSpan(ResponseLength)[..ResponseLength];
            response[0] = 0xC1;
            response[1] = ResponseLength;
            response[2] = PacketCode;
            BinaryPrimitives.WriteUInt16LittleEndian(response[3..], revision);
            BinaryPrimitives.WriteUInt16LittleEndian(response[5..], effectiveRateBasisPoints);
            return ResponseLength;
        }
    }
}
