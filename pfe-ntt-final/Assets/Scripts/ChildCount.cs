using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChildCount : MonoBehaviour
{
    public int childCount;
    public int totalTriangles;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        childCount = this.transform.childCount;
        totalTriangles = 0;
        foreach(Transform child in transform){
            totalTriangles += (child.GetComponent<MeshFilter>().mesh.GetTriangles(0).Length)/3;
        }
    }
}
