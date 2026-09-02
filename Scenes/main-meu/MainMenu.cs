using Godot;
using System;

public partial class MainMenu : Control
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void On_newRobot_pressed()
	{
		GetTree().ChangeSceneToFile("res://Scenes/constructor.tscn");
    }

	public void On_loadRobot_pressed()
	{
		GD.Print("Load Robot button pressed");
    }

	public void On_quit_pressed()
	{
        GetTree().Quit();
    }

}
