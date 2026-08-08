using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Animancer;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Editor;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public static class TrainingEnemyAnimationPresentationBuilder
    {
        const string ModelPath = "Assets/AssetArt/Animation/ZZZ/敌人/怪兽/怪兽.fbx";
        const string PresentationPrefabPath = "Assets/Prefabs/Characters/Presentation/AI/TrainingEnemyMonsterPresentation.prefab";
        const string RootPath = "Assets/Configs/Character/TrainingEnemy/Presentation";
        const string RigPath = RootPath + "/Rig/TrainingEnemyMonsterAnimationRig.asset";
        const string IkProfilePath = RootPath + "/Rig/TrainingEnemyMonsterFullBodyIkProfile.asset";
        const string FootCalibrationPath = RootPath + "/FootPlacement/TrainingEnemyMonsterFootPlacementRigCalibration.asset";
        const string FootPlacementProfilePath = RootPath + "/FootPlacement/TrainingEnemyMonsterFootPlacementProfile.asset";
        const string FootAnalysisSourcePath = RootPath + "/FootPlacement/TrainingEnemyMonsterFootPlacementAnalysisSource.asset";
        const string SamplingRigPath = RootPath + "/FootPlacement/TrainingEnemyMonsterFootPlacementSamplingRig.prefab";
        const string BlendSpacePath = RootPath + "/Locomotion/TrainingEnemyMonsterLocomotion.asset";
        const string BlendProfilePath = RootPath + "/Blend/TrainingEnemyMonsterFullBodyBlendProfile.asset";
        const string BlendPolicyPath = RootPath + "/Blend/TrainingEnemyMonsterFullBodyBlendPolicy.asset";
        const string PoseGraphPath = RootPath + "/PoseGraph/TrainingEnemyMonsterPoseGraph.asset";
        const string ProfilePath = RootPath + "/Profile/TrainingEnemyMonsterAnimationPresentationProfile.asset";
        const string MaterialPath = RootPath + "/Materials";
        const string BodyMaterialPath = MaterialPath + "/TrainingEnemyMonsterBody.mat";
        const string WeaponMaterialPath = MaterialPath + "/TrainingEnemyMonsterWeapon.mat";
        const string TexturePath = "Assets/AssetArt/Animation/ZZZ/敌人/怪兽/Tex";
        const string LocomotionSlotName = "TrainingEnemyMonsterLocomotionSlot";
        const string LocomotionBindingName = "TrainingEnemyMonsterLocomotionBinding";
        const string FootAnalysisIdentity = "TrainingEnemy.Monster.FootPlacementAnalysis";

        [MenuItem("Tools/3C/Characters/Build Training Enemy Animation Presentation")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Training Enemy Animation Presentation cannot be built in Play Mode.");

            TrainingEnemyAnimationAssetAuthoring.ConfigureImporter();
            EnsureFolders();
            Dictionary<string, AnimationClip> clips = LoadClips();
            GameObject prefabRoot = LoadPresentationPrefabContents();
            int curveCount = 0;
            try
            {
                Animator animator = prefabRoot.GetComponentInChildren<Animator>(true) ??
                    throw new InvalidOperationException("Training Enemy monster Presentation Prefab has no Animator.");
                Transform[] bones = CollectPhysicalBones(animator);
                CharacterAnimationRigDefinition rig = BuildRig(animator, bones);
                CharacterFullBodyIkProfile ikProfile = BuildIkProfile();
                ApplyMaterials(prefabRoot, BuildMaterial(
                    BodyMaterialPath,
                     "Training Enemy Monster Body",
                     TexturePath + "/Monster_Goblin_D.png",
                     TexturePath + "/Monster_Goblin_N.png",
                     string.Empty), BuildMaterial(
                     WeaponMaterialPath,
                     "Training Enemy Monster Weapon",
                     TexturePath + "/Monster_Metro_Goblin_Weapon_D.png",
                     string.Empty,
                     TexturePath + "/Monster_Metro_Goblin_Weapon_M.png"));
                BindPrefab(animator, rig, bones);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PresentationPrefabPath);
                CharacterFootPlacementAnalysisSource footAnalysisSource = BuildFootAnalysis(
                    rig,
                    clips["Goblin_Ani_Idle"],
                    clips.Values);
                CharacterFootPlacementProfile footPlacementProfile = BuildFootPlacementProfile();
                CharacterAnimationBlendSpaceAsset blendSpace = BuildLocomotion(
                    rig,
                    clips["Goblin_Ani_Idle"],
                    clips["Goblin_Ani_Run"]);
                CharacterAnimationBlendProfile blendProfile = BuildBlendProfile(rig);
                CharacterAnimationBlendPolicy blendPolicy = BuildBlendPolicy(rig, blendProfile);
                CharacterPresentationPoseGraphAsset poseGraph = BuildPoseGraph(
                    blendPolicy,
                    ikProfile,
                    footPlacementProfile,
                    footAnalysisSource.RigCalibration);
                CharacterAnimationPresentationProfile profile = BuildProfile(
                    rig,
                    ikProfile,
                    blendSpace,
                    poseGraph,
                    footAnalysisSource);
                curveCount = TrainingEnemyAnimationAssetAuthoring.BakeRootMotionCurves(
                    clips,
                    prefabRoot);
                EditorUtility.SetDirty(profile);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PresentationPrefabPath);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
            Debug.Log($"Training Enemy monster Animation Presentation built with {TrainingEnemyAnimationAssetAuthoring.RequiredClipNames.Count} configured clips and {curveCount} baked Root Motion curves.");
        }

        static GameObject LoadPresentationPrefabContents()
        {
            EnsureFolder("Assets/Prefabs/Characters/Presentation/AI");
            if (!AssetDatabase.LoadAssetAtPath<GameObject>(PresentationPrefabPath))
            {
                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) ??
                    throw new InvalidOperationException("Training Enemy monster model could not be loaded.");
                GameObject instance = PrefabUtility.InstantiatePrefab(model) as GameObject ??
                    throw new InvalidOperationException("Training Enemy monster model could not be instantiated.");
                try
                {
                    instance.name = "TrainingEnemyMonsterPresentation";
                    PrefabUtility.SaveAsPrefabAsset(instance, PresentationPrefabPath);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(instance);
                }
            }
            return PrefabUtility.LoadPrefabContents(PresentationPrefabPath);
        }

        static Dictionary<string, AnimationClip> LoadClips()
        {
            Dictionary<string, AnimationClip> sourceClips = AssetDatabase.LoadAllAssetsAtPath(ModelPath)
                .OfType<AnimationClip>()
                .Where(value => !value.name.StartsWith("__preview__", StringComparison.Ordinal))
                .ToDictionary(value => value.name, StringComparer.Ordinal);
            string[] missing = TrainingEnemyAnimationAssetAuthoring.RequiredClipNames
                .Where(value => !sourceClips.ContainsKey(value))
                .ToArray();
            if (missing.Length > 0)
                throw new InvalidOperationException($"Training Enemy monster FBX is missing clips: {string.Join(", ", missing)}.");
            var clips = new Dictionary<string, AnimationClip>(StringComparer.Ordinal);
            foreach (string clipName in TrainingEnemyAnimationAssetAuthoring.RequiredClipNames)
            {
                string path = RootPath + "/Clips/" + clipName + ".anim";
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (!clip)
                {
                    clip = new AnimationClip();
                    AssetDatabase.CreateAsset(clip, path);
                }
                EditorUtility.CopySerialized(sourceClips[clipName], clip);
                clip.name = clipName;
                EditorUtility.SetDirty(clip);
                clips.Add(clipName, clip);
            }
            AssetDatabase.SaveAssets();
            return clips;
        }

        static Transform[] CollectPhysicalBones(Animator animator)
        {
            var used = new HashSet<Transform> { animator.transform };
            foreach (SkinnedMeshRenderer renderer in animator.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                foreach (Transform bone in renderer.bones)
                {
                    for (Transform current = bone; current && current != animator.transform.parent; current = current.parent)
                        used.Add(current);
                }
            }
            Transform[] bones = animator.GetComponentsInChildren<Transform>(true)
                .Where(used.Contains)
                .ToArray();
            if (bones.Length < 10 || bones[0] != animator.transform)
                throw new InvalidOperationException("Training Enemy monster skeleton could not be resolved from its SkinnedMeshRenderers.");
            return bones;
        }

        static CharacterAnimationRigDefinition BuildRig(Animator animator, Transform[] bones)
        {
            CharacterAnimationRigDefinition rig = LoadOrCreate<CharacterAnimationRigDefinition>(
                RigPath,
                "Training Enemy Monster Animation Rig");
            var indices = new Dictionary<Transform, int>();
            var ids = new Dictionary<Transform, AnimationBoneId>();
            var definitions = new CharacterAnimationPhysicalBoneDefinition[bones.Length];
            for (int i = 0; i < bones.Length; i++)
            {
                Transform bone = bones[i];
                indices.Add(bone, i);
                AnimationBoneId id = new AnimationBoneId(BoneId(animator.transform, bone));
                ids.Add(bone, id);
                int parentIndex = bone == animator.transform ? -1 : indices[bone.parent];
                definitions[i] = new CharacterAnimationPhysicalBoneDefinition(
                    id,
                    parentIndex,
                    bone.localPosition,
                    bone.localRotation,
                    bone.localScale);
            }

            Transform pelvis = RequireBone(bones, "Bip001 Pelvis");
            rig.Configure(
                "training-enemy.monster.animation-rig",
                "training-enemy.monster.rig-" + AssetDatabase.GetAssetDependencyHash(ModelPath),
                definitions,
                Array.Empty<CharacterAnimationVirtualBoneDefinition>(),
                CharacterAnimationRootBonePolicy.ExcludeSourceRoot,
                CharacterAnimationScalePolicy.PreserveReferenceScale,
                ids[pelvis],
                ids[pelvis],
                new[]
                {
                    ids[RequireBone(bones, "Bip001 Spine")],
                    ids[RequireBone(bones, "Bip001 Spine1")],
                    ids[RequireBone(bones, "Bip001 Spine2")]
                },
                new CharacterAnimationArmChainDefinition(
                    ids[RequireBone(bones, "Bip001 L Clavicle")],
                    ids[RequireBone(bones, "Bip001 L UpperArm")],
                    ids[RequireBone(bones, "Bip001 L Forearm")],
                    ids[RequireBone(bones, "Bip001 L Hand")]),
                new CharacterAnimationArmChainDefinition(
                    ids[RequireBone(bones, "Bip001 R Clavicle")],
                    ids[RequireBone(bones, "Bip001 R UpperArm")],
                    ids[RequireBone(bones, "Bip001 R Forearm")],
                    ids[RequireBone(bones, "Bip001 R Hand")]),
                new CharacterAnimationLegChainDefinition(
                    ids[RequireBone(bones, "Bip001 L Thigh")],
                    ids[RequireBone(bones, "Bip001 L Calf")],
                    ids[RequireBone(bones, "Bip001 L Foot")],
                    ids[RequireBone(bones, "Bip001 L Toe0")]),
                new CharacterAnimationLegChainDefinition(
                    ids[RequireBone(bones, "Bip001 R Thigh")],
                    ids[RequireBone(bones, "Bip001 R Calf")],
                    ids[RequireBone(bones, "Bip001 R Foot")],
                    ids[RequireBone(bones, "Bip001 R Toe0")]),
                ids[RequireBone(bones, "Bip001 Head")]);
            EditorUtility.SetDirty(rig);
            return rig;
        }

        static CharacterAnimationBlendSpaceAsset BuildLocomotion(
            CharacterAnimationRigDefinition rig,
            AnimationClip idle,
            AnimationClip run)
        {
            CharacterAnimationBlendSpaceAsset blendSpace = LoadOrCreate<CharacterAnimationBlendSpaceAsset>(
                BlendSpacePath,
                "Training Enemy Monster Locomotion");
            if (!blendSpace.BlendSpaceId.IsValid)
                CharacterAnimationBlendSpaceAuthoringService.Initialize(
                    blendSpace,
                    new CharacterAnimationBlendSpaceId("training-enemy.monster.locomotion"));
            CharacterAnimationBlendSpaceAuthoringService.SetRig(blendSpace, rig);
            CharacterAnimationBlendSpaceAuthoringService.SetMode(blendSpace, CharacterAnimationBlendSpaceMode.Linear1D);
            CharacterAnimationBlendSpaceAuthoringService.SetAxis(
                blendSpace,
                0,
                AnimationPoseParameterIds.MotorPlanarSpeed,
                "m/s",
                0f,
                6f);
            CharacterAnimationBlendSpaceAuthoringService.SetPhase(
                blendSpace,
                CharacterAnimationBlendSpacePhasePolicy.SharedNormalizedPhase,
                default);
            CharacterAnimationBlendSpaceSampleId[] existing = blendSpace.Samples
                .Where(value => value != null)
                .Select(value => value.SampleId)
                .ToArray();
            if (existing.Length > 0)
                CharacterAnimationBlendSpaceAuthoringService.DeleteSamples(blendSpace, existing);
            var idleId = new CharacterAnimationBlendSpaceSampleId("training-enemy.monster.locomotion.idle");
            var runId = new CharacterAnimationBlendSpaceSampleId("training-enemy.monster.locomotion.run");
            CharacterAnimationBlendSpaceAuthoringService.CreateSample(blendSpace, idleId, Vector2.zero);
            CharacterAnimationBlendSpaceAuthoringService.SetSampleClip(blendSpace, idleId, idle);
            CharacterAnimationBlendSpaceAuthoringService.SetSampleRole(
                blendSpace,
                idleId,
                CharacterAnimationBlendSpaceSampleRole.StationaryPose,
                0f);
            CharacterAnimationBlendSpaceAuthoringService.SetSampleParameters(
                blendSpace,
                idleId,
                new[]
                {
                    new CharacterAnimationBlendSpaceSampleParameter(
                        AnimationPoseParameterIds.FootPlacementWeight,
                        1f)
                });
            CharacterAnimationBlendSpaceAuthoringService.CreateSample(blendSpace, runId, new Vector2(6f, 0f));
            CharacterAnimationBlendSpaceAuthoringService.SetSampleClip(blendSpace, runId, run);
            CharacterAnimationBlendSpaceAuthoringService.SetSampleRole(
                blendSpace,
                runId,
                CharacterAnimationBlendSpaceSampleRole.DynamicCycle,
                0f);
            CharacterAnimationBlendSpaceAuthoringService.SetSampleParameters(
                blendSpace,
                runId,
                new[]
                {
                    new CharacterAnimationBlendSpaceSampleParameter(
                        AnimationPoseParameterIds.FootPlacementWeight,
                        1f)
                });
            CharacterAnimationBlendSpaceAuthoringService.ReplacePoseParameterPolicies(
                blendSpace,
                new[]
                {
                    new CharacterAnimationBlendSpacePoseParameterPolicy(
                        AnimationPoseParameterIds.FootPlacementWeight,
                        CharacterAnimationBlendSpaceParameterPolicy.RequireAllSamplesWeighted)
                });
            return blendSpace;
        }

        static CharacterFullBodyIkProfile BuildIkProfile()
        {
            CharacterFullBodyIkProfile profile = LoadOrCreate<CharacterFullBodyIkProfile>(
                IkProfilePath,
                "Training Enemy Monster Full Body IK Profile");
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("m_ProfileId").stringValue = "training-enemy.monster.full-body-ik";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            serialized.Update();
            serialized.FindProperty("m_Revision").stringValue = profile.ComputeRevision();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            profile.RequireValid();
            EditorUtility.SetDirty(profile);
            return profile;
        }

        static CharacterFootPlacementAnalysisSource BuildFootAnalysis(
            CharacterAnimationRigDefinition rig,
            AnimationClip calibrationClip,
            IEnumerable<AnimationClip> clips)
        {
            BuildSamplingRig();
            CharacterFootPlacementRigCalibration calibration = LoadOrCreate<CharacterFootPlacementRigCalibration>(
                FootCalibrationPath,
                "Training Enemy Monster Foot Placement Rig Calibration");
            GameObject root = PrefabUtility.LoadPrefabContents(SamplingRigPath);
            try
            {
                Animator animator = root.GetComponentsInChildren<Animator>(true).Single();
                CharacterAnimationRigBinding binding = root.GetComponentsInChildren<CharacterAnimationRigBinding>(true).Single();
                CharacterWorldAwarePresentationBinding world = root.GetComponentsInChildren<CharacterWorldAwarePresentationBinding>(true).Single();
                var rigPayload = new CharacterAnimationRigPayload(rig);
                binding.RequireValid(rigPayload);
                calibrationClip.SampleAnimation(animator.gameObject, 0f);
                Transform leftAnkle = binding.PhysicalBones[rigPayload.LeftLeg.AnklePhysicalBoneIndex];
                Transform leftToe = binding.PhysicalBones[rigPayload.LeftLeg.ToePhysicalBoneIndex];
                Transform rightAnkle = binding.PhysicalBones[rigPayload.RightLeg.AnklePhysicalBoneIndex];
                Transform rightToe = binding.PhysicalBones[rigPayload.RightLeg.ToePhysicalBoneIndex];
                Vector3 up = world.PresentationRoot.up;
                float groundHeight = Mathf.Min(
                    Mathf.Min(Vector3.Dot(leftAnkle.position, up), Vector3.Dot(leftToe.position, up)),
                    Mathf.Min(Vector3.Dot(rightAnkle.position, up), Vector3.Dot(rightToe.position, up)));
                CharacterFootPlacementFootCalibration left = BuildFootCalibration(
                    leftAnkle,
                    leftToe,
                    up,
                    groundHeight,
                    rigPayload.LeftLeg.LegLength,
                    "Left");
                CharacterFootPlacementFootCalibration right = BuildFootCalibration(
                    rightAnkle,
                    rightToe,
                    up,
                    groundHeight,
                    rigPayload.RightLeg.LegLength,
                    "Right");
                calibration.Configure(
                    new CharacterFootPlacementRigCalibrationId("TrainingEnemy.Monster.FootPlacementRig"),
                    rig,
                    left,
                    right);
                EditorUtility.SetDirty(calibration);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            string samplingRigGuid = AssetDatabase.AssetPathToGUID(SamplingRigPath);
            CharacterFootPlacementAnalysisSource source = LoadOrCreate<CharacterFootPlacementAnalysisSource>(
                FootAnalysisSourcePath,
                "Training Enemy Monster Foot Placement Analysis Source");
            source.Configure(
                new CharacterFootPlacementAnalysisSourceId(FootAnalysisIdentity),
                1,
                samplingRigGuid,
                rig,
                calibration,
                calibrationClip,
                0f);
            EditorUtility.SetDirty(source);
            AssetDatabase.SaveAssets();
            CharacterFootPlacementSamplingRigAuthoringService.RebuildGeometryValidation(source);
            source.RequireValid();
            foreach (AnimationClip clip in clips.OrderBy(value => value.name, StringComparer.Ordinal))
                AnimationFootAnalysisArtifactBuilder.Build(clip, source);
            return source;
        }

        static CharacterFootPlacementProfile BuildFootPlacementProfile()
        {
            int groundLayer = LayerMask.NameToLayer("Ground");
            int footPlacementLayer = LayerMask.NameToLayer("FootPlacementSurface");
            if (groundLayer < 0 || footPlacementLayer < 0)
                throw new InvalidOperationException("Training Enemy Foot Placement requires Ground and FootPlacementSurface layers.");
            CharacterFootPlacementProfile profile = LoadOrCreate<CharacterFootPlacementProfile>(
                FootPlacementProfilePath,
                "Training Enemy Monster Foot Placement Profile");
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("m_ProfileId").stringValue = "training-enemy.monster.foot-placement";
            serialized.FindProperty("m_LyraCurrentGrounding.m_GroundLayerMask").intValue =
                1 << groundLayer | 1 << footPlacementLayer;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            serialized.Update();
            serialized.FindProperty("m_Revision").stringValue = profile.ComputeRevision();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            profile.RequireValid();
            EditorUtility.SetDirty(profile);
            return profile;
        }

        static CharacterFootPlacementFootCalibration BuildFootCalibration(
            Transform ankle,
            Transform toe,
            Vector3 up,
            float groundHeight,
            float legLength,
            string side)
        {
            Vector3 heelContact = ProjectToGround(ankle.position, up, groundHeight);
            Vector3 toeContact = ProjectToGround(toe.position, up, groundHeight);
            Vector3 forward = Vector3.ProjectOnPlane(toeContact - heelContact, up);
            if (forward.magnitude < legLength * 0.03f)
                throw new InvalidOperationException($"Training Enemy {side} Foot heel-to-toe baseline is too short for calibration.");
            Quaternion soleWorldRotation = Quaternion.LookRotation(forward.normalized, up);
            return new CharacterFootPlacementFootCalibration(
                ankle.InverseTransformPoint(heelContact),
                toe.InverseTransformPoint(toeContact),
                Quaternion.Inverse(ankle.rotation) * soleWorldRotation);
        }

        static Vector3 ProjectToGround(Vector3 point, Vector3 up, float groundHeight) =>
            point - up * (Vector3.Dot(point, up) - groundHeight);

        static void BuildSamplingRig()
        {
            if (!AssetDatabase.LoadAssetAtPath<GameObject>(SamplingRigPath))
            {
                var seed = new GameObject("TrainingEnemyMonsterFootPlacementSamplingRig");
                try
                {
                    PrefabUtility.SaveAsPrefabAsset(seed, SamplingRigPath);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(seed);
                }
            }

            GameObject root = PrefabUtility.LoadPrefabContents(SamplingRigPath);
            try
            {
                for (int i = root.transform.childCount - 1; i >= 0; i--)
                    UnityEngine.Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
                GameObject presentation = AssetDatabase.LoadAssetAtPath<GameObject>(PresentationPrefabPath) ??
                    throw new InvalidOperationException("Training Enemy Presentation Prefab is missing for the Sampling Rig.");
                GameObject nested = PrefabUtility.InstantiatePrefab(presentation, root.transform) as GameObject ??
                    throw new InvalidOperationException("Training Enemy Presentation Prefab could not be instantiated into the Sampling Rig.");
                nested.name = "Presentation";
                int playerLayer = LayerMask.NameToLayer("Player");
                if (playerLayer < 0)
                    throw new InvalidOperationException("Training Enemy Sampling Rig requires the formal Player collision layer.");
                root.layer = playerLayer;
                CharacterWorldAwarePresentationBinding world = root.GetComponent<CharacterWorldAwarePresentationBinding>() ??
                    root.AddComponent<CharacterWorldAwarePresentationBinding>();
                world.Configure(root.transform, root.transform);
                PrefabUtility.SaveAsPrefabAsset(root, SamplingRigPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            AssetDatabase.ImportAsset(SamplingRigPath, ImportAssetOptions.ForceUpdate);
        }

        static Material BuildMaterial(
            string path,
            string name,
            string baseMapPath,
            string normalMapPath,
            string metallicMapPath)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ??
                throw new InvalidOperationException("Universal Render Pipeline/Lit shader is unavailable.");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.SetTexture("_BaseMap", RequireTexture(baseMapPath));
            Texture2D normalMap = LoadOptionalTexture(normalMapPath);
            Texture2D metallicMap = LoadOptionalTexture(metallicMapPath);
            material.SetTexture("_BumpMap", normalMap);
            material.SetTexture("_MetallicGlossMap", metallicMap);
            material.SetFloat("_BumpScale", 1f);
            material.SetFloat("_Metallic", metallicMap ? 1f : 0f);
            material.SetFloat("_Smoothness", 0.5f);
            SetKeyword(material, "_NORMALMAP", normalMap);
            SetKeyword(material, "_METALLICSPECGLOSSMAP", metallicMap);
            EditorUtility.SetDirty(material);
            return material;
        }

        static Texture2D RequireTexture(string path) =>
            AssetDatabase.LoadAssetAtPath<Texture2D>(path) ??
            throw new InvalidOperationException($"Training Enemy material texture is missing: {path}");

        static Texture2D LoadOptionalTexture(string path) =>
            string.IsNullOrEmpty(path) ? null : RequireTexture(path);

        static void SetKeyword(Material material, string keyword, bool enabled)
        {
            if (enabled)
                material.EnableKeyword(keyword);
            else
                material.DisableKeyword(keyword);
        }

        static void ApplyMaterials(GameObject root, Material body, Material weapon)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                throw new InvalidOperationException("Training Enemy monster Presentation has no Renderers.");
            foreach (Renderer renderer in renderers)
            {
                bool isWeaponRenderer = renderer.name.IndexOf("Weapon", StringComparison.OrdinalIgnoreCase) >= 0;
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                    materials[i] = isWeaponRenderer ? weapon : body;
                renderer.sharedMaterials = materials;
                EditorUtility.SetDirty(renderer);
            }
        }

        static CharacterAnimationBlendProfile BuildBlendProfile(CharacterAnimationRigDefinition rig)
        {
            CharacterAnimationBlendProfile profile = LoadOrCreate<CharacterAnimationBlendProfile>(
                BlendProfilePath,
                "Training Enemy Monster Full Body Blend Profile");
            CharacterAnimationBoneDurationMultiplier[] bones = Enumerable.Range(0, rig.PoseBoneCount)
                .Select(index => new CharacterAnimationBoneDurationMultiplier(rig.GetPoseBoneId(index), 1f))
                .ToArray();
            profile.Configure("training-enemy.monster.full-body", rig, 1f, bones);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        static CharacterAnimationBlendPolicy BuildBlendPolicy(
            CharacterAnimationRigDefinition rig,
            CharacterAnimationBlendProfile blendProfile)
        {
            CharacterAnimationBlendPolicy policy = LoadOrCreate<CharacterAnimationBlendPolicy>(
                BlendPolicyPath,
                "Training Enemy Monster Full Body Blend Policy");
            var stack = new CharacterAnimationBlendStackPolicy();
            stack.Configure(4, AnimationStoredPosePolicy.CompressOldest, 0.05f, 1f);
            var transition = new CharacterAnimationBlendTransitionRule();
            transition.Configure(0.15f, CharacterAnimationBlendMode.EaseInOut, null, blendProfile);
            policy.Configure(
                "training-enemy.monster.full-body",
                "training-enemy.monster.full-body-v1",
                stack,
                transition,
                Array.Empty<CharacterAnimationBlendTransitionOverride>(),
                rig);
            EditorUtility.SetDirty(policy);
            return policy;
        }

        static CharacterPresentationPoseGraphAsset BuildPoseGraph(
            CharacterAnimationBlendPolicy blendPolicy,
            CharacterFullBodyIkProfile ikProfile,
            CharacterFootPlacementProfile footPlacementProfile,
            CharacterFootPlacementRigCalibration footCalibration)
        {
            CharacterPresentationPoseGraphAsset asset = LoadOrCreate<CharacterPresentationPoseGraphAsset>(
                PoseGraphPath,
                "Training Enemy Monster Pose Graph");
            CharacterBlendSpacePoseSourceSlot slot = LoadSubAsset<CharacterBlendSpacePoseSourceSlot>(
                PoseGraphPath,
                LocomotionSlotName);
            if (!slot)
            {
                slot = ScriptableObject.CreateInstance<CharacterBlendSpacePoseSourceSlot>();
                slot.name = LocomotionSlotName;
                AssetDatabase.AddObjectToAsset(slot, asset);
            }

            var speed = new PoseNodeId("training-enemy.monster.pose.speed");
            var footPlacementWeight = new PoseNodeId("training-enemy.monster.pose.foot-placement-weight");
            var locomotion = new PoseNodeId("training-enemy.monster.pose.locomotion");
            var action = new PoseNodeId("training-enemy.monster.pose.action-input");
            var actionSlot = new PoseNodeId("training-enemy.monster.pose.action-slot");
            var parameterResolve = new PoseNodeId("training-enemy.monster.pose.parameter-resolve");
            var toComponent = new PoseNodeId("training-enemy.monster.pose.to-component");
            var footPlacement = new PoseNodeId("training-enemy.monster.pose.foot-placement");
            var fullBodyIk = new PoseNodeId("training-enemy.monster.pose.full-body-ik");
            var toLocal = new PoseNodeId("training-enemy.monster.pose.to-local");
            var output = new PoseNodeId("training-enemy.monster.pose.output");
            var channel = new AnimationChannelId("training-enemy.monster.animation.full-body");
            var graph = new CharacterTypedPoseGraph(
                new PoseGraphId("training-enemy.monster.pose-graph"),
                "training-enemy.monster.pose-graph-v2",
                new[]
                {
                    new CharacterPoseParameterDeclaration(
                        AnimationPoseParameterIds.MotorPlanarSpeed,
                        PoseParameterValueType.Float,
                        0f,
                        "m/s"),
                    new CharacterPoseParameterDeclaration(
                        AnimationPoseParameterIds.FootPlacementWeight,
                        PoseParameterValueType.Float,
                        1f,
                        string.Empty)
                },
                new[]
                {
                    new CharacterTypedPoseNode(speed, "Motor Planar Speed", new CharacterProgramParameterInputPosePayload(AnimationPoseParameterIds.MotorPlanarSpeed)),
                    new CharacterTypedPoseNode(footPlacementWeight, "Foot Placement Weight", new CharacterProgramParameterInputPosePayload(AnimationPoseParameterIds.FootPlacementWeight)),
                    new CharacterTypedPoseNode(locomotion, "Monster Locomotion", new CharacterBlendSpacePlayerPosePayload(slot, CharacterAnimationBlendSpaceInputRangePolicy.Clamp)),
                    new CharacterTypedPoseNode(action, "Monster Action Playback", new CharacterActionPlaybackInputPosePayload(channel)),
                    new CharacterTypedPoseNode(actionSlot, "Monster Full Body Action", new CharacterAnimationSlotPosePayload(new AnimationSlotId("training-enemy.monster.full-body"), channel, AnimationSelectionAvailabilityPolicy.AllowEmpty, blendPolicy)),
                    new CharacterTypedPoseNode(
                        parameterResolve,
                        "Resolve Monster Pose Parameters",
                        new CharacterPoseParameterResolvePayload(
                            new[]
                            {
                                new CharacterPoseParameterPolicy(
                                    AnimationPoseParameterIds.MotorPlanarSpeed,
                                    PoseParameterResolvePolicy.Base),
                                new CharacterPoseParameterPolicy(
                                    AnimationPoseParameterIds.FootPlacementWeight,
                                    PoseParameterResolvePolicy.Weighted)
                            })),
                    new CharacterTypedPoseNode(toComponent, "Monster Local To Component", new CharacterLocalToComponentPosePayload()),
                    new CharacterTypedPoseNode(footPlacement, "Monster Foot Grounding", new CharacterFootGroundingPosePayload(footPlacementProfile, footCalibration)),
                    new CharacterTypedPoseNode(
                        fullBodyIk,
                        "Monster Full Body IK",
                        new CharacterFullBodyIkPosePayload(ikProfile),
                        new[]
                        {
                            new CharacterPoseDynamicPort(
                                new PosePortId("foot-goals"),
                                "Foot Goals",
                                CharacterPosePortKind.FullBodyIkGoals,
                                CharacterPosePortDirection.Input,
                                true,
                                0)
                        }),
                    new CharacterTypedPoseNode(toLocal, "Monster Component To Local", new CharacterComponentToLocalPosePayload()),
                    new CharacterTypedPoseNode(output, "Output", new CharacterOutputPosePayload())
                },
                new[]
                {
                    new CharacterPoseEdge("training-enemy.monster.edge.speed", speed, new PosePortId("parameter"), locomotion, new PosePortId("x")),
                    new CharacterPoseEdge("training-enemy.monster.edge.foot-placement-weight", footPlacementWeight, new PosePortId("parameter"), footPlacement, new PosePortId("weight")),
                    new CharacterPoseEdge("training-enemy.monster.edge.locomotion", locomotion, new PosePortId("pose"), actionSlot, new PosePortId("source-pose")),
                    new CharacterPoseEdge("training-enemy.monster.edge.action", action, new PosePortId("action-playback"), actionSlot, new PosePortId("action-playback")),
                    new CharacterPoseEdge("training-enemy.monster.edge.parameter-resolve-base", actionSlot, new PosePortId("pose"), parameterResolve, new PosePortId("base-pose")),
                    new CharacterPoseEdge("training-enemy.monster.edge.parameter-resolve-source", actionSlot, new PosePortId("pose"), parameterResolve, new PosePortId("parameter-source-pose")),
                    new CharacterPoseEdge("training-enemy.monster.edge.to-component", parameterResolve, new PosePortId("pose"), toComponent, new PosePortId("local-pose")),
                    new CharacterPoseEdge("training-enemy.monster.edge.foot-placement-pose", toComponent, new PosePortId("component-pose"), footPlacement, new PosePortId("pose")),
                    new CharacterPoseEdge("training-enemy.monster.edge.full-body-ik", toComponent, new PosePortId("component-pose"), fullBodyIk, new PosePortId("pose")),
                    new CharacterPoseEdge("training-enemy.monster.edge.foot-placement-goals", footPlacement, new PosePortId("goals"), fullBodyIk, new PosePortId("foot-goals")),
                    new CharacterPoseEdge("training-enemy.monster.edge.to-local", fullBodyIk, new PosePortId("result"), toLocal, new PosePortId("component-pose")),
                    new CharacterPoseEdge("training-enemy.monster.edge.output", toLocal, new PosePortId("local-pose"), output, new PosePortId("pose"))
                },
                new[]
                {
                    new CharacterPoseGraphLayoutEntry(speed, new Vector2(-720f, -160f)),
                    new CharacterPoseGraphLayoutEntry(footPlacementWeight, new Vector2(260f, 260f)),
                    new CharacterPoseGraphLayoutEntry(locomotion, new Vector2(-480f, -160f)),
                    new CharacterPoseGraphLayoutEntry(action, new Vector2(-480f, 160f)),
                    new CharacterPoseGraphLayoutEntry(actionSlot, Vector2.zero),
                    new CharacterPoseGraphLayoutEntry(parameterResolve, new Vector2(240f, 0f)),
                    new CharacterPoseGraphLayoutEntry(toComponent, new Vector2(500f, 0f)),
                    new CharacterPoseGraphLayoutEntry(footPlacement, new Vector2(500f, 180f)),
                    new CharacterPoseGraphLayoutEntry(fullBodyIk, new Vector2(740f, 0f)),
                    new CharacterPoseGraphLayoutEntry(toLocal, new Vector2(980f, 0f)),
                    new CharacterPoseGraphLayoutEntry(output, new Vector2(1220f, 0f))
                });
            asset.SetGraph(graph);
            asset.SetSourceSlots(new CharacterPresentationPoseSourceSlot[] { slot });
            EditorUtility.SetDirty(slot);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        static CharacterAnimationPresentationProfile BuildProfile(
            CharacterAnimationRigDefinition rig,
            CharacterFullBodyIkProfile ikProfile,
            CharacterAnimationBlendSpaceAsset blendSpace,
            CharacterPresentationPoseGraphAsset poseGraph,
            CharacterFootPlacementAnalysisSource footAnalysisSource)
        {
            CharacterAnimationPresentationProfile profile = LoadOrCreate<CharacterAnimationPresentationProfile>(
                ProfilePath,
                "Training Enemy Monster Animation Presentation Profile");
            if (!footAnalysisSource || footAnalysisSource.AnalysisSourceId.Value != FootAnalysisIdentity)
                throw new InvalidOperationException("Training Enemy Foot Analysis Source identity is invalid.");
            CharacterBlendSpacePoseSourceSlot slot = poseGraph.SourceSlots
                .OfType<CharacterBlendSpacePoseSourceSlot>()
                .Single(value => string.Equals(value.name, LocomotionSlotName, StringComparison.Ordinal));
            CharacterBlendSpacePoseSourceBinding binding = LoadSubAsset<CharacterBlendSpacePoseSourceBinding>(
                ProfilePath,
                LocomotionBindingName);
            if (!binding)
            {
                binding = ScriptableObject.CreateInstance<CharacterBlendSpacePoseSourceBinding>();
                binding.name = LocomotionBindingName;
                AssetDatabase.AddObjectToAsset(binding, profile);
            }
            binding.Configure(slot, blendSpace, rig, FootAnalysisIdentity);
            profile.SetPresentationGraph(poseGraph, rig);
            profile.SetFullBodyIkProfile(ikProfile);
            profile.SetPoseSourceBindings(new CharacterPresentationPoseSourceBinding[] { binding });
            profile.SetMotionMatchingProfile(null);
            profile.SetFootPlacementAnalysis(
                CharacterFootPlacementAnalysisMode.GeneratedPerFootFeatures,
                AssetDatabase.AssetPathToGUID(FootAnalysisSourcePath));
            var errors = new List<string>();
            if (!profile.CollectConfigurationErrors(errors))
                throw new InvalidOperationException(string.Join("\n", errors));
            EditorUtility.SetDirty(binding);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        static void BindPrefab(
            Animator animator,
            CharacterAnimationRigDefinition rig,
            Transform[] bones)
        {
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            CharacterAnimationRigBinding binding = animator.GetComponent<CharacterAnimationRigBinding>() ??
                animator.gameObject.AddComponent<CharacterAnimationRigBinding>();
            binding.Configure(animator, new CharacterAnimationRigPayload(rig), bones);
            EditorUtility.SetDirty(animator);
            EditorUtility.SetDirty(binding);
        }

        static Transform RequireBone(IEnumerable<Transform> bones, string name)
        {
            Transform[] matches = bones.Where(value => string.Equals(value.name, name, StringComparison.Ordinal)).ToArray();
            return matches.Length == 1
                ? matches[0]
                : throw new InvalidOperationException($"Training Enemy monster rig requires exactly one '{name}' Bone.");
        }

        static string BoneId(Transform root, Transform bone)
        {
            if (bone == root)
                return "animation-bone/root";
            var segments = new Stack<string>();
            for (Transform current = bone; current && current != root; current = current.parent)
                segments.Push(SanitizeIdentity(current.name));
            return "animation-bone/" + string.Join("/", segments);
        }

        static string SanitizeIdentity(string value)
        {
            char[] chars = value.Select(character =>
                    char.IsLetterOrDigit(character) || character == '.' || character == '_' || character == '-'
                        ? character
                        : '_')
                .ToArray();
            return new string(chars);
        }

        static T LoadOrCreate<T>(string path, string name) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset)
                return asset;
            asset = ScriptableObject.CreateInstance<T>();
            asset.name = name;
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static T LoadSubAsset<T>(string path, string name) where T : UnityEngine.Object =>
            AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<T>()
                .SingleOrDefault(value => string.Equals(value.name, name, StringComparison.Ordinal));

        static void EnsureFolders()
        {
            foreach (string path in new[]
                     {
                         RootPath,
                         RootPath + "/Rig",
                         RootPath + "/FootPlacement",
                         RootPath + "/Clips",
                         MaterialPath,
                         RootPath + "/Locomotion",
                         RootPath + "/Blend",
                         RootPath + "/PoseGraph",
                         RootPath + "/Profile"
                     })
                EnsureFolder(path);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (string.IsNullOrWhiteSpace(parent) || string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException($"Invalid asset folder '{path}'.");
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
