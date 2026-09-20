using ThirdPersonSimulation.Fixed;

namespace ThirdPersonSimulation
{
    public static class AbilityTimelineProgressCodec
    {
        public static void Write(CanonicalWriter writer, in AbilityTimelineProgress progress)
        {
            if (!progress.IsValid)
                throw new System.ArgumentException("Timeline progress is invalid.", nameof(progress));
            writer.WriteString(progress.TimelineId);
            writer.WriteString(progress.ContentRevision);
            writer.WriteUInt64(progress.Generation);
            writer.WriteUInt64(progress.LogicTick);
            writer.WriteInt64(progress.Duration.Raw);
            writer.WriteInt64(progress.PreviousTime.Raw);
            writer.WriteInt64(progress.Time.Raw);
            writer.WriteInt32(progress.PreviousCycle);
            writer.WriteInt32(progress.Cycle);
            writer.WriteBoolean(progress.Loop);
            writer.WriteByte((byte)progress.State);
        }

        public static AbilityTimelineProgress Read(CanonicalReader reader) => new AbilityTimelineProgress(
            reader.ReadString(),
            reader.ReadString(),
            reader.ReadUInt64(),
            reader.ReadUInt64(),
            FixedScalar.FromRaw(reader.ReadInt64()),
            FixedScalar.FromRaw(reader.ReadInt64()),
            FixedScalar.FromRaw(reader.ReadInt64()),
            reader.ReadInt32(),
            reader.ReadInt32(),
            reader.ReadBoolean(),
            (AbilityTimelineProgressState)reader.ReadByte());
    }
}
