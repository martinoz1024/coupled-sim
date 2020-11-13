﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Singleton which exists throughout all scenes.
public class PersistentManager : MonoBehaviour
{
    public static PersistentManager Instance { get; private set; }

    public bool nextScene;
    public bool stopLogging;
    public int experimentnr;
    public bool _StopWithEyeGaze;
    public bool _visualizeGaze;
    public bool _visualizeGaze_client;

    public List<int> ExpOrder;
    public bool createOrder = true;
    public int listNr = 0;
    public int ParticipantNr;

    public int hostRole;
    public int clientRole;
    public bool clientConnectedToHost;
    public int client_nextScene;

    public bool ClientClosed;
    public bool SendEndGameToClient;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
