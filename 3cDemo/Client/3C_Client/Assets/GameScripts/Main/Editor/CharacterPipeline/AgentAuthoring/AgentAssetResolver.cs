using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using FlowCanvas;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEngine;

using ThirdPersonCharacter.Pipeline.Editor.Authoring.Presentation;
using ThirdPersonCharacter.Pipeline.Editor.Authoring.Skill;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public sealed class AgentAssetResolver
    {
        readonly CharacterPipelineDefinition m_Definition;
        readonly AgentAuthoringTarget m_Current;

        public AgentAssetResolver(CharacterPipelineDefinition definition, AgentAuthoringTarget current)
        {
            m_Definition = definition;
            m_Current = current;
        }

        public bool TryResolveInputValue(string inputValueId, out CharacterInputValueDefinition value)
        {
            value = null;
            CharacterInputProfile profile = m_Definition ? m_Definition.InputProfile : null;
            if (!profile || string.IsNullOrEmpty(inputValueId))
                return false;

            IReadOnlyList<CharacterInputValueDefinition> values = profile.InputValues;
            for (int i = 0; i < values.Count; i++)
            {
                CharacterInputValueDefinition candidate = values[i];
                if (candidate != null && string.Equals(candidate.InputValueId, inputValueId, StringComparison.Ordinal))
                {
                    value = candidate;
                    return true;
                }
            }
            return false;
        }

        public bool TryResolvePortableInputValue(
            string inputValueId,
            ProgramInputValueKind expectedKind)
        {
            if (string.IsNullOrEmpty(inputValueId) || m_Current?.context?.inputValues == null)
                return false;
            return m_Current.context.inputValues.Any(value =>
                value != null &&
                string.Equals(value.inputValueId, inputValueId, StringComparison.Ordinal) &&
                string.Equals(value.valueType, expectedKind.ToString(), StringComparison.Ordinal));
        }

        public bool TryResolveActionRequest(string requestId, out CharacterActionRequestDefinition request)
        {
            request = null;
            CharacterInputProfile profile = m_Definition ? m_Definition.InputProfile : null;
            if (!profile || string.IsNullOrEmpty(requestId))
                return false;

            IReadOnlyList<CharacterActionRequestDefinition> requests = profile.ActionRequests;
            for (int i = 0; i < requests.Count; i++)
            {
                CharacterActionRequestDefinition candidate = requests[i];
                if (candidate != null && string.Equals(candidate.RequestId, requestId, StringComparison.Ordinal))
                {
                    request = candidate;
                    return true;
                }
            }
            return false;
        }

        public bool TryResolveActionProfile(string actionId, out ActionProfile profile)
        {
            profile = null;
            if (!m_Definition || string.IsNullOrEmpty(actionId))
                return false;

            IReadOnlyList<ActionProfile> profiles = m_Definition.ActionProfiles;
            for (int i = 0; i < profiles.Count; i++)
            {
                ActionProfile candidate = profiles[i];
                if (candidate && string.Equals(candidate.ActionId, actionId, StringComparison.Ordinal))
                {
                    profile = candidate;
                    return true;
                }
            }
            return false;
        }

        public bool TryResolveActionContext(AgentAssetReference reference, out ActionContextSlot actionContext)
        {
            actionContext = null;
            UnityEngine.Object asset = ResolveObject(reference.AssetGuid, reference.AssetPath, typeof(ActionContextSlot));
            if (asset is ActionContextSlot directContext)
            {
                actionContext = directContext;
                return true;
            }
            return false;
        }

        public bool TryResolveRootMotionCurve(AgentAssetReference reference, out RootMotionCurveAsset curve)
        {
            curve = ResolveObject(reference.AssetGuid, reference.AssetPath, typeof(RootMotionCurveAsset)) as RootMotionCurveAsset;
            return curve;
        }

        public bool TryResolveSkillGraph(string identity, out FlowGraph graph)
        {
            graph = null;
            if (!m_Definition || string.IsNullOrWhiteSpace(identity))
                return false;
            var index = new BtsmtlSkillGraphClosureIndex();
            index.Build(m_Definition);
            return index.Graphs.TryGetValue(identity, out graph);
        }

        public bool TryResolveSkillTimeline(string identity, out TimelineAsset timeline)
        {
            timeline = null;
            if (!m_Definition || string.IsNullOrWhiteSpace(identity))
                return false;
            var index = new BtsmtlSkillGraphClosureIndex();
            index.Build(m_Definition);
            return index.Timelines.TryGetValue(identity, out timeline);
        }

        public bool TryResolveSkillObject<T>(AgentPackageObjectReference reference, out T value)
            where T : UnityEngine.Object
        {
            value = null;
            if (reference == null || !string.IsNullOrEmpty(reference.localId))
                return false;
            string path = AssetDatabase.GUIDToAssetPath(reference.assetGuid);
            if (string.IsNullOrEmpty(path) || !string.Equals(path, reference.assetPath, StringComparison.Ordinal))
                return false;
            value = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<T>()
                .FirstOrDefault(candidate => AssetDatabase.TryGetGUIDAndLocalFileIdentifier(candidate, out _, out long localFileId) && localFileId == reference.localFileId);
            return value;
        }

        static UnityEngine.Object ResolveObject(string guid, string path, Type expectedType)
        {
            if (!string.IsNullOrEmpty(guid))
            {
                string resolvedPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(resolvedPath))
                    return AssetDatabase.LoadAssetAtPath(resolvedPath, expectedType);
            }

            if (!string.IsNullOrEmpty(path))
                return AssetDatabase.LoadAssetAtPath(path, expectedType);

            return null;
        }
    }
}
