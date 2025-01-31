# Copyright 2023 Treer (https://github.com/Treer)
# License: MIT, see LICENSE.txt for rights granted

extends CanvasLayer

signal home_button_pressed
signal tileserver_change_requested(tileServersEnum)
signal garbage_collection_requested
signal console_requested
signal screenshot_requested

var pos_coords_text: String : set = set_pos_coords_text
var pos_desc_text:   String : set = set_pos_desc_text
#var pos_desc_short_text: String setget set_pos_desc_short_text
#var pos_desc_long_text: String setget set_pos_desc_long_text

@onready var pause := $Pause
@onready var pause_button := find_child("PauseButton", true, true)
@onready var resume_option := $Pause/VBoxOptions/Resume
#@onready var label := $PressESCToOpenConsole

enum MenubarId_file {FORCE_GC = 1001, SAVE_SCREENSHOT, SHOW_CONSOLE, EXIT}
enum MenubarId_navigate {HOME = 2001}
enum MenubarId_view {TILESERVER1 = 3001, CHECKERBOARD = 3100, PIXELSCALE}
enum MenubarId_generator {SETTING1 = 4001}

const CONFIGFILE_SECTION_MENU = "Menu"
var settings_menu_id_to_key: Dictionary = {}
var tileserver_config
var tilesever_names = []

func _ready():
	print_rich("[color=purple]pause-layer.gd:_ready called[/color]")	
#	if DisplayServer.is_touchscreen_available():
#		label.visible = false
#	else:
#		# to hide the pause_button on desktop: un-comment the next line
#		# pause_button.hide()
#		pass

	EventBus.connect("map_zoom_changed", _on_map_zoom_changed)
	EventBus.connect("console_open_toggled", _on_console_open_toggled)


	# Add the File menu
	var menuFile = $"VBoxContainer/PanelContainer/MenuBar/File Menu".get_popup()
	# Things that didn't prevent the fake mouse cursor from drawing under the popups
	#menuFile.always_on_top = false
	#menuFile.exclusive = false
	#menuFile.gui_embed_subwindows = true
	#menuFile.set_as_toplevel(false)
	menuFile.connect("id_pressed", _on_menuitem_pressed)
	menuFile.add_item("Save screenshot", MenubarId_file.SAVE_SCREENSHOT)
	menuFile.add_item("Show console", MenubarId_file.SHOW_CONSOLE)
	menuFile.add_item("Force garbage collection", MenubarId_file.FORCE_GC)
	menuFile.add_separator()
	menuFile.add_item("Quit", MenubarId_file.EXIT)

	# Add the Navigation menu
	var menuNavigate = $"VBoxContainer/PanelContainer/MenuBar/Navigate Menu".get_popup()
	menuNavigate.connect("id_pressed", _on_menuitem_pressed)
	menuNavigate.add_item("Home", MenubarId_navigate.HOME)

	# Add the View menu
	var menuView = $"VBoxContainer/PanelContainer/MenuBar/View Menu".get_popup()
	menuView.connect("id_pressed", _on_menuitem_pressed)
	# the rest of the view menu will be added when set_tilesevers() is called
	set_tilesevers([], -1)
	
	# ensure PixelScaleLabel visibility is false, matching its menuitem checkbox
	find_child("PixelScaleLabel", true, false).visible = false


func _unhandled_input(event):
	if event.is_action_pressed("pause"):
		if get_tree().paused:
			resume()
		else:
			pause_game()
		#get_tree().set_input_as_handled()


func set_tilesevers(list, selectedIndex):
	tilesever_names = list.duplicate()
	
	var menuView = $"VBoxContainer/PanelContainer/MenuBar/View Menu".get_popup()
	menuView.clear()
	var tilesever_names_index = 0
	for tileEnumName in tilesever_names:
		menuView.add_check_item(tileEnumName.replace("_", " "), MenubarId_view.TILESERVER1 + tilesever_names_index)
		tilesever_names_index += 1

	if selectedIndex >= 0:
		var idx = menuView.get_item_index(MenubarId_view.TILESERVER1 + selectedIndex)
		menuView.set_item_checked(idx, true)	

	menuView.add_separator()
	menuView.add_check_item("Checkerboard", MenubarId_view.CHECKERBOARD)
	menuView.add_check_item("Pixel scale", MenubarId_view.PIXELSCALE)

func resume():
	print("Resuming")
	get_tree().paused = false
	pause.hide()


func pause_game():
	resume_option.grab_focus()
	get_tree().paused = true
	pause.show()


func _on_Resume_pressed():
	print("Resume pressed")
	resume()

func set_pos_coords_text(text: String):
	find_child("CoordsLabel",true).text =  "[font top_spacing=2 bottom_spacing=2][bgcolor=#00000088] %s " % text
#func set_pos_desc_short_text(text: String): find_child("DescShortLabel",true).text = text
#func set_pos_desc_long_text(text: String): 	find_child("DescLongLabel",true).text = text

func set_pos_desc_text(text: String):
	var label = find_child("LongDescLabel",true)
	label.visible = !text.is_empty();
	label.text = "[font top_spacing=2 bottom_spacing=2][bgcolor=#00000088] %s " % text

