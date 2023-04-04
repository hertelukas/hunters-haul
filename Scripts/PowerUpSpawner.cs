using System;
using Godot;

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

		powerUp.Position = data.As<Vector2>();
		
		return powerUp;
	};
}