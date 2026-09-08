using UnityEngine;

public struct NoiseEvent 
{
    public Vector3 position;
    public float radius;
    public GameObject Source;


    public NoiseEvent(Vector3 position , float radius , GameObject Source)
    {
        this.position = position;
        this.radius = radius;
        this.Source = Source;
    }
    
}
