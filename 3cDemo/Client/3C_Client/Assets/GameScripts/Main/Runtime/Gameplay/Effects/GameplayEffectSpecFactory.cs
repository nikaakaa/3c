using System;
using System.Collections.Generic;
using ThirdPersonGameplay.Attributes;
using ThirdPersonGameplay.Tags;

namespace ThirdPersonGameplay.Effects
{
    internal sealed class GameplayEffectSpecFactory
    {
        readonly GameplayEffectRuntimeState m_State;

        public GameplayEffectSpecFactory(GameplayEffectRuntimeState state)
        {
            m_State = state ?? throw new ArgumentNullException(nameof(state));
        }

        public bool TryBuild(
            GameplayEffectApplyRequest request,
            out GameplayEffectSpec spec,
            out GameplayEffectApplyResultCode code,
            out string reason)
        {
            spec = null;
            code = GameplayEffectApplyResultCode.Rejected;
            reason = string.Empty;
            if (request == null || !request.EffectId.IsValid)
                return Fail(GameplayEffectApplyResultCode.InvalidContext, "EffectIdMissing", out code, out reason);
            if (!m_State.Definition.TryGetEffect(request.EffectId, out GameplayEffectDefinitionData definition))
                return Fail(GameplayEffectApplyResultCode.MissingDefinition, "EffectDefinitionMissing", out code, out reason);
            if (request.DefinitionRevision != 0 && request.DefinitionRevision != definition.DefinitionRevision)
                return Fail(GameplayEffectApplyResultCode.DefinitionRevisionMismatch, "DefinitionRevisionMismatch", out code, out reason);
            if (request.Context.IsPredicted && !request.Context.HasPredictionIdentity)
                return Fail(GameplayEffectApplyResultCode.InvalidPrediction, "PredictionIdentityMissing", out code, out reason);

            if (!TryBuildSetByCaller(request, definition, out Dictionary<string, float> setByCaller, out code, out reason) ||
                !TryBuildSourceSnapshots(request, definition, out Dictionary<GameplayAttributeId, float> sourceSnapshots, out code, out reason) ||
                !TryBuildTargetSnapshots(definition, out Dictionary<GameplayAttributeId, float> targetSnapshots, out code, out reason))
                return false;

            bool selfSource = IsSelfSource(request.Context);
            GameplayTagId[] targetTags = m_State.Tags.CopyOwnedTags();
            GameplayTagId[] sourceTags = Copy(request.SourceTagSnapshot);
            if (sourceTags.Length == 0 && selfSource)
                sourceTags = targetTags;
            GameplayEffectStackKey stackKey = definition.StackingPolicy == GameplayEffectStackingPolicy.AggregateBySource
                ? new GameplayEffectStackKey(definition.EffectId, request.Context.SourceActorId)
                : new GameplayEffectStackKey(definition.EffectId, string.Empty);

            var draft = new GameplayEffectSpec(
                definition,
                request.Context,
                setByCaller,
                sourceSnapshots,
                targetSnapshots,
                sourceTags,
                targetTags,
                0,
                0,
                0,
                stackKey,
                request.AuthoritativeInstanceId,
                request.AuthoritativeLifecycleRevision);

            ulong durationTicks = 0;
            if (definition.DurationPolicy == GameplayEffectDurationPolicy.Duration &&
                (!TryResolveMagnitude(draft, definition.DurationMagnitude, out float durationSeconds, out _) ||
                 !TryConvertSecondsToTicks(durationSeconds, out durationTicks)))
                return Fail(GameplayEffectApplyResultCode.InvalidContext, "DurationMagnitudeInvalid", out code, out reason);

            ulong periodTicks = 0;
            if (definition.HasPeriod &&
                (!TryResolveMagnitude(draft, definition.PeriodMagnitude, out float periodSeconds, out _) ||
                 !TryConvertSecondsToTicks(periodSeconds, out periodTicks)))
                return Fail(GameplayEffectApplyResultCode.InvalidContext, "PeriodMagnitudeInvalid", out code, out reason);

            spec = new GameplayEffectSpec(
                definition,
                request.Context,
                setByCaller,
                sourceSnapshots,
                targetSnapshots,
                sourceTags,
                targetTags,
                durationTicks,
                periodTicks,
                periodTicks > 0 ? GameplayEffectRuntimeState.CheckedAdd(m_State.CurrentTick, periodTicks) : 0,
                stackKey,
                request.AuthoritativeInstanceId,
                request.AuthoritativeLifecycleRevision);
            code = GameplayEffectApplyResultCode.Applied;
            return true;
        }

