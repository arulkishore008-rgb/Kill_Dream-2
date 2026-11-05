using UnityEngine;

public enum WeaponAnimationStyle
{
    Unarmed = 0,
    Pistol = 1,
    Rifle = 2,
    Heavy = 3
}

// Attach this alongside EquippableGun on each weapon hand prefab.
// Weapons that share a stance should share the same animation style.
[DisallowMultipleComponent]
public class WeaponAnimationProfile : MonoBehaviour
{
    public WeaponAnimationStyle style = WeaponAnimationStyle.Rifle;
}
