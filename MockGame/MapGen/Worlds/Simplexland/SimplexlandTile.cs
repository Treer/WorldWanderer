// Copyright 2023 Treer (https://github.com/Treer)
// License: MIT, see LICENSE.txt for rights granted

using Godot;
using MapGen.Tiles;

namespace MapGen.Worlds.Simplexland
{
    public class SimplexlandTile : ITile
    {

        /// <summary>Coordinate of the top-left corner of the tile (where HeightData[0] is located)</summary>
        public Vector2I Pos { get; private set; }
        public ITileServer TileServer { get; private set; }


        public float[] HeightData { get; private set; }

        public SimplexlandTile(Vector2I pos, ITileServer tileServer)
        {

            Pos = pos;
            TileServer = tileServer;
            HeightData = new float[TileServer.TileResolution * TileServer.TileResolution];
        }

        public SimplexlandTile Generate(Noise noise, float mountainHeight)
        {
            int width = TileServer.TileResolution;
            float scale = TileServer.Scale;

            int i = 0;
            for (int y = width - 1; y >= 0; y--)
            {
                for (int x = 0; x < width; x++)
                {
                    HeightData[i++] = noise.GetNoise2D(Pos.X + x * scale, Pos.Y + y * scale) * mountainHeight;
                }
            }

            return this;
        }

        public override string ToString()
        {
            return $"[{GetType().Name} at {Pos.X},{Pos.Y}]";
        }
    }
}
