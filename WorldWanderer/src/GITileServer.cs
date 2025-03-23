// Copyright 2024 Treer (https://github.com/Treer)
// License: MIT, see LICENSE.txt for rights granted

using Godot;
using MapGen.Tiles;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace MapViewer
{

    /// <summary>
    /// Wraps a GodotObject around an ITileServer to allow Godot to access the methods and properties exposed by the interface
    /// </summary>
    [HiddenTileServer]
    public partial class GITileServer : GodotObject, ITileServer // implements ITileServer so the compiler will ensure it exposes everything in the interface and is correct
    {
        public ITileServer WrappedITileServer { get; set; }
        
        public int TileLength => WrappedITileServer.TileLength;
        public int TileResolution => WrappedITileServer.TileResolution;
        public float Scale => WrappedITileServer.Scale;
        public Type TileType => WrappedITileServer.TileType;
        public string DiagnosticFilenameSuffix => WrappedITileServer.DiagnosticFilenameSuffix;
        public ITileRender2D Render2D => WrappedITileServer.Render2D;
        public Task<ITile> GetTile(Vector2 worldCoord) => WrappedITileServer.GetTile(worldCoord);
        public Vector2I TileContainingWorldCoord(Vector2 worldCoord) => WrappedITileServer.TileContainingWorldCoord(worldCoord);
        
        public GITileServer(ITileServer tileServer) { WrappedITileServer = tileServer; }
    }
}
