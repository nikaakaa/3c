using System;
using System.Reflection;
using NUnit.Framework;
using ThirdPersonAction;
using ThirdPersonCharacterStateMachine;
using ThirdPersonInput;
using UnityEngine;

namespace Tests.Editor.Character.Action.Branch
{
    public sealed class ActionLifecycleBranchTests
    {
        [Test]
        public void AcceptedActionEvaluatesActionBranchAtFrameZero()
        {
            FullBodyActionRuntimeModule module = new FullBodyActionRuntimeModule();
            CharacterActionCatalog catalog = CreateCatalog(CreateDefinition(
                ActionStateIds.Dodge,
                ActionRequestType.Dodge,
                InputRequestKind.Dodge,
                "action.dodge",
                ActionAnimationKeys.DodgeDirectional));
            CharacterResolvedAction action = CreateResolvedAction(
                ActionStateIds.Dodge,
                ActionRequestType.Dodge,
                InputRequestKind.Dodge,
                CharacterFrameRequestProviderId.Dodge,
                ActionAnimationKeys.DodgeDirectional,
                CharacterStateVariant.Directional,
                10);

            ActionLifecycleFrame frame = module.TickActionLifecycle(in action, in catalog, 0.02f, 10);

            Assert.True(frame.HasActionBranchOutcome);
            Assert.AreEqual(0, frame.ActionBranchOutcome.TimelineOutcome.CurrentFrame);
            Assert.True(frame.ActionBranchOutcome.TimelineOutcome.HasAnimation);
            Assert.True(frame.ActionBranchOutcome.BodyClaim.ClaimsFullBody);
        }

        [Test]
        public void TimelineOutcomeOverridesResolvedActionMotionAndAnimation()
        {
            ActionAnimationKey timelineKey = new ActionAnimationKey("Action.Dodge.Timeline");
            FullBodyActionRuntimeModule module = new FullBodyActionRuntimeModule();
            CharacterActionCatalog catalog = CreateCatalog(CreateDefinition(
                ActionStateIds.Dodge,
                ActionRequestType.Dodge,
                InputRequestKind.Dodge,
                "action.dodge",
                timelineKey));
            CharacterResolvedAction action = CreateResolvedAction(
                ActionStateIds.Dodge,
                ActionRequestType.Dodge,
                InputRequestKind.Dodge,
                CharacterFrameRequestProviderId.Dodge,
                ActionAnimationKeys.DodgeBackstep,
                CharacterStateVariant.Directional,
                10);

            ActionLifecycleFrame frame = module.TickActionLifecycle(in action, in catalog, 0.02f, 10);

            Assert.True(frame.HasAnimationRequest);
            Assert.AreEqual(timelineKey, frame.AnimationRequest.Key);
            Assert.AreEqual(8f, frame.MotionSpec.Distance);
            Assert.AreEqual(0.02f, frame.MotionSpec.StateTime);
        }

        [Test]
        public void ContinuingActionEvaluatesNextActionBranchFrame()
        {
            FullBodyActionRuntimeModule module = new FullBodyActionRuntimeModule();
            CharacterActionCatalog catalog = CreateCatalog(CreateDefinition(
                ActionStateIds.Dodge,
                ActionRequestType.Dodge,
                InputRequestKind.Dodge,
                "action.dodge",
                ActionAnimationKeys.DodgeDirectional));
            CharacterResolvedAction action = CreateResolvedAction(
                ActionStateIds.Dodge,
                ActionRequestType.Dodge,
                InputRequestKind.Dodge,
                CharacterFrameRequestProviderId.Dodge,
                ActionAnimationKeys.DodgeDirectional,
                CharacterStateVariant.Directional,
                10);
            CharacterResolvedAction none = default;

            module.TickActionLifecycle(in action, in catalog, 0.02f, 10);
            ActionLifecycleFrame next = module.TickActionLifecycle(in none, in catalog, 0.02f, 11);

            Assert.True(next.HasActionBranchOutcome);
            Assert.AreEqual(1, next.ActionBranchOutcome.TimelineOutcome.CurrentFrame);
            CollectionAssert.Contains(next.ActionBranchOutcome.TimelineOutcome.ActiveWindowFactIds, "window.action.dodge");
        }

