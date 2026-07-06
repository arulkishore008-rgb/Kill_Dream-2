using UnityEngine;

public class weaponshift : MonoBehaviour
{
    public int selectedweapon = 0;
    void Start()
    {
        selectweapon();
    }

    void Update()
    {
        int previous_selectedweapon = selectedweapon; 

        if (Input.GetAxis("Mouse ScrollWheel") > 0f)
        {
            if (selectedweapon >= transform.childCount -1)
                selectedweapon = 0;
            else
                selectedweapon++;
        }
        if (Input.GetAxis("Mouse ScrollWheel") < 0f)
        {
            if (selectedweapon <= 0f)
                selectedweapon = transform.childCount - 1;
            else
                selectedweapon--;
        }

        if(previous_selectedweapon != selectedweapon)
        {
            selectweapon();
        }
    }
     void selectweapon()
    {
        int i = 0;
        foreach (Transform weapon in transform)
        {
            if (i == selectedweapon)
                weapon.gameObject.SetActive(true);
            else 
                weapon.gameObject.SetActive(false);

                i++;
        }
    }
}
