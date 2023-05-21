# Copyright 2023 Treer (https://github.com/Treer)
# License: MIT, see LICENSE.txt for rights granted

extends Node2D

@export var coords: Vector2 = Vector2(0,0)  # gets location at the center of the screen
@export var zoom_scale: float : get = get_zoom_scale # scale map is being displayed at, 0.5 means twice as much of the map is visible
@export var linear_zoom: bool = false # Makes the zooming less responsive, but may be preferable if recording a video
@export var linear_zoom_speed: float = 0.5
@export_file("*.ini") var settings_file_path = "user://settings.ini"

var velocity: Vector2
var velocity_adj: float = 1 #Leave this at 1 for normal use, but use the console to lower it if you need to slow down scrolling and zooming for movie maker
var velocity_falloff_adj: float = 1
var zoom_speed_adj: float = 1
var zoom: float = 0.0 # a value between -1 and 1, with 0 meaning 1:1 pixel scale, and negatives meaning zoom out (more of the map is visible)	
var fixed_framerate_delta: float = 0 # A non-zero value means movie writer mode is enabled and process() should use this delta rather than the one passed to it
var settings: ConfigFile = null # may be null if settings file cannot be created
const scrollWheel_zoom_speed = 0.1
const max_zoom_in_factor = 4
const max_zoom_out_factor = 8

# +Y is up in Map coords, but down in CanvasLayer world coords.
# Multiply by this to switch between the two.
const FlipYAxis = Vector2(1, -1);


# `pre_start()` is called when a scene is loaded.
# Use this function to receive params from `Game.change_scene(params)`.
func pre_start(params):
	print("\nmapview.gd:pre_start() called with params = ")
	for key in params:
		var val = params[key]
		printt("", key, val)
	set_process(false)


	if OS.has_feature("movie"):
		# MovieMaker mode is active
		fixed_framerate_delta = 1.0 / ProjectSettings.get_setting("editor/movie_writer/fps")
		zoom_speed_adj = 0.15
		velocity_adj = 0.3
		velocity_falloff_adj = 0.2
		#DisplayServer.mouse_set_mode(DisplayServer.MOUSE_MODE_VISIBLE)
		#Input.set_custom_mouse_cursor(load("res://assets/home_button.png"))

	# hook the window-size change event
	# CenterContainers don't seem to work on a CanvasLayer.offset, so if there's a proper way
	# to keep it ceneterd, I can't find it
	get_viewport().connect(get_viewport().size_changed.get_name(), on_viewport_size_changed)
	on_viewport_size_changed()
	
	
	# Hook other UI actions
	# Doing it this way makes it harder to see from the tree editor, but easier to search for 
	# and less likely to silently become broken
	$SaveFileDialog.connect($SaveFileDialog.file_selected.get_name(), on_saveFileDialog_ok)
	$TileManager.connect($TileManager.tileserver_changed.get_name(),  on_tileserver_changed)
	var guiLayer = $ParallaxBackground/GuiLayer
	guiLayer.set_tilesevers($TileManager.TileServerList, $TileManager.SelectedTileServer)
	guiLayer.connect(guiLayer.tileserver_change_requested.get_name(),    on_tileserver_change_requested)
	guiLayer.connect(guiLayer.garbage_collection_requested.get_name(),   on_garbage_collection_requested)
	guiLayer.connect(guiLayer.home_button_pressed.get_name(),            on_teleported_home)
	guiLayer.connect(guiLayer.console_requested.get_name(),              on_console_requested)
	guiLayer.connect(guiLayer.screenshot_requested.get_name(),           on_screenshot_requested)	
	
	var consoleInterface = $ConsoleInterface
	consoleInterface.connect(consoleInterface.seed_changed.get_name(), on_seed_changed)
	consoleInterface.connect(consoleInterface.teleported.get_name(),   on_teleported)
	consoleInterface.connect(
		consoleInterface.synchronous_generation_changed.get_name(), func (generate_synchronously):
			consoleInterface.write_line("Synchronous generation was %s, now set %s" % [$TileManager.GenerateTilesSynchronously, generate_synchronously])
			$TileManager.GenerateTilesSynchronously = generate_synchronously
	)
	consoleInterface.connect(
		consoleInterface.linear_zoom_changed.get_name(), func (use_linear_zoom):
			consoleInterface.write_line("Linear zoom was %s, now set %s" % [linear_zoom, use_linear_zoom])
			linear_zoom = use_linear_zoom
	)
	consoleInterface.connect(
		consoleInterface.linear_zoom_speed_changed.get_name(), func (speed):
			consoleInterface.write_line("Linear zoom speed was %s, now set to %s" % [linear_zoom_speed, speed])
			linear_zoom_speed = speed
	)
	consoleInterface.connect(
		consoleInterface.lerp_zoom_speed_changed.get_name(), func (scale):
			consoleInterface.write_line("Zoom speed was %s, now set to %s" % [zoom_speed_adj, scale])
			zoom_speed_adj = scale
	)
	consoleInterface.connect(
		consoleInterface.velocity_changed.get_name(), func (velocity_scale):
			consoleInterface.write_line("Velocity was %s, now set to %s" % [velocity_adj, velocity_scale])
			velocity_adj = velocity_scale
	)

	consoleInterface.connect(
		consoleInterface.screensize_changed.get_name(), func (width, height):
			width  = clamp(width,  10, 4096) # prevent typos from doing something stupid
			height = clamp(height, 10, 4096)
			consoleInterface.write_line("Set window size to %d, %d" % [width, height])
			DisplayServer.window_set_size(Vector2(width, height))
	)
	
