// <copyright file="ValoriaThroneDurationFieldBuilder.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Shared.ComponentBuilders;

using System.Reflection;
using Microsoft.AspNetCore.Components.Rendering;
using MUnique.OpenMU.Web.Shared.Components.Form;
using MUnique.OpenMU.Web.Shared.Services;

/// <summary>
/// Builds minute-based duration fields for the explicitly supported Valoria Throne options.
/// </summary>
public sealed class ValoriaThroneDurationFieldBuilder : BaseComponentBuilder, IComponentBuilder
{
    private const string ValoriaThroneOptionsTypeName = "MUnique.OpenMU.PlugIns.ValoriaThrone.Configuration.ValoriaThroneOptions";

    private static readonly HashSet<string> MinutePropertyNames =
    [
        "AnnouncementDuration",
        "PreparationDuration",
        "RegistrationDuration",
        "BattleDuration",
        "CrownPhaseDuration",
        "CoronationConfirmationDuration",
        "CoronationDuration",
        "ReignDuration",
        "CooldownDuration",
    ];

    /// <inheritdoc />
    public bool CanBuildComponent(PropertyInfo propertyInfo)
    {
        return propertyInfo.PropertyType == typeof(TimeSpan)
               && propertyInfo.DeclaringType?.FullName == ValoriaThroneOptionsTypeName
               && MinutePropertyNames.Contains(propertyInfo.Name);
    }

    /// <inheritdoc />
    public int BuildComponent(object model, PropertyInfo propertyInfo, RenderTreeBuilder builder, int currentIndex, IChangeNotificationService notificationService)
    {
        return this.BuildField<TimeSpan, ValoriaThroneMinutesField>(model, propertyInfo, builder, currentIndex, notificationService);
    }
}
