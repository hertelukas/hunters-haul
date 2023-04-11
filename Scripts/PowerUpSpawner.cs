using System;
using System.Collections.Generic;
using Godot;
using Array = Godot.Collections.Array;

namespace HuntersHaul.Scripts;

public partial class PowerUpSpawner : MultiplayerSpawner
{

	public PowerUpSpawner()
	{
		SpawnFunction = Callable.From(spawnPowerUp); 
	}

	private readonly Func<Variant, Node> spawnPowerUp = data =>
	{
		var dataArray = data.As<Array>();
		var powerUpScenes = CustomTools.DirContent("res://PowerUps");
		GD.Seed(dataArray[2].As<ulong>());
		var sceneName = powerUpScenes[(int)(GD.Randi() % powerUpScenes.Count)];
		var powerUpScene = (PackedScene)GD.Load($"res://PowerUps/{sceneName}.tscn");
		var powerUp = (Node2D)powerUpScene.Instantiate();

		powerUp.Position = dataArray[0].As<Vector2>();
		powerUp.Name = dataArray[1].As<int>().ToString();
		
		return powerUp;
	};
	
}