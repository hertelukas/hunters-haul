using Godot;

public partial class HoverEffect : ColorRect
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		MouseEntered += MakeLarge;
		MouseExited += MakeNormal;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void MakeLarge()
	{
		CustomMinimumSize = new Vector2((float)(Size.X * 1.2), 0);
	}

	private void MakeNormal()
	{
		CustomMinimumSize = new Vector2(0, 0);
	}
}
