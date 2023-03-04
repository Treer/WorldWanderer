// Copyright 2023 Treer (https://github.com/Treer)
// License: MIT, see LICENSE.txt for rights granted

using Godot;
using MapGen.Helpers;
using System.Collections.Concurrent;

namespace MapGen.Tiles
{
    public class DeferredTile<T> : IDeferredTile
    {
        private double _startTime_unix;

        public Vector2I Pos { get; private set; }
        public ITileServer TileServer { get; private set; }
        public double FinishTime_unix { get; set; }
        public bool Finished { get { return FinishTime_unix > double.MinValue; } }
        /// <inheritdoc/>
        public ITile? WrappedTile { get; private set; }
        /// <inheritdoc/>
        public bool Abort { get; set; }

        /// <param name="synchronous">Can be set true for debugging - helps keep GD.Print() output in order</param>
        public DeferredTile(Vector2I pos, ITileServer tileServer, Action<IDeferredTile, T> callback, T callbackParm, bool synchronous = false) {
            Pos = pos;
            TileServer = tileServer;
            FinishTime_unix = double.MinValue;
            WrappedTile = null;
            Abort = false;

            WaitCallback action = async state => {
                WrappedTile = await tileServer
                    .GetTile(pos)
                    .ConfigureAwait(synchronous); // https://cpratt.co/async-tips-tricks/  if we're not passing this to a helper thread then we'll want to keep in the same context when running synchronously

                FinishTime_unix = Time.GetUnixTimeFromSystem();
                callback(this, (T)state);


                #if DEBUG
                if (Time.GetUnixTimeFromSystem() - _startTime_unix > 5) {
                    Logger.GlobalLog.LogInformation($"[color=yellow]DeferredTile took {FinishTime_unix - _startTime_unix:F2} seconds[/color] to generate and {Time.GetUnixTimeFromSystem() - FinishTime_unix:F2} to callback. Its thread may have been held up by a debugging image.");
                }
                #endif
            };

            _startTime_unix = Time.GetUnixTimeFromSystem();
            if (synchronous) {
                // Invoke the delegate directly on the current thread, for debugging - helps keep GD.Print() output in order
                action(callbackParm);
            } else {
                ThreadPool.QueueUserWorkItem(action, callbackParm);
            }
        }
    }
}
