using System;
using ThirdPersonMovement;
using UnityEngine;

namespace ThirdPersonAnimation
{
    [CreateAssetMenu(fileName = "RunLocomotionAnimationConfig", menuName = "3C/Animation/RunLocomotionAnimationConfig")]
    public sealed class RunLocomotionAnimationConfigSO : ScriptableObject
    {
        [SerializeField] LocomotionAnimationPhaseConfig idle = LocomotionAnimationPhaseConfig.Manual("Idle");
        [SerializeField] LocomotionAnimationPhaseConfig moveStart = LocomotionAnimationPhaseConfig.AfterDuration("RunStart", 0.08f);
        [SerializeField] LocomotionAnimationPhaseConfig moveLoop = LocomotionAnimationPhaseConfig.Manual("RunLoop");
        [SerializeField] LocomotionAnimationPhaseConfig moveStop = LocomotionAnimationPhaseConfig.OnAnimationEnd("RunEnd");
        [SerializeField] LocomotionAnimationPhaseConfig walkStart = LocomotionAnimationPhaseConfig.AfterDuration("WalkStart", 0.08f);
        [SerializeField] LocomotionAnimationPhaseConfig walkLoop = LocomotionAnimationPhaseConfig.Manual("WalkLoop");
        [SerializeField] LocomotionAnimationPhaseConfig walkEnd = LocomotionAnimationPhaseConfig.AfterDuration("WalkEnd", 0.05f);
        [SerializeField] LocomotionAnimationPhaseConfig runStart = LocomotionAnimationPhaseConfig.AfterDuration("RunStart", 0.08f);
        [SerializeField] LocomotionAnimationPhaseConfig runLoop = LocomotionAnimationPhaseConfig.Manual("RunLoop");
        [SerializeField] LocomotionAnimationPhaseConfig runEnd = LocomotionAnimationPhaseConfig.OnAnimationEnd("RunEnd");
        [SerializeField] LocomotionPhaseMotionProfileBinding[] motionProfiles = Array.Empty<LocomotionPhaseMotionProfileBinding>();

        public LocomotionAnimationPhaseConfig Idle => idle;
        public LocomotionAnimationPhaseConfig MoveStart => moveStart;
        public LocomotionAnimationPhaseConfig MoveLoop => moveLoop;
        public LocomotionAnimationPhaseConfig MoveStop => moveStop;
        public LocomotionAnimationPhaseConfig WalkStart => ResolveGaitPhaseConfig(BasicMovementPhase.MoveStart, BasicMovementGait.Walk);
        public LocomotionAnimationPhaseConfig WalkLoop => ResolveGaitPhaseConfig(BasicMovementPhase.MoveLoop, BasicMovementGait.Walk);
        public LocomotionAnimationPhaseConfig WalkEnd => ResolveGaitPhaseConfig(BasicMovementPhase.MoveStop, BasicMovementGait.Walk);
        public LocomotionAnimationPhaseConfig RunStart => ResolveGaitPhaseConfig(BasicMovementPhase.MoveStart, BasicMovementGait.Run);
        public LocomotionAnimationPhaseConfig RunLoop => ResolveGaitPhaseConfig(BasicMovementPhase.MoveLoop, BasicMovementGait.Run);
        public LocomotionAnimationPhaseConfig RunEnd => ResolveGaitPhaseConfig(BasicMovementPhase.MoveStop, BasicMovementGait.Run);
        public LocomotionPhaseMotionProfileBinding[] MotionProfiles => motionProfiles ?? Array.Empty<LocomotionPhaseMotionProfileBinding>();

        public LocomotionAnimationPhaseConfig ResolvePhaseConfig(BasicMovementPhase phase)
        {
            return ResolvePhaseConfig(phase, BasicMovementGait.Run);
        }

        public LocomotionAnimationPhaseConfig ResolvePhaseConfig(BasicMovementPhase phase, BasicMovementGait gait)
        {
            return ResolveGaitPhaseConfig(phase, gait);
        }

        public string ResolveAliasKey(BasicMovementPhase phase)
        {
            return ResolveAliasKey(phase, BasicMovementGait.Run);
        }

        public string ResolveAliasKey(BasicMovementPhase phase, BasicMovementGait gait)
        {
            return ResolvePhaseConfig(phase, gait).AliasKey;
        }

