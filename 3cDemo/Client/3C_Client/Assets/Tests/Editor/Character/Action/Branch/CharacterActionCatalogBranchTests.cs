using System;
using System.Reflection;
using NUnit.Framework;
using ThirdPersonAction;
using ThirdPersonCharacterStateMachine;
using ThirdPersonInput;
using UnityEngine;

namespace Tests.Editor.Character.Action.Branch
{
    public sealed class CharacterActionCatalogBranchTests
    {
        [Test]
        public void RuntimeDefinitionCarriesExplicitActionBranch()
        {
            ActionBranchDefinition branch = CreateDodgeBranch();
            CharacterActionDefinition definition = new CharacterActionDefinition(
                ActionStateIds.Dodge,
                ActionRequestType.Dodge,
                InputRequestKind.Dodge,
                CharacterStateIds.Dodge,
                10,
                20,
                CreateDirectionalDodge(),
                CreateBackstepDodge(),
                branch);

            Assert.True(definition.TryGetActionBranch(out ActionBranchDefinition resolved));
            Assert.AreEqual(branch.BranchId, resolved.BranchId);
            Assert.AreEqual(ActionNodeKind.Timeline, resolved.RootNode.Kind);
        }

        [Test]
        public void CatalogSoBuildsExplicitActionBranchIntoRuntimeCatalog()
        {
            CharacterActionDefinitionSO definitionAsset = CreateDodgeAsset(CreateRequiredDodgeTimeline());
            CharacterActionCatalogSO catalogAsset = ScriptableObject.CreateInstance<CharacterActionCatalogSO>();
            SetField(catalogAsset, "definitions", new[] { definitionAsset });

            CharacterActionCatalogValidationResult validation = catalogAsset.Validate();
            CharacterActionCatalog catalog = catalogAsset.ToCatalog();

            Assert.False(validation.HasErrors, validation.DescribeErrors());
            Assert.True(catalog.TryGetActionBranch(ActionStateIds.Dodge, out ActionBranchDefinition branch));
            Assert.True(branch.CanEvaluate);
            Assert.AreEqual("action.dodge", branch.BranchId.Value);
            Assert.AreEqual("timeline.dodge", branch.RootNode.NodeId.Value);
        }

        [Test]
        public void RequiredTimelineReportsErrorWithoutUsingDodgeVariantFields()
        {
            ActionBranchTimelineAuthoring requiredWithoutTracks = new ActionBranchTimelineAuthoring(
                true,
                "action.dodge",
                "timeline.dodge",
                12,
                BodyOccupancyKind.FullBody,
                CharacterFrameOutputChannel.Motion | CharacterFrameOutputChannel.Animation,
                Array.Empty<ActionTimelineTrackAuthoring>());
            CharacterActionDefinitionSO definitionAsset = CreateDodgeAsset(requiredWithoutTracks);

            CharacterActionCatalogValidationResult validation = definitionAsset.Validate();
            CharacterActionDefinition definition = definitionAsset.ToDefinition();

            Assert.True(validation.HasErrors);
            CollectionAssert.Contains(validation.Errors, "Dodge action branch timeline is required.");
            Assert.False(definition.TryGetActionBranch(out _));
        }

        static CharacterActionDefinitionSO CreateDodgeAsset(ActionBranchTimelineAuthoring timeline)
        {
            CharacterActionDefinitionSO asset = ScriptableObject.CreateInstance<CharacterActionDefinitionSO>();
            asset.name = "Dodge";
            SetField(asset, "actionStateId", ActionStateIds.Dodge.Value);
            SetField(asset, "requestType", ActionRequestType.Dodge);
            SetField(asset, "sourceInputKind", InputRequestKind.Dodge);
            SetField(asset, "motionSourceStateId", CharacterStateIds.Dodge.Value);
            SetField(asset, "priority", 10);
            SetField(asset, "resistance", 20);
            SetField(asset, "directionalDodge", CreateDirectionalDodgeAuthoring());
            SetField(asset, "backstepDodge", CreateBackstepDodgeAuthoring());
            SetField(asset, "actionBranchTimeline", timeline);
            return asset;
        }

        static ActionBranchDefinition CreateDodgeBranch()
        {
            return ActionBranchDefinition.Define(
                "action.dodge",
                ActionStateIds.Dodge,
                ActionNodeDefinition.Timeline("timeline.dodge", CreateTimeline()),
                BodyOccupancyClaim.FullBodyAction(0));
        }

        static ActionBranchTimelineAuthoring CreateRequiredDodgeTimeline()
        {
            return new ActionBranchTimelineAuthoring(
                true,
                "action.dodge",
                "timeline.dodge",
                12,
                BodyOccupancyKind.FullBody,
                CharacterFrameOutputChannel.Motion | CharacterFrameOutputChannel.Animation,
                new[]
                {
                    new ActionTimelineTrackAuthoring(
                        ActionTimelineTrackKind.Animation,
                        new[]
                        {
                            new ActionTimelineClipAuthoring(
                                ActionTimelineClipKind.AnimationKey,
                                0,
                                12,
                                ActionTimelineClipPayloadAuthoring.Animation(ActionAnimationKeys.DodgeDirectional.Value))
                        }),
                    new ActionTimelineTrackAuthoring(
                        ActionTimelineTrackKind.Motion,
                        new[]
                        {
                            new ActionTimelineClipAuthoring(
                                ActionTimelineClipKind.Motion,
                                0,
                                12,
                                ActionTimelineClipPayloadAuthoring.Motion(
                                    CharacterStateIds.Dodge.Value,
                                    CharacterStateVariant.Directional,
                                    0.42f,
                                    5.5f,
                                    true,
                                    false))
                        })
                });
        }

        static ActionTimelineDefinition CreateTimeline()
        {
            return new ActionTimelineDefinition(
                ActionStateIds.Dodge,
                12,
                new[]
                {
                    new ActionTimelineTrackDefinition(
                        ActionTimelineTrackKind.Animation,
                        new[]
                        {
                            new ActionTimelineClipDefinition(
                                ActionTimelineClipKind.AnimationKey,
                                0,
                                12,
                                ActionTimelineClipPayload.Animation(ActionAnimationKeys.DodgeDirectional))
                        })
                });
        }

        static DodgeActionVariantDefinition CreateDirectionalDodge()
        {
            return new DodgeActionVariantDefinition(
                DodgeActionVariant.Directional,
                0.42f,
                5.5f,
                true,
                ActionAnimationKeys.DodgeDirectional);
        }

        static DodgeActionVariantDefinition CreateBackstepDodge()
        {
            return new DodgeActionVariantDefinition(
                DodgeActionVariant.Backstep,
                0.61f,
                2.75f,
                false,
                ActionAnimationKeys.DodgeBackstep);
        }

        static DodgeActionVariantAuthoring CreateDirectionalDodgeAuthoring()
        {
            return new DodgeActionVariantAuthoring(
                DodgeActionVariant.Directional,
                0.42f,
                5.5f,
                true,
                ActionAnimationKeys.DodgeDirectional.Value);
        }

        static DodgeActionVariantAuthoring CreateBackstepDodgeAuthoring()
        {
            return new DodgeActionVariantAuthoring(
                DodgeActionVariant.Backstep,
                0.61f,
                2.75f,
                false,
                ActionAnimationKeys.DodgeBackstep.Value);
        }

        static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, fieldName);
            field.SetValue(target, value);
        }
    }
}
