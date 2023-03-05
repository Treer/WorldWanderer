# Procedural World Wanderer

## A Godot 4 map-viewer for developing dotnet procedural landscapes

<img src="https://user-images.githubusercontent.com/6390507/222958322-390aab7d-98db-4619-be43-c01896880d74.svg" align="right" width="240px" />

![License](https://img.shields.io/badge/license-MIT-green)

This is a map viewer similar to Amidst for Minecraft, but for procedural landscape generators that are written in C#. It was written as an in‑house tool for developing my own game (so it isn't highly polished), but could be used for other games as either an in‑house development tool or polished into an Amidst-like player-tool.

[![moving screenshot link](https://user-images.githubusercontent.com/6390507/222958354-bd3cb6f9-36dc-43ec-9aff-0eb2646bb860.jpg)](https://www.youtube.com/watch?v=-67ixJ2LEjg)

## Features

* Displays "under the mouse-cursor" terrain information text
* Boolean settings for each map can be exposed in a Generation menu
* Int and float settings for each map can be exposed in the console (press Esc for console)
* Teleport and seed commands are built into the console.
* Screenshotting (use Ctrl+arrowkeys if you wish to move exactly one screenshot-length of distance)
* Godot compiles to many platforms, so the map viewer can run native and accelerated on Windows, Linux etc.

## Going forward

TODO: Better documentation - in the meantime I hope the section below and the [Simplexland example](https://github.com/Treer/WorldWanderer/tree/master/MockGame/MapGen/Worlds/Simplexland) is clear enough.

My attention will stay on gamedev rather than this tool, but any improvements I add along the way should get automatically picked up by this repository, and who knows... perhaps someone will implement cool new features or polish or mapgens and open pull requests.

## Adding MapGens

Terrain generation code is normally part of the game, which is called "MockGame" in this project. The map viewer project just links to the game's assembly and finds any terrain generators by using reflection.

Add a class that implements ITileServer, and the map viewer will automatically find and add it to the View menu. To implement ITileServer you'll usually end up implementing 3 concrete classes
* a class that implements [ITile](https://github.com/Treer/WorldWanderer/blob/master/MockGame/MapGen/Tiles/ITile.cs) - a container for whatever data your mapgen wishes to store about a gridsquare.
* a class that implements [ITileServer](https://github.com/Treer/WorldWanderer/blob/master/MockGame/MapGen/Tiles/ITileServer.cs) - responsible for fetching any requested tile. Also defines the size of a gridsquare. I often make these a subclass of BaseTileServer, but you don't have to.
* a class that implements [ITileRender2D](https://github.com/Treer/WorldWanderer/blob/master/MockGame/MapGen/Tiles/ITileRender2D.cs) - responsible for turning the data held in an ITile into a Texture2D.

These interfaces can be chained together in pipelines. [TileCache](https://github.com/Treer/WorldWanderer/blob/master/MockGame/MapGen/Tiles/TileCache.cs) is an ITileServer that wraps another ITileServer, caching the results of requested tiles in memory as it passes them through, and threadsafely awaiting tile construction if there are multiple requests for the same tile. I've used this in between two other tile servers, e.g. when a terrain tile is requested it will fetch a larger continent tile to get information about its wider location, and the TileCache means multiple terrain tiles won't cause multiple recalculations of their continent. Meanwhile continent generation code is kept separate from terrain code.

There are two example MapGens provided, the [TileTestServer](https://github.com/Treer/WorldWanderer/tree/master/WorldWanderer/src/TestTile) in the WorldWanderer Godot Project, and the [Simplexland example](https://github.com/Treer/WorldWanderer/tree/master/MockGame/MapGen/Worlds/Simplexland) in the MockGame.

### Constructors and seeds

TileServer constructors may have optional parameters (i.e. parameters with default values), or no parameters. The only non-optional parameter a TileServer constructor can have is one of type `ulong` with a name that contains the word "seed". The value that will be passed to that parameter can be determined by the user via the "seed" console command.

TileServer constructors are free to break these rules, and TileCache is an example of that, it just means they won't be picked up automatically by the mapviewer.

