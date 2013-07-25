using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
	public enum GameState { Starting, Running, Ended };

	private float _startTime;
	public GameState state;
	private bool gameEndet;
	private float passedTime = 0f;
	private List<NetworkPlayerController> _networkPlayerControllers = new List<NetworkPlayerController>();

	public NetworkPlayerController GetNetworkPlayerController(NetworkPlayer player)
	{
		foreach (NetworkPlayerController controller in _networkPlayerControllers)
		{
			if (controller.networkPlayer == player)
			{
				return controller;
			}
		}
		return null;
	}

	public float GameTime
	{
		get { return Time.time - _startTime; }
	}

	void OnPlayerDisconnected()
	{
		networkView.RPC("SetGameState", RPCMode.AllBuffered, (int)GameState.Ended);
	}

	void OnDisconnectedFromServer()
	{
		networkView.RPC("SetGameState", RPCMode.AllBuffered, (int)GameState.Ended);
	}

	void OnGUI()
	{
		if (gameEndet)
		{
			GUI.TextArea(new Rect(Screen.width * 0.5f, Screen.height * 0.5f, Screen.width * 0.2f, Screen.height * 0.1f),
			             "A player has been disconnected from the Game");
			Debug.Log("Has Disconnected");
		}
	}

	void Update()
	{
		if (gameEndet)
		{
			passedTime += Time.deltaTime;

			if (passedTime >= 3)
			{
				Application.Quit();
			}
		}
	}

	[RPC]
	void AddNetworkPlayerController(int playerID, string playerName, int Team, NetworkPlayer networkPlayer)
	{
		_networkPlayerControllers.Add(new NetworkPlayerController(playerID, playerName, Team, networkPlayer));
	}

	[RPC]
	void SetGameState(int state)
	{
		this.state = (GameState)state;
		if (this.state == GameState.Running)
		{
			_startTime = Time.time;
		}

		if (this.state == GameState.Ended)
		{
			gameEndet = true;
		}
	}
}
