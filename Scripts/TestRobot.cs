using Godot;

namespace RobotMaker.Scripts;

public partial class TestRobot : VehicleBody3D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		Steering = (float)Mathf.MoveToward(Steering, Input.GetAxis("right", "left") * 0.9f, delta * 10);
        EngineForce = Input.GetAxis("down", "up") * 300;
		
	}
}