// Copyright 2023 Treer (https://github.com/Treer)
// License: MIT, see LICENSE.txt for rights granted

using Godot;

namespace MapViewer
{
	/// <summary>
	/// C# interfaces don't fare well being passed into GDScript and then out again, so
	/// this class allows the TileManager to return the positional information to display 
	/// instead of GDScript trying to operate the C# routines.
	/// </summary>
	public partial class GPositionInformation: GodotObject
	{
		public string coords;
		public string long_description;
		public string short_description;
	}
}
