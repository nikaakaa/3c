using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal sealed class CharacterFootDiagnosticRecord
    {
        public int id;
        public string family;
        public JObject data;
    }

    internal sealed class CharacterFootDiagnosticRecordIndex
    {
        public int id;
        public string family;
        public string kind;
        public string side;
        public int startFrame;
        public int endFrame;
        public long offset;
        public int length;
        public string sha256;
    }

    internal sealed class CharacterFootDiagnosticTargetIndex
    {
        public string id;
        public List<int> eligible;
        public List<int> matched;
    }

    internal sealed class CharacterFootDiagnosticDetailIndex
    {
        public string schema = CharacterFootDiagnosticStore.IndexSchema;
        public string sampleIdentity;
        public List<CharacterFootDiagnosticRecordIndex> records;
        public List<CharacterFootDiagnosticTargetIndex> targets;
        public List<CharacterFootDiagnosticSourceIndex> sources;
    }

    internal sealed class CharacterFootDiagnosticArtifact
    {
        public string file;
        public string sha256;
        public long bytes;
    }

    internal sealed class CharacterFootDiagnosticPerformance
    {
        public double readAndValidateMilliseconds;
        public double analyzeMilliseconds;
        public double publishMilliseconds;
        public long detailBytes;
        public long indexBytes;
        public long reportBytes;
    }

    internal sealed class CharacterFootDiagnosticManifest
    {
        public string schema = CharacterFootDiagnosticStore.ManifestSchema;
        public string factsSchema;
        public JObject sample;
        public JObject analyzer;
        public JObject coverage;
        public CharacterFootDiagnosticArtifact details;
        public CharacterFootDiagnosticArtifact index;
        public CharacterFootDiagnosticPerformance performance;
    }

    internal sealed class CharacterFootDiagnosticStore : IDisposable
    {
        internal const string ManifestSchema = "character-foot-diagnostic-analysis/1";
        internal const string IndexSchema = "character-foot-diagnostic-index/1";
        internal const string ManifestFileName = "analysis.json";
        internal const string DetailFileName = "details.jsonl";
        internal const string IndexFileName = "details-index.json";
        static readonly UTF8Encoding s_Utf8 = new UTF8Encoding(false, true);
        readonly FileStream m_Stream;
        readonly List<CharacterFootDiagnosticRecordIndex> m_Records =
            new List<CharacterFootDiagnosticRecordIndex>();
        readonly string m_Directory;
        readonly string m_SampleIdentity;

        internal CharacterFootDiagnosticStore(string directory, string sampleIdentity)
        {
            if (string.IsNullOrWhiteSpace(sampleIdentity))
                throw new InvalidDataException("Foot diagnostic sample identity is missing.");
            m_Directory = directory;
            m_SampleIdentity = sampleIdentity;
            m_Stream = new FileStream(Path.Combine(directory, DetailFileName),
                FileMode.CreateNew, FileAccess.Write, FileShare.None, 65536,
                FileOptions.SequentialScan);
        }

        internal int Add(string family, JObject data)
        {
            if (string.IsNullOrWhiteSpace(family) || data == null)
                throw new InvalidDataException("Foot diagnostic record is invalid.");
            int id = m_Records.Count + 1;
            var record = new CharacterFootDiagnosticRecord
            {
                id = id,
                family = family,
                data = data
            };
            byte[] bytes = s_Utf8.GetBytes(JsonConvert.SerializeObject(record,
                Formatting.None, SerializerSettings()));
            m_Records.Add(new CharacterFootDiagnosticRecordIndex
            {
                id = id,
                family = family,
                kind = data.Value<string>("kind"),
                side = data.Value<string>("side"),
                startFrame = data.Value<int?>("startFrame") ?? data.Value<int?>("frame") ?? 0,
                endFrame = data.Value<int?>("endFrame") ?? data.Value<int?>("frame") ?? 0,
                offset = m_Stream.Position,
                length = bytes.Length,
                sha256 = Hash(bytes)
            });
            m_Stream.Write(bytes, 0, bytes.Length);
            m_Stream.WriteByte((byte)'\n');
            return id;
        }

        internal void Complete(IEnumerable<CharacterFootDiagnosticTargetIndex> targets,
            IReadOnlyList<CharacterFootDiagnosticSourceIndex> sources)
        {
            m_Stream.Flush(true);
            m_Stream.Dispose();
            WriteJson(Path.Combine(m_Directory, IndexFileName),
                new CharacterFootDiagnosticDetailIndex
                {
                    sampleIdentity = m_SampleIdentity,
                    records = m_Records,
                    targets = targets.OrderBy(value => value.id, StringComparer.Ordinal).ToList(),
                    sources = sources.ToList()
                }, false);
        }

        public void Dispose() => m_Stream.Dispose();

        internal static void WriteJson(string path, object document, bool indented)
        {
            using var stream = new FileStream(path, FileMode.CreateNew,
                FileAccess.Write, FileShare.Read, 65536, FileOptions.SequentialScan);
            using var writer = new StreamWriter(stream, s_Utf8);
            using var json = new JsonTextWriter(writer)
            {
                Formatting = indented ? Formatting.Indented : Formatting.None,
                Culture = CultureInfo.InvariantCulture
            };
            JsonSerializer.Create(SerializerSettings()).Serialize(json, document);
            json.Flush();
            writer.Flush();
            stream.Flush(true);
        }

        internal static JsonSerializerSettings SerializerSettings() =>
            new JsonSerializerSettings
            {
                Culture = CultureInfo.InvariantCulture,
                NullValueHandling = NullValueHandling.Ignore
            };

        internal static CharacterFootDiagnosticArtifact Artifact(string directory, string name)
        {
            string path = Path.Combine(directory, name);
            return new CharacterFootDiagnosticArtifact
            {
                file = name,
                sha256 = HashFile(path),
                bytes = new FileInfo(path).Length
            };
        }

        internal static string HashFile(string path)
        {
            using SHA256 sha = SHA256.Create();
            using FileStream stream = File.OpenRead(path);
            return Hex(sha.ComputeHash(stream));
        }

        static string Hash(byte[] bytes)
        {
            using SHA256 sha = SHA256.Create();
            return Hex(sha.ComputeHash(bytes));
        }

        static string Hex(byte[] bytes) => string.Concat(bytes.Select(
            value => value.ToString("x2", CultureInfo.InvariantCulture)));

        internal static CharacterFootDiagnosticReader Open(string manifestPath) =>
            new CharacterFootDiagnosticReader(manifestPath);

        internal static CharacterFootDiagnosticManifest ReadManifest(string manifestPath)
        {
            if (!string.Equals(Path.GetFileName(manifestPath), ManifestFileName, StringComparison.Ordinal))
                throw new InvalidDataException("Foot diagnostic input must be analysis.json.");
            CharacterFootDiagnosticManifest manifest =
                JsonConvert.DeserializeObject<CharacterFootDiagnosticManifest>(File.ReadAllText(manifestPath, s_Utf8));
            if (manifest == null || manifest.schema != ManifestSchema ||
                manifest.factsSchema != "character-foot-motion-facts/67" ||
                manifest.sample == null || manifest.index == null || manifest.details == null ||
                manifest.index.file != IndexFileName || manifest.details.file != DetailFileName)
                throw new InvalidDataException("Foot diagnostic manifest contract is invalid.");
            return manifest;
        }

        internal sealed class CharacterFootDiagnosticReader
        {
            readonly string m_Directory;
            readonly CharacterFootDiagnosticManifest m_Manifest;
            readonly CharacterFootDiagnosticDetailIndex m_Index;

            internal CharacterFootDiagnosticReader(string manifestPath)
            {
                m_Directory = Path.GetDirectoryName(Path.GetFullPath(manifestPath));
                m_Manifest = ReadManifest(manifestPath);
                string indexPath = Path.Combine(m_Directory, IndexFileName);
                if (HashFile(indexPath) != m_Manifest.index.sha256 ||
                    new FileInfo(indexPath).Length != m_Manifest.index.bytes ||
                    new FileInfo(Path.Combine(m_Directory, DetailFileName)).Length !=
                        m_Manifest.details.bytes)
                    throw new InvalidDataException("Foot diagnostic artifact identity is invalid.");
                m_Index = JsonConvert.DeserializeObject<CharacterFootDiagnosticDetailIndex>(
                    File.ReadAllText(indexPath, s_Utf8));
                if (m_Index == null || m_Index.schema != IndexSchema || m_Index.records == null ||
                    m_Index.targets == null || m_Index.sources == null || m_Index.sampleIdentity !=
                        m_Manifest.sample.Value<string>("identity"))
                    throw new InvalidDataException("Foot diagnostic index contract is invalid.");
                long offset = 0;
                for (int i = 0; i < m_Index.records.Count; i++)
                {
                    CharacterFootDiagnosticRecordIndex entry = m_Index.records[i];
                    if (entry.id != i + 1 || entry.offset != offset || entry.length <= 0 ||
                        entry.offset > m_Manifest.details.bytes - entry.length - 1)
                        throw new InvalidDataException("Foot diagnostic index range is invalid.");
                    offset += entry.length + 1L;
                }
                if (offset != m_Manifest.details.bytes)
                    throw new InvalidDataException("Foot diagnostic index does not cover details.");
                var targetIds = new HashSet<string>(StringComparer.Ordinal);
                foreach (CharacterFootDiagnosticTargetIndex target in m_Index.targets)
                {
                    if (!targetIds.Add(target.id) || target.eligible == null || target.matched == null ||
                        target.eligible.Any(value => value < 1 || value > m_Index.records.Count))
                        throw new InvalidDataException("Foot diagnostic target index is invalid.");
                    var eligible = new HashSet<int>(target.eligible);
                    if (eligible.Count != target.eligible.Count ||
                        target.matched.Distinct().Count() != target.matched.Count ||
                        target.matched.Any(value => !eligible.Contains(value)))
                        throw new InvalidDataException("Foot diagnostic target membership is invalid.");
                }
            }

            internal IReadOnlyList<CharacterFootDiagnosticRecordIndex> Records => m_Index.records;
            internal IReadOnlyList<CharacterFootDiagnosticTargetIndex> Targets => m_Index.targets;

            internal IReadOnlyList<string[]> ReadSource(string family, int frame, string side)
            {
                CharacterFootDiagnosticSourceIndex source = m_Index.sources.Single(value => value.family == family);
                string path = Path.GetFullPath(Path.Combine(m_Directory, source.file));
                if (new FileInfo(path).Length != source.bytes)
                    throw new InvalidDataException("Foot diagnostic source length has changed.");
                var result = new List<string[]>();
                using var stream = File.OpenRead(path);
                foreach (CharacterFootDiagnosticSourceRange range in source.ranges.Where(
                             value => value.frame == frame && value.side == side))
                {
                    if (range.offset < 0 || range.length <= 0 || range.offset > source.bytes - range.length)
                        throw new InvalidDataException("Foot diagnostic source range is invalid.");
                    var bytes = new byte[range.length];
                    stream.Position = range.offset;
                    int read = 0;
                    while (read < bytes.Length)
                    {
                        int count = stream.Read(bytes, read, bytes.Length - read);
                        if (count == 0) throw new EndOfStreamException("Foot diagnostic source is truncated.");
                        read += count;
                    }
                    if (Hash(bytes) != range.sha256)
                        throw new InvalidDataException("Foot diagnostic source range checksum is invalid.");
                    using var lines = new StringReader(s_Utf8.GetString(bytes));
                    string line;
                    while ((line = lines.ReadLine()) != null)
                    {
                        string[] cells = CharacterFootMotionDiagnosticAnalyzer.ParseCsvLine(line);
                        if (cells.Length != source.columns.Length)
                            throw new InvalidDataException("Foot diagnostic source row width is invalid.");
                        result.Add(cells);
                    }
                }
                if (family == "samples" && result.Count != 1)
                    throw new InvalidDataException("Foot diagnostic sample frame is unavailable or duplicated.");
                return result;
            }

            internal IReadOnlyList<string> SourceColumns(string family) =>
                m_Index.sources.Single(value => value.family == family).columns;

            internal JObject Read(int id)
            {
                if (id < 1 || id > m_Index.records.Count)
                    throw new InvalidDataException("Foot diagnostic record id is unavailable.");
                CharacterFootDiagnosticRecordIndex entry = m_Index.records[id - 1];
                var bytes = new byte[entry.length];
                using var stream = File.OpenRead(Path.Combine(m_Directory, DetailFileName));
                stream.Position = entry.offset;
                int read = 0;
                while (read < bytes.Length)
                {
                    int count = stream.Read(bytes, read, bytes.Length - read);
                    if (count == 0)
                        throw new EndOfStreamException("Foot diagnostic record is truncated.");
                    read += count;
                }
                if (stream.ReadByte() != '\n' || Hash(bytes) != entry.sha256)
                    throw new InvalidDataException("Foot diagnostic record checksum is invalid.");
                JObject record = JObject.Parse(s_Utf8.GetString(bytes));
                if (record.Value<int>("id") != id ||
                    record.Value<string>("family") != entry.family ||
                    !(record["data"] is JObject data))
                    throw new InvalidDataException("Foot diagnostic record identity is invalid.");
                return data;
            }
        }
    }
}
