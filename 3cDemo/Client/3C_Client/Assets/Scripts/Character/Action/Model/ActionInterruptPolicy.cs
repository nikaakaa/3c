using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ThirdPersonCharacterStateMachine;

namespace ThirdPersonAction
{
    public enum ActionFactSourceKind
    {
        None = 0,
        TimelineWindow = 1,
        Request = 2,
        Runtime = 3,
        Locomotion = 4
    }

    public readonly struct ActionFactDeclaration : IEquatable<ActionFactDeclaration>
    {
        public ActionFactDeclaration(
            TimelineFactId factId,
            ActionFactSourceKind sourceKind,
            bool requestFact)
        {
            FactId = factId;
            SourceKind = sourceKind;
            RequestFact = requestFact;
        }

        public TimelineFactId FactId { get; }
        public ActionFactSourceKind SourceKind { get; }
        public bool RequestFact { get; }
        public bool IsValid => FactId.IsValid && SourceKind != ActionFactSourceKind.None;

        public bool SameMeaning(ActionFactDeclaration other)
        {
            return FactId == other.FactId &&
                   SourceKind == other.SourceKind &&
                   RequestFact == other.RequestFact;
        }

        public bool Equals(ActionFactDeclaration other)
        {
            return SameMeaning(other);
        }

        public override bool Equals(object obj)
        {
            return obj is ActionFactDeclaration other && Equals(other);
        }

        public override int GetHashCode()
        {
            int hash = FactId.GetHashCode();
            hash = (hash * 397) ^ (int)SourceKind;
            hash = (hash * 397) ^ RequestFact.GetHashCode();
            return hash;
        }
    }

    public sealed class ActionFactValidationResult
    {
        readonly List<string> errors = new List<string>();
        readonly List<string> warnings = new List<string>();

        public IReadOnlyList<string> Errors => errors;
        public IReadOnlyList<string> Warnings => warnings;
        public bool HasErrors => errors.Count > 0;

        public void AddError(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
                errors.Add(message);
        }

        public void AddWarning(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
                warnings.Add(message);
        }
    }

    public sealed class ActionFactCompileContext
    {
        readonly ActionFactDeclaration[] declarations;

        public ActionFactCompileContext(ActionFactDeclaration[] declarations, int version = 1)
        {
            this.declarations = declarations ?? Array.Empty<ActionFactDeclaration>();
            Version = version <= 0 ? 1 : version;
        }

        public IReadOnlyList<ActionFactDeclaration> Declarations => declarations ?? Array.Empty<ActionFactDeclaration>();
        public int Version { get; }

        public static ActionFactCompileContext Empty => new ActionFactCompileContext(Array.Empty<ActionFactDeclaration>());

        public static ActionFactCompileContext FromTimeline(ActionTimelineDefinition timeline, int version = 1)
        {
            if (timeline == null || timeline.Tracks.Count == 0)
                return Empty;

            List<ActionFactDeclaration> result = new List<ActionFactDeclaration>();
            for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
            {
                ActionTimelineTrackDefinition track = timeline.Tracks[trackIndex];
                for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                {
                    ActionTimelineClipDefinition clip = track.Clips[clipIndex];
                    if (clip.Kind != ActionTimelineClipKind.HitboxWindow &&
                        clip.Kind != ActionTimelineClipKind.CancelWindow)
                    {
                        continue;
                    }

                    TimelineFactId factId = new TimelineFactId(clip.Payload.FactId);
                    if (!factId.IsValid)
                        continue;

                    result.Add(new ActionFactDeclaration(
                        factId,
                        ActionFactSourceKind.TimelineWindow,
                        clip.Kind == ActionTimelineClipKind.CancelWindow));
                }
            }

            return new ActionFactCompileContext(result.ToArray(), version);
        }
    }

    public readonly struct ActionFactSet
    {
        readonly TimelineFactId[] activeFacts;

        public ActionFactSet(TimelineFactId[] activeFacts)
        {
            this.activeFacts = activeFacts ?? Array.Empty<TimelineFactId>();
        }

        public IReadOnlyList<TimelineFactId> ActiveFacts => activeFacts ?? Array.Empty<TimelineFactId>();

        public bool Contains(TimelineFactId factId)
        {
            if (!factId.IsValid)
                return false;

            for (int i = 0; i < ActiveFacts.Count; i++)
            {
                if (ActiveFacts[i] == factId)
                    return true;
            }

            return false;
        }

        public static ActionFactSet Empty => new ActionFactSet(Array.Empty<TimelineFactId>());

        public static ActionFactSet FromStrings(IReadOnlyList<string> factIds)
        {
            if (factIds == null || factIds.Count == 0)
                return Empty;

            List<TimelineFactId> result = new List<TimelineFactId>();
            for (int i = 0; i < factIds.Count; i++)
            {
                TimelineFactId factId = new TimelineFactId(factIds[i]);
                if (factId.IsValid)
                    result.Add(factId);
            }

            return new ActionFactSet(result.ToArray());
        }
    }

