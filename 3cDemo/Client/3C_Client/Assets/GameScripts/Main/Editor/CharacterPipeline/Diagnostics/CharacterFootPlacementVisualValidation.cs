using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    [InitializeOnLoad]
    static class CharacterFootPlacementVisualValidation
    {
        const string EnabledKey = "ThirdPerson.CharacterFootPlacement.VisualValidation.Enabled.v1";
        static readonly Guid DiagnosticsOwnerId =
            new Guid("3e63c2cf-3796-4a28-b5bd-4d0e56bf5f7e");
        static CharacterFootPlacementVisualValidationBehaviour s_Behaviour;

        static CharacterFootPlacementVisualValidation()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update += EnsureBehaviour;
            AnimationPresentationRuntimeTargetRegistry.TargetRegistered += OnTargetRegistered;
        }

        internal static bool IsEnabled => EditorPrefs.GetBool(EnabledKey, false);

        internal static void Enable()
        {
            EditorPrefs.SetBool(EnabledKey, true);
            EnsureBehaviour();
            ConfigureTargets();
            SceneView.RepaintAll();
        }

        internal static void Disable()
        {
            EditorPrefs.SetBool(EnabledKey, false);
            RemoveDiagnosticsInterest();
            DestroyBehaviour();
            SceneView.RepaintAll();
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                EnsureBehaviour();
                ConfigureTargets();
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                RemoveDiagnosticsInterest();
                DestroyBehaviour();
            }
        }

        static void OnTargetRegistered(AnimationPresentationRuntimeTarget target)
        {
            if (IsEnabled)
                ConfigureTarget(target);
        }

        static void ConfigureTargets()
        {
            if (!EditorApplication.isPlaying || !IsEnabled)
                return;
            IReadOnlyList<AnimationPresentationRuntimeTarget> targets =
                AnimationPresentationRuntimeTargetRegistry.Targets;
            for (int i = 0; i < targets.Count; i++)
                ConfigureTarget(targets[i]);
        }

        static void ConfigureTarget(AnimationPresentationRuntimeTarget target)
        {
            if (target == null)
                return;
            target.SetDiagnosticsInterest(
                DiagnosticsOwnerId,
                AnimationPresentationDiagnosticsInterest.Capture |
                AnimationPresentationDiagnosticsInterest.OperationDetail |
                AnimationPresentationDiagnosticsInterest.FinalPoseDetail);
        }

        static void RemoveDiagnosticsInterest()
        {
            IReadOnlyList<AnimationPresentationRuntimeTarget> targets =
                AnimationPresentationRuntimeTargetRegistry.Targets;
            for (int i = 0; i < targets.Count; i++)
                targets[i]?.RemoveDiagnosticsInterest(DiagnosticsOwnerId);
        }

        static void EnsureBehaviour()
        {
            if (!EditorApplication.isPlaying || !IsEnabled || s_Behaviour)
                return;
            GameObject root = new GameObject("Foot Placement Visual Validation");
            root.hideFlags = HideFlags.HideAndDontSave;
            s_Behaviour = root.AddComponent<CharacterFootPlacementVisualValidationBehaviour>();
        }

        static void DestroyBehaviour()
        {
            if (!s_Behaviour)
                return;
            UnityEngine.Object.DestroyImmediate(s_Behaviour.gameObject);
            s_Behaviour = null;
        }
    }

    [ExecuteAlways]
    sealed class CharacterFootPlacementVisualValidationBehaviour : MonoBehaviour
    {
        static readonly int ZTest = Shader.PropertyToID("_ZTest");
        static readonly int Cull = Shader.PropertyToID("_Cull");
        static readonly int ZWrite = Shader.PropertyToID("_ZWrite");
        static readonly int SrcBlend = Shader.PropertyToID("_SrcBlend");
        static readonly int DstBlend = Shader.PropertyToID("_DstBlend");
        readonly List<CharacterWorldAwarePresentationBinding> m_Bindings =
            new List<CharacterWorldAwarePresentationBinding>();
        Material m_Material;
        double m_NextBindingRefresh;

        void OnEnable()
        {
            hideFlags = HideFlags.HideAndDontSave;
            m_Material = CreateMaterial();
            RefreshBindings();
        }

        void OnDisable()
        {
            if (m_Material)
                DestroyImmediate(m_Material);
            m_Material = null;
            m_Bindings.Clear();
        }

        void Update()
        {
            if (!Application.isPlaying)
                return;
            if (EditorApplication.timeSinceStartup >= m_NextBindingRefresh)
            {
                RefreshBindings();
                m_NextBindingRefresh = EditorApplication.timeSinceStartup + 0.25d;
            }
            SceneView.RepaintAll();
        }

        void OnRenderObject()
        {
            if (!Application.isPlaying || !m_Material || Camera.current == null)
                return;
            m_Material.SetPass(0);
            GL.PushMatrix();
            GL.MultMatrix(Matrix4x4.identity);
            GL.Begin(GL.LINES);
            for (int i = 0; i < m_Bindings.Count; i++)
                DrawBinding(m_Bindings[i]);
            GL.End();
            GL.PopMatrix();
        }

        void RefreshBindings()
        {
            m_Bindings.Clear();
            CharacterWorldAwarePresentationBinding[] bindings =
                FindObjectsByType<CharacterWorldAwarePresentationBinding>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None);
            for (int i = 0; i < bindings.Length; i++)
            {
                CharacterWorldAwarePresentationBinding binding = bindings[i];
                if (binding && binding.PresentationRoot)
                    m_Bindings.Add(binding);
            }
        }

        static Material CreateMaterial()
        {
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            if (!shader)
                return null;
            var material = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            material.SetInt(ZTest, (int)CompareFunction.Always);
            material.SetInt(Cull, (int)CullMode.Off);
            material.SetInt(ZWrite, 0);
            material.SetInt(SrcBlend, (int)BlendMode.SrcAlpha);
            material.SetInt(DstBlend, (int)BlendMode.OneMinusSrcAlpha);
            return material;
        }

        static void DrawBinding(CharacterWorldAwarePresentationBinding binding)
        {
            if (!CharacterFootLandingPredictionDebugRegistry.TryGet(
                    binding.PresentationRoot.GetInstanceID(),
                    out CharacterFootLandingPredictionDiagnostics diagnostics))
                return;
            DrawFoot(diagnostics.Left);
            DrawFoot(diagnostics.Right);
            DrawStride(diagnostics.StrideHips);
            DrawFinalPose(binding);
        }

        static void DrawFoot(CharacterFootLandingPredictionFootDiagnostics foot)
        {
            CharacterFootGroundPathDiagnostics path = foot.GroundPath;
            Color pathColor = foot.Side == CharacterFootSide.Left
                ? new Color(0.1f, 0.85f, 1f, 0.95f)
                : new Color(1f, 0.25f, 0.75f, 0.95f);
            if (path.Accepted && path.EnvelopeVertexCount > 1)
            {
                Vector3 previous = path.EnvelopeVertexAt(0).Position;
                for (int i = 1; i < path.EnvelopeVertexCount; i++)
                {
                    Vector3 current = path.EnvelopeVertexAt(i).Position;
                    Line(previous, current, pathColor);
                    previous = current;
                }
            }
            Marker(path.LastLanding, path.ComponentUp, Color.green, 0.08f);
            if (foot.Accepted)
                Marker(foot.LandingPoint, path.ComponentUp, Color.yellow, 0.07f);

            CharacterFootSwingMotionDiagnostics motion = foot.FootMotion;
            if (!motion.Accepted)
                return;
            Marker(motion.OriginalSole, path.ComponentUp, Color.white, 0.05f);
            Marker(motion.CorrectedSole, path.ComponentUp, SupportColor(motion.SupportLockState), 0.065f);
            if (motion.BaselineSample != default)
                Line(motion.OriginalSole, motion.BaselineSample, new Color(0.2f, 1f, 0.25f, 0.8f));
            if (motion.EnvelopeSample != default)
                Line(motion.OriginalSole, motion.EnvelopeSample, new Color(1f, 0.8f, 0.1f, 0.8f));
        }

        static void DrawStride(in CharacterFootStrideHipsDiagnostics stride)
        {
            if (!stride.Accepted)
                return;
            Line(stride.StrideStart, stride.StrideEnd, new Color(1f, 0.8f, 0.1f, 0.95f));
            Marker(stride.AnimatedPelvis + stride.PelvisDelta, Vector3.up, new Color(1f, 0.8f, 0.1f, 1f), 0.09f);
        }

        static void DrawFinalPose(CharacterWorldAwarePresentationBinding binding)
        {
            CharacterPipelineHost host = binding.GetComponentInParent<CharacterPipelineHost>();
            if (!host || !host.AnimationRigBinding || !host.AnimationRigBinding.Animator)
                return;
            if (!TryGetDebugViewForRoot(
                    binding.PresentationRoot.GetInstanceID(),
                    out AnimationPresentationRuntimeTarget target,
                    out AnimationPresentationDebugView debugView))
                return;
            AnimationFootPlacementRuntimeSnapshot foot = debugView.PosePlan.FootPlacement;
            if (!foot.IsAvailable)
                return;
            Transform root = host.AnimationRigBinding.Animator.transform;
            DrawFinalEffector(root, foot.LeftGoal, foot.LeftFoot, foot.LeftPhysicalAnkleComponentPosition, Color.cyan);
            DrawFinalEffector(root, foot.RightGoal, foot.RightFoot, foot.RightPhysicalAnkleComponentPosition, Color.magenta);
            DrawFinalPelvis(root, foot.Pelvis, foot.PhysicalPelvisComponentPosition);
            IReadOnlyList<Transform> bones = host.AnimationRigBinding.PhysicalBones;
            for (int i = 0; i < bones.Count; i++)
            {
                Transform bone = bones[i];
                if (!bone || !bone.parent || !bone.parent.IsChildOf(root))
                    continue;
                Line(bone.parent.position, bone.position, new Color(1f, 0.15f, 0.1f, 0.65f));
            }
        }

        static bool TryGetDebugViewForRoot(
            int rootInstanceId,
            out AnimationPresentationRuntimeTarget target,
            out AnimationPresentationDebugView debugView)
        {
            IReadOnlyList<AnimationPresentationRuntimeTarget> targets =
                AnimationPresentationRuntimeTargetRegistry.Targets;
            for (int i = 0; i < targets.Count; i++)
            {
                if (!targets[i].TryGetDebugView(out debugView) ||
                    !debugView.PosePlan.FootPlacement.IsAvailable ||
                    debugView.PosePlan.FootPlacement.LandingPrediction.RootInstanceId != rootInstanceId)
                    continue;
                target = targets[i];
                return true;
            }
            target = null;
            debugView = null;
            return false;
        }

        static void DrawFinalEffector(
            Transform root,
            CharacterFullBodyIkGoal goal,
            CharacterFullBodyIkEffectorDiagnostics solved,
            Vector3 physical,
            Color color)
        {
            Vector3 goalPosition = root.TransformPoint(goal.ComponentPosition);
            Vector3 solvedPosition = root.TransformPoint(solved.SolvedComponentPosition);
            Vector3 physicalPosition = root.TransformPoint(physical);
            Marker(goalPosition, root.up, Color.white, 0.07f);
            Marker(solvedPosition, root.up, color, 0.055f);
            Marker(physicalPosition, root.up, Color.red, 0.065f);
            Line(goalPosition, solvedPosition, color);
            Line(solvedPosition, physicalPosition, Color.red);
        }

        static void DrawFinalPelvis(
            Transform root,
            CharacterFullBodyIkEffectorDiagnostics pelvis,
            Vector3 physical)
        {
            Vector3 solved = root.TransformPoint(pelvis.SolvedComponentPosition);
            Vector3 physicalPosition = root.TransformPoint(physical);
            Marker(solved, root.up, new Color(1f, 0.8f, 0.1f, 1f), 0.075f);
            Marker(physicalPosition, root.up, Color.red, 0.08f);
            Line(solved, physicalPosition, Color.red);
        }

        static void Marker(Vector3 position, Vector3 normal, Color color, float size)
        {
            Vector3 axis = normal.sqrMagnitude > 0.000001f ? normal.normalized : Vector3.up;
            Vector3 tangent = Vector3.Cross(axis, Vector3.right);
            if (tangent.sqrMagnitude <= 0.000001f)
                tangent = Vector3.Cross(axis, Vector3.forward);
            tangent.Normalize();
            Vector3 bitangent = Vector3.Cross(axis, tangent).normalized;
            Line(position - tangent * size, position + tangent * size, color);
            Line(position - bitangent * size, position + bitangent * size, color);
            Line(position - axis * size, position + axis * size, color);
        }

        static void Line(Vector3 from, Vector3 to, Color color)
        {
            GL.Color(color);
            GL.Vertex(from);
            GL.Vertex(to);
        }

        static Color SupportColor(CharacterFootSupportLockState state) =>
            state switch
            {
                CharacterFootSupportLockState.Locked => new Color(0.2f, 1f, 0.25f, 1f),
                CharacterFootSupportLockState.Sliding => new Color(1f, 0.8f, 0.1f, 1f),
                CharacterFootSupportLockState.Unlocked => new Color(1f, 0.25f, 0.15f, 1f),
                _ => Color.white
            };
    }

    internal static class CharacterFootPlacementVisualValidationMenu
    {
        [MenuItem("Tools/3C/Diagnostics/Foot IK Visual Validation/Enable")]
        static void Enable() => CharacterFootPlacementVisualValidation.Enable();

        [MenuItem("Tools/3C/Diagnostics/Foot IK Visual Validation/Disable")]
        static void Disable() => CharacterFootPlacementVisualValidation.Disable();

        [MenuItem("Tools/3C/Diagnostics/Foot IK Visual Validation/Enable", true)]
        static bool ValidateEnable() => !CharacterFootPlacementVisualValidation.IsEnabled;

        [MenuItem("Tools/3C/Diagnostics/Foot IK Visual Validation/Disable", true)]
        static bool ValidateDisable() => CharacterFootPlacementVisualValidation.IsEnabled;
    }
}
