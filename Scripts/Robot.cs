using Godot;

namespace RobotMaker.Scripts;

public partial class Robot : VehicleBody3D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		EngineForce = Input.GetAxis("ui_up", "ui_down") * 300;
	}
}