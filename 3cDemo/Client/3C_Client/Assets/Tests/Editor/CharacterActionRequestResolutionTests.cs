using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ThirdPersonAction;
using ThirdPersonCharacterStateMachine;
using ThirdPersonInput;
using ThirdPersonMovement;
using UnityEngine;

namespace Tests.Editor
{
    public sealed class CharacterActionRequestResolutionTests
    {
        [Test]
        public void CharacterActionRequestKeepsResolvedOutputOut()
        {
            string[] propertyNames = typeof(CharacterActionRequest)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Select(property => property.Name)
                .ToArray();

            CollectionAssert.DoesNotContain(propertyNames, "TargetState");
            CollectionAssert.DoesNotContain(propertyNames, "AnimationKey");
            CollectionAssert.DoesNotContain(propertyNames, "MotionSpec");
            CollectionAssert.DoesNotContain(propertyNames, "Animator");
            CollectionAssert.DoesNotContain(propertyNames, "Animancer");
        }

        [Test]
        public void AttackRequestProviderSubstituteOutputsRequestOnly()
        {
            InputRequestBuffer buffer = new InputRequestBuffer();
            buffer.AddRequest(InputRequestKind.Attack, InputButtonKind.Attack, 10, 3);
            CharacterActionRequestSubmissionInput input = SubmissionInput(buffer, 10, DirectionalFacts(Vector3.forward));
            BufferedInputActionRequestProvider provider = new BufferedInputActionRequestProvider(
                CharacterFrameRequestProviderId.Attack,
                ActionRequestType.Attack,
                InputRequestKind.Attack);

            bool built = provider.TryBuild(in input, 2, out CharacterActionRequest request);

            Assert.True(built);
            Assert.True(request.HasRequest);
            Assert.AreEqual(CharacterFrameRequestProviderId.Attack, request.ProviderId);
            Assert.AreEqual(ActionRequestType.Attack, request.RequestType);
            Assert.AreEqual(InputRequestKind.Attack, request.SourceInputKind);
            Assert.AreEqual(10, request.OriginStep);
            Assert.AreEqual(13, request.ExpireStep);
            Assert.AreEqual(2, request.SourceOrder);
            Assert.False(request.HasWorldDirection);
        }

        [Test]
        public void AttackResolverSubstituteOwnsTargetState()
        {
            CharacterActionRequest request = new CharacterActionRequest(
                CharacterFrameRequestProviderId.Attack,
                ActionRequestType.Attack,
                InputRequestKind.Attack,
                10,
                13,
                0,
                1,
                CharacterStateVariant.None,
                Vector3.zero);
            CharacterActionResolveContext context = ResolveContext(10, DirectionalFacts(Vector3.forward));
            FixedResolvedActionResolver resolver = new FixedResolvedActionResolver(
                ActionRequestType.Attack,
                InputRequestKind.Attack,
                new ActionStateId("Action.Attack01"),
                new ActionAnimationKey("Action.Attack.Light.01"),
                CharacterStateVariant.None);

            bool resolved = resolver.TryResolve(in request, in context, out CharacterResolvedAction action);

            Assert.True(resolved);
            Assert.True(action.HasResolvedAction);
            Assert.AreEqual("Action.Attack01", action.InterruptRequest.TargetState.Value);
            Assert.AreEqual(InputRequestKind.Attack, action.Request.SourceInputKind);
            Assert.AreEqual(InputRequestKind.Attack, action.RequestFact.RequestKind);
            Assert.AreEqual("Action.Attack.Light.01", action.AnimationKey.Value);
        }

