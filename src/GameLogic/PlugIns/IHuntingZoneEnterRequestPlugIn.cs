// <copyright file="IHuntingZoneEnterRequestPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Runtime.InteropServices;
using MUnique.OpenMU.PlugIns;

/// <summary>Handles client requests to enter a castle hunting zone, such as Lands of Trials.</summary>
[Guid("FB275477-F9AB-4D7C-98CE-B529EA0E5E94")]
[PlugInPoint("Hunting zone entry request", "Handles requests to enter a castle hunting zone.")]
public interface IHuntingZoneEnterRequestPlugIn
{
    /// <summary>Handles a hunting-zone entry request.</summary>
    /// <param name="player">The requesting player.</param>
    /// <param name="arguments">The mutable request result.</param>
    ValueTask HandleHuntingZoneEnterRequestAsync(Player player, HuntingZoneEnterRequestArguments arguments);

    /// <summary>Contains the mutable request result.</summary>
    public sealed class HuntingZoneEnterRequestArguments
    {
        /// <summary>Initializes a new instance of the <see cref="HuntingZoneEnterRequestArguments"/> class.</summary>
        /// <param name="requestedMoney">The entrance fee sent by the client.</param>
        public HuntingZoneEnterRequestArguments(uint requestedMoney)
        {
            this.RequestedMoney = requestedMoney;
        }

        /// <summary>Gets the entrance fee sent by the client.</summary>
        public uint RequestedMoney { get; }

        /// <summary>Gets or sets a value indicating whether a plugin handled the request.</summary>
        public bool Handled { get; set; }
    }
}
