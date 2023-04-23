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
	private Array<PowerUp.PowerUpType> _availablePowerUps = new();
	private PowerUp.PowerUpType _currentPowerUp;
	
	private PlayerControls _inputs;
	private GameState _gameState;
	private bool _isHunter;
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
			// Reset PowerUps
			_currentPowerUp = PowerUp.PowerUpType.Nothing;
			_availablePowerUps = new Array<PowerUp.PowerUpType>();
			return;
		}

		
		if ((Multiplayer.MultiplayerPeer == null || Multiplayer.GetUniqueId().ToString() == Name.ToString())
		    && (!_isHunter || _gameState.GetTime() > 0))
		{
			// The client which this player represents will update the control state, and notify it to everyone
			_inputs.Update();
			_gui.SetPowerUps(_availablePowerUps, _currentSelectedPowerUp, _currentPowerUp);
		}
		else
		{
			_sprite.Visible = _currentPowerUp != PowerUp.PowerUpType.Invisible;
			_label.Visible = _currentPowerUp != PowerUp.PowerUpType.Invisible;
		}

		if (Multiplayer.MultiplayerPeer == null || IsMultiplayerAuthority())
		{
			// Server updates the position that will be notified to the clients
			SyncedPosition = Position;
			
			// Handle power-up selection
			if (_inputs.UsesPower && _currentPowerUp == PowerUp.PowerUpType.Nothing)
			{
				if (_currentSelectedPowerUp < _availablePowerUps.Count)
				{
					_currentPowerUp = _availablePowerUps[_currentSelectedPowerUp];
					Rpc("SetCurrentPowerUp", Variant.From(_currentPowerUp));
					_availablePowerUps.RemoveAt(_currentSelectedPowerUp);
					_currentSelectedPowerUp = Math.Clamp(
						_currentSelectedPowerUp, 
						0, 
						Math.Max(0, _availablePowerUps.Count - 1));
					Rpc("SetAvailablePowerUps", Variant.From(_availablePowerUps));
					_currentPowerUpDuration = 0;
				}
			}
		
			// Disable power up, if timer too high
			if (_currentPowerUp != PowerUp.PowerUpType.Nothing)
			{
				_currentPowerUpDuration += delta;
				if (_currentPowerUpDuration > PowerUpDuration)
				{
					_currentPowerUp = PowerUp.PowerUpType.Nothing;
				    Rpc("SetCurrentPowerUp", Variant.From(PowerUp.PowerUpType.Nothing));
				}
			}
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
		if (_isHunter)
		{
			Velocity *= HunterBoost;
		}
		if (_currentPowerUp == PowerUp.PowerUpType.Speed)
		{
			Velocity *= SpeedBoost;
		}
		MoveAndSlide();
		
		// If necessary, change selected power up
		if (_inputs.LastPressedNumber <= _availablePowerUps.Count)
		{
			_currentSelectedPowerUp = Math.Max(0, _inputs.LastPressedNumber - 1);
			_inputs.LastPressedNumber = _currentSelectedPowerUp + 1;
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
					if (_isHunter)
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
		// Only the server can choose whether the powerUp can be picked up
		if ((Multiplayer.MultiplayerPeer == null || IsMultiplayerAuthority())
		    && _availablePowerUps.Count < MaxPowerUps)
		{
			_availablePowerUps.Add(type);
			Rpc("SetAvailablePowerUps", Variant.From(_availablePowerUps));
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
		// This should only be called by the server
		Rpc("SetPlayerNameRpc", Variant.From(_isHunter ? $"{name} (Hunter)" : name));
	}

	public void SetHunter(bool isHunter)
	{
		// This should only be called by the server
		Rpc("SetHunterRpc", Variant.From(isHunter));
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
	
	[Rpc]
	private void SetCurrentPowerUp(Variant powerUpType)
	{
		_currentPowerUp = powerUpType.As<PowerUp.PowerUpType>();
	}

	[Rpc(CallLocal = true)]
	private void SetPlayerNameRpc(Variant name)
	{
		_label.Text = name.As<string>();
	}

	[Rpc(CallLocal = true)]
	private void SetHunterRpc(Variant isHunter)
	{
		_isHunter = isHunter.AsBool();
	}

	[Rpc]
	private void SetAvailablePowerUps(Variant powerUps)
	{
		_availablePowerUps = powerUps.As<Array<PowerUp.PowerUpType>>(); 
		_currentSelectedPowerUp = Math.Clamp(
			_currentSelectedPowerUp, 
			0, 
			Math.Max(0, _availablePowerUps.Count - 1));
	}
}