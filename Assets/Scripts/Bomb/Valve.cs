using System.Collections.Generic;
using System.Linq;
using System.Xml;
using UnityEngine;

[RequireComponent(typeof(NetworkView))]
public class Valve : MonoBehaviour
{


    public class ValveOccupant
    {
        public int player;
        public int minionCount;
        public float productivity;
        public int team;
    }
    public enum ValveState { Closed, Opened, FullyOccupied, NotFullyOccupied, NotOccupied }

    public const float _openValve = 100.0f;
    public const int _maxMinionCount = 5;

    public Team _valveTeam;
    public float _state = 100.0f;

    public Team.TeamIdentifier _occupant;
    public List<ValveOccupant> _occupants;
    public float _productivity = 0.0f;
    public int _minionCount = 0;


    // testing
    public ValveState currentState;
    // *********************

    public int _localPlayerID = -1;
    public int _localTeam = 0;
    public List<MinionAgent> _localMinions;
    public float _localProductivity = 0.0f;
    public int _localMinionCount;
    private AudioLibrary soundLibrary;
    private XmlDocument document;
    private ValveState lastState;


    public float State
    {
        get
        {
            return _state;
        }
    }

    // Use this for initialization
    private void Awake()
    {
        soundLibrary = transform.FindChild("sound_Valve").GetComponent<AudioLibrary>();
        _valveTeam = gameObject.GetComponent<Team>();
        _occupant = _valveTeam.ID;
        _localMinions = new List<MinionAgent>();
        _occupants = new List<ValveOccupant>();
        document = new XMLReader("Minion.xml").GetXML();
        lastState = ValveState.Opened;
    }

    void FindLocalPlayerID()
    {
        NetworkPlayerController netPlayer = GameObject.FindGameObjectWithTag(Tags.localPlayerController).GetComponent<LocalPlayerController>().networkPlayerController;
        if (netPlayer == null) return;
        _localPlayerID = netPlayer.playerID;
        _localTeam = netPlayer.team;
    }

    // Update is called once per frame
    void Update()
    {
        if (networkView.isMine && _localMinions.Count > 0 && !soundLibrary.aSources[XmlExtract("valve")].isPlaying) PlaySound(XmlExtract("valve"));
        if (networkView.isMine && _localMinions.Count == 0 && soundLibrary.aSources[XmlExtract("valve")].isPlaying) StopSound(XmlExtract("valve"));
        if (_localPlayerID == -1)
            FindLocalPlayerID();
        RemoveDeadMinions();
        CheckLocalMinionCount();
        CheckLocalProductivity();
        UseValve();

        currentState = GetValveState();
        if (!networkView.isMine || lastState == GetValveState()) return;
        if (GetValveState() != ValveState.Opened && GetValveState() != ValveState.Closed) return;
        PlayValveSound();
        lastState = GetValveState();
    }

    private string XmlExtract(string key)
    {
        return document.GetElementsByTagName(key)[0].InnerText;
    }

    private void UseValve()
    {
        if (!networkView.isMine) return;
        if (_valveTeam.isOwnTeam(_occupant))
            _state = Mathf.Clamp(_state + (_productivity * Time.deltaTime), 0f, _openValve);
        else if (_valveTeam.isEnemy(_occupant))
            _state = Mathf.Clamp(_state - (_productivity * Time.deltaTime), 0f, _openValve);
    }

    private void RemoveDeadMinions()
    {
        for (int i = _localMinions.Count - 1; i >= 0; i--)
        {
            if (_localMinions[i] == null)
                _localMinions.RemoveAt(i);
            else if (!_localMinions[i].GetComponent<Health>().IsAlive())
                _localMinions.RemoveAt(i);
        }
    }

    private void CheckLocalMinionCount()
    {
        if (_localMinionCount == _localMinions.Count) return;
        _localMinionCount = _localMinions.Count;
        if (networkView.isMine)
            SubmitLocalMinionCount(_localMinionCount, _localPlayerID, _localTeam);
        else
            networkView.RPC("SubmitLocalMinionCount", networkView.owner, _localMinionCount, _localPlayerID, _localTeam);
    }

    private void CheckLocalProductivity()
    {
        float tempProductivity = _localMinions.Sum(minion => minion.productivity); //sum of all localminions productivities
        if (tempProductivity == _localProductivity) return;
        _localProductivity = tempProductivity;
        if (networkView.isMine)
            SubmitLocalProductivity(_localProductivity, _localPlayerID);
        else
            networkView.RPC("SubmitLocalProductivity", networkView.owner, _localProductivity, _localPlayerID);
    }

    public bool stateComplete(MinionAgent minion)
    {
        return ((DoesValveBelongTo(minion) && GetValveState() == ValveState.Opened)
                || (!DoesValveBelongTo(minion) && GetValveState() == ValveState.Closed));
    }

    public bool isAvailable(MinionAgent minion)
    {
        return !(stateComplete(minion)
            || (_localMinions.Any(minionAgent => minionAgent == minion))
            || GetValveState() == ValveState.FullyOccupied
            || (_valveTeam.isEnemy(_occupant) && GetValveState() == ValveState.NotFullyOccupied));
    }

    public void AddMinion(MinionAgent minion)
    {
        DebugStreamer.message = "addMinion";
        if (_localMinions.Any(minionAgent => minionAgent == minion)) return;
        _localMinions.Add(minion);
    }

    public void RemoveMinion(MinionAgent minion)
    {
        DebugStreamer.message = "removeMinion";
        _localMinions.RemoveAll(minionAgent => minionAgent == minion);
    }


