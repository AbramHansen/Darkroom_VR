using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolScript : MonoBehaviour
{
    private float radius;
    public Vector3 centerPoint;
    public Vector3 topPoint;
    public Vector3 bottomPoint;
    public Vector3 leftPoint;
    public Vector3 rightPoint;


    // Start is called before the first frame update
    void Start()
    {
        radius = 0.1f;
        transform.localScale = new Vector3(radius, 0.002f, radius);

    }

    // Update is called once per frame
    void Update()
    {
        transform.localScale = new Vector3(radius, 0.002f, radius);
    }

    public void setParent(Transform parent)
    {
        transform.SetParent(parent);
    }

    public void increaseScale()
    {
        radius += 0.01f;
    }

    public void decreaseScale()
    {
        radius -= 0.01f;
    }
}