        [Test]
        public void DodgeProviderOutputsPureRequest()
        {
            InputRequestBuffer buffer = new InputRequestBuffer();
            buffer.AddRequest(InputRequestKind.Dodge, InputButtonKind.Dodge, 2, 4);
            CharacterActionRequestSubmissionInput input = SubmissionInput(buffer, 2, DirectionalFacts(Vector3.forward));
            DodgeActionRequestProvider provider = new DodgeActionRequestProvider();

            bool built = provider.TryBuild(in input, 3, out CharacterActionRequest request);

            Assert.True(built);
            Assert.AreEqual(CharacterFrameRequestProviderId.Dodge, request.ProviderId);
            Assert.AreEqual(ActionRequestType.Dodge, request.RequestType);
            Assert.AreEqual(InputRequestKind.Dodge, request.SourceInputKind);
            Assert.AreEqual(2, request.OriginStep);
            Assert.AreEqual(6, request.ExpireStep);
            Assert.AreEqual(0, request.PriorityHint);
            Assert.AreEqual(2, request.SourceOrder);
            Assert.AreEqual(CharacterStateVariant.None, request.VariantHint);
            Assert.False(request.HasWorldDirection);
        }

        [Test]
        public void DodgeResolverPreservesDirectionalResolvedAction()
        {
            InputRequestBuffer buffer = new InputRequestBuffer();
            buffer.AddRequest(InputRequestKind.Dodge, InputButtonKind.Dodge, 2, 4);
            CharacterActionRequestSubmissionInput input = SubmissionInput(buffer, 2, DirectionalFacts(Vector3.forward));
            DodgeActionRequestProvider provider = new DodgeActionRequestProvider();
            CharacterActionResolveContext context = CharacterActionResolveContext.FromSubmissionInput(in input);
            DodgeCharacterActionRequestResolver resolver = new DodgeCharacterActionRequestResolver();

            Assert.True(provider.TryBuild(in input, 0, out CharacterActionRequest request));
            bool resolved = resolver.TryResolve(in request, in context, out CharacterResolvedAction action);

            Assert.True(resolved);
            Assert.True(action.HasResolvedAction);
            Assert.AreEqual(ActionStateIds.Dodge, action.InterruptRequest.TargetState);
            Assert.AreEqual(InputRequestKind.Dodge, action.RequestFact.RequestKind);
            Assert.AreEqual(CharacterStateVariant.Directional, action.RequestFact.Variant);
            Assert.AreEqual(Vector3.forward, action.RequestFact.WorldDirection);
            Assert.AreEqual(ActionAnimationKeys.DodgeDirectional, action.AnimationKey);
            Assert.True(action.MotionSpec.HasSpec);
            Assert.AreEqual(ActionStateIds.Dodge, action.MotionSpec.ActionState);
            Assert.AreEqual(ThirdPersonCharacterStateMachine.CharacterStateIds.Dodge, action.MotionSpec.SourceState);
            Assert.AreEqual(0f, action.MotionSpec.Duration, 0.0001f);
            Assert.AreEqual(0f, action.MotionSpec.Distance, 0.0001f);
            Assert.False(action.MotionSpec.SetRunLatchOnComplete);
        }

        [Test]
        public void DodgeResolverPreservesBackstepResolvedAction()
        {
            InputRequestBuffer buffer = new InputRequestBuffer();
            buffer.AddRequest(InputRequestKind.Dodge, InputButtonKind.Dodge, 3, 4);
            CharacterActionRequestSubmissionInput input = SubmissionInput(buffer, 3, BackstepFacts());
            DodgeActionRequestProvider provider = new DodgeActionRequestProvider();
            CharacterActionResolveContext context = CharacterActionResolveContext.FromSubmissionInput(in input);
            DodgeCharacterActionRequestResolver resolver = new DodgeCharacterActionRequestResolver();

            Assert.True(provider.TryBuild(in input, 0, out CharacterActionRequest request));
            bool resolved = resolver.TryResolve(in request, in context, out CharacterResolvedAction action);

            Assert.True(resolved);
            Assert.AreEqual(CharacterStateVariant.Backstep, action.RequestFact.Variant);
            Assert.AreEqual(Vector3.back, action.RequestFact.WorldDirection);
            Assert.AreEqual(ActionAnimationKeys.DodgeBackstep, action.AnimationKey);
            Assert.AreEqual(0f, action.MotionSpec.Duration, 0.0001f);
            Assert.AreEqual(0f, action.MotionSpec.Distance, 0.0001f);
            Assert.False(action.MotionSpec.SetRunLatchOnComplete);
        }

