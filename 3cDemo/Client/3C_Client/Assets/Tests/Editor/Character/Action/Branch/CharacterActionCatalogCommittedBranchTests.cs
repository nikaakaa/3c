using System;
using System.Reflection;
using NUnit.Framework;
using ThirdPersonAction;
using ThirdPersonCharacterStateMachine;
using ThirdPersonInput;
using UnityEngine;

namespace Tests.Editor.Character.Action.Branch
{
    public sealed class CharacterActionCatalogCommittedBranchTests
    {
        [Test]
        public void RuntimeDefinitionCarriesExplicitCommittedActionBranch()
        {
            CommittedActionBranchDefinition branch = CreateDodgeBranch();
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

            Assert.True(definition.TryGetCommittedActionBranch(out CommittedActionBranchDefinition resolved));
            Assert.AreEqual(branch.BranchId, resolved.BranchId);
            Assert.AreEqual(CommittedActionNodeKind.Root, resolved.RootNode.Kind);
            Assert.AreEqual("timeline.dodge", resolved.RootNode.ChildIds[0].Value);
        }

        [Test]
        public void CatalogSoBuildsExplicitCommittedActionBranchIntoRuntimeCatalog()
        {
            CharacterActionDefinitionSO definitionAsset = CreateDodgeAsset(CreateRequiredDodgeBranch());
            CharacterActionCatalogSO catalogAsset = ScriptableObject.CreateInstance<CharacterActionCatalogSO>();
            SetField(catalogAsset, "definitions", new[] { definitionAsset });
            ActionTimelineCompileContext compileContext = CompileContext();

            CharacterActionCatalogValidationResult validation = catalogAsset.Validate(in compileContext);
            CharacterActionCatalog catalog = catalogAsset.ToCatalog(in compileContext);

            Assert.False(validation.HasErrors, validation.DescribeErrors());
            Assert.True(catalog.TryGetCommittedActionBranch(ActionStateIds.Dodge, out CommittedActionBranchDefinition branch));
            Assert.True(branch.CanEvaluate);
            Assert.AreEqual("action.dodge", branch.BranchId.Value);
            Assert.AreEqual(CommittedActionNodeKind.Root, branch.RootNode.Kind);
            Assert.AreEqual("branch.root.action.dodge", branch.RootNode.NodeId.Value);
            Assert.AreEqual("selector.dodge", branch.RootNode.ChildIds[0].Value);
        }

        [Test]
        public void RequiredTimelineReportsErrorWithoutUsingDodgeVariantFields()
        {
            CommittedActionBranchTimelineAuthoring requiredWithoutTracks = new CommittedActionBranchTimelineAuthoring(
                true,
                "action.dodge",
                "timeline.dodge",
                12f / 60f,
                BodyOccupancyKind.FullBody,
                CharacterFrameOutputChannel.Motion | CharacterFrameOutputChannel.Animation,
                Array.Empty<ActionTimelineTrackAuthoring>());
            CharacterActionDefinitionSO definitionAsset = CreateDodgeAsset(CreateRequiredDodgeBranch(requiredWithoutTracks));
            ActionTimelineCompileContext compileContext = CompileContext();

            CharacterActionCatalogValidationResult validation = definitionAsset.Validate(in compileContext);
            CharacterActionDefinition definition = definitionAsset.ToDefinition(in compileContext);

            Assert.True(validation.HasErrors);
            Assert.That(validation.Errors, Has.Some.Contains("timeline is required"));
            Assert.False(definition.TryGetCommittedActionBranch(out _));
        }

        [Test]
        public void CommittedActionBranchContractsDoNotDependOnDodgeTypes()
        {
            AssertContractHasNoDodgeMembers(typeof(CommittedActionBranchDefinition));
            AssertContractHasNoDodgeMembers(typeof(CommittedActionNodeDefinition));
            AssertContractHasNoDodgeMembers(typeof(CommittedActionBranchOutcome));
        }

