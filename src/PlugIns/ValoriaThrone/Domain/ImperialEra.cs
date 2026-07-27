// <copyright file="ImperialEra.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.PlugIns.ValoriaThrone.Domain;

/// <summary>Identifies the benefit selected by the current emperor.</summary>
public enum ImperialEra
{
    /// <summary>No era has been selected.</summary>
    None,

    /// <summary>Increases global experience.</summary>
    Ascension,

    /// <summary>Increases common item drops.</summary>
    Fortune,

    /// <summary>Removes normal PK warp restrictions.</summary>
    Freedom,

    /// <summary>Increases Chaos Machine and jewel success chances.</summary>
    Luck,
}
