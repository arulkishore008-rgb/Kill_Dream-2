using UnityEngine;

public class bullet : MonoBehaviour
{
    Rigidbody rb;
    [SerializeField]
    public float speed;
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }
    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }
}
