using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace HuntersHaul.Scripts;

public partial class GameState : Node
{
    private const int DefaultPort = 36187;
    private const int MaxPeers = 12;
    private int _searchTime = 10;
    private const double SpawnRate = 10f;
    public string PlayerName { get; private set; } = "Player";  // Player of this instance
    private DateTime _start;
    public bool IsOver { get; private set; }
    private double _lastSpawn;
    private double _syncedTime;

    private ENetMultiplayerPeer _peer;

    // Dictionary with player ids, and names
    private readonly Dictionary<long, string> _players = new();
    private readonly Dictionary<long, TimeSpan> _durations = new();
    private long _curHunterId = 0;

    [Signal]
    public delegate void PlayerListChangedEventHandler();

    [Signal]
    public delegate void ConnectionFailedEventHandler();

    [Signal]
    public delegate void ConnectionSucceededEventHandler();

    [Signal]
    public delegate void GameEndedEventHandler();

    [Signal]
    public delegate void GameErrorEventHandler(string what);

    private void PlayerConnected(long id)
    {
        GD.Print($"Player {id} connected");
        RpcId(id, "RegisterPlayer", PlayerName);
    }

    private void PlayerDisconnected(long id)
    {
        // Game is in progress
        if(HasNode("/root/Lobby"))
        {
            if(Multiplayer.IsServer())
            {
                EmitSignal(SignalName.GameError, $"Player {_players[id]} disconnected");
                EndGame();
            }
        }
        else
        {
            UnregisterPlayer(id);
        }
    }

    private void ConnectedOk()
    {
        EmitSignal(SignalName.ConnectionSucceeded);
    }

    private void ServerDisconnected()
    {
        EmitSignal(SignalName.GameError, "Server disconnected");
        EndGame();
    }

