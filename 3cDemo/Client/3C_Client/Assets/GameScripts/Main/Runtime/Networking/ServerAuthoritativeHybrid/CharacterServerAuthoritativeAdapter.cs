using System;
using System.Collections.Generic;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonCharacter.Pipeline.Motion;
using ThirdPersonCharacter.Pipeline.Network;

namespace ThirdPersonGameplay.Networking.ServerAuthoritativeHybrid
{
    public sealed class CharacterServerAuthoritativeAdapter
    {
        readonly ServerAuthoritativeBehaviorPolicyResolver m_BehaviorResolver;
        readonly ServerAuthoritativeTransactionPolicyResolver m_TransactionResolver;

        public CharacterServerAuthoritativeAdapter(ServerAuthoritativeCharacterSyncProfile profile)
        {
            if (!profile)
                throw new ArgumentNullException(nameof(profile));

            m_BehaviorResolver = new ServerAuthoritativeBehaviorPolicyResolver(profile);
            m_TransactionResolver = new ServerAuthoritativeTransactionPolicyResolver(profile);
        }

        public void DrainIncoming(
            ServerAuthoritativeHybridSession session,
            string subjectActorId,
            CharacterNetworkReceiveStage receiveStage)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));
            if (receiveStage == null)
                throw new ArgumentNullException(nameof(receiveStage));

            while (session.TryDequeueIncoming(subjectActorId, out ServerAuthoritativePacket packet))
            {
                switch (packet.Envelope.PacketKind)
                {
                    case ServerAuthoritativePacketKind.MotionCommand:
                        receiveStage.Push(new ExternalCharacterInputFact(
                            packet.Envelope.InputSequence,
                            ToCharacterInputValues(packet.MotionCommand.ContinuousInputValues),
                            ToCharacterInputRequests(packet.MotionCommand.ActionRequests)));
                        break;
                    case ServerAuthoritativePacketKind.MotionSnapshot:
                        receiveStage.Push(new ExternalPoseSample(
                            packet.MotionSnapshot.ServerTick,
                            packet.MotionSnapshot.Position,
                            packet.MotionSnapshot.Rotation,
                            packet.MotionSnapshot.StateId));
                        break;
                    case ServerAuthoritativePacketKind.MotionCorrection:
                        receiveStage.Push(new ExternalPoseCorrection(
                            packet.MotionCorrection.InputSequence,
                            packet.MotionCorrection.ServerTick,
                            packet.MotionCorrection.Position,
                            packet.MotionCorrection.Rotation));
                        break;
                    case ServerAuthoritativePacketKind.ActionInstanceDecision:
                        receiveStage.Push(new ActionLifecycleTransition(
                            packet.ActionDecision.ActionInstanceId,
                            ToCharacterTransitionKind(packet.ActionDecision.Decision),
                            packet.ActionDecision.LocalLogicTick,
                            packet.ActionDecision.InputSequence,
                            packet.ActionDecision.Reason,
                            string.Empty,
                            string.Empty,
                            "ExternalActionLifecycle",
                            packet.ActionDecision.ServerTick,
                            0));
                        break;
                    case ServerAuthoritativePacketKind.GameplayResult:
                        receiveStage.Push(new IncomingGameplayResult(
                            packet.GameplayResult.ResultId,
                            packet.GameplayResult.ActionInstanceId,
                            packet.GameplayResult.WindowId,
                            packet.GameplayResult.SourceActorId,
                            packet.GameplayResult.TargetActorId,
                            packet.GameplayResult.ResultType,
                            packet.GameplayResult.Reason,
                            packet.Envelope.ServerTick,
                            default));
                        break;
                    case ServerAuthoritativePacketKind.GameplayEffectLifecycle:
                        ServerAuthoritativeGameplayEffectLifecycle lifecycle = packet.GameplayEffectLifecycle;
                        receiveStage.Push(new GameplayEffectLifecycleFact(
                            lifecycle.EffectId,
                            lifecycle.InstanceId,
                            lifecycle.Operation,
                            lifecycle.Context,
                            lifecycle.StartTick,
                            lifecycle.EndTick,
                            lifecycle.StackCount,
                            lifecycle.LifecycleRevision,
                            lifecycle.DefinitionRevision,
                            lifecycle.Instant,
                            lifecycle.SetByCallerValues,
                            packet.Envelope.ServerTick));
                        break;
                    case ServerAuthoritativePacketKind.GameplayAttributeValue:
                        ServerAuthoritativeGameplayAttributeValue attribute = packet.GameplayAttributeValue;
                        receiveStage.Push(new GameplayAttributeValueFact(
                            attribute.AttributeId,
                            attribute.BeforeBase,
                            attribute.BaseValue,
                            attribute.BeforeCurrent,
                            attribute.CurrentValue,
                            attribute.ValueRevision,
                            attribute.CauseEffectId,
                            attribute.CauseEffectInstanceId,
                            attribute.CauseContext,
                            packet.Envelope.ServerTick));
                        break;
                    case ServerAuthoritativePacketKind.GameplayCue:
                        ServerAuthoritativeGameplayCue cue = packet.GameplayCue;
                        receiveStage.Push(new GameplayCueFact(
                            cue.BehaviorId,
                            cue.CueId,
                            cue.CueType,
                            cue.SourceActionInstanceId,
                            cue.SourceEffectId,
                            cue.SourceEffectInstanceId,
                            cue.Context,
                            packet.Envelope.ServerTick));
                        break;
                    default:
                        throw new InvalidOperationException(
                            $"ServerAuthoritative Character adapter cannot consume incoming packet '{packet.Envelope.PacketKind}'.");
                }
            }
        }

        public void CollectOutgoing(
            string subjectActorId,
            ulong localLogicTick,
            CharacterNetworkSendStage sendStage,
            ActionRuntime actionRuntime,
            ServerAuthoritativeHybridSession session)
        {
            if (string.IsNullOrWhiteSpace(subjectActorId))
                throw new ArgumentException("ServerAuthoritative Character adapter requires SubjectActorId.", nameof(subjectActorId));
            if (sendStage == null)
                throw new ArgumentNullException(nameof(sendStage));
            if (actionRuntime == null)
                throw new ArgumentNullException(nameof(actionRuntime));
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            CollectMotionCommand(subjectActorId, sendStage, session);
            CollectCorrectionAcknowledgement(subjectActorId, localLogicTick, sendStage, session);
            CollectActionActivations(subjectActorId, sendStage, session);
            CollectActionLifecycle(subjectActorId, sendStage, actionRuntime, session);
            CollectActionWindows(subjectActorId, sendStage, actionRuntime, session);
            CollectActionMotion(subjectActorId, sendStage, actionRuntime, session);
            CollectCues(subjectActorId, localLogicTick, sendStage, actionRuntime, session);
            CollectGameplayResults(subjectActorId, sendStage, actionRuntime, session);
            CollectGameplayEffects(subjectActorId, sendStage, session);
        }

        void CollectMotionCommand(
            string subjectActorId,
            CharacterNetworkSendStage sendStage,
            ServerAuthoritativeHybridSession session)
        {
            CharacterInputFrame inputFrame = sendStage.Motion.InputFrame;
            ResolvedCharacterMotionFact motion = sendStage.Motion.ResolvedMotion;
            if (inputFrame == null || !motion.IsValid)
                throw new InvalidOperationException(
                    "ServerAuthoritative motion command requires the completed Character input frame and resolved motion fact from the same logic tick.");
            if (inputFrame.InputSequence != motion.InputSequence || inputFrame.LocalLogicTick != motion.LocalLogicTick)
                throw new InvalidOperationException("ServerAuthoritative motion command facts do not belong to the same Character logic tick.");

            ServerAuthoritativePolicyResolution policy = RequirePolicy(
                session,
                0,
                string.Empty,
                ServerAuthoritativeFactKind.MotionCommand.ToString(),
                m_BehaviorResolver.ResolveFact(ServerAuthoritativeFactKind.MotionCommand));
            if (!policy.ShouldSend)
                return;

            session.EnqueueOutgoing(ServerAuthoritativePacket.MotionCommandPacket(
                Identity(subjectActorId),
                inputFrame.InputSequence,
                inputFrame.LocalLogicTick,
                ToServerAuthoritativeInputValues(inputFrame.InputValues),
                ToServerAuthoritativeInputRequests(inputFrame.NewRequests),
                motion.AppliedDisplacement,
                motion.AppliedYawDegrees,
                motion.Position,
                motion.Rotation,
                motion.Grounded,
                motion.HasMotion,
                motion.HorizontalSpeed,
                policy.PolicyId));
        }

        void CollectCorrectionAcknowledgement(
            string subjectActorId,
            ulong localLogicTick,
            CharacterNetworkSendStage sendStage,
            ServerAuthoritativeHybridSession session)
        {
            MotionCorrectionApplicationResult result = sendStage.Motion.CorrectionApplicationResult;
            if (!result.Applied)
                return;

            ServerAuthoritativePolicyResolution policy = RequirePolicy(
                session,
                0,
                string.Empty,
                ServerAuthoritativeFactKind.MotionCorrectionAcknowledgement.ToString(),
                m_BehaviorResolver.ResolveFact(ServerAuthoritativeFactKind.MotionCorrectionAcknowledgement));
            if (!policy.ShouldSend)
                return;

            session.EnqueueOutgoing(ServerAuthoritativePacket.MotionCorrectionAckPacket(
                Identity(subjectActorId),
                new ServerAuthoritativeMotionCorrectionAcknowledgement(result.InputSequence, result.SourceTick),
                localLogicTick,
                policy.PolicyId));
        }

        void CollectActionActivations(
            string subjectActorId,
            CharacterNetworkSendStage sendStage,
            ServerAuthoritativeHybridSession session)
        {
            for (int i = 0; i < sendStage.Action.ActivationOutputs.Count; i++)
            {
                ActionActivationOutput activation = sendStage.Action.ActivationOutputs[i];
                ServerAuthoritativePolicyResolution policy = RequirePolicy(
                    session,
                    activation.ActionInstanceId,
                    activation.ActionId,
                    "ActionActivation",
                    m_TransactionResolver.ResolveActivation(activation.ActionId));
                if (!policy.ShouldSend)
                    continue;

                session.EnqueueOutgoing(ServerAuthoritativePacket.ActionActivationPacket(
                    Identity(subjectActorId, activation.TargetSnapshot.TargetId),
                    activation.ActionInstanceId,
                    activation.ActionId,
                    activation.PredictionKey,
                    activation.InputSequence,
                    activation.LocalLogicTick,
                    activation.SourceInputRequestId,
                    activation.TargetKey,
                    activation.TargetSnapshot.TargetId,
                    policy.PolicyId));
            }
        }

        void CollectActionLifecycle(
            string subjectActorId,
            CharacterNetworkSendStage sendStage,
            ActionRuntime actionRuntime,
            ServerAuthoritativeHybridSession session)
        {
            for (int i = 0; i < sendStage.Action.LifecycleTransitions.Count; i++)
            {
                ActionLifecycleTransition transition = sendStage.Action.LifecycleTransitions[i];
                string actionId = RequireActionId(actionRuntime, transition.ActionInstanceId);
                ServerAuthoritativePolicyResolution policy = RequirePolicy(
                    session,
                    transition.ActionInstanceId,
                    actionId,
                    $"ActionLifecycle:{transition.TransitionType}",
                    m_TransactionResolver.ResolveLifecycle(actionId, transition.TransitionType));
                if (!policy.ShouldSend)
                    continue;

                session.EnqueueOutgoing(ServerAuthoritativePacket.ActionLifecycleTransitionPacket(
                    Identity(subjectActorId),
                    transition.ActionInstanceId,
                    ToServerAuthoritativeTransitionKind(transition.TransitionType),
                    transition.InputSequence,
                    transition.LocalLogicTick,
                    transition.SourceTick,
                    transition.Reason,
                    transition.SourceGraphId,
                    transition.SourceNodeId,
                    transition.SourceName,
                    transition.CorrectionId,
                    policy.PolicyId));
            }
        }

        void CollectActionWindows(
            string subjectActorId,
            CharacterNetworkSendStage sendStage,
            ActionRuntime actionRuntime,
            ServerAuthoritativeHybridSession session)
        {
            for (int i = 0; i < sendStage.Action.WindowSamples.Count; i++)
            {
                ActionWindowSample sample = sendStage.Action.WindowSamples[i];
                string actionId = RequireActionId(actionRuntime, sample.ActionInstanceId);
                ServerAuthoritativePolicyResolution policy = RequirePolicy(
                    session,
                    sample.ActionInstanceId,
                    actionId,
                    $"ActionWindow:{sample.WindowType}",
                    m_TransactionResolver.ResolveWindow(actionId, sample.WindowType));
                if (!policy.ShouldSend)
                    continue;

                session.EnqueueOutgoing(ServerAuthoritativePacket.ActionWindowDigestPacket(
                    Identity(subjectActorId),
                    new ServerAuthoritativeActionWindowDigest(
                        sample.ActionInstanceId,
                        sample.WindowId,
                        sample.WindowType,
                        sample.StartLocalLogicTick,
                        sample.EndLocalLogicTick,
                        sample.Digest,
                        string.Empty),
                    sample.StartLocalLogicTick,
                    policy.PolicyId));
            }
        }

        void CollectActionMotion(
            string subjectActorId,
            CharacterNetworkSendStage sendStage,
            ActionRuntime actionRuntime,
            ServerAuthoritativeHybridSession session)
        {
            for (int i = 0; i < sendStage.Motion.ActionMotionSamples.Count; i++)
            {
                ActionMotionSample sample = sendStage.Motion.ActionMotionSamples[i];
                string actionId = RequireActionId(actionRuntime, sample.ActionInstanceId);
                ServerAuthoritativePolicyResolution policy = RequirePolicy(
                    session,
                    sample.ActionInstanceId,
                    actionId,
                    $"ActionMotion:{sample.SourceType}",
                    m_TransactionResolver.ResolveMotion(actionId, sample.SourceType));
                if (!policy.ShouldSend)
                    continue;

                session.EnqueueOutgoing(ServerAuthoritativePacket.ActionMotionDigestPacket(
                    Identity(subjectActorId),
                    new ServerAuthoritativeActionMotionDigest(sample.ActionInstanceId, sample.SourceType.ToString()),
                    sample.InputSequence,
                    sample.LocalLogicTick,
                    policy.PolicyId));
            }
        }

        void CollectCues(
            string subjectActorId,
            ulong localLogicTick,
            CharacterNetworkSendStage sendStage,
            ActionRuntime actionRuntime,
            ServerAuthoritativeHybridSession session)
        {
            for (int i = 0; i < sendStage.Presentation.CueEvents.Count; i++)
            {
                GameplayCueFact cue = sendStage.Presentation.CueEvents[i];
                string actionId = string.Empty;
                ServerAuthoritativePolicyResolution resolution;
                if (cue.SourceActionInstanceId != 0)
                {
                    actionId = RequireActionId(actionRuntime, cue.SourceActionInstanceId);
                    if (!string.Equals(cue.BehaviorId, actionId, StringComparison.Ordinal))
                        throw new InvalidOperationException($"Gameplay cue behavior '{cue.BehaviorId}' does not match Action '{actionId}'.");
                    resolution = m_TransactionResolver.ResolveCue(actionId, cue.CueType);
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(cue.BehaviorId))
                        throw new InvalidOperationException("Non-action cue requires a gameplay BehaviorId.");
                    resolution = m_BehaviorResolver.ResolveEvent(cue.BehaviorId, ServerAuthoritativePacketKind.GameplayCue);
                }

                ServerAuthoritativePolicyResolution policy = RequirePolicy(
                    session,
                    cue.SourceActionInstanceId,
                    actionId,
                    $"Cue:{cue.CueType}",
                    resolution);
                if (!policy.ShouldSend)
                    continue;

                session.EnqueueOutgoing(ServerAuthoritativePacket.GameplayCuePacket(
                    Identity(subjectActorId),
                    new ServerAuthoritativeGameplayCue(
                        cue.BehaviorId,
                        cue.CueId,
                        cue.CueType,
                        cue.SourceActionInstanceId,
                        cue.SourceEffectId,
                        cue.SourceEffectInstanceId,
                        cue.Context),
                    localLogicTick,
                    policy.PolicyId));
            }
        }

        void CollectGameplayResults(
            string subjectActorId,
            CharacterNetworkSendStage sendStage,
            ActionRuntime actionRuntime,
            ServerAuthoritativeHybridSession session)
        {
            for (int i = 0; i < sendStage.GameplayResult.Events.Count; i++)
            {
                GameplayResultEvent result = sendStage.GameplayResult.Events[i];
                string actionId = string.Empty;
                ServerAuthoritativePolicyResolution resolution;
                if (result.ActionInstanceId != 0)
                {
                    actionId = RequireActionId(actionRuntime, result.ActionInstanceId);
                    resolution = m_TransactionResolver.ResolveGameplayResult(actionId);
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(result.BehaviorId))
                        throw new InvalidOperationException("Non-action gameplay result requires a gameplay BehaviorId.");
                    resolution = m_BehaviorResolver.ResolveEvent(result.BehaviorId, ServerAuthoritativePacketKind.GameplayResult);
                }

                ServerAuthoritativePolicyResolution policy = RequirePolicy(
                    session,
                    result.ActionInstanceId,
                    actionId,
                    $"GameplayResult:{result.ResultType}",
                    resolution);
                if (!policy.ShouldSend)
                    continue;

                session.EnqueueOutgoing(ServerAuthoritativePacket.GameplayResultPacket(
                    Identity(subjectActorId, result.TargetId),
                    new ServerAuthoritativeGameplayResult(
                        result.ResultId,
                        result.ActionInstanceId,
                        result.WindowId,
                        subjectActorId,
                        result.TargetId,
                        result.ResultType,
                        string.Empty),
                    result.LocalLogicTick,
                    0,
                    policy.PolicyId));
            }
        }

        void CollectGameplayEffects(
            string subjectActorId,
            CharacterNetworkSendStage sendStage,
            ServerAuthoritativeHybridSession session)
        {
            for (int i = 0; i < sendStage.GameplayEffect.LifecycleFacts.Count; i++)
            {
                GameplayEffectLifecycleFact fact = sendStage.GameplayEffect.LifecycleFacts[i];
                ServerAuthoritativePolicyResolution policy = RequirePolicy(
                    session,
                    fact.Context.SourceActionInstanceId,
                    fact.BehaviorId,
                    ServerAuthoritativePacketKind.GameplayEffectLifecycle.ToString(),
                    m_BehaviorResolver.ResolveGameplayEffect(fact.BehaviorId, ServerAuthoritativePacketKind.GameplayEffectLifecycle));
                if (!policy.ShouldSend)
                    continue;

                session.EnqueueOutgoing(ServerAuthoritativePacket.GameplayEffectLifecyclePacket(
                    Identity(subjectActorId, fact.Context.TargetActorId),
                    new ServerAuthoritativeGameplayEffectLifecycle(
                        fact.EffectId,
                        fact.InstanceId,
                        fact.Operation,
                        fact.Context,
                        fact.StartTick,
                        fact.EndTick,
                        fact.StackCount,
                        fact.LifecycleRevision,
                        fact.DefinitionRevision,
                        fact.Instant,
                        Copy(fact.SetByCallerValues)),
                    fact.LocalLogicTick,
                    0,
                    policy.PolicyId));
            }

            for (int i = 0; i < sendStage.GameplayEffect.AttributeFacts.Count; i++)
            {
                GameplayAttributeValueFact fact = sendStage.GameplayEffect.AttributeFacts[i];
                ServerAuthoritativePolicyResolution resolution = !string.IsNullOrEmpty(fact.CauseBehaviorId)
                    ? m_BehaviorResolver.ResolveGameplayEffect(fact.CauseBehaviorId, ServerAuthoritativePacketKind.GameplayAttributeValue)
                    : m_BehaviorResolver.ResolveFact(ServerAuthoritativeFactKind.GameplayAttributeValue);
                ServerAuthoritativePolicyResolution policy = RequirePolicy(
                    session,
                    fact.CauseContext.SourceActionInstanceId,
                    fact.CauseBehaviorId,
                    ServerAuthoritativePacketKind.GameplayAttributeValue.ToString(),
                    resolution);
                if (!policy.ShouldSend)
                    continue;

                session.EnqueueOutgoing(ServerAuthoritativePacket.GameplayAttributeValuePacket(
                    Identity(subjectActorId, fact.CauseContext.TargetActorId),
                    new ServerAuthoritativeGameplayAttributeValue(
                        fact.AttributeId,
                        fact.BeforeBase,
                        fact.BaseValue,
                        fact.BeforeCurrent,
                        fact.CurrentValue,
                        fact.ValueRevision,
                        fact.CauseEffectId,
                        fact.CauseEffectInstanceId,
                        fact.CauseContext),
                    fact.LocalLogicTick,
                    0,
                    policy.PolicyId));
            }
        }

        static ThirdPersonGameplay.Effects.GameplaySetByCallerValue[] Copy(
            IReadOnlyList<ThirdPersonGameplay.Effects.GameplaySetByCallerValue> values)
        {
            if (values == null || values.Count == 0)
                return Array.Empty<ThirdPersonGameplay.Effects.GameplaySetByCallerValue>();
            var result = new ThirdPersonGameplay.Effects.GameplaySetByCallerValue[values.Count];
            for (int i = 0; i < values.Count; i++)
                result[i] = values[i];
            return result;
        }

        static ServerAuthoritativePolicyResolution RequirePolicy(
            ServerAuthoritativeHybridSession session,
            ulong actionInstanceId,
            string actionId,
            string factKind,
            ServerAuthoritativePolicyResolution policy)
        {
            session.RecordPolicyDecision(new ServerAuthoritativePolicyDecisionDebugRecord(
                policy.BehaviorId,
                policy.BehaviorKind.ToString(),
                actionInstanceId,
                actionId,
                factKind,
                policy.Domain.ToString(),
                policy.PacketKind.ToString(),
                policy.PolicyId,
                policy.ShouldSend,
                policy.Reason,
                policy.Summary));
            if (!policy.IsConfigured)
            {
                throw new InvalidOperationException(
                    $"ServerAuthoritative policy is missing for '{factKind}' owner '{actionId}'.");
            }

            return policy;
        }

        static string RequireActionId(ActionRuntime actionRuntime, ulong actionInstanceId)
        {
            if (actionRuntime.TryGetActionId(actionInstanceId, out string actionId))
                return actionId;

            throw new InvalidOperationException(
                $"ServerAuthoritative adapter cannot resolve ActionId for instance '{actionInstanceId}'.");
        }

        static ServerAuthoritativeActorIdentity Identity(string subjectActorId, string targetActorId = "")
        {
            return new ServerAuthoritativeActorIdentity(
                subjectActorId,
                string.Empty,
                string.Empty,
                subjectActorId,
                targetActorId);
        }

        static ServerAuthoritativeInputValue[] ToServerAuthoritativeInputValues(
            IEnumerable<CharacterInputValue> inputValues)
        {
            var values = new List<ServerAuthoritativeInputValue>();
            if (inputValues == null)
                return values.ToArray();

            foreach (CharacterInputValue inputValue in inputValues)
            {
                switch (inputValue.ValueType)
                {
                    case CharacterInputValueType.Bool:
                        values.Add(ServerAuthoritativeInputValue.Bool(inputValue.InputValueId, inputValue.BoolValue));
                        break;
                    case CharacterInputValueType.Float:
                        values.Add(ServerAuthoritativeInputValue.Float(inputValue.InputValueId, inputValue.FloatValue));
                        break;
                    case CharacterInputValueType.Vector2:
                        values.Add(ServerAuthoritativeInputValue.Vector2ValueInput(inputValue.InputValueId, inputValue.Vector2Value));
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported Character input value type '{inputValue.ValueType}'.");
                }
            }

            return values.ToArray();
        }

        static ServerAuthoritativeInputRequest[] ToServerAuthoritativeInputRequests(
            IReadOnlyList<CharacterInputRequest> requests)
        {
            if (requests == null || requests.Count == 0)
                return Array.Empty<ServerAuthoritativeInputRequest>();

            var values = new ServerAuthoritativeInputRequest[requests.Count];
            for (int i = 0; i < requests.Count; i++)
            {
                CharacterInputRequest request = requests[i];
                values[i] = new ServerAuthoritativeInputRequest(
                    request.RequestId,
                    request.CreatedLocalLogicTick,
                    request.InputSequence,
                    request.ExpireLocalLogicTick,
                    request.BufferSeconds,
                    request.Priority,
                    request.Consumed);
            }

            return values;
        }

        static CharacterInputValue[] ToCharacterInputValues(ServerAuthoritativeInputValue[] inputValues)
        {
            if (inputValues == null || inputValues.Length == 0)
                return Array.Empty<CharacterInputValue>();

            var values = new CharacterInputValue[inputValues.Length];
            for (int i = 0; i < inputValues.Length; i++)
            {
                ServerAuthoritativeInputValue inputValue = inputValues[i];
                switch (inputValue.ValueKind)
                {
                    case ServerAuthoritativeInputValueKind.Bool:
                        values[i] = CharacterInputValue.Bool(inputValue.InputValueId, inputValue.BoolValue);
                        break;
                    case ServerAuthoritativeInputValueKind.Float:
                        values[i] = CharacterInputValue.Float(inputValue.InputValueId, inputValue.FloatValue);
                        break;
                    case ServerAuthoritativeInputValueKind.Vector2:
                        values[i] = CharacterInputValue.Vector2(inputValue.InputValueId, inputValue.Vector2Value);
                        break;
                    default:
                        throw new InvalidOperationException(
                            $"Unsupported ServerAuthoritative input value kind '{inputValue.ValueKind}'.");
                }
            }

            return values;
        }

        static CharacterInputRequest[] ToCharacterInputRequests(ServerAuthoritativeInputRequest[] requests)
        {
            if (requests == null || requests.Length == 0)
                return Array.Empty<CharacterInputRequest>();

            var values = new CharacterInputRequest[requests.Length];
            for (int i = 0; i < requests.Length; i++)
            {
                ServerAuthoritativeInputRequest request = requests[i];
                values[i] = new CharacterInputRequest(
                    request.RequestId,
                    request.CreatedLocalLogicTick,
                    request.InputSequence,
                    request.ExpireLocalLogicTick,
                    request.BufferSeconds,
                    request.Priority);
                if (request.Consumed)
                    values[i].MarkConsumed();
            }

            return values;
        }

        static ActionLifecycleTransitionType ToCharacterTransitionKind(ServerAuthoritativeActionDecisionKind decision)
        {
            switch (decision)
            {
                case ServerAuthoritativeActionDecisionKind.Confirmed:
                    return ActionLifecycleTransitionType.Confirm;
                case ServerAuthoritativeActionDecisionKind.Rejected:
                    return ActionLifecycleTransitionType.Reject;
                case ServerAuthoritativeActionDecisionKind.Corrected:
                    return ActionLifecycleTransitionType.Correct;
                default:
                    throw new InvalidOperationException($"Unsupported action decision '{decision}'.");
            }
        }

        static ServerAuthoritativeActionLifecycleTransitionKind ToServerAuthoritativeTransitionKind(
            ActionLifecycleTransitionType transitionType)
        {
            switch (transitionType)
            {
                case ActionLifecycleTransitionType.Confirm:
                    return ServerAuthoritativeActionLifecycleTransitionKind.Confirm;
                case ActionLifecycleTransitionType.Complete:
                    return ServerAuthoritativeActionLifecycleTransitionKind.Complete;
                case ActionLifecycleTransitionType.Cancel:
                    return ServerAuthoritativeActionLifecycleTransitionKind.Cancel;
                case ActionLifecycleTransitionType.Interrupt:
                    return ServerAuthoritativeActionLifecycleTransitionKind.Interrupt;
                case ActionLifecycleTransitionType.Reject:
                    return ServerAuthoritativeActionLifecycleTransitionKind.Reject;
                case ActionLifecycleTransitionType.Correct:
                    return ServerAuthoritativeActionLifecycleTransitionKind.Correct;
                case ActionLifecycleTransitionType.Abort:
                    return ServerAuthoritativeActionLifecycleTransitionKind.Abort;
                default:
                    throw new InvalidOperationException($"Unsupported action lifecycle transition '{transitionType}'.");
            }
        }
    }
}
