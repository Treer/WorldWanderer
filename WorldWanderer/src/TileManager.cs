// Copyright 2024 Treer (https://github.com/Treer)
// License: MIT, see LICENSE.txt for rights granted

using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MapGen.Tiles;
using MapViewer.DynamicConfig;

namespace MapViewer
{
	public partial class TileManager : Node
	{
		[Export]
		public NodePath ParallaxLayer { get; set; }

		private ParallaxLayer ParallaxLayerNode {
			get { return ParallaxLayer == null ? null : GetNode<ParallaxLayer>(ParallaxLayer); }
		}

		[Export]
		/// <summary>Keep this up to date. It should be a Vector2I but that will have to wait until godot 4</summary>
		public Vector2I ScreenSize {
			get { return _screenSize; }
			set {
				if (_screenSize != value) {
					_screenSize = value;
					updateRequired = true;
				}
			}
		}
		[Export]
		public float ScreenScale {
			get { return _screenScale; }
			set {
				if (_screenScale != value) {
					_screenScale = value;
					updateRequired = true;
				}
			}
		}
		[Export]
		public Vector2 ScreenTopLeft_WorldCoord {
			get { return _screenTopLeft_WorldCoord; }
			set {
				if (_screenTopLeft_WorldCoord != value) {
					_screenTopLeft_WorldCoord = value;
					updateRequired = true;
				}
			}
		}
		[Export]
		/// <summary>How many offscreen tiles should be remembered. It should be a Vector2I but that will have to wait until godot 4</summary>
		public Vector2 OffscreenColumnsAndRows {
			get { return _offscreenColumnsAndRows; }
			set {
				if (_offscreenColumnsAndRows != value) {
					_offscreenColumnsAndRows = value;
					updateRequired = true;
				}
			}
		}
		[Export]
		public double MinimumTimeBetweenUpdates_secs { get; set; } = 0.05;

		[Export]
		public int TileFadeInTime_ms { get; set; } = 600;

		[Export]
		public ulong WorldSeed { get; set; } = 1;

		[Signal]
		public delegate void tileserver_changedEventHandler(int oldServer, int newServer);

		/// <seealso cref="tile_server"/>
		public ITileServer TileServer { 
			get { return _tileServer; }
			private set {
				if (value == null) throw new ArgumentNullException("TileServer");
				// Unload all the tiles first
				AddOrRemoveRows(true, -TileRowList.Count); // Remove all the rows, rather than columns, as code assumes rows have at least one column
				_tileServer = value;
				_godotTileServer.WrappedITileServer = value;
				GD.Print($"Set _tileServer to {value.GetType().Name}");
				updateRequired = true;
			}
		}

		/// <summary>
		/// A Godot-accessible mirror of the <see cref="TileServer"/> property
		/// </summary>
		public GITileServer tile_server => _godotTileServer;


		/// <summary>
		/// Provides GDScript with a way to get and set the current TileServer's configuration values
		/// The ConfigFile may contain two optional sections, 'Menu' and 'Console', for checkbox menu-items and console variables respectively.
		/// </summary>
		public GodotConfigFile TileServerConfig { 
			get { return _tileServerConfig; } 
			private set {
				if (_tileServerConfig != value) {
					if (_tileServerConfig != null) {
						_tileServerConfig.ValueChanged -= TileServerConfig_ValueChanged;
					}
					_tileServerConfig = (ObservableConfigFile)value;
					_tileServerConfig.ValueChanged += TileServerConfig_ValueChanged;
				}
			}
		}
		private ObservableConfigFile _tileServerConfig;

		/// <summary>The section name for menu-item checkboxes in TileServerConfig</summary>
		public const string TileServerConfigSection_Menu = "Menu";
		/// <summary>The section name for values in TileServerConfig that can be set via console commands</summary>
		public const string TileServerConfigSection_Console = "Console";

		/// <summary>
		/// should normally be false, set true for debugging.
		/// Not sure this is properly implemented yet.
		/// </summary>
		public bool GenerateTilesSynchronously { get; private set; } = false;

		/// <summary>Gets an index into <see cref="TileServerList"/>. Use SetTileServer() to select a tile server</summary>
		public int SelectedTileServer { get; private set; } = -1;

