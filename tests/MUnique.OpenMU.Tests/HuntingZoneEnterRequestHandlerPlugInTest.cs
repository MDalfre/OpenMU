// <copyright file="HuntingZoneEnterRequestHandlerPlugInTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameServer;
using MUnique.OpenMU.GameServer.MessageHandler;
using MUnique.OpenMU.Network.Packets.ClientToServer;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>Tests the castle hunting-zone entry packet handler.</summary>
[TestFixture]
public class HuntingZoneEnterRequestHandlerPlugInTest
{
    /// <summary>Ensures that a valid B9:05 packet reaches the registered entry policy.</summary>
    [Test]
    public async ValueTask ValidRequestIsForwardedToEntryPlugInAsync()
    {
        const uint requestedMoney = 123_456;
        var gameContext = GameContextTestHelper.CreateGameContext();
        var entryPlugIn = new HuntingZoneEntryPlugIn();
        gameContext.PlugInManager.RegisterPlugInAtPlugInPoint<IHuntingZoneEnterRequestPlugIn>(entryPlugIn);
        gameContext.PlugInManager.RegisterPlugIn<ISubPacketHandlerPlugIn, HuntingZoneEnterRequestHandlerPlugIn>();
        var player = await PlayerTestHelper.CreatePlayerAsync(gameContext).ConfigureAwait(false);
        var packet = new byte[CastleSiegeHuntingZoneEnterRequest.Length];
        var request = new CastleSiegeHuntingZoneEnterRequest(packet);
        request.Money = requestedMoney;
        var clientVersionProvider = new Mock<IClientVersionProvider>();
        clientVersionProvider.Setup(provider => provider.ClientVersion).Returns(new ClientVersion(6, 3, ClientLanguage.English));
        using var groupHandler = new CastleSiegeGroupHandlerPlugIn(clientVersionProvider.Object, gameContext.PlugInManager, NullLoggerFactory.Instance);
        groupHandler.Initialize();

        await groupHandler.HandlePacketAsync(player, packet).ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(entryPlugIn.WasCalled, Is.True);
            Assert.That(entryPlugIn.RequestedMoney, Is.EqualTo(requestedMoney));
        });
    }

    /// <summary>Ensures that a truncated request is ignored.</summary>
    [Test]
    public async ValueTask TruncatedRequestIsIgnoredAsync()
    {
        var gameContext = GameContextTestHelper.CreateGameContext();
        var entryPlugIn = new HuntingZoneEntryPlugIn();
        gameContext.PlugInManager.RegisterPlugInAtPlugInPoint<IHuntingZoneEnterRequestPlugIn>(entryPlugIn);
        var player = await PlayerTestHelper.CreatePlayerAsync(gameContext).ConfigureAwait(false);

        await new HuntingZoneEnterRequestHandlerPlugIn()
            .HandlePacketAsync(player, new byte[CastleSiegeHuntingZoneEnterRequest.Length - 1])
            .ConfigureAwait(false);

        Assert.That(entryPlugIn.WasCalled, Is.False);
    }

    [Guid("5B6967C4-A4EA-414A-8C9B-0FBA1552FE7E")]
    private sealed class HuntingZoneEntryPlugIn : IHuntingZoneEnterRequestPlugIn
    {
        /// <summary>Gets a value indicating whether the plug-in was invoked.</summary>
        public bool WasCalled { get; private set; }

        /// <summary>Gets the requested client-side entrance fee.</summary>
        public uint RequestedMoney { get; private set; }

        /// <inheritdoc />
        public ValueTask HandleHuntingZoneEnterRequestAsync(Player player, IHuntingZoneEnterRequestPlugIn.HuntingZoneEnterRequestArguments arguments)
        {
            this.WasCalled = true;
            this.RequestedMoney = arguments.RequestedMoney;
            arguments.Handled = true;
            return ValueTask.CompletedTask;
        }
    }
}
