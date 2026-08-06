using UnityEngine;

public class CamHolder : MonoBehaviour
{
    Vector3 startPos;
    public float bobSpeed;
    public float bobAmount;
    public float Timer;
    void Start()
    {
        startPos = transform.localPosition;
    }

    void Update()
    {
        if (Input.GetAxis("Horizontal") > .1f)
        {
            Timer += Time.deltaTime;
            float y = Mathf.Sin(Timer) * bobAmount;
            transform.localPosition = startPos + Vector3.up * y;
        }
    }
}
