
extends 'res://addons/quentincaffeino/console/src/Type/BaseType.gd'


func _init():
	super('Bool')


# @param    Variant  value
# @returns  Variant
func normalize(value):
	value = value.to_lower()
	return value == '1' or value == 'true' or value == 'on' or value == 'yes' or value == 'enable' or value == 'enabled'
