using System;
using ThirdPersonMovement;
using UnityEngine;

namespace ThirdPersonAnimation
{
    [CreateAssetMenu(fileName = "LocomotionFootPhaseProfile", menuName = "3C/Animation/LocomotionFootPhaseProfile")]
    public sealed class LocomotionFootPhaseProfileSO : ScriptableObject
    {
        [SerializeField] BasicMovementPhase phase = BasicMovementPhase.MoveLoop;
        [SerializeField] BasicMovementGait gait = BasicMovementGait.Run;
        [SerializeField] string aliasKey = "RunLoop";
        [SerializeField] bool enablePhaseMatching = true;
        [SerializeField] bool loop;
        [SerializeField] LocomotionFootPhaseMarker[] markers = Array.Empty<LocomotionFootPhaseMarker>();

        public BasicMovementPhase Phase => phase;
        public BasicMovementGait Gait => gait;
        public string AliasKey => aliasKey ?? string.Empty;
        public bool EnablePhaseMatching => enablePhaseMatching;
        public bool Loop => loop;
        public LocomotionFootPhaseMarker[] Markers => markers ?? Array.Empty<LocomotionFootPhaseMarker>();

        public void SetProfileData(
            BasicMovementPhase phase,
            BasicMovementGait gait,
            string aliasKey,
            bool enablePhaseMatching,
            bool loop,
            params LocomotionFootPhaseMarker[] markers)
        {
            this.phase = phase;
            this.gait = gait;
            this.aliasKey = aliasKey ?? string.Empty;
            this.enablePhaseMatching = enablePhaseMatching;
            this.loop = loop;
            this.markers = markers ?? Array.Empty<LocomotionFootPhaseMarker>();
        }
    }
}

