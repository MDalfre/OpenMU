// <copyright file="ValoriaThronePlugInTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.PlugIns.ValoriaThrone.Tests;

using MUnique.OpenMU.PlugIns.ValoriaThrone.Configuration;
using MUnique.OpenMU.PlugIns.ValoriaThrone.Domain;

/// <summary>
/// Tests for <see cref="ValoriaThronePlugIn"/>.
/// </summary>
[TestFixture]
public class ValoriaThronePlugInTest
{
    /// <summary>
    /// Ensures that only the competitive Valoria phases suppress PvP penalties.
    /// </summary>
    /// <param name="state">The event state.</param>
    /// <param name="expected">Whether PvP penalties should be suppressed.</param>
    [TestCase(ValoriaThroneEventState.Disabled, false)]
    [TestCase(ValoriaThroneEventState.Idle, false)]
    [TestCase(ValoriaThroneEventState.Announcing, false)]
    [TestCase(ValoriaThroneEventState.Preparing, false)]
    [TestCase(ValoriaThroneEventState.RegistrationOpen, false)]
    [TestCase(ValoriaThroneEventState.GuardianBattle, true)]
    [TestCase(ValoriaThroneEventState.CrownOnGround, true)]
    [TestCase(ValoriaThroneEventState.CrownCarried, true)]
    [TestCase(ValoriaThroneEventState.CoronationInProgress, true)]
    [TestCase(ValoriaThroneEventState.Finishing, false)]
    [TestCase(ValoriaThroneEventState.Cooldown, false)]
    [TestCase(ValoriaThroneEventState.Faulted, false)]
    public void PvpPenaltySuppressionMatchesCompetitivePhases(ValoriaThroneEventState state, bool expected)
    {
        Assert.That(ValoriaThronePlugIn.IsPenaltyFreePvpState(state), Is.EqualTo(expected));
    }

    /// <summary>
    /// Ensures that an incomplete persisted configuration cannot prevent server startup.
    /// </summary>
    [Test]
    public void InvalidPersistedConfigurationDoesNotThrow()
    {
        var plugIn = new ValoriaThronePlugIn();
        var configuration = new ValoriaThroneOptions
        {
            Enabled = true,
            EventServerId = 2,
        };

        Assert.DoesNotThrow(() => plugIn.Configuration = configuration);
        Assert.That(plugIn.Configuration, Is.SameAs(configuration));
    }
}
