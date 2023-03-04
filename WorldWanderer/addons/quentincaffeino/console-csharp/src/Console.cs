using Godot;
using System.IO;

public partial class Console : Node
{
	CanvasLayer _console;

	public override void _Ready()
	{
		_console = GetTree().Root.GetNode<CanvasLayer>("Console");
	}

	public CommandBuilder AddCommand(string name, GodotObject target, string targetMethodName)
	{
		GodotObject consoleCommand = _console.Call("add_command", name, target, targetMethodName).Obj as GodotObject;
		return new CommandBuilder(consoleCommand);
	}
}
