using ThirdPersonCharacter.ActionSystem;
using ThirdPersonGameplay.Contracts;

namespace ThirdPersonGameplay.Networking.ServerAuthoritativeHybrid
{
    public readonly struct ServerAuthoritativePolicyResolution
    {
        public ServerAuthoritativePolicyResolution(
            string behaviorId,
            GameplayBehaviorKind behaviorKind,
            bool isConfigured,
            bool shouldSend,
            ServerAuthoritativeDomain domain,
            ServerAuthoritativePacketKind packetKind,
            string policyId,
            string reason,
            string summary)
        {
            BehaviorId = behaviorId ?? string.Empty;
            BehaviorKind = behaviorKind;
            IsConfigured = isConfigured;
            ShouldSend = shouldSend;
            Domain = domain;
            PacketKind = packetKind;
            PolicyId = policyId ?? string.Empty;
            Reason = reason ?? string.Empty;
            Summary = summary ?? string.Empty;
        }

        public string BehaviorId { get; }
        public GameplayBehaviorKind BehaviorKind { get; }
        public bool IsConfigured { get; }
        public bool ShouldSend { get; }
        public ServerAuthoritativeDomain Domain { get; }
        public ServerAuthoritativePacketKind PacketKind { get; }
        public string PolicyId { get; }
        public string Reason { get; }
        public string Summary { get; }
    }

    public sealed class ServerAuthoritativeBehaviorPolicyResolver
    {
        readonly ServerAuthoritativeCharacterSyncProfile m_Profile;

        public ServerAuthoritativeBehaviorPolicyResolver(ServerAuthoritativeCharacterSyncProfile profile)
        {
            m_Profile = profile;
        }

        public ServerAuthoritativePolicyResolution ResolveFact(ServerAuthoritativeFactKind factKind)
        {
            if (!m_Profile || !m_Profile.TryGetFactPolicy(factKind, out ServerAuthoritativeBehaviorPolicy policy))
                return Missing(factKind.ToString(), PacketKind(factKind));

            bool shouldSend;
            switch (factKind)
            {
                case ServerAuthoritativeFactKind.MotionCommand:
                    shouldSend = policy.CommandSendPolicy != ServerAuthoritativeCommandSendPolicy.None &&
                                 policy.PredictionPolicy != ServerAuthoritativePredictionPolicy.None &&
                                 policy.ReplicationPolicy != ServerAuthoritativeReplicationPolicy.None;
                    break;
                case ServerAuthoritativeFactKind.MotionCorrectionAcknowledgement:
                case ServerAuthoritativeFactKind.GameplayAttributeValue:
                    shouldSend = policy.AuthorityPolicy != ServerAuthoritativeAuthorityPolicy.LocalOnly &&
                                 policy.ReplicationPolicy != ServerAuthoritativeReplicationPolicy.None;
                    break;
                default:
                    return Missing(factKind.ToString(), ServerAuthoritativePacketKind.None);
            }

            return Configured(
                policy,
                BehaviorKind(policy.TargetDomain),
                PacketKind(factKind),
                factKind.ToString(),
                shouldSend,
                $"{policy.PredictionPolicy}, {policy.AuthorityPolicy}, {policy.ReplicationPolicy}, {policy.HistoryPolicy}");
        }

        public ServerAuthoritativePolicyResolution ResolveEvent(
            string behaviorId,
            ServerAuthoritativePacketKind packetKind)
        {
            if (!m_Profile || !m_Profile.TryGetBehaviorPolicy(behaviorId, out ServerAuthoritativeBehaviorPolicy policy))
                return Missing(behaviorId, packetKind);

            bool domainMatches = (packetKind == ServerAuthoritativePacketKind.GameplayResult &&
                                  policy.TargetDomain == ServerAuthoritativeDomain.GameplayResult) ||
                                 (packetKind == ServerAuthoritativePacketKind.GameplayCue &&
                                  (policy.TargetDomain == ServerAuthoritativeDomain.Presentation ||
                                   policy.TargetDomain == ServerAuthoritativeDomain.GameplayEffect));
            if (!domainMatches)
                return Missing($"{behaviorId}:DomainMismatch", packetKind);

            bool shouldSend = policy.AuthorityPolicy != ServerAuthoritativeAuthorityPolicy.LocalOnly &&
                              policy.ReplicationPolicy != ServerAuthoritativeReplicationPolicy.None;
            return Configured(
                policy,
                GameplayBehaviorKind.Event,
                packetKind,
                packetKind.ToString(),
                shouldSend,
                $"{policy.AuthorityPolicy}, {policy.ReplicationPolicy}, {policy.HistoryPolicy}");
        }

