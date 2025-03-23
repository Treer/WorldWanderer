// Copyright 2023 Treer (https://github.com/Treer)
// License: MIT, see LICENSE.txt for rights granted

using Godot;
using System.Collections.Concurrent;
using System.Reflection;

namespace MapGen.Tiles
{
    /// <summary>
    /// Wraps an ITileServer, caching generated tiles.
    /// perhaps rename to TileLayer?
    /// </summary>
    [HiddenTileServer]
    public class TileCache : ITileServer
    {
        private ITileServer tileServer;
        private ConcurrentDictionary<Vector2I, Task<Task<ITile>>> tiles = new ConcurrentDictionary<Vector2I, Task<Task<ITile>>>();
        public TileCache(ITileServer tileServer) {
            this.tileServer = tileServer;
        }

        public int TileResolution              => tileServer.TileResolution;
        public int TileLength                  => tileServer.TileLength;
        public float Scale                     => tileServer.Scale;
        public Type TileType                   => tileServer.TileType;
        public string DiagnosticFilenameSuffix => tileServer.DiagnosticFilenameSuffix;
        public ITileRender2D Render2D          => tileServer.Render2D;

        public async Task<ITile> GetTile(Vector2 worldCoord) {
            var tileCoords = TileContainingWorldCoord(worldCoord);

            // TODO: limit size in memory - maintain a list of when tiles were last accessed? Or perhaps the dictionary.Keys stays in order of keys added?

            // Wrap the GetTile call in a task which hasn't started yet, to avoid a GetTile() call starting needlessly
            var taskWrapper = new Task<Task<ITile>>(async () => await tileServer.GetTile(tileCoords).ConfigureAwait(false));

            Task<ITile> cachedTask;
            if (tiles.TryAdd(tileCoords, taskWrapper)) {
                taskWrapper.Start();
                cachedTask = await taskWrapper.ConfigureAwait(false); // https://cpratt.co/async-tips-tricks/
            } else {
                // the wrapped task has usually finished already, but not always, so await it
                cachedTask = await tiles[tileCoords].ConfigureAwait(false);
            }
            return await cachedTask.ConfigureAwait(false);
        }

        public Vector2I TileContainingWorldCoord(Vector2 worldCoord) {
            return tileServer.TileContainingWorldCoord(worldCoord);
        }
    }
}
