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

        var monster = context.CreateNew<MonsterDefinition>();
        monster.Designation = "Elf Soldier";
        source.Monsters.Add(monster);
        var buff = context.CreateNew<Buff>();
        buff.MagicEffectDefinition = context.CreateNew<MagicEffectDefinition>();
        monster.Buffs.Add(buff);

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
            Assert.That(clone.Monsters.Single().Buffs.Single().MagicEffectDefinition, Is.Not.SameAs(buff.MagicEffectDefinition));
            Assert.That(
                clone.Monsters.Single().Buffs.Single().GetType().GetProperty("RawMagicEffectDefinition")?.GetValue(clone.Monsters.Single().Buffs.Single()),
                Is.SameAs(clone.Monsters.Single().Buffs.Single().MagicEffectDefinition));
        });

        clonedMap.Name = "Gold Lorencia";
        clonedMap.TerrainData![0] = 9;
        Assert.Multiple(() =>
        {
            Assert.That(map.Name.ToString(), Is.EqualTo("Lorencia"));
            Assert.That(map.TerrainData![0], Is.EqualTo(1));
        });
    }

    /// <summary>
    /// Verifies that a collection which is persisted through a raw string is copied through its mutable adapter.
    /// </summary>
    [Test]
    public void CloneCopiesStringBackedCollection()
    {
        var sourceContext = new InMemoryContext(new InMemoryRepositoryProvider());
        var source = sourceContext.CreateNew<GameConfiguration>();
        var itemSlotType = sourceContext.CreateNew<ItemSlotType>();
        itemSlotType.ItemSlots.Add(0);
        itemSlotType.ItemSlots.Add(1);
        source.ItemSlotTypes.Add(itemSlotType);

        var targetContext = new StringBackedCollectionContext();
        var clone = GameConfigurationCloner.Clone(source, targetContext);
        var clonedItemSlotType = (StringBackedItemSlotType)clone.ItemSlotTypes.Single();

        Assert.Multiple(() =>
        {
            Assert.That(clonedItemSlotType.ItemSlots, Is.EqualTo(new[] { 0, 1 }));
            Assert.That(clonedItemSlotType.RawItemSlots, Is.EqualTo("0;1"));
        });
    }

    private sealed class StringBackedCollectionContext : InMemoryContext, IContext
    {
        public StringBackedCollectionContext()
            : base(new InMemoryRepositoryProvider())
        {
        }

        object IContext.CreateNew(Type type, params object?[] args)
        {
            return type == typeof(ItemSlotType)
                ? new StringBackedItemSlotType { Id = GuidV7.NewGuid() }
                : base.CreateNew(type, args);
        }
    }

    private sealed class StringBackedItemSlotType : Persistence.BasicModel.ItemSlotType
    {
        private ICollection<int>? _itemSlots;

        public string RawItemSlots { get; set; } = string.Empty;

        public override ICollection<int> ItemSlots => this._itemSlots ??= new CollectionToStringAdapter<int>(this.RawItemSlots, value => this.RawItemSlots = value);
    }
}
