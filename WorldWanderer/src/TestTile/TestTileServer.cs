// Copyright 2023 Treer (https://github.com/Treer)
// License: MIT, see LICENSE.txt for rights granted

using Godot;
using MapGen.Tiles;
using System;
using System.Threading.Tasks;

namespace MapViewer.TestTile
{
	/// <summary>
	/// A dummy ITileServer implementation that returns coloured tiles
	/// </summary>
	public class TestTileServer : ITileServer
	{
		public int TileResolution { get { return 64; } }
		public int TileLength { get { return 64; } }
		public float Scale { get { return 1.0f; } }
		public Type TileType => typeof(TestTile);
        public string DiagnosticFilenameSuffix => GetType().Name;
        public ITileRender2D Render2D => testTileRender2D;

		public Task<ITile> GetTile(Vector2 worldCoord) {
			var tileCoords = TileContainingWorldCoord(worldCoord);
			return Task.FromResult((ITile)new TestTile(tileCoords, this));
		}

		/// <inheritdoc/>
		public Vector2I TileContainingWorldCoord(Vector2 worldCoord) {
			return new Vector2I((int)Mathf.Floor(worldCoord.X / TileLength) * TileLength, (int)Mathf.Ceil(worldCoord.Y / TileLength) * TileLength);
		}

		private ITileRender2D testTileRender2D = new TestTileRender2D();
	}
}
