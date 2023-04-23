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
	public int LastPressedNumber;

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
		if (Input.IsActionPressed("use_power") != UsesPower)
		{
			Rpc("SetUsesPower", Variant.From(!UsesPower));
		}

		var lastPressedNumber = -1;
		// Register number inputs
		if (Input.IsKeyPressed(Key.Key1))
			lastPressedNumber = 1;
		else if (Input.IsKeyPressed(Key.Key2))
			lastPressedNumber = 2;
		else if (Input.IsKeyPressed(Key.Key3))
			lastPressedNumber = 3;
		else if (Input.IsKeyPressed(Key.Key4))
			lastPressedNumber = 4;
		else if (Input.IsKeyPressed(Key.Key5))
			lastPressedNumber = 5;
		else if (Input.IsKeyPressed(Key.Key6))
			lastPressedNumber = 6;
		else if (Input.IsKeyPressed(Key.Key7))
			lastPressedNumber = 7;
		else if (Input.IsKeyPressed(Key.Key8))
			lastPressedNumber = 8;
		else if (Input.IsKeyPressed(Key.Key9))
			lastPressedNumber = 9;

		if (lastPressedNumber != -1 && lastPressedNumber != LastPressedNumber)
		{
			Rpc("SetLastPressedNumber", Variant.From(lastPressedNumber));
		}

		Motion = m;
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	private void SetUsesPower(Variant usesPower)
	{
		UsesPower = usesPower.AsBool();
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	private void SetLastPressedNumber(Variant number)
	{
		LastPressedNumber = number.As<int>();
	}
}