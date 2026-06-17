using System;

namespace ThirdPersonAction
{
    public enum CharacterBodyDomain
    {
        None = 0,
        Locomotion = 1,
        FullBodyAction = 2,
        UpperBody = 3
    }

    public enum BodyOccupancyKind
    {
        None = 0,
        FullBody = 1,
        UpperBody = 2
    }

    [Flags]
    public enum CharacterFrameOutputChannel
    {
        None = 0,
        Motion = 1,
        Animation = 2
    }

    public readonly struct BodyOccupancyClaim
    {
        public BodyOccupancyClaim(
            CharacterBodyDomain domain,
            BodyOccupancyKind kind,
            CharacterFrameOutputChannel channels,
            int sourceStep)
        {
            Domain = domain;
            Kind = kind;
            Channels = channels;
            SourceStep = sourceStep < 0 ? 0 : sourceStep;
        }

        public CharacterBodyDomain Domain { get; }
        public BodyOccupancyKind Kind { get; }
        public CharacterFrameOutputChannel Channels { get; }
        public int SourceStep { get; }
        public bool HasClaim => Domain != CharacterBodyDomain.None && Kind != BodyOccupancyKind.None;
        public bool ClaimsFullBody => HasClaim && Kind == BodyOccupancyKind.FullBody;
        public bool ClaimsUpperBody => HasClaim && Kind == BodyOccupancyKind.UpperBody;

        public static BodyOccupancyClaim None(int sourceStep = 0)
        {
            return new BodyOccupancyClaim(
                CharacterBodyDomain.None,
                BodyOccupancyKind.None,
                CharacterFrameOutputChannel.None,
                sourceStep);
        }

        public static BodyOccupancyClaim FullBodyAction(int sourceStep)
        {
            return new BodyOccupancyClaim(
                CharacterBodyDomain.FullBodyAction,
                BodyOccupancyKind.FullBody,
                CharacterFrameOutputChannel.Motion | CharacterFrameOutputChannel.Animation,
                sourceStep);
        }

        public static BodyOccupancyClaim UpperBody(int sourceStep)
        {
            return new BodyOccupancyClaim(
                CharacterBodyDomain.UpperBody,
                BodyOccupancyKind.UpperBody,
                CharacterFrameOutputChannel.Animation,
                sourceStep);
        }
    }

    public readonly struct CharacterFrameCandidateOutput
    {
        public CharacterFrameCandidateOutput(
            CharacterBodyDomain domain,
            bool hasMotionCandidate,
            bool hasAnimationCandidate,
            int sourceStep)
        {
            Domain = domain;
            HasMotionCandidate = hasMotionCandidate;
            HasAnimationCandidate = hasAnimationCandidate;
            SourceStep = sourceStep < 0 ? 0 : sourceStep;
        }

        public CharacterBodyDomain Domain { get; }
        public bool HasMotionCandidate { get; }
        public bool HasAnimationCandidate { get; }
        public int SourceStep { get; }
        public bool HasAnyCandidate => HasMotionCandidate || HasAnimationCandidate;

        public static CharacterFrameCandidateOutput None(CharacterBodyDomain domain, int sourceStep = 0)
        {
            return new CharacterFrameCandidateOutput(domain, false, false, sourceStep);
        }

        public static CharacterFrameCandidateOutput Locomotion(
            bool hasMotionCandidate,
            bool hasAnimationCandidate,
            int sourceStep)
        {
            return new CharacterFrameCandidateOutput(
                CharacterBodyDomain.Locomotion,
                hasMotionCandidate,
                hasAnimationCandidate,
                sourceStep);
        }

        public static CharacterFrameCandidateOutput FullBodyAction(
            bool hasMotionCandidate,
            bool hasAnimationCandidate,
            int sourceStep)
        {
            return new CharacterFrameCandidateOutput(
                CharacterBodyDomain.FullBodyAction,
                hasMotionCandidate,
                hasAnimationCandidate,
                sourceStep);
        }

        public static CharacterFrameCandidateOutput UpperBody(
            bool hasMotionCandidate,
            bool hasAnimationCandidate,
            int sourceStep)
        {
            return new CharacterFrameCandidateOutput(
                CharacterBodyDomain.UpperBody,
                hasMotionCandidate,
                hasAnimationCandidate,
                sourceStep);
        }
    }

