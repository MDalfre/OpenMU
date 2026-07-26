// <copyright file="ValoriaThroneOptionsValidator.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.PlugIns.ValoriaThrone.Configuration;

/// <summary>
/// Validates Valoria Throne options before they are used.
/// </summary>
public static class ValoriaThroneOptionsValidator
{
    /// <summary>
    /// Validates the specified options.
    /// </summary>
    /// <param name="options">The options to validate.</param>
    public static void Validate(ValoriaThroneOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!options.Enabled)
        {
            return;
        }

        if (options.EventServerId is null)
        {
            throw new InvalidOperationException($"{nameof(ValoriaThroneOptions.EventServerId)} is required when the event is enabled.");
        }

        if (options.GuardianMonsterId == 0)
        {
            throw new InvalidOperationException($"{nameof(ValoriaThroneOptions.GuardianMonsterId)} is required when the event is enabled.");
        }

        if (options.AnnouncementDuration < TimeSpan.Zero
            || options.PreparationDuration < TimeSpan.Zero
            || options.RegistrationDuration < TimeSpan.Zero
            || options.BattleDuration <= TimeSpan.Zero
            || options.CrownPhaseDuration < TimeSpan.Zero
            || options.CooldownDuration < TimeSpan.Zero)
        {
            throw new InvalidOperationException("Valoria Throne durations must be non-negative and the battle duration must be positive.");
        }

        if (options.Schedule.Enabled)
        {
            if (options.Schedule.DaysOfWeek.Count == 0)
            {
                throw new InvalidOperationException("At least one schedule day is required.");
            }

            _ = TimeZoneInfo.FindSystemTimeZoneById(options.Schedule.TimeZone);
        }
    }
}