        public ServerAuthoritativePolicyResolution ResolveGameplayEffect(
            string behaviorId,
            ServerAuthoritativePacketKind packetKind)
        {
            if (!m_Profile || !m_Profile.TryGetBehaviorPolicy(behaviorId, out ServerAuthoritativeBehaviorPolicy policy) ||
                policy.TargetDomain != ServerAuthoritativeDomain.GameplayEffect)
                return Missing(behaviorId, packetKind);
            bool shouldSend = policy.AuthorityPolicy != ServerAuthoritativeAuthorityPolicy.LocalOnly &&
                              policy.ReplicationPolicy != ServerAuthoritativeReplicationPolicy.None;
            return Configured(
                policy,
                GameplayBehaviorKind.Effect,
                packetKind,
                packetKind.ToString(),
                shouldSend,
                $"{policy.PredictionPolicy}, {policy.AuthorityPolicy}, {policy.ReplicationPolicy}, {policy.HistoryPolicy}");
        }

        static ServerAuthoritativePolicyResolution Configured(
            ServerAuthoritativeBehaviorPolicy policy,
            GameplayBehaviorKind behaviorKind,
            ServerAuthoritativePacketKind packetKind,
            string suffix,
            bool shouldSend,
            string summary)
        {
            return new ServerAuthoritativePolicyResolution(
                policy.BehaviorId,
                behaviorKind,
                true,
                shouldSend,
                policy.TargetDomain,
                packetKind,
                BuildPolicyId(policy.BehaviorId, suffix),
                shouldSend ? string.Empty : "FilteredByPolicy",
                summary);
        }

        static ServerAuthoritativePolicyResolution Missing(string owner, ServerAuthoritativePacketKind packetKind)
        {
            return new ServerAuthoritativePolicyResolution(
                owner,
                default,
                false,
                false,
                default,
                packetKind,
                string.Empty,
                "MissingServerAuthoritativeBehaviorPolicy",
                string.Empty);
        }

        static GameplayBehaviorKind BehaviorKind(ServerAuthoritativeDomain domain)
        {
            if (domain == ServerAuthoritativeDomain.Motion)
                return GameplayBehaviorKind.Stream;
            if (domain == ServerAuthoritativeDomain.GameplayEffect)
                return GameplayBehaviorKind.Effect;
            return GameplayBehaviorKind.Event;
        }

        static ServerAuthoritativePacketKind PacketKind(ServerAuthoritativeFactKind factKind)
        {
            switch (factKind)
            {
                case ServerAuthoritativeFactKind.MotionCommand:
                    return ServerAuthoritativePacketKind.MotionCommand;
                case ServerAuthoritativeFactKind.MotionCorrectionAcknowledgement:
                    return ServerAuthoritativePacketKind.MotionCorrectionAck;
                case ServerAuthoritativeFactKind.GameplayAttributeValue:
                    return ServerAuthoritativePacketKind.GameplayAttributeValue;
                default:
                    return ServerAuthoritativePacketKind.None;
            }
        }

        internal static string BuildPolicyId(string ownerId, string suffix)
        {
            return $"{ServerAuthoritativeHybridSession.StableModelId}:{ownerId}:{suffix}";
        }
    }

    public sealed class ServerAuthoritativeTransactionPolicyResolver
    {
        readonly ServerAuthoritativeCharacterSyncProfile m_Profile;

        public ServerAuthoritativeTransactionPolicyResolver(ServerAuthoritativeCharacterSyncProfile profile)
        {
            m_Profile = profile;
        }

        public ServerAuthoritativePolicyResolution ResolveActivation(string actionId)
        {
            if (!TryGet(actionId, out ServerAuthoritativeActionPolicy policy))
                return Missing(actionId, ServerAuthoritativePacketKind.ActionActivation);
            bool send = IsNetworkVisible(policy);
            return Configured(policy, ServerAuthoritativeDomain.Action, ServerAuthoritativePacketKind.ActionActivation, "Activation", send,
                $"{policy.PredictionPolicy}, {policy.AuthorityPolicy}, {policy.ReplicationPolicy}");
        }

        public ServerAuthoritativePolicyResolution ResolveLifecycle(string actionId, ActionLifecycleTransitionType transitionType)
        {
            if (!TryGet(actionId, out ServerAuthoritativeActionPolicy policy))
                return Missing(actionId, ServerAuthoritativePacketKind.ActionLifecycleTransition);
            bool send = IsNetworkVisible(policy);
            return Configured(policy, ServerAuthoritativeDomain.Action, ServerAuthoritativePacketKind.ActionLifecycleTransition, $"Lifecycle:{transitionType}", send,
                $"{transitionType}, {policy.AuthorityPolicy}, {policy.ReplicationPolicy}");
        }

