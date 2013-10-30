﻿using System.Collections.Generic;
using UnityEngine;

public class HostList : MonoBehaviour
{
    public delegate void SelectionChangedHandler(ServerInfo item);

    public event SelectionChangedHandler SelectionChanged;
    
    private List<dfControl> _items = new List<dfControl>();

    private dfControl _container;
    private dfControl _root;
    private dfPanel _noHosts;
    private NetworkManager _networkManager;

    public ServerInfo SelectedItem { get; private set; }

    private void Awake()
    {
        _container = GetComponent<dfControl>();
        if (_container == null) return;
        _root = _container.GetRootContainer();
        _networkManager = GameObject.FindGameObjectWithTag(Tags.networkManager).GetComponent<NetworkManager>();
        _noHosts = _container.Find<dfPanel>("panel_nohosts");
        DisableList();
    }

    public void StartSearching()
    {
        _networkManager.ServerListUpdated += CreateHostList;
        _networkManager.StartSearching();
    }

    public void StopSearching()
    {
        _networkManager.ServerListUpdated -= CreateHostList;
        _networkManager.StopBroadcastSession();
    }

    private void CreateHostList()
    {  
        List<ServerInfo> hosts = _networkManager.ServerList;
        RemoveItems();
        if(_items.Count >0) return;
        if (hosts.Count == 0)
        {
            DisableList();
            return;
        }
        if (_container == null) return;
        EnableList();

        for (int i = 0; i < hosts.Count; i++)
        {
            ServerInfo info = hosts[i];
            GameObject itemGameObject = dfPoolManager.Pool["Server"].Spawn(false);
            itemGameObject.hideFlags = HideFlags.DontSave;
            itemGameObject.transform.parent = _container.transform;
            itemGameObject.SetActive(true);

            HostListItem listItem = itemGameObject.GetComponent<HostListItem>();
            dfControl item = itemGameObject.GetComponent<dfControl>();

            if (listItem != null)
            {
                listItem.Bind(info);
            }
           
            item.ZOrder = _items.Count;
            item.Show();

            _items.Add(item);

            InitializeItemEvents(info, item);

            if (i == 0)
            {
                item.Focus();
            }
        }

    }

    private void InitializeItemEvents(ServerInfo info, dfControl item)
    {
        item.EnterFocus += (sender, args) =>
            {
                SelectedItem = info;
                if (SelectionChanged != null) SelectionChanged(info);
                _networkManager.TargetServer = info;
            };
    }

    private void RemoveItems()
    {
            for ( int i = _items.Count-1; i>=0;i--)
            {
                dfControl item = _items[i];
                if(item != null)
                    dfPoolManager.Pool["Server"].Despawn(item.gameObject);
            }

    _items.Clear();
    }

    private void DisableList()
    {
        _noHosts.Show();


    }
    private void EnableList()
    {
        _noHosts.Hide();
    }
}
