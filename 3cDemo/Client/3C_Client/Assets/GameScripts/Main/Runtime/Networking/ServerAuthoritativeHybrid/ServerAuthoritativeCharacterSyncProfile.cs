using System;
using System.Collections.Generic;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Behavior;
using ThirdPersonGameplay.Contracts;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonGameplay.Effects;
using UnityEngine;

namespace ThirdPersonGameplay.Networking.ServerAuthoritativeHybrid
{
    [CreateAssetMenu(
        fileName = "ServerAuthoritativeCharacterSyncProfile",
        menuName = "3C/Networking/Server Authoritative Character Sync Profile")]
    public sealed class ServerAuthoritativeCharacterSyncProfile : ScriptableObject
    {
        [SerializeField] CharacterPipelineDefinition m_CharacterDefinition;
        [SerializeField] List<ServerAuthoritativeBehaviorPolicy> m_BehaviorPolicies = new List<ServerAuthoritativeBehaviorPolicy>();
        [SerializeField] List<ServerAuthoritativeActionPolicy> m_ActionPolicies = new List<ServerAuthoritativeActionPolicy>();
        [SerializeField] List<ServerAuthoritativeFactBinding> m_FactBindings = new List<ServerAuthoritativeFactBinding>();

        public CharacterPipelineDefinition CharacterDefinition => m_CharacterDefinition;
        public IReadOnlyList<ServerAuthoritativeBehaviorPolicy> BehaviorPolicies => m_BehaviorPolicies;
        public IReadOnlyList<ServerAuthoritativeActionPolicy> ActionPolicies => m_ActionPolicies;
        public IReadOnlyList<ServerAuthoritativeFactBinding> FactBindings => m_FactBindings;

        public bool TryGetBehaviorPolicy(string behaviorId, out ServerAuthoritativeBehaviorPolicy policy)
        {
            policy = null;
            if (string.IsNullOrEmpty(behaviorId))
                return false;

            for (int i = 0; i < m_BehaviorPolicies.Count; i++)
            {
                ServerAuthoritativeBehaviorPolicy candidate = m_BehaviorPolicies[i];
                if (candidate != null && string.Equals(candidate.BehaviorId, behaviorId, StringComparison.Ordinal))
                {
                    policy = candidate;
                    return true;
                }
            }

            return false;
        }

        public bool TryGetActionPolicy(string actionId, out ServerAuthoritativeActionPolicy policy)
        {
            policy = null;
            if (string.IsNullOrEmpty(actionId))
                return false;

            for (int i = 0; i < m_ActionPolicies.Count; i++)
            {
                ServerAuthoritativeActionPolicy candidate = m_ActionPolicies[i];
                if (candidate != null && string.Equals(candidate.ActionId, actionId, StringComparison.Ordinal))
                {
                    policy = candidate;
                    return true;
                }
            }

            return false;
        }

        public bool TryGetFactPolicy(
            ServerAuthoritativeFactKind factKind,
            out ServerAuthoritativeBehaviorPolicy policy)
        {
            policy = null;
            for (int i = 0; i < m_FactBindings.Count; i++)
            {
                ServerAuthoritativeFactBinding binding = m_FactBindings[i];
                if (binding != null && binding.FactKind == factKind)
                    return TryGetBehaviorPolicy(binding.BehaviorId, out policy);
            }

            return false;
        }

        public bool CollectConfigurationErrors(List<string> errors)
        {
            bool valid = true;
            if (!m_CharacterDefinition)
            {
                errors?.Add($"{name}: character pipeline definition is missing.");
                return false;
            }

            valid &= ValidateBehaviorPolicies(errors);
            valid &= ValidateActionPolicies(errors);
            valid &= ValidateFactBindings(errors);
            return valid;
        }

