// Copyright 2023 Treer (https://github.com/Treer)
// License: MIT, see LICENSE.txt for rights granted

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapGen.Tiles
{
    /// <summary>
    /// Marking a boolean property on a TileServer implementation with this attribute tells the map
    /// viewer to expose that property as a checkbox menu-item.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class ExposeAsMenuItemAttribute : Attribute
    {
        /// <param name="caption">The menu-item caption</param>
        /// <param name="discardExistingTiles">Whether the tiles should be re-rendered when the setting is changed with this console command</param>
        public ExposeAsMenuItemAttribute(string caption, bool discardExistingTiles = true)
        {
            Caption = caption;
            DiscardExistingTiles = discardExistingTiles;
        }

        /// <summary>The menu-item caption</summary>
        public string Caption { get; set; }

        /// <summary>Whether the tiles should be re-rendered when the setting is changed with this console command</summary>
        public bool DiscardExistingTiles { get; set; }
    }
}
