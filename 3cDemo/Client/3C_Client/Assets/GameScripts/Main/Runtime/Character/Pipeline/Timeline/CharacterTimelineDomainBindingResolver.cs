using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Diagnostics;
using BTSMTL.Timeline;
using BTSMTL.Timeline.Runtime;
using ThirdPersonSimulation;
using ThirdPersonSimulation.Fixed;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonGameplay.Tick;
using TimelinePlaybackStatus = BTSMTL.Timeline.TimelinePlaybackStatus;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.Lifecycle
{
    internal sealed class CharacterTimelineDomainBindingResolver : ITimelineDomainBindingResolver
    {
        readonly HashSet<string> m_SupportedTargets;
        int m_NextHandle = 1;
        internal CharacterTimelineDomainBindingResolver(IEnumerable<string> supportedTargets)
        {
            m_SupportedTargets = new HashSet<string>(supportedTargets ?? Array.Empty<string>(), StringComparer.Ordinal);
        }

        public bool TryResolve(
            TimelineBindingDeclaration declaration,
            TimelineBindingValue callValue,
            out TimelineBindingHandle handle,
            out string error)
        {
            handle = default;
            error = string.Empty;
            if (callValue.ValueKind == TimelineBindingValueKind.Target &&
                !string.IsNullOrEmpty(callValue.TargetIdentity) &&
                !m_SupportedTargets.Contains(callValue.TargetIdentity))
            {
                error = $"Timeline target '{callValue.TargetIdentity}' is not supported by this character.";
                return false;
            }
            handle = new TimelineBindingHandle(m_NextHandle++);
            return true;
        }
    }
}
