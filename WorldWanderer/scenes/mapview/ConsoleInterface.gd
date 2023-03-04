# Copyright 2023 Treer (https://github.com/Treer)
# License: MIT, see LICENSE.txt for rights granted

# Abstraction layer between Map Viewer and the command console addon being use
# (Provider pattern-ish)
extends Node

signal teleported(location: Vector2)
signal seed_changed(seed: int)
signal synchronous_generation_changed(use_single_thread: bool)
signal linear_zoom_changed(use_linear_zoom: bool)
signal linear_zoom_speed_changed(speed: float)
signal lerp_zoom_speed_changed(scale: float)
signal velocity_changed(scale: float)


const CONFIGFILE_SECTION_CONSOLE = "Console"
var tileserver_config


# Called when the node enters the scene tree for the first time.
func _ready():
	Console.connect(
		"toggled",               # relay this signal to the event bus
		func (is_console_shown): EventBus.emit_signal("console_open_toggled", is_console_shown)
	)	
	register_commands()
	
func show_console():
	Console.open()
	
func is_open():
	return Console.is_console_shown

func write_line(message = ''):
	Console.write_line(message)

func register_commands():
		Console.add_command('teleport', self, "do_teleport") \
			.set_description('Jump to a given location in the world') \
			.add_argument('x', TYPE_INT) \
			.add_argument('y', TYPE_INT) \
			.register()

		Console.add_command('seed', self, "do_seed_changed")\
			.set_description('Sets a new world-seed and recalculates the world') \
			.add_argument('seed', TYPE_INT) \
			.register()

		Console.add_command('synchronous_generation', self, "do_synchronous_generation_changed") \
			.set_description('Tiles are generated synchronously with the main thread one at a time when this is true, and generated in parallel otherwise. Synchronous generation is slower and impacts the UI but can simply debugging.') \
			.add_argument('enable', TYPE_BOOL) \
			.register()

		Console.add_command('linear_zoom', self, "do_linear_zoom_changed") \
			.set_description('Linear zooming is less responsive, but may be more desirable when recording a video') \
			.add_argument('enable', TYPE_BOOL) \
			.register()

		Console.add_command('linear_zoom_speed', self, "do_linear_zoom_speed_changed") \
			.set_description('Sets the zoom speed when linear_zoom is enabled. A desirable speed is probably around 1 to 2.') \
			.add_argument('speed', TYPE_FLOAT) \
			.register()

		Console.add_command('zoom_speed', self, "do_lerp_zoom_speed_changed") \
			.set_description('Sets the zoom speed. A value of 1 is normal speed (fast), a value of 0 would be bad (no zoom)') \
			.add_argument('scale', TYPE_FLOAT) \
			.register()

		Console.add_command('velocity', self, "do_velocity_changed") \
			.set_description('Leave this at 1 for normal use, but you can try a value closer to 0 if Movie Maker is making the movement too fast.') \
			.add_argument('scale', TYPE_FLOAT) \
			.register()

func do_teleport(x: int, y: int):
	emit_signal(teleported.get_name(), Vector2(x, y))

func do_seed_changed(world_seed: int):
	emit_signal(seed_changed.get_name(), world_seed)
		
func do_synchronous_generation_changed(use_single_thread: bool):
	emit_signal(synchronous_generation_changed.get_name(), use_single_thread)

func do_linear_zoom_changed(use_linear_zoom: bool):
	emit_signal(linear_zoom_changed.get_name(), use_linear_zoom)

func do_linear_zoom_speed_changed(speed: float):
	emit_signal(linear_zoom_speed_changed.get_name(), speed)

func do_lerp_zoom_speed_changed(scale: float):
	emit_signal(lerp_zoom_speed_changed.get_name(), scale)

func do_velocity_changed(scale: float):
	emit_signal(velocity_changed.get_name(), scale)

func expose_tileserver_config(configFile):
	var obsoleteCommands = Console.find_commands("set_")
	for obsoleteCommand in obsoleteCommands.get_keys():
		Console.remove_command(obsoleteCommand)

	tileserver_config = configFile # store it so we can set values in it if the checkbox menu-items are clicked
	var registerCommands = configFile.HasSection(CONFIGFILE_SECTION_CONSOLE)

	if registerCommands:
		for commandAndDesc in configFile.GetSectionKeys(CONFIGFILE_SECTION_CONSOLE):
			var split = commandAndDesc.split('|')
			if split.size() == 2:
				var command_name = split[0]
				var description = split[1]
				var value = configFile.GetValue(CONFIGFILE_SECTION_CONSOLE, commandAndDesc, false)
				var target_function = func (value_arg): self.do_set_command(commandAndDesc, value_arg[0])
				Console.add_command('set_%s' % [command_name], target_function) \
					.set_description(description) \
					.add_argument('value', typeof(value)) \
					.register()
			else:
				push_error("command string \"%s\" could not be split" % [commandAndDesc])

func do_set_command(config_key: String, value: float):
	tileserver_config.SetValue(CONFIGFILE_SECTION_CONSOLE, config_key, value)