# `start()` is called when the graphic transition ends.
func start():
	print_rich("[color=purple]mapview.gd:start() called[/color]")
	
	settings = ConfigFile.new();
	var error = settings.load(settings_file_path) 
	if error != OK and error != Error.ERR_FILE_NOT_FOUND: # if file isn't found then we just created one
		push_warning("Failed to open \"%s\", settings not be persisted: load() returned Error %d" % [settings_file_path, error])
		settings = null
	
	#var active_scene: Node = Game.get_active_scene()
	#print("\nCurrent active scene is: ", active_scene.name, " (", active_scene.filename, ")")
	EventBus.emit_signal(EventBus.map_zoom_changed.get_name(), self.zoom_scale)
	set_process(true)
	
	# Fake a tileserver_changed event to update the menu and console with the current TileServer's available setting
	on_tileserver_changed(-1, $TileManager.SelectedTileServer)
	set_world_seed($TileManager.WorldSeed) # just to update the window title

func _ready():
	# TODO: REMOVE
	# Since crystal-bit/godot-game-template doesn't support Godot 4 yet, duct-tape it
	print_rich("[color=purple]mapview.gd:_ready called[/color]")
	pre_start({})
	start()

		
func _process(delta):
	if fixed_framerate_delta > 0:
		delta = fixed_framerate_delta # movie writer is running, use frame-based time rather than time-based time
		
	var parallaxBackgroundNode: Node = $ParallaxBackground
	parallaxBackgroundNode.scroll_offset += velocity * velocity_adj / self.zoom_scale
	coords = parallaxBackgroundNode.scroll_offset * Vector2(-1, 1) # Don't need to flip the Y because it's already flipped once since +Y is up in Map coords, but down in CanvasLayer world coords.
	$TileManager.ScreenTopLeft_WorldCoord = viewportCoordsToMap(Vector2.ZERO)
	
	var currentScale = parallaxBackgroundNode.scale.x
	var newScale
	if linear_zoom:
		var change_direction = sign(self.zoom_scale - currentScale)
		var change_amount = change_direction * min(abs(self.zoom_scale - currentScale), currentScale * linear_zoom_speed * delta)
		newScale = currentScale + change_amount
	else:
		newScale = lerp(currentScale, self.zoom_scale, 0.3 * zoom_speed_adj)
	
	# apply the new Scale
	parallaxBackgroundNode.scale.x = newScale
	parallaxBackgroundNode.scale.y = parallaxBackgroundNode.scale.x

	if currentScale != newScale:
		EventBus.emit_signal(EventBus.map_zoom_changed.get_name(), newScale)
		$TileManager.ScreenScale = newScale


	if Input.is_mouse_button_pressed (MOUSE_BUTTON_LEFT):
		velocity = Vector2.ZERO
	else:
		velocity *= (1 - 0.1 * velocity_falloff_adj) # normally 0.9


