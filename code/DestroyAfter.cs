using Sandbox;

public sealed class DestroyAfter : Component
{
	[Property] public float DestroyAfterSeconds { get; set; } = 2f;
	float timeSinceCreated = 0f;
	protected override void OnUpdate()
	{
		timeSinceCreated += Time.Delta;

		if (timeSinceCreated > DestroyAfterSeconds)
			GameObject.Destroy();
	}
}