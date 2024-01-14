using Sandbox;

public sealed class Rotator : Component
{
	[Property] Angles RotateSpeed { get; set; } = new Angles(0, 0, 0);
	protected override void OnUpdate()
	{
		Transform.Rotation = Transform.Rotation * (RotateSpeed * Time.Delta);
	}
}