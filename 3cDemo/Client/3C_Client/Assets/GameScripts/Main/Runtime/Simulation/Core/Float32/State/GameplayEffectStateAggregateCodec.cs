using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ThirdPersonSimulation
{
    internal static class GameplayEffectStateAggregateCodec
    {
        const uint TagsMagic = 0x32544745;
        const uint AttributesMagic = 0x32544741;
        const uint ActiveMagic = 0x32464745;
        const uint PeriodsMagic = 0x32504745;
        const uint JournalMagic = 0x324A4745;
        const int StateVersion = 1;

        internal static void Write(
            CanonicalWriter writer,
            GameplayEffectStateAggregate aggregate,
            SimulationGameplayEffectProgram program)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));
            if (aggregate == null)
                throw new ArgumentNullException(nameof(aggregate));
            if (program == null)
                throw new ArgumentNullException(nameof(program));

            var tagSources = new SortedDictionary<string, string[]>(StringComparer.Ordinal);
            var attributes = new SortedDictionary<string, PortableAttributeState>(StringComparer.Ordinal);
            var activeEffects = new List<PortableActiveEffectState>();
            var periods = new SortedDictionary<ulong, ulong>();
            var journal = new SortedDictionary<ulong, List<PortablePredictionRecord>>();
            var lifecycleRevisions = new SortedDictionary<ulong, ulong>();
            aggregate.CopyTo(tagSources, attributes, activeEffects, periods, journal, lifecycleRevisions);

            writer.WriteBytes(WriteTags(tagSources));
            writer.WriteBytes(WriteAttributes(attributes));
            writer.WriteBytes(WriteActiveEffects(activeEffects));
            writer.WriteBytes(WritePeriods(periods));
            writer.WriteBytes(WriteJournal(journal, lifecycleRevisions));
            writer.WriteUInt64(aggregate.ChangeCursor);
        }

        internal static GameplayEffectStateAggregate Read(
            CanonicalReader reader,
            SimulationGameplayEffectProgram program)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));
            if (program == null)
                throw new ArgumentNullException(nameof(program));

            var tagSources = new SortedDictionary<string, string[]>(StringComparer.Ordinal);
            var attributes = new SortedDictionary<string, PortableAttributeState>(StringComparer.Ordinal);
            var activeEffects = new List<PortableActiveEffectState>();
            var periods = new SortedDictionary<ulong, ulong>();
            var journal = new SortedDictionary<ulong, List<PortablePredictionRecord>>();
            var lifecycleRevisions = new SortedDictionary<ulong, ulong>();

            ReadTags(reader.ReadBytes(), tagSources);
            ReadAttributes(reader.ReadBytes(), program, attributes);
            ReadActiveEffects(reader.ReadBytes(), program, activeEffects);
            ReadPeriods(reader.ReadBytes(), periods);
            ReadJournal(reader.ReadBytes(), program, journal, lifecycleRevisions);

            var aggregate = new GameplayEffectStateAggregate(
                tagSources,
                attributes,
                activeEffects,
                periods,
                journal,
                lifecycleRevisions,
                reader.ReadUInt64());
            return new SimulationGameplayEffectState(program, aggregate).Freeze();
        }

        static byte[] WriteTags(IReadOnlyDictionary<string, string[]> tagSources)
        {
            using var writer = new CanonicalWriter();
            writer.WriteUInt32(TagsMagic);
            writer.WriteInt32(StateVersion);
            writer.WriteInt32(tagSources.Count);
            foreach (KeyValuePair<string, string[]> pair in tagSources)
            {
                writer.WriteString(pair.Key);
                WriteStrings(writer, pair.Value);
            }
            return writer.ToArray();
        }

        static void ReadTags(byte[] bytes, IDictionary<string, string[]> tagSources)
        {
            CanonicalReader reader = Reader(bytes, TagsMagic, "Gameplay Effect Tags");
            int count = ReadCount(reader, "Gameplay Tag source");
            string previous = string.Empty;
            for (int i = 0; i < count; i++)
            {
                string source = SimulationIdentity.Require(reader.ReadString(), "GameplayTagSource");
                if (i > 0 && string.CompareOrdinal(previous, source) >= 0)
                    throw new InvalidDataException("Gameplay Tag sources are not canonically ordered.");
                previous = source;
                tagSources.Add(source, ReadStrings(reader, SimulationGameplayEffectProgram.NormalizeTag));
            }
            reader.RequireComplete();
        }

        static byte[] WriteAttributes(IReadOnlyDictionary<string, PortableAttributeState> attributes)
        {
            using var writer = new CanonicalWriter();
            writer.WriteUInt32(AttributesMagic);
            writer.WriteInt32(StateVersion);
            writer.WriteInt32(attributes.Count);
            foreach (KeyValuePair<string, PortableAttributeState> pair in attributes)
            {
                PortableAttributeState attribute = pair.Value;
                writer.WriteString(pair.Key);
                writer.WriteScalar(attribute.BaseValue);
                writer.WriteScalar(attribute.CurrentValue);
                writer.WriteUInt64(attribute.Revision);
                writer.WriteInt32(attribute.Modifiers.Count);
                for (int i = 0; i < attribute.Modifiers.Count; i++)
                    WriteModifier(writer, attribute.Modifiers[i]);
            }
            return writer.ToArray();
        }

        static void ReadAttributes(
            byte[] bytes,
            SimulationGameplayEffectProgram program,
            IDictionary<string, PortableAttributeState> attributes)
        {
            CanonicalReader reader = Reader(bytes, AttributesMagic, "Gameplay Effect Attributes");
            int count = ReadCount(reader, "Gameplay Attribute");
            for (int i = 0; i < count; i++)
            {
                string id = SimulationGameplayEffectProgram.NormalizeAttribute(reader.ReadString());
                if (!program.Attributes.TryGetValue(id, out PortableAttributeDefinition definition) || attributes.ContainsKey(id))
                    throw new InvalidDataException($"Gameplay Attribute state '{id}' is unknown or duplicated.");
                var attribute = new PortableAttributeState
                {
                    Definition = definition,
                    BaseValue = reader.ReadScalar(),
                    CurrentValue = reader.ReadScalar(),
                    Revision = reader.ReadUInt64()
                };
                if (attribute.Revision == 0)
                    throw new InvalidDataException($"Gameplay Attribute '{id}' revision is invalid.");
                int modifierCount = ReadCount(reader, "Gameplay Attribute modifier");
                for (int modifierIndex = 0; modifierIndex < modifierCount; modifierIndex++)
                    attribute.Modifiers.Add(ReadModifier(reader));
                attribute.Modifiers.Sort(CompareModifier);
                attributes.Add(id, attribute);
            }
            reader.RequireComplete();
            if (attributes.Count != program.Attributes.Count)
                throw new InvalidDataException("Gameplay Attribute state does not match the Program catalog.");
        }

        static byte[] WriteActiveEffects(IReadOnlyList<PortableActiveEffectState> activeEffects)
        {
            using var writer = new CanonicalWriter();
            writer.WriteUInt32(ActiveMagic);
            writer.WriteInt32(StateVersion);
            writer.WriteInt32(activeEffects.Count);
            for (int i = 0; i < activeEffects.Count; i++)
                WriteActive(writer, activeEffects[i]);
            return writer.ToArray();
        }

        static void ReadActiveEffects(
            byte[] bytes,
            SimulationGameplayEffectProgram program,
            List<PortableActiveEffectState> activeEffects)
        {
            CanonicalReader reader = Reader(bytes, ActiveMagic, "Active Gameplay Effects");
            int count = ReadCount(reader, "Active Gameplay Effect");
            for (int i = 0; i < count; i++)
                activeEffects.Add(ReadActive(reader, program));
            reader.RequireComplete();
            activeEffects.Sort(CompareActive);
        }

        static byte[] WritePeriods(IReadOnlyDictionary<ulong, ulong> periods)
        {
            using var writer = new CanonicalWriter();
            writer.WriteUInt32(PeriodsMagic);
            writer.WriteInt32(StateVersion);
            writer.WriteInt32(periods.Count);
            foreach (KeyValuePair<ulong, ulong> pair in periods)
            {
                writer.WriteUInt64(pair.Key);
                writer.WriteUInt64(pair.Value);
            }
            return writer.ToArray();
        }

        static void ReadPeriods(byte[] bytes, IDictionary<ulong, ulong> periods)
        {
            CanonicalReader reader = Reader(bytes, PeriodsMagic, "Gameplay Effect Periods");
            int count = ReadCount(reader, "Gameplay Effect period");
            ulong previous = 0;
            for (int i = 0; i < count; i++)
            {
                ulong instance = reader.ReadUInt64();
                ulong tick = reader.ReadUInt64();
                if (instance == 0 || tick == 0 || i > 0 && instance <= previous)
                    throw new InvalidDataException("Gameplay Effect periods are invalid or not canonically ordered.");
                previous = instance;
                periods.Add(instance, tick);
            }
            reader.RequireComplete();
        }

        static byte[] WriteJournal(
            IReadOnlyDictionary<ulong, List<PortablePredictionRecord>> journal,
            IReadOnlyDictionary<ulong, ulong> lifecycleRevisions)
        {
            using var writer = new CanonicalWriter();
            writer.WriteUInt32(JournalMagic);
            writer.WriteInt32(StateVersion);
            writer.WriteInt32(journal.Count);
            foreach (KeyValuePair<ulong, List<PortablePredictionRecord>> pair in journal)
            {
                writer.WriteUInt64(pair.Key);
                writer.WriteInt32(pair.Value.Count);
                for (int i = 0; i < pair.Value.Count; i++)
                    WritePredictionRecord(writer, pair.Value[i]);
            }
            writer.WriteInt32(lifecycleRevisions.Count);
            foreach (KeyValuePair<ulong, ulong> pair in lifecycleRevisions)
            {
                writer.WriteUInt64(pair.Key);
                writer.WriteUInt64(pair.Value);
            }
            return writer.ToArray();
        }

        static void ReadJournal(
            byte[] bytes,
            SimulationGameplayEffectProgram program,
            IDictionary<ulong, List<PortablePredictionRecord>> journal,
            IDictionary<ulong, ulong> lifecycleRevisions)
        {
            CanonicalReader reader = Reader(bytes, JournalMagic, "Gameplay Effect Journal");
            int keyCount = ReadCount(reader, "Prediction key");
            ulong previous = 0;
            for (int i = 0; i < keyCount; i++)
            {
                ulong key = reader.ReadUInt64();
                if (key == 0 || i > 0 && key <= previous)
                    throw new InvalidDataException("Gameplay Effect prediction keys are invalid or not canonically ordered.");
                previous = key;
                int recordCount = ReadCount(reader, "Prediction record");
                var records = new List<PortablePredictionRecord>(recordCount);
                for (int recordIndex = 0; recordIndex < recordCount; recordIndex++)
                {
                    PortablePredictionRecord record = ReadPredictionRecord(reader, program);
                    if (record.Spec.Context.PredictionKey != key)
                        throw new InvalidDataException("Gameplay Effect prediction record key does not match its context.");
                    records.Add(record);
                }
                journal.Add(key, records);
            }

            int revisionCount = ReadCount(reader, "Gameplay Effect lifecycle revision");
            previous = 0;
            for (int i = 0; i < revisionCount; i++)
            {
                ulong instance = reader.ReadUInt64();
                ulong revision = reader.ReadUInt64();
                if (instance == 0 || revision == 0 || i > 0 && instance <= previous)
                    throw new InvalidDataException("Gameplay Effect lifecycle revisions are invalid or not canonically ordered.");
                previous = instance;
                lifecycleRevisions.Add(instance, revision);
            }
            reader.RequireComplete();
        }

        static void WriteActive(CanonicalWriter writer, PortableActiveEffectState active)
        {
            writer.WriteUInt64(active.Handle);
            writer.WriteUInt64(active.InstanceId);
            WriteSpec(writer, active.Spec);
            writer.WriteUInt64(active.StartTick);
            writer.WriteUInt64(active.EndTick);
            writer.WriteUInt64(active.InsertionSequence);
            writer.WriteInt32(active.StackCount);
            writer.WriteBoolean(active.Inhibited);
            writer.WriteUInt64(active.LifecycleRevision);
        }

        static PortableActiveEffectState ReadActive(CanonicalReader reader, SimulationGameplayEffectProgram program)
        {
            var active = new PortableActiveEffectState
            {
                Handle = reader.ReadUInt64(),
                InstanceId = reader.ReadUInt64(),
                Spec = ReadSpec(reader, program),
                StartTick = reader.ReadUInt64(),
                EndTick = reader.ReadUInt64(),
                InsertionSequence = reader.ReadUInt64(),
                StackCount = reader.ReadInt32(),
                Inhibited = reader.ReadBoolean(),
                LifecycleRevision = reader.ReadUInt64()
            };
            if (active.Handle == 0 || active.InstanceId == 0 || active.StartTick == 0 ||
                active.InsertionSequence == 0 || active.StackCount <= 0 || active.LifecycleRevision == 0)
                throw new InvalidDataException("Active Gameplay Effect identity is incomplete.");
            return active;
        }

        static void WriteSpec(CanonicalWriter writer, PortableEffectSpecState spec)
        {
            writer.WriteString(spec.Definition.Id);
            WriteContext(writer, spec.Context);
            WriteScalarMap(writer, spec.SetByCaller);
            WriteScalarMap(writer, spec.SourceAttributes);
            WriteScalarMap(writer, spec.TargetAttributes);
            WriteStrings(writer, spec.SourceTags);
            WriteStrings(writer, spec.TargetTags);
            writer.WriteUInt64(spec.DurationTicks);
            writer.WriteUInt64(spec.PeriodTicks);
        }

        static PortableEffectSpecState ReadSpec(CanonicalReader reader, SimulationGameplayEffectProgram program)
        {
            PortableEffectDefinition definition = program.RequireEffect(reader.ReadString());
            var spec = new PortableEffectSpecState
            {
                Definition = definition,
                Context = ReadContext(reader),
                SourceTags = Array.Empty<string>(),
                TargetTags = Array.Empty<string>()
            };
            ReadScalarMap(reader, spec.SetByCaller, value => SimulationIdentity.Require(value, "SetByCallerParameter"));
            ReadScalarMap(reader, spec.SourceAttributes, SimulationGameplayEffectProgram.NormalizeAttribute);
            ReadScalarMap(reader, spec.TargetAttributes, SimulationGameplayEffectProgram.NormalizeAttribute);
            spec.SourceTags = ReadStrings(reader, SimulationGameplayEffectProgram.NormalizeTag);
            spec.TargetTags = ReadStrings(reader, SimulationGameplayEffectProgram.NormalizeTag);
            spec.DurationTicks = reader.ReadUInt64();
            spec.PeriodTicks = reader.ReadUInt64();
            return spec;
        }

        static void WritePredictionRecord(CanonicalWriter writer, PortablePredictionRecord record)
        {
            WriteSpec(writer, record.Spec);
            writer.WriteUInt64(record.Handle);
            writer.WriteUInt64(record.InstanceId);
            writer.WriteBoolean(record.CreatedActive);
            writer.WriteBoolean(record.HasActiveBefore);
            if (record.HasActiveBefore)
                WriteActiveSnapshot(writer, record.ActiveBefore);
            writer.WriteBoolean(record.Confirmed);
            WriteStrings(writer, record.CueIds.OrderBy(value => value, StringComparer.Ordinal));
            writer.WriteInt32(record.Attributes.Count);
            foreach (KeyValuePair<string, PortablePredictionAttributeSnapshot> pair in record.Attributes)
            {
                PortablePredictionAttributeSnapshot value = pair.Value;
                writer.WriteString(pair.Key);
                writer.WriteScalar(value.BaseValue);
                writer.WriteScalar(value.CurrentValue);
                writer.WriteUInt64(value.BeforeRevision);
                writer.WriteUInt64(value.AfterRevision);
            }
        }

        static PortablePredictionRecord ReadPredictionRecord(
            CanonicalReader reader,
            SimulationGameplayEffectProgram program)
        {
            var record = new PortablePredictionRecord
            {
                Spec = ReadSpec(reader, program),
                Handle = reader.ReadUInt64(),
                InstanceId = reader.ReadUInt64(),
                CreatedActive = reader.ReadBoolean(),
                HasActiveBefore = reader.ReadBoolean()
            };
            if (record.HasActiveBefore)
                record.ActiveBefore = ReadActiveSnapshot(reader);
            record.Confirmed = reader.ReadBoolean();
            record.CueIds.AddRange(ReadStrings(reader, value => SimulationIdentity.Require(value, "GameplayCue")));
            int attributeCount = ReadCount(reader, "Prediction Attribute");
            for (int i = 0; i < attributeCount; i++)
            {
                string id = SimulationGameplayEffectProgram.NormalizeAttribute(reader.ReadString());
                record.Attributes.Add(
                    id,
                    new PortablePredictionAttributeSnapshot(
                        id,
                        reader.ReadScalar(),
                        reader.ReadScalar(),
                        reader.ReadUInt64(),
                        reader.ReadUInt64()));
            }
            if (record.Handle == 0 || record.InstanceId == 0 || record.Spec.Context.PredictionKey == 0)
                throw new InvalidDataException("Gameplay Effect prediction record identity is incomplete.");
            return record;
        }

        static void WriteActiveSnapshot(CanonicalWriter writer, GameplayEffectActiveControlSnapshot snapshot)
        {
            writer.WriteUInt64(snapshot.InstanceId);
            writer.WriteUInt64(snapshot.StartTick);
            writer.WriteUInt64(snapshot.EndTick);
            writer.WriteUInt64(snapshot.NextPeriodTick);
            writer.WriteInt32(snapshot.StackCount);
            writer.WriteBoolean(snapshot.Inhibited);
            writer.WriteUInt64(snapshot.LifecycleRevision);
        }

        static GameplayEffectActiveControlSnapshot ReadActiveSnapshot(CanonicalReader reader)
        {
            return new GameplayEffectActiveControlSnapshot(
                reader.ReadUInt64(),
                reader.ReadUInt64(),
                reader.ReadUInt64(),
                reader.ReadUInt64(),
                reader.ReadInt32(),
                reader.ReadBoolean(),
                reader.ReadUInt64());
        }

        static void WriteContext(CanonicalWriter writer, SimulationGameplayEffectContext context)
        {
            writer.WriteString(context.SourceActorId.Value);
            writer.WriteString(context.TargetActorId.Value);
            writer.WriteUInt64(context.SourceActionInstanceId);
            writer.WriteUInt64(context.PredictionKey);
            writer.WriteUInt64(context.GameplayResultId);
            writer.WriteUInt64(context.SourceTick);
            writer.WriteByte((byte)context.ApplicationMode);
        }

        static SimulationGameplayEffectContext ReadContext(CanonicalReader reader)
        {
            return new SimulationGameplayEffectContext(
                new ActorId(reader.ReadString()),
                new ActorId(reader.ReadString()),
                reader.ReadUInt64(),
                reader.ReadUInt64(),
                reader.ReadUInt64(),
                reader.ReadUInt64(),
                (SimulationGameplayEffectApplicationMode)reader.ReadByte());
        }

        static void WriteModifier(CanonicalWriter writer, PortableAttributeModifierState modifier)
        {
            writer.WriteUInt64(modifier.Handle);
            writer.WriteUInt64(modifier.SourceEffectHandle);
            writer.WriteByte((byte)modifier.Operation);
            writer.WriteScalar(modifier.Magnitude);
            writer.WriteInt32(modifier.Priority);
            writer.WriteByte((byte)modifier.ClampBound);
            writer.WriteString(modifier.LiveAttributeId);
            writer.WriteScalar(modifier.LiveCoefficient);
            writer.WriteScalar(modifier.LivePostAdd);
            writer.WriteUInt64(modifier.InsertionSequence);
        }

        static PortableAttributeModifierState ReadModifier(CanonicalReader reader)
        {
            var modifier = new PortableAttributeModifierState
            {
                Handle = reader.ReadUInt64(),
                SourceEffectHandle = reader.ReadUInt64(),
                Operation = (PortableModifierOperation)reader.ReadByte(),
                Magnitude = reader.ReadScalar(),
                Priority = reader.ReadInt32(),
                ClampBound = (PortableClampBound)reader.ReadByte(),
                LiveAttributeId = reader.ReadString(),
                LiveCoefficient = reader.ReadScalar(),
                LivePostAdd = reader.ReadScalar(),
                InsertionSequence = reader.ReadUInt64()
            };
            if (modifier.Handle == 0 || modifier.SourceEffectHandle == 0 || modifier.InsertionSequence == 0 ||
                !Enum.IsDefined(typeof(PortableModifierOperation), modifier.Operation) ||
                !Enum.IsDefined(typeof(PortableClampBound), modifier.ClampBound))
                throw new InvalidDataException("Gameplay Attribute modifier identity is invalid.");
            if (!string.IsNullOrEmpty(modifier.LiveAttributeId))
                modifier.LiveAttributeId = SimulationGameplayEffectProgram.NormalizeAttribute(modifier.LiveAttributeId);
            return modifier;
        }

        static void WriteScalarMap(CanonicalWriter writer, SortedDictionary<string, Float32Scalar> values)
        {
            writer.WriteInt32(values.Count);
            foreach (KeyValuePair<string, Float32Scalar> pair in values)
            {
                writer.WriteString(pair.Key);
                writer.WriteScalar(pair.Value);
            }
        }

        static void ReadScalarMap(
            CanonicalReader reader,
            SortedDictionary<string, Float32Scalar> destination,
            Func<string, string> normalize)
        {
            int count = ReadCount(reader, "scalar map");
            string previous = string.Empty;
            for (int i = 0; i < count; i++)
            {
                string key = normalize(reader.ReadString());
                if (i > 0 && string.CompareOrdinal(previous, key) >= 0)
                    throw new InvalidDataException("Canonical scalar map is not sorted and unique.");
                previous = key;
                destination.Add(key, reader.ReadScalar());
            }
        }

        static void WriteStrings(CanonicalWriter writer, IEnumerable<string> values)
        {
            string[] items = values == null ? Array.Empty<string>() : values.ToArray();
            writer.WriteInt32(items.Length);
            for (int i = 0; i < items.Length; i++)
                writer.WriteString(items[i]);
        }

        static string[] ReadStrings(CanonicalReader reader, Func<string, string> normalize)
        {
            int count = ReadCount(reader, "string array");
            var result = new string[count];
            for (int i = 0; i < count; i++)
            {
                result[i] = normalize(reader.ReadString());
                if (i > 0 && string.CompareOrdinal(result[i - 1], result[i]) >= 0)
                    throw new InvalidDataException("Canonical string array is not sorted and unique.");
            }
            return result;
        }

        static CanonicalReader Reader(byte[] bytes, uint magic, string label)
        {
            var reader = new CanonicalReader(bytes ?? throw new ArgumentNullException(nameof(bytes)));
            if (reader.ReadUInt32() != magic || reader.ReadInt32() != StateVersion)
                throw new InvalidDataException($"{label} state header is invalid.");
            return reader;
        }

        static int ReadCount(CanonicalReader reader, string label)
        {
            int count = reader.ReadInt32();
            if (count < 0 || count > 100000)
                throw new InvalidDataException($"{label} count '{count}' is invalid.");
            return count;
        }

        static int CompareModifier(PortableAttributeModifierState left, PortableAttributeModifierState right)
        {
            int byInsertion = left.InsertionSequence.CompareTo(right.InsertionSequence);
            return byInsertion != 0 ? byInsertion : left.Handle.CompareTo(right.Handle);
        }

        static int CompareActive(PortableActiveEffectState left, PortableActiveEffectState right)
        {
            int byInsertion = left.InsertionSequence.CompareTo(right.InsertionSequence);
            return byInsertion != 0 ? byInsertion : left.Handle.CompareTo(right.Handle);
        }
    }
}
