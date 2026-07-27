// <copyright file="ValoriaThroneSuccessRateModifierTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.PlugIns.ValoriaThrone.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using MUnique.OpenMU.DataModel.Configuration.ItemCrafting;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.PlayerActions.Items;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.PlugIns.ValoriaThrone.Configuration;
using MUnique.OpenMU.PlugIns.ValoriaThrone.Domain;
using MUnique.OpenMU.PlugIns.ValoriaThrone.Services;

/// <summary>
/// Tests the Luck era success rate modifiers.
/// </summary>
[TestFixture]
public class ValoriaThroneSuccessRateModifierTest
{
    /// <summary>
    /// Ensures that regular Chaos Machine combinations use the configured multiplier and cap.
    /// </summary>
    [Test]
    public void LuckEraModifiesRegularChaosCombination()
    {
        var plugIn = CreatePlugIn(1.5f, 80.0, 1.1f, 100.0);
        var arguments = new IChaosSuccessRateModifierPlugIn.ChaosSuccessRateArguments { EffectiveRate = 60.0 };
        var handler = new SimpleItemCraftingHandler(new SimpleCraftingSettings());

        plugIn.ModifyChaosSuccessRate(null!, handler, arguments);

        Assert.That(arguments.EffectiveRate, Is.EqualTo(80.0));
        Assert.That(arguments.Modifiers, Has.Count.EqualTo(1));
    }

    /// <summary>
    /// Ensures that Soul, Life and Harmony use the configured multiplier and cap.
    /// </summary>
    [TestCase(14, 14)]
    [TestCase(16, 14)]
    [TestCase(42, 14)]
    public void LuckEraModifiesConfiguredJewel(short number, byte group)
    {
        var plugIn = CreatePlugIn(1.1f, 100.0, 1.5f, 80.0);
        var arguments = new IJewelSuccessRateModifierPlugIn.JewelSuccessRateArguments { EffectiveChance = 0.6 };
        var jewel = new TemporaryItem { Definition = new ItemDefinition { Number = number, Group = group } };

        plugIn.ModifyJewelSuccessRate(null!, jewel, new TemporaryItem(), arguments);

        Assert.That(arguments.EffectiveChance, Is.EqualTo(0.8));
    }

    /// <summary>
    /// Ensures that jewels outside the configured list remain unchanged.
    /// </summary>
    [Test]
    public void LuckEraDoesNotModifyBless()
    {
        var plugIn = CreatePlugIn(1.1f, 100.0, 1.5f, 80.0);
        var arguments = new IJewelSuccessRateModifierPlugIn.JewelSuccessRateArguments { EffectiveChance = 0.6 };
        var jewel = new TemporaryItem { Definition = new ItemDefinition { Number = 13, Group = 14 } };

        plugIn.ModifyJewelSuccessRate(null!, jewel, new TemporaryItem(), arguments);

        Assert.That(arguments.EffectiveChance, Is.EqualTo(0.6));
    }

    private static ValoriaThronePlugIn CreatePlugIn(float chaosMultiplier, double chaosMaximum, float jewelMultiplier, double jewelMaximum)
    {
        var registry = new ValoriaThroneRuntimeRegistry();
        var mapOperations = new ValoriaThroneMapOperations(registry, NullLogger<ValoriaThroneMapOperations>.Instance);
        var controller = new ValoriaThroneEventController(
            registry,
            mapOperations,
            new ValoriaThroneMessenger(),
            new InMemoryValoriaThroneStateStore(),
            TimeProvider.System,
            NullLogger<ValoriaThroneEventController>.Instance);
        var plugIn = new ValoriaThronePlugIn(
            controller,
            mapOperations,
            new ValoriaThroneAdmissionPolicy(controller),
            new ValoriaThroneScheduler(TimeProvider.System));
        plugIn.Configuration = new ValoriaThroneOptions
        {
            ImperialReign = new ImperialReign
            {
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
                SelectedEra = ImperialEra.Luck,
            },
            ImperialEras = new ValoriaImperialEraOptions
            {
                LuckChaosMachineMultiplier = chaosMultiplier,
                MaximumChaosMachineSuccessRate = chaosMaximum,
                LuckJewelMultiplier = jewelMultiplier,
                MaximumJewelSuccessRate = jewelMaximum,
            },
        };
        return plugIn;
    }
}
