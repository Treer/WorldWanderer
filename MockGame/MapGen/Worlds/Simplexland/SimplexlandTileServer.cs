// Copyright 2023 Treer (https://github.com/Treer)
// License: MIT, see LICENSE.txt for rights granted

using Godot;
using MapGen.Tiles;
using System.ComponentModel.DataAnnotations;

namespace MapGen.Worlds.Simplexland
{
    [Display(Name = "Simplexland example")]
    public class SimplexlandTileServer : ITileServer // any concrete class that implements ITileServer and has a simple constructor will get listed in the View menu of the map viewer 
    {
        /// <inheritdoc/>
        public int TileResolution { get { return 128; } }
        /// <inheritdoc/>
        public int TileLength { get { return 640; } }
        /// <inheritdoc/>
        public float Scale { get { return 5.0f; } }
        /// <inheritdoc/>
        public Type TileType => typeof(SimplexlandTile);
        /// <inheritdoc/>
        public ITileRender2D Render2D { get; } = new SimplexlandTileRender2D();
        /// <inheritdoc/>
        public string DiagnosticFilenameSuffix => $"Simplexland with sealevel {Sealevel}";

        /// <summary>
        /// An example of how to expose a setting so that it can be adjusted within the Map Viewer using the MapViewer console.
        /// </summary>
        [ExposeAsConsoleCommand("sealevel", "Higher values produce more ocean, with values above 0 leading to islands and values below zero leading to lakes. 50 is the default")]
        public float Sealevel { get; set; } = 50;

        /// <summary>
        /// An example of how to expose a setting so that it can be adjusted within the Map Viewer using the MapViewer console.
        /// </summary>
        [ExposeAsConsoleCommand("mountain_height", "Height of mountains. 600 is the default")]
        public float MountainHeight { get; set; } = 600;

        /// <summary>
        /// An example of how to expose a checkbox menu-item in the Map Viewer's "Generator" menu
        /// </summary>
        [ExposeAsMenuItem("Contour lines")]
        public bool ShowContourLines { get; set; } = false;


        private Noise noise;

        /// <param name="seed">
        /// Even though FastNoiseLite uses an int for a seed, the TileManager will only supply a seed to the TileServer
        /// constructor if its first parameter is of type ulong with "seed" in it's name, so use ulong here.
        /// </param>
        public SimplexlandTileServer(ulong seed = 1)
        {

            var openSimplexNoise = new Godot.FastNoiseLite();
            openSimplexNoise.NoiseType = Godot.FastNoiseLite.NoiseTypeEnum.SimplexSmooth;
            openSimplexNoise.Seed = (int)seed;
            openSimplexNoise.FractalOctaves = 7;
            openSimplexNoise.Frequency = 1 / 6000.0f; // Period
            //openSimplexNoise.FractalGain = 0.5f;      // Persistence

            noise = openSimplexNoise;
        }

        public Task<ITile> GetTile(Vector2 worldCoord)
        {
            var tileCoords = TileContainingWorldCoord(worldCoord);
            return Task.FromResult(
                (ITile)new SimplexlandTile(tileCoords, this).Generate(noise, MountainHeight)
            );
        }

        /// <inheritdoc/>
        public Vector2I TileContainingWorldCoord(Vector2 worldCoord)
        {
            return new Vector2I(
                (int)Mathf.Floor(worldCoord.X / TileLength) * TileLength,
                (int)Mathf.Ceil(worldCoord.Y / TileLength) * TileLength // Y+ is up, so worldCoords of top-left corner have higher Y value than bottom corner
            );
        }
    }
}