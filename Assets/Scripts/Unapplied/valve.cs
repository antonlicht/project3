using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class valve : MonoBehaviour
{


	public class ValveOccupant
	{
        public int player;
        public int minionCount;
        public float productivity;
        public Team.TeamIdentifier team;
	}

	public enum ValveState { Closed, Opened, FullyOccupied, NotFullyOccupied, NotOccupied }
	private Team.TeamIdentifier occupant;
	private float productivity = 0.0f;
	private bool currentlyDecaying = false;
	private Team.TeamIdentifier team;

	private float _state = 0.0f;
	private List<MinionAgent> _localMinions;
	private List<ValveOccupant> _occupants;
	private float _localProductivity = 0.0f;
	private float _localProductivitySave = 0.0f;

	public float _openValve = 100.0f;
	public int _maxMinionCount = 5;

	public float State 
	{
		get
		{
			return _state;
		}
	}

    public int MinionCount
    {
        get { return GetMinionCount(); }
    }

	// Use this for initialization
	void Start ()
	{
		team = GetComponent<Team>().ID;

		_occupants.Add(new ValveOccupant());
	    _occupants[0].player = -89;
        _occupants[0].minionCount = 0;
        _occupants[0].productivity = 0;

        _occupants.Add(new ValveOccupant());
	    _occupants[1].minionCount = 0;
        _occupants[1].productivity = 0;
	}
	
	// Update is called once per frame
	void Update ()
	{

		if (networkView.isMine)
		{
			if (!currentlyDecaying)
			{
				_state += GetEntireProductivity();
			}
			else
			{
				_state -= GetEntireProductivity();
			}
		}


		_localProductivitySave = _localMinions.Sum(minion => minion.productivity); //sum of all localminions productivities
		if (_localProductivitySave != _localProductivity)
		{
			_localProductivity = _localProductivitySave;
			if (!networkView.isMine)
			{
				networkView.RPC("SubmitLocalProductivity", RPCMode.Server, _localProductivity, GetComponent<LocalPlayerController>().networkPlayerController.playerID);
			}
		}

		if (team != occupant)
		{
			currentlyDecaying = true;
		}

		if (_state <= 0.0f)
		{
			currentlyDecaying = false;
		}

	}

	public bool AddMinion(MinionAgent minion)
	{
		if (DoesValveBelongTo(minion) && GetValveState() == ValveState.Opened) //being occupied by team x but already fully opened, then minions from team x may not use it
		{
			return false;
		}
		if (occupant != team && (GetValveState() == ValveState.NotFullyOccupied || GetValveState() == ValveState.FullyOccupied)) //valve occupied by enemy team, and at least one enemy is at valve, minion may not use it
		{
			return false;
		}
		if (GetValveState() == ValveState.FullyOccupied)
		{
			return false;
		}
        if (!DoesValveBelongTo(minion) && GetValveState() == ValveState.Closed)
        {
            return false;
        }

		_localMinions.Add(minion);
		float localProductivity = _localMinions.Sum(mini => mini.productivity);

		if (!networkView.isMine)
			networkView.RPC("SubmitLocalMinionCount", RPCMode.Server, 
                _localMinions.Count, localProductivity, GetComponent<LocalPlayerController>().networkPlayerController.playerID, (int)minion.GetComponent<Team>().ID);
		else
			SubmitLocalMinionCount(_localMinions.Count, localProductivity, GetComponent<LocalPlayerController>().networkPlayerController.playerID, (int)minion.GetComponent<Team>().ID);

		return true;
	}

	public bool RemoveMinion(MinionAgent minion)
	{
		foreach (MinionAgent localMinion in _localMinions)
		{
			if (localMinion.gameObject == minion.gameObject)
			{
                _localMinions.Remove(localMinion);
                float localProductivity = _localMinions.Sum(mini => mini.productivity);
			    if (!networkView.isMine)
			        networkView.RPC("SubmitLocalMinionCount", RPCMode.Server, _localMinions.Count, localProductivity,
			                        GetComponent<LocalPlayerController>().networkPlayerController.playerID,
			                        (int) minion.GetComponent<Team>().ID);
			    else
			        SubmitLocalMinionCount(_localMinions.Count, localProductivity,
			                               GetComponent<LocalPlayerController>().networkPlayerController.playerID,
			                               (int) minion.GetComponent<Team>().ID);
				return true;
			}
		}
		return false;
	}

	public ValveState GetValveState()
	{
		if (_occupants.Count > 0)
		{
			if (_occupants.Count < _maxMinionCount)
			{
				return ValveState.NotFullyOccupied;
			}
			return ValveState.FullyOccupied;
		}

		if (_state <= 0.0f)
		{
			return ValveState.Closed;
		}
		if (_state >= _openValve)
		{
			return ValveState.Opened;
		}
		return ValveState.NotOccupied;
	}

	private bool DoesValveBelongTo(MinionAgent minion)
	{
		return team == minion.GetComponent<Team>().ID;
	}

	public virtual void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info)
	{
		stream.Serialize(ref _state);
	}

	private int GetMinionCount()
	{
        return _occupants[0].minionCount + _occupants[1].minionCount;
	}

	private float GetEntireProductivity()
	{
        return _occupants[0].productivity + _occupants[1].productivity;
	}


	[RPC]
	public void SubmitLocalMinionCount(int count, float productivity, int playerID, int team)
	{
		if (occupant == (Team.TeamIdentifier)team) //belongs to your team
		{
            if (_occupants[0].player == playerID)
            {
                _occupants[0].minionCount = count;
                _occupants[0].productivity = productivity;
			}
			else
            {
                _occupants[1].player = playerID;
                _occupants[1].minionCount = count;
                _occupants[1].productivity = productivity;
			}
		}
		else //does not belong to your team so it could have been used from AddMinion only
		{
            occupant = (Team.TeamIdentifier)team;

            _occupants[1].minionCount = 0;
            _occupants[1].productivity = 0;
            _occupants[1].team = (Team.TeamIdentifier)team;

            _occupants[0].player = playerID;
            _occupants[0].minionCount = count;
            _occupants[0].productivity = productivity;
            _occupants[0].team = (Team.TeamIdentifier) team;
			networkView.RPC("UpdateOccupant", RPCMode.OthersBuffered, team);
		}
        networkView.RPC("UpdateMinionCount", RPCMode.OthersBuffered, _occupants[0].minionCount, _occupants[1].minionCount);
	}

	[RPC]
	public void SubmitLocalProductivity(float productivity, int playerID)
	{
        if (_occupants[0].player == playerID)
		{
            _occupants[0].productivity = productivity;
		}
		else
		{
            _occupants[1].productivity = productivity;
		}
	}

	[RPC]
	public void UpdateMinionCount(int first, int second)
    {
        _occupants[0].minionCount = first;
        _occupants[1].minionCount = second;
	}

	[RPC]
	public void UpdateOccupant(int team)
	{
		occupant = (Team.TeamIdentifier)team;
	}
}
