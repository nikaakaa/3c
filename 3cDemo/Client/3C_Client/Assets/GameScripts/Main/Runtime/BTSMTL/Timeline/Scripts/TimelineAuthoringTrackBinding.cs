#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonSimulation;
using UnityEngine;

namespace BTSMTL.Timeline
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
    public sealed class TimelineAuthoringTrackFieldAttribute : Attribute
    {
        public TimelineAuthoringTrackFieldAttribute(
            string fieldId,
            string errorCode,
            string errorMessage)
        {
            FieldId = fieldId ?? string.Empty;
            ErrorCode = errorCode ?? string.Empty;
            ErrorMessage = errorMessage ?? string.Empty;
        }

        public string FieldId { get; }
        public string ErrorCode { get; }
        public string ErrorMessage { get; }
    }

    public readonly struct TimelineAuthoringTrackExport
    {
        public TimelineAuthoringTrackExport(string animationChannelId, string animationSlotId)
        {
            AnimationChannelId = animationChannelId ?? string.Empty;
            AnimationSlotId = animationSlotId ?? string.Empty;
        }

        public string AnimationChannelId { get; }
        public string AnimationSlotId { get; }
    }

    public readonly struct TimelineAuthoringTrackIssue
    {
        public TimelineAuthoringTrackIssue(string fieldId, string errorCode, string errorMessage)
        {
            FieldId = fieldId ?? string.Empty;
            ErrorCode = errorCode ?? string.Empty;
            ErrorMessage = errorMessage ?? string.Empty;
        }

        public string FieldId { get; }
        public string ErrorCode { get; }
        public string ErrorMessage { get; }
    }

    public static class TimelineAuthoringTrackBinding
    {
        public static Clip CreateClip(
            TimelineData timeline,
            TimelineContractCatalog catalog,
            Track track,
            UnityEngine.AnimationClip animationAsset,
            int startFrame)
        {
            if (track is AnimationTrack animation)
                return timeline.AddClip(catalog, animationAsset, animation, startFrame);
            return timeline.AddClip(catalog, track, startFrame);
        }

        public static IReadOnlyList<TimelineAuthoringTrackIssue> Validate(
            string kind,
            string animationChannelId,
            string animationSlotId)
        {
            Type trackType = TimelineAuthoringTypeCatalog.RequireTrackType(kind);
            var values = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["animationChannelId"] = animationChannelId,
                ["animationSlotId"] = animationSlotId
            };
            return trackType
                .GetCustomAttributes(typeof(TimelineAuthoringTrackFieldAttribute), true)
                .OfType<TimelineAuthoringTrackFieldAttribute>()
                .Where(field => !IsIdentity(values[field.FieldId]))
                .Select(field => new TimelineAuthoringTrackIssue(
                    field.FieldId,
                    field.ErrorCode,
                    field.ErrorMessage))
                .ToArray();
        }

        public static void Apply(
            Track track,
            string animationChannelId,
            string animationSlotId)
        {
            if (track is not AnimationTrack animation)
                return;
            animation.SetAnimationChannelId(new AnimationChannelId(animationChannelId));
            animation.SetAnimationSlotId(animationSlotId);
        }

        public static TimelineAuthoringTrackExport Export(Track track) =>
            track is AnimationTrack animation
                ? new TimelineAuthoringTrackExport(
                    animation.AnimationChannelId.Value,
                    animation.AnimationSlotId)
                : new TimelineAuthoringTrackExport(string.Empty, string.Empty);

        static bool IsIdentity(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Any(char.IsWhiteSpace) ||
                value.StartsWith("@", StringComparison.Ordinal))
                return false;
            if (!value.StartsWith("local:", StringComparison.Ordinal))
                return true;
            string local = value.Substring("local:".Length);
            return local.Length > 0 && local.All(character =>
                char.IsLetterOrDigit(character) || character == '-' || character == '_' || character == '.');
        }
    }
}
#endif
