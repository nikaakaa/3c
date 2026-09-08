using System;
using System.Collections.Generic;
using System.Linq;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public static class CharacterPoseStateAliasResolver
    {
        public static Dictionary<PoseStateAliasId, HashSet<PoseStateId>> Expand(
            IReadOnlyList<CharacterPoseStateAlias> aliases)
        {
            var authored = aliases.ToDictionary(value => value.AliasId);
            var result = new Dictionary<PoseStateAliasId, HashSet<PoseStateId>>();
            var visiting = new HashSet<PoseStateAliasId>();
            foreach (CharacterPoseStateAlias alias in aliases.OrderBy(value => value.AliasId))
                ExpandAlias(alias.AliasId, authored, result, visiting);
            return result;
        }

        static HashSet<PoseStateId> ExpandAlias(
            PoseStateAliasId aliasId,
            IReadOnlyDictionary<PoseStateAliasId, CharacterPoseStateAlias> authored,
            Dictionary<PoseStateAliasId, HashSet<PoseStateId>> result,
            HashSet<PoseStateAliasId> visiting)
        {
            if (result.TryGetValue(aliasId, out HashSet<PoseStateId> existing))
                return existing;
            if (!visiting.Add(aliasId))
                throw new InvalidOperationException($"Pose State Alias cycle contains '{aliasId}'.");
            if (!authored.TryGetValue(aliasId, out CharacterPoseStateAlias alias))
                throw new InvalidOperationException($"Pose State Alias '{aliasId}' is missing.");
            var states = new HashSet<PoseStateId>();
            for (int i = 0; i < alias.Sources.Count; i++)
            {
                CharacterPoseStateTransitionSource source = alias.Sources[i];
                if (source.Kind == PoseStateTransitionSourceKind.State)
                    states.Add(source.StateId);
                else
                    states.UnionWith(ExpandAlias(source.AliasId, authored, result, visiting));
            }
            visiting.Remove(aliasId);
            if (states.Count == 0)
                throw new InvalidOperationException($"Pose State Alias '{aliasId}' expands to no State.");
            result.Add(aliasId, states);
            return states;
        }

    }
}
