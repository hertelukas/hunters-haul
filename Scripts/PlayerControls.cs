using System;
using Godot;

namespace HuntersHaul.Scripts;

public partial class PlayerControls : Node
{

	private Vector2 _motion;
	private float _maxDiagonal;
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
				_motion.X = Mathf.Clamp(value.X, -_maxDiagonal, _maxDiagonal);
				_motion.Y = Mathf.Clamp(value.Y, -_maxDiagonal, _maxDiagonal);
			}
		}
	}

	public bool UsesPower;

	public override void _Ready()
	{
		_maxDiagonal = (float)Math.Sqrt(0.5);
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
		UsesPower = Input.IsActionPressed("use_power");
			

		Motion = m;
	}
}