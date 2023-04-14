using System;
using System.Linq;
using Godot;
using Godot.Collections;

namespace HuntersHaul.Scripts;

public partial class Player : CharacterBody2D
{
	private const float Speed = 300.0f;
	private const float HunterBoost = 1.2f;
	private const float SpeedBoost = 1.5f;
	private const float PowerUpDuration = 10f;

	[ExportGroup("Synced Data")]
	[Export] public Vector2 SyncedPosition;
	[Export] public Array<PowerUp.PowerUpType> AvailablePowerUps = new();
	[Export] public PowerUp.PowerUpType CurrentPowerUp;
	
	private PlayerControls _inputs;
	private GameState _gameState;
	[Export] public bool IsHunter;
	private string _playerName;
	private Sprite2D _sprite;
	private Label _label;

	private GUI _gui;

	private string _currentAnim = "";

	private const int MaxPowerUps = 3;
	private int _currentSelectedPowerUp;
	private double _currentPowerUpDuration;
	
	/// <summary>
	/// Updating the state of a player
	/// </summary>
	/// <param name="delta">Time between last update</param>
	public override void _PhysicsProcess(double delta)
	{
		if (_gameState.IsOver)
		{
			// Prevents moving on next round
			_inputs.Motion = new Vector2(0, 0);
			// Make visible
			_sprite.Visible = true;
			_label.Visible = true;
			return;
		}

		
		if ((Multiplayer.MultiplayerPeer == null || Multiplayer.GetUniqueId().ToString() == Name.ToString())
		    && (!IsHunter || _gameState.GetTime() > 0))
		{
			// The client which this player represents will update the control state, and notify it to everyone
			_inputs.Update();
			_gui.SetPowerUps(AvailablePowerUps, _currentSelectedPowerUp, CurrentPowerUp);
		}
		else
		{
			_sprite.Visible = CurrentPowerUp != PowerUp.PowerUpType.Invisible;
			_label.Visible = CurrentPowerUp != PowerUp.PowerUpType.Invisible;
            		
		}

		if (Multiplayer.MultiplayerPeer == null || IsMultiplayerAuthority())
		{
			// Server updates the position that will be notified to the clients
			SyncedPosition = Position;
		}
		else if (Multiplayer.GetUniqueId().ToString() != Name.ToString())
		{
			// The client simply updates the position to the last known one
			Position = SyncedPosition;
		}
		else
		{
			// If this is our player, prevent stuttering by lerping position
			Position = Position.Lerp(SyncedPosition, 0.25f);
		}
		
		Velocity = _inputs.Motion * Speed;
		if (IsHunter)
		{
			Velocity *= HunterBoost;
		}
		if (CurrentPowerUp == PowerUp.PowerUpType.Speed)
		{
			Velocity *= SpeedBoost;
		}
		MoveAndSlide();
		
		// If necessary, change selected power up
		if (_inputs.LastPressedNumber <= AvailablePowerUps.Count)
		{
			_currentSelectedPowerUp = Math.Max(0, _inputs.LastPressedNumber - 1);
			_inputs.LastPressedNumber = _currentSelectedPowerUp + 1;
		}
		
		if (_inputs.UsesPower && CurrentPowerUp == PowerUp.PowerUpType.Nothing)
		{
			if (_currentSelectedPowerUp < AvailablePowerUps.Count)
			{
				CurrentPowerUp = AvailablePowerUps[_currentSelectedPowerUp];
				AvailablePowerUps.RemoveAt(_currentSelectedPowerUp);
				_currentSelectedPowerUp = Math.Clamp(_currentSelectedPowerUp, 0, AvailablePowerUps.Count);
				_currentPowerUpDuration = 0;
			}
		}
		
		// Disable power up, if timer too high
		if (CurrentPowerUp != PowerUp.PowerUpType.Nothing)
		{
			_currentPowerUpDuration += delta;
			if (_currentPowerUpDuration > PowerUpDuration)
        	{
        		CurrentPowerUp = PowerUp.PowerUpType.Nothing;
        	}
		}
		
		
		// Update animation based on last known player input state
		var newAnim = "standing";
		if (_inputs.Motion.Y < 0)
		{
			newAnim = "walk_up";
		}
		else if (_inputs.Motion.Y > 0)
		{
			newAnim = "walk_down";
		}
		else if (_inputs.Motion.X < 0)
		{
			newAnim = "walk_left";
		}
		else if (_inputs.Motion.X > 0)
		{
			newAnim = "walk_right";
		}

		if (newAnim != _currentAnim)
		{
			_currentAnim = newAnim;
			GetNode<AnimationPlayer>("Anim").Play(_currentAnim);
		}

		
		for (var i = 0; i < GetSlideCollisionCount(); i++)
		{
			var collision = GetSlideCollision(i);

			switch (collision.GetCollider())
			{
				case Player:
					if (IsHunter)
					{
						_gameState.StopHunt();
					}
					break;
			}
		}
	}

	
	/// <summary>
	/// Pick ups a power-up
	/// </summary>
	/// <param name="type">Type of the power-up</param>
	/// <returns>Whether the player has picked it up</returns>
	public bool PickUpPowerUp(PowerUp.PowerUpType type)
	{
		if (AvailablePowerUps.Count < MaxPowerUps)
		{
			AvailablePowerUps.Add(type);
			return true;
		}
		return false;
	}

	/// <summary>
	/// Set the name in the label
	/// </summary>
	/// <param name="name">Name of this player</param>
	public void SetPlayerName(string name)
	{
		_label.Text = IsHunter ? $"{name} (Hunter)" : name;
	}
	
	public override void _Ready()
	{
		_gameState = GetNode<GameState>("/root/GameState");
		
		GD.Print($"{Multiplayer.GetUniqueId()}: Spawning player {Name}");
		if (StringExtensions.IsValidInt(Name))
		{
			GetNode<MultiplayerSynchronizer>("Inputs/InputsSync").SetMultiplayerAuthority(Name.ToString().ToInt());
		}
		_inputs = GetNode<PlayerControls>("Inputs");

		// Set camera
		var camera = GetNode<Camera2D>("Camera");
		if (Multiplayer.GetUniqueId().ToString() == Name.ToString())
		{
			camera.MakeCurrent();
			_gui = GetNode<GUI>($"/root/{_gameState.WorldName}/CanvasLayer/GUI");
		}

		_label = GetNode<Label>("Label");
		_sprite = GetNode<Sprite2D>("Sprite2D");
	}
}