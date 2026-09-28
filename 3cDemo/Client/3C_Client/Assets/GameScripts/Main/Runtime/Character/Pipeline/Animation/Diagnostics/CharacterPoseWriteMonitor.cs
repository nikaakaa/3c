#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using ThirdPersonCharacter.Pipeline.Presentation;
using UnityEngine;
using UnityEngine.Rendering;

namespace ThirdPersonCharacter.Pipeline.Animation.Diagnostics
{
    internal sealed class CharacterPoseWriteMonitor : IDisposable
    {
        const float PositionTolerance = 0.00001f;
        const float RotationToleranceDegrees = 0.1f;
        const float ScaleTolerance = 0.00001f;
        static readonly string[] s_Names =
        {
            "LogicRoot", "VisualRoot", "PoseRoot", "AnimationRoot", "Pelvis",
            "LeftHip", "LeftKnee", "LeftAnkle", "LeftToe",
            "RightHip", "RightKnee", "RightAnkle", "RightToe"
        };
        readonly string m_ActorId;
        readonly Transform[] m_Bones;
        readonly Snapshot[] m_Written = new Snapshot[13];
        ulong m_PresentationFrame;
        ulong m_CompletionIdentity;
        int m_UnityFrame = -1;
        int m_LastMismatchFrame = -2;
        bool m_Captured;

        internal CharacterPoseWriteMonitor(string actorId, CharacterAnimationRigBinding binding,
            CharacterAnimationRigPayload rig, CharacterRootHierarchyBinding roots)
        {
            m_ActorId = actorId;
            var bones = binding.PhysicalBones;
            m_Bones = new[]
            {
                roots.LogicRoot, roots.VisualRoot, roots.PoseRoot,
                bones[rig.RootPhysicalBoneIndex], bones[rig.PelvisPhysicalBoneIndex],
                bones[rig.LeftLeg.HipPhysicalBoneIndex], bones[rig.LeftLeg.KneePhysicalBoneIndex],
                bones[rig.LeftLeg.AnklePhysicalBoneIndex], bones[rig.LeftLeg.ToePhysicalBoneIndex],
                bones[rig.RightLeg.HipPhysicalBoneIndex], bones[rig.RightLeg.KneePhysicalBoneIndex],
                bones[rig.RightLeg.AnklePhysicalBoneIndex], bones[rig.RightLeg.ToePhysicalBoneIndex]
            };
            RenderPipelineManager.beginCameraRendering += OnBeforeCamera;
            RenderPipelineManager.endCameraRendering += OnAfterCamera;
        }

        internal void Capture(ulong presentationFrame, ulong completionIdentity)
        {
            m_PresentationFrame = presentationFrame;
            m_CompletionIdentity = completionIdentity;
            m_UnityFrame = Time.frameCount;
            for (int i = 0; i < m_Bones.Length; i++)
                m_Written[i] = new Snapshot(m_Bones[i]);
            m_Captured = true;
        }

        internal void Invalidate() => m_Captured = false;

        void OnBeforeCamera(ScriptableRenderContext context, Camera camera) => Check(camera, "BeforeRender");
        void OnAfterCamera(ScriptableRenderContext context, Camera camera) => Check(camera, "AfterRender");

        void Check(Camera camera, string stage)
        {
            if (!m_Captured || Time.frameCount != m_UnityFrame || camera.cameraType != CameraType.Game)
                return;
            int changed = 0;
            int first = -1;
            float maxPosition = 0;
            float maxRotation = 0;
            float maxScale = 0;
            for (int i = 0; i < m_Bones.Length; i++)
            {
                var actual = new Snapshot(m_Bones[i]);
                float position = Vector3.Distance(m_Written[i].Position, actual.Position);
                float rotation = Quaternion.Angle(m_Written[i].Rotation, actual.Rotation);
                float scale = Vector3.Distance(m_Written[i].Scale, actual.Scale);
                if (position <= PositionTolerance && rotation <= RotationToleranceDegrees && scale <= ScaleTolerance)
                    continue;
                if (first < 0)
                    first = i;
                changed++;
                maxPosition = Mathf.Max(maxPosition, position);
                maxRotation = Mathf.Max(maxRotation, rotation);
                maxScale = Mathf.Max(maxScale, scale);
            }
            if (changed == 0)
                return;
            bool report = m_UnityFrame > m_LastMismatchFrame + 1;
            m_LastMismatchFrame = m_UnityFrame;
            if (!report)
                return;
            Transform bone = m_Bones[first];
            Snapshot expected = m_Written[first];
            Debug.LogWarning(
                $"[PoseWriteMismatch] actor={m_ActorId} unityFrame={m_UnityFrame} presentationFrame={m_PresentationFrame} " +
                $"completion={m_CompletionIdentity} stage={stage} camera={camera.name}({camera.GetInstanceID()}) " +
                $"changed={changed} first={s_Names[first]}({bone.name}) " +
                $"maxPositionM={maxPosition:R} maxRotationDeg={maxRotation:R} maxScale={maxScale:R} " +
                $"expectedPosition={expected.Position.ToString("F6")} actualPosition={bone.localPosition.ToString("F6")} " +
                $"expectedRotation={expected.Rotation.ToString("F6")} actualRotation={bone.localRotation.ToString("F6")} " +
                $"expectedScale={expected.Scale.ToString("F6")} actualScale={bone.localScale.ToString("F6")}",
                bone);
        }

        public void Dispose()
        {
            m_Captured = false;
            RenderPipelineManager.beginCameraRendering -= OnBeforeCamera;
            RenderPipelineManager.endCameraRendering -= OnAfterCamera;
        }

        readonly struct Snapshot
        {
            internal Snapshot(Transform transform)
            {
                Position = transform.localPosition;
                Rotation = transform.localRotation;
                Scale = transform.localScale;
            }
            internal Vector3 Position { get; }
            internal Quaternion Rotation { get; }
            internal Vector3 Scale { get; }
        }
    }
}
#endif
