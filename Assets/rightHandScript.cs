using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class rightHandScript : MonoBehaviour
{
    //tool
    public GameObject tool;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        tool.GetComponent<ToolScript>().setParent(this.transform);
    }
}
