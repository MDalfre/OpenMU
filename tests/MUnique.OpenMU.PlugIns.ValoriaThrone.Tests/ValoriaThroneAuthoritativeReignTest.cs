// <copyright file="ValoriaThroneAuthoritativeReignTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.PlugIns.ValoriaThrone.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using MUnique.OpenMU.PlugIns.ValoriaThrone.Configuration;
using MUnique.OpenMU.PlugIns.ValoriaThrone.Domain;
using MUnique.OpenMU.PlugIns.ValoriaThrone.Services;

/// <summary>Tests selection of the global Valoria reign across configurations.</summary>
[TestFixture]
public class ValoriaThroneAuthoritativeReignTest
{
    [Test]
    public void EmptyConfigurationDoesNotClearPreviouslyLoadedReign()
    {
        var controller = this.CreateController();
        var reign = CreateReign(DateTimeOffset.UtcNow);

        controller.Configure(new ValoriaThroneOptions { ImperialReign = reign });
        controller.Configure(ValoriaThroneOptions.Default);

        Assert.That(controller.ActiveReign?.Id, Is.EqualTo(reign.Id));
    }

    [Test]
    public void NewerReignWinsRegardlessOfConfigurationLoadOrder()
    {
        var older = CreateReign(DateTimeOffset.UtcNow.AddDays(-1));
        var newer = CreateReign(DateTimeOffset.UtcNow);
        var controller = this.CreateController();

        controller.Configure(new ValoriaThroneOptions { ImperialReign = newer });
        controller.Configure(new ValoriaThroneOptions { ImperialReign = older });

        Assert.That(controller.ActiveReign?.Id, Is.EqualTo(newer.Id));
    }

    private static ImperialReign CreateReign(DateTimeOffset startedAt) => new()
    {
        Id = Guid.NewGuid(),
        EmperorCharacterId = Guid.NewGuid(),
        ImperialGuildId = 123,
        StartedAt = startedAt,
        ExpiresAt = startedAt.AddDays(7),
    };

    private ValoriaThroneEventController CreateController()
    {
        var registry = new ValoriaThroneRuntimeRegistry();
        var mapOperations = new ValoriaThroneMapOperations(registry, NullLogger<ValoriaThroneMapOperations>.Instance);
        return new ValoriaThroneEventController(registry, mapOperations, new ValoriaThroneMessenger(), new InMemoryValoriaThroneStateStore(), TimeProvider.System, NullLogger<ValoriaThroneEventController>.Instance);
    }
}