func _input(event):
	if $ConsoleInterface.is_open():
		return # The Console has focus. It doesn't block these input events but does use mousewheel, scrollbars, arrow keys etc.

	if event is InputEventMouseMotion:
		if event.button_mask ==  MOUSE_BUTTON_MASK_LEFT:
			velocity = event.relative.normalized() * min(60, event.relative.length())
		display_position_information(viewportCoordsToMap(event.position))
		
	if event is InputEventMouseButton:
		if event.button_index == MOUSE_BUTTON_WHEEL_UP:
			zoom = min(1.0, zoom + scrollWheel_zoom_speed)
		elif event.button_index == MOUSE_BUTTON_WHEEL_DOWN:
			zoom = max(-1.0, zoom - scrollWheel_zoom_speed)

	var scrollFunction = scroll_by_tile_size
	if event is InputEventFromWindow and event.is_command_or_control_pressed():		
		scrollFunction = scroll_by_window_size # have Ctrl+arrowkey as another way to navigate a whole page at a time
			
	if event.is_action_released("ui_up"):        scrollFunction.call(Vector2.UP)
	if event.is_action_released("ui_down"):      scrollFunction.call(Vector2.DOWN)
	if event.is_action_released("ui_left"):      scrollFunction.call(Vector2.LEFT)
	if event.is_action_released("ui_right"):     scrollFunction.call(Vector2.RIGHT)
	if event.is_action_released("ui_page_up"):   scroll_by_window_size(Vector2.UP)
	if event.is_action_released("ui_page_down"): scroll_by_window_size(Vector2.DOWN)
	if event.is_action_released("ui_home"):      scroll_by_window_size(Vector2.LEFT)
	if event.is_action_released("ui_end"):       scroll_by_window_size(Vector2.RIGHT)	
			
func viewportCoordsToMap(screenCoords: Vector2) -> Vector2:
	var world = $ParallaxBackground/ParallaxLayer.get_viewport_transform().affine_inverse() * screenCoords - $ParallaxBackground.scroll_offset 
	return world * FlipYAxis # +Y is up in Map coords, but down in CanvasLayer world coords 
	#Original calc, which is wrong except for at 0,0
	#return ($ParallaxBackground.offset + screenCoords) / self.zoom_scale * Vector2(-1, 1) - $ParallaxBackground.scroll_offset
	
func get_zoom_scale() -> float:
	var zoomed_scale
	if zoom < 0.0:
		zoomed_scale = 1 / (1 - zoom * (max_zoom_out_factor - 1))
	else:
		zoomed_scale = 1 + zoom * (max_zoom_in_factor - 1)
	# increase the zoom amount by the scale of the tiles, so we keep the same max and min
	# number of tiles onscreen regardless of how many kms a tile actually represents
	var tile_scale = $TileManager.TileScale()
	return zoomed_scale / tile_scale

		
func set_world_seed(world_seed: int):
	$TileManager.WorldSeed = world_seed	
	var build_string = ""
	if OS.has_feature("debug"): build_string = " (DEBUG)"
	var em_dash = char(8212)
	var title = "%s%s %s Seed %s" % [ProjectSettings.get_setting("application/config/name"), build_string, em_dash, world_seed]	
	DisplayServer.window_set_title(title)
	reload_all_tiles()
		
## Allow scrolling by an exact screensize amount, to allow image stitching etc.
func scroll_by_window_size(direction: Vector2):
	$ParallaxBackground.scroll_offset += $ParallaxBackground/ParallaxLayer.get_viewport_rect().size / $ParallaxBackground.scale * direction * -1

func scroll_by_tile_size(direction: Vector2):
	$ParallaxBackground.scroll_offset += $TileManager.TileLength() * direction * -1
		
