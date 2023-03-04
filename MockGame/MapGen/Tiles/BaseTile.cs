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

        protected Vector2I LowerCorner => Pos + ((TileServer.TileLength - 1) * Vector2I.Up);

    }
}
