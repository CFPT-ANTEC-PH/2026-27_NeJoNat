using Godot;
using System;

namespace RobotMaker.Scripts;

[GlobalClass, Tool]
public partial class ConstructorUI : CanvasLayer
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		if (Engine.IsEditorHint())
			return;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (Engine.IsEditorHint())
			return;
	}
}
