using ThirdPersonCharacterStateMachine;
using ThirdPersonInput;
using UnityEngine;

namespace ThirdPersonAction
{
    [System.Serializable]
    public struct DodgeActionVariantAuthoring
    {
        [SerializeField] DodgeActionVariant variant;
        [SerializeField, Min(0f)] float duration;
        [SerializeField, Min(0f)] float distance;
        [SerializeField] bool rotateToDirection;
        [SerializeField] string animationKey;

        public DodgeActionVariantAuthoring(
            DodgeActionVariant variant,
            float duration,
            float distance,
            bool rotateToDirection,
            string animationKey)
        {
            this.variant = variant;
            this.duration = duration;
            this.distance = distance;
            this.rotateToDirection = rotateToDirection;
            this.animationKey = animationKey ?? string.Empty;
        }

        public DodgeActionVariant Variant => variant;
        public float Duration => duration;
        public float Distance => distance;
        public bool RotateToDirection => rotateToDirection;
        public string AnimationKey => animationKey;

        public DodgeActionVariantDefinition ToDefinition()
        {
            return new DodgeActionVariantDefinition(
                variant,
                duration,
                distance,
                rotateToDirection,
                new ActionAnimationKey(animationKey));
        }
    }

    [CreateAssetMenu(fileName = "CharacterActionDefinition", menuName = "3C/Action/CharacterActionDefinition")]
    public sealed class CharacterActionDefinitionSO : ScriptableObject
    {
        [SerializeField] string actionStateId;
        [SerializeField] ActionRequestType requestType;
        [SerializeField] InputRequestKind sourceInputKind;
        [SerializeField] string motionSourceStateId;
        [SerializeField, Min(0)] int priority;
        [SerializeField, Min(0)] int resistance;
        [SerializeField] DodgeActionVariantAuthoring directionalDodge;
        [SerializeField] DodgeActionVariantAuthoring backstepDodge;
        [SerializeField] ActionBranchTimelineAuthoring actionBranchTimeline;

        public ActionStateId ActionState => new ActionStateId(actionStateId);
        public ActionRequestType RequestType => requestType;
        public InputRequestKind SourceInputKind => sourceInputKind;
        public ActionBranchTimelineAuthoring ActionBranchTimeline => actionBranchTimeline;

        public CharacterActionDefinition ToDefinition()
        {
            ActionStateId actionState = new ActionStateId(actionStateId);
            CharacterStateId motionSourceState = new CharacterStateId(motionSourceStateId);
            return new CharacterActionDefinition(
                actionState,
                requestType,
                sourceInputKind,
                motionSourceState,
                priority,
                resistance,
                directionalDodge.ToDefinition(),
                backstepDodge.ToDefinition(),
                actionBranchTimeline.ToBranchDefinition(actionState, 0));
        }

        public CharacterActionCatalogValidationResult Validate()
        {
            CharacterActionCatalogValidationResult result = new CharacterActionCatalogValidationResult();
            ValidateInto(result, name);
            return result;
        }

        public void ValidateInto(CharacterActionCatalogValidationResult result, string owner)
        {
            string prefix = string.IsNullOrWhiteSpace(owner) ? "Action definition" : owner;
            CharacterActionDefinition definition = ToDefinition();
            if (!definition.ActionState.IsValid)
                result.AddError($"{prefix} action id is missing.");
            if (definition.RequestBinding.RequestType == ActionRequestType.None)
                result.AddError($"{prefix} request type is missing.");
            if (!definition.MotionSourceState.IsValid)
                result.AddError($"{prefix} motion source state is missing.");
            if (definition.Priority < 0)
                result.AddError($"{prefix} priority is invalid.");
            if (definition.Resistance < 0)
                result.AddError($"{prefix} resistance is invalid.");

            if (definition.IsDodge)
            {
                ValidateDodgeVariant(result, prefix, directionalDodge, DodgeActionVariant.Directional);
                ValidateDodgeVariant(result, prefix, backstepDodge, DodgeActionVariant.Backstep);
            }

            actionBranchTimeline.ValidateInto(result, prefix, definition.ActionState, 0);
        }

        static void ValidateDodgeVariant(
            CharacterActionCatalogValidationResult result,
            string prefix,
            DodgeActionVariantAuthoring authoring,
            DodgeActionVariant expected)
        {
            if (authoring.Variant != expected)
                result.AddError($"{prefix} {expected} variant id is invalid.");
            if (authoring.Duration <= 0f)
                result.AddError($"{prefix} {expected} duration is missing.");
            if (authoring.Distance <= 0f)
                result.AddError($"{prefix} {expected} distance is missing.");
            if (string.IsNullOrWhiteSpace(authoring.AnimationKey))
                result.AddError($"{prefix} {expected} animation key is missing.");
        }
    }
}
