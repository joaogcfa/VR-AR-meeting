using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class FollowHand : MonoBehaviour
{
    public GameObject handToFollow;
    private PhotonView view;

    void Start()
    {
        view = this.GetComponent<PhotonView>();
    }

    // Update is called once per frame
    void Update()
    {
        if (this.view.IsMine){
            this.transform.position =  handToFollow.transform.position;
            this.transform.rotation = handToFollow.transform.rotation;
        }
    }
}
