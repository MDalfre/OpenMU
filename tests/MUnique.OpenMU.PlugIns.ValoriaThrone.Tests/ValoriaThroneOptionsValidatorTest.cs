// <copyright file="ValoriaThroneOptionsValidatorTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.PlugIns.ValoriaThrone.Tests;

using MUnique.OpenMU.PlugIns.ValoriaThrone.Configuration;

/// <summary>
/// Tests for <see cref="ValoriaThroneOptionsValidator"/>.
/// </summary>
[TestFixture]
public class ValoriaThroneOptionsValidatorTest
{
    /// <summary>
    /// Tests that the disabled default configuration is accepted.
    /// </summary>
    [Test]
    public void ValidateDisabledDefaultConfiguration()
    {
        Assert.DoesNotThrow(() => ValoriaThroneOptionsValidator.Validate(ValoriaThroneOptions.Default));
    }

    /// <summary>
    /// Tests that an enabled configuration requires an event server.
    /// </summary>
    [Test]
    public void ValidateEnabledConfigurationWithoutEventServerThrows()
    {
        var options = new ValoriaThroneOptions
        {
            Enabled = true,
            GuardianMonsterId = 1,
        };

        Assert.Throws<InvalidOperationException>(() => ValoriaThroneOptionsValidator.Validate(options));
    }

    /// <summary>
    /// Tests that an enabled configuration requires a guardian definition.
    /// </summary>
    [Test]
    public void ValidateEnabledConfigurationWithoutGuardianThrows()
    {
        var options = new ValoriaThroneOptions
        {
            Enabled = true,
            EventServerId = 1,
        };

        Assert.Throws<InvalidOperationException>(() => ValoriaThroneOptionsValidator.Validate(options));
    }

    /// <summary>
    /// Tests that a valid enabled configuration is accepted.
    /// </summary>
    [Test]
    public void ValidateEnabledConfiguration()
    {
        var options = new ValoriaThroneOptions
        {
            Enabled = true,
            EventServerId = 1,
            GuardianMonsterId = 1,
        };

        Assert.DoesNotThrow(() => ValoriaThroneOptionsValidator.Validate(options));
    }
}
