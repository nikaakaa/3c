using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Editor.RootMotion;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    static class TrainingEnemyAnimationAssetAuthoring
    {
        const string ModelPath = "Assets/AssetArt/Animation/ZZZ/敌人/怪兽/怪兽.fbx";
        const string RootMotionPath = "Assets/Configs/Character/TrainingEnemy/Presentation/RootMotion";
        const string CurvePath = RootMotionPath + "/Curves";
        const string BakeControllerPath = RootMotionPath + "/TrainingEnemyMonsterRootMotionBake.controller";
        const string MotionNodeName = "Bip001";
        const float CurveSampleRate = 60f;

        static readonly string[] s_LoopingClipNames =
        {
            "Goblin_Ani_Idle",
            "Goblin_Ani_Run",
            "Goblin_Ani_Walk_F",
            "Goblin_Ani_Walk_B",
            "Goblin_Ani_Walk_L",
            "Goblin_Ani_Walk_R",
            "Goblin_Ani_Hit_Stay",
            "Goblin_Ani_Stun_Hit_Stay",
            "Goblin_Ani_Debuff_Stun_Loop",
            "Goblin_Ani_Death_Stay"
        };

        static readonly string[] s_OneShotClipNames =
        {
            "Monster_Goblin_Ani_Hit_Shake",
            "Goblin_Ani_Stun_Hit_L_Front",
            "Goblin_Ani_Attack_04_01",
            "Goblin_Ani_Walk_L_Start",
            "Goblin_Ani_Debuff_Stun_End",
            "Goblin_Ani_Run_End",
            "Goblin_Ani_Attack_05_Start",
            "Goblin_Ani_Run_Start",
            "Goblin_Ani_Attack_02",
            "Goblin_Ani_Hit_L_Back",
            "Goblin_Ani_Walk_B_Start",
            "Goblin_Ani_Attack_05",
            "Goblin_Ani_Stun_Hit_H_Back",
            "Goblin_Ani_Attack_02_Stamp",
            "Goblin_Ani_Death_Hit_Back",
            "Goblin_Ani_Hit_L_Front",
            "Goblin_Ani_Attack_05_Miss_2",
            "Goblin_Ani_Attack_06",
            "Goblin_Ani_Walk_R_Start",
            "Goblin_Ani_Stun_Hit_L_Back",
            "Goblin_Ani_Death_Hit_Front",
            "Goblin_Ani_Attack_07",
            "Goblin_Ani_Attack_04",
            "Goblin_Ani_Debuff_Stun_Start",
            "Goblin_Ani_Attack_03",
            "Goblin_Ani_Walk_F_Start",
            "Goblin_Ani_Attack_01",
            "Goblin_Ani_Stun_Hit_H_Front",
            "Goblin_Ani_Born",
            "Goblin_Ani_Attack_05_Full"
        };

        static readonly string[] s_RequiredClipNames = s_LoopingClipNames
            .Concat(s_OneShotClipNames)
            .ToArray();

        public static IReadOnlyList<string> RequiredClipNames => s_RequiredClipNames;

        public static void ConfigureImporter()
        {
            if (AssetImporter.GetAtPath(ModelPath) is not ModelImporter importer)
                throw new InvalidOperationException($"Training Enemy monster FBX has no ModelImporter: {ModelPath}");
            if (importer.animationType != ModelImporterAnimationType.Generic)
                throw new InvalidOperationException("Training Enemy monster FBX must use the Generic animation rig.");

            ModelImporterClipAnimation[] importedAnimations = importer.clipAnimations;
            Dictionary<string, ModelImporterClipAnimation> clips = importedAnimations
                .ToDictionary(value => value.name, StringComparer.Ordinal);
            string[] missing = s_RequiredClipNames.Where(value => !clips.ContainsKey(value)).ToArray();
            if (missing.Length > 0)
                throw new InvalidOperationException($"Training Enemy monster FBX is missing clips: {string.Join(", ", missing)}.");

            HashSet<string> required = s_RequiredClipNames.ToHashSet(StringComparer.Ordinal);
            ModelImporterClipAnimation[] animations = importedAnimations
                .Where(value => required.Contains(value.name))
                .ToArray();
            bool changed = animations.Length != importedAnimations.Length;
            changed |= SetMotionNode(importer, MotionNodeName);
            changed |= SetImportQuality(importer);
            HashSet<string> looping = s_LoopingClipNames.ToHashSet(StringComparer.Ordinal);
            for (int i = 0; i < animations.Length; i++)
            {
                ModelImporterClipAnimation clip = animations[i];
                changed |= ConfigureClip(clip, looping.Contains(clip.name));
            }

            if (!changed)
                return;
            importer.clipAnimations = animations;
            importer.SaveAndReimport();
        }

        public static int BakeRootMotionCurves(
            IReadOnlyDictionary<string, AnimationClip> clips,
            GameObject sampleObject)
        {
            if (clips == null)
                throw new ArgumentNullException(nameof(clips));
            if (!sampleObject)
                throw new ArgumentNullException(nameof(sampleObject));

            EnsureFolder(CurvePath);
            AnimatorController controller = BuildBakeController(clips["Goblin_Ani_Idle"]);
            int count = 0;
            foreach (string clipName in s_RequiredClipNames)
            {
                AnimationClip clip = clips[clipName];
                RootMotionCurveAsset curve = LoadOrCreateCurve(clipName);
                RootMotionCurveBakingService.Bake(
                    clip,
                    sampleObject,
                    controller,
                    curve,
                    CurveSampleRate,
                    RootMotionCurveEvaluationMode.FullLocalDelta);
                RequireValidCurve(curve, clip);
                count++;
            }
            return count;
        }

        static bool SetMotionNode(ModelImporter importer, string motionNodeName)
        {
            if (string.Equals(importer.motionNodeName, motionNodeName, StringComparison.Ordinal))
                return false;
            importer.motionNodeName = motionNodeName;
            return true;
        }

        static bool SetImportQuality(ModelImporter importer)
        {
            bool changed = false;
            if (importer.animationCompression != ModelImporterAnimationCompression.Optimal)
            {
                importer.animationCompression = ModelImporterAnimationCompression.Optimal;
                changed = true;
            }
            if (!importer.resampleCurves)
            {
                importer.resampleCurves = true;
                changed = true;
            }
            if (!importer.removeConstantScaleCurves)
            {
                importer.removeConstantScaleCurves = true;
                changed = true;
            }
            changed |= SetError(importer.animationRotationError, 0.25f, value => importer.animationRotationError = value);
            changed |= SetError(importer.animationPositionError, 0.25f, value => importer.animationPositionError = value);
            changed |= SetError(importer.animationScaleError, 0.25f, value => importer.animationScaleError = value);
            if (importer.importAnimatedCustomProperties)
            {
                importer.importAnimatedCustomProperties = false;
                changed = true;
            }
            if (importer.importConstraints)
            {
                importer.importConstraints = false;
                changed = true;
            }
            return changed;
        }

        static bool SetError(float current, float expected, Action<float> assign)
        {
            if (Mathf.Approximately(current, expected))
                return false;
            assign(expected);
            return true;
        }

        static bool ConfigureClip(ModelImporterClipAnimation clip, bool loop)
        {
            bool changed = false;
            changed |= Set(clip.loopTime, loop, value => clip.loopTime = value);
            changed |= Set(clip.loopPose, loop, value => clip.loopPose = value);
            changed |= Set(clip.lockRootRotation, loop, value => clip.lockRootRotation = value);
            changed |= Set(clip.lockRootHeightY, loop, value => clip.lockRootHeightY = value);
            changed |= Set(clip.lockRootPositionXZ, false, value => clip.lockRootPositionXZ = value);
            changed |= Set(clip.keepOriginalOrientation, true, value => clip.keepOriginalOrientation = value);
            changed |= Set(clip.keepOriginalPositionY, true, value => clip.keepOriginalPositionY = value);
            changed |= Set(clip.keepOriginalPositionXZ, false, value => clip.keepOriginalPositionXZ = value);
            changed |= Set(clip.heightFromFeet, false, value => clip.heightFromFeet = value);
            changed |= Set(clip.mirror, false, value => clip.mirror = value);
            if (!Mathf.Approximately(clip.cycleOffset, 0f))
            {
                clip.cycleOffset = 0f;
                changed = true;
            }
            return changed;
        }

        static bool Set(bool current, bool expected, Action<bool> assign)
        {
            if (current == expected)
                return false;
            assign(expected);
            return true;
        }

        static AnimatorController BuildBakeController(AnimationClip referenceClip)
        {
            EnsureFolder(RootMotionPath);
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(BakeControllerPath);
            if (!controller)
                controller = AnimatorController.CreateAnimatorControllerAtPath(BakeControllerPath);
            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            AnimatorState bakeState = stateMachine.states
                .Select(value => value.state)
                .SingleOrDefault(value => string.Equals(value.name, "Bake", StringComparison.Ordinal));
            if (!bakeState)
                bakeState = stateMachine.AddState("Bake");
            foreach (ChildAnimatorState child in stateMachine.states)
            {
                if (child.state != bakeState)
                    stateMachine.RemoveState(child.state);
            }
            bakeState.motion = referenceClip;
            bakeState.writeDefaultValues = false;
            stateMachine.defaultState = bakeState;
            EditorUtility.SetDirty(bakeState);
            EditorUtility.SetDirty(stateMachine);
            EditorUtility.SetDirty(controller);
            return controller;
        }

        static RootMotionCurveAsset LoadOrCreateCurve(string clipName)
        {
            string path = $"{CurvePath}/{clipName}_RootMotion.asset";
            RootMotionCurveAsset asset = AssetDatabase.LoadAssetAtPath<RootMotionCurveAsset>(path);
            if (asset)
                return asset;
            asset = ScriptableObject.CreateInstance<RootMotionCurveAsset>();
            asset.name = clipName + " Root Motion";
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static void RequireValidCurve(RootMotionCurveAsset curve, AnimationClip clip)
        {
            if (!curve.TryValidate(out string error))
                throw new InvalidOperationException($"{clip.name}: {error}");
            if (curve.SourceClip != clip ||
                curve.Duration <= 0f ||
                !Mathf.Approximately(curve.SampleRate, CurveSampleRate) ||
                curve.LocalPositionX.length < 2 ||
                curve.LocalPositionY.length < 2 ||
                curve.LocalPositionZ.length < 2 ||
                curve.ForwardDistance.length < 2 ||
                curve.LocalYaw.length < 2)
            {
                throw new InvalidOperationException($"Root Motion curve '{curve.name}' is incomplete.");
            }
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