        [Test]
        public void NewAcceptedActionSwitchesActionBranch()
        {
            ActionStateId attackState = new ActionStateId("Action.Attack");
            ActionAnimationKey attackKey = new ActionAnimationKey("Action.Attack.Light");
            FullBodyActionRuntimeModule module = new FullBodyActionRuntimeModule();
            CharacterActionCatalog catalog = CreateCatalog(
                CreateDefinition(
                    ActionStateIds.Dodge,
                    ActionRequestType.Dodge,
                    InputRequestKind.Dodge,
                    "action.dodge",
                    ActionAnimationKeys.DodgeDirectional),
                CreateDefinition(
                    attackState,
                    ActionRequestType.Attack,
                    InputRequestKind.Attack,
                    "action.attack",
                    attackKey));
            CharacterResolvedAction dodge = CreateResolvedAction(
                ActionStateIds.Dodge,
                ActionRequestType.Dodge,
                InputRequestKind.Dodge,
                CharacterFrameRequestProviderId.Dodge,
                ActionAnimationKeys.DodgeDirectional,
                CharacterStateVariant.Directional,
                10);
            CharacterResolvedAction attack = CreateResolvedAction(
                attackState,
                ActionRequestType.Attack,
                InputRequestKind.Attack,
                CharacterFrameRequestProviderId.Attack,
                attackKey,
                CharacterStateVariant.None,
                11);

            module.TickActionLifecycle(in dodge, in catalog, 0.02f, 10);
            ActionLifecycleFrame switched = module.TickActionLifecycle(in attack, in catalog, 0.02f, 11);

            Assert.True(switched.StartedThisFrame);
            Assert.True(switched.HasActionBranchOutcome);
            Assert.AreEqual(0, switched.ActionBranchOutcome.TimelineOutcome.CurrentFrame);
            Assert.AreEqual(attackKey, switched.ActionBranchOutcome.TimelineOutcome.AnimationKey);
        }

        [Test]
        public void CompletedActionStopsActionBranchOutputOnNextFrame()
        {
            FullBodyActionRuntimeModule module = new FullBodyActionRuntimeModule();
            CharacterActionCatalog catalog = CreateCatalog(CreateDefinition(
                ActionStateIds.Dodge,
                ActionRequestType.Dodge,
                InputRequestKind.Dodge,
                "action.dodge",
                ActionAnimationKeys.DodgeDirectional));
            CharacterResolvedAction action = CreateResolvedAction(
                ActionStateIds.Dodge,
                ActionRequestType.Dodge,
                InputRequestKind.Dodge,
                CharacterFrameRequestProviderId.Dodge,
                ActionAnimationKeys.DodgeDirectional,
                CharacterStateVariant.Directional,
                10);
            ActionLifecycleFrame entered = module.TickActionLifecycle(in action, in catalog, 0.02f, 10);
            ActionMotionResolveResult completed = new ActionMotionResolveResult(
                entered.MotionSpec,
                default,
                false,
                true,
                false,
                10,
                "complete");
            CharacterResolvedAction none = default;

            module.CompleteActionLifecycle(in completed, false);
            ActionLifecycleFrame next = module.TickActionLifecycle(in none, in catalog, 0.02f, 11);

            Assert.True(next.ExitedThisFrame);
            Assert.False(next.HasAction);
            Assert.False(next.HasActionBranchOutcome);
        }

        [Test]
        public void ActionOutputSubmissionCarriesActionBranchWindowFactsAsPureData()
        {
            ActionBranchOutcome outcome = new ActionBranchOutcome(
                new ActionTimelineOutcome(
                    1,
                    12,
                    default,
                    false,
                    ActionMotionSpec.None(12),
                    false,
                    new[] { "window.action.dodge" },
                    Array.Empty<ActionCueRequest>()),
                CharacterFrameCandidateOutput.FullBodyAction(false, false, 12),
                BodyOccupancyClaim.FullBodyAction(12),
                12);

            CharacterFrameActionOutputSubmission submission = new CharacterFrameActionOutputSubmission(
                default,
                false,
                false,
                default,
                false,
                12,
                outcome);

            Assert.True(submission.HasActionBranchOutcome);
            CollectionAssert.Contains(submission.ActionTimelineOutcome.ActiveWindowFactIds, "window.action.dodge");
        }

