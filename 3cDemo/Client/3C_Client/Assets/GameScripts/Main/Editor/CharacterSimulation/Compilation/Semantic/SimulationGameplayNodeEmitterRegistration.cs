using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonGameplay.Tags;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal static class SimulationGameplayNodeEmitterRegistration
    {
        public static void Register(SimulationNodeEmitterRegistry registry)
        {
            registry.Register(SimulationNodeEmitterRegistry.Simple<HasGameplayTagNode>(node => new SimulationNodeEmission(
                SimulationOperationCode.GameplayEffectHasTag,
                text0: TagIdentity(node.Tag.Value))));
            registry.Register(SimulationNodeEmitterRegistry.Simple<MatchGameplayTagQueryNode>(node => new SimulationNodeEmission(
                SimulationOperationCode.GameplayEffectMatchTags,
                constants: QueryFields(node.Query, "Query"))));
            registry.Register(SimulationNodeEmitterRegistry.Simple<ReadGameplayAttributeNode>(node => new SimulationNodeEmission(
                SimulationOperationCode.GameplayAttributeRead,
                text0: AttributeIdentity(node.Attribute.Value))));
            registry.Register(SimulationNodeEmitterRegistry.Simple<ApplyGameplayEffectNode>(ApplyGameplayEffect));
            registry.Register(SimulationNodeEmitterRegistry.Simple<RemoveGameplayEffectNode>(RemoveGameplayEffect));
        }

        static SimulationNodeEmission ApplyGameplayEffect(ApplyGameplayEffectNode node)
        {
            var constants = new List<KeyValuePair<string, object>>
            {
                new KeyValuePair<string, object>("DefinitionRevision", node.Effect ? node.Effect.DefinitionRevision : 0U),
                new KeyValuePair<string, object>("ActionContext", SimulationNodeEmitterContext.AssetIdentity(node.ActionContext)),
                new KeyValuePair<string, object>("Predicted", node.Predicted)
            };
            for (int i = 0; i < node.SetByCallerValues.Count; i++)
            {
                string parameterId = node.SetByCallerValues[i].ParameterId;
                constants.Add(new KeyValuePair<string, object>($"SetByCaller:{parameterId}", node.SetByCallerValues[i].Value));
            }
            return new SimulationNodeEmission(
                SimulationOperationCode.GameplayEffectApply,
                text0: EffectIdentity(node.Effect ? node.Effect.EffectId.Value : string.Empty),
                constants: constants);
        }

        static SimulationNodeEmission RemoveGameplayEffect(RemoveGameplayEffectNode node)
        {
            var constants = new List<KeyValuePair<string, object>>
            {
                new KeyValuePair<string, object>("Handle", node.Handle),
                new KeyValuePair<string, object>("Effect", EffectIdentity(node.Effect ? node.Effect.EffectId.Value : string.Empty))
            };
            constants.AddRange(QueryFields(node.EffectTagQuery, "Query"));
            return new SimulationNodeEmission(
                SimulationOperationCode.GameplayEffectRemove,
                integer0: (int)node.Selector,
                constants: constants);
        }

        static KeyValuePair<string, object>[] QueryFields(GameplayTagQuery query, string prefix)
        {
            var fields = new List<KeyValuePair<string, object>>();
            Add(query?.All, "All");
            Add(query?.Any, "Any");
            Add(query?.None, "None");
            return fields.ToArray();

            void Add(IReadOnlyList<GameplayTagId> values, string kind)
            {
                if (values == null)
                    return;
                for (int i = 0; i < values.Count; i++)
                    fields.Add(new KeyValuePair<string, object>($"{prefix}:{kind}:{i:D4}", TagIdentity(values[i].Value)));
            }
        }

        static string TagIdentity(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : $"tag:{value.Trim()}";
        static string AttributeIdentity(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : $"attribute:{value.Trim()}";
        static string EffectIdentity(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : $"effect:{value.Trim()}";
    }
}
