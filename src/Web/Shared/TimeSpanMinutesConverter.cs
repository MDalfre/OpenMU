// <copyright file="TimeSpanMinutesConverter.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Shared;

using System.Globalization;

/// <summary>
/// Converts form values between a <see cref="TimeSpan"/> and minutes.
/// </summary>
public static class TimeSpanMinutesConverter
{
    /// <summary>
    /// Formats a duration as minutes for an HTML number input.
    /// </summary>
    /// <param name="value">The duration.</param>
    /// <returns>The duration in minutes, using an invariant decimal separator.</returns>
    public static string Format(TimeSpan value)
    {
        return value.TotalMinutes.ToString("0.################", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Parses a non-negative number of minutes.
    /// </summary>
    /// <param name="value">The value entered in the form.</param>
    /// <param name="result">The parsed duration.</param>
    /// <returns><see langword="true"/> when the value is valid.</returns>
    public static bool TryParse(string? value, out TimeSpan result)
    {
        result = default;
        if (!TryParseMinutes(value, out var minutes) || !double.IsFinite(minutes) || minutes < 0)
        {
            return false;
        }

        try
        {
            result = TimeSpan.FromMinutes(minutes);
            return true;
        }
        catch (OverflowException)
        {
            return false;
        }
    }

    private static bool TryParseMinutes(string? value, out double minutes)
    {
        const NumberStyles styles = NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite;
        if (double.TryParse(value, styles, CultureInfo.InvariantCulture, out minutes)
            || double.TryParse(value, styles, CultureInfo.CurrentCulture, out minutes))
        {
            return true;
        }

        return value is not null
               && !value.Contains('.', StringComparison.Ordinal)
               && double.TryParse(value.Replace(',', '.'), styles, CultureInfo.InvariantCulture, out minutes);
    }
}