        [Test]
        public void DodgeResolverRejectsWhenActionCatalogMissing()
        {
            InputRequestBuffer buffer = new InputRequestBuffer();
            buffer.AddRequest(InputRequestKind.Dodge, InputButtonKind.Dodge, 3, 4);
            CharacterActionRequestSubmissionInput input = SubmissionInput(
                buffer,
                3,
                DirectionalFacts(Vector3.forward),
                null,
                CharacterActionCatalog.Empty,
                false);
            DodgeActionRequestProvider provider = new DodgeActionRequestProvider();
            CharacterActionResolveContext context = CharacterActionResolveContext.FromSubmissionInput(in input);
            DodgeCharacterActionRequestResolver resolver = new DodgeCharacterActionRequestResolver();

            Assert.True(provider.TryBuild(in input, 0, out CharacterActionRequest request));
            Assert.False(resolver.TryResolve(in request, in context, out CharacterResolvedAction action));
            Assert.False(action.HasResolvedAction);
        }

        [Test]
        public void JumpProviderResolverSubstituteUsesArbiterWithoutMainFlowBranch()
        {
            InputRequestBuffer buffer = new InputRequestBuffer();
            buffer.AddRequest(InputRequestKind.Jump, InputButtonKind.Jump, 20, 2);
            CharacterActionRequestSubmissionInput input = SubmissionInput(
                buffer,
                20,
                DirectionalFacts(Vector3.forward),
                new[] { new ActionInterruptPolicy(ActionStateIds.None, new ActionStateId("Action.Jump"), 0) });
            RequestResolvingSubmissionProvider provider = new RequestResolvingSubmissionProvider(
                new BufferedInputActionRequestProvider(
                    CharacterFrameRequestProviderId.Jump,
                    ActionRequestType.Custom,
                    InputRequestKind.Jump),
                new FixedResolvedActionResolver(
                    ActionRequestType.Custom,
                    InputRequestKind.Jump,
                    new ActionStateId("Action.Jump"),
                    new ActionAnimationKey("Action.Jump"),
                    CharacterStateVariant.None));

            CharacterActionRequestSubmissionResult result = CharacterActionRequestSubmissionArbiter.Evaluate(
                in input,
                new ICharacterFrameRequestSubmissionProvider[] { provider });

            Assert.True(result.Accepted);
            Assert.AreEqual(InputRequestKind.Jump, result.Request.RequestKind);
            Assert.AreEqual("Action.Jump", result.Decision.TargetState.Value);
            Assert.AreEqual(CharacterFrameRequestProviderId.Jump, result.RequestSubmissions.First.ProviderId);
        }

        [Test]
        public void ArbiterInjectedProvidersUsePriorityAndStableTieBreak()
        {
            CharacterActionRequestSubmissionInput input = SubmissionInput(
                new InputRequestBuffer(),
                30,
                DirectionalFacts(Vector3.forward),
                new[]
                {
                    new ActionInterruptPolicy(ActionStateIds.None, new ActionStateId("Action.Low"), 0),
                    new ActionInterruptPolicy(ActionStateIds.None, new ActionStateId("Action.High"), 0),
                    new ActionInterruptPolicy(ActionStateIds.None, new ActionStateId("Action.First"), 0),
                    new ActionInterruptPolicy(ActionStateIds.None, new ActionStateId("Action.Second"), 0)
                });

            CharacterActionRequestSubmissionResult priorityResult = CharacterActionRequestSubmissionArbiter.Evaluate(
                in input,
                new ICharacterFrameRequestSubmissionProvider[]
                {
                    new StaticSubmissionProvider(CharacterFrameRequestProviderId.Attack, InputRequestKind.Attack, ActionRequestType.Attack, new ActionStateId("Action.Low"), 10),
                    new StaticSubmissionProvider(CharacterFrameRequestProviderId.Jump, InputRequestKind.Jump, ActionRequestType.Custom, new ActionStateId("Action.High"), 20)
                });

            CharacterActionRequestSubmissionResult tieBreakResult = CharacterActionRequestSubmissionArbiter.Evaluate(
                in input,
                new ICharacterFrameRequestSubmissionProvider[]
                {
                    new StaticSubmissionProvider(CharacterFrameRequestProviderId.Attack, InputRequestKind.Attack, ActionRequestType.Attack, new ActionStateId("Action.First"), 30),
                    new StaticSubmissionProvider(CharacterFrameRequestProviderId.Jump, InputRequestKind.Jump, ActionRequestType.Custom, new ActionStateId("Action.Second"), 30)
                });

            Assert.True(priorityResult.Accepted);
            Assert.AreEqual("Action.High", priorityResult.Decision.TargetState.Value);
            Assert.True(tieBreakResult.Accepted);
            Assert.AreEqual("Action.First", tieBreakResult.Decision.TargetState.Value);
        }

