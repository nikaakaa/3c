using System;
using System.Collections.Generic;
using ThirdPersonCharacterStateMachine;
using ThirdPersonInput;

namespace ThirdPersonAction
{
    public readonly struct CharacterActionRequestBinding : IEquatable<CharacterActionRequestBinding>
    {
        public CharacterActionRequestBinding(ActionRequestType requestType, InputRequestKind sourceInputKind)
        {
            RequestType = requestType;
            SourceInputKind = sourceInputKind;
        }

        public ActionRequestType RequestType { get; }
        public InputRequestKind SourceInputKind { get; }
        public bool IsValid => RequestType != ActionRequestType.None;

        public bool Equals(CharacterActionRequestBinding other)
        {
            return RequestType == other.RequestType && SourceInputKind == other.SourceInputKind;
        }

        public override bool Equals(object obj)
        {
            return obj is CharacterActionRequestBinding other && Equals(other);
        }

        public override int GetHashCode()
        {
            return ((int)RequestType * 397) ^ (int)SourceInputKind;
        }

        public override string ToString()
        {
            return $"{RequestType}:{SourceInputKind}";
        }
    }

    public readonly struct DodgeActionVariantDefinition
    {
        public DodgeActionVariantDefinition(
            DodgeActionVariant variant,
            float duration,
            float distance,
            bool rotateToDirection,
            ActionAnimationKey animationKey)
            : this(variant, duration, distance, 0, 0, rotateToDirection, animationKey)
        {
        }

        public DodgeActionVariantDefinition(
            DodgeActionVariant variant,
            float duration,
            float distance,
            int priority,
            int resistance,
            bool rotateToDirection,
            ActionAnimationKey animationKey)
        {
            Variant = variant;
            Duration = Math.Max(0f, duration);
            Distance = Math.Max(0f, distance);
            Priority = Math.Max(0, priority);
            Resistance = Math.Max(0, resistance);
            RotateToDirection = rotateToDirection;
            AnimationKey = animationKey;
        }

        public DodgeActionVariant Variant { get; }
        public float Duration { get; }
        public float Distance { get; }
        public int Priority { get; }
        public int Resistance { get; }
        public bool RotateToDirection { get; }
        public ActionAnimationKey AnimationKey { get; }
        public bool HasDefinition => Duration > 0f && AnimationKey.IsValid;

        public DodgeActionVariantDefinition WithInterruptValues(int priority, int resistance)
        {
            return new DodgeActionVariantDefinition(
                Variant,
                Duration,
                Distance,
                priority,
                resistance,
                RotateToDirection,
                AnimationKey);
        }
    }

    public readonly struct CharacterActionDefinition
    {
        public CharacterActionDefinition(
            ActionStateId actionState,
            ActionRequestType requestType,
            InputRequestKind sourceInputKind,
            CharacterStateId motionSourceState,
            int priority,
            int resistance,
            DodgeActionVariantDefinition directionalDodge,
            DodgeActionVariantDefinition backstepDodge)
            : this(
                actionState,
                requestType,
                sourceInputKind,
                motionSourceState,
                priority,
                resistance,
                directionalDodge,
                backstepDodge,
                ActionBranchDefinition.Empty)
        {
        }

        public CharacterActionDefinition(
            ActionStateId actionState,
            ActionRequestType requestType,
            InputRequestKind sourceInputKind,
            CharacterStateId motionSourceState,
            int priority,
            int resistance,
            DodgeActionVariantDefinition directionalDodge,
            DodgeActionVariantDefinition backstepDodge,
            ActionBranchDefinition actionBranch)
        {
            ActionState = actionState;
            RequestBinding = new CharacterActionRequestBinding(requestType, sourceInputKind);
            MotionSourceState = motionSourceState;
            Priority = Math.Max(0, priority);
            Resistance = Math.Max(0, resistance);
            DirectionalDodge = directionalDodge.WithInterruptValues(Priority, Resistance);
            BackstepDodge = backstepDodge.WithInterruptValues(Priority, Resistance);
            ActionBranch = actionBranch;
        }

        public ActionStateId ActionState { get; }
        public CharacterActionRequestBinding RequestBinding { get; }
        public CharacterStateId MotionSourceState { get; }
        public int Priority { get; }
        public int Resistance { get; }
        public DodgeActionVariantDefinition DirectionalDodge { get; }
        public DodgeActionVariantDefinition BackstepDodge { get; }
        public ActionBranchDefinition ActionBranch { get; }
        public bool HasDefinition => ActionState.IsValid && RequestBinding.IsValid;
        public bool IsDodge => ActionState.Matches(ActionStateIds.Dodge);
        public bool HasActionBranch => ActionBranch.CanEvaluate;

        public bool TryGetActionBranch(out ActionBranchDefinition definition)
        {
            definition = ActionBranch;
            return definition.CanEvaluate;
        }

        public bool TryGetDodgeVariant(DodgeActionVariant variant, out DodgeActionVariantDefinition definition)
        {
            definition = variant == DodgeActionVariant.Backstep ? BackstepDodge : DirectionalDodge;
            return IsDodge && definition.HasDefinition;
        }

        public bool TryGetDodgeTuning(out DodgeActionTuning tuning)
        {
            if (!IsDodge || !DirectionalDodge.HasDefinition || !BackstepDodge.HasDefinition)
            {
                tuning = default;
                return false;
            }

            tuning = new DodgeActionTuning(
                DirectionalDodge.Duration,
                DirectionalDodge.Distance,
                BackstepDodge.Duration,
                BackstepDodge.Distance,
                Priority,
                Resistance,
                DirectionalDodge.RotateToDirection,
                BackstepDodge.RotateToDirection);
            return true;
        }

    }

