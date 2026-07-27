// <copyright file="ValoriaThroneOptionsTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.PlugIns.ValoriaThrone.Tests;

using System.ComponentModel.DataAnnotations;
using System.Reflection;
using MUnique.OpenMU.DataModel.Composition;
using MUnique.OpenMU.PlugIns.ValoriaThrone.Configuration;

/// <summary>
/// Tests for <see cref="ValoriaThroneOptions"/>.
/// </summary>
[TestFixture]
public class ValoriaThroneOptionsTest
{
    /// <summary>
    /// Ensures that support monsters can be created and edited inline by the administration panel.
    /// </summary>
    [Test]
    public void SupportMonstersAreEditableAggregateMembers()
    {
        var property = typeof(ValoriaThroneOptions).GetProperty(nameof(ValoriaThroneOptions.SupportMonsters))
                       ?? throw new InvalidOperationException("SupportMonsters property was not found.");

        Assert.That(property.GetCustomAttribute<MemberOfAggregateAttribute>(), Is.Not.Null);
        Assert.That(property.GetCustomAttribute<ScaffoldColumnAttribute>()?.Scaffold, Is.True);
    }
}
