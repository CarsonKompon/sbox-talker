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

		var spawnPoints = Scene.GetAllComponents<SpawnPoint>().ToArray();
		var randomSpawnPoint = spawnPoints[Random.Shared.Next(0, spawnPoints.Length)];

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
