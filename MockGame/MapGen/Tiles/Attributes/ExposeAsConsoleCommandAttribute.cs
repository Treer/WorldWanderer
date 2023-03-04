// Copyright 2023 Treer (https://github.com/Treer)
// License: MIT, see LICENSE.txt for rights granted

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MapGen.Tiles
{
    /// <summary>
    /// Marking a boolean property on a TileServer implementation with this attribute tells the map
    /// viewer to expose that property as a checkbox menu-item.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class ExposeAsConsoleCommandAttribute : Attribute
    {

        const char seperatorChar = '|';

        /// <param name="command"></param>
        /// <param name="description"></param>
        /// <param name="discardExistingTiles">Whether the tiles should be re-rendered when the setting is changed with this console command</param>
        public ExposeAsConsoleCommandAttribute(string command, string description, bool discardExistingTiles = true)
        {
            Command = Regex
                .Replace(command, "[^a-zA-Z0-9_-]", "") // strip out any chars that might screw up commandline syntax
                .Replace(seperatorChar, '_');

            Description = description.Replace(seperatorChar, '_');
            DiscardExistingTiles = discardExistingTiles;
        }

        public string Command { get; set; }
        public string Description { get; set; }

        /// <summary>
        /// Returns a string combining command and description, which can be used as a key in a ConfigFile, allowing
        /// either command or description to be extracted from the key.
        /// </summary>
        public string CommandAndDescription()
        {
            return $"{Command}{seperatorChar}{Description}";
        }
        /// <summary>Whether the tiles should be re-rendered when the setting is changed with this console command</summary>
        public bool DiscardExistingTiles { get; set; }
    }
}