    public static class ActionFactIdResolver
    {
        static readonly Regex validFactId = new Regex("^[A-Za-z][A-Za-z0-9_.-]*$", RegexOptions.Compiled);

        public static ActionFactValidationResult Validate(ActionFactCompileContext context)
        {
            ActionFactValidationResult result = new ActionFactValidationResult();
            Dictionary<TimelineFactId, ActionFactDeclaration> byId = new Dictionary<TimelineFactId, ActionFactDeclaration>();
            IReadOnlyList<ActionFactDeclaration> declarations = context?.Declarations ?? Array.Empty<ActionFactDeclaration>();
            for (int i = 0; i < declarations.Count; i++)
            {
                ActionFactDeclaration declaration = declarations[i];
                if (!declaration.FactId.IsValid)
                {
                    result.AddError($"action fact declaration {i} id is missing.");
                    continue;
                }
                if (!IsValidFactId(declaration.FactId.Value))
                    result.AddError($"action fact declaration {i} id is invalid:{declaration.FactId.Value}.");
                if (declaration.SourceKind == ActionFactSourceKind.None)
                    result.AddError($"action fact declaration {i} source is missing:{declaration.FactId.Value}.");

                if (!byId.TryGetValue(declaration.FactId, out ActionFactDeclaration existing))
                {
                    byId.Add(declaration.FactId, declaration);
                    continue;
                }

                if (existing.SameMeaning(declaration))
                    result.AddWarning($"action fact declaration {i} is duplicate:{declaration.FactId.Value}.");
                else
                    result.AddError($"action fact declaration {i} conflicts:{declaration.FactId.Value}.");
            }

            return result;
        }

        public static bool TryResolve(
            ActionFactCompileContext context,
            TimelineFactId factId,
            out ActionFactDeclaration declaration)
        {
            IReadOnlyList<ActionFactDeclaration> declarations = context?.Declarations ?? Array.Empty<ActionFactDeclaration>();
            for (int i = 0; i < declarations.Count; i++)
            {
                if (declarations[i].FactId == factId)
                {
                    declaration = declarations[i];
                    return declarations[i].IsValid;
                }
            }

            declaration = default;
            return false;
        }

        public static bool IsValidFactId(string factId)
        {
            return !string.IsNullOrWhiteSpace(factId) && validFactId.IsMatch(factId.Trim());
        }
    }

    public enum ActionTransitionResistanceRule
    {
        UseCurrentState = 0
    }

    [Serializable]
    public readonly struct ActionInterruptPolicy
    {
        public ActionInterruptPolicy(
            ActionStateId fromState,
            ActionStateId targetState,
            int minPriority,
            ActionInterruptTimingRule timingRule = ActionInterruptTimingRule.Always,
            float windowStart = 0f,
            float windowEnd = 0f,
            bool force = false,
            string windowId = "",
            string requiredFactId = "",
            ActionRequestType requestType = ActionRequestType.None,
            ActionTransitionResistanceRule resistanceRule = ActionTransitionResistanceRule.UseCurrentState)
        {
            FromState = fromState;
            TargetState = targetState;
            MinPriority = minPriority;
            TimingRule = timingRule;
            WindowStart = windowStart;
            WindowEnd = windowEnd;
            WindowId = windowId ?? string.Empty;
            RequiredFactId = new TimelineFactId(requiredFactId);
            Force = force;
            RequestType = requestType;
            ResistanceRule = resistanceRule;
        }

        public ActionStateId FromState { get; }
        public ActionStateId TargetState { get; }
        public ActionRequestType RequestType { get; }
        public int MinPriority { get; }
        public ActionInterruptTimingRule TimingRule { get; }
        public float WindowStart { get; }
        public float WindowEnd { get; }
        public string WindowId { get; }
        public TimelineFactId RequiredFactId { get; }
        public bool Force { get; }
        public ActionTransitionResistanceRule ResistanceRule { get; }
        public bool RequiresTimelineWindow => !string.IsNullOrWhiteSpace(WindowId);
        public bool RequiresTimelineFact => RequiredFactId.IsValid;

        public bool Matches(ActionInterruptContext context, ActionInterruptRequest request)
        {
            return FromState.Matches(context.CurrentState) &&
                   TargetState.Matches(request.TargetState) &&
                   (RequestType == ActionRequestType.None || RequestType == request.RequestType);
        }
    }
}
