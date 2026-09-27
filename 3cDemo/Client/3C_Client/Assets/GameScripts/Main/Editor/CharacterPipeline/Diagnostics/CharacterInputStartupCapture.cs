using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using ThirdPersonCharacter.Pipeline.Simulation.Fixed;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Presentation;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    [InitializeOnLoad]
    internal static class CharacterInputStartupCapture
    {
        const string StateKey = "ThirdPerson.CharacterInputTrace.Startup";
        static Document s_Document;
        static CharacterInputStartupCapture()
        {
            FixedCharacterHost.StartupMilestone += OnCharacterMilestone;
            SimulationSessionHost.StartupMilestone += OnSessionMilestone;
            CharacterPresentationDomainRuntimeFactory.StartupMilestone += OnPresentationMilestone;
            CharacterPoseNativeDomainRuntimeFactory.StartupMilestone += OnPresentationMilestone;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }
        internal static string Path => Read()?.path ?? string.Empty;

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode && !CharacterFixedInputTraceWorkflow.IsPending)
                Begin("manual-play", string.Empty, string.Empty);
            else if (state == PlayModeStateChange.EnteredPlayMode)
                Mark("Running");
        }

        internal static void Begin(string operation, string traceId, string actorId)
        {
            var document = new Document
            {
                operation = operation,
                trace_id = traceId,
                actor_id = actorId,
                started_utc = DateTime.UtcNow.ToString("O"),
                started_timestamp = Stopwatch.GetTimestamp(),
                path = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..",
                    "Diagnostics", "CharacterRuntimeTraces", "Startup", Guid.NewGuid().ToString("N") + ".json"))
            };
            Save(document);
            Mark("requested");
        }

        internal static void Mark(string phase)
            => MarkAt(phase, Stopwatch.GetTimestamp());

        static void MarkAt(string phase, long timestamp)
        {
            var document = Read();
            if (document == null || document.phases.Exists(value => value.phase == phase))
                return;
            document.phases.Add(new Phase
            {
                phase = phase,
                elapsed_milliseconds = (timestamp - document.started_timestamp) * 1000d / Stopwatch.Frequency
            });
            Save(document);
        }

        static void OnCharacterMilestone(string actorId, string sessionId, string phase, long timestamp)
        {
            var document = Read();
            if (document == null)
                return;
            if (!string.IsNullOrEmpty(document.actor_id) &&
                !string.Equals(document.actor_id, actorId, StringComparison.Ordinal))
            {
                MarkAt(actorId + "/" + phase, timestamp);
                return;
            }
            if (string.IsNullOrEmpty(document.actor_id))
            {
                document.actor_id = actorId;
                Save(document);
            }
            if (string.IsNullOrEmpty(document.session_id))
            {
                document.session_id = sessionId;
                Save(document);
            }
            MarkAt(phase, timestamp);
        }

        static void OnSessionMilestone(string sessionId, string phase, long timestamp)
        {
            var document = Read();
            if (document != null && string.Equals(document.session_id, sessionId, StringComparison.Ordinal))
                MarkAt(phase, timestamp);
        }

        static void OnPresentationMilestone(string actorId, string phase, long timestamp)
        {
            var document = Read();
            if (document == null)
                return;
            MarkAt(string.Equals(document.actor_id, actorId, StringComparison.Ordinal)
                ? phase
                : actorId + "/" + phase, timestamp);
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
            public string actor_id;
            public string session_id;
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
