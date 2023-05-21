// Copyright 2023 Treer (https://github.com/Treer)
// License: MIT, see LICENSE.txt for rights granted

using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapGen.Tiles
{
    public abstract class BaseTile: ITile
    {
        public abstract Vector2I Pos { get; }
        public abstract ITileServer TileServer { get; }

        /// <summary>
        /// Be very careful with this.   <br/><br/>
        /// 
        /// If your tile dimensions represent something discrete such as a block of pixels or heightmap
        /// values, then a tile's LowerCorner would be one 'unit' above the start of the tile below it. (This
        /// is why the function below is subtracting 1 from TileServer.TileLength) <br/>
        /// 
        /// However, if your tile represents something continuous, such as an area in a triangle mesh, then
        /// the LowerCorner would be the same height as the start of the tile below it, and you should overload
        /// this function with one that doesn't subtract 1 from TileServer.TileLength. <br/><br/>
        /// 
        /// https://stackoverflow.com/questions/16566702/what-is-the-interval-when-rasterizing-primitives    <br/>
        /// https://learn.microsoft.com/en-gb/windows/win32/direct3d9/directly-mapping-texels-to-pixels
        /// </summary>
        protected Vector2I LowerCorner => Pos + ((TileServer.TileLength - 1) * Vector2I.Up);
    }
}
