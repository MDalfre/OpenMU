// <copyright file="GameContextPlayerCountTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Tests the player count notifications of the game context.
/// </summary>
[TestFixture]
public class GameContextPlayerCountTests
{
    /// <summary>
    /// Ensures that adding and removing players emits the current player count.
    /// </summary>
    [Test]
    public async ValueTask AddAndRemovePlayerRaisesPlayerCountChanged()
    {
        var gameContext = GameContextTestHelper.CreateGameContext();
        var playerCounts = new List<int>();
        gameContext.PlayerCountChanged += (_, playerCount) => playerCounts.Add(playerCount);
        var player = new TestPlayer(gameContext);

        await gameContext.AddPlayerAsync(player);
        await gameContext.RemovePlayerAsync(player);

        Assert.That(playerCounts, Is.EqualTo(new[] { 1, 0 }));
    }

    private class TestPlayer : Player
    {
        public TestPlayer(IGameContext gameContext)
            : base(gameContext)
        {
        }

        protected override ICustomPlugInContainer<IViewPlugIn> CreateViewPlugInContainer()
        {
            return new MockViewPlugInContainer();
        }
    }
}
