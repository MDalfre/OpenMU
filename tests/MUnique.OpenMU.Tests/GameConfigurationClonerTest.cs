// <copyright file="GameConfigurationClonerTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using System.Collections.Specialized;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.Persistence.InMemory;

/// <summary>
/// Tests the <see cref="GameConfigurationCloner"/>.
/// </summary>
[TestFixture]
public class GameConfigurationClonerTest
{
    /// <summary>
    /// Verifies that clearing an observed collection emits a valid reset event.
    /// </summary>
    [Test]
    public void ObservedCollectionCanBeCleared()
    {
        var rawCollection = new List<string> { "entry" };
        var adapter = new CollectionAdapter<object, string>(rawCollection);
        NotifyCollectionChangedEventArgs? eventArgs = null;
        adapter.CollectionChanged += (_, args) => eventArgs = args;

        adapter.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(rawCollection, Is.Empty);
            Assert.That(eventArgs?.Action, Is.EqualTo(NotifyCollectionChangedAction.Reset));
        });
    }

    /// <summary>
    /// Verifies that cloned entities have new identities and references only point into the cloned graph.
    /// </summary>
    [Test]
    public void CloneCreatesIndependentGraph()
    {
        var context = new InMemoryContext(new InMemoryRepositoryProvider());
        var source = context.CreateNew<GameConfiguration>();
        var map = context.CreateNew<GameMapDefinition>();
        map.Name = "Lorencia";
        map.TerrainData = new byte[] { 1, 2, 3 };
        map.SafezoneMap = map;
        source.Maps.Add(map);

        var dropGroup = context.CreateNew<DropItemGroup>();
        dropGroup.Description = "Jewels";
        source.DropItemGroups.Add(dropGroup);
        map.DropItemGroups.Add(dropGroup);

        var itemDefinition = context.CreateNew<ItemDefinition>();
        itemDefinition.Name = "Leather Helm";
        source.Items.Add(itemDefinition);
        var itemSet = context.CreateNew<ItemSetGroup>();
        itemSet.Name = "Warrior";
        source.ItemSetGroups.Add(itemSet);
        var itemOfSet = context.CreateNew<ItemOfItemSet>();
        itemOfSet.ItemDefinition = itemDefinition;
        itemOfSet.ItemSetGroup = itemSet;
        itemSet.Items.Add(itemOfSet);

        var clone = GameConfigurationCloner.Clone(source, context);
        var clonedMap = clone.Maps.Single();
        var clonedDropGroup = clone.DropItemGroups.Single();

        Assert.Multiple(() =>
        {
            Assert.That(((IIdentifiable)clone).Id, Is.Not.EqualTo(((IIdentifiable)source).Id));
            Assert.That(((IIdentifiable)clonedMap).Id, Is.Not.EqualTo(((IIdentifiable)map).Id));
            Assert.That(((IIdentifiable)clonedDropGroup).Id, Is.Not.EqualTo(((IIdentifiable)dropGroup).Id));
            Assert.That(clonedMap, Is.Not.SameAs(map));
            Assert.That(clonedMap.SafezoneMap, Is.SameAs(clonedMap));
            Assert.That(clonedMap.DropItemGroups.Single(), Is.SameAs(clonedDropGroup));
            Assert.That(clonedMap.TerrainData, Is.Not.SameAs(map.TerrainData));
            Assert.That(clonedMap.TerrainData, Is.EqualTo(map.TerrainData));
            Assert.That(clone.ItemSetGroups.Single().Items.Single().Name, Is.EqualTo("Warrior Leather Helm"));
            Assert.That(clone.ItemSetGroups.Single().Items.Single().ItemDefinition, Is.SameAs(clone.Items.Single()));
        });

        clonedMap.Name = "Gold Lorencia";
        clonedMap.TerrainData![0] = 9;
        Assert.Multiple(() =>
        {
            Assert.That(map.Name.ToString(), Is.EqualTo("Lorencia"));
            Assert.That(map.TerrainData![0], Is.EqualTo(1));
        });
    }
}