        [Test]
        public void FullBodyActionFrameSubmitterBuildsActionOutputWithActionBranchOutcome()
        {
            ActionBranchOutcome outcome = new ActionBranchOutcome(
                new ActionTimelineOutcome(
                    1,
                    12,
                    default,
                    false,
                    ActionMotionSpec.None(12),
                    false,
                    new[] { "window.action.dodge" },
                    Array.Empty<ActionCueRequest>()),
                CharacterFrameCandidateOutput.FullBodyAction(false, false, 12),
                BodyOccupancyClaim.FullBodyAction(12),
                12);
            ActionLifecycleFrame frame = new ActionLifecycleFrame(
                default,
                ActionMotionSpec.None(12),
                default,
                false,
                false,
                false,
                12,
                outcome);
            CharacterInputRequestFact request = CharacterInputRequestFact.None(InputRequestKind.Dodge);
            MethodInfo method = typeof(FullBodyActionFrameSubmitter).GetMethod(
                "BuildActionOutput",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.NotNull(method);
            object[] args = { frame, request, 12 };
            CharacterFrameActionOutputSubmission output =
                (CharacterFrameActionOutputSubmission)method.Invoke(null, args);

            Assert.True(output.HasActionBranchOutcome);
            CollectionAssert.Contains(output.ActionTimelineOutcome.ActiveWindowFactIds, "window.action.dodge");
        }


        [Test]
        public void RestoreStateDoesNotCarryActionBranchRuntimeObjects()
        {
            PropertyInfo[] properties = typeof(ActionLifecycleRestoreState).GetProperties(
                BindingFlags.Instance | BindingFlags.Public);

            for (int i = 0; i < properties.Length; i++)
            {
                Assert.AreNotEqual(typeof(ActionBranchOutcome), properties[i].PropertyType);
                Assert.AreNotEqual(typeof(ActionBranchDefinition), properties[i].PropertyType);
                Assert.AreNotEqual(typeof(ActionTimelineDefinition), properties[i].PropertyType);
            }
        }

        static CharacterActionCatalog CreateCatalog(params CharacterActionDefinition[] definitions)
        {
            return new CharacterActionCatalog(definitions);
        }

        static CharacterActionDefinition CreateDefinition(
            ActionStateId actionState,
            ActionRequestType requestType,
            InputRequestKind inputKind,
            string branchId,
            ActionAnimationKey animationKey)
        {
            ActionBranchDefinition branch = ActionBranchDefinition.Define(
                branchId,
                actionState,
                ActionNodeDefinition.Timeline($"{branchId}.timeline", CreateTimeline(actionState, branchId, animationKey)),
                BodyOccupancyClaim.FullBodyAction(0));

            return new CharacterActionDefinition(
                actionState,
                requestType,
                inputKind,
                new CharacterStateId(actionState.Value),
                10,
                20,
                CreateDirectionalDodge(),
                CreateBackstepDodge(),
                branch);
        }

        static ActionTimelineDefinition CreateTimeline(
            ActionStateId actionState,
            string branchId,
            ActionAnimationKey animationKey)
        {
            return new ActionTimelineDefinition(
                actionState,
                4,
                new[]
                {
                    new ActionTimelineTrackDefinition(
                        ActionTimelineTrackKind.Animation,
                        new[]
                        {
                            new ActionTimelineClipDefinition(
                                ActionTimelineClipKind.AnimationKey,
                                0,
                                4,
                                ActionTimelineClipPayload.Animation(animationKey))
                        }),
                    new ActionTimelineTrackDefinition(
                        ActionTimelineTrackKind.Motion,
                        new[]
                        {
                            new ActionTimelineClipDefinition(
                                ActionTimelineClipKind.Motion,
                                0,
                                4,
                                ActionTimelineClipPayload.Motion(new ActionMotionSpec(
                                    actionState,
                                    new CharacterStateId(actionState.Value),
                                    CharacterStateVariant.Directional,
                                    0.25f,
                                    8f,
                                    true,
                                    false,
                                    Vector3.forward,
                                    0f,
                                    0)))
                        }),
                    new ActionTimelineTrackDefinition(
                        ActionTimelineTrackKind.Hitbox,
                        new[]
                        {
                            new ActionTimelineClipDefinition(
                                ActionTimelineClipKind.HitboxWindow,
                                1,
                                2,
                                ActionTimelineClipPayload.Fact($"window.{branchId}"))
                        })
                });
        }

        static CharacterResolvedAction CreateResolvedAction(
            ActionStateId actionState,
            ActionRequestType requestType,
            InputRequestKind inputKind,
            CharacterFrameRequestProviderId providerId,
            ActionAnimationKey animationKey,
            CharacterStateVariant variant,
            int step)
        {
            CharacterActionRequest request = new CharacterActionRequest(
                providerId,
                requestType,
                inputKind,
                step,
                step + 4,
                10,
                0,
                variant,
                Vector3.forward);
            CharacterInputRequestFact requestFact = new CharacterInputRequestFact(
                true,
                inputKind,
                step,
                step + 4,
                10,
                variant,
                Vector3.forward);
            ActionInterruptRequest interruptRequest = new ActionInterruptRequest(
                step,
                requestType,
                actionState,
                10,
                0,
                step,
                step + 4);
            ActionMotionSpec motionSpec = new ActionMotionSpec(
                actionState,
                new CharacterStateId(actionState.Value),
                variant,
                0.4f,
                4f,
                true,
                false,
                Vector3.forward,
                0f,
                step);

            return new CharacterResolvedAction(
                providerId,
                request,
                requestFact,
                interruptRequest,
                new ActionInterruptContext(ActionStateIds.None, 0f, 0, step),
                animationKey,
                motionSpec);
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
    }
}
