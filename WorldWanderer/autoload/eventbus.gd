# Copyright 2023 Treer (https://github.com/Treer)
# License: MIT, see LICENSE.txt for rights granted

# EventBus autoload. Use `EventBus` global variable as a shortcut to access
# features.
# E.g.:
#    EventBus.emit_signal("map_zoom_changed", getScale())
#    EventBus.connect("map_zoom_changed", self, "on_map_zoom_changed")
extends Node

## scale is a float, representing pixels per meter
signal map_zoom_changed(scale)

signal console_open_toggled(is_console_open)
