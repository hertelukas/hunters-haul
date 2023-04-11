using Godot;

namespace HuntersHaul.Scripts;

public partial class PowerUp : Area2D 
{
	[Export] public PowerUpType Type;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is Player player)
		{
			if (player.PickUpPowerUp(Type))
			{
				// Delete power-up
				QueueFree();
			}
		}
	}

	public enum PowerUpType
	{
		Speed
	}
}