        [Test]
        public void BufferedInputRequestStoresOnlyInputKeyAndTiming()
        {
            string[] propertyNames = typeof(BufferedInputRequest)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Select(property => property.Name)
                .ToArray();

            CollectionAssert.Contains(propertyNames, "Kind");
            CollectionAssert.Contains(propertyNames, "OriginStep");
            CollectionAssert.Contains(propertyNames, "ExpireStep");
            CollectionAssert.Contains(propertyNames, "Consumed");
            CollectionAssert.DoesNotContain(propertyNames, "TargetState");
            CollectionAssert.DoesNotContain(propertyNames, "AnimationKey");
            CollectionAssert.DoesNotContain(propertyNames, "MotionSpec");
        }

        [Test]
        public void DefaultArbiterKeepsAttackRequestForFutureComboResolver()
        {
            InputRequestBuffer buffer = new InputRequestBuffer();
            buffer.AddRequest(InputRequestKind.Attack, InputButtonKind.Attack, 34, 4);
            CharacterActionRequestSubmissionInput input = SubmissionInput(
                buffer,
                34,
                DirectionalFacts(Vector3.forward),
                Array.Empty<ActionInterruptPolicy>());

            CharacterActionRequestSubmissionResult result = CharacterActionRequestSubmissionArbiter.Evaluate(in input);

            Assert.False(result.Accepted);
            Assert.False(result.Request.HasRequest);
            Assert.True(buffer.TryPeek(InputRequestKind.Attack, 34, out _));
        }

        [Test]
        public void RejectedDodgeRequestDoesNotConsumeInput()
        {
            InputRequestBuffer buffer = new InputRequestBuffer();
            buffer.AddRequest(InputRequestKind.Dodge, InputButtonKind.Dodge, 40, 4);
            CharacterActionRequestSubmissionInput input = SubmissionInput(
                buffer,
                40,
                DirectionalFacts(Vector3.forward),
                new[] { new ActionInterruptPolicy(ActionStateIds.None, ActionStateIds.Dodge, 99) });

            CharacterActionRequestSubmissionResult result = CharacterActionRequestSubmissionArbiter.Evaluate(in input);

            Assert.False(result.Accepted);
            Assert.True(buffer.TryPeek(InputRequestKind.Dodge, 40, out BufferedInputRequest request));
            Assert.False(request.Consumed);
        }

        static CharacterActionRequestSubmissionInput SubmissionInput(
            InputRequestBuffer buffer,
            int step,
            LocomotionDecisionFacts facts,
            IReadOnlyList<ActionInterruptPolicy> policies = null,
            CharacterActionCatalog? actionCatalog = null,
            bool hasActionCatalog = true)
        {
            BasicLocomotionInputSnapshot locomotionInput = new BasicLocomotionInputSnapshot(
                0.02f,
                facts.MoveIntent.RawInput,
                Vector2.zero);
            CharacterActionCatalog catalog = actionCatalog ?? Catalog();
            return new CharacterActionRequestSubmissionInput(
                buffer,
                step,
                CharacterStateMachineSnapshot.Inactive,
                locomotionInput,
                false,
                facts,
                default,
                hasActionCatalog,
                catalog,
                0,
                policies ?? new[] { new ActionInterruptPolicy(ActionStateIds.None, ActionStateIds.Dodge, 0) });
        }

