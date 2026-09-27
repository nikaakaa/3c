using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal static class CharacterInputStartupCapture
    {
        const string StateKey = "ThirdPerson.CharacterInputTrace.Startup";
        static Document s_Document;
        internal static string Path => Read()?.path ?? string.Empty;

        internal static void Begin(string operation, string traceId)
        {
            var document = new Document
            {
                operation = operation,
                trace_id = traceId,
                started_utc = DateTime.UtcNow.ToString("O"),
                started_timestamp = Stopwatch.GetTimestamp(),
                path = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..",
                    "Diagnostics", "CharacterRuntimeTraces", "Startup", Guid.NewGuid().ToString("N") + ".json"))
            };
            Save(document);
            Mark("requested");
        }

        internal static void Mark(string phase)
        {
            var document = Read();
            if (document == null || document.phases.Exists(value => value.phase == phase))
                return;
            document.phases.Add(new Phase
            {
                phase = phase,
                elapsed_milliseconds = (Stopwatch.GetTimestamp() - document.started_timestamp) * 1000d / Stopwatch.Frequency
            });
            Save(document);
        }

        internal static void BindTrace(string traceId)
        {
            var document = Read();
            if (document == null)
                return;
            document.trace_id = traceId;
            Save(document);
        }

        internal static void Fail(string failure)
        {
            var document = Read();
            if (document == null)
                return;
            document.failure = failure;
            Save(document);
        }

        static Document Read()
        {
            if (s_Document != null)
                return s_Document;
            string json = SessionState.GetString(StateKey, string.Empty);
            return s_Document = string.IsNullOrEmpty(json) ? null : JsonConvert.DeserializeObject<Document>(json);
        }

        static void Save(Document document)
        {
            s_Document = document;
            string json = JsonConvert.SerializeObject(document, Formatting.Indented);
            SessionState.SetString(StateKey, json);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(document.path));
            File.WriteAllText(document.path, json, new UTF8Encoding(false));
        }

        sealed class Document
        {
            public string schema = "character-input-startup/1";
            public string operation;
            public string trace_id;
            public string started_utc;
            public long started_timestamp;
            public string path;
            public string failure = string.Empty;
            public List<Phase> phases = new List<Phase>();
        }

        sealed class Phase
        {
            public string phase;
            public double elapsed_milliseconds;
        }
    }
}
