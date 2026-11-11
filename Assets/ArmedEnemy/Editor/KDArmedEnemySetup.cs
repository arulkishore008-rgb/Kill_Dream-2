using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace KillDream.Editor
{
    public static class KDArmedEnemySetup
    {
        private static string cachedRoot;
        private static string Root
        {
            get
            {
                if (!string.IsNullOrEmpty(cachedRoot) &&
                    File.Exists(cachedRoot + "/Editor/KDArmedEnemySetup.cs") &&
                    File.Exists(cachedRoot + "/Character/Ch15_Optimized.fbx") &&
                    AssetDatabase.IsValidFolder(cachedRoot + "/Animations"))
                    return cachedRoot;

                // Resolve only when a menu runs, after Unity has imported the
                // scripts. The pack may live at Assets/ArmedEnemy or elsewhere.
                const string suffix = "/Editor/KDArmedEnemySetup.cs";
                string[] roots = AssetDatabase.FindAssets("KDArmedEnemySetup t:MonoScript")
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Where(p => p.StartsWith("Assets/", StringComparison.Ordinal) &&
                        p.EndsWith(suffix, StringComparison.Ordinal))
                    .Select(p => p.Substring(0, p.Length - suffix.Length))
                    .Where(p => File.Exists(p + "/Character/Ch15_Optimized.fbx") &&
                        AssetDatabase.IsValidFolder(p + "/Animations"))
                    .Distinct().ToArray();
                if (roots.Length != 1)
                    throw new InvalidOperationException("Expected one armed enemy pack with Editor/KDArmedEnemySetup.cs, Character/Ch15_Optimized.fbx and Animations in the same folder. Found " + roots.Length + ". Keep one complete pack under Assets.");
                cachedRoot = roots[0];
                return cachedRoot;
            }
        }
        private static string Generated { get { return Root + "/Generated"; } }
        private static string CharacterPath { get { return Root + "/Character/Ch15_Optimized.fbx"; } }
        private static string ControllerPath { get { return Generated + "/KD_ArmedEnemy.controller"; } }
        private static string PrefabPath { get { return Generated + "/KD_ArmedEnemy.prefab"; } }
        private static readonly string[] ClipNames = {
            "RifleIdle", "AimIdle", "PatrolWalk", "ChaseRun", "InvestigateWalk", "SearchLookAround", "Alert",
            "CombatForward", "CombatBackward", "StrafeLeft", "StrafeRight", "Fire", "HitReact", "Death", "GrenadeDeath"
        };
        private static readonly HashSet<string> Once = new HashSet<string> { "Alert", "Fire", "HitReact", "Death", "GrenadeDeath" };

        [MenuItem("Tools/Kill Dream/Armed Enemy/1. Build Animation Assets")]
        public static void BuildAssets()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("Stop Play Mode before building enemy assets.");
                return;
            }
            try
            {
                EnsureFolder(Generated + "/Materials");
                AssetDatabase.Refresh();
                Progress("Configuring the character Avatar", 0f);
                ConfigureModel(CharacterPath, false, null);
                var clips = new Dictionary<string, AnimationClip>();
                for (int i = 0; i < ClipNames.Length; i++)
                {
                    string name = ClipNames[i];
                    Progress("Importing " + name, (i + 1f) / (ClipNames.Length + 4f));
                    string path = Root + "/Animations/" + name + ".fbx";
                    ConfigureModel(path, true, name);
                    clips[name] = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                        .FirstOrDefault(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal));
                    if (clips[name] == null)
                        throw new InvalidOperationException("No animation take imported from " + path);
                    if (!clips[name].humanMotion)
                        throw new InvalidOperationException("The imported take is not Humanoid: " + path);
                }
                ConfigureTextures();
                Material body1 = MaterialFor("1001", "Ch15_body2");
                Material body2 = MaterialFor("1002", "Ch15_body3");
                AvatarMask mask = BuildMask();
                AnimatorController controller = BuildController(clips, mask);
                Progress("Building the reusable prefab", 0.97f);
                BuildPrefab(controller, body1, body2);
                AssetDatabase.SaveAssets();
                Debug.Log("Armed enemy assets built. Inspect the generated prefab, then select your existing enemy and run '2. Apply Visual To Selected Enemy'. No scene was replaced.");
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            }
            catch (OperationCanceledException) { Debug.Log("Asset setup canceled. Run it again to complete the generated assets."); }
            catch (Exception e) { Debug.LogException(e); }
            finally { EditorUtility.ClearProgressBar(); }
        }

        private static void Progress(string text, float value)
        {
            if (EditorUtility.DisplayCancelableProgressBar("Armed enemy animation setup", text, value))
                throw new OperationCanceledException();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static Avatar GetAvatar(string path)
        {
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
        }

        private static void ConfigureModel(string path, bool animations, string clipName)
        {
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) throw new InvalidOperationException("Missing model: " + path);
            bool resetCopiedDescription = importer.avatarSetup == ModelImporterAvatarSetup.CopyFromOther ||
                importer.sourceAvatar != null;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importAnimation = animations;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.isReadable = false;
            importer.optimizeGameObjects = false;
            importer.animationType = ModelImporterAnimationType.Human;
            // Blender's animation FBXs have an extra Armature transform. Build
            // each clip's own Avatar, then let Humanoid retarget to the character.
            importer.sourceAvatar = null;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            if (resetCopiedDescription)
            {
                // Unity's Rig inspector also clears these arrays when changing
                // Avatar Definition. Clearing only sourceAvatar leaves the
                // previously copied skeleton and bone mapping behind.
                HumanDescription description = importer.humanDescription;
                description.human = Array.Empty<HumanBone>();
                description.skeleton = Array.Empty<SkeletonBone>();
                importer.humanDescription = description;
            }
            importer.autoGenerateAvatarMappingIfUnspecified = true;
            importer.animationCompression = ModelImporterAnimationCompression.Optimal;
            importer.SaveAndReimport();
            Avatar avatar = GetAvatar(path);
            if (avatar == null || !avatar.isValid || !avatar.isHuman)
            {
                // Keep automatic Mixamo mapping when valid; only repair an
                // unsuccessful mapping using the actual imported bone names.
                ApplyExplicitHumanoidMapping(importer, path);
                importer.SaveAndReimport();
                avatar = GetAvatar(path);
            }
            if (avatar == null || !avatar.isValid || !avatar.isHuman)
                throw new InvalidOperationException("Humanoid Avatar is invalid for " + path + ". Open Rig > Configure and inspect the mapping before proceeding.");
            if (!animations) return;
            ModelImporterClipAnimation[] takes = importer.defaultClipAnimations;
            if (takes.Length != 1)
                throw new InvalidOperationException("Expected one baked take in " + path + ", found " + takes.Length);
            ModelImporterClipAnimation take = takes[0];
            take.name = clipName;
            take.loopTime = !Once.Contains(clipName);
            take.loopPose = take.loopTime;
            take.lockRootRotation = true;
            take.lockRootHeightY = true;
            take.lockRootPositionXZ = true;
            take.keepOriginalOrientation = true;
            take.keepOriginalPositionY = true;
            take.keepOriginalPositionXZ = true;
            importer.clipAnimations = new[] { take };
            importer.SaveAndReimport();
        }

        private static string Normalize(string name)
        {
            string text = name.ToLowerInvariant().Replace("mixamorig", "");
            return new string(text.Where(char.IsLetterOrDigit).ToArray());
        }

        private static void ApplyExplicitHumanoidMapping(ModelImporter importer, string path)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (model == null) throw new InvalidOperationException("Cannot read skeleton from " + path);
            Transform[] transforms = model.GetComponentsInChildren<Transform>(true);
            var names = transforms.GroupBy(t => Normalize(t.name)).ToDictionary(g => g.Key, g => g.First().name);
            var mapping = new Dictionary<HumanBodyBones, string> {
                { HumanBodyBones.Hips, "Hips" }, { HumanBodyBones.Spine, "Spine" },
                { HumanBodyBones.Chest, "Spine1" }, { HumanBodyBones.UpperChest, "Spine2" },
                { HumanBodyBones.Neck, "Neck" }, { HumanBodyBones.Head, "Head" },
                { HumanBodyBones.LeftShoulder, "LeftShoulder" }, { HumanBodyBones.RightShoulder, "RightShoulder" },
                { HumanBodyBones.LeftUpperArm, "LeftArm" }, { HumanBodyBones.RightUpperArm, "RightArm" },
                { HumanBodyBones.LeftLowerArm, "LeftForeArm" }, { HumanBodyBones.RightLowerArm, "RightForeArm" },
                { HumanBodyBones.LeftHand, "LeftHand" }, { HumanBodyBones.RightHand, "RightHand" },
                { HumanBodyBones.LeftUpperLeg, "LeftUpLeg" }, { HumanBodyBones.RightUpperLeg, "RightUpLeg" },
                { HumanBodyBones.LeftLowerLeg, "LeftLeg" }, { HumanBodyBones.RightLowerLeg, "RightLeg" },
                { HumanBodyBones.LeftFoot, "LeftFoot" }, { HumanBodyBones.RightFoot, "RightFoot" },
                { HumanBodyBones.LeftToes, "LeftToeBase" }, { HumanBodyBones.RightToes, "RightToeBase" }
            };
            string[] fingers = { "Thumb", "Index", "Middle", "Ring", "Little" };
            string[] mixamoFingers = { "Thumb", "Index", "Middle", "Ring", "Pinky" };
            string[] joints = { "Proximal", "Intermediate", "Distal" };
            foreach (string side in new[] { "Left", "Right" })
                for (int f = 0; f < fingers.Length; f++)
                    for (int j = 0; j < joints.Length; j++)
                    {
                        HumanBodyBones bone = (HumanBodyBones)Enum.Parse(typeof(HumanBodyBones), side + fingers[f] + joints[j]);
                        mapping[bone] = side + "Hand" + mixamoFingers[f] + (j + 1);
                    }
            var human = new List<HumanBone>();
            foreach (var pair in mapping)
            {
                string actual;
                if (!names.TryGetValue(Normalize(pair.Value), out actual)) continue;
                human.Add(new HumanBone
                {
                    humanName = HumanTrait.BoneName[(int)pair.Key],
                    boneName = actual,
                    limit = new HumanLimit { useDefaultValues = true }
                });
            }
            HumanDescription description = importer.humanDescription;
            description.human = human.ToArray();
            description.skeleton = transforms.Select(t => new SkeletonBone
            {
                name = t.name,
                position = t.localPosition,
                rotation = t.localRotation,
                scale = t.localScale
            }).ToArray();
            description.upperArmTwist = 0.5f; description.lowerArmTwist = 0.5f;
            description.upperLegTwist = 0.5f; description.lowerLegTwist = 0.5f;
            description.armStretch = 0.05f; description.legStretch = 0.05f;
            importer.humanDescription = description;
        }

        private static void ConfigureTextures()
        {
            string folder = Root + "/Character/Textures";
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;
                bool normal = path.Contains("_Normal");
                importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                importer.sRGBTexture = !normal && !path.Contains("_Glossiness");
                importer.mipmapEnabled = true;
                importer.maxTextureSize = 2048;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.compressionQuality = 75;
                importer.isReadable = false;
                importer.SaveAndReimport();
            }
        }

        private static Texture2D Texture(string tile, string kind)
        {
            string path = Root + "/Character/Textures/Ch15_" + tile + "_" + kind + ".png";
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null && kind != "Emissive") throw new InvalidOperationException("Missing texture: " + path);
            return texture;
        }

        private static Material MaterialFor(string tile, string name)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard (Specular setup)");
            if (shader == null) throw new InvalidOperationException("URP Lit or Standard Specular shader is required.");
            string path = Generated + "/Materials/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
            material.shader = shader;
            material.SetTexture(material.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex", Texture(tile, "Diffuse"));
            material.SetColor(material.HasProperty("_BaseColor") ? "_BaseColor" : "_Color", Color.white);
            material.SetTexture("_BumpMap", Texture(tile, "Normal"));
            material.SetTexture("_SpecGlossMap", Texture(tile, "SpecGloss"));
            if (material.HasProperty("_WorkflowMode")) material.SetFloat("_WorkflowMode", 0f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 1f);
            if (material.HasProperty("_GlossMapScale")) material.SetFloat("_GlossMapScale", 1f);
            material.EnableKeyword("_NORMALMAP");
            if (shader.name.StartsWith("Universal Render Pipeline/", StringComparison.Ordinal))
            {
                material.EnableKeyword("_SPECULAR_SETUP");
                material.EnableKeyword("_METALLICSPECGLOSSMAP");
                material.DisableKeyword("_SPECGLOSSMAP");
            }
            else
            {
                material.EnableKeyword("_SPECGLOSSMAP");
                material.DisableKeyword("_SPECULAR_SETUP");
                material.DisableKeyword("_METALLICSPECGLOSSMAP");
            }
            Texture2D emission = Texture(tile, "Emissive");
            if (emission != null)
            {
                material.SetTexture("_EmissionMap", emission); material.SetColor("_EmissionColor", Color.white);
                material.EnableKeyword("_EMISSION");
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static AvatarMask BuildMask()
        {
            string path = Generated + "/WeaponUpperBody.mask";
            AvatarMask mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(path);
            if (mask == null) { mask = new AvatarMask(); AssetDatabase.CreateAsset(mask, path); }
            for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
                mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, false);
            foreach (AvatarMaskBodyPart part in new[] { AvatarMaskBodyPart.Body, AvatarMaskBodyPart.Head, AvatarMaskBodyPart.LeftArm,
                AvatarMaskBodyPart.RightArm, AvatarMaskBodyPart.LeftFingers, AvatarMaskBodyPart.RightFingers })
                mask.SetHumanoidBodyPartActive(part, true);
            EditorUtility.SetDirty(mask);
            return mask;
        }

        private static AnimatorController BuildController(Dictionary<string, AnimationClip> clips, AvatarMask mask)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            UnityEngine.Object[] old = AssetDatabase.LoadAllAssetsAtPath(ControllerPath);
            var sm = new AnimatorStateMachine { name = "Base Layer" };
            AssetDatabase.AddObjectToAsset(sm, controller);
            controller.layers = new[] { new AnimatorControllerLayer { name = "Base Layer", stateMachine = sm, defaultWeight = 1f } };
            controller.parameters = new AnimatorControllerParameter[0];
            foreach (UnityEngine.Object asset in old)
                if (asset != controller) UnityEngine.Object.DestroyImmediate(asset, true);
            foreach (string name in new[] { "Speed", "MoveX", "MoveY" }) controller.AddParameter(name, AnimatorControllerParameterType.Float);
            foreach (string name in new[] { "AIState", "AttackIndex" }) controller.AddParameter(name, AnimatorControllerParameterType.Int);
            foreach (string name in new[] { "IsAiming", "Isidle", "Iswalking", "IsChasing", "IsProcessing", "IsDead", "GrenadeDeath", "Isattacking", "IsFiring" })
                controller.AddParameter(name, AnimatorControllerParameterType.Bool);
            controller.AddParameter("Fire", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("HitReact", AnimatorControllerParameterType.Trigger);
            AnimatorState locomotion = sm.AddState("Locomotion", new Vector3(300, 50));
            locomotion.motion = SpeedTree(controller, "Patrol and Chase", "Speed",
                new[] { clips["RifleIdle"], clips["PatrolWalk"], clips["ChaseRun"] }, new[] { 0f, 2.5f, 6.5f });
            locomotion.iKOnFeet = true;
            sm.defaultState = locomotion;
            AnimatorState investigate = sm.AddState("Investigate", new Vector3(300, 150));
            investigate.motion = SpeedTree(controller, "Investigate Movement", "Speed",
                new[] { clips["RifleIdle"], clips["InvestigateWalk"] }, new[] { 0f, 2.5f });
            investigate.iKOnFeet = true;
            AnimatorState search = sm.AddState("Search", new Vector3(300, 250));
            search.motion = SpeedTree(controller, "Search Movement", "Speed",
                new[] { clips["SearchLookAround"], clips["InvestigateWalk"] }, new[] { 0f, 2.5f });
            search.iKOnFeet = true;
            AnimatorState combat = sm.AddState("Combat Movement", new Vector3(600, 50));
            var combatTree = new BlendTree
            {
                name = "Combat Direction",
                blendType = BlendTreeType.FreeformCartesian2D,
                blendParameter = "MoveX",
                blendParameterY = "MoveY",
                useAutomaticThresholds = false
            };
            AssetDatabase.AddObjectToAsset(combatTree, controller);
            combatTree.children = new[] {
                new ChildMotion { motion = clips["RifleIdle"], position = Vector2.zero, timeScale = 1f },
                new ChildMotion { motion = clips["CombatForward"], position = Vector2.up, timeScale = 1.44f },
                new ChildMotion { motion = clips["CombatBackward"], position = Vector2.down, timeScale = 1.44f },
                new ChildMotion { motion = clips["StrafeLeft"], position = Vector2.left, timeScale = 1.44f },
                new ChildMotion { motion = clips["StrafeRight"], position = Vector2.right, timeScale = 1.44f }
            };
            combat.motion = combatTree; combat.iKOnFeet = true;
            AnimatorState alert = sm.AddState("Alert", new Vector3(600, 150)); alert.motion = clips["Alert"];
            AnimatorState hit = sm.AddState("Hit Reaction", new Vector3(600, 250)); hit.motion = clips["HitReact"];
            AnimatorState death = sm.AddState("Death", new Vector3(900, 50)); death.motion = clips["Death"];
            AnimatorState grenadeDeath = sm.AddState("Grenade Death", new Vector3(900, 150)); grenadeDeath.motion = clips["GrenadeDeath"];
            AnimatorStateTransition d = Any(sm, death, 0.06f); d.AddCondition(AnimatorConditionMode.If, 0, "IsDead");
            d.AddCondition(AnimatorConditionMode.IfNot, 0, "GrenadeDeath");
            AnimatorStateTransition gd = Any(sm, grenadeDeath, 0.06f); gd.AddCondition(AnimatorConditionMode.If, 0, "GrenadeDeath");
            AnimatorStateTransition h = Alive(Any(sm, hit, 0.06f)); h.AddCondition(AnimatorConditionMode.If, 0, "HitReact");
            StateEntry(sm, investigate, 1); StateEntry(sm, search, 2); StateEntry(sm, alert, 4); StateEntry(sm, combat, 5);
            StateEntry(sm, locomotion, 0); StateEntry(sm, locomotion, 3); StateEntry(sm, locomotion, 7);

            var upper = new AnimatorStateMachine { name = "Weapon UpperBody" };
            AssetDatabase.AddObjectToAsset(upper, controller);
            AnimatorState ready = upper.AddState("Aim Ready", new Vector3(300, 50)); ready.motion = clips["AimIdle"];
            AnimatorState fire = upper.AddState("Fire", new Vector3(550, 50)); fire.motion = clips["Fire"];
            upper.defaultState = ready;
            AnimatorStateTransition shot = Alive(Any(upper, fire, 0.035f)); shot.canTransitionToSelf = true;
            shot.AddCondition(AnimatorConditionMode.If, 0, "IsAiming"); shot.AddCondition(AnimatorConditionMode.If, 0, "Fire");
            AnimatorStateTransition recover = fire.AddTransition(ready); recover.hasExitTime = true;
            recover.exitTime = 0.92f; recover.duration = 0.04f; recover.hasFixedDuration = true;
            var layers = controller.layers.ToList();
            layers.Add(new AnimatorControllerLayer
            {
                name = "Weapon UpperBody",
                stateMachine = upper,
                avatarMask = mask,
                blendingMode = AnimatorLayerBlendingMode.Override,
                defaultWeight = 0f
            });
            controller.layers = layers.ToArray();
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static BlendTree SpeedTree(AnimatorController owner, string name, string parameter, AnimationClip[] clips, float[] thresholds)
        {
            var tree = new BlendTree { name = name, blendType = BlendTreeType.Simple1D, blendParameter = parameter, useAutomaticThresholds = false };
            AssetDatabase.AddObjectToAsset(tree, owner);
            tree.children = clips.Select((clip, i) => new ChildMotion { motion = clip, threshold = thresholds[i], timeScale = 1f }).ToArray();
            return tree;
        }
        private static AnimatorStateTransition Any(AnimatorStateMachine machine, AnimatorState state, float duration)
        {
            AnimatorStateTransition transition = machine.AddAnyStateTransition(state);
            transition.hasExitTime = false; transition.hasFixedDuration = true; transition.duration = duration;
            transition.canTransitionToSelf = false;
            return transition;
        }
        private static AnimatorStateTransition Alive(AnimatorStateTransition transition)
        {
            transition.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
            transition.AddCondition(AnimatorConditionMode.IfNot, 0, "GrenadeDeath");
            return transition;
        }
        private static void StateEntry(AnimatorStateMachine machine, AnimatorState state, int value)
        {
            AnimatorStateTransition transition = Alive(Any(machine, state, 0.12f));
            transition.AddCondition(AnimatorConditionMode.Equals, value, "AIState");
        }

        private static GameObject AddVisual(GameObject root, AnimatorController controller, Material body1, Material body2, float desiredHeight, float desiredFoot)
        {
            Vector3 rootScale = root.transform.lossyScale;
            if (rootScale.x <= 0f || rootScale.y <= 0f || rootScale.z <= 0f ||
                Mathf.Abs(Mathf.Abs(rootScale.x) - Mathf.Abs(rootScale.y)) > 0.001f ||
                Mathf.Abs(Mathf.Abs(rootScale.x) - Mathf.Abs(rootScale.z)) > 0.001f)
                throw new InvalidOperationException("The enemy root has non-uniform scale. Humanoid animation requires a uniform root scale; correct that before applying the visual.");
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterPath);
            if (source == null) throw new InvalidOperationException("Build/import the character first.");
            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(source);
            visual.name = "KD_ArmedVisual";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = Vector3.zero; visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;
            foreach (Transform t in visual.GetComponentsInChildren<Transform>(true))
            { t.gameObject.layer = root.layer; Record(t.gameObject); }
            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true)) renderer.sharedMaterials = new[] { body1, body2 };
            Bounds bounds = LocalBounds(root.transform, visual);
            float scale = desiredHeight / Mathf.Max(0.1f, bounds.size.y);
            visual.transform.localScale = Vector3.one * scale;
            bounds = LocalBounds(root.transform, visual);
            visual.transform.localPosition += Vector3.up * (desiredFoot - bounds.min.y);
            Animator animator = visual.GetComponent<Animator>();
            if (animator == null) animator = visual.AddComponent<Animator>();
            animator.avatar = GetAvatar(CharacterPath);
            animator.runtimeAnimatorController = controller; animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.enabled = true;
            // Keep the imported bind transforms. Evaluating a clip here would
            // save an animated bone pose into the prefab/scene as overrides.
            // The character FBX is static; its imported bounds do not include
            // the separate aim/fall takes. Expand only after fitting the visual
            // so skinning cannot disappear at the edge of the camera frustum.
            foreach (SkinnedMeshRenderer skinned in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                Bounds local = skinned.localBounds;
                local.Expand(Mathf.Max(local.size.x, Mathf.Max(local.size.y, local.size.z)) * 2f);
                skinned.localBounds = local;
            }
            Record(visual.transform);
            Record(animator);
            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true)) Record(renderer);
            return visual;
        }

        private static void Record(UnityEngine.Object target)
        {
            EditorUtility.SetDirty(target);
            if (PrefabUtility.IsPartOfPrefabInstance(target))
                PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }

        private static Bounds LocalBounds(Transform root, GameObject visual)
        {
            bool first = true; Bounds bounds = default(Bounds);
            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                Bounds world = renderer.bounds;
                for (int x = -1; x <= 1; x += 2) for (int y = -1; y <= 1; y += 2) for (int z = -1; z <= 1; z += 2)
                        {
                            Vector3 point = root.InverseTransformPoint(world.center + Vector3.Scale(world.extents, new Vector3(x, y, z)));
                            if (first) { bounds = new Bounds(point, Vector3.zero); first = false; } else bounds.Encapsulate(point);
                        }
            }
            if (first) throw new InvalidOperationException("Character has no renderers.");
            return bounds;
        }

        private static SerializedProperty Required(SerializedObject owner, string name)
        {
            SerializedProperty property = owner.FindProperty(name);
            if (property == null) throw new InvalidOperationException("Missing field '" + name + "'. Install the supplied replacement scripts first.");
            return property;
        }

        private static void Bind(ArmedEnemy enemy, GameObject visual, bool createFirePoint)
        {
            Animator animator = visual.GetComponent<Animator>();
            var serialized = new SerializedObject(enemy);
            Required(serialized, "animationAnimator").objectReferenceValue = animator;
            Required(serialized, "upperBodyLayerName").stringValue = "Weapon UpperBody";
            KillDream.KDArmedAimPitch aim = visual.GetComponent<KillDream.KDArmedAimPitch>();
            if (aim == null) aim = visual.AddComponent<KillDream.KDArmedAimPitch>();
            aim.Configure(enemy);
            Record(aim);
            Transform head = visual.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => Normalize(t.name) == "head");
            if (head != null) Required(serialized, "enemyrad").objectReferenceValue = head;
            Transform hand = visual.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => Normalize(t.name) == "righthand");
            if (hand != null) Required(serialized, "Handenemy").objectReferenceValue = hand;
            if (createFirePoint && Required(serialized, "firePoint").objectReferenceValue == null)
            {
                // A visible weapon is project-specific. This is a clearly named
                // placeholder origin; assign the actual rifle muzzle afterward.
                var origin = new GameObject("FirePoint_ASSIGN_RIFLE_MUZZLE");
                origin.transform.SetParent(visual.transform, false);
                origin.transform.localPosition = new Vector3(0f, 1.35f, 0.45f);
                Record(origin.transform);
                Required(serialized, "firePoint").objectReferenceValue = origin.transform;
            }
            serialized.ApplyModifiedProperties();
            Record(enemy);
        }

        private static void UpdateExistingVisual(GameObject visual, AnimatorController controller, Material body1, Material body2)
        {
            Animator animator = visual.GetComponent<Animator>();
            if (animator == null) throw new InvalidOperationException("The existing KD_ArmedVisual has no Animator. Restore that visual before rebinding.");
            animator.avatar = GetAvatar(CharacterPath);
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false; animator.enabled = true;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            Record(animator);
            foreach (SkinnedMeshRenderer renderer in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (renderer.sharedMesh != null && AssetDatabase.GetAssetPath(renderer.sharedMesh) == CharacterPath)
                { renderer.sharedMaterials = new[] { body1, body2 }; Record(renderer); }
            // Keep attachments, weapon offsets, the visual transform, bone bind
            // overrides and the already-expanded bounds. Do not evaluate a pose.
        }

        private static void BuildPrefab(AnimatorController controller, Material body1, Material body2)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            {
                GameObject contents = PrefabUtility.LoadPrefabContents(PrefabPath);
                try
                {
                    ArmedEnemy owner = contents.GetComponent<ArmedEnemy>();
                    Transform visual = contents.transform.Find("KD_ArmedVisual");
                    if (owner == null || visual == null)
                        throw new InvalidOperationException("Existing generated prefab was structurally changed. Restore its ArmedEnemy/KD_ArmedVisual structure before rebuilding.");
                    UpdateExistingVisual(visual.gameObject, controller, body1, body2);
                    Bind(owner, visual.gameObject, true);
                    if (PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath) == null)
                        throw new InvalidOperationException("Unity could not save " + PrefabPath + ". Inspect the first Console error from this build.");
                }
                finally { PrefabUtility.UnloadPrefabContents(contents); }
                return;
            }
            var root = new GameObject("KD_ArmedEnemy");
            try
            {
                int layer = LayerMask.NameToLayer("Enemies"); if (layer >= 0) root.layer = layer;
                NavMeshAgent agent = root.AddComponent<NavMeshAgent>(); agent.height = 1.82f; agent.radius = 0.3f; agent.baseOffset = 0f;
                CapsuleCollider capsule = root.AddComponent<CapsuleCollider>(); capsule.height = 1.82f; capsule.radius = 0.3f; capsule.center = Vector3.up * 0.91f;
                ArmedEnemy enemy = root.AddComponent<ArmedEnemy>();
                int player = LayerMask.NameToLayer("Player"); if (player >= 0) enemy.Player_Layer = 1 << player;
                int wall = LayerMask.NameToLayer("Wall"); int ground = LayerMask.NameToLayer("Ground");
                enemy.ObstacleMask = (wall >= 0 ? 1 << wall : 0) | (ground >= 0 ? 1 << ground : 0) | 1;
                enemy.SightMask = enemy.ObstacleMask.value | enemy.Player_Layer.value;
                GameObject visual = AddVisual(root, controller, body1, body2, capsule.height, 0f);
                Bind(enemy, visual, true);
                if (PrefabUtility.SaveAsPrefabAsset(root, PrefabPath) == null)
                    throw new InvalidOperationException("Unity could not save " + PrefabPath + ". Inspect the first Console error from this build.");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [MenuItem("Tools/Kill Dream/Armed Enemy/2. Apply Visual To Selected Enemy")]
        public static void ApplyToSelected()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogError("Stop Play Mode first."); return; }
            ArmedEnemy enemy = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponentInParent<ArmedEnemy>() : null;
            if (enemy == null) { Debug.LogError("Select the existing GameObject with ArmedEnemy."); return; }
            if (!enemy.gameObject.scene.IsValid() || PrefabUtility.IsPartOfPrefabAsset(enemy.gameObject))
            { Debug.LogError("Select an enemy instance in the scene Hierarchy, not a prefab/model asset in Project."); return; }
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            Material body1 = AssetDatabase.LoadAssetAtPath<Material>(Generated + "/Materials/Ch15_body2.mat");
            Material body2 = AssetDatabase.LoadAssetAtPath<Material>(Generated + "/Materials/Ch15_body3.mat");
            if (controller == null || body1 == null || body2 == null) { Debug.LogError("Run '1. Build Animation Assets' first."); return; }
            int group = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Apply rigged armed enemy visual");
            try
            {
                Undo.RegisterFullObjectHierarchyUndo(enemy.gameObject, "Apply armed enemy visual");
                Transform existing = enemy.transform.Find("KD_ArmedVisual");
                if (existing != null)
                {
                    UpdateExistingVisual(existing.gameObject, controller, body1, body2);
                    Bind(enemy, existing.gameObject, false);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(enemy);
                    EditorSceneManager.MarkSceneDirty(enemy.gameObject.scene);
                    Debug.Log("Rebound the existing visual on " + enemy.name + "; weapon attachments and visual offsets were retained.");
                    return;
                }
                CapsuleCollider capsule = enemy.GetComponent<CapsuleCollider>();
                if (capsule == null)
                {
                    capsule = Undo.AddComponent<CapsuleCollider>(enemy.gameObject);
                    capsule.height = 1.82f; capsule.radius = 0.3f; capsule.center = Vector3.up * 0.91f;
                    Record(capsule);
                }
                float height = capsule.height;
                float foot = capsule != null ? capsule.center.y - capsule.height * 0.5f : 0f;
                Renderer rootRenderer = enemy.GetComponent<Renderer>();
                if (rootRenderer != null) { Undo.RecordObject(rootRenderer, "Hide old placeholder"); rootRenderer.enabled = false; Record(rootRenderer); }
                foreach (SkinnedMeshRenderer old in enemy.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                { Undo.RecordObject(old, "Hide old skinned visual"); old.enabled = false; Record(old); }
                foreach (MeshRenderer old in enemy.GetComponentsInChildren<MeshRenderer>(true))
                {
                    MeshFilter filter = old.GetComponent<MeshFilter>();
                    string path = filter != null && filter.sharedMesh != null ? AssetDatabase.GetAssetPath(filter.sharedMesh) : "";
                    bool character = path.IndexOf("Armed Enemy Compressed.fbx", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        path.IndexOf("Ch15_nonPBR.fbx", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        old.sharedMaterials.Any(m => m != null && m.name.StartsWith("Ch15_", StringComparison.OrdinalIgnoreCase));
                    if (character) { Undo.RecordObject(old, "Hide old static character fragments"); old.enabled = false; Record(old); }
                }
                foreach (Animator oldAnimator in enemy.GetComponentsInChildren<Animator>(true))
                {
                    if (oldAnimator.gameObject == enemy.gameObject || (oldAnimator.avatar != null && oldAnimator.avatar.isHuman))
                    { Undo.RecordObject(oldAnimator, "Use visual Animator"); oldAnimator.enabled = false; Record(oldAnimator); }
                }
                GameObject visual = AddVisual(enemy.gameObject, controller, body1, body2, height, foot);
                Undo.RegisterCreatedObjectUndo(visual, "Add rigged visual");
                Bind(enemy, visual, false);
                PrefabUtility.RecordPrefabInstancePropertyModifications(enemy);
                EditorSceneManager.MarkSceneDirty(enemy.gameObject.scene);
                Selection.activeGameObject = enemy.gameObject;
                Debug.Log("Applied the rigged visual and controller to " + enemy.name + ". AI tuning, player target, patrol points, NavMeshAgent and collider were retained. Check the rifle's muzzle/grip and save the scene after testing.");
            }
            catch (Exception e) { Undo.RevertAllDownToGroup(group); Debug.LogException(e); }
            finally { Undo.CollapseUndoOperations(group); }
        }

        [MenuItem("Tools/Kill Dream/Armed Enemy/Optional - Connect Selected Player Damage")]
        public static void ConnectSelectedPlayerDamage()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogError("Stop Play Mode first."); return; }
            fpsmovement player = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponentInParent<fpsmovement>() : null;
            if (player == null) { Debug.LogError("Select the existing FPS player root."); return; }
            if (!player.gameObject.scene.IsValid() || PrefabUtility.IsPartOfPrefabAsset(player.gameObject))
            { Debug.LogError("Select the player instance in the scene Hierarchy."); return; }
            if (player.GetComponents<MonoBehaviour>().Any(c => c is IEnemyRangedDamageable || c is IEnemyMeleeDamageable))
            { Debug.Log("This player already has an enemy damage receiver; no additional component was added."); return; }
            Undo.AddComponent<KillDream.KDEnemyDamageReceiver>(player.gameObject);
            EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
            Debug.Log("Connected enemy hits to the player's existing fpsmovement.TakeDamage method.");
        }

        [MenuItem("Tools/Kill Dream/Armed Enemy/3. Validate Generated Assets")]
        public static void ValidateGeneratedAssets()
        {
            try
            {
                AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
                if (controller == null) throw new InvalidOperationException("Build the animation assets first.");
                var parameters = controller.parameters.ToDictionary(p => p.name, p => p.type);
                var required = new Dictionary<string, AnimatorControllerParameterType> {
                    { "AIState", AnimatorControllerParameterType.Int }, { "Speed", AnimatorControllerParameterType.Float },
                    { "MoveX", AnimatorControllerParameterType.Float }, { "MoveY", AnimatorControllerParameterType.Float },
                    { "Fire", AnimatorControllerParameterType.Trigger }, { "HitReact", AnimatorControllerParameterType.Trigger },
                    { "IsAiming", AnimatorControllerParameterType.Bool }, { "IsDead", AnimatorControllerParameterType.Bool },
                    { "GrenadeDeath", AnimatorControllerParameterType.Bool }
                };
                foreach (var pair in required)
                    if (!parameters.ContainsKey(pair.Key) || parameters[pair.Key] != pair.Value)
                        throw new InvalidOperationException("Wrong or missing Animator parameter: " + pair.Key);
                AnimatorControllerLayer layer = controller.layers.FirstOrDefault(l => l.name == "Weapon UpperBody");
                if (layer == null || layer.avatarMask == null || layer.stateMachine.defaultState == null)
                    throw new InvalidOperationException("The upper-body layer, mask, or default state is missing.");
                foreach (AvatarMaskBodyPart part in new[] { AvatarMaskBodyPart.Root, AvatarMaskBodyPart.LeftLeg, AvatarMaskBodyPart.RightLeg })
                    if (layer.avatarMask.GetHumanoidBodyPartActive(part))
                        throw new InvalidOperationException("Weapon overlay incorrectly includes " + part);
                foreach (string name in ClipNames)
                {
                    string path = Root + "/Animations/" + name + ".fbx";
                    ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
                    Avatar avatar = GetAvatar(path);
                    if (importer == null || importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel ||
                        importer.sourceAvatar != null || avatar == null || !avatar.isHuman || !avatar.isValid)
                        throw new InvalidOperationException("Animation needs its own valid Humanoid Avatar: " + name + ". Run '1. Build Animation Assets' to repair its Rig settings.");
                    AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                        .FirstOrDefault(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal));
                    if (clip == null || !clip.humanMotion || clip.length <= 0f)
                        throw new InvalidOperationException("Missing or invalid Humanoid clip: " + name);
                }
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                if (prefab == null) throw new InvalidOperationException("Generated enemy prefab is missing.");
                ArmedEnemy enemy = prefab.GetComponent<ArmedEnemy>();
                if (enemy == null) throw new InvalidOperationException("Prefab has no ArmedEnemy component.");
                Animator animator = Required(new SerializedObject(enemy), "animationAnimator").objectReferenceValue as Animator;
                if (animator == null || animator.avatar == null || !animator.avatar.isHuman || !animator.avatar.isValid || animator.applyRootMotion)
                    throw new InvalidOperationException("Prefab Animator/Avatar binding or root-motion setting is invalid.");
                Debug.Log("Generated asset validation passed: 15 Humanoid takes, parameter types, upper-body mask and prefab binding. Runtime behaviour and visual transitions still require the Play Mode checklist.");
            }
            catch (Exception e) { Debug.LogException(e); }
        }
    }
}