    public readonly struct CharacterActionCatalog
    {
        readonly CharacterActionDefinition[] definitions;

        public CharacterActionCatalog(CharacterActionDefinition[] definitions)
        {
            this.definitions = definitions ?? Array.Empty<CharacterActionDefinition>();
        }

        public int Count => definitions != null ? definitions.Length : 0;
        public bool HasCatalog => Count > 0;

        public bool TryGetDefinition(ActionStateId actionState, out CharacterActionDefinition definition)
        {
            if (actionState.IsValid && definitions != null)
            {
                for (int i = 0; i < definitions.Length; i++)
                {
                    CharacterActionDefinition candidate = definitions[i];
                    if (candidate.ActionState.Matches(actionState))
                    {
                        definition = candidate;
                        return candidate.HasDefinition;
                    }
                }
            }

            definition = default;
            return false;
        }

        public bool TryGetDefinition(
            ActionRequestType requestType,
            InputRequestKind sourceInputKind,
            out CharacterActionDefinition definition)
        {
            CharacterActionRequestBinding binding = new CharacterActionRequestBinding(requestType, sourceInputKind);
            if (binding.IsValid && definitions != null)
            {
                for (int i = 0; i < definitions.Length; i++)
                {
                    CharacterActionDefinition candidate = definitions[i];
                    if (candidate.RequestBinding.Equals(binding))
                    {
                        definition = candidate;
                        return candidate.HasDefinition;
                    }
                }
            }

            definition = default;
            return false;
        }

        public bool TryGetDodgeDefinition(out CharacterActionDefinition definition)
        {
            return TryGetDefinition(ActionStateIds.Dodge, out definition) && definition.TryGetDodgeTuning(out _);
        }

        public bool TryGetActionBranch(ActionStateId actionState, out ActionBranchDefinition branch)
        {
            if (TryGetDefinition(actionState, out CharacterActionDefinition definition) &&
                definition.TryGetActionBranch(out branch))
            {
                return true;
            }

            branch = ActionBranchDefinition.Empty;
            return false;
        }

        public static CharacterActionCatalog Empty => new CharacterActionCatalog(Array.Empty<CharacterActionDefinition>());
    }

    public sealed class CharacterActionCatalogValidationResult
    {
        readonly List<string> errors = new List<string>();
        readonly List<string> warnings = new List<string>();

        public IReadOnlyList<string> Errors => errors;
        public IReadOnlyList<string> Warnings => warnings;
        public bool HasErrors => errors.Count > 0;

        public void AddError(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
                errors.Add(message);
        }

        public void AddWarning(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
                warnings.Add(message);
        }

        public string DescribeErrors()
        {
            return string.Join(Environment.NewLine, errors);
        }
    }
}
