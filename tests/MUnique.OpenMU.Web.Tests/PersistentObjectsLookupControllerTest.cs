// <copyright file="PersistentObjectsLookupControllerTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Tests;

using System.Threading;
using Microsoft.Extensions.Logging;
using Moq;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.Web.Shared.Services;
using PersistentGameConfiguration = MUnique.OpenMU.Persistence.BasicModel.GameConfiguration;
using PersistentSkill = MUnique.OpenMU.Persistence.BasicModel.Skill;

/// <summary>
/// Tests the lookup used by optional skill-reference fields such as Monster.AttackSkill.
/// </summary>
public class PersistentObjectsLookupControllerTest
{
    /// <summary>
    /// Ensures that all skills come from the current game configuration and are not copied.
    /// </summary>
    [Test]
    public async Task EmptySearchReturnsCurrentGameConfigurationSkills()
    {
        var currentSkill = new PersistentSkill { Number = 42, Name = "Death Stab" };
        var anotherSkill = new PersistentSkill { Number = 9, Name = "Evil Spirit" };
        var configuration = new PersistentGameConfiguration();
        configuration.Skills.Add(currentSkill);
        configuration.Skills.Add(anotherSkill);
        var controller = CreateController(configuration);

        var result = (await controller.GetSuggestionsAsync<Skill>(null, null)).ToList();

        Assert.That(result, Is.EqualTo(new[] { currentSkill, anotherSkill }));
        Assert.That(result[0], Is.SameAs(currentSkill));
    }

    /// <summary>
    /// Ensures that skill lookup supports both the skill number and its name.
    /// </summary>
    [Test]
    public async Task SearchMatchesSkillNumberAndName()
    {
        var deathStab = new PersistentSkill { Number = 42, Name = "Death Stab" };
        var configuration = new PersistentGameConfiguration();
        configuration.Skills.Add(deathStab);
        configuration.Skills.Add(new PersistentSkill { Number = 9, Name = "Evil Spirit" });
        configuration.Skills.Add(new PersistentSkill { Number = 250, Name = string.Empty });
        var controller = CreateController(configuration);

        var byNumber = await controller.GetSuggestionsAsync<Skill>("42", null);
        var byName = await controller.GetSuggestionsAsync<Skill>("death stab", null);
        var unnamedByNumber = await controller.GetSuggestionsAsync<Skill>("250", null);

        Assert.Multiple(() =>
        {
            Assert.That(byNumber.Single(), Is.SameAs(deathStab));
            Assert.That(byName.Single(), Is.SameAs(deathStab));
            Assert.That(unnamedByNumber.Single().Number, Is.EqualTo(250));
        });
    }

    /// <summary>
    /// Ensures that an empty configuration produces an empty list.
    /// </summary>
    [Test]
    public async Task ConfigurationWithoutSkillsReturnsEmptyList()
    {
        var controller = CreateController(new PersistentGameConfiguration());

        var result = await controller.GetSuggestionsAsync<Skill>(null, null);

        Assert.That(result, Is.Empty);
    }

    private static PersistentObjectsLookupController CreateController(GameConfiguration configuration)
    {
        var dataSource = new Mock<IDataSource<GameConfiguration>>();
        dataSource
            .Setup(source => source.GetOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<GameConfiguration>(configuration));
        return new PersistentObjectsLookupController(
            Mock.Of<IPersistenceContextProvider>(),
            dataSource.Object,
            Mock.Of<ILogger<PersistentObjectsLookupController>>());
    }
}
