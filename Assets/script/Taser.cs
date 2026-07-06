using Unity.VisualScripting;
using UnityEngine;
using static Interfaces;

public class Taser : MonoBehaviour , Iweapon
{
    public float raylength;
    void Start()
    {
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            Taserr();
        }
    }
    private void Taserr()
    {
        GameObject camHolder = GameObject.FindGameObjectWithTag("MainCamera");

        RaycastHit hit;

        if (Physics.Raycast(camHolder.transform.position, camHolder.transform.forward, out hit, raylength))
        {
            Ishockable shockable = hit.collider.GetComponent<Ishockable>();

            if (shockable != null)
            {
                shockable.Recieveshock(10f);
            }

        }

    }
    public void useweapon()
    {
        Debug.Log("gun fire");
    }

}
