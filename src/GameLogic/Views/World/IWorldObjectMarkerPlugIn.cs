// <copyright file="IWorldObjectMarkerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Views.World;

using MUnique.OpenMU.GameLogic.Views;

/// <summary>
/// A view plugin which toggles a client-side marker on a world object.
/// </summary>
public interface IWorldObjectMarkerPlugIn : IViewPlugIn
{
    /// <summary>
    /// Toggles the marker on the specified object.
    /// </summary>
    /// <param name="target">The marked object.</param>
    /// <param name="markerId">The marker identifier.</param>
    /// <param name="isActive">Whether the marker is active.</param>
    ValueTask SetMarkerAsync(IIdentifiable target, byte markerId, bool isActive);
}