        bool ValidateBehaviorPolicies(List<string> errors)
        {
            bool valid = true;
            HashSet<string> configuredIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < m_BehaviorPolicies.Count; i++)
            {
                ServerAuthoritativeBehaviorPolicy policy = m_BehaviorPolicies[i];
                if (policy == null || string.IsNullOrEmpty(policy.BehaviorId))
                {
                    errors?.Add($"{name}: behavior policy #{i} id is missing.");
                    valid = false;
                    continue;
                }
                if (!configuredIds.Add(policy.BehaviorId))
                {
                    errors?.Add($"{name}: duplicate behavior policy '{policy.BehaviorId}'.");
                    valid = false;
                    continue;
                }
                if (!m_CharacterDefinition.TryGetBehaviorProfile(policy.BehaviorId, out IGameplayBehaviorProfile definition) ||
                    definition.BehaviorKind == GameplayBehaviorKind.Transaction)
                {
                    errors?.Add($"{name}: behavior policy '{policy.BehaviorId}' does not exist in {m_CharacterDefinition.name}.");
                    valid = false;
                    continue;
                }

                valid &= policy.CollectConfigurationErrors(name, definition.BehaviorKind, errors);
            }

            IReadOnlyList<GameplayBehaviorProfile> definitions = m_CharacterDefinition.BehaviorProfiles;
            for (int i = 0; i < definitions.Count; i++)
            {
                GameplayBehaviorProfile definition = definitions[i];
                if (definition && !configuredIds.Contains(definition.BehaviorId))
                {
                    errors?.Add($"{name}: behavior policy '{definition.BehaviorId}' is missing.");
                    valid = false;
                }
            }

            IReadOnlyList<GameplayEffectDefinition> effects = m_CharacterDefinition.GameplayEffectProfile.EffectDefinitions;
            for (int i = 0; i < effects.Count; i++)
            {
                GameplayEffectDefinition definition = effects[i];
                if (definition && !configuredIds.Contains(definition.BehaviorId))
                {
                    errors?.Add($"{name}: behavior policy '{definition.BehaviorId}' is missing.");
                    valid = false;
                }
            }

            return valid;
        }

        bool ValidateActionPolicies(List<string> errors)
        {
            bool valid = true;
            HashSet<string> configuredIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < m_ActionPolicies.Count; i++)
            {
                ServerAuthoritativeActionPolicy policy = m_ActionPolicies[i];
                if (policy == null || string.IsNullOrEmpty(policy.ActionId))
                {
                    errors?.Add($"{name}: action policy #{i} id is missing.");
                    valid = false;
                    continue;
                }
                if (!configuredIds.Add(policy.ActionId))
                {
                    errors?.Add($"{name}: duplicate action policy '{policy.ActionId}'.");
                    valid = false;
                    continue;
                }
                if (!TryGetActionDefinition(policy.ActionId, out _))
                {
                    errors?.Add($"{name}: action policy '{policy.ActionId}' does not exist in {m_CharacterDefinition.name}.");
                    valid = false;
                    continue;
                }

                valid &= policy.CollectConfigurationErrors(name, errors);
            }

            IReadOnlyList<ActionProfile> definitions = m_CharacterDefinition.ActionProfiles;
            for (int i = 0; i < definitions.Count; i++)
            {
                ActionProfile definition = definitions[i];
                if (definition && !configuredIds.Contains(definition.ActionId))
                {
                    errors?.Add($"{name}: action policy '{definition.ActionId}' is missing.");
                    valid = false;
                }
            }

