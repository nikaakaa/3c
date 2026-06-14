using System;
using UnityEngine;

namespace ThirdPersonMovement
{
    public enum TurnBackMotionYawSource
    {
        BakedMotionProfile = 1
    }

    public enum TurnBackMotionTranslationSource
    {
        None = 0,
        BakedMotionProfile = 1
    }

    [Serializable]
    public struct TurnBackMotionPolicy
    {
        public const string DefaultAliasKey = "Locomotion.Turn.Back";
        public const string DefaultBakedMotionProfileId = "Locomotion.Turn.Back";
        public const float DefaultTurnCompleteNormalizedTime = 1f;

        [SerializeField] bool enabled;
        [SerializeField] string aliasKey;
        [SerializeField] BasicMovementPhase entryPhase;
        [SerializeField] BasicMovementGait entryGait;
        [SerializeField] TurnBackMotionYawSource yawSource;
        [SerializeField] TurnBackMotionTranslationSource translationSource;
        [SerializeField] bool suppressInputRotation;
        [SerializeField] bool suppressInputPlanarMovement;
        [SerializeField, Range(0f, 1f)] float turnCompleteNormalizedTime;
        [SerializeField, Min(0f)] float enterFadeDuration;
        [SerializeField, Range(0f, 1f)] float startNormalizedTime;
        [SerializeField, Range(0f, 1f)] float lockInputNormalizedTime;
        [SerializeField, Range(0f, 1f)] float exitNormalizedTime;
        [SerializeField] string bakedMotionProfileId;

        public TurnBackMotionPolicy(
            string aliasKey,
            BasicMovementPhase entryPhase,
            BasicMovementGait entryGait,
            TurnBackMotionYawSource yawSource,
            TurnBackMotionTranslationSource translationSource,
            bool suppressInputRotation,
            bool suppressInputPlanarMovement,
            float turnCompleteNormalizedTime,
            float enterFadeDuration,
            float startNormalizedTime,
            float lockInputNormalizedTime,
            float exitNormalizedTime,
            string bakedMotionProfileId)
        {
            enabled = true;
            this.aliasKey = string.IsNullOrWhiteSpace(aliasKey) ? DefaultAliasKey : aliasKey.Trim();
            this.entryPhase = entryPhase;
            this.entryGait = entryGait;
            this.yawSource = yawSource;
            this.translationSource = translationSource;
            this.suppressInputRotation = suppressInputRotation;
            this.suppressInputPlanarMovement = suppressInputPlanarMovement;
            this.turnCompleteNormalizedTime = Mathf.Clamp01(turnCompleteNormalizedTime);
            this.enterFadeDuration = Mathf.Max(0f, enterFadeDuration);
            this.startNormalizedTime = Mathf.Clamp01(startNormalizedTime);
            this.lockInputNormalizedTime = Mathf.Clamp01(lockInputNormalizedTime);
            this.exitNormalizedTime = Mathf.Clamp01(exitNormalizedTime);
            this.bakedMotionProfileId = bakedMotionProfileId ?? string.Empty;
        }

        public bool IsEnabled => enabled;
        public string AliasKey => string.IsNullOrWhiteSpace(aliasKey) ? DefaultAliasKey : aliasKey.Trim();
        public BasicMovementPhase EntryPhase => enabled ? entryPhase : BasicMovementPhase.MoveLoop;
        public BasicMovementGait EntryGait => enabled ? entryGait : BasicMovementGait.Run;
        public TurnBackMotionYawSource YawSource => yawSource;
        public TurnBackMotionTranslationSource TranslationSource => translationSource;
        public bool SuppressInputRotation => enabled ? suppressInputRotation : true;
        public bool SuppressInputPlanarMovement => enabled ? suppressInputPlanarMovement : true;
        public float TurnCompleteNormalizedTime => enabled && turnCompleteNormalizedTime > 0f
            ? Mathf.Clamp01(turnCompleteNormalizedTime)
            : DefaultTurnCompleteNormalizedTime;
        public float EnterFadeDuration => Mathf.Max(0f, enterFadeDuration);
        public float StartNormalizedTime => Mathf.Clamp01(startNormalizedTime);
        public float LockInputNormalizedTime => Mathf.Clamp01(lockInputNormalizedTime);
        public float ExitNormalizedTime => Mathf.Clamp01(exitNormalizedTime);
        public string BakedMotionProfileId => bakedMotionProfileId ?? string.Empty;
        public bool HasBakedMotionProfile => !string.IsNullOrWhiteSpace(BakedMotionProfileId);

