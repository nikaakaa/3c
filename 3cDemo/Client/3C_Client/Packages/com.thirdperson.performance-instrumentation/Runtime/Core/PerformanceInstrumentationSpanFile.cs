using System;
using System.IO;
using System.Text;

namespace ThirdPersonPerformance.Instrumentation
{
    public static class PerformanceInstrumentationSpanFile
    {
        public const string FileSchema = PerformanceInstrumentationIdentity.SpanFileSchema;

        public static void Write(string path, PerformanceSpanRecord[] records)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Performance instrumentation span path is missing.", nameof(path));
            if (records == null)
                throw new ArgumentNullException(nameof(records));
            string fullPath = Path.GetFullPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            if (File.Exists(fullPath))
                throw new IOException($"Performance instrumentation span file already exists: {fullPath}");
            string temporaryPath = fullPath + ".tmp";
            using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new BinaryWriter(stream, new UTF8Encoding(false), false))
            {
                writer.Write(FileSchema);
                writer.Write(PerformanceInstrumentationIdentity.SpanLayoutRevision);
                writer.Write(System.Diagnostics.Stopwatch.Frequency);
                writer.Write(records.Length);
                for (int i = 0; i < records.Length; i++)
                    WriteRecord(writer, records[i]);
            }
            File.Move(temporaryPath, fullPath);
        }

        static void WriteRecord(BinaryWriter writer, PerformanceSpanRecord record)
        {
            writer.Write(record.Sequence);
            writer.Write(record.PointId);
            writer.Write(record.MetricId);
            writer.Write(record.RenderFrame);
            writer.Write(record.LogicTick);
            writer.Write(record.ActorId);
            writer.Write(record.ProgramIdentity);
            writer.Write(record.PipelineIdentity);
            writer.Write(record.StartedAt);
            writer.Write(record.DurationTicks);
            writer.Write(record.ThreadId);
            writer.Write((byte)record.ContextFlags);
            writer.Write((byte)record.EndState);
        }
    }
}
