using System;
using System.Collections.Generic;
using ThirdPersonAction;

namespace ThirdPersonCharacterGraph
{
    public enum CharacterBranchClaimChannel
    {
        None = 0,
        FullBody = 1,
        UpperBody = 2,
        LowerBody = 3
    }

    public readonly struct CharacterBranchClaimDescriptor
    {
        public CharacterBranchClaimDescriptor(
            CharacterGraphBranchKind branchKind,
            CharacterBranchClaimChannel channel,
            CharacterFrameOutputChannel outputChannels,
            int sourceStep)
        {
            BranchKind = branchKind;
            Channel = channel;
            OutputChannels = outputChannels;
            SourceStep = sourceStep < 0 ? 0 : sourceStep;
        }

        public CharacterGraphBranchKind BranchKind { get; }
        public CharacterBranchClaimChannel Channel { get; }
        public CharacterFrameOutputChannel OutputChannels { get; }
        public int SourceStep { get; }
        public bool HasClaim => BranchKind != CharacterGraphBranchKind.None && Channel != CharacterBranchClaimChannel.None;

        public static CharacterBranchClaimDescriptor None(int sourceStep = 0)
        {
            return new CharacterBranchClaimDescriptor(
                CharacterGraphBranchKind.None,
                CharacterBranchClaimChannel.None,
                CharacterFrameOutputChannel.None,
                sourceStep);
        }

        public static CharacterBranchClaimDescriptor FullBodyAction(int sourceStep)
        {
            return new CharacterBranchClaimDescriptor(
                CharacterGraphBranchKind.Action,
                CharacterBranchClaimChannel.FullBody,
                CharacterFrameOutputChannel.Motion | CharacterFrameOutputChannel.Animation,
                sourceStep);
        }

        public static CharacterBranchClaimDescriptor UpperBody(int sourceStep)
        {
            return new CharacterBranchClaimDescriptor(
                CharacterGraphBranchKind.UpperBody,
                CharacterBranchClaimChannel.UpperBody,
                CharacterFrameOutputChannel.Animation,
                sourceStep);
        }

        public static CharacterBranchClaimDescriptor LowerBodyLocomotion(int sourceStep)
        {
            return new CharacterBranchClaimDescriptor(
                CharacterGraphBranchKind.Locomotion,
                CharacterBranchClaimChannel.LowerBody,
                CharacterFrameOutputChannel.Motion | CharacterFrameOutputChannel.Animation,
                sourceStep);
        }
    }

    public readonly struct CharacterBranchDiagnostics
    {
        readonly string[] messages;

        public CharacterBranchDiagnostics(string[] messages, int sourceStep)
        {
            this.messages = messages ?? Array.Empty<string>();
            SourceStep = sourceStep < 0 ? 0 : sourceStep;
        }

        public IReadOnlyList<string> Messages => messages ?? Array.Empty<string>();
        public int SourceStep { get; }
        public bool HasMessages => Messages.Count > 0;

        public static CharacterBranchDiagnostics None(int sourceStep = 0)
        {
            return new CharacterBranchDiagnostics(Array.Empty<string>(), sourceStep);
        }

        public static CharacterBranchDiagnostics Unimplemented(CharacterGraphBranchKind kind, int sourceStep)
        {
            return new CharacterBranchDiagnostics(new[] { $"branch-unimplemented:{kind}" }, sourceStep);
        }
    }

    public readonly struct LocomotionBranchInput
    {
        public LocomotionBranchInput(CharacterGraphInput graphInput)
        {
            GraphInput = graphInput;
        }

        public CharacterGraphInput GraphInput { get; }
        public int SourceStep => GraphInput.SourceStep;
    }

    public readonly struct LocomotionBranchOutcome
    {
        public LocomotionBranchOutcome(
            CharacterFrameCandidateOutput candidate,
            CharacterBranchDiagnostics diagnostics,
            int sourceStep)
        {
            Candidate = candidate;
            Diagnostics = diagnostics;
            SourceStep = sourceStep < 0 ? 0 : sourceStep;
        }

        public CharacterFrameCandidateOutput Candidate { get; }
        public CharacterBranchDiagnostics Diagnostics { get; }
        public int SourceStep { get; }
        public bool HasOutput => Candidate.HasAnyCandidate;

        public static LocomotionBranchOutcome Empty(int sourceStep = 0)
        {
            return new LocomotionBranchOutcome(
                CharacterFrameCandidateOutput.None(CharacterBodyDomain.Locomotion, sourceStep),
                CharacterBranchDiagnostics.Unimplemented(CharacterGraphBranchKind.Locomotion, sourceStep),
                sourceStep);
        }
    }

    public readonly struct UpperBodyBranchInput
    {
        public UpperBodyBranchInput(CharacterGraphInput graphInput)
        {
            GraphInput = graphInput;
        }

        public CharacterGraphInput GraphInput { get; }
        public int SourceStep => GraphInput.SourceStep;
    }

    public readonly struct UpperBodyBranchOutcome
    {
        public UpperBodyBranchOutcome(
            CharacterFrameCandidateOutput candidate,
            BodyOccupancyClaim occupancyClaim,
            CharacterBranchDiagnostics diagnostics,
            int sourceStep)
        {
            Candidate = candidate;
            OccupancyClaim = occupancyClaim;
            Diagnostics = diagnostics;
            SourceStep = sourceStep < 0 ? 0 : sourceStep;
        }

        public CharacterFrameCandidateOutput Candidate { get; }
        public BodyOccupancyClaim OccupancyClaim { get; }
        public CharacterBranchDiagnostics Diagnostics { get; }
        public int SourceStep { get; }
        public bool HasOutput => Candidate.HasAnyCandidate || OccupancyClaim.HasClaim;

        public static UpperBodyBranchOutcome Empty(int sourceStep = 0)
        {
            return new UpperBodyBranchOutcome(
                CharacterFrameCandidateOutput.None(CharacterBodyDomain.UpperBody, sourceStep),
                BodyOccupancyClaim.None(sourceStep),
                CharacterBranchDiagnostics.Unimplemented(CharacterGraphBranchKind.UpperBody, sourceStep),
                sourceStep);
        }
    }

    public readonly struct CueBranchInput
    {
        public CueBranchInput(CharacterGraphInput graphInput)
        {
            GraphInput = graphInput;
        }

        public CharacterGraphInput GraphInput { get; }
        public int SourceStep => GraphInput.SourceStep;
    }

    public readonly struct CueBranchOutcome
    {
        public CueBranchOutcome(
            CueOutcome cue,
            CharacterBranchDiagnostics diagnostics,
            int sourceStep)
        {
            Cue = cue;
            Diagnostics = diagnostics;
            SourceStep = sourceStep < 0 ? 0 : sourceStep;
        }

        public CueOutcome Cue { get; }
        public CharacterBranchDiagnostics Diagnostics { get; }
        public int SourceStep { get; }
        public bool HasOutput => Cue.HasCue;

        public static CueBranchOutcome Empty(int sourceStep = 0)
        {
            return new CueBranchOutcome(
                CueOutcome.None(sourceStep),
                CharacterBranchDiagnostics.Unimplemented(CharacterGraphBranchKind.Cue, sourceStep),
                sourceStep);
        }
    }
}
