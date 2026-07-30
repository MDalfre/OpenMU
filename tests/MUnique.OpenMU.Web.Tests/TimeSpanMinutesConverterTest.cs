// <copyright file="TimeSpanMinutesConverterTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Tests;

using MUnique.OpenMU.Web.Shared;

/// <summary>
/// Tests the minutes mapping used by time span form fields.
/// </summary>
public class TimeSpanMinutesConverterTest
{
    /// <summary>
    /// Tests known duration conversions.
    /// </summary>
    /// <param name="seconds">The duration in seconds.</param>
    /// <param name="expectedMinutes">The expected form value.</param>
    [TestCase(30, "0.5")]
    [TestCase(60, "1")]
    [TestCase(90, "1.5")]
    [TestCase(300, "5")]
    public void FormatUsesMinutes(int seconds, string expectedMinutes)
    {
        Assert.That(TimeSpanMinutesConverter.Format(TimeSpan.FromSeconds(seconds)), Is.EqualTo(expectedMinutes));
    }

    /// <summary>
    /// Tests that loading and saving an unchanged value keeps its duration.
    /// </summary>
    [Test]
    public void FormatAndParseRoundTripsWithoutChangingTheDuration()
    {
        var original = TimeSpan.FromSeconds(90);

        var parsed = TimeSpanMinutesConverter.TryParse(TimeSpanMinutesConverter.Format(original), out var reloaded);

        Assert.Multiple(() =>
        {
            Assert.That(parsed, Is.True);
            Assert.That(reloaded, Is.EqualTo(original));
        });
    }

    /// <summary>
    /// Tests decimal comma input and negative validation.
    /// </summary>
    [Test]
    public void ParseAcceptsDecimalCommaAndRejectsNegativeValues()
    {
        Assert.Multiple(() =>
        {
            Assert.That(TimeSpanMinutesConverter.TryParse("0,5", out var parsed), Is.True);
            Assert.That(parsed, Is.EqualTo(TimeSpan.FromSeconds(30)));
            Assert.That(TimeSpanMinutesConverter.TryParse("-0.5", out _), Is.False);
            Assert.That(TimeSpanMinutesConverter.TryParse("NaN", out _), Is.False);
        });
    }
}
