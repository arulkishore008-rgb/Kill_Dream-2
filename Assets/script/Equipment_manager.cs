using UnityEditor;
using UnityEngine;
using static Interfaces;

public class Equipment_manager : MonoBehaviour
{
    public Transform WeaponHolder;
    private GameObject CurrentWeapon;
    public Iweapon CurrentWeaponScript;




    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (CurrentWeaponScript != null)
            {
                CurrentWeaponScript.useweapon();
            }
        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            Unequip();
        }
    }
    public void EquipItem(Items ItemtoEquip)
    {


        if (ItemtoEquip == null || ItemtoEquip.weapon == null )  return;


        Unequip();

        if (ItemtoEquip.weapon == null)
        {
            return;
        }

        if (CurrentWeapon != null)
        {
            Destroy(CurrentWeapon);
        }

        CurrentWeapon = Instantiate(ItemtoEquip.weapon, WeaponHolder.position, WeaponHolder.rotation);

        CurrentWeapon.transform.SetParent(WeaponHolder);

        CurrentWeaponScript = CurrentWeapon.GetComponent<Iweapon>();

        Debug.Log("Equippped + " + ItemtoEquip.name);

    }

   void Unequip()
    {
        if (CurrentWeapon != null)
        {
            Destroy(CurrentWeapon);
            CurrentWeaponScript = null;

        }
    }
}
