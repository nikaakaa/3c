using UnityEngine;

namespace ThirdPersonGameplay.Networking.ServerAuthoritativeHybrid
{
    public readonly struct ServerAuthoritativePacket
    {
        ServerAuthoritativePacket(
            ServerAuthoritativePacketEnvelope envelope,
            ServerAuthoritativeMotionCommand motionCommand,
            ServerAuthoritativeMotionSnapshot motionSnapshot,
            ServerAuthoritativeMotionCorrection motionCorrection,
            ServerAuthoritativeActionActivation actionActivation,
            ServerAuthoritativeActionLifecycleTransition actionLifecycleTransition,
            ServerAuthoritativeActionInstanceDecision actionDecision,
            ServerAuthoritativeActionWindowDigest actionWindowDigest,
            ServerAuthoritativeActionMotionDigest actionMotionDigest,
            ServerAuthoritativeGameplayResult gameplayResult,
            ServerAuthoritativeGameplayEffectLifecycle gameplayEffectLifecycle,
            ServerAuthoritativeGameplayAttributeValue gameplayAttributeValue,
            ServerAuthoritativeGameplayCue gameplayCue,
            ServerAuthoritativeMotionCorrectionAcknowledgement motionCorrectionAcknowledgement)
        {
            Envelope = envelope;
            MotionCommand = motionCommand;
            MotionSnapshot = motionSnapshot;
            MotionCorrection = motionCorrection;
            MotionCorrectionAcknowledgement = motionCorrectionAcknowledgement;
            ActionActivation = actionActivation;
            ActionLifecycleTransition = actionLifecycleTransition;
            ActionDecision = actionDecision;
            ActionWindowDigest = actionWindowDigest;
            ActionMotionDigest = actionMotionDigest;
            GameplayResult = gameplayResult;
            GameplayEffectLifecycle = gameplayEffectLifecycle;
            GameplayAttributeValue = gameplayAttributeValue;
            GameplayCue = gameplayCue;
        }

        public ServerAuthoritativePacketEnvelope Envelope { get; }
        public ServerAuthoritativeMotionCommand MotionCommand { get; }
        public ServerAuthoritativeMotionSnapshot MotionSnapshot { get; }
        public ServerAuthoritativeMotionCorrection MotionCorrection { get; }
        public ServerAuthoritativeMotionCorrectionAcknowledgement MotionCorrectionAcknowledgement { get; }
        public ServerAuthoritativeActionActivation ActionActivation { get; }
        public ServerAuthoritativeActionLifecycleTransition ActionLifecycleTransition { get; }
        public ServerAuthoritativeActionInstanceDecision ActionDecision { get; }
        public ServerAuthoritativeActionWindowDigest ActionWindowDigest { get; }
        public ServerAuthoritativeActionMotionDigest ActionMotionDigest { get; }
        public ServerAuthoritativeGameplayResult GameplayResult { get; }
        public ServerAuthoritativeGameplayEffectLifecycle GameplayEffectLifecycle { get; }
        public ServerAuthoritativeGameplayAttributeValue GameplayAttributeValue { get; }
        public ServerAuthoritativeGameplayCue GameplayCue { get; }

        public ServerAuthoritativePacket WithPacketId(ulong packetId) => CreateCopy(Envelope.WithPacketId(packetId));
        public ServerAuthoritativePacket WithServerTick(ulong serverTick) => CreateCopy(Envelope.WithServerTick(serverTick));

        public static ServerAuthoritativePacket MotionCommandPacket(
            ServerAuthoritativeActorIdentity identity,
            ulong inputSequence,
            ulong localLogicTick,
            ServerAuthoritativeInputValue[] continuousInputValues,
            ServerAuthoritativeInputRequest[] actionRequests,
            Vector3 appliedDisplacement,
            float appliedYawDegrees,
            Vector3 resolvedPosition,
            Quaternion resolvedRotation,
            bool grounded,
            bool hasMotion,
            float horizontalSpeed,
            string policyId)
        {
            var envelope = new ServerAuthoritativePacketEnvelope(0, ServerAuthoritativeDomain.Motion, ServerAuthoritativePacketKind.MotionCommand, policyId, identity, MotionStableId(identity, inputSequence, localLogicTick), 0, inputSequence, localLogicTick, 0);
            return Create(envelope, motionCommand: new ServerAuthoritativeMotionCommand(continuousInputValues, actionRequests, appliedDisplacement, appliedYawDegrees, resolvedPosition, resolvedRotation, grounded, hasMotion, horizontalSpeed));
        }

        public static ServerAuthoritativePacket MotionSnapshotPacket(ServerAuthoritativeActorIdentity identity, ServerAuthoritativeMotionSnapshot snapshot, ulong inputSequence, ulong localLogicTick, string policyId)
        {
            var envelope = new ServerAuthoritativePacketEnvelope(0, ServerAuthoritativeDomain.Motion, ServerAuthoritativePacketKind.MotionSnapshot, policyId, identity, MotionStableId(identity, inputSequence, localLogicTick), 0, inputSequence, localLogicTick, snapshot.ServerTick);
            return Create(envelope, motionSnapshot: snapshot);
        }

        public static ServerAuthoritativePacket MotionCorrectionPacket(ServerAuthoritativeActorIdentity identity, ServerAuthoritativeMotionCorrection correction, ulong localLogicTick, string policyId)
        {
            var envelope = new ServerAuthoritativePacketEnvelope(0, ServerAuthoritativeDomain.Motion, ServerAuthoritativePacketKind.MotionCorrection, policyId, identity, MotionStableId(identity, correction.InputSequence, localLogicTick), 0, correction.InputSequence, localLogicTick, correction.ServerTick);
            return Create(envelope, motionCorrection: correction);
        }

        public static ServerAuthoritativePacket MotionCorrectionAckPacket(ServerAuthoritativeActorIdentity identity, ServerAuthoritativeMotionCorrectionAcknowledgement acknowledgement, ulong localLogicTick, string policyId)
        {
            var envelope = new ServerAuthoritativePacketEnvelope(0, ServerAuthoritativeDomain.Motion, ServerAuthoritativePacketKind.MotionCorrectionAck, policyId, identity, MotionStableId(identity, acknowledgement.InputSequence, localLogicTick), 0, acknowledgement.InputSequence, localLogicTick, acknowledgement.ServerTick);
            return Create(envelope, motionCorrectionAcknowledgement: acknowledgement);
        }

        public static ServerAuthoritativePacket ActionActivationPacket(ServerAuthoritativeActorIdentity identity, ulong actionInstanceId, string actionId, ulong predictionKey, ulong inputSequence, ulong localLogicTick, string sourceInputRequestId, string targetKey, string targetStableId, string policyId)
        {
            var envelope = new ServerAuthoritativePacketEnvelope(0, ServerAuthoritativeDomain.Action, ServerAuthoritativePacketKind.ActionActivation, policyId, identity, ActionStableId(actionInstanceId, actionId, predictionKey), predictionKey, inputSequence, localLogicTick, 0);
            return Create(envelope, actionActivation: new ServerAuthoritativeActionActivation(actionInstanceId, actionId, sourceInputRequestId, targetKey, targetStableId));
        }

        public static ServerAuthoritativePacket ActionLifecycleTransitionPacket(
            ServerAuthoritativeActorIdentity identity,
            ulong actionInstanceId,
            ServerAuthoritativeActionLifecycleTransitionKind transitionKind,
            ulong inputSequence,
            ulong localLogicTick,
            ulong serverTick,
            string reason,
            string sourceGraphId,
            string sourceNodeId,
            string sourceName,
            ulong correctionId,
            string policyId)
        {
            var envelope = new ServerAuthoritativePacketEnvelope(0, ServerAuthoritativeDomain.Action, ServerAuthoritativePacketKind.ActionLifecycleTransition, policyId, identity, ActionStableId(actionInstanceId, transitionKind.ToString(), 0), 0, inputSequence, localLogicTick, serverTick);
            return Create(envelope, actionLifecycleTransition: new ServerAuthoritativeActionLifecycleTransition(actionInstanceId, transitionKind, reason, sourceGraphId, sourceNodeId, sourceName, correctionId));
        }

        public static ServerAuthoritativePacket ActionInstanceDecisionPacket(ServerAuthoritativeActorIdentity identity, ServerAuthoritativeActionInstanceDecision decision, string policyId)
        {
            var envelope = new ServerAuthoritativePacketEnvelope(0, ServerAuthoritativeDomain.Action, ServerAuthoritativePacketKind.ActionInstanceDecision, policyId, identity, ActionStableId(decision.ActionInstanceId, decision.ActionId, decision.PredictionKey), decision.PredictionKey, decision.InputSequence, decision.LocalLogicTick, decision.ServerTick);
            return Create(envelope, actionDecision: decision);
        }

        public static ServerAuthoritativePacket ActionWindowDigestPacket(ServerAuthoritativeActorIdentity identity, ServerAuthoritativeActionWindowDigest digest, ulong localLogicTick, string policyId)
        {
            var envelope = new ServerAuthoritativePacketEnvelope(0, ServerAuthoritativeDomain.Action, ServerAuthoritativePacketKind.ActionWindowDigest, policyId, identity, ActionStableId(digest.ActionInstanceId, digest.WindowId, 0), 0, 0, localLogicTick, 0);
            return Create(envelope, actionWindowDigest: digest);
        }

        public static ServerAuthoritativePacket ActionMotionDigestPacket(ServerAuthoritativeActorIdentity identity, ServerAuthoritativeActionMotionDigest digest, ulong inputSequence, ulong localLogicTick, string policyId)
        {
            var envelope = new ServerAuthoritativePacketEnvelope(0, ServerAuthoritativeDomain.Motion, ServerAuthoritativePacketKind.ActionMotionDigest, policyId, identity, MotionStableId(identity, inputSequence, localLogicTick), 0, inputSequence, localLogicTick, 0);
            return Create(envelope, actionMotionDigest: digest);
        }

        public static ServerAuthoritativePacket GameplayResultPacket(ServerAuthoritativeActorIdentity identity, ServerAuthoritativeGameplayResult result, ulong localLogicTick, ulong serverTick, string policyId)
        {
            var envelope = new ServerAuthoritativePacketEnvelope(0, ServerAuthoritativeDomain.GameplayResult, ServerAuthoritativePacketKind.GameplayResult, policyId, identity, ResultStableId(result.ResultId, result.ResultType), 0, 0, localLogicTick, serverTick);
            return Create(envelope, gameplayResult: result);
        }

        public static ServerAuthoritativePacket GameplayEffectLifecyclePacket(ServerAuthoritativeActorIdentity identity, ServerAuthoritativeGameplayEffectLifecycle lifecycle, ulong localLogicTick, ulong serverTick, string policyId)
        {
            string stableId = $"{lifecycle.EffectId.Value}:{lifecycle.InstanceId.Value}:{lifecycle.LifecycleRevision}";
            var envelope = new ServerAuthoritativePacketEnvelope(0, ServerAuthoritativeDomain.GameplayEffect, ServerAuthoritativePacketKind.GameplayEffectLifecycle, policyId, identity, stableId, lifecycle.Context.PredictionKey, 0, localLogicTick, serverTick);
            return Create(envelope, gameplayEffectLifecycle: lifecycle);
        }

        public static ServerAuthoritativePacket GameplayAttributeValuePacket(ServerAuthoritativeActorIdentity identity, ServerAuthoritativeGameplayAttributeValue attribute, ulong localLogicTick, ulong serverTick, string policyId)
        {
            string stableId = $"{attribute.AttributeId.Value}:{attribute.ValueRevision}:{attribute.CauseEffectInstanceId.Value}";
            var envelope = new ServerAuthoritativePacketEnvelope(0, ServerAuthoritativeDomain.GameplayEffect, ServerAuthoritativePacketKind.GameplayAttributeValue, policyId, identity, stableId, attribute.CauseContext.PredictionKey, 0, localLogicTick, serverTick);
            return Create(envelope, gameplayAttributeValue: attribute);
        }

        public static ServerAuthoritativePacket GameplayCuePacket(ServerAuthoritativeActorIdentity identity, ServerAuthoritativeGameplayCue cue, ulong localLogicTick, string policyId)
        {
            string stableId = $"{cue.BehaviorId}:{cue.CueId}:{cue.SourceActionInstanceId}:{cue.SourceEffectInstanceId.Value}:{localLogicTick}";
            var envelope = new ServerAuthoritativePacketEnvelope(0, ServerAuthoritativeDomain.Presentation, ServerAuthoritativePacketKind.GameplayCue, policyId, identity, stableId, cue.Context.PredictionKey, 0, localLogicTick, 0);
            return Create(envelope, gameplayCue: cue);
        }

        ServerAuthoritativePacket CreateCopy(ServerAuthoritativePacketEnvelope envelope)
        {
            return new ServerAuthoritativePacket(envelope, MotionCommand, MotionSnapshot, MotionCorrection, ActionActivation, ActionLifecycleTransition, ActionDecision, ActionWindowDigest, ActionMotionDigest, GameplayResult, GameplayEffectLifecycle, GameplayAttributeValue, GameplayCue, MotionCorrectionAcknowledgement);
        }

        static ServerAuthoritativePacket Create(
            ServerAuthoritativePacketEnvelope envelope,
            ServerAuthoritativeMotionCommand motionCommand = default,
            ServerAuthoritativeMotionSnapshot motionSnapshot = default,
            ServerAuthoritativeMotionCorrection motionCorrection = default,
            ServerAuthoritativeActionActivation actionActivation = default,
            ServerAuthoritativeActionLifecycleTransition actionLifecycleTransition = default,
            ServerAuthoritativeActionInstanceDecision actionDecision = default,
            ServerAuthoritativeActionWindowDigest actionWindowDigest = default,
            ServerAuthoritativeActionMotionDigest actionMotionDigest = default,
            ServerAuthoritativeGameplayResult gameplayResult = default,
            ServerAuthoritativeGameplayEffectLifecycle gameplayEffectLifecycle = default,
            ServerAuthoritativeGameplayAttributeValue gameplayAttributeValue = default,
            ServerAuthoritativeGameplayCue gameplayCue = default,
            ServerAuthoritativeMotionCorrectionAcknowledgement motionCorrectionAcknowledgement = default)
        {
            return new ServerAuthoritativePacket(envelope, motionCommand, motionSnapshot, motionCorrection, actionActivation, actionLifecycleTransition, actionDecision, actionWindowDigest, actionMotionDigest, gameplayResult, gameplayEffectLifecycle, gameplayAttributeValue, gameplayCue, motionCorrectionAcknowledgement);
        }

        static string MotionStableId(ServerAuthoritativeActorIdentity identity, ulong inputSequence, ulong localLogicTick) => $"{identity.SubjectActorId}:{inputSequence}:{localLogicTick}";
        static string ActionStableId(ulong actionInstanceId, string label, ulong predictionKey) => actionInstanceId != 0 ? actionInstanceId.ToString() : $"{label ?? string.Empty}:{predictionKey}";
        static string ResultStableId(ulong resultId, string resultType) => resultId != 0 ? resultId.ToString() : resultType ?? string.Empty;
    }
}
