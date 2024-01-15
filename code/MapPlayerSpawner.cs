using Sandbox;
using System;
using System.Linq;

public class MapPlayerSpawner : Component
{
	protected override void OnEnabled()
	{
		base.OnEnabled();

		if (Components.TryGet<MapInstance>(out var mapInstance))
		{
			mapInstance.MapName = Scene.Title;
			mapInstance.OnMapLoaded += RespawnPlayers;
			mapInstance.OnMapUnloaded += CleanUp;

			// already loaded
			if (mapInstance.IsLoaded)
			{
				RespawnPlayers();
			}
		}
	}

	protected override void OnDisabled()
	{
		if (Components.TryGet<MapInstance>(out var mapInstance))
		{
			mapInstance.OnMapLoaded -= RespawnPlayers;
		}

	}

	void RespawnPlayers()
	{

		// var map = Components.Get<MapInstance>();
		// foreach (var obj in map.GameObject.Children)
		// {
		// 	if (obj.Components.Get<Rigidbody>() is Rigidbody rb)
		// 	{
		// 		var newObj = obj.Clone(obj.Transform.World);
		// 		newObj.Components.Create<Grabbable>();
		// 		newObj.Network.Spawn();
		// 		obj.Destroy();
		// 	}
		// }

		var spawnPoints = Scene.GetAllComponents<SpawnPoint>().ToArray();
		var randomSpawnPoint = new Transform(Vector3.Zero, Rotation.Identity);
		if (spawnPoints.Length > 0) randomSpawnPoint = spawnPoints[Random.Shared.Int(0, spawnPoints.Length - 1)].Transform.World;

		foreach (var player in Scene.GetAllComponents<PlayerController>().ToArray())
		{
			player.Transform.Position = randomSpawnPoint.Position;

			if (player.Components.TryGet<PlayerController>(out var pc))
			{
				pc.EyeAngles = randomSpawnPoint.Rotation.Angles();
			}

		}
	}

	void CleanUp()
	{
		foreach (var obj in Scene.Children)
		{
			if (obj.Tags.Has("interact"))
			{
				obj.Destroy();
			}
		}
	}
}
