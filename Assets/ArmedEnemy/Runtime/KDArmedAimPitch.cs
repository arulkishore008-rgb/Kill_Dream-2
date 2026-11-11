using UnityEngine;
using UnityEngine.Rendering;

namespace KillDream
{
    // Cosmetic weapon presentation. ArmedEnemy/NavMeshAgent retain gameplay,
    // hit detection, damage, root translation and facing authority.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public sealed class KDArmedAimPitch : MonoBehaviour
    {
        [SerializeField] private ArmedEnemy enemy;
        [Range(0f, 45f)][SerializeField] private float maximumPitch = 25f;
        [Min(0f)][SerializeField] private float pitchSpeed = 120f;

        [Header("Weapon Binding")]
        [SerializeField] private Transform weaponRoot;
        [SerializeField] private Transform muzzle;
        [SerializeField] private Transform leftHandGrip;
        [SerializeField] private bool fitWeaponOnStart = true;
        [Tooltip("Hand-relative offset in metres, before the visual's scale.")]
        [SerializeField] private Vector3 weaponFitPosition;
        [SerializeField] private Quaternion weaponFitRotation = Quaternion.identity;
        [Min(0.0001f)][SerializeField] private float weaponFitScale = 10f;
        [Range(0f, 1f)][SerializeField] private float supportHandWeight = 1f;
        [Range(0f, 45f)][SerializeField] private float maximumWeaponAimCorrection = 20f;

        [Header("Shot Feedback")]
        [SerializeField] private bool showShotEffects = true;
        [Min(0.01f)][SerializeField] private float shotEffectDuration = 0.045f;

        private Animator animator;
        private Transform chest, rightHand, leftUpperArm, leftLowerArm, leftHand;
        private float pitch;
        private LineRenderer tracer;
        private Light flashLight;
        private GameObject flashSphere;
        private Material shotMaterial;
        private float shotVisibleUntil;

        public void Configure(ArmedEnemy owner) { enemy = owner; }

        public void ConfigureWeapon(ArmedEnemy owner, Transform weapon, Transform muzzlePoint,
            Transform supportGrip, Vector3 localFitPosition, Quaternion localFitRotation, float localFitScale)
        {
            enemy = owner;
            weaponRoot = weapon;
            muzzle = muzzlePoint;
            leftHandGrip = supportGrip;
            weaponFitPosition = localFitPosition;
            weaponFitRotation = localFitRotation;
            weaponFitScale = Mathf.Max(0.0001f, localFitScale);
            fitWeaponOnStart = true;
        }

        private bool CacheBones()
        {
            animator = GetComponent<Animator>();
            if (animator.avatar == null || !animator.avatar.isHuman || !animator.avatar.isValid)
                return false;
            chest = Bone(HumanBodyBones.UpperChest);
            if (chest == null) chest = Bone(HumanBodyBones.Chest);
            if (chest == null) chest = Bone(HumanBodyBones.Spine);
            rightHand = Bone(HumanBodyBones.RightHand);
            leftUpperArm = Bone(HumanBodyBones.LeftUpperArm);
            leftLowerArm = Bone(HumanBodyBones.LeftLowerArm);
            leftHand = Bone(HumanBodyBones.LeftHand);
            return rightHand != null && leftUpperArm != null && leftLowerArm != null && leftHand != null;
        }

        private Transform Bone(HumanBodyBones bone)
        {
            if (Application.isPlaying) return animator.GetBoneTransform(bone);
            // Edit-time fitting must work before a scene Animator is initialized.
            // Resolve the Avatar's authored mapping without evaluating its pose.
            string humanName = HumanTrait.BoneName[(int)bone];
            string modelName = null;
            foreach (HumanBone mapped in animator.avatar.humanDescription.human)
                if (mapped.humanName == humanName) { modelName = mapped.boneName; break; }
            if (string.IsNullOrEmpty(modelName)) return null;
            foreach (Transform candidate in GetComponentsInChildren<Transform>(true))
                if (candidate.name == modelName) return candidate;
            return null;
        }

        // Also used by the Editor finalizer. Never evaluates animation or writes
        // bone transforms while fitting the real scene character.
        public bool FitWeaponToHand()
        {
            if (weaponRoot == null || !CacheBones()) return false;
            if (weaponRoot.parent != rightHand) weaponRoot.SetParent(rightHand, false);
            float visualScale = Mathf.Abs(transform.lossyScale.x);
            float handScale = Mathf.Abs(rightHand.lossyScale.x);
            if (visualScale < 0.0001f || handScale < 0.0001f) return false;
            float units = visualScale / handScale;
            weaponRoot.localPosition = weaponFitPosition * units;
            weaponRoot.localRotation = weaponFitRotation;
            weaponRoot.localScale = Vector3.one * (weaponFitScale * units);
            return true;
        }

        private void Start()
        {
            if (enemy == null) enemy = GetComponentInParent<ArmedEnemy>();
            if (!CacheBones())
            {
                Debug.LogError("Armed weapon presentation requires a valid Humanoid Avatar and both arm chains.", this);
                enabled = false;
                return;
            }
            if (fitWeaponOnStart && weaponRoot != null && !FitWeaponToHand())
                Debug.LogError("Cannot fit the armed enemy weapon: check the visual and hand scale.", this);
            if (showShotEffects && muzzle != null) CreateShotEffects();
        }

        private void LateUpdate()
        {
            if (shotVisibleUntil > 0f && Time.time >= shotVisibleUntil)
            {
                HideShotEffects();
                shotVisibleUntil = 0f;
            }
            if (enemy == null || chest == null || !animator.isActiveAndEnabled || animator.speed <= 0f || Time.deltaTime <= 0f) return;
            if (enemy.IsDead() || enemy.DebugState == EnemyAI.Enemystate.HitReact)
            {
                pitch = 0f;
                return;
            }
            bool aiming = enemy.DebugState == EnemyAI.Enemystate.Attack && enemy.player != null;
            float wantedPitch = 0f;
            Vector3 target = Vector3.zero;
            if (aiming)
            {
                target = enemy.AimTargetPosition;
                Vector3 direction = target - chest.position;
                float horizontal = new Vector2(direction.x, direction.z).magnitude;
                wantedPitch = Mathf.Clamp(-Mathf.Atan2(direction.y, Mathf.Max(0.1f, horizontal)) * Mathf.Rad2Deg,
                    -maximumPitch, maximumPitch);
            }
            pitch = Mathf.MoveTowards(pitch, wantedPitch, pitchSpeed * Time.deltaTime);
            if (Mathf.Abs(pitch) > 0.001f)
                chest.rotation = Quaternion.AngleAxis(pitch, enemy.transform.right) * chest.rotation;

            if (weaponRoot == null || muzzle == null || weaponRoot.parent != rightHand) return;
            if (aiming)
            {
                Vector3 direction = target - muzzle.position;
                if (direction.sqrMagnitude > 0.001f)
                {
                    // Correct modest residual wrist/retargeting offsets. Large
                    // yaw changes belong to the existing AI's root rotation.
                    Quaternion correction = Quaternion.FromToRotation(muzzle.forward, direction.normalized);
                    correction = Quaternion.RotateTowards(Quaternion.identity, correction, maximumWeaponAimCorrection);
                    rightHand.rotation = correction * rightHand.rotation;
                }
            }
            if (leftHandGrip != null && supportHandWeight > 0f)
                MatchSupportHand(leftHandGrip.position, leftHandGrip.rotation, supportHandWeight);
            if (tracer != null && tracer.enabled) tracer.SetPosition(0, muzzle.position);
        }

        private void MatchSupportHand(Vector3 position, Quaternion rotation, float weight)
        {
            Vector3 shoulder = leftUpperArm.position;
            Vector3 elbow = leftLowerArm.position;
            Vector3 wrist = leftHand.position;
            Vector3 target = Vector3.Lerp(wrist, position, weight);
            float upperLength = Vector3.Distance(shoulder, elbow);
            float lowerLength = Vector3.Distance(elbow, wrist);
            if (upperLength < 0.0001f || lowerLength < 0.0001f) return;
            Vector3 delta = target - shoulder;
            float distance = delta.magnitude;
            if (distance < 0.0001f) return;
            Vector3 direction = delta / distance;
            float reach = Mathf.Clamp(distance, Mathf.Abs(upperLength - lowerLength) + 0.0001f,
                upperLength + lowerLength - 0.0001f);
            target = shoulder + direction * reach;
            Vector3 bend = Vector3.ProjectOnPlane(elbow - shoulder, direction);
            if (bend.sqrMagnitude < 0.000001f)
                bend = Vector3.ProjectOnPlane(-enemy.transform.right - enemy.transform.up, direction);
            if (bend.sqrMagnitude < 0.000001f)
                bend = Vector3.Cross(direction, enemy.transform.forward);
            if (bend.sqrMagnitude < 0.000001f) return;
            bend.Normalize();
            float along = (upperLength * upperLength - lowerLength * lowerLength + reach * reach) / (2f * reach);
            float height = Mathf.Sqrt(Mathf.Max(0f, upperLength * upperLength - along * along));
            Vector3 elbowTarget = shoulder + direction * along + bend * height;
            leftUpperArm.rotation = Quaternion.FromToRotation(elbow - shoulder, elbowTarget - shoulder) * leftUpperArm.rotation;
            leftLowerArm.rotation = Quaternion.FromToRotation(leftHand.position - leftLowerArm.position,
                target - leftLowerArm.position) * leftLowerArm.rotation;
            leftHand.rotation = Quaternion.Slerp(leftHand.rotation, rotation, weight);
        }

        public void ShowShot(Vector3 origin, Vector3 hitPoint)
        {
            if (!showShotEffects || !isActiveAndEnabled || muzzle == null) return;
            if (tracer == null) CreateShotEffects();
            if (tracer == null) return;
            tracer.SetPosition(0, origin);
            tracer.SetPosition(1, hitPoint);
            tracer.enabled = true;
            flashSphere.SetActive(true);
            flashLight.enabled = true;
            shotVisibleUntil = Time.time + shotEffectDuration;
        }

        private void CreateShotEffects()
        {
            if (tracer != null || muzzle == null) return;
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Sprites/Default");
            if (shader == null)
            {
                Debug.LogError("No compatible shader is available for armed enemy shot feedback.", this);
                showShotEffects = false;
                return;
            }
            shotMaterial = new Material(shader) { name = "KD_ArmedShot_Runtime" };
            Color colour = new Color(1f, 0.55f, 0.15f, 1f);
            shotMaterial.SetColor(shotMaterial.HasProperty("_BaseColor") ? "_BaseColor" : "_Color", colour);
            if (shotMaterial.HasProperty("_EmissionColor"))
            {
                shotMaterial.SetColor("_EmissionColor", colour * 4f);
                shotMaterial.EnableKeyword("_EMISSION");
            }
            var line = new GameObject("KD_ShotTracer");
            line.transform.SetParent(transform, false);
            tracer = line.AddComponent<LineRenderer>();
            tracer.sharedMaterial = shotMaterial;
            tracer.useWorldSpace = true;
            tracer.positionCount = 2;
            tracer.startWidth = 0.009f; tracer.endWidth = 0.002f;
            tracer.startColor = colour; tracer.endColor = colour;
            tracer.numCapVertices = 2;
            tracer.shadowCastingMode = ShadowCastingMode.Off;
            tracer.receiveShadows = false;
            tracer.enabled = false;

            flashSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flashSphere.name = "KD_MuzzleFlash";
            flashSphere.transform.SetParent(muzzle, false);
            flashSphere.transform.localScale = Vector3.one * (0.025f / Mathf.Max(0.0001f, Mathf.Abs(muzzle.lossyScale.x)));
            Collider flashCollider = flashSphere.GetComponent<Collider>();
            flashCollider.enabled = false;
            Destroy(flashCollider);
            flashSphere.GetComponent<Renderer>().sharedMaterial = shotMaterial;
            flashLight = flashSphere.AddComponent<Light>();
            flashLight.type = LightType.Point;
            flashLight.color = colour;
            flashLight.range = 1.5f;
            flashLight.intensity = 2f;
            flashLight.shadows = LightShadows.None;
            flashLight.enabled = false;
            flashSphere.SetActive(false);
        }

        private void HideShotEffects()
        {
            if (tracer != null) tracer.enabled = false;
            if (flashLight != null) flashLight.enabled = false;
            if (flashSphere != null) flashSphere.SetActive(false);
        }

        private void OnDisable() { HideShotEffects(); shotVisibleUntil = 0f; }
        private void OnDestroy()
        {
            if (shotMaterial != null) Destroy(shotMaterial);
            if (tracer != null) Destroy(tracer.gameObject);
            if (flashSphere != null) Destroy(flashSphere);
        }
    }
}
