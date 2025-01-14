// Copyright 2023 Treer (https://github.com/Treer)
// License: MIT, see LICENSE.txt for rights granted

using Godot;
using MapGen.Tiles;

namespace MapGen.Worlds.Simplexland
{
    internal class SimplexlandTileRender2D : ITileRender2D
    {
        static readonly Gradient landColor;
        static SimplexlandTileRender2D()
        {

            landColor = new Gradient();
            landColor.AddPoint(-400f, new Color("4E72E2")); // very deep sea
            landColor.AddPoint(-40f, new Color("5E82E2")); // deep sea
            landColor.AddPoint(-0.5f, new Color("78A5FF")); // sea
            landColor.AddPoint(0, new Color("f5f5c0")); // beach
            landColor.AddPoint(14.5f, new Color("f5f5c0")); // beach
            landColor.AddPoint(15f, new Color("4c9627")); // green
            landColor.AddPoint(100f, new Color("75e03f")); // green
            landColor.AddPoint(350f, new Color("f5efab")); // tussock
            landColor.RemovePoint(0); // remove the colours it was initialized with
            landColor.RemovePoint(0); // remove the colours it was initialized with
        }


        public string AsStringLong(ITile tile, Vector2 localPosition)
        {
            var simplexlandTile = (SimplexlandTile)tile;
            int index = (int)localPosition.X + (int)localPosition.Y * tile.TileServer.TileResolution;
            float height = simplexlandTile.HeightData[index];

            return $"Height {height:0.#}m";
        }

        public string AsStringShort(ITile tile, Vector2 localPosition)
        {
            return $"({localPosition.X:0.##}, {localPosition.Y:0.##})";
        }

        public Texture2D AsTexture(ITile tile)
        {

            var simplexlandTile = (SimplexlandTile)tile;
            SimplexlandTileServer tileServer = (SimplexlandTileServer)simplexlandTile.TileServer;
            float sealevel = tileServer.Sealevel;
            bool showContourLines = tileServer.ShowContourLines;
            int width = tile.TileServer.TileResolution;

            var tileImage = Image.CreateEmpty(width, width, false, Image.Format.Rgb8);

            Vector3 lightingAngle = new Vector3(-1, -2, 1).Normalized(); // light should be shining from 10 - 11 o'clock
            Color color;
            int i = 0;
            for (int y = 0; y < width; y++)
            {

                float height_center = simplexlandTile.HeightData[i];
                float height_left = 2 * height_center - simplexlandTile.HeightData[i + 1]; // copy from the gradient to the right of the current point

                for (int x = 0; x < width; x++)
                {
                    float height_right = x + 1 < width ? simplexlandTile.HeightData[i + 1] : 2 * height_center - simplexlandTile.HeightData[i - 1]; // copy from the gradient to the left of the current point

                    float height_up = y > 0 ? simplexlandTile.HeightData[i - width] : 2 * height_center - simplexlandTile.HeightData[i + width]; // copy from the gradient to the bottom of the current point
                    float height_down = y + 1 < width ? simplexlandTile.HeightData[i + width] : 2 * height_center - simplexlandTile.HeightData[i - width]; // copy from the gradient above the current point

                    Vector3 horzGradient = new Vector3(-1, (height_right - height_left) / 2f, 0);
                    Vector3 vertGradient = new Vector3(0, (height_down - height_up) / 2f, 1);
                    Vector3 surfaceNormal = horzGradient.Cross(vertGradient).Normalized();

                    float brightness = Mathf.Clamp(-lightingAngle.Dot(surfaceNormal), 0f, 1f);
                    float darkenStrength = height_center > sealevel ? 0.3f : 0.05f;
                    color = landColor.Sample(height_center - sealevel).Darkened((1 - brightness) * darkenStrength);

                    if (showContourLines && height_center > sealevel + 3f)
                    {
                        var lineStrength = 1f - Mathf.Abs(Mathf.Clamp(height_center % 20, 0f, 3f) - 1.5f) / 1.5f;
                        color = color.Darkened(lineStrength * 0.5f);
                    }

                    tileImage.SetPixel(x, y, color);
                    height_left = height_center;
                    height_center = height_right;
                    i++;
                }
            }

            return ImageTexture.CreateFromImage(tileImage);
        }
    }
}