        public ServerAuthoritativePolicyResolution ResolveWindow(string actionId, string windowType)
        {
            if (!TryGet(actionId, out ServerAuthoritativeActionPolicy policy) ||
                !policy.TryGetWindowPolicy(windowType, out ServerAuthoritativeWindowPolicy window))
                return Missing($"{actionId}:{windowType}", ServerAuthoritativePacketKind.ActionWindowDigest);
            bool send = window.WriteDigest && window.ReplicationPolicy != ServerAuthoritativeWindowReplicationPolicy.None;
            return Configured(policy, ServerAuthoritativeDomain.Action, ServerAuthoritativePacketKind.ActionWindowDigest, $"Window:{windowType}", send,
                $"{window.AuthorityPolicy}, {window.HistoryPolicy}, {window.ReplicationPolicy}");
        }

        public ServerAuthoritativePolicyResolution ResolveMotion(string actionId, ActionMotionSourceType sourceType)
        {
            if (!TryGet(actionId, out ServerAuthoritativeActionPolicy policy) ||
                !policy.TryGetMotionPolicy(sourceType, out ServerAuthoritativeMotionPolicy motion))
                return Missing($"{actionId}:{sourceType}", ServerAuthoritativePacketKind.ActionMotionDigest);
            bool send = motion.PredictionPolicy == ServerAuthoritativePredictionPolicy.LocalPredicted && IsNetworkVisible(policy);
            return Configured(policy, ServerAuthoritativeDomain.Motion, ServerAuthoritativePacketKind.ActionMotionDigest, $"Motion:{sourceType}", send,
                $"{motion.PredictionPolicy}, {policy.AuthorityPolicy}, {policy.ReplicationPolicy}");
        }

        public ServerAuthoritativePolicyResolution ResolveCue(string actionId, string cueType)
        {
            if (!TryGet(actionId, out ServerAuthoritativeActionPolicy policy) ||
                !policy.TryGetCuePolicy(cueType, out ServerAuthoritativeCuePolicy cue))
                return Missing($"{actionId}:{cueType}", ServerAuthoritativePacketKind.GameplayCue);
            bool send = cue.PlaybackPolicy != ServerAuthoritativeCuePlaybackPolicy.LocalOnly &&
                        policy.ReplicationPolicy != ServerAuthoritativeReplicationPolicy.None;
            return Configured(policy, ServerAuthoritativeDomain.Presentation, ServerAuthoritativePacketKind.GameplayCue, $"Cue:{cueType}", send,
                $"{cue.PlaybackPolicy}, {policy.ReplicationPolicy}");
        }

        public ServerAuthoritativePolicyResolution ResolveGameplayResult(string actionId)
        {
            if (!TryGet(actionId, out ServerAuthoritativeActionPolicy policy) || policy.GameplayResultPolicy == null)
                return Missing(actionId, ServerAuthoritativePacketKind.GameplayResult);
            ServerAuthoritativeGameplayResultPolicy result = policy.GameplayResultPolicy;
            bool send = result.ProposalPolicy == ServerAuthoritativeGameplayResultProposalPolicy.ClientProposal &&
                        result.ReplicationPolicy != ServerAuthoritativeGameplayResultReplicationPolicy.None &&
                        result.WriteDigest;
            return Configured(policy, ServerAuthoritativeDomain.GameplayResult, ServerAuthoritativePacketKind.GameplayResult, "GameplayResult", send,
                $"{result.ProposalPolicy}, {result.HistoryPolicy}, {result.ReplicationPolicy}");
        }

        bool TryGet(string actionId, out ServerAuthoritativeActionPolicy policy)
        {
            policy = null;
            return m_Profile && m_Profile.TryGetActionPolicy(actionId, out policy);
        }

        static bool IsNetworkVisible(ServerAuthoritativeActionPolicy policy)
        {
            return policy.AuthorityPolicy != ServerAuthoritativeAuthorityPolicy.LocalOnly &&
                   policy.ReplicationPolicy != ServerAuthoritativeReplicationPolicy.None;
        }

        static ServerAuthoritativePolicyResolution Configured(
            ServerAuthoritativeActionPolicy policy,
            ServerAuthoritativeDomain domain,
            ServerAuthoritativePacketKind packetKind,
            string suffix,
            bool shouldSend,
            string summary)
        {
            return new ServerAuthoritativePolicyResolution(
                policy.ActionId,
                GameplayBehaviorKind.Transaction,
                true,
                shouldSend,
                domain,
                packetKind,
                ServerAuthoritativeBehaviorPolicyResolver.BuildPolicyId(policy.ActionId, suffix),
                shouldSend ? string.Empty : "FilteredByPolicy",
                summary);
        }

        static ServerAuthoritativePolicyResolution Missing(string owner, ServerAuthoritativePacketKind packetKind)
        {
            return new ServerAuthoritativePolicyResolution(
                owner,
                GameplayBehaviorKind.Transaction,
                false,
                false,
                default,
                packetKind,
                string.Empty,
                "MissingServerAuthoritativeTransactionPolicy",
                string.Empty);
        }
    }
}
