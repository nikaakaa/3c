using System;
using System.Collections.Generic;
using System.IO;

namespace ThirdPersonSimulation
{
    internal static class SimulationProgramSemanticsCodec
    {
        const int MaximumTableCount = 1000000;

        internal static void WriteControlFlow(CanonicalWriter writer, ProgramControlFlowEdge value)
        {
            writer.WriteString(value.Identity);
            writer.WriteInt32(value.Source.Value);
            writer.WriteInt32(value.Target.Value);
            writer.WriteString(value.SourcePort);
            writer.WriteString(value.TargetPort);
            writer.WriteByte((byte)value.Kind);
            writer.WriteInt32(value.Order);
            writer.WriteInt32(value.Priority);
            writer.WriteByte((byte)value.AbortPolicy);
            writer.WriteBoolean(value.HasCondition);
            if (value.HasCondition)
                writer.WriteInt32(value.Condition.Value);
        }

        internal static ProgramControlFlowEdge ReadControlFlow(CanonicalReader reader)
        {
            string identity = reader.ReadString();
            var source = new OperationHandle(reader.ReadInt32());
            var target = new OperationHandle(reader.ReadInt32());
            string sourcePort = reader.ReadString();
            string targetPort = reader.ReadString();
            ProgramControlFlowKind kind = ReadEnum<ProgramControlFlowKind>(reader.ReadByte());
            int order = reader.ReadInt32();
            int priority = reader.ReadInt32();
            ProgramAbortPolicy abortPolicy = ReadEnum<ProgramAbortPolicy>(reader.ReadByte());
            bool hasCondition = reader.ReadBoolean();
            OperationHandle condition = hasCondition ? new OperationHandle(reader.ReadInt32()) : default;
            return new ProgramControlFlowEdge(identity, source, target, sourcePort, targetPort, kind, order, priority, abortPolicy, hasCondition, condition);
        }

        internal static void WriteReference(CanonicalWriter writer, ProgramReference value)
        {
            writer.WriteString(value.Identity);
            writer.WriteBoolean(value.HasSourceOperation);
            if (value.HasSourceOperation)
                writer.WriteInt32(value.SourceOperation.Value);
            writer.WriteByte((byte)value.Kind);
            writer.WriteInt32(value.TargetIndex);
            writer.WriteString(value.ExternalIdentity);
        }

        internal static ProgramReference ReadReference(CanonicalReader reader)
        {
            string identity = reader.ReadString();
            OperationHandle sourceOperation = reader.ReadBoolean() ? new OperationHandle(reader.ReadInt32()) : OperationHandle.Invalid;
            return new ProgramReference(identity, sourceOperation, ReadEnum<ProgramReferenceKind>(reader.ReadByte()), reader.ReadInt32(), reader.ReadString());
        }

        internal static void WriteStateSlot(CanonicalWriter writer, ProgramStateSlot value, bool includeDefault)
        {
            writer.WriteInt32(value.Index);
            writer.WriteString(value.Identity);
            writer.WriteByte((byte)value.ValueKind);
            writer.WriteByte((byte)value.OwnerKind);
            writer.WriteInt32((int)value.Semantic);
            writer.WriteString(value.OwnerIdentity);
            writer.WriteString(value.StateCodecIdentity);
            if (includeDefault)
                writer.WriteInt32(value.DefaultConstantIndex);
        }

        internal static ProgramStateSlot ReadStateSlot(CanonicalReader reader)
        {
            var slot = new ProgramStateSlot(
                reader.ReadInt32(),
                reader.ReadString(),
                ReadEnum<ProgramStateValueKind>(reader.ReadByte()),
                ReadEnum<ProgramStateOwnerKind>(reader.ReadByte()),
                ReadEnum<ProgramStateSemantic>(reader.ReadInt32()),
                reader.ReadString(),
                reader.ReadString(),
                reader.ReadInt32());
            return slot;
        }

        internal static void WriteScope(CanonicalWriter writer, ProgramScopeLayout value)
        {
            writer.WriteInt32(value.CompiledOwnerIndex);
            writer.WriteString(value.Identity);
            writer.WriteByte((byte)value.Kind);
            writer.WriteString(value.OwnerIdentity);
            writer.WriteInt32(value.OwnerOperation.IsValid ? value.OwnerOperation.Value : -1);
            WriteIntArray(writer, value.StateSlots);
        }

        internal static ProgramScopeLayout ReadScope(CanonicalReader reader)
        {
            int compiledOwnerIndex = reader.ReadInt32();
            string identity = reader.ReadString();
            ProgramScopeKind kind = ReadEnum<ProgramScopeKind>(reader.ReadByte());
            string ownerIdentity = reader.ReadString();
            int operation = reader.ReadInt32();
            return new ProgramScopeLayout(
                compiledOwnerIndex,
                identity,
                kind,
                ownerIdentity,
                operation >= 0 ? new OperationHandle(operation) : OperationHandle.Invalid,
                ReadIntArray(reader));
        }

        internal static void WriteWorldRequest(CanonicalWriter writer, ProgramWorldRequestLayout value)
        {
            writer.WriteInt32(value.Index);
            writer.WriteString(value.Identity);
            writer.WriteUInt64((ulong)value.RequiredCapability);
        }

        internal static ProgramWorldRequestLayout ReadWorldRequest(CanonicalReader reader)
        {
            return new ProgramWorldRequestLayout(reader.ReadInt32(), reader.ReadString(), (WorldCapability)reader.ReadUInt64());
        }