		/// <summary>Exposes the ordered names of the available TileServers to Godot. Use the index of this array when calling SetTileServer()</summary>
		public string[] TileServerList = new string[] { };

		/// <summary>
		/// Call this if you've changed a rendering setting on the current TileServer and want the screen redrawn
		/// </summary>
		public void RerenderAllTiles() {
			AddOrRemoveRows(true, -TileRowList.Count); // Remove all the rows, rather than columns, as code assumes rows have at least one column
			updateRequired = true;
		}

		/// <summary>
		/// +Y is up in Map coords, but down in CanvasLayer world coords.
		/// Multiply by this to switch between the two.
		/// </summary>
		public static readonly Vector2 FlipYAxis = new Vector2(1, -1);

		private Vector2I _screenSize                           = new Vector2I(640, 240);
		private float _screenScale                             = 1f;
		private Vector2 _screenTopLeft_WorldCoord              = new Vector2(-320, 120);
		private Vector2 _offscreenColumnsAndRows               = new Vector2(4, 4);
		private List<List<ITile>> _tileRowList                 = new List<List<ITile>>();
		private Dictionary<Type, string> _availableTileServers = new Dictionary<Type, string>();
		private ITileServer _tileServer                        = testTileServer;
		private GITileServer _godotTileServer                  = new GITileServer(testTileServer);

		private Dictionary<ITile, Sprite2D> sprites = new Dictionary<ITile, Sprite2D>();
		private List<(IDeferredTile, Texture2D)> newlyGeneratedTiles = new List<(IDeferredTile, Texture2D)>();
		private List<ITile> tilesToFadeIn = new List<ITile>();

		private double lastUpdate = double.MinValue;
		private bool updateRequired = false;
		private int tilesLoaded = 0, tilesUnloaded = 0, tilesTotal = 0;
		private static ITileServer testTileServer = new TestTile.TestTileServer();

		/// <summary>A non-zero value means movie writer mode is enabled and this is how many seconds elapse each frame</summary>
		private float fixedFramerateDelta = 0;
		private double simulatedTime = 0;
		private double TimestampNow => fixedFramerateDelta == 0 ? Time.GetUnixTimeFromSystem() : simulatedTime;


		private List<List<ITile>> TileRowList {
			get {
				if (_tileRowList.Count == 0) {
					_tileRowList.Add(new List<ITile> { LoadTile(_screenTopLeft_WorldCoord) });
				//} else if (_tileRowList.Count == 1 && _tileRowList.First().Count == 0) {
				//	GD.Print("Top row was empty");
				//	_tileRowList.First().Add(LoadTile(_screenTopLeft_WorldCoord));
				} else if (_tileRowList.First().Count == 0) {
					GD.Print($"eep _tileRowList.Count {_tileRowList.Count}, {string.Join(",", _tileRowList.Select(x=>x.Count))}\r\n{new System.Diagnostics.StackTrace()}");
				}
				return _tileRowList;
			}
		}
		private List<ITile> TopTileRow {
			get { return TileRowList.First(); }
		}
		private List<ITile> BottomTileRow {
			get { return TileRowList.Last(); }
		}
		private ITile TopLeftTile {
			get {
				return TopTileRow.First();
			}
		}

		/// <summary>Called when the node enters the scene tree for the first time.</summary>
		public override void _Ready()
		{
			// log all exceptions until Godot has VS2022 support
			GD.Print("Adding exception logger");
			AppDomain.CurrentDomain.UnhandledException += (sender, e) => {
				GD.PrintErr($"UnhandledException handler: {e.ExceptionObject}");
			};
			AppDomain.CurrentDomain.FirstChanceException += (sender, e) => {
				if (e.Exception.TargetSite.DeclaringType.Assembly == Assembly.GetExecutingAssembly()) {
					GD.PrintErr($"FirstChanceException handler: {e.Exception.Message}\n{e.Exception.StackTrace}");
				} else {
					GD.PrintErr($"{e.Exception.GetType().Name} somewhere not my code: {e.Exception.TargetSite.DeclaringType.Assembly}: {e.Exception.Message}\n{e.Exception.StackTrace}");
				}
			};

			if (OS.HasFeature("movie")) {
				// MovieMaker mode is active, time everything by frames rather than real elapsed time
				fixedFramerateDelta = 1.0f / ProjectSettings.GetSetting("editor/movie_writer/fps").AsInt32();
			}

			MapGen.MapGen.Init(OS.GetCmdlineUserArgs(), GetTree().Root);

			FindAvailableTileServers();

			SetTileServer(0);
			updateRequired = true;
		}

