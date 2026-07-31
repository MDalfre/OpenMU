// <copyright file="ValoriaThroneOptionsTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.PlugIns.ValoriaThrone.Tests;

using System.ComponentModel.DataAnnotations;
using System.Reflection;
using MUnique.OpenMU.DataModel.Composition;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.PlugIns.ValoriaThrone.Configuration;
using MUnique.OpenMU.PlugIns.ValoriaThrone.Domain;
using MUnique.OpenMU.PlugIns.ValoriaThrone.Services;

/// <summary>
/// Tests for <see cref="ValoriaThroneOptions"/>.
/// </summary>
[TestFixture]
public class ValoriaThroneOptionsTest
{
    /// <summary>
    /// Ensures that support monsters can be created and edited inline by the administration panel.
    /// </summary>
    [Test]
    public void SupportMonstersAreEditableAggregateMembers()
    {
        var property = typeof(ValoriaThroneOptions).GetProperty(nameof(ValoriaThroneOptions.SupportMonsters))
                       ?? throw new InvalidOperationException("SupportMonsters property was not found.");

        Assert.That(property.GetCustomAttribute<MemberOfAggregateAttribute>(), Is.Not.Null);
        Assert.That(property.GetCustomAttribute<ScaffoldColumnAttribute>()?.Scaffold, Is.True);
    }

    /// <summary>
    /// Ensures that the fallback crown representation is configured by default.
    /// </summary>
    [Test]
    public void DefaultCrownRepresentationIsConfigured()
    {
        var options = ValoriaThroneOptions.Default;

        Assert.That(options.CrownItemGroup, Is.EqualTo(14));
        Assert.That(options.CrownItemNumber, Is.EqualTo(13));
        Assert.That(options.CrownVisualItemLevel, Is.EqualTo(15));
        Assert.That(options.CrownCarrierMarkerId, Is.EqualTo(1));
        Assert.That(options.CrownDeliveryDuration, Is.GreaterThan(TimeSpan.Zero));
        Assert.That(options.CrownRespawnDelay, Is.GreaterThanOrEqualTo(TimeSpan.Zero));
        Assert.That(options.SeniorNpcId, Is.EqualTo(223));
        Assert.That(options.LandsOfTrials.GatekeeperNpcId, Is.EqualTo(220));
        Assert.That(options.CoronationConfirmationDuration, Is.GreaterThan(TimeSpan.Zero));
        Assert.That(options.CoronationDuration, Is.GreaterThan(TimeSpan.Zero));
        Assert.That(options.CoronationRadius, Is.GreaterThanOrEqualTo(0));
        Assert.That(options.ImperialEras.DefaultEra, Is.EqualTo(ImperialEra.Ascension));
        Assert.That(options.ImperialEras.SelectionDuration, Is.GreaterThan(TimeSpan.Zero));
        Assert.That(options.ImperialEras.AscensionExperienceMultiplier, Is.EqualTo(1.1f));
        Assert.That(options.ImperialEras.FortuneDropMultiplier, Is.EqualTo(1.1f));
        Assert.That(options.ImperialEras.MaximumChaosMachineSuccessRate, Is.EqualTo(100.0));
        Assert.That(options.ImperialEras.MaximumJewelSuccessRate, Is.EqualTo(100.0));
        Assert.That(options.ImperialEras.AffectedJewels, Is.EquivalentTo(new[] { ValoriaJewelKind.Soul, ValoriaJewelKind.Life, ValoriaJewelKind.Harmony }));
    }

    /// <summary>
    /// Ensures the emperor status uses a client id independent from the Castle Siege crown and GM statuses.
    /// </summary>
    [Test]
    public void ValoriaEmperorStatusUsesDedicatedClientEffectId()
    {
        Assert.That(ValoriaThroneEventController.ValoriaEmperorStatusId, Is.EqualTo(173));
        Assert.That(ValoriaThroneEventController.ValoriaEmperorStatusId, Is.Not.EqualTo(20));
        Assert.That(ValoriaThroneEventController.ValoriaEmperorStatusId, Is.Not.EqualTo(28));
    }

    /// <summary>
    /// Ensures the Valoria status effects can be sent to the client without a null power-up collection.
    /// </summary>
    /// <param name="fieldName">The status definition field name.</param>
    [TestCase("CrownCarrierStatusDefinition")]
    [TestCase("ValoriaEmperorStatusDefinition")]
    public void ValoriaStatusEffectHasAnEmptyPowerUpCollection(string fieldName)
    {
        var field = typeof(ValoriaThroneEventController).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static)
                    ?? throw new InvalidOperationException($"{fieldName} was not found.");
        var definition = field.GetValue(null) as MagicEffectDefinition
                         ?? throw new InvalidOperationException($"{fieldName} contains no magic effect definition.");

        Assert.That(definition.PowerUpDefinitions, Is.Empty);
    }
}