func _on_map_zoom_changed(mapScale_pixelsPerMeter: float):
	find_child("MapLegendScale", true, false).MapScale = mapScale_pixelsPerMeter
	find_child("PixelScaleLabel", true, false).text = "Pixels per meter: %0.3f" % mapScale_pixelsPerMeter

func _on_console_open_toggled(is_console_open: bool):
	visible = !is_console_open

func _on_Main_Menu_pressed():
	# TODO: REPAIR
	# Since crystal-bit/godot-game-template doesn't support Godot 4 yet, this will fail
	push_error("Not supported yet")
	print_rich("[color=red]Not supported yet[/color]")
	#Game.change_scene("res://scenes/menu/menu.tscn", {
	#	'show_progress_bar': false
	#})

func _on_PauseButton_pressed():
	pause_game()

func _on_HomeButton_pressed():
	emit_signal("home_button_pressed")

func _on_checkerboardChanged(showCheckers):
	var oddSprites = get_tree().get_nodes_in_group("isOdd")
	if showCheckers:
		for sprite in oddSprites: sprite.modulate = Color.DARK_GRAY
	else:
		for sprite in oddSprites: sprite.modulate = Color.WHITE

func _on_pixelScaleChanged(showPixelScale):
	find_child("PixelScaleLabel", true, false).visible = showPixelScale

func _on_menuitem_pressed(itemId: int):

	if itemId >= MenubarId_view.TILESERVER1 && itemId < MenubarId_view.TILESERVER1 + tilesever_names.size():
		# Set the checkbox to the selected TileServer menuitem
		var menuView = find_child("View Menu", true, true).get_popup()
		var tileServersEnum = itemId - MenubarId_view.TILESERVER1
		
		var tileserver_index = 0
		for tileEnumName in tilesever_names:
			var menuItemId = MenubarId_view.TILESERVER1 + tileserver_index
			var idx = menuView.get_item_index(menuItemId)
			menuView.set_item_checked(idx, menuItemId == itemId)	
			tileserver_index += 1	

		emit_signal(tileserver_change_requested.get_name(), tileServersEnum)

	elif settings_menu_id_to_key.has(itemId):
		var settingsMenu: MenuButton = $"VBoxContainer/PanelContainer/MenuBar/Generator Menu (TileServer Settings)"
		var settingsPopup = settingsMenu.get_popup()
		var idx = settingsPopup.get_item_index(itemId)
		var checked = !settingsPopup.is_item_checked(idx)
		settingsPopup.set_item_checked(idx, checked)
		tileserver_config.SetValue(CONFIGFILE_SECTION_MENU, settings_menu_id_to_key[itemId], checked)
		
	else:	
		match itemId:
			MenubarId_file.FORCE_GC:
				emit_signal(garbage_collection_requested.get_name())
			MenubarId_file.SAVE_SCREENSHOT:
				emit_signal(screenshot_requested.get_name())
			MenubarId_file.SHOW_CONSOLE:
				emit_signal(console_requested.get_name())
			MenubarId_file.EXIT:
				print("Asked to exit")
				get_tree().quit()
			MenubarId_navigate.HOME:
				_on_HomeButton_pressed()
			MenubarId_view.CHECKERBOARD, MenubarId_view.PIXELSCALE:
				var menuView = find_child("View Menu", true, true).get_popup()
				var idx = menuView.get_item_index(itemId)
				var checked = !menuView.is_item_checked(idx)	
				menuView.set_item_checked(idx, checked)	
				match itemId:
					MenubarId_view.CHECKERBOARD: _on_checkerboardChanged(checked)
					MenubarId_view.PIXELSCALE:   _on_pixelScaleChanged(checked)

func expose_tileserver_config(configFile):
	var settingsMenu: MenuButton = $"VBoxContainer/PanelContainer/MenuBar/Generator Menu (TileServer Settings)"
	var settingsPopup = settingsMenu.get_popup()	
	if !settingsPopup.is_connected(settingsPopup.id_pressed.get_name(), _on_menuitem_pressed):
		settingsPopup.connect(settingsPopup.id_pressed.get_name(), _on_menuitem_pressed)
	
	var showMenu = configFile.HasSection(CONFIGFILE_SECTION_MENU)
	settingsMenu.visible = showMenu
	settingsPopup.clear()
	settings_menu_id_to_key.clear()
	tileserver_config = configFile # store it so we can set values in it if the checkbox menu-items are clicked
	
	if showMenu:
		var settingId = MenubarId_generator.SETTING1 + 0
		for settingCaption in configFile.GetSectionKeys(CONFIGFILE_SECTION_MENU):
			settings_menu_id_to_key[settingId] = settingCaption
			settingsPopup.add_check_item(settingCaption, settingId)
			var settingIndex = settingsPopup.get_item_index(settingId)
			settingsPopup.set_item_checked(settingIndex, configFile.GetValue(CONFIGFILE_SECTION_MENU, settingCaption, false))
			settingId += 1	