		// Called every frame. 'delta' is the elapsed time since the previous frame.
		public override void _Process(double delta)
		{
			simulatedTime += fixedFramerateDelta;

			if (updateRequired && lastUpdate + MinimumTimeBetweenUpdates_secs < TimestampNow) {
				lastUpdate = TimestampNow;
				updateRequired = false;
				Update();
			}
			PaintNewlyGeneratedTiles();
			FadeTilesIn();
		}

		public void FindAvailableTileServers() {
			var tileServers = AppDomain.CurrentDomain.GetAssemblies()
				.SelectMany(s => s.GetTypes())
				.Where(t => !t.IsAbstract && !t.IsInterface && t.GetInterfaces().Contains(typeof(ITileServer)))
				.Where(t => !t.GetCustomAttributes(typeof(HiddenTileServerAttribute), false).Any()); // exclude any ITileServers marked [HiddenTileServer]

			_availableTileServers.Clear();
			var skippedTileServers = new List<string>();
			foreach ( var tileServerType in tileServers) {
				// only load TileServers that can be constructed with a default constructor or take only a worldseed
				if (GetTileServerConstructor(tileServerType) != null) {
					_availableTileServers.Add(tileServerType, GetTileServerName(tileServerType));
				} else {
					skippedTileServers.Add(tileServerType.Name);
				}
			}
			if (skippedTileServers.Any()) { 
				GD.PrintRich($"[color=orange]The following TileServer classes are skipped[/color] because no suitable constructors were found: {string.Join(", ", skippedTileServers)}");
			}

			// reorder the dictionary to be sorted by TileServer name
			_availableTileServers = _availableTileServers.OrderBy(kv => kv.Value).ToDictionary(kv => kv.Key, kv => kv.Value);
			TileServerList = _availableTileServers.Values.ToArray();

			GD.Print($"Available TileServers: {string.Join(", ", TileServerList)}");
		}

		/// <summary>Returns null, or a constructor which needs only a ulong worldseed, or a default constructor</summary>
		private ConstructorInfo GetTileServerConstructor(Type tileServerType) {

			// only load TileServers that can be constructed with a default constructor, or a constructor which needs only a ulong worldseed
			return tileServerType
				.GetConstructors()
				.Where(x =>	!x.GetParameters().Any(p => !p.IsOptional)                                           // the constructor has no parameters that aren't optional,
					|| (                                                                                         // or
						x.GetParameters().Count(p => !p.IsOptional) == 1 &&                                      //   there is 1 parameter that isn't optional
						x.GetParameters()[0].ParameterType == typeof(ulong) &&                                   //   and it is a ulong
						x.GetParameters()[0].Name.Contains("seed", StringComparison.InvariantCultureIgnoreCase)  //   and it contains 'seed' in its name
					)
				)
				.OrderByDescending(x => x.GetParameters().Count(p => !p.IsOptional)) // favor the constructor with the seed parameter
				.FirstOrDefault();
		}

		private ITileServer ConstructTileServer(Type tileServerType, ulong seed) {

			var constructorInfo = GetTileServerConstructor(tileServerType);

			var arguments = new object[constructorInfo.GetParameters().Count()];
			Array.Fill(arguments, Type.Missing);

			var firstParm = constructorInfo.GetParameters().FirstOrDefault();
			if (firstParm?.ParameterType == typeof(ulong) && firstParm.Name.Contains("seed", StringComparison.InvariantCultureIgnoreCase)) {
				// this constructor takes a seed parameter
				arguments[0] = seed;
			}
			return (ITileServer)constructorInfo.Invoke(arguments);
		}

		private string GetTileServerName(Type tileServerType) {

			// Ideally TileServers implementations have a [Display(Name="value")] attribute to indicate
			// what the Tile Manager GUI should call them.
			string result = tileServerType
				.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.DisplayAttribute), true).Cast<System.ComponentModel.DataAnnotations.DisplayAttribute>()
				.SingleOrDefault()?.Name;