        static CharacterActionResolveContext ResolveContext(int step, LocomotionDecisionFacts facts)
        {
            BasicLocomotionInputSnapshot locomotionInput = new BasicLocomotionInputSnapshot(
                0.02f,
                facts.MoveIntent.RawInput,
                Vector2.zero);
            return new CharacterActionResolveContext(
                step,
                CharacterStateMachineSnapshot.Inactive,
                in locomotionInput,
                false,
                in facts,
                default,
                true,
                Catalog(),
                0);
        }

        static CharacterActionCatalog Catalog()
        {
            DodgeActionTuning config = CatalogDodgeTuning();
            return new CharacterActionCatalog(new[]
            {
                new CharacterActionDefinition(
                    ActionStateIds.Dodge,
                    ActionRequestType.Dodge,
                    InputRequestKind.Dodge,
                    CharacterStateIds.Dodge,
                    config.Priority,
                    config.Resistance,
                    new DodgeActionVariantDefinition(
                        DodgeActionVariant.Directional,
                        config.DirectionalDuration,
                        config.DirectionalDistance,
                        config.DirectionalRotateToDirection,
                        ActionAnimationKeys.DodgeDirectional),
                    new DodgeActionVariantDefinition(
                        DodgeActionVariant.Backstep,
                        config.BackstepDuration,
                        config.BackstepDistance,
                        config.BackstepRotateToDirection,
                        ActionAnimationKeys.DodgeBackstep),
                    CreateDodgeBranch(config))
            });
        }

        static CommittedActionBranchDefinition CreateDodgeBranch(DodgeActionTuning config)
        {
            CommittedActionNodeDefinition directionalCondition = CommittedActionNodeDefinition.ConditionNode(
                "condition.directional",
                CommittedActionConditionDefinition.ActionVariant(CharacterStateVariant.Directional),
                new CommittedActionNodeId("timeline.directional"));
            CommittedActionNodeDefinition backstepCondition = CommittedActionNodeDefinition.ConditionNode(
                "condition.backstep",
                CommittedActionConditionDefinition.ActionVariant(CharacterStateVariant.Backstep),
                new CommittedActionNodeId("timeline.backstep"));
            return CommittedActionBranchDefinition.Define(
                "action.dodge",
                ActionStateIds.Dodge,
                CommittedActionNodeDefinition.Selector(
                    "selector.dodge",
                    directionalCondition.NodeId,
                    backstepCondition.NodeId),
                BodyOccupancyClaim.CommittedActionFullBody(0),
                new[]
                {
                    directionalCondition,
                    backstepCondition,
                    CommittedActionNodeDefinition.Timeline(
                        "timeline.directional",
                        CreateTimeline(ActionAnimationKeys.DodgeDirectional, CharacterStateVariant.Directional, config.DirectionalDuration, config.DirectionalDistance, true, true)),
                    CommittedActionNodeDefinition.Timeline(
                        "timeline.backstep",
                        CreateTimeline(ActionAnimationKeys.DodgeBackstep, CharacterStateVariant.Backstep, config.BackstepDuration, config.BackstepDistance, false, false))
                });
        }

