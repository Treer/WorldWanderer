// Copyright 2023 Treer (https://github.com/Treer)
// License: MIT, see LICENSE.txt for rights granted

using System;

namespace MapGen.Tiles
{
    /// <summary>
    /// TileServers which are not intended to be available from the GUI mapviewer can be marked with [HiddenTileServer].
    /// This is useful when having a pipeline of different ITileManagers & ITiles chained together, which
    /// does not provide a proper ITileRender2D for each ITileManager & ITile pair.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class HiddenTileServerAttribute : Attribute
    {
    }
}
