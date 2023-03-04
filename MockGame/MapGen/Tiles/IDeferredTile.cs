// Copyright 2023 Treer (https://github.com/Treer)
// License: MIT, see LICENSE.txt for rights granted

namespace MapGen.Tiles
{
    public interface IDeferredTile: ITile
    {
        bool Finished { get; }
        double FinishTime_unix { get; set; }
        /// <summary>
        /// Null until it has finished generating.
        /// </summary>
        ITile? WrappedTile { get; }
        /// <summary>
        /// Does not abort any process by itself, merely provides a mechanism for one part
        /// of the program to signal to other parts when a tile is unloaded.
        /// </summary>
        public bool Abort { get; set; }
    }
}
