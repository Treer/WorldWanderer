# Copyright 2023 Treer (https://github.com/Treer)
# License: MIT, see LICENSE.txt for rights granted

extends TextureRect

# MovieMaker mode is incapable of including the mouse cursor:
#    https://github.com/godotengine/godot-proposals/issues/5387
# So this node draws a simulated cursor if movie maker mode is enabled

func _ready():
	process_mode = Node.PROCESS_MODE_ALWAYS

	# Only show cursor if MovieMaker mode is active
	visible = OS.has_feature("movie")
	
func _process(delta):
	global_position = get_viewport().get_mouse_position()
