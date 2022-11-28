using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class FollowCamera : MonoBehaviour
{
    public GameObject cam;
    public bool isHead;
    private Vector3 pos;

    private PhotonView view;

    // Start is called before the first frame update
    void Start()
    {
        
        view = this.GetComponent<PhotonView>();

    }

    // Update is called once per frame
    void Update()
    {

        if (this.view.IsMine){
            if(isHead){
                pos = new Vector3(cam.transform.position.x,cam.transform.position.y,cam.transform.position.z);
                this.transform.rotation = cam.transform.rotation;  //Quaternion.Euler(transform.rotation.x, transform.rotation.y, camera.transform.rotation.x);
                this.transform.position = pos;
            }
            else{
                pos = new Vector3(cam.transform.position.x,cam.transform.position.y - 0.5f,cam.transform.position.z);
                this.transform.rotation = Quaternion.Euler(this.transform.eulerAngles.x, cam.transform.eulerAngles.y, this.transform.eulerAngles.z);
                this.transform.position = pos;
            }

        }
        
    }
}
