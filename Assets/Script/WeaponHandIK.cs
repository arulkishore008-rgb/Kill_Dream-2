
using UnityEngine;
using UnityEngine.Animations.Rigging;

[DefaultExecutionOrder(200)]
public class WeaponHandIK : MonoBehaviour
{
    public TwoBoneIKConstraint leftArmIK;
    public Transform leftHandTarget;
    public float blendSpeed = 8f;

    private Transform currentGrip;

    public void OnGunEquipped(EquippableGun gun)
    {
        currentGrip = null;

        if (gun != null)
        {
            Transform[] children =
                gun.GetComponentsInChildren<Transform>(true);

            foreach (Transform child in children)
            {
                if (child.name == "LeftHandGrip")
                {
                    currentGrip = child;
                    break;
                }
            }
        }

        if (currentGrip != null && leftHandTarget != null)
        {
            leftHandTarget.SetPositionAndRotation(
                currentGrip.position,
                currentGrip.rotation
            );
        }
    }

    private void LateUpdate()
    {
        if (leftArmIK == null || leftHandTarget == null)
            return;

        if (currentGrip != null)
        {
            leftHandTarget.SetPositionAndRotation(
                currentGrip.position,
                currentGrip.rotation
            );
        }

        float targetWeight = currentGrip != null ? 1f : 0f;

        leftArmIK.weight = Mathf.MoveTowards(
            leftArmIK.weight,
            targetWeight,
            blendSpeed * Time.deltaTime
        );
    }
}
