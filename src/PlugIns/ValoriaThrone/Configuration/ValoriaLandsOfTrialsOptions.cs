// <copyright file="ValoriaLandsOfTrialsOptions.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.PlugIns.ValoriaThrone.Configuration;

/// <summary>
/// Configures access to Lands of Trials during an imperial reign.
/// </summary>
public sealed class ValoriaLandsOfTrialsOptions
{
    /// <summary>Gets or sets a value indicating whether the access policy is enabled.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Gets or sets the Lands of Trials map identifier.</summary>
    public byte MapId { get; set; } = 31;

    /// <summary>Gets or sets the NPC definition which grants access to the map.</summary>
    public short GatekeeperNpcId { get; set; } = 223;

    /// <summary>Gets or sets a value indicating whether imperial guild members may enter.</summary>
    public bool AllowImperialGuild { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether current imperial guild allies may enter.</summary>
    public bool AllowAlliances { get; set; } = true;

    /// <summary>Gets or sets the entry position x coordinate.</summary>
    public byte EntryPositionX { get; set; } = 64;

    /// <summary>Gets or sets the entry position y coordinate.</summary>
    public byte EntryPositionY { get; set; } = 14;
}
