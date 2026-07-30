// <copyright file="ValoriaEraDialogProtocol.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.PlugIns.ValoriaThrone.Domain;

/// <summary>
/// Identifies Valoria Era actions carried by the existing Season 6 server-command packet.
/// </summary>
public static class ValoriaEraDialogProtocol
{
    /// <summary>The command category reserved for the Valoria Era dialog.</summary>
    public const byte CommandCategory = 250;

    /// <summary>Opens the Era selection dialog.</summary>
    public const byte Open = 1;
}
