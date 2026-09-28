#if KK_DIAGNOSTIC_SAMPLING
using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Presentation;
using UnityEngine;
using UnityEngine.Rendering;

namespace ThirdPersonCharacter.Pipeline.Animation.Diagnostics
{
    public sealed class CharacterPoseRenderCaptureRuntime : IDisposable
    {
        static readonly Dictionary<Guid, CharacterPoseRenderCaptureRuntime> s_Targets =
            new Dictionary<Guid, CharacterPoseRenderCaptureRuntime>();
        static readonly string[] s_BoneNames =
        {
            "logic-root", "visual-root", "pose-root", "animation-root", "pelvis",
            "left-hip", "left-knee", "left-ankle", "left-toe",
            "right-hip", "right-knee", "right-ankle", "right-toe"
        };
        readonly Guid m_RuntimeId;
        readonly Camera m_Camera;
        readonly Transform[] m_Bones;
        readonly CharacterPoseRenderTransformSnapshot[] m_Written = new CharacterPoseRenderTransformSnapshot[13];
        readonly CharacterPoseRenderTransformSnapshot[] m_Before = new CharacterPoseRenderTransformSnapshot[13];
        readonly CharacterPoseRenderTransformSnapshot[] m_After = new CharacterPoseRenderTransformSnapshot[13];
        readonly CharacterPoseRenderBoneCaptureRow[] m_Rows = new CharacterPoseRenderBoneCaptureRow[13];
        CharacterPoseDiagnosticFrame m_Frame;
        CharacterNativePoseCaptureFrame m_Animation;
        CharacterNativeCameraCaptureFrame m_CameraFrame;
        CharacterNativeBodyCaptureFrame m_Facts;
        CharacterNativeCommandCaptureFrame m_Commands;
        int m_UnityFrame;
        double m_WrittenTime;
        double m_BeforeTime;
        double m_AfterTime;
        bool m_Pending;
        bool m_BeforeAvailable;
        bool m_AfterAvailable;

        internal CharacterPoseRenderCaptureRuntime(Guid runtimeId, CharacterAnimationRigBinding binding,
            CharacterAnimationRigPayload rig, CharacterRootHierarchyBinding roots, Camera camera)
        {
            m_RuntimeId = runtimeId;
            m_Camera = camera;
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
            s_Targets.Add(runtimeId, this);
            RenderPipelineManager.beginCameraRendering += OnBeforeCamera;
            RenderPipelineManager.endCameraRendering += OnAfterCamera;
        }

        internal void Capture(in CharacterPoseDiagnosticFrame frame,
            in CharacterNativePoseCaptureFrame animation, in CharacterNativeCameraCaptureFrame camera,
            in CharacterNativeBodyCaptureFrame facts, in CharacterNativeCommandCaptureFrame commands)
        {
            m_Frame = frame;
            m_Animation = animation;
            m_CameraFrame = camera;
            m_Facts = facts;
            m_Commands = commands;
            m_UnityFrame = Time.frameCount;
            m_WrittenTime = Time.realtimeSinceStartupAsDouble;
            m_BeforeTime = 0;
            m_AfterTime = 0;
            m_BeforeAvailable = false;
            m_AfterAvailable = false;
            Read(m_Written);
            m_Pending = true;
        }

        void OnBeforeCamera(ScriptableRenderContext context, Camera camera)
        {
            if (!m_Pending || camera != m_Camera || Time.frameCount != m_UnityFrame || m_BeforeAvailable)
                return;
            try
            {
                Read(m_Before);
                m_BeforeTime = Time.realtimeSinceStartupAsDouble;
                m_BeforeAvailable = true;
            }
            catch (Exception exception)
            {
                Fail(exception);
            }
        }

        void OnAfterCamera(ScriptableRenderContext context, Camera camera)
        {
            if (!m_Pending || camera != m_Camera || Time.frameCount != m_UnityFrame || !m_BeforeAvailable)
                return;
            try
            {
                Read(m_After);
                m_AfterTime = Time.realtimeSinceStartupAsDouble;
                m_AfterAvailable = true;
                Flush();
            }
            catch (Exception exception)
            {
                Fail(exception);
            }
        }

        void Read(CharacterPoseRenderTransformSnapshot[] output)
        {
            for (int i = 0; i < m_Bones.Length; i++)
                output[i] = new CharacterPoseRenderTransformSnapshot(m_Bones[i]);
        }

        public static void FlushPending(Guid runtimeId)
        {
            if (s_Targets.TryGetValue(runtimeId, out var capture))
                capture.Flush();
        }

        internal void Flush()
        {
            if (!m_Pending)
                return;
            m_Pending = false;
            try
            {
                for (int i = 0; i < m_Rows.Length; i++)
                {
                    var before = m_BeforeAvailable ? m_Before[i] : default;
                    var after = m_AfterAvailable ? m_After[i] : default;
                    m_Rows[i] = new CharacterPoseRenderBoneCaptureRow(s_BoneNames[i],
                        m_BeforeAvailable, m_AfterAvailable, in m_Written[i], in before, in after);
                }
                var render = new CharacterPoseRenderCaptureFrame(m_UnityFrame,
                    m_Camera ? m_Camera.GetInstanceID() : 0, m_BeforeAvailable, m_AfterAvailable,
                    m_WrittenTime, m_BeforeTime, m_AfterTime, m_Rows);
                CharacterNativePresentationDiagnosticEvent.Publish(m_RuntimeId, in m_Frame,
                    in m_Animation, in m_CameraFrame, in m_Facts, in m_Commands, in render);
            }
            catch (Exception exception)
            {
                Fail(exception);
            }
            finally
            {
                m_Animation = default;
                m_CameraFrame = default;
                m_Facts = default;
                m_Commands = default;
            }
        }

        void Fail(Exception exception)
        {
            m_Pending = false;
            CharacterPoseCaptureFailure.Report(m_RuntimeId,
                CharacterNativePresentationDiagnosticEvent.EventId, exception);
        }

        public void Dispose()
        {
            Flush();
            RenderPipelineManager.beginCameraRendering -= OnBeforeCamera;
            RenderPipelineManager.endCameraRendering -= OnAfterCamera;
            s_Targets.Remove(m_RuntimeId);
        }
    }
}
#endif
