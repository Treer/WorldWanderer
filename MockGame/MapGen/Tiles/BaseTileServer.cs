// Copyright 2023 Treer (https://github.com/Treer)
// License: MIT, see LICENSE.txt for rights granted

using Godot;
using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace MapGen.Tiles
{
    [Display(Name = "untitled subclass of BaseTileServer")] // subclasses should set a display name, or they will be called this
    public abstract class BaseTileServer<TRenderer> : ITileServer where TRenderer : ITileRender2D, new() {
        /// <inheritdoc/>
        public abstract int TileLength { get; }
        /// <inheritdoc/>
        public abstract int TileResolution { get; }
        /// <inheritdoc/>
        public float Scale { get { return TileLength / (float)TileResolution; } }
        public abstract Type TileType { get; }
        public string DiagnosticFilenameSuffix => GetType().Name;
        public abstract Task<ITile> GetTile(Vector2 worldCoord);
        public ITileRender2D Render2D { get; set; } = new TRenderer();

        /// <inheritdoc/>
        public virtual Vector2I TileContainingWorldCoord(Vector2 worldCoord) {
            return new Vector2I(
                (int)Mathf.Floor(worldCoord.X / TileLength) * TileLength,
                (int)Mathf.Ceil(worldCoord.Y / TileLength) * TileLength
            );
        }
    }
}
