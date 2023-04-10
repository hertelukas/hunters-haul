using System;
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
		var powerUpScene = (PackedScene)GD.Load("res://PowerUps/DummyPowerUp.tscn");
		var powerUp = (Node2D)powerUpScene.Instantiate();

		powerUp.Position = data.As<Array>()[0].As<Vector2>();
		powerUp.Name = data.As<Array>()[1].As<int>().ToString();
		
		return powerUp;
	};
}