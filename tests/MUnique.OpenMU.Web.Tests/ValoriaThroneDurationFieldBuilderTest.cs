// <copyright file="ValoriaThroneDurationFieldBuilderTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Tests;

using MUnique.OpenMU.PlugIns.ValoriaThrone.Configuration;
using MUnique.OpenMU.Web.Shared.ComponentBuilders;

/// <summary>
/// Tests the explicit Valoria Throne duration whitelist.
/// </summary>
public class ValoriaThroneDurationFieldBuilderTest
{
    private readonly ValoriaThroneDurationFieldBuilder _builder = new();

    /// <summary>
    /// Tests the fields which are shown in minutes.
    /// </summary>
    [TestCase(nameof(ValoriaThroneOptions.AnnouncementDuration))]
    [TestCase(nameof(ValoriaThroneOptions.PreparationDuration))]
    [TestCase(nameof(ValoriaThroneOptions.RegistrationDuration))]
    [TestCase(nameof(ValoriaThroneOptions.BattleDuration))]
    [TestCase(nameof(ValoriaThroneOptions.CrownPhaseDuration))]
    [TestCase(nameof(ValoriaThroneOptions.CoronationConfirmationDuration))]
    [TestCase(nameof(ValoriaThroneOptions.CoronationDuration))]
    [TestCase(nameof(ValoriaThroneOptions.ReignDuration))]
    [TestCase(nameof(ValoriaThroneOptions.CooldownDuration))]
    public void WhitelistedValoriaFieldsUseMinutes(string propertyName)
    {
        var property = typeof(ValoriaThroneOptions).GetProperty(propertyName)!;

        Assert.That(this._builder.CanBuildComponent(property), Is.True);
    }

    /// <summary>
    /// Tests the Valoria durations which must retain the generic millisecond editor.
    /// </summary>
    [TestCase(nameof(ValoriaThroneOptions.CrownDeliveryDuration))]
    [TestCase(nameof(ValoriaThroneOptions.CrownRespawnDelay))]
    public void NonWhitelistedValoriaFieldsRetainGenericEditor(string propertyName)
    {
        var property = typeof(ValoriaThroneOptions).GetProperty(propertyName)!;

        Assert.That(this._builder.CanBuildComponent(property), Is.False);
    }

    /// <summary>
    /// Tests that the nested Era configuration retains the generic editor.
    /// </summary>
    [Test]
    public void ImperialEraDurationRetainsGenericEditor()
    {
        var property = typeof(ValoriaImperialEraOptions).GetProperty(nameof(ValoriaImperialEraOptions.SelectionDuration))!;

        Assert.That(this._builder.CanBuildComponent(property), Is.False);
    }
}
