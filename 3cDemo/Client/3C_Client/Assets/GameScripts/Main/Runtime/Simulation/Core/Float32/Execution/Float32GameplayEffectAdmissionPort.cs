using System;
using System.Collections.Generic;
using System.Linq;

namespace ThirdPersonSimulation
{
    internal sealed partial class Float32GameplayEffectTarget
    {
        ActorId IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, Float32Scalar>.ActorId => m_ActorId;
        bool IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, Float32Scalar>.ContextIsValid(SimulationGameplayEffectApplication application) => application.Context.IsValid;
        ActorId IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, Float32Scalar>.SourceActorId(SimulationGameplayEffectApplication application) => application.Context.SourceActorId;
        ActorId IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, Float32Scalar>.TargetActorId(SimulationGameplayEffectApplication application) => application.Context.TargetActorId;
        bool IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, Float32Scalar>.IsPredicted(SimulationGameplayEffectApplication application) => application.Context.IsPredicted;
        ulong IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, Float32Scalar>.SourceActionInstanceId(SimulationGameplayEffectApplication application) => application.Context.SourceActionInstanceId;
        ulong IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, Float32Scalar>.PredictionKey(SimulationGameplayEffectApplication application) => application.Context.PredictionKey;
        ulong IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, Float32Scalar>.AuthoritativeInstanceId(SimulationGameplayEffectApplication application) => application.AuthoritativeInstanceId;
        uint IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, Float32Scalar>.ApplicationDefinitionRevision(SimulationGameplayEffectApplication application) => application.DefinitionRevision;

        bool IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, Float32Scalar>.TryCreateSpec(
            SimulationGameplayEffectApplication application,
            out PortableEffectSpecState spec)
        {
            try
            {
                spec = new PortableEffectSpecState
                {
                    Definition = m_State.Program.RequireEffect(application.EffectId),
                    Context = application.Context
                };
                return true;
            }
            catch (KeyNotFoundException)
            {
                spec = null;
                return false;
            }
        }

        uint IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, Float32Scalar>.DefinitionRevision(PortableEffectSpecState spec) => spec.Definition.Revision;
        int IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, Float32Scalar>.SuppliedParameterCount(SimulationGameplayEffectApplication application) => application.SetByCallerValues.Count;
        string IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, Float32Scalar>.SuppliedParameterId(SimulationGameplayEffectApplication application, int index) => application.SetByCallerValues[index].ParameterId;
        Float32Scalar IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, Float32Scalar>.SuppliedParameterValue(SimulationGameplayEffectApplication application, int index) => application.SetByCallerValues[index].Value;
        bool IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, Float32Scalar>.DeclaresParameter(PortableEffectSpecState spec, string parameterId) => spec.Definition.SetByCallerParameters.Contains(parameterId, StringComparer.Ordinal);
        int IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, Float32Scalar>.RequiredParameterCount(PortableEffectSpecState spec) => spec.Definition.SetByCallerParameters.Length;
        string IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, Float32Scalar>.RequiredParameterId(PortableEffectSpecState spec, int index) => spec.Definition.SetByCallerParameters[index];
        bool IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, Float32Scalar>.ContainsParameter(PortableEffectSpecState spec, string parameterId) => spec.SetByCaller.ContainsKey(parameterId);
        void IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, Float32Scalar>.AddParameter(PortableEffectSpecState spec, string parameterId, Float32Scalar value) => spec.SetByCaller.Add(parameterId, value);
        int IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, Float32Scalar>.SuppliedSourceAttributeCount(SimulationGameplayEffectApplication application) => application.SourceAttributeSnapshots.Count;
        string IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, Float32Scalar>.SuppliedSourceAttributeId(SimulationGameplayEffectApplication application, int index) => application.SourceAttributeSnapshots[index].AttributeId;
        Float32Scalar IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, Float32Scalar>.SuppliedSourceAttributeValue(SimulationGameplayEffectApplication application, int index) => application.SourceAttributeSnapshots[index].Value;
        string IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, Float32Scalar>.NormalizeAttributeId(string attributeId) => SimulationGameplayEffectProgram.NormalizeAttribute(attributeId);
        IEnumerable<string> IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, Float32Scalar>.RequiredSnapshotAttributes(PortableEffectSpecState spec, GameplayEffectAttributeSnapshotKind kind) =>
            CollectSnapshotAttributes(
                spec.Definition,
                kind == GameplayEffectAttributeSnapshotKind.Source
                    ? PortableMagnitudeSource.SourceAttributeSnapshot
                    : PortableMagnitudeSource.TargetAttributeSnapshot);

        bool IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, Float32Scalar>.TryReadTargetAttribute(string attributeId, out Float32Scalar value)
        {
            if (m_State.TryGetAttribute(attributeId, out PortableAttributeState attribute))
            {
                value = attribute.CurrentValue;
                return true;
            }
            value = default;
            return false;
        }

        void IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, Float32Scalar>.AddSourceAttribute(PortableEffectSpecState spec, string attributeId, Float32Scalar value) => spec.SourceAttributes.Add(attributeId, value);
        void IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, Float32Scalar>.AddTargetAttribute(PortableEffectSpecState spec, string attributeId, Float32Scalar value) => spec.TargetAttributes.Add(attributeId, value);
        string[] IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, Float32Scalar>.CopyTargetTags() => m_State.CopyOwnedTags().ToArray();
        int IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, Float32Scalar>.SourceTagCount(SimulationGameplayEffectApplication application) => application.SourceTagSnapshot.Count;
        string IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, Float32Scalar>.SourceTag(SimulationGameplayEffectApplication application, int index) => application.SourceTagSnapshot[index];
        string IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, Float32Scalar>.NormalizeTag(string tag) => SimulationGameplayEffectProgram.NormalizeTag(tag);
        void IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, Float32Scalar>.SetTargetTags(PortableEffectSpecState spec, string[] tags) => spec.TargetTags = tags;
        void IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, Float32Scalar>.SetSourceTags(PortableEffectSpecState spec, string[] tags) => spec.SourceTags = tags;
        bool IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, Float32Scalar>.RequiresDuration(PortableEffectSpecState spec) => spec.Definition.DurationPolicy == PortableEffectDurationPolicy.Duration;
        bool IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, Float32Scalar>.HasPeriod(PortableEffectSpecState spec) => spec.Definition.HasPeriod;

        bool IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, Float32Scalar>.TryResolveDurationTicks(PortableEffectSpecState spec, out ulong ticks)
        {
            ticks = 0;
            return TryResolveMagnitude(spec, spec.Definition.DurationMagnitude, out Float32Scalar seconds, out _) &&
                   TrySecondsToTicks(seconds, out ticks);
        }

        bool IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, Float32Scalar>.TryResolvePeriodTicks(PortableEffectSpecState spec, out ulong ticks)
        {
            ticks = 0;
            return TryResolveMagnitude(spec, spec.Definition.PeriodMagnitude, out Float32Scalar seconds, out _) &&
                   TrySecondsToTicks(seconds, out ticks);
        }

        void IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, Float32Scalar>.SetDurationTicks(PortableEffectSpecState spec, ulong ticks) => spec.DurationTicks = ticks;
        void IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, Float32Scalar>.SetPeriodTicks(PortableEffectSpecState spec, ulong ticks) => spec.PeriodTicks = ticks;
        GameplayEffectPreparedSpec<PortableEffectSpecState> IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, Float32Scalar>.DescribeSpec(PortableEffectSpecState spec) => DescribeSpec(spec);
    }
}

