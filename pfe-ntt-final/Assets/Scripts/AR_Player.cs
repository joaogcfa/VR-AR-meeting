using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class AR_Player : MonoBehaviour
{

    PhotonView view;

    public GameObject child;
    // Start is called before the first frame update
    void Start()
    {
        // creating view
         view = this.GetComponent<PhotonView>();
         if (!this.view.IsMine){
            child.SetActive(false);
        }            
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