        static ActionTimelineDefinition CreateTimeline(
            ActionAnimationKey animationKey,
            CharacterStateVariant variant,
            float duration,
            float distance,
            bool rotateToDirection,
            bool setRunLatch)
        {
            return new ActionTimelineDefinition(
                ActionStateIds.Dodge,
                21,
                new[]
                {
                    new ActionTimelineTrackDefinition(
                        ActionTimelineTrackKind.Animation,
                        new[]
                        {
                            new ActionTimelineClipDefinition(
                                ActionTimelineClipKind.AnimationKey,
                                0,
                                21,
                                ActionTimelineClipPayload.Animation(animationKey))
                        }),
                    new ActionTimelineTrackDefinition(
                        ActionTimelineTrackKind.Motion,
                        new[]
                        {
                            new ActionTimelineClipDefinition(
                                ActionTimelineClipKind.Motion,
                                0,
                                21,
                                ActionTimelineClipPayload.Motion(new ActionMotionSpec(
                                    ActionStateIds.Dodge,
                                    CharacterStateIds.Dodge,
                                    variant,
                                    duration,
                                    distance,
                                    rotateToDirection,
                                    setRunLatch,
                                    Vector3.zero,
                                    0f,
                                    0)))
                        })
                });
        }

        static DodgeActionTuning CatalogDodgeTuning()
        {
            return new DodgeActionTuning(0.42f, 5.5f, 0.61f, 2.75f, 33, 44, true, false);
        }

        static LocomotionDecisionFacts DirectionalFacts(Vector3 worldDirection)
        {
            MovementInputIntent intent = MovementInputIntent.FromRaw(Vector2.up, 0.01f);
            return new LocomotionDecisionFacts(
                intent,
                BasicMovementGait.Walk,
                BasicMovementPhaseFacts.None,
                new LocomotionSpatialFacts(worldDirection, Vector3.forward, Vector3.forward, Vector3.right),
                LocomotionTurnBackIntent.None);
        }

        static LocomotionDecisionFacts BackstepFacts()
        {
            MovementInputIntent intent = MovementInputIntent.FromRaw(Vector2.zero, 0.01f);
            return new LocomotionDecisionFacts(
                intent,
                BasicMovementGait.Walk,
                BasicMovementPhaseFacts.None,
                new LocomotionSpatialFacts(Vector3.zero, Vector3.forward, Vector3.forward, Vector3.right),
                LocomotionTurnBackIntent.None);
        }

        sealed class BufferedInputActionRequestProvider : ICharacterActionRequestProvider
        {
            readonly CharacterFrameRequestProviderId providerId;
            readonly ActionRequestType requestType;
            readonly InputRequestKind inputKind;

            public BufferedInputActionRequestProvider(
                CharacterFrameRequestProviderId providerId,
                ActionRequestType requestType,
                InputRequestKind inputKind)
            {
                this.providerId = providerId;
                this.requestType = requestType;
                this.inputKind = inputKind;
            }

            public bool TryBuild(in CharacterActionRequestSubmissionInput input, int sourceOrder, out CharacterActionRequest request)
            {
                request = default;
                if (input.InputBuffer == null ||
                    !input.InputBuffer.TryPeek(inputKind, input.CurrentStep, out BufferedInputRequest bufferedRequest))
                {
                    return false;
                }

                request = CharacterActionRequest.FromBufferedInput(providerId, requestType, in bufferedRequest, sourceOrder);
                return true;
            }
        }

        sealed class FixedResolvedActionResolver : ICharacterActionRequestResolver
        {
            readonly ActionRequestType requestType;
            readonly InputRequestKind inputKind;
            readonly ActionStateId targetState;
            readonly ActionAnimationKey animationKey;
            readonly CharacterStateVariant variant;

            public FixedResolvedActionResolver(
                ActionRequestType requestType,
                InputRequestKind inputKind,
                ActionStateId targetState,
                ActionAnimationKey animationKey,
                CharacterStateVariant variant)
            {
                this.requestType = requestType;
                this.inputKind = inputKind;
                this.targetState = targetState;
                this.animationKey = animationKey;
                this.variant = variant;
            }

