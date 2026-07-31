// <copyright file="CastleSiegeGroupHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.MessageHandler;

using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using MUnique.OpenMU.PlugIns;

/// <summary>Handles grouped castle-siege packets.</summary>
[PlugIn]
[Guid("9112C60D-D35C-4AF3-B263-A8318D590CD2")]
internal sealed class CastleSiegeGroupHandlerPlugIn : GroupPacketHandlerPlugIn
{
    /// <summary>The castle-siege packet group key.</summary>
    internal const byte GroupKey = 0xB9;

    /// <summary>Initializes a new instance of the <see cref="CastleSiegeGroupHandlerPlugIn"/> class.</summary>
    /// <param name="clientVersionProvider">The client version provider.</param>
    /// <param name="manager">The plug-in manager.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    public CastleSiegeGroupHandlerPlugIn(IClientVersionProvider clientVersionProvider, PlugInManager manager, ILoggerFactory loggerFactory)
        : base(clientVersionProvider, manager, loggerFactory)
    {
    }

    /// <inheritdoc />
    public override byte Key => GroupKey;

    /// <inheritdoc />
    public override bool IsEncryptionExpected => false;
}
