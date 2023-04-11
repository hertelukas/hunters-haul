using System;
using System.Globalization;
using Godot;

namespace HuntersHaul.Scripts;

public partial class GUI : Control
{
	private Label _time;
	private GameState _gameState;
	private Button _leaveGameButton;
	private Button _nextRoundButton;
	private bool _showingUi;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_time = GetNode<Label>("GridContainer/Time");
		_gameState = GetNode<HuntersHaul.Scripts.GameState>("/root/GameState");
		_leaveGameButton = GetNode<Button>("GridContainer/LeaveGameButton");
		_leaveGameButton.Disabled = true;
		_leaveGameButton.Visible = false;
		_nextRoundButton = GetNode<Button>("GridContainer/NextRoundButton");
		_nextRoundButton.Disabled = true;
		_nextRoundButton.Visible = false;
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
			// TODO only server should write this and sync it
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