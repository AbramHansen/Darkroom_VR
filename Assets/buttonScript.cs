using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class buttonScript : MonoBehaviour
{
    private bool pressed = false;
    private string directory = "";

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerExit(Collider collider)
    {
        pressed = true;
    }

    public bool hasBeenPressed()
    {
        return pressed;
    }

    public void reset()
    {
        pressed = false;
    }

    public void setDir(string dir)
    {
        directory = dir;
    }

    public string getDir()
    {
        return directory;
    }
}
