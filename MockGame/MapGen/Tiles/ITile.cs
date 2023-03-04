// Copyright 2023 Treer (https://github.com/Treer)
// License: MIT, see LICENSE.txt for rights granted

using Godot;

namespace MapGen.Tiles
{
    /// <summary>
    /// Base interface for a Tile of a procedurally generated world
    /// 
    /// Mostly empty because MapViewer shouldn't need to know anything about a tile's implementation.
    /// A TileServer should provide any tools needed to interact with its tiles.
    /// (even giving tiles a position forces choices: 2D or 3D, int, longint or float etc.)
    /// </summary>
    public interface ITile
    {
        /// <summary>
        /// World coordinates for the corner of the tile that's (0,0) in its local coords (let's say "top-left corner" for now)
        /// </summary>
        Vector2I Pos { get; }

        ITileServer TileServer { get; }
    }
}