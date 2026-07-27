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

        if (options.SeniorNpcId <= 0)
        {
            throw new InvalidOperationException($"{nameof(ValoriaThroneOptions.SeniorNpcId)} is required when the event is enabled.");
        }

        if (options.CrownVisualItemLevel > 15)
        {
            throw new InvalidOperationException($"{nameof(ValoriaThroneOptions.CrownVisualItemLevel)} cannot exceed 15.");
        }

        foreach (var supportMonster in options.SupportMonsters)
        {
            if (supportMonster.MonsterId <= 0)
            {
                throw new InvalidOperationException($"{nameof(ValoriaThroneSupportMonsterOptions.MonsterId)} is required for every support monster.");
            }
        }

        if (options.AnnouncementDuration < TimeSpan.Zero
            || options.PreparationDuration < TimeSpan.Zero
            || options.RegistrationDuration < TimeSpan.Zero
            || options.BattleDuration <= TimeSpan.Zero
            || options.CrownPhaseDuration < TimeSpan.Zero
            || options.CrownDeliveryDuration <= TimeSpan.Zero
            || options.CrownRespawnDelay < TimeSpan.Zero
            || options.CoronationConfirmationDuration <= TimeSpan.Zero
            || options.CoronationDuration <= TimeSpan.Zero
            || options.ReignDuration <= TimeSpan.Zero
            || options.CooldownDuration < TimeSpan.Zero)
        {
            throw new InvalidOperationException("Valoria Throne durations must be non-negative and the battle duration must be positive.");
        }

        if (options.CoronationRadius == 0)
        {
            throw new InvalidOperationException($"{nameof(ValoriaThroneOptions.CoronationRadius)} must be positive.");
        }

        if (options.LandsOfTrials is null)
        {
            throw new InvalidOperationException("LandsOfTrials configuration is required.");
        }

        if (options.LandsOfTrials.Enabled && options.LandsOfTrials.GatekeeperNpcId <= 0)
        {
            throw new InvalidOperationException("LandsOfTrials GatekeeperNpcId is required when access is enabled.");
        }

        if (options.ImperialEras is null
            || options.ImperialEras.SelectionDuration <= TimeSpan.Zero
            || options.ImperialEras.DefaultEra == Domain.ImperialEra.None
            || options.ImperialEras.AscensionExperienceMultiplier <= 0
            || options.ImperialEras.FortuneDropMultiplier <= 0
            || options.ImperialEras.LuckChaosMachineMultiplier <= 0
            || options.ImperialEras.MaximumChaosMachineSuccessRate is <= 0 or > 100
            || options.ImperialEras.LuckJewelMultiplier <= 0
            || options.ImperialEras.MaximumJewelSuccessRate is <= 0 or > 100)
        {
            throw new InvalidOperationException("ImperialEras configuration contains invalid values.");
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