        public static TurnBackMotionPolicy Default => new TurnBackMotionPolicy(
            DefaultAliasKey,
            BasicMovementPhase.MoveLoop,
            BasicMovementGait.Run,
            TurnBackMotionYawSource.BakedMotionProfile,
            TurnBackMotionTranslationSource.BakedMotionProfile,
            true,
            true,
            DefaultTurnCompleteNormalizedTime,
            0.08f,
            0f,
            DefaultTurnCompleteNormalizedTime,
            DefaultTurnCompleteNormalizedTime,
            DefaultBakedMotionProfileId);
    }

    public enum BasicMovementPlanarDeltaSpace
    {
        Local,
        World,
        EntryLocal
    }

    public readonly struct BasicMovementMotionFacts
    {
        public BasicMovementMotionFacts(
            bool hasAnimationMotion,
            Vector3 localPlanarDelta,
            float yawDelta,
            BasicMovementPhase sourcePhase,
            string sourceAliasKey,
            bool suppressInputRotation = false,
            bool suppressInputPlanarMovement = false,
            BasicMovementPlanarDeltaSpace planarDeltaSpace = BasicMovementPlanarDeltaSpace.Local,
            TurnBackMotionPolicy turnBackMotionPolicy = default,
            Vector3 entryPlanarBasisForward = default)
        {
            HasAnimationMotion = hasAnimationMotion;
            LocalPlanarDelta = new Vector3(localPlanarDelta.x, 0f, localPlanarDelta.z);
            YawDelta = yawDelta;
            SourcePhase = sourcePhase;
            SourceAliasKey = sourceAliasKey ?? string.Empty;
            SuppressInputRotation = suppressInputRotation;
            SuppressInputPlanarMovement = suppressInputPlanarMovement;
            PlanarDeltaSpace = planarDeltaSpace;
            TurnBackMotionPolicy = turnBackMotionPolicy;
            HasTurnBackMotionPolicy = turnBackMotionPolicy.IsEnabled;
            EntryPlanarBasisForward = NormalizePlanarOrZero(entryPlanarBasisForward);
        }

        public bool HasAnimationMotion { get; }
        public Vector3 LocalPlanarDelta { get; }
        public float YawDelta { get; }
        public BasicMovementPhase SourcePhase { get; }
        public string SourceAliasKey { get; }
        public bool SuppressInputRotation { get; }
        public bool SuppressInputPlanarMovement { get; }
        public BasicMovementPlanarDeltaSpace PlanarDeltaSpace { get; }
        public TurnBackMotionPolicy TurnBackMotionPolicy { get; }
        public bool HasTurnBackMotionPolicy { get; }
        public Vector3 EntryPlanarBasisForward { get; }

        public static BasicMovementMotionFacts None(BasicMovementPhase phase)
        {
            return new BasicMovementMotionFacts(false, Vector3.zero, 0f, phase, string.Empty);
        }

        static Vector3 NormalizePlanarOrZero(Vector3 value)
        {
            value.y = 0f;
            float sqrMagnitude = value.sqrMagnitude;
            return sqrMagnitude > 0.000001f ? value / Mathf.Sqrt(sqrMagnitude) : Vector3.zero;
        }
    }
}
