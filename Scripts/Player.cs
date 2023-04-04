using Godot;

namespace HuntersHaul.Scripts;

public partial class Player : CharacterBody2D
{
	private const float Speed = 300.0f;
	private const float HunterBoost = 1.1f;

	[Export] public Vector2 SyncedPosition;

	private PlayerControls _inputs;
	private GameState _gameState;
	[Export] public bool IsHunter;
	public string PlayerName;

	private string _currentAnim = "";

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
			return;
		}
		
		if ((Multiplayer.MultiplayerPeer == null || Multiplayer.GetUniqueId().ToString() == Name.ToString())
		    && (!IsHunter || _gameState.GetTime() > 0))
		{
			// The client which this player represents will update the control state, and notify it to everyone
			_inputs.Update();
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
		
		Velocity = _inputs.Motion * Speed * HunterBoost;
		MoveAndSlide();
		
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
			if (collision.GetCollider() is Player && IsHunter)
			{
				_gameState.StopHunt();
			}
		}
	}

	/// <summary>
	/// Set the name in the label
	/// </summary>
	/// <param name="name">Name of this player</param>
	public void SetPlayerName(string name)
	{
		var label = GetNode<Label>("Label");
		label.Text = IsHunter ? $"{name} (Hunter)" : name;
	}
	
	public override void _Ready()
	{
		_gameState = GetNode<HuntersHaul.Scripts.GameState>("/root/GameState");
		
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
		}
	}
}