        static CharacterActionDefinitionSO CreateDodgeAsset(CommittedActionBranchAuthoring branch)
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
            SetField(asset, "committedActionBranch", branch);
            return asset;
        }

        static CommittedActionBranchDefinition CreateDodgeBranch()
        {
            CommittedActionNodeDefinition timeline =
                CommittedActionNodeDefinition.Timeline("timeline.dodge", CreateTimeline());
            return CommittedActionBranchDefinition.Define(
                "action.dodge",
                ActionStateIds.Dodge,
                CommittedActionNodeDefinition.Root("branch.root.action.dodge", timeline.NodeId),
                BodyOccupancyClaim.CommittedActionFullBody(0),
                new[] { timeline });
        }

        static CommittedActionBranchTimelineAuthoring CreateRequiredDodgeTimeline()
        {
            return new CommittedActionBranchTimelineAuthoring(
                true,
                "action.dodge",
                "timeline.dodge",
                12f / 60f,
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
                                0f,
                                12f / 60f,
                                ActionTimelineClipPayloadAuthoring.Animation(ActionAnimationKeys.DodgeDirectional.Value))
                        }),
                    new ActionTimelineTrackAuthoring(
                        ActionTimelineTrackKind.Motion,
                        new[]
                        {
                            new ActionTimelineClipAuthoring(
                                ActionTimelineClipKind.Motion,
                                0f,
                                12f / 60f,
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

        static CommittedActionBranchAuthoring CreateRequiredDodgeBranch()
        {
            return CreateRequiredDodgeBranch(CreateRequiredDodgeTimeline());
        }

        static CommittedActionBranchAuthoring CreateRequiredDodgeBranch(CommittedActionBranchTimelineAuthoring timeline)
        {
            return new CommittedActionBranchAuthoring(
                1,
                true,
                "action.dodge",
                "branch.root.action.dodge",
                BodyOccupancyKind.FullBody,
                CharacterFrameOutputChannel.Motion | CharacterFrameOutputChannel.Animation,
                new[]
                {
                    CommittedActionBranchNodeAuthoring.Root(
                        "branch.root.action.dodge",
                        "selector.dodge",
                        Vector2.zero),
                    CommittedActionBranchNodeAuthoring.Selector(
                        "selector.dodge",
                        new[] { "condition.directional", "condition.backstep" },
                        new Vector2(1f, 0f)),
                    CommittedActionBranchNodeAuthoring.ConditionNode(
                        "condition.directional",
                        new CommittedActionBranchConditionAuthoring(
                            CommittedActionConditionKind.ActionVariantEquals,
                            CharacterStateVariant.Directional,
                            false),
                        new[] { "timeline.directional" },
                        new Vector2(1f, 0f)),
                    CommittedActionBranchNodeAuthoring.ConditionNode(
                        "condition.backstep",
                        new CommittedActionBranchConditionAuthoring(
                            CommittedActionConditionKind.ActionVariantEquals,
                            CharacterStateVariant.Backstep,
                            false),
                        new[] { "timeline.backstep" },
                        new Vector2(1f, 1f)),
                    CommittedActionBranchNodeAuthoring.TimelineNode(
                        "timeline.directional",
                        timeline,
                        new Vector2(2f, 0f)),
                    CommittedActionBranchNodeAuthoring.TimelineNode(
                        "timeline.backstep",
                        timeline,
                        new Vector2(2f, 1f))
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

        static ActionTimelineCompileContext CompileContext()
        {
            return new ActionTimelineCompileContext(1f / 60f);
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

        static void AssertContractHasNoDodgeMembers(Type type)
        {
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < fields.Length; i++)
                Assert.False(fields[i].FieldType.Name.Contains("Dodge"), $"{type.Name}.{fields[i].Name}");

            PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < properties.Length; i++)
                Assert.False(properties[i].PropertyType.Name.Contains("Dodge"), $"{type.Name}.{properties[i].Name}");
        }
    }
}
