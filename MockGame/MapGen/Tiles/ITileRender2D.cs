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
    public interface ITileRender2D
    {
        public Texture2D AsTexture(ITile tile);
        /// <summary>Short description of the location in the tile, e.g. "23m" (the altitude)</summary>
        public string AsStringShort(ITile tile, Vector2 localPosition);
        /// <summary>Description of the location in the tile, e.g. "Meadows"</summary>
        public string AsStringLong(ITile tile, Vector2 localPosition);
    }
}
