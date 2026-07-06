using UnityEditor;
using UnityEngine;

public class Equipment_manager : MonoBehaviour
{
    public Transform WeaponHolder;
    private GameObject CurrentWeapon;


    public void EquipItem(Items ItemtoEquip)
    {
        if (ItemtoEquip == null || ItemtoEquip.weapon == null)
        {
            return;
        }
        if (CurrentWeapon != null)
        {
            Destroy(CurrentWeapon);
        }

        CurrentWeapon = Instantiate(ItemtoEquip.weapon, WeaponHolder.position, WeaponHolder.rotation);

        CurrentWeapon.transform.SetParent(WeaponHolder);

        Debug.Log("Equippped + " + ItemtoEquip.name);

    }

   
}