    private void ConnectedFail()
    {
        Multiplayer.MultiplayerPeer = null;
        EmitSignal(SignalName.ConnectionFailed);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    void RegisterPlayer(string newPlayerName)
    {
        GD.Print($"Registering new player {newPlayerName}");
        var id = Multiplayer.GetRemoteSenderId();
        _players[id] = newPlayerName;
        EmitSignal(SignalName.PlayerListChanged);

    }

    void UnregisterPlayer(long id)
    {
        _players.Remove(id);
        EmitSignal(SignalName.PlayerListChanged);
    }

    [Rpc(CallLocal = true)]
    void LoadWorld()
    {
        var world = (PackedScene)GD.Load("res://Worlds/World.tscn");
        GetTree().Root.AddChild(world.Instantiate());
        
        // Hide lobby
        GetTree().Root.GetNode<Lobby>("Lobby").Hide();

        GetTree().Paused = false;
        _start = DateTime.Now;
        IsOver = false;
    }

    public void HostGame(string newPlayerName)
    {
        GD.Print($"{newPlayerName} is host");
        PlayerName = newPlayerName;
        _peer = new ENetMultiplayerPeer();
        _peer.CreateServer(DefaultPort, MaxPeers);
        Multiplayer.MultiplayerPeer = _peer;
    }

    public void JoinGame(string ip, string newPlayerName)
    {
        GD.Print($"Joining {ip} as {newPlayerName}");
        PlayerName = newPlayerName;
        _peer = new ENetMultiplayerPeer();
        _peer.CreateClient(ip, DefaultPort);
        Multiplayer.MultiplayerPeer = _peer;
    }

    public void BeginGame(int searchTime)
    {
        System.Diagnostics.Debug.Assert(Multiplayer.IsServer());
        
        Rpc("LoadWorld");
        Rpc("SetSearchTime", Variant.From(searchTime));

        SpawnPlayers();
    }

    public void BeginNextRound()
    {
        System.Diagnostics.Debug.Assert(Multiplayer.IsServer());
        
        var world = GetTree().Root.GetNode("World");
        
        SetPlayerPositions();
        _start = DateTime.Now;
        Rpc("UnsetOver");
        
    }

    private void SpawnPlayers()
    {
        var world = GetTree().Root.GetNode("World");
        var playerScene = (PackedScene)GD.Load("res://Player.tscn");
        var playerParent = world.GetNode<Node2D>("Players");
        
        var players = new List<Player>();

        for (var i = 0; i < _players.Count + 1; i++)
        {
            var player = (Player)playerScene.Instantiate();
            players.Add(player);
            players[i].Name = i == 0 ? "1" : _players.Keys.ToArray()[i - 1].ToString();
            playerParent.AddChild(player);
        }
       
        SetPlayerPositions();
    }

    private void SetPlayerPositions()
    {
        var world = GetTree().Root.GetNode("World");
        
        var playerParent = world.GetNode<Node2D>("Players");
        // Peer id and spawn points
        var multiplayerIdSpawnIndexDict = new Godot.Collections.Dictionary<long, long>
        {
            [1] = 0
        };
        var spawnPointIndex = 1;
        foreach (var p in _players.Keys)
        {
            multiplayerIdSpawnIndexDict[p] = spawnPointIndex;
            spawnPointIndex++;
        }
        
        GD.Randomize();
        var hunterIndex = 0L;
        do
        {
            hunterIndex = GD.Randi() % (uint)(_players.Count + 1);
            _curHunterId = multiplayerIdSpawnIndexDict.First(pair => pair.Value == hunterIndex).Key;
        } while (_durations.ContainsKey(_curHunterId));

        var i = 0;
        var players = playerParent.GetChildren().Cast<Player>().ToArray();

        foreach (var pointNumber in multiplayerIdSpawnIndexDict.Keys)
        {
            var spawnPosition = world.GetNode<Marker2D>("SpawnPoints/" + multiplayerIdSpawnIndexDict[pointNumber]).Position;
            players[i].SyncedPosition = spawnPosition;
            players[i].Position = spawnPosition;
            players[i].IsHunter = hunterIndex-- == 0;
            players[i].SetPlayerName(pointNumber == Multiplayer.GetUniqueId() ? PlayerName : _players[pointNumber]);
            i++;
        }
    }

    public void EndGame()
    {
        // Game is running
        if (HasNode("/root/World"))
        {
            GetNode("/root/World").QueueFree();
        }

        EmitSignal(SignalName.GameEnded);
        _players.Clear();
        _durations.Clear();
        _peer = null;
        Multiplayer.MultiplayerPeer.Close();
        Multiplayer.MultiplayerPeer = null;
    }

    public IEnumerable<string> GetPlayers()
    {
        return _players.Values;
    }
    
    public string GetPlayerName(long id)
    {
        return id == 1 ? PlayerName : _players[id];
    }

    public Dictionary<long, TimeSpan> GetDurations()
    {
        return _durations;
    }

    public bool HasNextRound()
    {
        // <= because players count does not include server
        return _durations.Count <= _players.Count;
    }

    public double GetTime()
    {
        if (!Multiplayer.IsServer())
        {
            return _syncedTime;
        }
        if (_durations.ContainsKey(_curHunterId))
        {
            return _durations[_curHunterId].TotalSeconds;

        }
        return (DateTime.Now - _start).Subtract(TimeSpan.FromSeconds(_searchTime)).TotalSeconds;
    }
    
    public override void _Ready()
    {
        Multiplayer.PeerConnected += PlayerConnected;
        Multiplayer.PeerDisconnected += PlayerDisconnected;
        Multiplayer.ConnectedToServer += ConnectedOk;
        Multiplayer.ConnectionFailed += ConnectedFail;
        Multiplayer.ServerDisconnected += ServerDisconnected;
    }

    public override void _Process(double delta)
    {
        // Game not running
        if (!HasNode("/root/World"))
            return;
        
        if (!Multiplayer.IsServer())
            return;

        Rpc("SyncGameState", Variant.From(GetTime()));
        
        // Spawn powerups
        // TODO
        return;
        _lastSpawn += delta;
        if (_lastSpawn > SpawnRate)
        {
            _lastSpawn = 0;
            var powerUpSpawnPoints = GetTree().Root.GetNode("World").GetNode<Node2D>("PowerUpSpawnPoints");
            var spawnPointsCount = powerUpSpawnPoints.GetChildCount();
            GD.Randomize();
            var idx = Math.Abs((int)GD.Randi()) % spawnPointsCount;
            var spawnPoint = powerUpSpawnPoints.GetChild<Marker2D>(idx).Position; 
            
            
            GetNode<MultiplayerSpawner>("/root/World/PowerUpSpawner").Spawn(Variant.From(spawnPoint));
        }
    }

    [Rpc]
    private void SyncGameState(Variant data)
    {
        _syncedTime = data.As<double>();
    }

    public void StopHunt()
    {
        // Only the server can stop the game
        if(!Multiplayer.IsServer())
            return;
        
        // Don't stop it didn't even begin
        if (GetTime() < 0)
        {
            return;
        }

        _durations[_curHunterId] = (DateTime.Now - _start).Subtract(TimeSpan.FromSeconds(_searchTime));
        Rpc("SetOver");
    }

    [Rpc(CallLocal = true)]
    private void SetOver()
    {
        IsOver = true;
    }

    [Rpc(CallLocal = true)]
    private void UnsetOver()
    {
        IsOver = false;
    }

    [Rpc(CallLocal = true)]
    private void SetSearchTime(Variant time)
    {
        _searchTime = time.As<int>();
    }
}