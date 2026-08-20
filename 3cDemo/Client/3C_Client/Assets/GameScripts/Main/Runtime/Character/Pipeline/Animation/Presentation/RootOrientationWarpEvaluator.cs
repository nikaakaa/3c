using System;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.Presentation
{
    internal sealed class RootOrientationWarpRuntime
    {
        sealed class Page
        {
            internal bool Relevant;
            internal AnimationPoseSourceId SourceId;
            internal ulong BodyDiscontinuityGeneration;
            internal float CapturedTargetAngle;
            internal float CurrentFacingError;
            internal float SourceYaw;
            internal float RootYawOffset;
        }

        readonly CharacterPresentationRootOrientationWarpDescriptor m_Descriptor;
        readonly AnimationClipPlayerRuntime m_Sequence;
        Page m_Committed = new Page();
        Page m_Pending = new Page();
        Page m_Active;
        bool m_FrameOpen;

        internal RootOrientationWarpRuntime(
            CharacterPresentationRootOrientationWarpDescriptor descriptor,
            AnimationClipPlayerRuntime sequence)
        {
            m_Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            m_Sequence = sequence ?? throw new ArgumentNullException(nameof(sequence));
            if (descriptor.ClipPlayerIndex < 0 ||
                Math.Abs(descriptor.Duration - sequence.Duration) > 0.0001f)
            {
                throw new InvalidOperationException(
                    $"Root Orientation Warp '{descriptor.NodeId}' does not match its Clip Player.");
            }
            m_Active = m_Committed;
        }

        internal PoseNodeId NodeId => m_Descriptor.NodeId;
        internal bool IsRelevant => m_Active.Relevant;
        internal float CapturedTargetAngle => m_Active.CapturedTargetAngle;
        internal float CurrentFacingError => m_Active.CurrentFacingError;
        internal float SourceYaw => m_Active.SourceYaw;
        internal float RootYawOffset => m_Active.RootYawOffset;
        internal bool HasOpenFrame => m_FrameOpen;

        internal void BeginFrame()
        {
            if (m_FrameOpen)
                throw new InvalidOperationException("Root Orientation Warp frame is already open.");
            Copy(m_Committed, m_Pending);
            m_Active = m_Pending;
            m_FrameOpen = true;
        }

        internal void CommitFrame()
        {
            RequireOpenFrame();
            Page previous = m_Committed;
            m_Committed = m_Pending;
            m_Pending = previous;
            m_Active = m_Committed;
            m_FrameOpen = false;
        }

        internal void DiscardFrame()
        {
            RequireOpenFrame();
            m_Active = m_Committed;
            m_FrameOpen = false;
        }

        internal CharacterRootOrientationWarpNativeControl Prepare(
            in CharacterPresentationFactFrame factFrame)
        {
            if (!factFrame.IsValid)
                throw new ArgumentException("Root Orientation Warp fact frame is invalid.", nameof(factFrame));
            bool relevant = m_Sequence.IsRelevant;
            AnimationPoseSourceId sourceId = relevant
                ? m_Sequence.SourceId
                : default;
            if (!relevant)
            {
                Reset();
                return new CharacterRootOrientationWarpNativeControl(false, 0f);
            }
            if (!m_Active.Relevant ||
                !sourceId.Equals(m_Active.SourceId) ||
                factFrame.BodyDiscontinuityGeneration !=
                m_Active.BodyDiscontinuityGeneration)
            {
                m_Active.CapturedTargetAngle = Mathf.DeltaAngle(
                    0f,
                    factFrame.FacingError);
                m_Active.SourceId = sourceId;
                m_Active.BodyDiscontinuityGeneration =
                    factFrame.BodyDiscontinuityGeneration;
            }
            m_Active.Relevant = true;
            m_Active.CurrentFacingError = Mathf.DeltaAngle(0f, factFrame.FacingError);
            m_Active.SourceYaw = m_Descriptor.YawCurve.Evaluate(
                Mathf.Clamp(m_Sequence.SampleTime, 0f,
                    m_Descriptor.Duration));
            float authorProgress = m_Active.SourceYaw /
                                   m_Descriptor.TotalYaw;
            m_Active.RootYawOffset = Mathf.DeltaAngle(
                0f,
                factFrame.FacingError -
                m_Active.CapturedTargetAngle +
                m_Active.CapturedTargetAngle * authorProgress);
            return new CharacterRootOrientationWarpNativeControl(
                true,
                m_Active.RootYawOffset);
        }

        internal RootOrientationWarpRuntimeSnapshot CreateDiagnosticsSnapshot() =>
            new RootOrientationWarpRuntimeSnapshot(
                NodeId,
                m_Committed.Relevant,
                m_Committed.CurrentFacingError,
                m_Committed.CapturedTargetAngle,
                m_Committed.SourceYaw,
                m_Committed.RootYawOffset);

        internal void Reset()
        {
            if (m_FrameOpen)
                throw new InvalidOperationException("Root Orientation Warp frame is open.");
            Clear(m_Committed);
            Clear(m_Pending);
            m_Active = m_Committed;
        }

        static void Copy(Page source, Page destination)
        {
            destination.Relevant = source.Relevant;
            destination.SourceId = source.SourceId;
            destination.BodyDiscontinuityGeneration = source.BodyDiscontinuityGeneration;
            destination.CapturedTargetAngle = source.CapturedTargetAngle;
            destination.CurrentFacingError = source.CurrentFacingError;
            destination.SourceYaw = source.SourceYaw;
            destination.RootYawOffset = source.RootYawOffset;
        }

        static void Clear(Page page)
        {
            page.Relevant = false;
            page.SourceId = default;
            page.BodyDiscontinuityGeneration = 0;
            page.CapturedTargetAngle = 0f;
            page.CurrentFacingError = 0f;
            page.SourceYaw = 0f;
            page.RootYawOffset = 0f;
        }

        void RequireOpenFrame()
        {
            if (!m_FrameOpen)
                throw new InvalidOperationException("Root Orientation Warp frame is not open.");
        }
    }
}