        public LocomotionMotionProfileSO ResolveMotionProfile(BasicMovementPhase phase)
        {
            return ResolveMotionProfile(phase, BasicMovementGait.Run, ResolveAliasKey(phase));
        }

        public LocomotionMotionProfileSO ResolveMotionProfile(BasicMovementPhase phase, string aliasKey)
        {
            return ResolveMotionProfile(phase, BasicMovementGait.Run, aliasKey);
        }

        public LocomotionMotionProfileSO ResolveMotionProfile(BasicMovementPhase phase, BasicMovementGait gait, string aliasKey)
        {
            LocomotionPhaseMotionProfileBinding[] profiles = motionProfiles;
            if (profiles == null || profiles.Length == 0)
                return null;

            for (int i = 0; i < profiles.Length; i++)
            {
                LocomotionPhaseMotionProfileBinding binding = profiles[i];
                LocomotionMotionProfileSO profile = binding.Profile;
                if (profile != null &&
                    binding.IsEnabled &&
                    binding.Matches(phase, gait, aliasKey) &&
                    profile.Phase == phase &&
                    profile.Gait == binding.Gait &&
                    profile.AliasKey == binding.AliasKey)
                {
                    return profile;
                }
            }

            return null;
        }

        public void SetMotionProfileBindings(params LocomotionPhaseMotionProfileBinding[] bindings)
        {
            motionProfiles = bindings ?? Array.Empty<LocomotionPhaseMotionProfileBinding>();
        }

        public BasicMovementPhaseTiming ResolvePhaseTiming(BasicMovementPhase phase, BasicMovementPhaseTiming fallback)
        {
            return ResolvePhaseTiming(phase, BasicMovementGait.Run, fallback);
        }

        public BasicMovementPhaseTiming ResolvePhaseTiming(BasicMovementPhase phase, BasicMovementGait gait, BasicMovementPhaseTiming fallback)
        {
            return ResolveMovementTiming(ResolvePhaseConfig(phase, gait), fallback);
        }

        public BasicMovementSettings ApplyPhaseTiming(in BasicMovementSettings settings)
        {
            return ApplyPhaseTiming(BasicMovementGait.Run, in settings);
        }

        public BasicMovementSettings ApplyPhaseTiming(BasicMovementGait gait, in BasicMovementSettings settings)
        {
            BasicMovementSettings result = settings;
            result = result.WithPhaseTiming(BasicMovementPhase.Idle, ResolvePhaseTiming(BasicMovementPhase.Idle, gait, result.ResolvePhaseTiming(BasicMovementPhase.Idle)));
            result = result.WithPhaseTiming(BasicMovementPhase.MoveStart, ResolvePhaseTiming(BasicMovementPhase.MoveStart, gait, result.ResolvePhaseTiming(BasicMovementPhase.MoveStart)));
            result = result.WithPhaseTiming(BasicMovementPhase.MoveLoop, ResolvePhaseTiming(BasicMovementPhase.MoveLoop, gait, result.ResolvePhaseTiming(BasicMovementPhase.MoveLoop)));
            result = result.WithPhaseTiming(BasicMovementPhase.MoveStop, ResolvePhaseTiming(BasicMovementPhase.MoveStop, gait, result.ResolvePhaseTiming(BasicMovementPhase.MoveStop)));
            return result;
        }

        public RunLocomotionAnimationConfigValidationResult Validate(bool requireTimedPhaseExits = false)
        {
            RunLocomotionAnimationConfigValidationResult result = new RunLocomotionAnimationConfigValidationResult();
            ValidatePhase(idle, "Idle", false, result);
            ValidatePhase(walkStart, "WalkStart (MoveStart + Walk)", requireTimedPhaseExits, result);
            ValidatePhase(walkLoop, "WalkLoop (MoveLoop + Walk)", false, result);
            ValidatePhase(walkEnd, "WalkEnd (MoveStop + Walk)", requireTimedPhaseExits, result);
            ValidatePhase(runStart, "RunStart (MoveStart + Run)", requireTimedPhaseExits, result);
            ValidatePhase(runLoop, "RunLoop (MoveLoop + Run)", false, result);
            ValidatePhase(runEnd, "RunEnd (MoveStop + Run)", requireTimedPhaseExits, result);
            ValidateMotionProfiles(result);
            return result;
        }