    public readonly struct CharacterFrameArbitrationInput
    {
        public CharacterFrameArbitrationInput(
            BodyOccupancyClaim occupancyClaim,
            CharacterFrameCandidateOutput locomotionCandidate,
            CharacterFrameCandidateOutput fullBodyActionCandidate,
            CharacterFrameCandidateOutput upperBodyCandidate,
            int sourceStep)
        {
            OccupancyClaim = occupancyClaim;
            LocomotionCandidate = locomotionCandidate;
            FullBodyActionCandidate = fullBodyActionCandidate;
            UpperBodyCandidate = upperBodyCandidate;
            SourceStep = sourceStep < 0 ? 0 : sourceStep;
        }

        public BodyOccupancyClaim OccupancyClaim { get; }
        public CharacterFrameCandidateOutput LocomotionCandidate { get; }
        public CharacterFrameCandidateOutput FullBodyActionCandidate { get; }
        public CharacterFrameCandidateOutput UpperBodyCandidate { get; }
        public int SourceStep { get; }
        public bool HasInput =>
            OccupancyClaim.HasClaim ||
            LocomotionCandidate.HasAnyCandidate ||
            FullBodyActionCandidate.HasAnyCandidate ||
            UpperBodyCandidate.HasAnyCandidate;

        public static CharacterFrameArbitrationInput FromSubmission(in CharacterFrameSubmission submission)
        {
            return submission.ArbitrationInput;
        }

        public static CharacterFrameArbitrationInput None(int sourceStep = 0)
        {
            return new CharacterFrameArbitrationInput(
                BodyOccupancyClaim.None(sourceStep),
                CharacterFrameCandidateOutput.None(CharacterBodyDomain.Locomotion, sourceStep),
                CharacterFrameCandidateOutput.None(CharacterBodyDomain.FullBodyAction, sourceStep),
                CharacterFrameCandidateOutput.None(CharacterBodyDomain.UpperBody, sourceStep),
                sourceStep);
        }
    }

    public readonly struct BodyOccupancyDecision
    {
        public BodyOccupancyDecision(
            CharacterBodyDomain baseLayerOwner,
            CharacterBodyDomain upperBodyOwner,
            bool fullBodyClaimAccepted,
            bool suppressLocomotionMotion,
            bool suppressLocomotionAnimation,
            bool allowUpperBody,
            int sourceStep)
        {
            BaseLayerOwner = baseLayerOwner;
            UpperBodyOwner = upperBodyOwner;
            FullBodyClaimAccepted = fullBodyClaimAccepted;
            SuppressLocomotionMotion = suppressLocomotionMotion;
            SuppressLocomotionAnimation = suppressLocomotionAnimation;
            AllowUpperBody = allowUpperBody;
            SourceStep = sourceStep < 0 ? 0 : sourceStep;
        }

        public CharacterBodyDomain BaseLayerOwner { get; }
        public CharacterBodyDomain UpperBodyOwner { get; }
        public bool FullBodyClaimAccepted { get; }
        public bool SuppressLocomotionMotion { get; }
        public bool SuppressLocomotionAnimation { get; }
        public bool AllowUpperBody { get; }
        public int SourceStep { get; }
        public bool HasDecision =>
            BaseLayerOwner != CharacterBodyDomain.None ||
            UpperBodyOwner != CharacterBodyDomain.None ||
            FullBodyClaimAccepted;

        public static BodyOccupancyDecision None(int sourceStep = 0)
        {
            return new BodyOccupancyDecision(
                CharacterBodyDomain.None,
                CharacterBodyDomain.None,
                false,
                false,
                false,
                false,
                sourceStep);
        }
    }
}
