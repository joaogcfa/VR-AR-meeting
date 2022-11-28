using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Humanoid_Behavior : MonoBehaviour
{
    public GameObject human;
    public GameObject pointA;
    public GameObject pointB;
    public string targetPoint;
    private float step;
    private float speed;
    private Vector3 alturaY;

    void Start()
    {
        pointA = this.gameObject.transform.GetChild(0).gameObject;
        pointB = this.gameObject.transform.GetChild(1).gameObject;
        human = this.gameObject.transform.GetChild(2).gameObject;
        targetPoint = "B";
        speed = 1f;
    }

    // Update is called once per frame
    void Update()
    {
        step =  speed * Time.deltaTime; // calculate distance to move

        if(targetPoint == "B"){
            human.transform.position = Vector3.MoveTowards(human.transform.position, pointB.transform.position, step);
            print(Vector3.Distance(human.transform.position, pointB.transform.position));
            if (Vector3.Distance(human.transform.position, pointB.transform.position) < 1.1f)
            {
                targetPoint = "A";
            }
            
        }

        else if (targetPoint == "A"){
            human.transform.position = Vector3.MoveTowards(human.transform.position, pointA.transform.position, step);
            if (Vector3.Distance(human.transform.position, pointA.transform.position) < 1.1f)
            {
                targetPoint = "B";
            }
        }

        alturaY = new Vector3 (human.transform.position.x,-1.1f,human.transform.position.z);
        human.transform.position = alturaY;
    }

        
        
    
}
