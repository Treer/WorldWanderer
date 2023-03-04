// Copyright 2023 Treer (https://github.com/Treer)
// License: MIT, see LICENSE.txt for rights granted

using Godot;
using MapGen.Tiles;
using System;
using System.Diagnostics;

namespace MapViewer.TestTile
{
	public class TestTile: ITile
	{
		public Vector2I Pos { get; private set; }
		public ITileServer TileServer { get; private set; }

		public TestTile(Vector2I pos, ITileServer tileServer) {
			Pos = pos;
			TileServer = tileServer;
		}
	}
}
