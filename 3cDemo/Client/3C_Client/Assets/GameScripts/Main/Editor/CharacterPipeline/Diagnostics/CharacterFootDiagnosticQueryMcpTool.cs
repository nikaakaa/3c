using System;
using System.IO;
using System.Linq;
using System.Text;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    [McpForUnityTool("character.foot_diagnostics",
        Description = "Read sealed Foot diagnostic summaries, indexed events, or exact source frames without replaying or analyzing again.",
        StructuredOutput = true, AutoRegister = true, RequiresPolling = false,
        HasBehaviorAnnotations = true, ReadOnlyHint = true, DestructiveHint = false,
        IdempotentHint = true, OpenWorldHint = false)]
    public static class CharacterFootDiagnosticQueryMcpTool
    {
        public sealed class Parameters
        {
            [ToolParameter("Action: summary, events, detail, or frame. Defaults to summary.", Required = false)]
            public string action { get; set; }
            [ToolParameter("Exact path to the sealed diagnoses/analysis.json.", Required = true)]
            public string analysis_path { get; set; }
            [ToolParameter("Target id for events.", Required = false)]
            public string target_id { get; set; }
            [ToolParameter("Record id from a representative or events index.", Required = false)]
            public int? detail_id { get; set; }
            [ToolParameter("FrameSequence for frame.", Required = false)]
            public int? frame { get; set; }
            [ToolParameter("Left or Right for frame.", Required = false)]
            public string side { get; set; }
            [ToolParameter("Source family: samples or geometry. Defaults to samples.", Required = false)]
            public string family { get; set; }
            [ToolParameter("Exact source columns to return; omitted returns all.", Required = false)]
            public string[] columns { get; set; }
            [ToolParameter("events selection: matched or eligible. Defaults to matched.", Required = false)]
            public string selection { get; set; }
            [ToolParameter("Zero-based events offset.", Required = false)]
            public int? skip { get; set; }
            [ToolParameter("events page size, 1 through 100. Defaults to 20.", Required = false)]
            public int? take { get; set; }
        }

        public static object HandleCommand(JObject parameters)
        {
            try
            {
                string path = parameters?.Value<string>("analysis_path");
                if (string.IsNullOrWhiteSpace(path))
                    throw new InvalidDataException("analysis_path is required.");
                string action = parameters.Value<string>("action") ?? "summary";
                if (action == "summary")
                {
                    CharacterFootDiagnosticManifest manifest = CharacterFootDiagnosticStore.ReadManifest(path);
                    JObject score = JObject.Parse(File.ReadAllText(Path.Combine(
                        Path.GetDirectoryName(Path.GetFullPath(path)), "quality-score.json"), Encoding.UTF8));
                    if (score.Value<string>("schema") != "character-foot-quality-score/3" ||
                        score["facts"]?.Value<string>("indexSha256") != manifest.index.sha256 ||
                        score["facts"]?.Value<string>("sampleIdentity") != manifest.sample.Value<string>("identity"))
                        throw new InvalidDataException("Foot quality report belongs to another analysis.");
                    return new SuccessResponse("Foot diagnostic summary loaded.", score);
                }
                var reader = CharacterFootDiagnosticStore.Open(path);
                if (action == "detail")
                {
                    int id = parameters.Value<int?>("detail_id") ??
                        throw new InvalidDataException("detail_id is required.");
                    return new SuccessResponse("Foot diagnostic detail loaded.", reader.Read(id));
                }
                if (action == "events")
                {
                    string targetId = parameters.Value<string>("target_id");
                    CharacterFootDiagnosticTargetIndex target = reader.Targets.SingleOrDefault(value => value.id == targetId) ??
                        throw new InvalidDataException("target_id is unavailable.");
                    string selection = parameters.Value<string>("selection") ?? "matched";
                    if (selection != "matched" && selection != "eligible")
                        throw new InvalidDataException("selection must be matched or eligible.");
                    int skip = parameters.Value<int?>("skip") ?? 0;
                    int take = parameters.Value<int?>("take") ?? 20;
                    if (skip < 0 || take < 1 || take > 100)
                        throw new InvalidDataException("Event page range is invalid.");
                    var ids = selection == "matched" ? target.matched : target.eligible;
                    return new SuccessResponse("Foot diagnostic event page loaded.", new
                    {
                        targetId,
                        eligibleEventCount = target.eligible.Count,
                        matchedEventCount = target.matched.Count,
                        selection,
                        total = ids.Count,
                        skip,
                        records = ids.Skip(skip).Take(take).Select(id => reader.Records[id - 1]).ToArray()
                    });
                }
                if (action == "frame")
                {
                    int frame = parameters.Value<int?>("frame") ?? throw new InvalidDataException("frame is required.");
                    string side = parameters.Value<string>("side");
                    string family = parameters.Value<string>("family") ?? "samples";
                    if (side != "Left" && side != "Right" || family != "samples" && family != "geometry")
                        throw new InvalidDataException("Frame Side or source family is invalid.");
                    var columns = reader.SourceColumns(family);
                    var selected = parameters["columns"] is JArray requested
                        ? requested.Values<string>().ToArray() : columns.ToArray();
                    if (selected.Distinct(StringComparer.Ordinal).Count() != selected.Length ||
                        selected.Any(value => !columns.Contains(value)))
                        throw new InvalidDataException("Requested source columns are invalid.");
                    string[] allColumns = columns.ToArray();
                    int[] indices = selected.Select(value => Array.IndexOf(allColumns, value)).ToArray();
                    return new SuccessResponse("Foot diagnostic source frame loaded.", new
                    {
                        frame,
                        side,
                        family,
                        columns = selected,
                        rows = reader.ReadSource(family, frame, side)
                            .Select(row => indices.Select(index => row[index]).ToArray()).ToArray(),
                        relatedDetails = reader.Records.Where(value => value.startFrame <= frame &&
                            value.endFrame >= frame && (value.side == side || value.family == "pelvisFrames")).ToArray()
                    });
                }
                throw new InvalidDataException("Unsupported Foot diagnostic read action.");
            }
            catch (Exception exception)
            {
                return new ErrorResponse(exception.GetType().Name + ": " + exception.Message);
            }
        }
    }
}
