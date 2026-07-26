// <copyright file="MapEntryValidationEventArgs.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using MUnique.OpenMU.GameLogic.PlayerActions;

/// <summary>
/// Contains the result of a map entry validation.
/// </summary>
public sealed class MapEntryValidationEventArgs : EventArgs
{
    /// <summary>Gets or sets a value indicating whether the entry is denied.</summary>
    public bool Denied { get; set; }

    /// <summary>Gets or sets the message shown for a denied entry.</summary>
    public string? Message { get; set; }

    /// <summary>Gets or sets the gate used when a denied login must be redirected.</summary>
    public ExitGate? RedirectGate { get; set; }
}
