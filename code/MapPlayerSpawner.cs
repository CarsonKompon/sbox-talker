using Sandbox;
using System;
using System.Linq;

public sealed class MapPlayerSpawner : Component
{
	protected override void OnEnabled()
	{
		base.OnEnabled();

		if (Components.TryGet<MapInstance>(out var mapInstance))
		{
			mapInstance.OnMapLoaded += RespawnPlayers;

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
		if (spawnPoints.Length == 0)
			return;
		var randomSpawnPoint = spawnPoints[Random.Shared.Int(0, spawnPoints.Length - 1)];

		foreach (var player in Scene.GetAllComponents<PlayerController>().ToArray())
		{
			player.Transform.Position = randomSpawnPoint.Transform.Position;

			if (player.Components.TryGet<PlayerController>(out var pc))
			{
				pc.EyeAngles = randomSpawnPoint.Transform.Rotation.Angles();
			}

		}
	}
}