            public bool TryResolve(
                in CharacterActionRequest request,
                in CharacterActionResolveContext context,
                out CharacterResolvedAction resolvedAction)
            {
                resolvedAction = default;
                if (!request.HasRequest || request.RequestType != requestType || request.SourceInputKind != inputKind)
                    return false;

                CharacterInputRequestFact requestFact = new CharacterInputRequestFact(
                    true,
                    inputKind,
                    request.OriginStep,
                    request.ExpireStep,
                    Mathf.Max(1, request.PriorityHint),
                    variant,
                    request.WorldDirection);
                ActionInterruptRequest interruptRequest = new ActionInterruptRequest(
                    request.OriginStep,
                    requestType,
                    targetState,
                    requestFact.Priority,
                    request.SourceOrder,
                    request.OriginStep,
                    request.ExpireStep);
                ActionInterruptContext interruptContext = CommittedActionInterruptRequestFactory.CreateContext(
                    context.Snapshot,
                    context.CurrentStep,
                    context.CurrentActionResistance,
                    context.CurrentTimelineFacts);
                resolvedAction = new CharacterResolvedAction(
                    request.ProviderId,
                    request,
                    requestFact,
                    interruptRequest,
                    interruptContext,
                    animationKey,
                    ActionMotionSpec.None(context.CurrentStep));
                return true;
            }
        }

        sealed class RequestResolvingSubmissionProvider : ICharacterFrameRequestSubmissionProvider
        {
            readonly ICharacterActionRequestProvider requestProvider;
            readonly ICharacterActionRequestResolver resolver;

            public RequestResolvingSubmissionProvider(
                ICharacterActionRequestProvider requestProvider,
                ICharacterActionRequestResolver resolver)
            {
                this.requestProvider = requestProvider;
                this.resolver = resolver;
            }

            public bool TryBuild(
                in CharacterActionRequestSubmissionInput input,
                int sourceOrder,
                out CharacterActionRequestSubmissionCandidate candidate)
            {
                candidate = default;
                if (!requestProvider.TryBuild(in input, sourceOrder, out CharacterActionRequest request))
                    return false;

                CharacterActionResolveContext context = CharacterActionResolveContext.FromSubmissionInput(in input);
                if (!resolver.TryResolve(in request, in context, out CharacterResolvedAction resolvedAction))
                    return false;

                candidate = new CharacterActionRequestSubmissionCandidate(in resolvedAction, sourceOrder);
                return true;
            }
        }

        sealed class StaticSubmissionProvider : ICharacterFrameRequestSubmissionProvider
        {
            readonly CharacterFrameRequestProviderId providerId;
            readonly InputRequestKind inputKind;
            readonly ActionRequestType requestType;
            readonly ActionStateId targetState;
            readonly int priority;

            public StaticSubmissionProvider(
                CharacterFrameRequestProviderId providerId,
                InputRequestKind inputKind,
                ActionRequestType requestType,
                ActionStateId targetState,
                int priority)
            {
                this.providerId = providerId;
                this.inputKind = inputKind;
                this.requestType = requestType;
                this.targetState = targetState;
                this.priority = priority;
            }

            public bool TryBuild(
                in CharacterActionRequestSubmissionInput input,
                int sourceOrder,
                out CharacterActionRequestSubmissionCandidate candidate)
            {
                CharacterInputRequestFact requestFact = new CharacterInputRequestFact(
                    true,
                    inputKind,
                    input.CurrentStep,
                    input.CurrentStep + 2,
                    priority,
                    CharacterStateVariant.None,
                    Vector3.zero);
                ActionInterruptRequest interruptRequest = new ActionInterruptRequest(
                    input.CurrentStep,
                    requestType,
                    targetState,
                    priority,
                    sourceOrder,
                    input.CurrentStep,
                    input.CurrentStep + 2);
                ActionInterruptContext interruptContext = CommittedActionInterruptRequestFactory.CreateContext(
                    input.Snapshot,
                    input.CurrentStep,
                    0,
                    input.CurrentTimelineFacts);
                candidate = new CharacterActionRequestSubmissionCandidate(
                    providerId,
                    requestFact,
                    interruptRequest,
                    interruptContext,
                    sourceOrder);
                return true;
            }
        }
    }
}
