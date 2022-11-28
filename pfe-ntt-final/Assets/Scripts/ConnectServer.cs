using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ConnectServer : MonoBehaviourPunCallbacks
{

    public Text statusText;

    void Start()
    {
        PhotonNetwork.ConnectUsingSettings();
        print("1");
    }

    void Update(){
        // print(PhotonNetwork.NetworkClientState);
        statusText.text = PhotonNetwork.NetworkClientState.ToString();
    }

    public override void OnConnectedToMaster(){
        PhotonNetwork.JoinLobby();
        print("2");
    }

    public override void OnJoinedLobby(){
        
        #if UNITY_STANDALONE_WIN
            print("WINDOWS");
            PhotonNetwork.JoinRoom("pfe-ntt-room1");

        #endif

        #if UNITY_LUMIN
            print("Lumen");
            PhotonNetwork.CreateRoom("pfe-ntt-room1");

        #endif

        //PhotonNetwork.JoinRoom("pfe-ntt-room");
        //PhotonNetwork.CreateRoom("pfe-ntt-room");
    }

    public override void OnJoinedRoom(){
        print("Going to meshing guide");
        PhotonNetwork.LoadLevel("MeshingGuide");
        print("5");
    }
}