func display_position_information(map_coords_pos: Vector2):
	var posInfo = $TileManager.GetPositionInformation(map_coords_pos)	
	$ParallaxBackground/GuiLayer.pos_coords_text = "%s %s" % [posInfo.long_description, posInfo.coords]
	#$ParallaxBackground/GuiLayer.pos_desc_short_text = posInfo.short_description
	#$ParallaxBackground/GuiLayer.pos_desc_long_text = posInfo.long_description

func reload_all_tiles():
	$TileManager.SetTileServer($TileManager.SelectedTileServer)

func on_viewport_size_changed():
	# CenterContainers don't seem to work on a CanvasLayer.offset, so if there's a proper way
	# to keep it ceneterd, I can't find it
	$ParallaxBackground.offset = get_viewport().size / 2
	$TileManager.ScreenSize = get_viewport().size
	print("Size changed ", get_viewport().size)

# newServer is an index of TileManager.TileServersList[]
func on_tileserver_change_requested(newSever: int):
	$TileManager.SetTileServer(newSever)

# oldServer & newServer are indexes of TileManager.TileServersList[]
func on_tileserver_changed(_oldServer: int, _newSever: int):
	$ParallaxBackground/GuiLayer.expose_tileserver_config($TileManager.TileServerConfig)
	$ConsoleInterface.expose_tileserver_config($TileManager.TileServerConfig)
	# The tile scale might have changed
	EventBus.emit_signal(EventBus.map_zoom_changed.get_name(), self.zoom_scale)	
	
func on_garbage_collection_requested():
	$TileManager.ForceGarbageCollection()

func on_console_requested():
	$ConsoleInterface.show_console()

func on_teleported_home():
	on_teleported(Vector2.ZERO)

func on_teleported(location: Vector2):
	$ConsoleInterface.write_line("Teleported from (%d, %d) to (%d, %d)" % [coords.x, coords.y, location.x, location.y])
	$ParallaxBackground.scroll_offset = location * Vector2(-1, 1)

func on_seed_changed(world_seed: int):
	$ConsoleInterface.write_line("Setting seed to %d (was %d)" % [world_seed, $TileManager.WorldSeed])
	set_world_seed(world_seed)

func on_screenshot_requested():
	if settings != null:
		var suggested_path = settings.get_value("Screenshots", "default_path", null)
		if suggested_path != null:
			if !suggested_path.ends_with("/"): suggested_path += "/"
			$SaveFileDialog.current_path = suggested_path
	
	# Get a filename-suitable description of the Tile source to append to the suggested filename
	var suffix = $TileManager.DiagnosticFilenameSuffix()
	var illegalCharsRegex = RegEx.new()
	illegalCharsRegex.compile("[:/\\?*\"|%<>]") # The chars that is_valid_filename() will fail
	suffix = illegalCharsRegex.sub(suffix, "", true)	
	if suffix.length() > 0: suffix = "_%s" % [suffix]	
	var datetime: Dictionary = Time.get_datetime_dict_from_system()
	var suggested_file_name = "seed%s_%04d-%02d-%02d_%02d.%02d.%02d_(%d,%d)%s.png" % [$TileManager.WorldSeed, datetime.year, datetime.month, datetime.day, datetime.hour, datetime.minute, datetime.second, coords.x, coords.y, suffix]
	$SaveFileDialog.current_file = suggested_file_name	
	$SaveFileDialog.popup()
	
func on_saveFileDialog_ok(file_path):	
	var gui_visible = $ParallaxBackground/GuiLayer.visible
	$ParallaxBackground/GuiLayer.visible = false
	await RenderingServer.frame_post_draw
	var screenshot: Image = $ParallaxBackground/ParallaxLayer.get_viewport().get_texture().get_image()
	$ParallaxBackground/GuiLayer.visible = gui_visible
	screenshot.save_png(file_path)
	if settings != null:
		settings.set_value("Screenshots", "default_path", file_path.get_base_dir())
		settings.save(settings_file_path)
	
	
	
