// <copyright file="ValoriaThroneSupportMonsterOptions.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.PlugIns.ValoriaThrone.Configuration;

/// <summary>
/// Configures a support monster spawned together with the guardian.
/// </summary>
public sealed class ValoriaThroneSupportMonsterOptions
{
    /// <summary>Gets or sets the monster definition identifier.</summary>
    public short MonsterId { get; set; }

    /// <summary>Gets or sets the spawn x coordinate.</summary>
    public byte SpawnX { get; set; }

    /// <summary>Gets or sets the spawn y coordinate.</summary>
    public byte SpawnY { get; set; }

    /// <summary>Gets or sets the initial direction.</summary>
    public byte Direction { get; set; }
}