        internal static void WriteOutputChannel(CanonicalWriter writer, ProgramOutputChannelLayout value)
        {
            writer.WriteInt32(value.Index);
            writer.WriteString(value.Identity);
            writer.WriteByte((byte)value.Kind);
        }

        internal static ProgramOutputChannelLayout ReadOutputChannel(CanonicalReader reader)
        {
            return new ProgramOutputChannelLayout(reader.ReadInt32(), reader.ReadString(), ReadEnum<ProgramOutputChannelKind>(reader.ReadByte()));
        }

        internal static void WriteCatalogEntry(CanonicalWriter writer, ProgramCatalogEntry value)
        {
            writer.WriteInt32(value.Index);
            writer.WriteByte((byte)value.Kind);
            writer.WriteString(value.Identity);
            writer.WriteInt32(value.Revision);
            writer.WriteInt32(value.Fields.Count);
            for (int i = 0; i < value.Fields.Count; i++)
            {
                ProgramCatalogField field = value.Fields[i];
                writer.WriteString(field.Name);
                writer.WriteByte((byte)field.Kind);
                if (field.Kind == ProgramCatalogFieldKind.Constant)
                    writer.WriteInt32(field.ConstantIndex);
                else
                    writer.WriteString(field.Identity);
            }
        }

        internal static ProgramCatalogEntry ReadCatalogEntry(CanonicalReader reader)
        {
            int index = reader.ReadInt32();
            ProgramCatalogEntryKind kind = ReadEnum<ProgramCatalogEntryKind>(reader.ReadByte());
            string identity = reader.ReadString();
            int revision = reader.ReadInt32();
            int fieldCount = ReadCount(reader);
            var fields = new ProgramCatalogField[fieldCount];
            for (int i = 0; i < fieldCount; i++)
            {
                string name = reader.ReadString();
                ProgramCatalogFieldKind fieldKind = ReadEnum<ProgramCatalogFieldKind>(reader.ReadByte());
                fields[i] = fieldKind == ProgramCatalogFieldKind.Constant
                    ? new ProgramCatalogField(name, fieldKind, reader.ReadInt32(), null)
                    : new ProgramCatalogField(name, fieldKind, -1, reader.ReadString());
            }
            return new ProgramCatalogEntry(index, kind, identity, revision, fields);
        }

        internal static void WriteSourceMap(CanonicalWriter writer, ProgramSourceMapEntry value)
        {
            writer.WriteByte((byte)value.TargetKind);
            writer.WriteInt32(value.TargetIndex);
            writer.WriteString(value.SourceType);
            writer.WriteString(value.GraphId);
            writer.WriteString(value.NodeId);
            writer.WriteString(value.PortId);
            writer.WriteString(value.EdgeId);
            writer.WriteString(value.DeclarationId);
            writer.WriteString(value.TimelineId);
            writer.WriteString(value.TrackId);
            writer.WriteString(value.ClipId);
            writer.WriteString(value.DisplayPath);
            writer.WriteString(value.ContentHash);
        }

        internal static ProgramSourceMapEntry ReadSourceMap(CanonicalReader reader)
        {
            return new ProgramSourceMapEntry(
                ReadEnum<ProgramSourceTargetKind>(reader.ReadByte()),
                reader.ReadInt32(),
                reader.ReadString(),
                reader.ReadString(),
                reader.ReadString(),
                reader.ReadString(),
                reader.ReadString(),
                reader.ReadString(),
                reader.ReadString(),
                reader.ReadString(),
                reader.ReadString(),
                reader.ReadString(),
                reader.ReadString());
        }

        internal static void WriteProducer(CanonicalWriter writer, ProgramProducer value)
        {
            writer.WriteInt32(value.Index);
            writer.WriteString(value.Identity);
            writer.WriteString(value.AnimationChannelId.Value);
            writer.WriteString(value.SourceIdentity);
            writer.WriteByte((byte)value.ChannelKind);
        }

        internal static ProgramProducer ReadProducer(CanonicalReader reader)
        {
            return new ProgramProducer(
                reader.ReadInt32(),
                reader.ReadString(),
                new AnimationChannelId(reader.ReadString()),
                reader.ReadString(),
                ReadEnum<ProgramOutputChannelKind>(reader.ReadByte()));
        }

        internal static void WriteIntArray(CanonicalWriter writer, IReadOnlyList<int> values)
        {
            writer.WriteInt32(values.Count);
            for (int i = 0; i < values.Count; i++)
                writer.WriteInt32(values[i]);
        }

        internal static int[] ReadIntArray(CanonicalReader reader)
        {
            int count = ReadCount(reader);
            var values = new int[count];
            for (int i = 0; i < count; i++)
                values[i] = reader.ReadInt32();
            return values;
        }

        internal static int ReadCount(CanonicalReader reader)
        {
            int count = reader.ReadInt32();
            if (count < 0 || count > MaximumTableCount)
                throw new InvalidDataException($"Canonical table count '{count}' is invalid.");
            return count;
        }

        internal static T ReadEnum<T>(int value) where T : struct
        {
            object candidate = Enum.ToObject(typeof(T), value);
            if (!Enum.IsDefined(typeof(T), candidate))
                throw new InvalidDataException($"Enum value '{value}' is invalid for {typeof(T).Name}.");
            return (T)candidate;
        }
    }
}
