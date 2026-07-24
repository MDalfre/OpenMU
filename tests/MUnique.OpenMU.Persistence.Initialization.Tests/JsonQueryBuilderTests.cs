// <copyright file="JsonQueryBuilderTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Tests;

using System.Diagnostics;
using System.IO;
using Microsoft.EntityFrameworkCore;
using MUnique.OpenMU.Persistence.EntityFramework;
using MUnique.OpenMU.Persistence.EntityFramework.Json;
using Account = MUnique.OpenMU.Persistence.EntityFramework.Model.Account;
using AttributeDefinition = MUnique.OpenMU.Persistence.EntityFramework.Model.AttributeDefinition;
using GameConfiguration = MUnique.OpenMU.Persistence.EntityFramework.Model.GameConfiguration;
using ItemOptionType = MUnique.OpenMU.Persistence.EntityFramework.Model.ItemOptionType;

/// <summary>
/// Tests for the <see cref="JsonQueryBuilder"/>.
/// </summary>
[TestFixture]
internal class JsonQueryBuilderTests
{
    /// <summary>
    /// Sets up this instance.
    /// </summary>
    [OneTimeSetUp]
    public void Setup()
    {
        ConnectionConfigurator.Initialize(new ConfigFileDatabaseConnectionStringProvider());
    }

    /// <summary>
    /// Tests the json query builder for the <see cref="GameConfiguration"/> type.
    /// </summary>
    [Test]
    public void JsonQueryBuilderGameConfiguration()
    {
        using var installationContext = new ConfigurationContext();
        var type = installationContext.Model.GetEntityTypes().FirstOrDefault(t => t.ClrType == typeof(GameConfiguration));
        string result;
        Stopwatch stopwatch = new();
        stopwatch.Start();
        try
        {
            var builder = new GameConfigurationJsonQueryBuilder();
            result = builder.BuildJsonQueryForEntity(type!);
        }
        finally
        {
            stopwatch.Stop();
        }

        result = $"-- Json query created in {stopwatch.ElapsedMilliseconds} ms:{Environment.NewLine}" + result;
        //// File.WriteAllText(@"C:\temp\json_GameConfiguration.txt", result);
    }

    /// <summary>
    /// Ensures that shared configuration references are embedded so that their descriptive fields are available.
    /// </summary>
    [Test]
    public void GameConfigurationJsonQueryBuilderEmbedsSharedReferences()
    {
        using var installationContext = new ConfigurationContext();
        var builder = new TestGameConfigurationJsonQueryBuilder();
        var attributeNavigation = installationContext.Model.GetEntityTypes()
            .SelectMany(entityType => entityType.GetNavigations())
            .First(navigation => navigation.TargetEntityType.ClrType == typeof(AttributeDefinition));
        var optionTypeNavigation = installationContext.Model.GetEntityTypes()
            .SelectMany(entityType => entityType.GetNavigations())
            .First(navigation => navigation.TargetEntityType.ClrType == typeof(ItemOptionType));

        Assert.Multiple(() =>
        {
            Assert.That(builder.ShouldEmbed(attributeNavigation), Is.True);
            Assert.That(builder.ShouldEmbed(optionTypeNavigation), Is.True);
        });
    }

    /// <summary>
    /// Tests the json query builder for the <see cref="Account"/> type.
    /// </summary>
    [Test]
    public void JsonQueryBuilderAccount()
    {
        using var installationContext = new ConfigurationContext();
        var type = installationContext.Model.GetEntityTypes().FirstOrDefault(t => t.ClrType == typeof(Account));
        string result;
        Stopwatch stopwatch = new();
        stopwatch.Start();
        try
        {
            var builder = new JsonQueryBuilder();
            result = builder.BuildJsonQueryForEntity(type!);
        }
        finally
        {
            stopwatch.Stop();
        }

        result = $"-- Json query created in {stopwatch.ElapsedMilliseconds} ms:{Environment.NewLine}" + result;
        //// File.WriteAllText(@"C:\temp\json_Account.txt", result);
    }

    /// <summary>
    /// Loads the <see cref="GameConfiguration"/> using the <see cref="JsonQueryBuilder"/> and the <see cref="JsonObjectLoader"/>.
    /// It always fails, because it reports the taken time.
    /// </summary>
    [Test]
    [Ignore("It hits the database.")]
    public async Task LoadConfigByJsonAsync()
    {
        await using var installationContext = new ConfigurationContext();
        installationContext.Database.OpenConnection();
        var builder = new GameConfigurationJsonObjectLoader();
        IEnumerable<GameConfiguration> result;
        Stopwatch stopwatch = new();
        stopwatch.Start();
        try
        {
            result = await builder.LoadAllObjectsAsync<EntityFramework.Model.GameConfiguration>(installationContext).ConfigureAwait(false);
            result = result.ToList();
        }
        finally
        {
            stopwatch.Stop();
        }

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.Not.EqualTo(0));
        Assert.That(stopwatch.ElapsedMilliseconds, Is.EqualTo(0));
    }

    private sealed class TestGameConfigurationJsonQueryBuilder : GameConfigurationJsonQueryBuilder
    {
        public bool ShouldEmbed(Microsoft.EntityFrameworkCore.Metadata.INavigation navigation)
        {
            return this.ShouldEmbedNavigation(navigation);
        }
    }
}
