using System;
using System.Globalization;
using System.Linq;
using Godot;
using Godot.Collections;

namespace HuntersHaul.Scripts;

public partial class GUI : Control
{
	private Label _time;
	private GameState _gameState;
	private Button _leaveGameButton;
	private Button _nextRoundButton;
	private bool _showingUi;
	private VBoxContainer _rightBox;

	private Array<PowerUp.PowerUpType> _lastPowerUps = new();
	private int _lastSelected;
	private PowerUp.PowerUpType _lastCurrent;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_time = GetNode<Label>("HBoxContainer/Center/Time");
		_gameState = GetNode<GameState>("/root/GameState");
		_leaveGameButton = GetNode<Button>("HBoxContainer/Center/LeaveGameButton");
		_leaveGameButton.Disabled = true;
		_leaveGameButton.Visible = false;
		_nextRoundButton = GetNode<Button>("HBoxContainer/Center/NextRoundButton");
		_nextRoundButton.Disabled = true;
		_nextRoundButton.Visible = false;
		_rightBox = GetNode<VBoxContainer>("HBoxContainer/Right");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (!Multiplayer.HasMultiplayerPeer()) return;
		
		var text = "";
		if (_gameState.IsOver)
		{
			ShowUi(true);

			if (!Multiplayer.IsServer()) return;
			foreach (var durationPair in _gameState.GetDurations())
			{
				text += _gameState.GetPlayerName(durationPair.Key);
				text += " needed ";
				text += durationPair.Value.TotalSeconds.ToString("0.00", CultureInfo.InvariantCulture);
				text += " seconds\n";
			}
			
			_time.Text = text;
			return;
		}
		ShowUi(false);
		if (!Multiplayer.IsServer()) return;
		
		var time  = _gameState.GetTime();
		var timeString = Math.Abs(time).ToString("0.00", CultureInfo.InvariantCulture);
		text = $"{timeString}s needed to catch Kleeb!";
		if (time < 0)
		{
			text = $"{timeString}s to hide!";
		}

		_time.Text = text;
	}

	public void SetPowerUps(Array<PowerUp.PowerUpType> availablePowerUps, int selected, PowerUp.PowerUpType current)
	{
		if (current == _lastCurrent && selected == _lastSelected && availablePowerUps.SequenceEqual(_lastPowerUps))
		{
			return;
		}
		
		foreach (var child in _rightBox.GetChildren())
		{
			child.QueueFree();
		}

		_lastPowerUps.Clear();
		_lastSelected = selected;
		_lastCurrent = current;

		var i = 0;
		foreach (var p in availablePowerUps)
		{
			_lastPowerUps.Add(p);
			var powerUpLabel = new Label();
			powerUpLabel.Text = $"{p} ({i + 1})";
			powerUpLabel.AddThemeConstantOverride("outline_size", i == selected ? 8 : 2);
			powerUpLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
			_rightBox.AddChild(powerUpLabel);
			i++;
		}

		if (current != PowerUp.PowerUpType.Nothing)
		{
			var currentLabel = new Label();
			currentLabel.Text = $"Current: {current.ToString()}";
			currentLabel.AddThemeConstantOverride("outline_size", 2);
			currentLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
			_rightBox.AddChild(currentLabel);
		}
	}

	private void ShowUi(bool state)
	{
		if (state == _showingUi)
		{
			return;
		}

		_showingUi = state;
		_leaveGameButton.Disabled = !state;
		_leaveGameButton.Visible = state;

		if (_gameState.HasNextRound() && Multiplayer.IsServer())
		{
			_nextRoundButton.Disabled = !state;
			_nextRoundButton.Visible = state;
		}
	}

	private void OnNextRoundPressed()
	{
		GD.Print("====================");
		GD.Print("Next Round");
		GD.Print("====================");
		_gameState.BeginNextRound();

	}

	private void OnExitGamePressed()
	{
		_gameState.EndGame();
	}
}