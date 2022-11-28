using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class FollowCamera : MonoBehaviour
{
    public GameObject camera;
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
                pos = new Vector3(camera.transform.position.x,camera.transform.position.y,camera.transform.position.z);
                this.transform.rotation = camera.transform.rotation;  //Quaternion.Euler(transform.rotation.x, transform.rotation.y, camera.transform.rotation.x);
                this.transform.position = pos;
            }
            else{
                pos = new Vector3(camera.transform.position.x,camera.transform.position.y - 0.5f,camera.transform.position.z);
                this.transform.rotation = Quaternion.Euler(this.transform.eulerAngles.x, camera.transform.eulerAngles.y, this.transform.eulerAngles.z);
                this.transform.position = pos;
            }

        }
        
    }
}
