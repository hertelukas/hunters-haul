using System;
using Godot;

namespace HuntersHaul.Scripts;

public partial class PlayerControls : Node
{

	private Vector2 _motion;
	private float MaxDiagonal;
	[Export]
	public Vector2 Motion
	{
		get => _motion;
		set
		{
			_motion.X = Mathf.Clamp(value.X, -1, 1);
			_motion.Y = Mathf.Clamp(value.Y, -1, 1);
			
			// Limit vertical speed to 1 as well
			if (Math.Sqrt(Math.Pow(_motion.X, 2) + Math.Pow(_motion.Y, 2)) > 1)
			{
				_motion.X = Mathf.Clamp(value.X, -MaxDiagonal, MaxDiagonal);
				_motion.Y = Mathf.Clamp(value.Y, -MaxDiagonal, MaxDiagonal);
			}
		}
	}

	public override void _Ready()
	{
		MaxDiagonal = (float)Math.Sqrt(0.5);
	}


	public void Update()
	{
		var m = new Vector2();
		if (Input.IsActionPressed("move_left"))
			m += new Vector2(-1, 0);
		if(Input.IsActionPressed("move_right"))
			m += new Vector2(1, 0);
		if(Input.IsActionPressed("move_up"))
			m += new Vector2(0, -1);
		if (Input.IsActionPressed("move_down"))
			m += new Vector2(0, 1);

		Motion = m;
	}
}