using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonGameplay.Tags;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public enum AgentMutationKind
    {
        EnsureGameplayTag,
        SetActionProfileGrantedTags,
        SetActionProfileCancelQuery,
        SetActionProfileTargetRequirement,
        SetActionRequestTimingClass,
        ConfigureControlConfiguration,
        SetSkillFlowDocument
    }

    public readonly struct AgentAssetReference
    {
        public AgentAssetReference(string logicalId, string assetPath, string assetGuid)
        {
            LogicalId = logicalId ?? string.Empty;
            AssetPath = assetPath ?? string.Empty;
            AssetGuid = assetGuid ?? string.Empty;
        }

        public string LogicalId { get; }
        public string AssetPath { get; }
        public string AssetGuid { get; }
        public bool HasExplicitAsset => !string.IsNullOrEmpty(AssetPath) || !string.IsNullOrEmpty(AssetGuid);
    }

    public abstract class AgentMutation
    {
        protected AgentMutation(
            string id,
            AgentMutationKind kind,
            string operationName,
            string path,
            string ownerScope)
        {
            Id = id;
            Kind = kind;
            OperationName = operationName;
            Path = path;
            OwnerScope = ownerScope ?? string.Empty;
        }

        public string Id { get; }
        public AgentMutationKind Kind { get; }
        public string OperationName { get; }
        public string Path { get; }
        public string OwnerScope { get; }
    }

    public sealed class AgentEnsureGameplayTagMutation : AgentMutation
    {
        public AgentEnsureGameplayTagMutation(
            string id,
            string path,
            string tag,
            string parentTag,
            string displayName,
            string debugCategory)
            : base(id, AgentMutationKind.EnsureGameplayTag, "ensure_gameplay_tag", path, "GameplayTagCatalog")
        {
            Tag = new GameplayTagId(tag);
            ParentTag = new GameplayTagId(parentTag);
            DisplayName = displayName ?? string.Empty;
            DebugCategory = debugCategory ?? string.Empty;
        }

        public GameplayTagId Tag { get; }
        public GameplayTagId ParentTag { get; }
        public string DisplayName { get; }
        public string DebugCategory { get; }
    }

    public abstract class AgentActionProfileAdmissionMutation : AgentMutation
    {
        protected AgentActionProfileAdmissionMutation(
            string id,
            AgentMutationKind kind,
            string operationName,
            string path,
            AgentAssetReference actionProfile)
            : base(id, kind, operationName, path, actionProfile.LogicalId)
        {
            ActionProfile = actionProfile;
        }

        public AgentAssetReference ActionProfile { get; }
    }

    public sealed class AgentSetActionProfileGrantedTagsMutation : AgentActionProfileAdmissionMutation
    {
        readonly ReadOnlyCollection<GameplayTagId> m_Tags;

        public AgentSetActionProfileGrantedTagsMutation(
            string id,
            string path,
            AgentAssetReference actionProfile,
            IList<GameplayTagId> tags)
            : base(id, AgentMutationKind.SetActionProfileGrantedTags, "set_action_profile_granted_tags", path, actionProfile)
        {
            m_Tags = new ReadOnlyCollection<GameplayTagId>(new List<GameplayTagId>(tags));
        }

        public IReadOnlyList<GameplayTagId> Tags => m_Tags;
    }

    public sealed class AgentSetActionProfileCancelQueryMutation : AgentActionProfileAdmissionMutation
    {
        readonly ReadOnlyCollection<GameplayTagId> m_All;
        readonly ReadOnlyCollection<GameplayTagId> m_Any;
        readonly ReadOnlyCollection<GameplayTagId> m_None;

        public AgentSetActionProfileCancelQueryMutation(
            string id,
            string path,
            AgentAssetReference actionProfile,
            IList<GameplayTagId> all,
            IList<GameplayTagId> any,
            IList<GameplayTagId> none)
            : base(id, AgentMutationKind.SetActionProfileCancelQuery, "set_action_profile_cancel_query", path, actionProfile)
        {
            m_All = new ReadOnlyCollection<GameplayTagId>(new List<GameplayTagId>(all));
            m_Any = new ReadOnlyCollection<GameplayTagId>(new List<GameplayTagId>(any));
            m_None = new ReadOnlyCollection<GameplayTagId>(new List<GameplayTagId>(none));
        }

        public IReadOnlyList<GameplayTagId> All => m_All;
        public IReadOnlyList<GameplayTagId> Any => m_Any;
        public IReadOnlyList<GameplayTagId> None => m_None;
    }

    public sealed class AgentSetActionProfileTargetRequirementMutation : AgentActionProfileAdmissionMutation
    {
        public AgentSetActionProfileTargetRequirementMutation(
            string id,
            string path,
            AgentAssetReference actionProfile,
            ActionTargetRequirement targetRequirement)
            : base(id, AgentMutationKind.SetActionProfileTargetRequirement, "set_action_profile_target_requirement", path, actionProfile)
        {
            TargetRequirement = targetRequirement;
        }

        public ActionTargetRequirement TargetRequirement { get; }
    }

    public sealed class AgentSetActionRequestTimingClassMutation : AgentMutation
    {
        public AgentSetActionRequestTimingClassMutation(
            string id,
            string path,
            string requestId,
            CharacterActionRequestTimingClass timingClass)
            : base(id, AgentMutationKind.SetActionRequestTimingClass, "set_action_request_timing_class", path, requestId)
        {
            RequestId = requestId;
            TimingClass = timingClass;
        }

        public string RequestId { get; }
        public CharacterActionRequestTimingClass TimingClass { get; }
    }

    public sealed class AgentConfigureControlConfigurationMutation : AgentMutation
    {
        public AgentConfigureControlConfigurationMutation(
            string id,
            string path,
            AgentDocumentControlConfiguration configuration)
            : base(id, AgentMutationKind.ConfigureControlConfiguration, "configure_control_configuration", path, "CharacterControlConfiguration")
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public AgentDocumentControlConfiguration Configuration { get; }
    }

    public sealed class AgentMutationPlan
    {
        readonly ReadOnlyCollection<AgentMutation> m_Commands;

        public AgentMutationPlan(
            IList<AgentMutation> commands,
            string domain,
            string rootIdentity,
            string sourceRevision)
        {
            m_Commands = new ReadOnlyCollection<AgentMutation>(new List<AgentMutation>(commands));
            Domain = domain;
            RootIdentity = rootIdentity;
            SourceRevision = sourceRevision;
        }

        public IReadOnlyList<AgentMutation> Commands => m_Commands;
        public string Domain { get; }
        public string RootIdentity { get; }
        public string SourceRevision { get; }
    }
}
