# Copyright 2023 Treer (https://github.com/Treer)
# License: MIT, see LICENSE.txt for rights granted

@tool
extends Control

const MAX_SCALE_LENGTH_ON_SCREEN = 300
const MARGIN = 8
const CONTROL_HEIGHT = 34
const fontSize = 16;
const postWidth: int = 1
const barWidth: int = 2

@export var MapScale: float = 1: set = set_mapscale
@export var ForeColor: Color = Color.WHITE
@export var BackColor: Color = Color("#000000", 0.4)

var scaleLengthOnScreen: int
var text: String
var textWidth: int
var font: Font

# Called when the node enters the scene tree for the first time.
func _ready():
	font = get_theme_font("sans-serif")
	MapScale = 1
	
func _draw():
	var y1 = 21
	var y2 = 26
	var y3 = 30
	var yFont = y1 - 2

	# draw dark line outlines
	var xAdj = -floor((1 + postWidth) / 2.0)
	draw_line(Vector2(MARGIN + xAdj, y1), Vector2(MARGIN + xAdj, y3), BackColor, 1)
	draw_line(Vector2(MARGIN + scaleLengthOnScreen + xAdj, y1), Vector2(MARGIN + scaleLengthOnScreen + xAdj, y3), BackColor, 1)
	xAdj = ceil((1 + postWidth) / 2.0)
	draw_line(Vector2(MARGIN - xAdj, y1), Vector2(MARGIN - xAdj, y3), BackColor, 1)
	draw_line(Vector2(MARGIN + scaleLengthOnScreen + xAdj, y1), Vector2(MARGIN + scaleLengthOnScreen + xAdj, y3), BackColor, 1)
	var yAdj = -floor((1 + barWidth) / 2.0)
	draw_line(Vector2(MARGIN, y2 + yAdj), Vector2(MARGIN + scaleLengthOnScreen, y2 + yAdj), BackColor, 1)
	yAdj = ceil((1 + barWidth) / 2.0)
	draw_line(Vector2(MARGIN, y2 + yAdj), Vector2(MARGIN + scaleLengthOnScreen, y2 + yAdj), BackColor, 1)

	# draw scale lines
	draw_line(Vector2(MARGIN, y2), Vector2(MARGIN + scaleLengthOnScreen, y2), ForeColor, barWidth)
	draw_line(Vector2(MARGIN + scaleLengthOnScreen, y1), Vector2(MARGIN + scaleLengthOnScreen, y3), ForeColor, postWidth)
	draw_line(Vector2(MARGIN, y1), Vector2(MARGIN, y3), ForeColor, postWidth)
	
	# draw text
	var stringSize = font.get_string_size(text, HORIZONTAL_ALIGNMENT_LEFT, -1, fontSize)
	draw_string_outline(font, Vector2((custom_minimum_size.x - stringSize.x) / 2, yFont), text, HORIZONTAL_ALIGNMENT_LEFT, -1, fontSize, 6, BackColor)
	draw_string(font, Vector2((custom_minimum_size.x - stringSize.x) / 2, yFont), text, HORIZONTAL_ALIGNMENT_LEFT, -1, fontSize, ForeColor)
	

func set_mapscale(mapScale: float):
	var scaleLengthInWorld = getScaleLengthInWorld(mapScale)
	scaleLengthOnScreen = scaleLengthInWorld * mapScale
	if scaleLengthInWorld >= 1000:
		text = "%d km" % [scaleLengthInWorld / 1000]
	else:
		text = "%d meters" % [scaleLengthInWorld]

	var stringSize = font.get_string_size(text, HORIZONTAL_ALIGNMENT_LEFT, -1, fontSize)
	custom_minimum_size = Vector2(max(stringSize.x, scaleLengthOnScreen + 2 * MARGIN), CONTROL_HEIGHT)
	queue_redraw()
	#textWidth = fontMetrics.stringWidth(text);
	#setWidth(Math.max(scaleLengthOnScreen, textWidth) + (MARGIN * 2));
	
func getScaleLengthInWorld(mapScale: float):
	if mapScale == 0:
		push_error("MapScale must not be 0")
		return MAX_SCALE_LENGTH_ON_SCREEN
	var firstDigit = 1
	var base = 10
	var result = firstDigit * base
	var previousResult = result
	while (mapScale * result < MAX_SCALE_LENGTH_ON_SCREEN):
		firstDigit = getNextFirstDigit(firstDigit)
		base = getNextBase(firstDigit, base)
		previousResult = result
		result = firstDigit * base
	return previousResult

func getNextFirstDigit(firstDigit: int):
	if firstDigit == 1:
		return 2
	elif firstDigit == 2:
		return 5
	else:
		return 1

func getNextBase(firstDigit: int, base: int):
	if firstDigit == 1:
		# First digit has wrapped back to 1, so raise the base by 10
		return base * 10
	else:
		return base