			// If the TileServer wasn't decorated with a [Display(Name="")] attribute, then try creating
			// a GUI display name from it's Type name.
			if (string.IsNullOrEmpty(result)) {
				result = tileServerType.Name
					.Replace("TileServer", "", StringComparison.InvariantCultureIgnoreCase)
					.Replace("_", " ");

				result = string.IsNullOrWhiteSpace(result) ? tileServerType.Name : result;
			}

			return result;
		}



		public Rect2 TileCoverage {
			get {
				var size = new Vector2(
					TopTileRow.Count * TileServer.TileLength,
					-TileRowList.Count * TileServer.TileLength
				);
				return new Rect2(TopLeftTile.Pos, size);
			}
		}

		/// <summary>Returns null if worldCoord is outside the TileCoverage area</summary>
		public ITile ManagedTileAt(Vector2 worldCoord) {
		
			ITile result = null;
			var topLeft = TopLeftTile;
			int rowIndex = (int)Mathf.Floor((topLeft.Pos.Y - worldCoord.Y) / TileServer.TileLength);
			if (rowIndex >= 0 && rowIndex < TileRowList.Count) {
				var row = TileRowList[rowIndex];
				int colIndex = (int)Mathf.Floor((worldCoord.X - topLeft.Pos.X) / TileServer.TileLength);
				if (colIndex >= 0 && colIndex < row.Count) {
					result = row[colIndex];
				}
			}
			return result;
		}

		public GPositionInformation GetPositionInformation(Vector2 worldCoord) {

			var result = new GPositionInformation();
			//result.coords = $"({worldCoord.X:0.##}, {worldCoord.Y:0.##})";
			result.coords = $"[{worldCoord.X:0}, {worldCoord.Y:0}]";

			var tile = ManagedTileAt(worldCoord);
			if (tile is IDeferredTile deferredTile) { tile = deferredTile.WrappedTile; }
			if (tile != null) {
				var localPos = ((worldCoord - tile.Pos) * FlipYAxis) / tile.TileServer.Scale;
				result.short_description = tile.TileServer.Render2D.AsStringShort(tile, localPos);
				result.long_description  = tile.TileServer.Render2D.AsStringLong(tile, localPos);
			}
			return result;
		}

		public void ForceGarbageCollection() {
			// To let me see if there's a GC issue, like https://github.com/godotengine/godot/issues/36649
			GD.Print("Collecting CLR garbage...");
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
			GD.Print("Collected.");
		}

		public void SetTileServer(int tileserverIndex) {

			Type tileserverType = _availableTileServers.Keys.ToArray()[tileserverIndex];
			string name = _availableTileServers[tileserverType];

			GD.Print($"Setting tile manager to {tileserverIndex}: \"{name}\" ({tileserverType.Name})");
			try {
				TileServer = ConstructTileServer(tileserverType, WorldSeed);
			} catch(Exception ex) { 
				GD.PrintErr($"Failed to construct TileServer {name}: {ex}");
				return;
			}
			TileServerConfig = CopySettingsFromTileServer(TileServer);
		
			EmitSignal(SignalName.tileserver_changed, SelectedTileServer, tileserverIndex);
			SelectedTileServer = tileserverIndex;
		}

		private void TileServerConfig_ValueChanged(object sender, (string, string, Variant) sectionKeyValue) {
			if (sectionKeyValue.Item1 == TileServerConfigSection_Menu) {
				// find the matching boolean property on the TileServer and update its value
				PropertyInfo[] properties = TileServer.GetType().GetProperties();
				foreach (PropertyInfo prop in properties) {
					object[] attributes = prop.GetCustomAttributes(true);
					foreach (object attrib in attributes) {
						if (attrib is ExposeAsMenuItemAttribute menuAttribute && menuAttribute.Caption == sectionKeyValue.Item2) {
							prop.SetValue(TileServer, sectionKeyValue.Item3.AsBool());
							if (menuAttribute.DiscardExistingTiles) { RerenderAllTiles(); }
							return;
						}
					}
				}

			} else if (sectionKeyValue.Item1 == TileServerConfigSection_Console) {
				// find the matching property on the TileServer and update its value
				PropertyInfo[] properties = TileServer.GetType().GetProperties();
				foreach (PropertyInfo prop in properties) {
					object[] attributes = prop.GetCustomAttributes(true);
					foreach (object attrib in attributes) {
						if (attrib is ExposeAsConsoleCommandAttribute consoleAttribute && consoleAttribute.CommandAndDescription() == sectionKeyValue.Item2) {

							var value = sectionKeyValue.Item3.Obj;
							// handle any conversions that require explicit casts
							if (prop.PropertyType == typeof(float) && value is double) { value = Convert.ToSingle(value); }
							if (prop.PropertyType == typeof(int)   && value is double) { value = Convert.ToInt32(value); }
							if (prop.PropertyType == typeof(int)   && value is long)   { value = Convert.ToInt32(value); }

							prop.SetValue(TileServer, value);
							if (consoleAttribute.DiscardExistingTiles) { RerenderAllTiles(); }
							return;
						}
					}
				}
			}
		}

		/// <summary>
		/// Uses reflection to populate an ObservableConfigFile with any configurable settings
		/// the TileServer is exposing.
		/// </summary>
		private ObservableConfigFile CopySettingsFromTileServer(ITileServer tileServer) {

			ObservableConfigFile result = new ObservableConfigFile();

			PropertyInfo[] properties = tileServer.GetType().GetProperties();
			foreach (PropertyInfo prop in properties) {
				object[] attributes = prop.GetCustomAttributes(true);
				foreach (object attrib in attributes) {

					if (attrib is ExposeAsMenuItemAttribute menuAttribute) {
						if (prop.PropertyType != typeof(bool)) {
							throw new InvalidOperationException("Checkbox menu-items can only expose boolean properties. [ExposeAsMenuItem] must only be used on boolean properties");
						}
						result.SetValue(TileServerConfigSection_Menu, menuAttribute.Caption, (bool)prop.GetValue(tileServer));

					} else if (attrib is ExposeAsConsoleCommandAttribute consoleAttribute) {
						Variant value;
						if (prop.PropertyType == typeof(bool)) {
							value = (bool)prop.GetValue(tileServer);
						} else if (prop.PropertyType == typeof(int)) {
							value = (int)prop.GetValue(tileServer);
						} else if (prop.PropertyType == typeof(long)) {
							value = (long)prop.GetValue(tileServer);
						} else if (prop.PropertyType == typeof(float)) {
							value = (float)prop.GetValue(tileServer);
						} else if (prop.PropertyType == typeof(double)) {
							value = (double)prop.GetValue(tileServer);
						} else if (prop.PropertyType == typeof(string)) {
							value = (string)prop.GetValue(tileServer);
						} else if (prop.PropertyType == typeof(Vector2)) {
							value = (Vector2)prop.GetValue(tileServer);
						} else if (prop.PropertyType == typeof(Vector2I)) {
							value = (Vector2I)prop.GetValue(tileServer);
						} else if (prop.PropertyType == typeof(Rect2)) {
							value = (Rect2)prop.GetValue(tileServer);
						} else if (prop.PropertyType == typeof(Godot.Color)) {
							value = (Godot.Color)prop.GetValue(tileServer);
						} else if (prop.PropertyType == typeof(Vector3)) {
							value = (Vector3)prop.GetValue(tileServer);
						} else {
							throw new NotImplementedException($"[ExposeAsConsoleCommand] doesn't yet know how to handle a property of type {prop.PropertyType}");
						}
						result.SetValue(TileServerConfigSection_Console, consoleAttribute.CommandAndDescription(), value);
					}

				}
			}

			return result;
		}


		/// <summary>
		/// Loads the tile from the tile sever and adds it to the screen
		/// </summary>
		private ITile LoadTile(Vector2 worldCoord) {

			try {
				//GD.Print($"LoadTile() from {TileServer.GetType().Name}");
				Vector2I pos = TileServer.TileContainingWorldCoord(worldCoord);
				ITile tile = new DeferredTile<TileManager>(pos, TileServer, OnTileGenerated, this, GenerateTilesSynchronously);

				tilesLoaded++;
				tilesTotal++;
				return tile;
			} catch (Exception ex) {
				GD.Print($"LoadTile exception {ex}");
				throw;
			}
		}

		private void OnTileGenerated(IDeferredTile tile, TileManager tileManager) {
			// this call is running in a worker thread, so put the tiles into a list so
			// they can be actioned/drawn in the same _process thread.

			// Render the texture while we're still in the worker thread
			Texture2D texture = null;
			if (!tile.Abort) {
				texture = tile.TileServer.Render2D.AsTexture(tile.WrappedTile);
				tile.FinishTime_unix = TimestampNow;
			}

			lock (tileManager.newlyGeneratedTiles) {
				tileManager.newlyGeneratedTiles.Add((tile, texture));
			}
		}

		private void PaintNewlyGeneratedTiles() {

			List<(IDeferredTile, Texture2D)> tiles;
			lock (newlyGeneratedTiles) {
				tiles = new List<(IDeferredTile, Texture2D)>(newlyGeneratedTiles.Where(x => !x.Item1.Abort));
				newlyGeneratedTiles.Clear();
			}

			var canvas = ParallaxLayerNode;
			if (canvas == null) { GD.PrintErr("TileManager doesn't have a ParallaxLayer assigned"); }

			foreach (var tile in tiles) {
				//GD.Print("PaintNewlyGeneratedTile");
				var sprite = new Sprite2D();
				sprites[tile.Item1] = sprite;

				sprite.Texture = tile.Item2;
				sprite.Offset = tile.Item1.Pos * FlipYAxis;
				sprite.Centered = false;
				sprite.Scale = new Vector2(tile.Item1.TileServer.Scale, tile.Item1.TileServer.Scale);
				//sprite.GlobalScale = Vector2.One;
				sprite.Offset = sprite.Offset / tile.Item1.TileServer.Scale;

				sprite.Modulate = new Color(1, 1, 1, 0);
				tilesToFadeIn.Add(tile.Item1);

				canvas.AddChild(sprite);
			}
		}

		private void FadeTilesIn() {
			if (!tilesToFadeIn.Any()) { return; }

			double secs = TimestampNow;

			//GD.Print($"Fading in {tilesToFadeIn.Count} tiles");
			for (int i = tilesToFadeIn.Count - 1; i >= 0; i--) {
				var tile = tilesToFadeIn[i] as IDeferredTile;
				if (tile?.Abort == false && sprites.TryGetValue(tile, out Sprite2D sprite)) {

					float alpha = Mathf.Min(1, (float)(secs - tile.FinishTime_unix) * 1000 / TileFadeInTime_ms);
					sprite.Modulate = new Color(1, 1, 1, alpha);

					if (alpha >= 1.0f) {
						// tile has finished fading in
						tilesToFadeIn.RemoveAt(i);

						// Now we've finished using modulate for fading we can use it for a checkerboard
						bool isOdd = (((tile.Pos.X + tile.Pos.Y) / tile.TileServer.TileLength) & 1) == 1;
						if (isOdd) { sprite.AddToGroup("isOdd"); }
					}
				} else {
					tilesToFadeIn.RemoveAt(i);
				}
			}
		}


		/// <summary>
		/// Removes the tile from the screen
		/// </summary>
		private void UnloadTile(ITile tile) {

			if (tile is IDeferredTile deferredTile) {
				deferredTile.Abort = true;
			}

			if (sprites.TryGetValue(tile, out Sprite2D sprite)) { 
				var canvas = ParallaxLayerNode;
				canvas.RemoveChild(sprite);
				sprite.QueueFree();
				sprites.Remove(tile);
			}
			tilesUnloaded++;
			tilesTotal--;
		}

		private void AddOrRemoveColumns(bool leftSide, int amount) {

			if (amount > 0) {
				// add columns
				for (int i = 0; i < amount; i++) {
					int insertionIndex = leftSide ? 0 : TopTileRow.Count;
					//GD.Print($"insertionIndex {insertionIndex}, TopTileRow.Count {TopTileRow.Count}");
					ITile neighbour = leftSide ? TopTileRow.First() : TopTileRow.Last();
					float x = leftSide
						? neighbour.Pos.X - TileServer.TileLength
						: neighbour.Pos.X + TileServer.TileLength;
					float y = neighbour.Pos.Y;

					foreach (var row in TileRowList) {
						row.Insert(insertionIndex, LoadTile(new Vector2(x, y)));
						y -= TileServer.TileLength;
					}
				}

			} else if (amount < 0) {
				// remove columns
				//GD.Print($"Remove {-amount} columns");
				var rowList = TileRowList; // take a copy to avoid all the empty checks/fixes this getter performs
				var firstRow = rowList.First();
				for (int i = 0; i < -amount && firstRow.Count > 0; i++) {
					int deletionIndex = leftSide ? 0 : firstRow.Count - 1;
					foreach (var row in rowList) {
						var deletedTile = row[deletionIndex];
						row.RemoveAt(deletionIndex);
						UnloadTile(deletedTile);
					}
				}
				// Prevent empty rows from existing, so code may assume rows have at least one column/Tile
				if(!firstRow.Any()) { rowList.Clear(); }
			}
		}

		private void AddOrRemoveRows(bool topSide, int amount) {

			if (amount > 0) {
				// add rows
				for (int i = 0; i < amount; i++) {
					int insertionIndex = topSide ? 0 : TileRowList.Count;
					ITile neighbour = topSide ? TopTileRow.First() : BottomTileRow.First();
					float x = neighbour.Pos.X;
					float y = topSide
						? neighbour.Pos.Y + TileServer.TileLength
						: neighbour.Pos.Y - TileServer.TileLength;

					var newRow = new List<ITile>();
					for (int col = 0; col < TopTileRow.Count; col ++) {
						newRow.Add(LoadTile(new Vector2(x + col * TileServer.TileLength, y)));
					}
					TileRowList.Insert(insertionIndex, newRow);
				}

			} else if (amount < 0) {
				// remove rows
				for (int i = 0; i < -amount && TileRowList.Count > 0; i++) {
					int deletionIndex = topSide ? 0 : TileRowList.Count - 1;
					var deletedRow = TileRowList[deletionIndex];
					TileRowList.RemoveAt(deletionIndex);
					foreach (var tile in deletedRow) {
						UnloadTile(tile);
					}				
				}
			}
		}

		public void Update() {

			Rect2 tiledArea = TileCoverage;
			Rect2 screenArea = new Rect2(ScreenTopLeft_WorldCoord, new Vector2(ScreenSize.X, -ScreenSize.Y) / ScreenScale);

			// Rect2.Intersects() fails if Rect.Size.Y is negative, so make use of Rect2.Abs()
			if (!screenArea.Abs().Intersects(tiledArea.Abs(), true)) {
				// Coords have shifted somewhere so far offscreen that none of the current tiles are relevant.
				// Unload all the tiles first, so the location of the tilemap can shift, to prevent a massive
				// span of tiles being generated that stretches between the old tiles and the new coordinates.
				AddOrRemoveRows(true, -TileRowList.Count); // Remove all the rows, rather than columns, as code assumes rows have at least one column
				tiledArea = TileCoverage;
			}

			int tilesNeeded_Top = (int)Math.Ceiling((screenArea.Position.Y - tiledArea.Position.Y) / TileServer.TileLength);
			if (tilesNeeded_Top < 0) { tilesNeeded_Top = (int)Math.Min(0, tilesNeeded_Top + OffscreenColumnsAndRows.Y); }

			int tilesNeeded_Bottom = (int)Math.Ceiling((tiledArea.End.Y - screenArea.End.Y) / TileServer.TileLength);
			if (tilesNeeded_Bottom < 0) { tilesNeeded_Bottom = (int)Math.Min(0, tilesNeeded_Bottom + OffscreenColumnsAndRows.Y); }

			int tilesNeeded_Left = (int)Math.Ceiling((tiledArea.Position.X - screenArea.Position.X) / TileServer.TileLength);
			if (tilesNeeded_Left < 0) { tilesNeeded_Left = (int)Math.Min(0, tilesNeeded_Left + OffscreenColumnsAndRows.Y); }

			int tilesNeeded_Right = (int)Math.Ceiling((screenArea.End.X - tiledArea.End.X) / TileServer.TileLength);
			if (tilesNeeded_Right < 0) { tilesNeeded_Right = (int)Math.Min(0, tilesNeeded_Right + OffscreenColumnsAndRows.Y); }

			tilesLoaded = 0;
			tilesUnloaded = 0;

			AddOrRemoveRows(true,    tilesNeeded_Top);
			AddOrRemoveRows(false, tilesNeeded_Bottom);
			AddOrRemoveColumns(true,  tilesNeeded_Left);
			AddOrRemoveColumns(false, tilesNeeded_Right);
		}
	}

}
