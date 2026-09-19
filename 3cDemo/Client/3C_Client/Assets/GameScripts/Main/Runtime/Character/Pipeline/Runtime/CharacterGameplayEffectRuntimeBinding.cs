using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Pipeline.GameplayEffect;
using ThirdPersonGameplay.Attributes;
using ThirdPersonGameplay.Effects;
using ThirdPersonGameplay.Tags;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline
{
    public static class GameplayEffectRuntimeDefinitionCodec
    {
        public static SemanticDataDocument EncodeDefinition(GameplayEffectDefinition effect, string sourceIdentity)
        {
            if (!effect)
                throw new ArgumentNullException(nameof(effect));
            string source = SimulationIdentityValue(sourceIdentity);
            var writer = new SemanticDataWriter();
            writer.WriteInt32(1);
            writer.WriteString(effect.EffectId.Value);
            writer.WriteUInt32(effect.DefinitionRevision);
            writer.WriteInt32((int)effect.DurationPolicy);
            WriteMagnitude(writer, effect.DurationMagnitude, source, "Duration");
            writer.WriteBoolean(effect.HasPeriod);
            WriteMagnitude(writer, effect.PeriodMagnitude, source, "Period");
            writer.WriteBoolean(effect.ExecuteOnApplication);
            writer.WriteInt32((int)effect.StackingPolicy);
            writer.WriteInt32(effect.MaxStacks);
            writer.WriteInt32((int)effect.DurationUpdate);
            writer.WriteInt32((int)effect.PeriodUpdate);
            writer.WriteInt32((int)effect.OverflowPolicy);
            writer.WriteInt32(effect.SetByCallerParameters.Count);
            for (int i = 0; i < effect.SetByCallerParameters.Count; i++)
                writer.WriteString(effect.SetByCallerParameters[i]?.ParameterId);
            writer.WriteInt32(effect.Components.Count);
            for (int i = 0; i < effect.Components.Count; i++)
                WriteComponent(writer, effect.Components[i], source, i);
            return writer.Build();
        }

        static void WriteComponent(
            SemanticDataWriter writer,
            GameplayEffectComponentDefinition component,
            string source,
            int index)
        {
            if (component == null)
                throw new InvalidOperationException($"Gameplay Effect component #{index} is missing.");
            writer.WriteString(component.GetType().FullName);
            switch (component)
            {
                case GameplayModifierComponentDefinition modifier:
                    writer.WriteString(modifier.AttributeId.Value);
                    writer.WriteInt32((int)modifier.Application);
                    writer.WriteInt32((int)modifier.Operation);
                    WriteMagnitude(writer, modifier.Magnitude, source, $"Component[{index}].Magnitude");
                    writer.WriteInt32(modifier.Priority);
                    writer.WriteInt32((int)modifier.ClampBound);
                    writer.WriteBoolean(modifier.ScaleWithStack);
                    break;
                case GrantedTagsComponentDefinition granted:
                    WriteTags(writer, granted.Tags);
                    break;
                case GameplayTagRequirementsComponentDefinition tags:
                    writer.WriteInt32((int)tags.Phase);
                    WriteQuery(writer, tags.Source);
                    WriteQuery(writer, tags.Target);
                    break;
                case GameplayAttributeRequirementsComponentDefinition attributes:
                    writer.WriteInt32((int)attributes.Phase);
                    writer.WriteInt32((int)attributes.Source);
                    writer.WriteString(attributes.AttributeId.Value);
                    writer.WriteInt32((int)attributes.Comparison);
                    WriteMagnitude(writer, attributes.Threshold, source, $"Component[{index}].Threshold");
                    break;
                case GameplayEffectExecutionComponentDefinition execution:
                    writer.WriteInt32(execution.Mutations.Count);
                    for (int i = 0; i < execution.Mutations.Count; i++)
                    {
                        GameplayExecutionMutationDefinition mutation = execution.Mutations[i] ??
                            throw new InvalidOperationException($"Execution mutation #{i} is missing.");
                        writer.WriteString(mutation.AttributeId.Value);
                        writer.WriteInt32((int)mutation.Operation);
                        WriteMagnitude(writer, mutation.Magnitude, source, $"Component[{index}].Mutation[{i}]");
                        writer.WriteInt32((int)mutation.ClampBound);
                    }
                    break;
                case AdditionalEffectsComponentDefinition additional:
                    writer.WriteInt32(additional.Effects.Count);
                    for (int i = 0; i < additional.Effects.Count; i++)
                    {
                        GameplayAdditionalEffectDefinition child = additional.Effects[i] ??
                            throw new InvalidOperationException($"Additional Effect #{i} is missing.");
                        writer.WriteInt32((int)child.Trigger);
                        writer.WriteString(child.Effect ? child.Effect.EffectId.Value : string.Empty);
                        writer.WriteInt32(child.ParameterBindings.Count);
                        for (int bindingIndex = 0; bindingIndex < child.ParameterBindings.Count; bindingIndex++)
                        {
                            GameplayAdditionalEffectParameterBindingDefinition binding = child.ParameterBindings[bindingIndex] ??
                                throw new InvalidOperationException($"Additional Effect binding #{bindingIndex} is missing.");
                            writer.WriteString(binding.ChildParameterId);
                            writer.WriteInt32((int)binding.Source);
                            writer.WriteString(binding.ParentParameterId);
                            writer.WriteNumber(binding.Constant, $"{source}/Component[{index}].Effect[{i}].Binding[{bindingIndex}]");
                        }
                    }
                    break;
                case GameplayAttackCollisionComponentDefinition collision:
                    writer.WriteInt32((int)collision.Kind);
                    writer.WriteNumber(collision.CenterOffset.x, $"{source}/Component[{index}].CenterOffset.x");
                    writer.WriteNumber(collision.CenterOffset.y, $"{source}/Component[{index}].CenterOffset.y");
                    writer.WriteNumber(collision.CenterOffset.z, $"{source}/Component[{index}].CenterOffset.z");
                    writer.WriteNumber(collision.Width, $"{source}/Component[{index}].Width");
                    writer.WriteNumber(collision.Height, $"{source}/Component[{index}].Height");
                    writer.WriteNumber(collision.Depth, $"{source}/Component[{index}].Depth");
                    writer.WriteNumber(collision.FanAngle, $"{source}/Component[{index}].FanAngle");
                    writer.WriteNumber(collision.Radius, $"{source}/Component[{index}].Radius");
                    writer.WriteNumber(collision.InvalidRadius, $"{source}/Component[{index}].InvalidRadius");
                    writer.WriteNumber(collision.InvalidAngle, $"{source}/Component[{index}].InvalidAngle");
                    writer.WriteInt32(collision.FollowDirectionType);
                    writer.WriteInt32(collision.CoreDistance);
                    writer.WriteBoolean(collision.IsSubtractive);
                    writer.WriteNumber(collision.HitInterval, $"{source}/Component[{index}].HitInterval");
                    writer.WriteInt32(collision.AliveMaxHitCount);
                    writer.WriteInt32(collision.UnitMaxHitCount);
                    writer.WriteBoolean(collision.FollowAttacker);
                    break;
                case GameplayAttackPropertyComponentDefinition attack:
                    writer.WriteString(attack.SourceKey);
                    writer.WriteInt32(attack.HitType);
                    writer.WriteInt32(attack.HitStrengthType);
                    writer.WriteInt32(attack.CombatTags.Count);
                    for (int i = 0; i < attack.CombatTags.Count; i++)
                        writer.WriteString(attack.CombatTags[i]);
                    WriteMagnitude(writer, attack.DamagePercentage, source, $"Component[{index}].DamagePercentage");
                    WriteMagnitude(writer, attack.AddedDamage, source, $"Component[{index}].AddedDamage");
                    WriteMagnitude(writer, attack.BreakStunRatio, source, $"Component[{index}].BreakStunRatio");
                    WriteMagnitude(writer, attack.ElementAccumulation, source, $"Component[{index}].ElementAccumulation");
                    WriteMagnitude(writer, attack.ExhaustedAccumulation, source, $"Component[{index}].ExhaustedAccumulation");
                    WriteMagnitude(writer, attack.ExhaustedChase, source, $"Component[{index}].ExhaustedChase");
                    writer.WriteInt32(attack.DamageElement);
                    writer.WriteInt32(attack.DamageHitType);
                    writer.WriteInt32(attack.DamageBreakLevel);
                    WriteMagnitude(writer, attack.DamageBreakLevelProbability, source, $"Component[{index}].DamageBreakLevelProbability");
                    writer.WriteInt32(attack.TriggerBuffLevel);
                    writer.WriteInt32(attack.DestructionClass);
                    writer.WriteInt32(attack.DestructionDurability);
                    writer.WriteInt32(attack.OverrideDamageStaggerLevel);
                    writer.WriteInt32(attack.DamageTextId);
                    writer.WriteInt32(attack.DamageTextWaitMilliseconds);
                    writer.WriteInt32(attack.FrameHalt);
                    writer.WriteInt32(attack.AttackerFrameHalt);
                    writer.WriteUInt32(attack.GroundHitEffectId);
                    writer.WriteUInt32(attack.SkyHitEffectId);
                    writer.WriteUInt32(attack.DownHitEffectId);
                    writer.WriteString(attack.StandardConfigKey);
                    writer.WriteString(attack.AbilityTargetKey);
                    writer.WriteBoolean(attack.IsCauseStun);
                    writer.WriteBoolean(attack.IsHeavyAttack);
                    writer.WriteBoolean(attack.IsCauseExhausted);
                    writer.WriteBoolean(attack.IsHeal);
                    writer.WriteBoolean(attack.IsIndirect);
                    writer.WriteBoolean(attack.HitsEnemy);
                    writer.WriteBoolean(attack.HitsAllied);
                    writer.WriteBoolean(attack.HitsNeutral);
                    writer.WriteBoolean(attack.UseAbilityTargetKey);
                    writer.WriteBoolean(attack.BanDamage);
                    break;
                case GameplayCueBindingComponentDefinition cue:
                    writer.WriteString(cue.CueId);
                    writer.WriteInt32((int)cue.Trigger);
                    break;
                default:
                    throw new InvalidOperationException($"Gameplay Effect component '{component.GetType().FullName}' has no portable compiler.");
            }
        }

        static void WriteMagnitude(
            SemanticDataWriter writer,
            GameplayMagnitudeDefinition magnitude,
            string source,
            string field)
        {
            if (magnitude == null)
                throw new InvalidOperationException($"Magnitude '{field}' is missing.");
            writer.WriteInt32((int)magnitude.Source);
            writer.WriteNumber(magnitude.Constant, $"{source}/{field}.Constant");
            writer.WriteString(magnitude.SetByCallerParameterId);
            writer.WriteString(magnitude.AttributeId.Value);
            writer.WriteNumber(magnitude.Coefficient, $"{source}/{field}.Coefficient");
            writer.WriteNumber(magnitude.PostAdd, $"{source}/{field}.PostAdd");
        }

        static void WriteQuery(SemanticDataWriter writer, GameplayTagQuery query)
        {
            if (query == null)
                throw new InvalidOperationException("Gameplay Tag query is missing.");
            WriteTags(writer, query.All);
            WriteTags(writer, query.Any);
            WriteTags(writer, query.None);
        }

        static void WriteTags(SemanticDataWriter writer, IReadOnlyList<GameplayTagId> tags)
        {
            writer.WriteInt32(tags.Count);
            for (int i = 0; i < tags.Count; i++)
                writer.WriteString(tags[i].Value);
        }

        static string SimulationIdentityValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Gameplay Effect source identity is required.", nameof(value));
            return value.Trim();
        }
    }

    public sealed partial class CharacterPipelineDefinition
    {
        public CharacterGameplayEffectRuntimeBinding BuildGameplayEffectRuntimeBinding()
        {
            CharacterGameplayEffectProfile profile = GameplayEffectProfile ??
                throw new InvalidOperationException("Character Pipeline Definition Gameplay Effect profile is missing.");
            var errors = new List<string>();
            if (!profile.CollectConfigurationErrors(out _, errors))
                throw new InvalidOperationException(string.Join(" ", errors));

            using var writer = new CanonicalWriter();
            writer.WriteInt32(CharacterGameplayEffectRuntimeBinding.CatalogFormatVersion);
            IReadOnlyList<GameplayTagDefinition> tagDefinitions = profile.TagCatalog.Tags
                .Where(value => value != null)
                .OrderBy(value => value.TagId.Value, StringComparer.Ordinal)
                .ToArray();
            var initialTags = new HashSet<string>(profile.InitialTags.Select(value => value.Value), StringComparer.Ordinal);
            writer.WriteInt32(tagDefinitions.Count);
            for (int i = 0; i < tagDefinitions.Count; i++)
            {
                GameplayTagDefinition tag = tagDefinitions[i];
                writer.WriteString(tag.TagId.Value);
                writer.WriteString(tag.ParentTag.Value);
                writer.WriteBoolean(initialTags.Contains(tag.TagId.Value));
            }

            IReadOnlyList<GameplayAttributeDefinition> attributeDefinitions = profile.AttributeDefinitions
                .Where(value => value != null)
                .OrderBy(value => value.AttributeId.Value, StringComparer.Ordinal)
                .ToArray();
            var initialAttributes = new Dictionary<string, InitialGameplayAttributeValue>(StringComparer.Ordinal);
            for (int i = 0; i < profile.InitialAttributes.Count; i++)
            {
                InitialGameplayAttributeValue initial = profile.InitialAttributes[i];
                if (initial?.Definition)
                    initialAttributes.Add(initial.Definition.AttributeId.Value, initial);
            }
            writer.WriteInt32(attributeDefinitions.Count);
            for (int i = 0; i < attributeDefinitions.Count; i++)
            {
                GameplayAttributeDefinition attribute = attributeDefinitions[i];
                if (!initialAttributes.TryGetValue(attribute.AttributeId.Value, out InitialGameplayAttributeValue initial))
                    throw new InvalidOperationException($"Gameplay Attribute '{attribute.AttributeId}' has no initial value.");
                writer.WriteString(attribute.AttributeId.Value);
                writer.WriteDouble(initial.BaseValue);
                WriteBound(writer, attribute.Minimum);
                WriteBound(writer, attribute.Maximum);
            }

            IReadOnlyList<GameplayEffectDefinition> effects = profile.EffectDefinitions
                .Where(value => value != null)
                .OrderBy(value => value.EffectId.Value, StringComparer.Ordinal)
                .ToArray();
            writer.WriteInt32(effects.Count);
            for (int i = 0; i < effects.Count; i++)
            {
                GameplayEffectDefinition effect = effects[i];
                string source = $"character-gameplay-effect:{profile.name}/effect:{effect.EffectId.Value}";
                writer.WriteString(effect.EffectId.Value);
                writer.WriteUInt32(effect.DefinitionRevision);
                WriteTags(writer, effect.Tags);
                writer.WriteBytes(LowerDefinition(GameplayEffectRuntimeDefinitionCodec.EncodeDefinition(effect, source)));
            }

            byte[] catalogBytes = writer.ToArray();
            string sourceIdentity = $"character-gameplay-effect-profile:{profile.name}";
            return new CharacterGameplayEffectRuntimeBinding(
                sourceIdentity,
                SimulationCanonicalPayloadHash.Compute(catalogBytes),
                CharacterGameplayEffectRuntimeBinding.ContractSemanticVersion,
                catalogBytes);
        }

        static void WriteBound(CanonicalWriter writer, GameplayAttributeBoundDefinition bound)
        {
            writer.WriteBoolean(bound != null && bound.Enabled);
            if (bound == null || !bound.Enabled)
                return;
            writer.WriteInt32((int)bound.Source);
            switch (bound.Source)
            {
                case GameplayAttributeBoundSource.Constant:
                    writer.WriteDouble(bound.Constant);
                    break;
                case GameplayAttributeBoundSource.Attribute:
                    writer.WriteString(bound.AttributeId.Value);
                    break;
                default:
                    throw new InvalidOperationException($"Gameplay Attribute bound source '{bound.Source}' is invalid.");
            }
        }

        static void WriteTags(CanonicalWriter writer, IReadOnlyList<GameplayTagId> tags)
        {
            writer.WriteInt32(tags.Count);
            for (int i = 0; i < tags.Count; i++)
                writer.WriteString(tags[i].Value);
        }

        static byte[] LowerDefinition(SemanticDataDocument document)
        {
            using var writer = new CanonicalWriter();
            for (int i = 0; i < document.Tokens.Count; i++)
            {
                SemanticDataToken token = document.Tokens[i];
                switch (token.Kind)
                {
                    case SemanticDataTokenKind.Boolean: writer.WriteBoolean(token.Boolean); break;
                    case SemanticDataTokenKind.Int32: writer.WriteInt32(token.Int32); break;
                    case SemanticDataTokenKind.UInt32: writer.WriteUInt32(token.UInt32); break;
                    case SemanticDataTokenKind.UInt64: writer.WriteUInt64(token.UInt64); break;
                    case SemanticDataTokenKind.String: writer.WriteString(token.Text); break;
                    case SemanticDataTokenKind.Number: writer.WriteDouble(token.Number); break;
                    case SemanticDataTokenKind.Bytes: writer.WriteBytes(token.Bytes.ToArray()); break;
                    default: throw new InvalidOperationException($"Gameplay Effect definition token '{token.Kind}' is unsupported.");
                }
            }
            return writer.ToArray();
        }
    }
}
