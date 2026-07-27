// <copyright file="ImperialEraPresentationTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.PlugIns.ValoriaThrone.Tests;

using MUnique.OpenMU.PlugIns.ValoriaThrone.Configuration;
using MUnique.OpenMU.PlugIns.ValoriaThrone.Domain;

/// <summary>Tests centralized imperial era presentation.</summary>
[TestFixture]
public class ImperialEraPresentationTest
{
    /// <summary>Tests that configured relative bonuses are shown to players.</summary>
    [Test]
    public void DescriptionUsesConfiguredMultiplier()
    {
        var options = new ValoriaImperialEraOptions { AscensionExperienceMultiplier = 1.25f };

        Assert.That(ImperialEraPresentation.GetDescription(ImperialEra.Ascension, options), Does.Contain("25%"));
    }
}