        public void ResetToDefaultConfig()
        {
            idle = LocomotionAnimationPhaseConfig.Manual("Idle");
            moveStart = LocomotionAnimationPhaseConfig.AfterDuration("RunStart", 0.08f);
            moveLoop = LocomotionAnimationPhaseConfig.Manual("RunLoop");
            moveStop = LocomotionAnimationPhaseConfig.OnAnimationEnd("RunEnd");
            walkStart = LocomotionAnimationPhaseConfig.AfterDuration("WalkStart", 0.08f);
            walkLoop = LocomotionAnimationPhaseConfig.Manual("WalkLoop");
            walkEnd = LocomotionAnimationPhaseConfig.AfterDuration("WalkEnd", 0.05f);
            runStart = LocomotionAnimationPhaseConfig.AfterDuration("RunStart", 0.08f);
            runLoop = LocomotionAnimationPhaseConfig.Manual("RunLoop");
            runEnd = LocomotionAnimationPhaseConfig.OnAnimationEnd("RunEnd");
            motionProfiles = Array.Empty<LocomotionPhaseMotionProfileBinding>();
        }

        LocomotionAnimationPhaseConfig ResolveGaitPhaseConfig(BasicMovementPhase phase, BasicMovementGait gait)
        {
            if (phase == BasicMovementPhase.Idle)
                return idle;

            if (gait == BasicMovementGait.Walk)
            {
                return phase switch
                {
                    BasicMovementPhase.MoveStart => IsConfigured(walkStart) ? walkStart : LocomotionAnimationPhaseConfig.AfterDuration("WalkStart", 0.08f),
                    BasicMovementPhase.MoveLoop => IsConfigured(walkLoop) ? walkLoop : LocomotionAnimationPhaseConfig.Manual("WalkLoop"),
                    BasicMovementPhase.MoveStop => IsConfigured(walkEnd) ? walkEnd : LocomotionAnimationPhaseConfig.AfterDuration("WalkEnd", 0.05f),
                    _ => idle
                };
            }

            return phase switch
            {
                BasicMovementPhase.MoveStart => IsConfigured(runStart) ? runStart : moveStart,
                BasicMovementPhase.MoveLoop => IsConfigured(runLoop) ? runLoop : moveLoop,
                BasicMovementPhase.MoveStop => IsConfigured(runEnd) ? runEnd : moveStop,
                _ => idle
            };
        }

        static bool IsConfigured(LocomotionAnimationPhaseConfig phase)
        {
            return !string.IsNullOrWhiteSpace(phase.AliasKey);
        }

        void ValidateMotionProfiles(RunLocomotionAnimationConfigValidationResult result)
        {
            LocomotionPhaseMotionProfileBinding[] profiles = motionProfiles;
            if (profiles == null)
                return;

            for (int i = 0; i < profiles.Length; i++)
                LocomotionMotionProfileValidator.ValidateBinding(in profiles[i], $"MotionProfile[{i}]", result);
        }

        static void ValidatePhase(
            LocomotionAnimationPhaseConfig phase,
            string name,
            bool requireAutomaticExit,
            RunLocomotionAnimationConfigValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(phase.AliasKey))
                result.AddError($"{name} alias key is missing.");

            if (requireAutomaticExit && phase.ExitPolicy == LocomotionAnimationExitPolicy.Manual)
                result.AddError($"{name} exit policy must not be Manual.");

            if (phase.ExitPolicy == LocomotionAnimationExitPolicy.AfterDuration && phase.ExitDuration < 0f)
                result.AddError($"{name} exit duration is invalid.");
        }

        static BasicMovementPhaseTiming ResolveMovementTiming(
            LocomotionAnimationPhaseConfig phase,
            BasicMovementPhaseTiming fallback)
        {
            return phase.ExitPolicy switch
            {
                LocomotionAnimationExitPolicy.AfterDuration => phase.ExitDuration >= 0f
                    ? BasicMovementPhaseTiming.AfterDuration(phase.ExitDuration)
                    : fallback,
                LocomotionAnimationExitPolicy.OnAnimationEnd => fallback,
                _ => BasicMovementPhaseTiming.Manual
            };
        }
    }
}
