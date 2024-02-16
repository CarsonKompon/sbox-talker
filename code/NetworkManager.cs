using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Sandbox;
using Sandbox.Network;
using Sandbox.Citizen;
using System.Linq;

public sealed class NetworkManager : Component, Component.INetworkListener
{
	public static NetworkManager Instance { get; private set; }

	/// <summary>
	/// Create a server (if we're not joining one)
	/// </summary>
	[Property] public bool StartServer { get; set; } = true;

	/// <summary>
	/// The prefab to spawn for the player to control.
	/// </summary>
	[Property] public GameObject PlayerPrefab { get; set; }

	[Property] public GameObject FreezePrefab { get; set; }

	public List<Connection> Connections = new();
	public Connection Host = null;
	[Sync] public long HostSteamId { get; set; }

	public List<PlayerController> Players => GameManager.ActiveScene.Components.GetAll<PlayerController>(FindMode.EnabledInSelfAndDescendants).ToList();

	protected override void OnAwake()
	{
		base.OnAwake();

		Instance = this;
	}

	protected override async Task OnLoad()
	{
		if (Scene.IsEditor)
			return;

		if (StartServer && !GameNetworkSystem.IsActive)
		{
			LoadingScreen.Title = "Creating Lobby";
			await Task.DelayRealtimeSeconds(0.1f);
			GameNetworkSystem.CreateLobby();

			if (MainMenuPanel.IsPrivateLobby)
			{
				await Task.DelayRealtimeSeconds(0.5f);
				GameNetworkSystem.Disconnect();
			}
		}
	}


	/// <summary>
	/// A client is fully connected to the server. This is called on the host.
	/// </summary>
	public void OnActive(Connection channel)
	{
		Log.Info($"Player '{channel.DisplayName}' has joined the game");

		Connections.Add(channel);

		if (Connections.Count == 1)
		{
			Host = channel;
			HostSteamId = (long)channel.SteamId;

			if (MainMenuPanel.IsPrivateLobby)
				Scene.Title = "PRIVATE_LOBBY";
			else if (!string.IsNullOrEmpty(LaunchArguments.Map))
				Scene.Title = LaunchArguments.Map;
		}

		if (PlayerPrefab is null)
			return;

		SpawnPlayer(channel);

		if (!MainMenuPanel.IsPrivateLobby)
			SendMap(Scene.Title);
	}

	[Broadcast]
	public void SendMap(string mapName)
	{
		var map = Scene.Components.Get<MapInstance>(FindMode.EnabledInSelfAndDescendants);
		if (map is not null) map.MapName = mapName;
	}

	public async void SpawnPlayer(Connection channel)
	{
		var map = Scene.Components.Get<MapInstance>(FindMode.EnabledInSelfAndDescendants);
		while (!map.IsLoaded)
			await GameTask.DelayRealtimeSeconds(1f);

		var startLocation = Transform.World;
		var spawnPoints = Scene.GetAllComponents<SpawnPoint>().ToList();
		if (spawnPoints.Count > 0)
		{
			startLocation = spawnPoints[Random.Shared.Int(0, spawnPoints.Count - 1)].Transform.World;
		}

		startLocation.Scale = 1;

		// Spawn this object and make the client the owner
		var player = PlayerPrefab.Clone(startLocation, name: $"Player - {channel.DisplayName}");
		player.NetworkSpawn(channel);

		var playerController = player.Components.Get<PlayerController>(FindMode.EverythingInSelfAndDescendants);
		playerController.SetName(channel.DisplayName);

		// var clothing = new ClothingContainer();
		// clothing.Deserialize(channel.GetUserData("avatar"));
		// if (player.Components.TryGet<SkinnedModelRenderer>(out var body, FindMode.EverythingInSelfAndDescendants))
		// {
		// 	clothing.Apply(body);
		// }
	}

	public void OnDisconnected(Connection channel)
	{
		foreach (var player in Players)
		{
			if (player.SteamId == (long)channel.SteamId)
			{
				player.GameObject.Destroy();
			}
		}

		Connections.Remove(channel);
	}

	public void OnBecameHost(Connection previousHost)
	{
		foreach (var player in Players)
		{
			if (player.SteamId == (long)previousHost.SteamId)
			{
				player.GameObject.Destroy();
			}
		}

		Host = Connections.FirstOrDefault(x => x.SteamId == (ulong)Game.SteamId);
		HostSteamId = (long)Game.SteamId;

		Log.Info("You are now the host!");
	}
}
