using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChildCount : MonoBehaviour
{
    public int childCount;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        childCount = this.transform.childCount;
    }
}
