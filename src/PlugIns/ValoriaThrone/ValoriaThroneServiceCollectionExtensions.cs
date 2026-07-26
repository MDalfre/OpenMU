// <copyright file="ValoriaThroneServiceCollectionExtensions.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace Microsoft.Extensions.DependencyInjection;

using MUnique.OpenMU.PlugIns.ValoriaThrone.Services;

/// <summary>
/// Registers Valoria Throne services.
/// </summary>
public static class ValoriaThroneServiceCollectionExtensions
{
    /// <summary>
    /// Adds Valoria Throne services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddValoriaThrone(this IServiceCollection services)
    {
        services.AddSingleton<ValoriaThroneRuntimeRegistry>()
            .AddSingleton<IValoriaThroneStateStore, InMemoryValoriaThroneStateStore>()
            .AddSingleton<IValoriaThroneMessenger, ValoriaThroneMessenger>()
            .AddSingleton<IValoriaThroneMapOperations, ValoriaThroneMapOperations>()
            .AddSingleton<IValoriaThroneEventController, ValoriaThroneEventController>()
            .AddSingleton<ValoriaThroneAdmissionPolicy>()
            .AddSingleton<ValoriaThroneScheduler>()
            .AddSingleton(TimeProvider.System);
        return services;
    }
}