        public bool TryResolveMagnitude(
            GameplayEffectSpec spec,
            GameplayMagnitudeData magnitude,
            out float value,
            out GameplayAttributeId liveAttribute)
        {
            value = 0f;
            liveAttribute = default;
            switch (magnitude.Source)
            {
                case GameplayMagnitudeSource.Constant:
                    value = magnitude.Constant;
                    break;
                case GameplayMagnitudeSource.SetByCaller:
                    if (!spec.TryGetSetByCaller(magnitude.SetByCallerParameterId, out value))
                        return false;
                    break;
                case GameplayMagnitudeSource.SourceAttributeSnapshot:
                    if (!spec.TryGetSourceAttribute(magnitude.AttributeId, out value))
                        return false;
                    break;
                case GameplayMagnitudeSource.TargetAttributeSnapshot:
                    if (!spec.TryGetTargetAttribute(magnitude.AttributeId, out value))
                        return false;
                    break;
                case GameplayMagnitudeSource.TargetAttributeLive:
                    if (!m_State.Attributes.TryGetValue(magnitude.AttributeId, out GameplayAttributeValue attribute))
                        return false;
                    value = attribute.CurrentValue;
                    liveAttribute = magnitude.AttributeId;
                    break;
                default:
                    return false;
            }
            value = value * magnitude.Coefficient + magnitude.PostAdd;
            return GameplayNumber.IsFinite(value);
        }

        public GameplayEffectSpec CreateRejectedSpec(GameplayEffectApplyRequest request)
        {
            if (request == null || !m_State.Definition.TryGetEffect(request.EffectId, out GameplayEffectDefinitionData definition))
                return null;
            return new GameplayEffectSpec(
                definition,
                request.Context,
                new Dictionary<string, float>(StringComparer.Ordinal),
                new Dictionary<GameplayAttributeId, float>(),
                new Dictionary<GameplayAttributeId, float>(),
                Array.Empty<GameplayTagId>(),
                Array.Empty<GameplayTagId>(),
                0,
                0,
                0,
                default,
                request.AuthoritativeInstanceId,
                request.AuthoritativeLifecycleRevision);
        }

        bool TryBuildSetByCaller(
            GameplayEffectApplyRequest request,
            GameplayEffectDefinitionData definition,
            out Dictionary<string, float> values,
            out GameplayEffectApplyResultCode code,
            out string reason)
        {
            values = new Dictionary<string, float>(StringComparer.Ordinal);
            code = GameplayEffectApplyResultCode.Applied;
            reason = string.Empty;
            for (int i = 0; i < request.SetByCallerValues.Count; i++)
            {
                GameplaySetByCallerValue value = request.SetByCallerValues[i];
                if (!GameplayNumber.IsFinite(value.Value))
                    return Fail(GameplayEffectApplyResultCode.InvalidContext, $"NonFiniteSetByCaller:{value.ParameterId}", out code, out reason);
                if (string.IsNullOrEmpty(value.ParameterId) || !definition.SetByCallerParameters.ContainsKey(value.ParameterId))
                    return Fail(GameplayEffectApplyResultCode.UndeclaredSetByCaller, $"UndeclaredSetByCaller:{value.ParameterId}", out code, out reason);
                if (!values.TryAdd(value.ParameterId, value.Value))
                    return Fail(GameplayEffectApplyResultCode.UndeclaredSetByCaller, $"DuplicateSetByCaller:{value.ParameterId}", out code, out reason);
            }
            foreach (KeyValuePair<string, bool> parameter in definition.SetByCallerParameters)
            {
                if (parameter.Value && !values.ContainsKey(parameter.Key))
                    return Fail(GameplayEffectApplyResultCode.MissingSetByCaller, $"MissingSetByCaller:{parameter.Key}", out code, out reason);
            }
            return true;
        }

