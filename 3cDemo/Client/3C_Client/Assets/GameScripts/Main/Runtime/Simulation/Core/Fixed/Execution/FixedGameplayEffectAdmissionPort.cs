using ThirdPersonSimulation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ThirdPersonSimulation.Fixed
{
    internal sealed partial class FixedGameplayEffectTarget
    {
        ActorId IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, FixedScalar>.ActorId => m_ActorId;
        bool IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, FixedScalar>.ContextIsValid(SimulationGameplayEffectApplication application) => application.Context.IsValid;
        ActorId IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, FixedScalar>.SourceActorId(SimulationGameplayEffectApplication application) => application.Context.SourceActorId;
        ActorId IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, FixedScalar>.TargetActorId(SimulationGameplayEffectApplication application) => application.Context.TargetActorId;
        bool IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, FixedScalar>.IsPredicted(SimulationGameplayEffectApplication application) => application.Context.IsPredicted;
        ulong IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, FixedScalar>.SourceActionInstanceId(SimulationGameplayEffectApplication application) => application.Context.SourceActionInstanceId;
        ulong IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, FixedScalar>.PredictionKey(SimulationGameplayEffectApplication application) => application.Context.PredictionKey;
        ulong IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, FixedScalar>.AuthoritativeInstanceId(SimulationGameplayEffectApplication application) => application.AuthoritativeInstanceId;
        uint IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, FixedScalar>.ApplicationDefinitionRevision(SimulationGameplayEffectApplication application) => application.DefinitionRevision;

        bool IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, FixedScalar>.TryCreateSpec(
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

        uint IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, FixedScalar>.DefinitionRevision(PortableEffectSpecState spec) => spec.Definition.Revision;
        int IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, FixedScalar>.SuppliedParameterCount(SimulationGameplayEffectApplication application) => application.SetByCallerValues.Count;
        string IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, FixedScalar>.SuppliedParameterId(SimulationGameplayEffectApplication application, int index) => application.SetByCallerValues[index].ParameterId;
        FixedScalar IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, FixedScalar>.SuppliedParameterValue(SimulationGameplayEffectApplication application, int index) => application.SetByCallerValues[index].Value;
        bool IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, FixedScalar>.DeclaresParameter(PortableEffectSpecState spec, string parameterId) => spec.Definition.SetByCallerParameters.Contains(parameterId, StringComparer.Ordinal);
        int IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, FixedScalar>.RequiredParameterCount(PortableEffectSpecState spec) => spec.Definition.SetByCallerParameters.Length;
        string IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, FixedScalar>.RequiredParameterId(PortableEffectSpecState spec, int index) => spec.Definition.SetByCallerParameters[index];
        bool IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, FixedScalar>.ContainsParameter(PortableEffectSpecState spec, string parameterId) => spec.SetByCaller.ContainsKey(parameterId);
        void IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, FixedScalar>.AddParameter(PortableEffectSpecState spec, string parameterId, FixedScalar value) => spec.SetByCaller.Add(parameterId, value);
        int IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, FixedScalar>.SuppliedSourceAttributeCount(SimulationGameplayEffectApplication application) => application.SourceAttributeSnapshots.Count;
        string IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, FixedScalar>.SuppliedSourceAttributeId(SimulationGameplayEffectApplication application, int index) => application.SourceAttributeSnapshots[index].AttributeId;
        FixedScalar IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, FixedScalar>.SuppliedSourceAttributeValue(SimulationGameplayEffectApplication application, int index) => application.SourceAttributeSnapshots[index].Value;
        string IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, FixedScalar>.NormalizeAttributeId(string attributeId) => SimulationGameplayEffectProgram.NormalizeAttribute(attributeId);
        IEnumerable<string> IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, FixedScalar>.RequiredSnapshotAttributes(PortableEffectSpecState spec, GameplayEffectAttributeSnapshotKind kind) =>
            CollectSnapshotAttributes(
                spec.Definition,
                kind == GameplayEffectAttributeSnapshotKind.Source
                    ? PortableMagnitudeSource.SourceAttributeSnapshot
                    : PortableMagnitudeSource.TargetAttributeSnapshot);

        bool IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, FixedScalar>.TryReadTargetAttribute(string attributeId, out FixedScalar value)
        {
            if (m_State.TryGetAttribute(attributeId, out PortableAttributeState attribute))
            {
                value = attribute.CurrentValue;
                return true;
            }
            value = default;
            return false;
        }

        void IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, FixedScalar>.AddSourceAttribute(PortableEffectSpecState spec, string attributeId, FixedScalar value) => spec.SourceAttributes.Add(attributeId, value);
        void IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, FixedScalar>.AddTargetAttribute(PortableEffectSpecState spec, string attributeId, FixedScalar value) => spec.TargetAttributes.Add(attributeId, value);
        string[] IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, FixedScalar>.CopyTargetTags() => m_State.CopyOwnedTags().ToArray();
        int IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, FixedScalar>.SourceTagCount(SimulationGameplayEffectApplication application) => application.SourceTagSnapshot.Count;
        string IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, FixedScalar>.SourceTag(SimulationGameplayEffectApplication application, int index) => application.SourceTagSnapshot[index];
        string IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, FixedScalar>.NormalizeTag(string tag) => SimulationGameplayEffectProgram.NormalizeTag(tag);
        void IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, FixedScalar>.SetTargetTags(PortableEffectSpecState spec, string[] tags) => spec.TargetTags = tags;
        void IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, FixedScalar>.SetSourceTags(PortableEffectSpecState spec, string[] tags) => spec.SourceTags = tags;
        bool IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, FixedScalar>.RequiresDuration(PortableEffectSpecState spec) => spec.Definition.DurationPolicy == PortableEffectDurationPolicy.Duration;
        bool IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, FixedScalar>.HasPeriod(PortableEffectSpecState spec) => spec.Definition.HasPeriod;

        bool IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, FixedScalar>.TryResolveDurationTicks(PortableEffectSpecState spec, out ulong ticks)
        {
            ticks = 0;
            return TryResolveMagnitude(spec, spec.Definition.DurationMagnitude, out FixedScalar seconds, out _) &&
                   TrySecondsToTicks(seconds, out ticks);
        }

        bool IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, FixedScalar>.TryResolvePeriodTicks(PortableEffectSpecState spec, out ulong ticks)
        {
            ticks = 0;
            return TryResolveMagnitude(spec, spec.Definition.PeriodMagnitude, out FixedScalar seconds, out _) &&
                   TrySecondsToTicks(seconds, out ticks);
        }

        void IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, FixedScalar>.SetDurationTicks(PortableEffectSpecState spec, ulong ticks) => spec.DurationTicks = ticks;
        void IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, FixedScalar>.SetPeriodTicks(PortableEffectSpecState spec, ulong ticks) => spec.PeriodTicks = ticks;
        GameplayEffectPreparedSpec<PortableEffectSpecState> IGameplayEffectApplicationAdmissionPort<SimulationGameplayEffectApplication, PortableEffectSpecState, FixedScalar>.DescribeSpec(PortableEffectSpecState spec) => DescribeSpec(spec);
    }
}