            return valid;
        }

        bool ValidateFactBindings(List<string> errors)
        {
            bool valid = true;
            HashSet<ServerAuthoritativeFactKind> configuredKinds = new HashSet<ServerAuthoritativeFactKind>();
            for (int i = 0; i < m_FactBindings.Count; i++)
            {
                ServerAuthoritativeFactBinding binding = m_FactBindings[i];
                if (binding == null || binding.FactKind == ServerAuthoritativeFactKind.None)
                {
                    errors?.Add($"{name}: fact binding #{i} kind is missing.");
                    valid = false;
                    continue;
                }
                if (!configuredKinds.Add(binding.FactKind))
                {
                    errors?.Add($"{name}: fact '{binding.FactKind}' has multiple policy owners.");
                    valid = false;
                    continue;
                }
                if (!TryGetBehaviorPolicy(binding.BehaviorId, out ServerAuthoritativeBehaviorPolicy policy))
                {
                    errors?.Add($"{name}: fact '{binding.FactKind}' references missing behavior policy '{binding.BehaviorId}'.");
                    valid = false;
                    continue;
                }

                valid &= ValidateFactOwner(binding.FactKind, policy, errors);
            }

            valid &= RequireFactBinding(ServerAuthoritativeFactKind.MotionCommand, configuredKinds, errors);
            valid &= RequireFactBinding(ServerAuthoritativeFactKind.MotionCorrectionAcknowledgement, configuredKinds, errors);
            return valid;
        }

        bool ValidateFactOwner(
            ServerAuthoritativeFactKind factKind,
            ServerAuthoritativeBehaviorPolicy policy,
            List<string> errors)
        {
            GameplayBehaviorKind expectedKind;
            ServerAuthoritativeDomain expectedDomain;
            switch (factKind)
            {
                case ServerAuthoritativeFactKind.MotionCommand:
                case ServerAuthoritativeFactKind.MotionCorrectionAcknowledgement:
                    expectedKind = GameplayBehaviorKind.Stream;
                    expectedDomain = ServerAuthoritativeDomain.Motion;
                    break;
                case ServerAuthoritativeFactKind.GameplayAttributeValue:
                    expectedKind = GameplayBehaviorKind.Event;
                    expectedDomain = ServerAuthoritativeDomain.GameplayEffect;
                    break;
                default:
                    errors?.Add($"{name}: fact kind '{factKind}' is unsupported.");
                    return false;
            }

            if (!m_CharacterDefinition.TryGetBehaviorProfile(policy.BehaviorId, out IGameplayBehaviorProfile definition) ||
                definition.BehaviorKind != expectedKind ||
                policy.TargetDomain != expectedDomain)
            {
                errors?.Add(
                    $"{name}: fact '{factKind}' owner '{policy.BehaviorId}' must be {expectedKind}/{expectedDomain}.");
                return false;
            }

            return true;
        }

        bool RequireFactBinding(
            ServerAuthoritativeFactKind factKind,
            HashSet<ServerAuthoritativeFactKind> configuredKinds,
            List<string> errors)
        {
            if (configuredKinds.Contains(factKind))
                return true;

            errors?.Add($"{name}: fact binding '{factKind}' is missing.");
            return false;
        }

        bool TryGetActionDefinition(string actionId, out ActionProfile definition)
        {
            definition = null;
            IReadOnlyList<ActionProfile> definitions = m_CharacterDefinition.ActionProfiles;
            for (int i = 0; i < definitions.Count; i++)
            {
                ActionProfile candidate = definitions[i];
                if (candidate && string.Equals(candidate.ActionId, actionId, StringComparison.Ordinal))
                {
                    definition = candidate;
                    return true;
                }
            }

            return false;
        }
    }

    [Serializable]
    public sealed class ServerAuthoritativeBehaviorPolicy
    {
        [SerializeField] string m_BehaviorId;
        [SerializeField] ServerAuthoritativeDomain m_TargetDomain = ServerAuthoritativeDomain.Motion;
        [SerializeField] ServerAuthoritativePredictionPolicy m_PredictionPolicy = ServerAuthoritativePredictionPolicy.LocalPredicted;
        [SerializeField] ServerAuthoritativeAuthorityPolicy m_AuthorityPolicy = ServerAuthoritativeAuthorityPolicy.ServerAuthoritative;
        [SerializeField] ServerAuthoritativeReplicationPolicy m_ReplicationPolicy = ServerAuthoritativeReplicationPolicy.Broadcast;
        [SerializeField] ServerAuthoritativeSnapshotPolicy m_SnapshotPolicy = ServerAuthoritativeSnapshotPolicy.ServerSnapshot;
        [SerializeField] ServerAuthoritativeRemotePresentationPolicy m_RemotePresentationPolicy = ServerAuthoritativeRemotePresentationPolicy.RemoteInterpolated;
        [SerializeField] ServerAuthoritativeHistoryPolicy m_HistoryPolicy = ServerAuthoritativeHistoryPolicy.IncludeDigestOnly;
        [SerializeField] ServerAuthoritativeCommandSendPolicy m_CommandSendPolicy = ServerAuthoritativeCommandSendPolicy.EveryTick;

        public string BehaviorId => m_BehaviorId ?? string.Empty;
        public ServerAuthoritativeDomain TargetDomain => m_TargetDomain;
        public ServerAuthoritativePredictionPolicy PredictionPolicy => m_PredictionPolicy;
        public ServerAuthoritativeAuthorityPolicy AuthorityPolicy => m_AuthorityPolicy;
        public ServerAuthoritativeReplicationPolicy ReplicationPolicy => m_ReplicationPolicy;
        public ServerAuthoritativeSnapshotPolicy SnapshotPolicy => m_SnapshotPolicy;
        public ServerAuthoritativeRemotePresentationPolicy RemotePresentationPolicy => m_RemotePresentationPolicy;
        public ServerAuthoritativeHistoryPolicy HistoryPolicy => m_HistoryPolicy;
        public ServerAuthoritativeCommandSendPolicy CommandSendPolicy => m_CommandSendPolicy;

        public bool CollectConfigurationErrors(
            string ownerName,
            GameplayBehaviorKind behaviorKind,
            List<string> errors)
        {
            bool valid = true;
            ServerAuthoritativeDomain expectedDomain;
            switch (behaviorKind)
            {
                case GameplayBehaviorKind.Stream:
                    expectedDomain = ServerAuthoritativeDomain.Motion;
                    break;
                case GameplayBehaviorKind.Effect:
                    expectedDomain = ServerAuthoritativeDomain.GameplayEffect;
                    break;
                case GameplayBehaviorKind.Event:
                    expectedDomain = m_TargetDomain;
                    if (m_TargetDomain != ServerAuthoritativeDomain.GameplayResult &&
                        m_TargetDomain != ServerAuthoritativeDomain.Presentation &&
                        m_TargetDomain != ServerAuthoritativeDomain.GameplayEffect)
                    {
                        errors?.Add($"{ownerName}: event behavior '{BehaviorId}' has invalid domain '{m_TargetDomain}'.");
                        valid = false;
                    }
                    break;
                default:
                    errors?.Add($"{ownerName}: behavior '{BehaviorId}' cannot use transaction policy storage.");
                    return false;
            }

            if (m_TargetDomain != expectedDomain)
            {
                errors?.Add($"{ownerName}: behavior '{BehaviorId}' must target '{expectedDomain}'.");
                valid = false;
            }
            if (behaviorKind != GameplayBehaviorKind.Stream &&
                (m_SnapshotPolicy != ServerAuthoritativeSnapshotPolicy.None ||
                 m_RemotePresentationPolicy != ServerAuthoritativeRemotePresentationPolicy.None ||
                 m_CommandSendPolicy != ServerAuthoritativeCommandSendPolicy.None))
            {
                errors?.Add($"{ownerName}: non-stream behavior '{BehaviorId}' cannot use stream policies.");
                valid = false;
            }

            return valid;
        }
    }

    [Serializable]
    public sealed class ServerAuthoritativeActionPolicy
    {
        [SerializeField] string m_ActionId;
        [SerializeField] ServerAuthoritativePredictionPolicy m_PredictionPolicy = ServerAuthoritativePredictionPolicy.LocalPredicted;
        [SerializeField] ServerAuthoritativeAuthorityPolicy m_AuthorityPolicy = ServerAuthoritativeAuthorityPolicy.ServerAuthoritative;
        [SerializeField] ServerAuthoritativeReplicationPolicy m_ReplicationPolicy = ServerAuthoritativeReplicationPolicy.Broadcast;
        [SerializeField] List<ServerAuthoritativeWindowPolicy> m_WindowPolicies = new List<ServerAuthoritativeWindowPolicy>();
        [SerializeField] List<ServerAuthoritativeMotionPolicy> m_MotionPolicies = new List<ServerAuthoritativeMotionPolicy>();
        [SerializeField] List<ServerAuthoritativeCuePolicy> m_CuePolicies = new List<ServerAuthoritativeCuePolicy>();
        [SerializeField] ServerAuthoritativeGameplayResultPolicy m_GameplayResultPolicy = new ServerAuthoritativeGameplayResultPolicy();

        public string ActionId => m_ActionId ?? string.Empty;
        public ServerAuthoritativePredictionPolicy PredictionPolicy => m_PredictionPolicy;
        public ServerAuthoritativeAuthorityPolicy AuthorityPolicy => m_AuthorityPolicy;
        public ServerAuthoritativeReplicationPolicy ReplicationPolicy => m_ReplicationPolicy;
        public IReadOnlyList<ServerAuthoritativeWindowPolicy> WindowPolicies => m_WindowPolicies;
        public IReadOnlyList<ServerAuthoritativeMotionPolicy> MotionPolicies => m_MotionPolicies;
        public IReadOnlyList<ServerAuthoritativeCuePolicy> CuePolicies => m_CuePolicies;
        public ServerAuthoritativeGameplayResultPolicy GameplayResultPolicy => m_GameplayResultPolicy;

        public bool TryGetWindowPolicy(string windowType, out ServerAuthoritativeWindowPolicy policy)
        {
            policy = null;
            for (int i = 0; i < m_WindowPolicies.Count; i++)
            {
                ServerAuthoritativeWindowPolicy candidate = m_WindowPolicies[i];
                if (candidate != null && string.Equals(candidate.WindowType, windowType, StringComparison.Ordinal))
                {
                    policy = candidate;
                    return true;
                }
            }

            return false;
        }

        public bool TryGetMotionPolicy(ActionMotionSourceType sourceType, out ServerAuthoritativeMotionPolicy policy)
        {
            policy = null;
            for (int i = 0; i < m_MotionPolicies.Count; i++)
            {
                ServerAuthoritativeMotionPolicy candidate = m_MotionPolicies[i];
                if (candidate != null && candidate.SourceType == sourceType)
                {
                    policy = candidate;
                    return true;
                }
            }

            return false;
        }

        public bool TryGetCuePolicy(string cueType, out ServerAuthoritativeCuePolicy policy)
        {
            policy = null;
            for (int i = 0; i < m_CuePolicies.Count; i++)
            {
                ServerAuthoritativeCuePolicy candidate = m_CuePolicies[i];
                if (candidate != null && string.Equals(candidate.CueType, cueType, StringComparison.Ordinal))
                {
                    policy = candidate;
                    return true;
                }
            }

            return false;
        }

        public bool CollectConfigurationErrors(string ownerName, List<string> errors)
        {
            bool valid = m_GameplayResultPolicy != null;
            if (!valid)
                errors?.Add($"{ownerName}: action '{ActionId}' gameplay result policy is missing.");
            valid &= ValidateUniqueWindows(ownerName, errors);
            valid &= ValidateUniqueMotion(ownerName, errors);
            valid &= ValidateUniqueCues(ownerName, errors);
            return valid;
        }

        bool ValidateUniqueWindows(string ownerName, List<string> errors)
        {
            bool valid = true;
            HashSet<string> values = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < m_WindowPolicies.Count; i++)
            {
                string value = m_WindowPolicies[i]?.WindowType;
                if (string.IsNullOrEmpty(value) || !values.Add(value))
                {
                    errors?.Add($"{ownerName}: action '{ActionId}' window policy #{i} is missing or duplicated.");
                    valid = false;
                }
            }
            return valid;
        }

        bool ValidateUniqueMotion(string ownerName, List<string> errors)
        {
            bool valid = true;
            HashSet<ActionMotionSourceType> values = new HashSet<ActionMotionSourceType>();
            for (int i = 0; i < m_MotionPolicies.Count; i++)
            {
                ServerAuthoritativeMotionPolicy policy = m_MotionPolicies[i];
                if (policy == null || policy.SourceType == ActionMotionSourceType.None || !values.Add(policy.SourceType))
                {
                    errors?.Add($"{ownerName}: action '{ActionId}' motion policy #{i} is missing or duplicated.");
                    valid = false;
                }
            }
            return valid;
        }

        bool ValidateUniqueCues(string ownerName, List<string> errors)
        {
            bool valid = true;
            HashSet<string> values = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < m_CuePolicies.Count; i++)
            {
                string value = m_CuePolicies[i]?.CueType;
                if (string.IsNullOrEmpty(value) || !values.Add(value))
                {
                    errors?.Add($"{ownerName}: action '{ActionId}' cue policy #{i} is missing or duplicated.");
                    valid = false;
                }
            }
            return valid;
        }
    }

    [Serializable]
    public sealed class ServerAuthoritativeWindowPolicy
    {
        [SerializeField] string m_WindowType;
        [SerializeField] ServerAuthoritativeWindowAuthorityPolicy m_AuthorityPolicy = ServerAuthoritativeWindowAuthorityPolicy.ServerCorrectable;
        [SerializeField] ServerAuthoritativeWindowHistoryPolicy m_HistoryPolicy = ServerAuthoritativeWindowHistoryPolicy.IncludeDigestOnly;
        [SerializeField] ServerAuthoritativeWindowReplicationPolicy m_ReplicationPolicy = ServerAuthoritativeWindowReplicationPolicy.DigestOnly;
        [SerializeField] bool m_WriteDigest = true;

        public string WindowType => m_WindowType ?? string.Empty;
        public ServerAuthoritativeWindowAuthorityPolicy AuthorityPolicy => m_AuthorityPolicy;
        public ServerAuthoritativeWindowHistoryPolicy HistoryPolicy => m_HistoryPolicy;
        public ServerAuthoritativeWindowReplicationPolicy ReplicationPolicy => m_ReplicationPolicy;
        public bool WriteDigest => m_WriteDigest;
    }

    [Serializable]
    public sealed class ServerAuthoritativeMotionPolicy
    {
        [SerializeField] ActionMotionSourceType m_SourceType = ActionMotionSourceType.RootMotion;
        [SerializeField] ServerAuthoritativePredictionPolicy m_PredictionPolicy = ServerAuthoritativePredictionPolicy.LocalPredicted;

        public ActionMotionSourceType SourceType => m_SourceType;
        public ServerAuthoritativePredictionPolicy PredictionPolicy => m_PredictionPolicy;
    }

    [Serializable]
    public sealed class ServerAuthoritativeCuePolicy
    {
        [SerializeField] string m_CueType;
        [SerializeField] ServerAuthoritativeCuePlaybackPolicy m_PlaybackPolicy = ServerAuthoritativeCuePlaybackPolicy.LocalPredicted;

        public string CueType => m_CueType ?? string.Empty;
        public ServerAuthoritativeCuePlaybackPolicy PlaybackPolicy => m_PlaybackPolicy;
    }

    [Serializable]
    public sealed class ServerAuthoritativeGameplayResultPolicy
    {
        [SerializeField] ServerAuthoritativeGameplayResultProposalPolicy m_ProposalPolicy = ServerAuthoritativeGameplayResultProposalPolicy.AuthorityOnly;
        [SerializeField] ServerAuthoritativeGameplayResultHistoryPolicy m_HistoryPolicy = ServerAuthoritativeGameplayResultHistoryPolicy.IncludeDigestOnly;
        [SerializeField] ServerAuthoritativeGameplayResultReplicationPolicy m_ReplicationPolicy = ServerAuthoritativeGameplayResultReplicationPolicy.Broadcast;
        [SerializeField] bool m_WriteDigest = true;

        public ServerAuthoritativeGameplayResultProposalPolicy ProposalPolicy => m_ProposalPolicy;
        public ServerAuthoritativeGameplayResultHistoryPolicy HistoryPolicy => m_HistoryPolicy;
        public ServerAuthoritativeGameplayResultReplicationPolicy ReplicationPolicy => m_ReplicationPolicy;
        public bool WriteDigest => m_WriteDigest;
    }

    [Serializable]
    public sealed class ServerAuthoritativeFactBinding
    {
        [SerializeField] ServerAuthoritativeFactKind m_FactKind;
        [SerializeField] string m_BehaviorId;

        public ServerAuthoritativeFactKind FactKind => m_FactKind;
        public string BehaviorId => m_BehaviorId ?? string.Empty;
    }
}
