// <copyright file="ValoriaThronePlugInTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.PlugIns.ValoriaThrone.Tests;

using MUnique.OpenMU.PlugIns.ValoriaThrone.Configuration;

/// <summary>
/// Tests for <see cref="ValoriaThronePlugIn"/>.
/// </summary>
[TestFixture]
public class ValoriaThronePlugInTest
{
    /// <summary>
    /// Ensures that an incomplete persisted configuration cannot prevent server startup.
    /// </summary>
    [Test]
    public void InvalidPersistedConfigurationDoesNotThrow()
    {
        var plugIn = new ValoriaThronePlugIn();
        var configuration = new ValoriaThroneOptions
        {
            Enabled = true,
            EventServerId = 2,
        };

        Assert.DoesNotThrow(() => plugIn.Configuration = configuration);
        Assert.That(plugIn.Configuration, Is.SameAs(configuration));
    }
}
