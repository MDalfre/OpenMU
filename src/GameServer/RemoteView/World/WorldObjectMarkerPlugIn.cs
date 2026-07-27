// <copyright file="WorldObjectMarkerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView.World;

using System.Buffers.Binary;
using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.GameLogic.Views.World;
using MUnique.OpenMU.Network;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Sends custom world object marker updates to the open-source client.
/// </summary>
[PlugIn]
[Guid("DB7AF489-9741-42BB-996B-EF246B71D42A")]
public sealed class WorldObjectMarkerPlugIn : IWorldObjectMarkerPlugIn
{
    private const byte PacketCode = 0xFB;
    private const int PacketLength = 7;
    private readonly RemotePlayer _player;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorldObjectMarkerPlugIn"/> class.
    /// </summary>
    /// <param name="player">The player whose client receives the update.</param>
    public WorldObjectMarkerPlugIn(RemotePlayer player)
    {
        this._player = player;
    }

    /// <inheritdoc />
    public async ValueTask SetMarkerAsync(IIdentifiable target, byte markerId, bool isActive)
    {
        if (this._player.Connection is not { } connection)
        {
            return;
        }

        await connection.SendAsync(WritePacket).ConfigureAwait(false);
        int WritePacket()
        {
            var packet = connection.Output.GetSpan(PacketLength)[..PacketLength];
            packet[0] = 0xC1;
            packet[1] = PacketLength;
            packet[2] = PacketCode;
            BinaryPrimitives.WriteUInt16LittleEndian(packet[3..], target.GetId(this._player));
            packet[5] = markerId;
            packet[6] = isActive ? (byte)1 : (byte)0;
            return PacketLength;
        }
    }
}
