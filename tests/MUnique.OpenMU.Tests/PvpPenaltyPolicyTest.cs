// <copyright file="PvpPenaltyPolicyTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.PlugIns;

/// <summary>
/// Tests the extensible PvP penalty policy.
/// </summary>
[TestFixture]
public class PvpPenaltyPolicyTest
{
    private IGameContext _gameContext = null!;
    private Player _attacker = null!;
    private Player _defender = null!;

    /// <summary>
    /// Creates two players and registers a policy which suppresses PvP penalties.
    /// </summary>
    [SetUp]
    public async ValueTask SetUpAsync()
    {
        this._gameContext = GameContextTestHelper.CreateGameContext();
        this._gameContext.PlugInManager.RegisterPlugInAtPlugInPoint<IPvpPenaltyPolicyPlugIn>(new SuppressPvpPenaltyPlugIn());
        this._attacker = await PlayerTestHelper.CreatePlayerAsync(this._gameContext).ConfigureAwait(false);
        this._defender = await PlayerTestHelper.CreatePlayerAsync(this._gameContext).ConfigureAwait(false);
        await this._attacker.CurrentMap!.AddAsync(this._defender).ConfigureAwait(false);
    }

    /// <summary>
    /// Ensures that an exempt kill doesn't change any persisted PK value.
    /// </summary>
    [Test]
    public async ValueTask SuppressedPenaltyDoesNotEscalatePlayerKillerStateAsync()
    {
        var initialState = this._attacker.SelectedCharacter!.State;
        var initialRemainingSeconds = this._attacker.SelectedCharacter.StateRemainingSeconds;
        var initialKillCount = this._attacker.SelectedCharacter.PlayerKillCount;

        await this._attacker.AfterKilledPlayerAsync(this._defender).ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(this._attacker.SelectedCharacter.State, Is.EqualTo(initialState));
            Assert.That(this._attacker.SelectedCharacter.StateRemainingSeconds, Is.EqualTo(initialRemainingSeconds));
            Assert.That(this._attacker.SelectedCharacter.PlayerKillCount, Is.EqualTo(initialKillCount));
        });
    }

    /// <summary>
    /// Ensures that exempt PvP hits don't initiate the normal self-defense state.
    /// </summary>
    [Test]
    public void SuppressedPenaltyDoesNotInitiateSelfDefense()
    {
        var plugIn = new SelfDefensePlugIn();

        plugIn.AttackableGotHit(this._defender, this._attacker, new HitInfo(100, 0, DamageAttributes.Undefined));

        Assert.That(this._gameContext.SelfDefenseState, Is.Empty);
    }

    [Guid("B8C4EB3C-E482-43BE-949A-4E700F372B34")]
    private sealed class SuppressPvpPenaltyPlugIn : IPvpPenaltyPolicyPlugIn
    {
        /// <inheritdoc />
        public void EvaluatePvpPenalty(Player attacker, Player defender, IPvpPenaltyPolicyPlugIn.PvpPenaltyArguments arguments)
        {
            arguments.IsPenaltySuppressed = true;
        }
    }
}
