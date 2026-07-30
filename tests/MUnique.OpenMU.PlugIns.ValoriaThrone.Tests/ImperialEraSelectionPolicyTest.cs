// <copyright file="ImperialEraSelectionPolicyTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.PlugIns.ValoriaThrone.Tests;

using MUnique.OpenMU.PlugIns.ValoriaThrone.Domain;

/// <summary>
/// Tests authoritative Imperial Era selection eligibility.
/// </summary>
[TestFixture]
public class ImperialEraSelectionPolicyTest
{
    private static readonly DateTimeOffset Now = new(2026, 7, 30, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid EmperorId = Guid.Parse("EFCA7457-8ECA-49D1-8A9A-A516B277B085");

    /// <summary>
    /// Tests all supported Era values.
    /// </summary>
    [TestCase(ImperialEra.Ascension)]
    [TestCase(ImperialEra.Fortune)]
    [TestCase(ImperialEra.Freedom)]
    [TestCase(ImperialEra.Luck)]
    public void EmperorCanSelectEachSupportedEra(ImperialEra era)
    {
        Assert.That(CanSelect(CreateReign(), EmperorId, era), Is.True);
    }

    /// <summary>
    /// Tests invalid callers, values and reign states.
    /// </summary>
    [Test]
    public void RejectsInvalidOrIneligibleSelections()
    {
        var selectedReign = CreateReign();
        selectedReign.SelectedEra = ImperialEra.Fortune;

        Assert.Multiple(() =>
        {
            Assert.That(CanSelect(null, EmperorId, ImperialEra.Ascension), Is.False);
            Assert.That(CanSelect(CreateReign(), Guid.NewGuid(), ImperialEra.Ascension), Is.False);
            Assert.That(CanSelect(CreateReign(), EmperorId, ImperialEra.None), Is.False);
            Assert.That(CanSelect(CreateReign(), EmperorId, (ImperialEra)byte.MaxValue), Is.False);
            Assert.That(CanSelect(selectedReign, EmperorId, ImperialEra.Luck), Is.False);
            Assert.That(CanSelect(CreateReign(Now.AddMinutes(-31)), EmperorId, ImperialEra.Luck), Is.False);
            Assert.That(CanSelect(CreateReign(Now.AddMinutes(1)), EmperorId, ImperialEra.Luck), Is.False);
            Assert.That(CanSelect(CreateReign(expiresAt: Now), EmperorId, ImperialEra.Luck), Is.False);
            Assert.That(CanSelect(CreateReign(), EmperorId, ImperialEra.Luck, ValoriaThroneEventState.GuardianBattle), Is.False);
        });
    }

    private static bool CanSelect(
        ImperialReign? reign,
        Guid characterId,
        ImperialEra era,
        ValoriaThroneEventState state = ValoriaThroneEventState.Cooldown)
    {
        return ImperialEraSelectionPolicy.CanSelect(reign, characterId, era, state, Now, TimeSpan.FromMinutes(30));
    }

    private static ImperialReign CreateReign(DateTimeOffset? startedAt = null, DateTimeOffset? expiresAt = null) => new()
    {
        EmperorCharacterId = EmperorId,
        StartedAt = startedAt ?? Now.AddMinutes(-5),
        ExpiresAt = expiresAt ?? Now.AddDays(7),
    };
}
