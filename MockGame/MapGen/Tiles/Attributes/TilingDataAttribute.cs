// Copyright 2023 Treer (https://github.com/Treer)
// License: MIT, see LICENSE.txt for rights granted

using System;
using static Godot.Image;

namespace MapGen.Tiles
{
    /// <summary>
    /// Indicates that a Tile's array property is tiling data, and how it should be
    /// interpolated when needed at a different resolution.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class TilingDataAttribute : Attribute
    {
        public Interpolation InterpolationMethod { get; set; }
        public TilingDataAttribute(Interpolation interpolationMethod = Interpolation.Cubic)
        {
            InterpolationMethod = interpolationMethod;
        }
    }
}
