using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Motion
{
    public sealed class MotionResolveDebugFrame
    {
        readonly List<MotionContributionDebugRecord> m_Contributions = new List<MotionContributionDebugRecord>();
        readonly List<MotionChannelDebugRecord> m_ChannelResults = new List<MotionChannelDebugRecord>();
        readonly List<MotionWarpDebugRecord> m_MotionWarpWindows = new List<MotionWarpDebugRecord>();

        public IReadOnlyList<MotionContributionDebugRecord> Contributions => m_Contributions;
        public IReadOnlyList<MotionChannelDebugRecord> ChannelResults => m_ChannelResults;
        public IReadOnlyList<MotionWarpDebugRecord> MotionWarpWindows => m_MotionWarpWindows;
        public MotionIntent RawIntent { get; private set; }
        public MotionIntent ModifiedIntent { get; private set; }
        public Vector3 ModifierDeltaDisplacement { get; private set; }
        public float ModifierDeltaYawDegrees { get; private set; }
        public MotionCorrectionDebugRecord Correction { get; private set; }
        public CharacterMotionExecutionDebugRecord Execution { get; private set; }

        public void Clear()
        {
            m_Contributions.Clear();
            m_ChannelResults.Clear();
            m_MotionWarpWindows.Clear();
            RawIntent = default;
            ModifiedIntent = default;
            ModifierDeltaDisplacement = Vector3.zero;
            ModifierDeltaYawDegrees = 0f;
            Correction = default;
            Execution = default;
        }

        public void AddContribution(MotionContribution contribution, Vector3 resolvedDisplacement, float resolvedYaw)
        {
            m_Contributions.Add(new MotionContributionDebugRecord(contribution, resolvedDisplacement, resolvedYaw));
        }

        public void AddChannelResult(MotionChannel channel, MotionIntent intent, bool consumedLowerChannels, MotionContribution winner)
        {
            m_ChannelResults.Add(new MotionChannelDebugRecord(channel, intent, consumedLowerChannels, winner));
        }

        public void SetMotionWarpWindows(IReadOnlyList<MotionWarpWindow> windows)
        {
            m_MotionWarpWindows.Clear();
            if (windows == null)
                return;

            for (int i = 0; i < windows.Count; i++)
                m_MotionWarpWindows.Add(new MotionWarpDebugRecord(windows[i]));
        }

        public void SetRawIntent(MotionIntent intent)
        {
            RawIntent = intent;
        }

        public void SetModifiedIntent(MotionIntent before, MotionIntent after)
        {
            ModifiedIntent = after;
            ModifierDeltaDisplacement = after.Displacement - before.Displacement;
            ModifierDeltaYawDegrees = after.YawDegrees - before.YawDegrees;
        }

        public void SetCorrection(MotionCorrectionApplicationResult result)
        {
            Correction = new MotionCorrectionDebugRecord(result);
        }

        public void SetExecution(
            string implementationId,
            CharacterMotionExecutionInput input,
            CharacterMotionExecutionResult result)
        {
            Execution = new CharacterMotionExecutionDebugRecord(implementationId, input, result);
        }
    }

    public readonly struct CharacterMotionExecutionDebugRecord
    {
        public CharacterMotionExecutionDebugRecord(
            string implementationId,
            CharacterMotionExecutionInput input,
            CharacterMotionExecutionResult result)
        {
            ImplementationId = implementationId ?? string.Empty;
            LocalLogicTick = input.LocalLogicTick;
            RequestedDisplacement = input.RequestedDisplacement.ToUnityVector();
            AppliedDisplacement = result.AppliedDisplacement.ToUnityVector();
            RequestedYawDegrees = input.RequestedYawDegrees;
            AppliedYawDegrees = result.AppliedYawDegrees;
            Position = result.FinalState.Position.ToUnityVector();
            Rotation = result.FinalState.Rotation.ToUnityRotation();
            Grounded = result.FinalState.Grounded;
            CollisionSummary = result.CollisionSummary;
            Valid = result.IsValid;
        }

        public string ImplementationId { get; }
        public ulong LocalLogicTick { get; }
        public Vector3 RequestedDisplacement { get; }
        public Vector3 AppliedDisplacement { get; }
        public float RequestedYawDegrees { get; }
        public float AppliedYawDegrees { get; }
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public bool Grounded { get; }
        public CharacterMotionCollisionSummary CollisionSummary { get; }
        public bool Valid { get; }
    }

    public readonly struct MotionContributionDebugRecord
    {
        public MotionContributionDebugRecord(MotionContribution contribution, Vector3 resolvedDisplacement, float resolvedYaw)
        {
            SourceId = contribution.SourceId;
            SourceName = contribution.SourceName;
            DebugSourceIdentity = contribution.DebugSourceIdentity;
            Channel = contribution.Channel;
            BlendMode = contribution.BlendMode;
            SourceType = contribution.SourceType;
            Priority = contribution.Priority;
            Weight = contribution.Weight;
            ConsumeLowerChannels = contribution.ConsumeLowerChannels;
            ResolvedDisplacement = resolvedDisplacement;
            ResolvedYawDegrees = resolvedYaw;
        }

        public string SourceId { get; }
        public string SourceName { get; }
        public string DebugSourceIdentity { get; }
        public MotionChannel Channel { get; }
        public MotionBlendMode BlendMode { get; }
        public MotionContributionSourceType SourceType { get; }
        public int Priority { get; }
        public float Weight { get; }
        public bool ConsumeLowerChannels { get; }
        public Vector3 ResolvedDisplacement { get; }
        public float ResolvedYawDegrees { get; }
    }

    public readonly struct MotionChannelDebugRecord
    {
        public MotionChannelDebugRecord(MotionChannel channel, MotionIntent intent, bool consumedLowerChannels, MotionContribution winner)
        {
            Channel = channel;
            Displacement = intent.Displacement;
            YawDegrees = intent.YawDegrees;
            HasMotion = intent.HasMotion;
            ConsumedLowerChannels = consumedLowerChannels;
            WinnerSourceId = winner.SourceId;
            WinnerSourceName = winner.SourceName;
            WinnerSourceType = winner.SourceType;
        }

        public MotionChannel Channel { get; }
        public Vector3 Displacement { get; }
        public float YawDegrees { get; }
        public bool HasMotion { get; }
        public bool ConsumedLowerChannels { get; }
        public string WinnerSourceId { get; }
        public string WinnerSourceName { get; }
        public MotionContributionSourceType WinnerSourceType { get; }
    }

    public readonly struct MotionCorrectionDebugRecord
    {
        public MotionCorrectionDebugRecord(MotionCorrectionApplicationResult result)
        {
            Extent = result.Extent;
            InputSequence = result.InputSequence;
            SourceTick = result.SourceTick;
            BeforePosition = result.BeforePosition;
            TargetPosition = result.TargetPosition;
            AppliedDelta = result.AppliedDelta;
            AppliedYawDegrees = result.AppliedYawDegrees;
            Applied = result.Applied;
        }

        public MotionCorrectionApplicationExtent Extent { get; }
        public ulong InputSequence { get; }
        public ulong SourceTick { get; }
        public Vector3 BeforePosition { get; }
        public Vector3 TargetPosition { get; }
        public Vector3 AppliedDelta { get; }
        public float AppliedYawDegrees { get; }
        public bool Applied { get; }
    }

    public readonly struct MotionWarpDebugRecord
    {
        public MotionWarpDebugRecord(MotionWarpWindow window)
        {
            SourceId = window.SourceId;
            SourceName = window.SourceName;
            TargetKey = window.TargetKey;
            DebugSourceIdentity = window.DebugSourceIdentity;
            NormalizedTime = window.NormalizedTime;
            Weight = window.Weight;
            PositionWeight = window.PositionWeight;
            YawWeight = window.YawWeight;
        }

        public string SourceId { get; }
        public string SourceName { get; }
        public string TargetKey { get; }
        public string DebugSourceIdentity { get; }
        public float NormalizedTime { get; }
        public float Weight { get; }
        public float PositionWeight { get; }
        public float YawWeight { get; }
    }
}