    public ValveState GetValveState()
    {
        if (_state <= 0.0f)
        {
            return ValveState.Closed;
        }
        if (_state >= _openValve)
        {
            return ValveState.Opened;
        }

        if (_minionCount > 0)
        {
            return _minionCount < _maxMinionCount ? ValveState.NotFullyOccupied : ValveState.FullyOccupied;
        }
        return ValveState.NotOccupied;
    }

    public int GetRotationDirection()
    {
        ValveState state = GetValveState();
        if (new[] { ValveState.Closed, ValveState.Opened, ValveState.NotOccupied }.Contains(state))
            return 0;
        if (_valveTeam.isOwnTeam(_occupant))
            return 1;
        return -1;
    }

    private bool DoesValveBelongTo(MinionAgent minion)
    {
        return _valveTeam.isOwnTeam(minion.gameObject.GetComponent<Team>());
    }

    public virtual void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info)
    {
        stream.Serialize(ref _state);
    }



    //Only called on Server
    [RPC]
    public void SubmitLocalMinionCount(int count, int playerID, int team)
    {
        if (!networkView.isMine)
            return;
        ValveOccupant submittedOccupant = null;
        foreach (ValveOccupant valveOccupant in _occupants)
        {
            if (valveOccupant.player == playerID)
            {
                submittedOccupant = valveOccupant;
            }
        }
        if (submittedOccupant != null)
        {
            if (count == 0)
                _occupants.Remove(submittedOccupant);
            else
            {
                submittedOccupant.minionCount = count;
            }
        }
        else if (count != 0)
        {
            submittedOccupant = new ValveOccupant();
            submittedOccupant.minionCount = count;
            submittedOccupant.player = playerID;
            submittedOccupant.team = team;
            _occupants.Add(submittedOccupant);
            if (_occupants.Count == 1)
            {
                UpdateOccupant(team);
                networkView.RPC("UpdateOccupant", RPCMode.OthersBuffered, team);
            }
        }
        else
            return;

        _minionCount = _occupants.Sum(minion => minion.minionCount);
        _productivity = _occupants.Sum(minion => minion.productivity);
        networkView.RPC("UpdateMinionCount", RPCMode.OthersBuffered, _minionCount);
    }

    [RPC]
    public void SubmitLocalProductivity(float productivity, int playerID)
    {
        if (!networkView.isMine)
            return;
        ValveOccupant submittedOccupant = null;
        foreach (ValveOccupant valveOccupant in _occupants)
        {
            if (valveOccupant.player == playerID)
            {
                submittedOccupant = valveOccupant;
            }
        }
        if (submittedOccupant != null)
            submittedOccupant.productivity = productivity;
    }

    [RPC]
    public void UpdateMinionCount(int globalMinionCount)
    {
        _minionCount = globalMinionCount;
    }

    [RPC]
    public void UpdateOccupant(int team)
    {
        _occupant = (Team.TeamIdentifier)team;
    }

    /// <summary>
    /// Tries to play sound.
    /// </summary>
    /// <param name="name">Name of the Sound file, should be extracted from an XML!</param>
    public void PlayValveSound()
    {
        networkView.RPC("StartValveSound", RPCMode.All);
    }

    [RPC]
    public void StartValveSound()
    {
        foreach (GameObject player in GameObject.FindGameObjectsWithTag(Tags.player).Where(player => player.networkView.isMine))
        {
            if (player.GetComponent<Team>().ID != GetComponent<Team>().ID)
            {
                GameObject.Find("sounds_Vocal")
                          .GetComponent<AudioLibrary>()
                          .StartSound(
                              GetValveState() == ValveState.Opened
                                  ? new XMLReader("GameSettings.xml").GetXML().GetElementsByTagName("valveLost")[0]
                                        .InnerText
                                  : new XMLReader("GameSettings.xml").GetXML().GetElementsByTagName("valveConquered")[0]
                                        .InnerText, 0f);
            }
            else
            {
                GameObject.Find("sounds_Vocal")
                          .GetComponent<AudioLibrary>()
                          .StartSound(
                              GetValveState() == ValveState.Opened
                                  ? new XMLReader("GameSettings.xml").GetXML().GetElementsByTagName("valveConquered")[0]
                                        .InnerText
                                  : new XMLReader("GameSettings.xml").GetXML().GetElementsByTagName("valveLost")[0]
                                        .InnerText, 0f);
            }
        }
    }

    /// <summary>
    /// Tries to play sound.
    /// </summary>
    /// <param name="name">Name of the Sound file, should be extracted from an XML!</param>
    public void PlaySound(string name, float delay = 0f)
    {
        networkView.RPC("StartSound", RPCMode.All, name, delay);
    }

    [RPC]
    public void StartSound(string name, float delay)
    {
        if (soundLibrary == null)
            soundLibrary = transform.FindChild("sound_Valve").GetComponent<AudioLibrary>();
        soundLibrary.StartSound(name, delay);
    }

    /// <summary>
    /// Tries to stop sound.
    /// </summary>
    /// <param name="name">Name of the Sound file, should be extracted from an XML!</param>
    public void StopSound(string name)
    {
        networkView.RPC("EndSound", RPCMode.All, name);
    }

    [RPC]
    public void EndSound(string name)
    {
        if (soundLibrary == null)
            soundLibrary = transform.FindChild("sound_Valve").GetComponent<AudioLibrary>();
        soundLibrary.StopSound(name);
    }
}
