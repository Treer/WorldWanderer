// Copyright 2023 Treer (https://github.com/Treer)
// License: MIT, see LICENSE.txt for rights granted

using Godot;
using MapGen.Tiles;

namespace MapViewer.TestTile
{
    internal class TestTileRender2D : ITileRender2D
    {
        public string AsStringLong(ITile tile, Vector2 localPosition) {
            return tileColor(tile).ToHtml(false);
        }

        public string AsStringShort(ITile tile, Vector2 localPosition) {
            return $"({localPosition.X:0.##}, {localPosition.Y:0.##})";
        }

        public Texture2D AsTexture(ITile tile) {
            var color = tileColor(tile);
            int width = tile.TileServer.TileResolution;
            var tileImage = Image.CreateEmpty(width, width, true, Image.Format.Rgba8);
            tileImage.Fill(color);

            return ImageTexture.CreateFromImage(tileImage);
        }

        Color tileColor(ITile tile) {
            // The TestTile class doesn't contain any actual data or a heightmap, but it
            // does have a position, so give it a color based on that.
            int a = (int)(tile.Pos.X / tile.TileServer.TileResolution) * 16;
            int b = (int)(tile.Pos.Y / tile.TileServer.TileResolution) * 16;
            return Color.Color8((byte)(a % 256), (byte)(b % 256), (byte)((a >> 8 + b >> 8) % 256));
        }
    }
}
