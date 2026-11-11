using UnityEngine;

namespace KillDream
{
    // Optional bridge for the existing enemy damage interfaces. Health remains
    // owned by fpsmovement; this component does not introduce another health value.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(fpsmovement))]
    public sealed class KDEnemyDamageReceiver : MonoBehaviour, IEnemyRangedDamageable, IEnemyMeleeDamageable
    {
        private fpsmovement movement;

        private void Awake()
        {
            movement = GetComponent<fpsmovement>();
        }

        public void ReceiveEnemyRangedDamage(float damage, Vector3 hitPoint, Vector3 direction)
        {
            ApplyDamage(damage);
        }

        public void ReceiveEnemyMeleeDamage(float damage, Vector3 hitPoint)
        {
            ApplyDamage(damage);
        }

        private void ApplyDamage(float damage)
        {
            if (movement == null) movement = GetComponent<fpsmovement>();
            movement.TakeDamage(damage);
        }
    }
}
