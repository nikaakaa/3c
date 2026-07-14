using System;
using ThirdPersonCharacter.Pipeline.Motion;
using ThirdPersonGameplay.Tick;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal sealed class CharacterPresentationInterpolator
    {
        readonly ICharacterLogicPosePort m_LogicPosePort;
        readonly Transform m_VisualRoot;
        readonly Vector3 m_VisualRootBindLocalPosition;
        readonly Quaternion m_VisualRootBindLocalRotation;
        readonly PresentationLogicSample m_PreviousSample = new PresentationLogicSample();
        readonly PresentationLogicSample m_CurrentSample = new PresentationLogicSample();

        public CharacterPresentationInterpolator(ICharacterLogicPosePort logicPosePort, Transform visualRoot)
        {
            m_LogicPosePort = logicPosePort ?? throw new ArgumentNullException(nameof(logicPosePort));
            m_VisualRoot = visualRoot ? visualRoot : throw new ArgumentNullException(nameof(visualRoot));
            CharacterLogicBodyState logicState = ReadLogicState();
            Vector3 logicPosition = logicState.Position.ToUnityVector();
            Quaternion logicRotation = logicState.Rotation.ToUnityRotation();
            Quaternion inverseLogicRotation = Quaternion.Inverse(logicRotation);
            m_VisualRootBindLocalPosition = inverseLogicRotation * (visualRoot.position - logicPosition);
            m_VisualRootBindLocalRotation = inverseLogicRotation * visualRoot.rotation;
        }

        public bool HasLogicSample => m_CurrentSample.Valid;
        public ulong PreviousLogicTick => m_PreviousSample.LocalLogicTick;
        public ulong CurrentLogicTick => m_CurrentSample.LocalLogicTick;

        public void Reset()
        {
            m_PreviousSample.Reset();
            m_CurrentSample.Reset();
            CharacterLogicBodyState logicState = ReadLogicState();
            Vector3 logicPosition = logicState.Position.ToUnityVector();
            Quaternion logicRotation = logicState.Rotation.ToUnityRotation();
            m_VisualRoot.SetPositionAndRotation(
                logicPosition + logicRotation * m_VisualRootBindLocalPosition,
                logicRotation * m_VisualRootBindLocalRotation);
        }

        public void CaptureLogicSample(GameplayLogicTickContext context, CharacterPipelineFrame frame)
        {
            if (frame == null)
                return;

            m_PreviousSample.CopyFrom(m_CurrentSample);
            MotionResult motionResult = frame.Output.StrictGameplay.MotionResult;
            m_CurrentSample.Set(
                context,
                motionResult.Position,
                motionResult.Rotation,
                motionResult.Grounded,
                frame.Output.StrictGameplay.MotionCorrectionApplicationResult.Extent);

            if (!m_PreviousSample.Valid)
                m_PreviousSample.CopyFrom(m_CurrentSample);
        }

        public bool TryResolve(
            GameplayPresentationFrameContext context,
            out CharacterPresentationRootPose rootPose,
            out CharacterVisualPose visualPose,
            out float alpha)
        {
            if (!m_CurrentSample.Valid)
            {
                rootPose = default;
                visualPose = default;
                alpha = 0f;
                return false;
            }

            alpha = ResolveAlpha(context);
            rootPose = ResolvePresentationRootPose(alpha);
            visualPose = ResolveVisualPose(rootPose);
            return true;
        }

        public void ApplyVisualPose(CharacterVisualPose visualPose)
        {
            if (!visualPose.Valid)
                return;

            m_VisualRoot.SetPositionAndRotation(visualPose.Position, visualPose.Rotation);
        }

        float ResolveAlpha(GameplayPresentationFrameContext context)
        {
            if (!m_PreviousSample.Valid || m_PreviousSample.LocalLogicTick == m_CurrentSample.LocalLogicTick)
                return 1f;

            if (m_CurrentSample.CorrectionExtent == MotionCorrectionApplicationExtent.Full)
                return 1f;

            return Mathf.Clamp01(context.InterpolationAlpha);
        }

        CharacterPresentationRootPose ResolvePresentationRootPose(float alpha)
        {
            Vector3 logicPosition = m_PreviousSample.Valid
                ? Vector3.Lerp(m_PreviousSample.LogicPosition, m_CurrentSample.LogicPosition, alpha)
                : m_CurrentSample.LogicPosition;
            Quaternion logicRotation = m_PreviousSample.Valid
                ? Quaternion.Slerp(m_PreviousSample.LogicRotation, m_CurrentSample.LogicRotation, alpha)
                : m_CurrentSample.LogicRotation;

            return new CharacterPresentationRootPose(
                logicPosition,
                logicRotation,
                m_CurrentSample.Grounded,
                true);
        }

        CharacterVisualPose ResolveVisualPose(CharacterPresentationRootPose rootPose)
        {
            return new CharacterVisualPose(
                rootPose.TransformPoint(m_VisualRootBindLocalPosition),
                rootPose.Rotation * m_VisualRootBindLocalRotation,
                rootPose.Grounded,
                rootPose.Valid);
        }

        CharacterLogicBodyState ReadLogicState()
        {
            if (!m_LogicPosePort.TryReadState(out CharacterLogicBodyState state, out string error))
                throw new InvalidOperationException($"Logic pose port failed to initialize presentation: {error}");
            if (!state.IsValid)
                throw new InvalidOperationException("Logic pose port returned an invalid presentation state.");
            return state;
        }

        sealed class PresentationLogicSample
        {
            public bool Valid { get; private set; }
            public ulong LocalLogicTick { get; private set; }
            public Vector3 LogicPosition { get; private set; }
            public Quaternion LogicRotation { get; private set; }
            public bool Grounded { get; private set; }
            public MotionCorrectionApplicationExtent CorrectionExtent { get; private set; }

            public void Set(
                GameplayLogicTickContext context,
                Vector3 logicPosition,
                Quaternion logicRotation,
                bool grounded,
                MotionCorrectionApplicationExtent correctionExtent)
            {
                Valid = true;
                LocalLogicTick = context.LocalLogicTick;
                LogicPosition = logicPosition;
                LogicRotation = logicRotation;
                Grounded = grounded;
                CorrectionExtent = correctionExtent;
            }

            public void CopyFrom(PresentationLogicSample other)
            {
                Valid = other.Valid;
                LocalLogicTick = other.LocalLogicTick;
                LogicPosition = other.LogicPosition;
                LogicRotation = other.LogicRotation;
                Grounded = other.Grounded;
                CorrectionExtent = other.CorrectionExtent;
            }

            public void Reset()
            {
                Valid = false;
                LocalLogicTick = 0;
                LogicPosition = Vector3.zero;
                LogicRotation = Quaternion.identity;
                Grounded = false;
                CorrectionExtent = MotionCorrectionApplicationExtent.None;
            }
        }
    }
}
