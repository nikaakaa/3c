using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonGameplay.Tags;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal static class CharacterSimulationGameplayNodeEmitterRegistration
    {
        public static void Register(CharacterSimulationNodeEmitterRegistry registry)
        {
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<HasGameplayTagNode>(node => new CharacterSimulationNodeEmission(
                SimulationOperationCode.GameplayEffectHasTag,
                text0: TagIdentity(node.Tag.Value))));
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<MatchGameplayTagQueryNode>(node => new CharacterSimulationNodeEmission(
                SimulationOperationCode.GameplayEffectMatchTags,
                constants: QueryFields(node.Query, "Query"))));
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<ReadGameplayAttributeNode>(node => new CharacterSimulationNodeEmission(
                SimulationOperationCode.GameplayAttributeRead,
                text0: AttributeIdentity(node.Attribute.Value))));
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<ApplyGameplayEffectNode>(ApplyGameplayEffect));
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<RemoveGameplayEffectNode>(RemoveGameplayEffect));
        }

        static CharacterSimulationNodeEmission ApplyGameplayEffect(ApplyGameplayEffectNode node)
        {
            var constants = new List<KeyValuePair<string, object>>
            {
                new KeyValuePair<string, object>("DefinitionRevision", node.Effect ? node.Effect.DefinitionRevision : 0U),
                new KeyValuePair<string, object>("ActionContext", CharacterSimulationNodeEmitterContext.AssetIdentity(node.ActionContext)),
                new KeyValuePair<string, object>("Predicted", node.Predicted)
            };
            for (int i = 0; i < node.SetByCallerValues.Count; i++)
            {
                string parameterId = node.SetByCallerValues[i].ParameterId;
                constants.Add(new KeyValuePair<string, object>($"SetByCaller:{parameterId}", node.SetByCallerValues[i].Value));
            }
            return new CharacterSimulationNodeEmission(
                SimulationOperationCode.GameplayEffectApply,
                text0: EffectIdentity(node.Effect ? node.Effect.EffectId.Value : string.Empty),
                constants: constants);
        }

        static CharacterSimulationNodeEmission RemoveGameplayEffect(RemoveGameplayEffectNode node)
        {
            var constants = new List<KeyValuePair<string, object>>
            {
                new KeyValuePair<string, object>("Handle", node.Handle),
                new KeyValuePair<string, object>("Effect", EffectIdentity(node.Effect ? node.Effect.EffectId.Value : string.Empty))
            };
            constants.AddRange(QueryFields(node.EffectTagQuery, "Query"));
            return new CharacterSimulationNodeEmission(
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
