// Copyright 2023 Treer (https://github.com/Treer)
// License: MIT, see LICENSE.txt for rights granted

using Godot;

namespace MapGen.Tiles
{
    public interface ITileServer
    {
        /// <summary>The Width/Height of the tile in the world coordinate system. Divide by Scale to get the TileResolution of the Texture2D data.</summary>
        int TileLength { get; }
        /// <summary>The Width/Height of the tile data (when rendered as a 2D texture). Multiply by Scale to get the TileLength in worldCoords.</summary>
        int TileResolution { get; }
        /// <summary>The size of a datapoint (i.e. pixel obtained from the ITileRender2D) in the world coordinate system. Equal to TileLength / TileResolution</summary>
        float Scale { get; }

        /// <summary>
        /// Gets the type of the tiles that the TileServer serves.
        /// Not often needed, possibly obsolete, but an easy enough property to implement (it was initially
        /// so that a BaseTileServer class could use reflection to provide some tile data functions).
        /// </summary>
        Type TileType { get; }

        /// <summary>
        /// Gets a short string that will be appended to the end of screenshot filenames. This might be empty, or just the name of
        /// the TileServer, or a compact representation of the TileServer configuration generating the screencaptured tiles.
        /// </summary>
        public string DiagnosticFilenameSuffix { get; }

        /// <summary>Get the tile that covers worldCoord, or which has worldCoord as its top left corner</summary>
        Task<ITile> GetTile(Vector2 worldCoord);

        /// <summary>Returns the ITile.Pos (topleft tile coord) for the tile which contains the given worldCoord</summary>
        Vector2I TileContainingWorldCoord(Vector2 worldCoord);

        /// <summary>
        /// A 2D renderer matched to the tiles this instance serves.
        /// 
        /// If instances of a TileServer class will not provide a functional ITileRender2D, the TileServer class should
        /// be marked with the [HiddenTileServer] attribute, to prevent it being available in the mapviewer GUI.
        /// </summary>
        /// <seealso cref="HiddenTileServerAttribute"/>
        ITileRender2D Render2D { get; }
    }
}