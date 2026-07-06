using UnityEngine;

public class doorscript : MonoBehaviour
{
    public float speed;
    public static doorscript instance;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        instance = this;
    }

    // Update is called once per frame
    void Update()
    {

        
    }
   public void doormove()
    {
        transform.Translate(Vector3.up * speed * Time.deltaTime);
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "bullet")
        {
            doormove();
        }
    }
}
