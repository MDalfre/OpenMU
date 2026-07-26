// <copyright file="ValoriaThroneScheduler.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.PlugIns.ValoriaThrone.Services;

using MUnique.OpenMU.PlugIns.ValoriaThrone.Configuration;
using MUnique.OpenMU.PlugIns.ValoriaThrone.Domain;

/// <summary>
/// Determines whether the configured local event time is due.
/// </summary>
public sealed class ValoriaThroneScheduler
{
    private readonly TimeProvider _timeProvider;
    private DateOnly? _lastTriggeredDate;
    private ValoriaThroneOptions _options = ValoriaThroneOptions.Default;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValoriaThroneScheduler"/> class.
    /// </summary>
    /// <param name="timeProvider">The clock used by the scheduler.</param>
    public ValoriaThroneScheduler(TimeProvider timeProvider)
    {
        this._timeProvider = timeProvider;
    }

    /// <summary>Configures the scheduler.</summary>
    public void Configure(ValoriaThroneOptions options)
    {
        this._options = options;
        this._lastTriggeredDate = null;
    }

    /// <summary>Determines whether an automatic event should start now.</summary>
    public bool IsDue()
    {
        if (!this._options.Enabled || !this._options.Schedule.Enabled)
        {
            return false;
        }

        var zone = TimeZoneInfo.FindSystemTimeZoneById(this._options.Schedule.TimeZone);
        var localTime = TimeZoneInfo.ConvertTime(this._timeProvider.GetUtcNow(), zone);
        var localDate = DateOnly.FromDateTime(localTime.DateTime);
        if (this._lastTriggeredDate == localDate
            || !this._options.Schedule.DaysOfWeek.Contains(localTime.DayOfWeek)
            || localTime.Hour != this._options.Schedule.StartTime.Hour
            || localTime.Minute != this._options.Schedule.StartTime.Minute)
        {
            return false;
        }

        this._lastTriggeredDate = localDate;
        return true;
    }
}
