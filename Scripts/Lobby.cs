using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace HuntersHaul.Scripts;

public partial class Lobby : Control
{
	private GameState _gameState;
	private Control _connectControl;
	private Control _playersControl;
	private LineEdit _nameLineEdit;
	private LineEdit _ipLineEdit;
	private Button _hostButton;
	private Button _joinButton;
	private Label _errorLabel;
	
	private ItemList _playerList;
	private Button _startButton;
	private SpinBox _searchTimeBox;
	private Button _cycleLeftButton;
	private Button _cycleRightButton;
	private Label _currentMapLabel;
	private List<string> _scenes;
	private int _sceneIndex;

	private AcceptDialog _errorDialog;
	
	private string _playerName;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_gameState = GetNode<HuntersHaul.Scripts.GameState>("/root/GameState");
		_gameState.ConnectionFailed += OnConnectionFailed;
		_gameState.ConnectionSucceeded += OnConnectionSuccess;
		_gameState.PlayerListChanged += RefreshLobby;
		_gameState.GameEnded += OnGameEnded;
		_gameState.GameError += OnGameError;

		_connectControl = GetNode<Control>("Connect");
		_playersControl = GetNode<Control>("Players");
		_nameLineEdit = GetNode<LineEdit>("Connect/GridContainer/Name");
		_ipLineEdit = GetNode<LineEdit>("Connect/GridContainer/IP");
		_hostButton = GetNode<Button>("Connect/GridContainer/HostButton");
		_joinButton = GetNode<Button>("Connect/GridContainer/JoinButton");
		_errorLabel = GetNode<Label>("Connect/GridContainer/ErrorLabel");
		
		_playerList = GetNode<ItemList>("Players/PlayerList");
		_startButton = GetNode<Button>("Players/StartButton");
		_searchTimeBox = GetNode<SpinBox>("Players/SearchTime");
		_currentMapLabel = GetNode<Label>("Players/WorldChooserContainer/CurrentMap");
		_cycleLeftButton = GetNode<Button>("Players/WorldChooserContainer/CycleLeftButton");
		_cycleRightButton = GetNode<Button>("Players/WorldChooserContainer/CycleRightButton");

		_errorDialog = GetNode<AcceptDialog>("ErrorDialog");

		_scenes = DirContent("res://Worlds");
		_currentMapLabel.Text = _scenes[0];
	}

	private void OnHostPressed()
	{
		if(string.IsNullOrWhiteSpace(_nameLineEdit.Text))
		{
			_errorLabel.Text = "Invalid name!";
			return;
		}
		
		_connectControl.Hide();
		_playersControl.Show();

		_playerName = _nameLineEdit.Text;
		_gameState.HostGame(_playerName);
		RefreshLobby();
	}

	private void OnJoinPressed()
	{
		if (string.IsNullOrWhiteSpace(_nameLineEdit.Text))
		{
			_errorLabel.Text = "Invalid name!";
			return;
		}

		var ip = _ipLineEdit.Text;

		if (!ip.IsValidIPAddress())
		{
			_errorLabel.Text = "Invalid IP address!";
			return;
		}
		
		_hostButton.Disabled = true;
		_joinButton.Disabled = true;
		
		_playerName = _nameLineEdit.Text;
		
		_gameState.JoinGame(_ipLineEdit.Text, _playerName);
	}

	private void OnConnectionSuccess()
	{
		_connectControl.Hide();
		_playersControl.Show();
	}

	private void OnConnectionFailed()
	{
		_hostButton.Disabled = false;
		_joinButton.Disabled = false;
	}

	private void OnGameEnded()
	{
		Show();
		_connectControl.Show();
		_playersControl.Hide();
		_hostButton.Disabled = false;
		_joinButton.Disabled = false;
	}

	private void OnGameError(string msg)
	{
		_errorDialog.DialogText = msg;
		_errorDialog.PopupCentered();
		_hostButton.Disabled = false;
		_joinButton.Disabled = false;
	}

	private void RefreshLobby()
	{
		var players = _gameState.GetPlayers().OrderBy(p => p);
		_playerList.Clear();
		_playerList.AddItem($"{_gameState.PlayerName} (You)");

		foreach (var player in players)
		{
			_playerList.AddItem(player);
		}

		_startButton.Disabled = !Multiplayer.IsServer() || _playerList.ItemCount < 2;
		_cycleLeftButton.Disabled = !Multiplayer.IsServer();
		_cycleRightButton.Disabled = !Multiplayer.IsServer();
	}

	private void OnStartPressed()
	{
		// TODO handle invalid spinBox entries
		_gameState.BeginGame((int)_searchTimeBox.Value, _scenes[_sceneIndex]);
	}

	private void OnQuitPressed()
	{
		GetTree().Quit();
	}

	private void OnRightPressed()
	{
		_sceneIndex = ++_sceneIndex % _scenes.Count;
		GD.Print(_sceneIndex);
		_currentMapLabel.Text = _scenes[_sceneIndex];
	}

	private void OnLeftPressed()
	{
		_sceneIndex = (--_sceneIndex % _scenes.Count);
		if (_sceneIndex < 0)
		{
			_sceneIndex += _scenes.Count;
		}
		GD.Print(_sceneIndex);
		_currentMapLabel.Text = _scenes[_sceneIndex];
	}

	private List<string> DirContent(string path)
	{
		using var dir = DirAccess.Open(path);
		var result = new List<string>();
		if (dir != null)
		{
			dir.ListDirBegin();
			string fileName = dir.GetNext();
			while (fileName != "")
			{
				if (!dir.CurrentIsDir())
				{
					if (fileName.EndsWith(".tscn"))
					{
						result.Add(fileName.Remove(fileName.IndexOf(".tscn", StringComparison.Ordinal)));
						
					}
				}
				fileName = dir.GetNext();
			}
		}
		else
		{
			GD.Print("An error occurred when trying to access the path.");
		}

		return result;
	}

}