        bool TryBuildSourceSnapshots(
            GameplayEffectApplyRequest request,
            GameplayEffectDefinitionData definition,
            out Dictionary<GameplayAttributeId, float> snapshots,
            out GameplayEffectApplyResultCode code,
            out string reason)
        {
            snapshots = new Dictionary<GameplayAttributeId, float>();
            code = GameplayEffectApplyResultCode.Applied;
            reason = string.Empty;
            var supplied = new Dictionary<GameplayAttributeId, float>();
            for (int i = 0; i < request.SourceAttributeSnapshots.Count; i++)
            {
                GameplayAttributeCapture value = request.SourceAttributeSnapshots[i];
                if (!GameplayNumber.IsFinite(value.Value))
                    return Fail(GameplayEffectApplyResultCode.InvalidContext, $"NonFiniteSourceAttribute:{value.AttributeId}", out code, out reason);
                if (!value.AttributeId.IsValid || !supplied.TryAdd(value.AttributeId, value.Value))
                    return Fail(GameplayEffectApplyResultCode.InvalidContext, $"DuplicateOrInvalidSourceAttribute:{value.AttributeId}", out code, out reason);
            }
            bool selfSource = IsSelfSource(request.Context);
            for (int i = 0; i < definition.SourceSnapshotAttributes.Length; i++)
            {
                GameplayAttributeId id = definition.SourceSnapshotAttributes[i];
                if (supplied.TryGetValue(id, out float value))
                    snapshots.Add(id, value);
                else if (selfSource && m_State.Attributes.TryGetValue(id, out GameplayAttributeValue attribute))
                    snapshots.Add(id, attribute.CurrentValue);
                else
                    return Fail(GameplayEffectApplyResultCode.InvalidContext, $"SourceAttributeSnapshotMissing:{id}", out code, out reason);
            }
            return true;
        }

        bool TryBuildTargetSnapshots(
            GameplayEffectDefinitionData definition,
            out Dictionary<GameplayAttributeId, float> snapshots,
            out GameplayEffectApplyResultCode code,
            out string reason)
        {
            snapshots = new Dictionary<GameplayAttributeId, float>();
            code = GameplayEffectApplyResultCode.Applied;
            reason = string.Empty;
            for (int i = 0; i < definition.TargetSnapshotAttributes.Length; i++)
            {
                GameplayAttributeId id = definition.TargetSnapshotAttributes[i];
                if (!m_State.Attributes.TryGetValue(id, out GameplayAttributeValue value))
                    return Fail(GameplayEffectApplyResultCode.InvalidContext, $"TargetAttributeSnapshotMissing:{id}", out code, out reason);
                snapshots.Add(id, value.CurrentValue);
            }
            return true;
        }

        bool TryConvertSecondsToTicks(float seconds, out ulong ticks)
        {
            ticks = 0;
            if (!GameplayNumber.IsFinite(seconds) || seconds <= 0f || m_State.Definition.LogicTickRate <= 0)
                return false;
            double value = Math.Ceiling(seconds * m_State.Definition.LogicTickRate);
            if (value < 1d || value > ulong.MaxValue)
                return false;
            ticks = (ulong)value;
            return true;
        }

        static bool IsSelfSource(GameplayEffectContext context)
        {
            return string.IsNullOrEmpty(context.SourceActorId) ||
                   string.Equals(context.SourceActorId, context.TargetActorId, StringComparison.Ordinal);
        }

        static bool Fail(
            GameplayEffectApplyResultCode failureCode,
            string failureReason,
            out GameplayEffectApplyResultCode code,
            out string reason)
        {
            code = failureCode;
            reason = failureReason;
            return false;
        }

        static T[] Copy<T>(IReadOnlyList<T> values)
        {
            if (values == null || values.Count == 0)
                return Array.Empty<T>();
            var result = new T[values.Count];
            for (int i = 0; i < values.Count; i++)
                result[i] = values[i];
            return result;
        }
    }
}
