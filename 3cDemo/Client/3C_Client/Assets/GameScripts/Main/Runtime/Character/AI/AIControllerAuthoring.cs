using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonSimulation;
using TreeDesigner;
using UnityEngine;

namespace ThirdPersonCharacter.AI
{
    public enum AICandidateOrdering : byte
    {
        ActorId = 1,
        DistanceThenActorId = 2
    }

    [CreateAssetMenu(fileName = "AIPerceptionProfile", menuName = "3C/AI/Perception Profile")]
    public sealed class AIPerceptionProfile : ScriptableObject
    {
        [SerializeField] string[] m_CandidateActorIds = Array.Empty<string>();
        [SerializeField] AICandidateOrdering m_Ordering = AICandidateOrdering.DistanceThenActorId;

        public IReadOnlyList<string> CandidateActorIds => m_CandidateActorIds ?? Array.Empty<string>();
        public AICandidateOrdering Ordering => m_Ordering;

        public bool CollectConfigurationErrors(List<string> errors)
        {
            bool valid = Enum.IsDefined(typeof(AICandidateOrdering), m_Ordering);
            if (!valid)
                errors?.Add($"{name}: candidate ordering is invalid.");
            var identities = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < CandidateActorIds.Count; i++)
            {
                string actorId = CandidateActorIds[i];
                if (string.IsNullOrWhiteSpace(actorId) || !string.Equals(actorId, actorId.Trim(), StringComparison.Ordinal))
                {
                    errors?.Add($"{name}: candidate ActorId #{i} is invalid.");
                    valid = false;
                }
                else if (!identities.Add(actorId))
                {
                    errors?.Add($"{name}: candidate ActorId '{actorId}' is duplicated.");
                    valid = false;
                }
            }
            return valid;
        }

#if UNITY_EDITOR
        public void ConfigureAuthoring(IEnumerable<string> candidateActorIds, AICandidateOrdering ordering)
        {
            if (candidateActorIds == null)
                throw new ArgumentNullException(nameof(candidateActorIds));
            m_CandidateActorIds = new List<string>(candidateActorIds).ToArray();
            m_Ordering = ordering;
            var errors = new List<string>();
            if (!CollectConfigurationErrors(errors))
                throw new InvalidOperationException(string.Join("\n", errors));
        }
#endif
    }

    [Serializable]
    [TreeWindow("OpenAIControllerTreeWindow")]
    [AcceptableNodePaths("Base", "AI")]
    public sealed class AIControllerTree : OneRootTree
    {
        public override GraphAuthoringRole AuthoringRole => GraphAuthoringRole.AIController;

        public override bool CanCreateNodeType(Type type)
        {
            return base.CanCreateNodeType(type) &&
                   NodeAuthoringCapabilityPolicy.TryGetCapability(type, out NodeAuthoringCapability capability) &&
                   NodeAuthoringCapabilityPolicy.Allows(AuthoringRole, capability);
        }

#if UNITY_EDITOR
        public override bool CheckInit()
        {
            bool dirty = base.CheckInit();
            if (!string.IsNullOrEmpty(RootGUID))
                return dirty;
            RootGUID = CreateNode(typeof(RootNode)).GUID;
            return true;
        }
#endif
    }

    public static class AIControllerSourceRevision
    {
        public static string Compute(
            AIControllerDefinition definition,
            ProgramId characterProgramId,
            ProgramHash characterProgramHash,
            StableHash perceptionSchemaHash)
        {
            if (!definition || definition.RootTreeAsset?.Tree is not AIControllerTree root ||
                !characterProgramId.IsValid || !characterProgramHash.IsValid || !perceptionSchemaHash.IsValid)
            {
                throw new InvalidOperationException("AI Controller source revision inputs are incomplete.");
            }
            return StableHash.Compute(
                "ai-controller-source/2",
                definition.ControllerId,
                root.GraphAuthoringId,
                GraphAuthoringFingerprint.Compute(root),
                characterProgramId.Value,
                characterProgramHash.ToString(),
                perceptionSchemaHash.ToString()).ToString();
        }
    }
}
