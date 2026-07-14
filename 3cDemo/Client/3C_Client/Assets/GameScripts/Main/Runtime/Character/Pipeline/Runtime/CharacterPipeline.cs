using System;
using System.Collections.Generic;
using BTSMTL.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonCharacter.Pipeline.GameplayEffect;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonCharacter.Pipeline.Logic;
using ThirdPersonCharacter.Pipeline.Camera;
using ThirdPersonCharacter.Pipeline.Motion;
using ThirdPersonCharacter.Pipeline.Network;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonCharacter.Pipeline.Diagnostics;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonGameplay.Tick;
using ThirdPersonGameplay.Effects;
using ThirdPersonCamera;
using TreeDesigner;
using Animancer;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline
{
    public sealed class CharacterPipeline : IGameplayTickTarget, IDisposable
    {
        readonly CharacterInputSource m_InputSource;
        readonly CharacterMotionAuthority m_MotionAuthority;
        readonly string m_ActorId;
        readonly CharacterInputStage m_InputStage;
        readonly CharacterGameplayEffectAdapter m_GameplayEffectAdapter;
        readonly ActionRuntime m_ActionRuntime;
        readonly CharacterAnimationPlaybackCommandQueue m_AnimationPlaybackCommands;
        readonly CharacterAnimationPresentationBindingIndex m_AnimationBindingIndex;
        readonly CharacterAnimationTracePublisher m_AnimationTracePublisher;
        readonly CharacterGraphContext m_GraphContext;
        readonly CharacterBTSMTLPhase m_BTSMTLPhase;
        readonly CharacterNetworkReceiveStage m_NetworkReceiveStage;
        readonly CharacterNetworkSendStage m_NetworkSendStage;
        readonly CharacterActionLifecycleInputStage m_ActionLifecycleInputStage = new CharacterActionLifecycleInputStage();
        readonly CharacterMotionStage m_MotionStage;
        readonly CharacterPresentationStage m_PresentationStage;
        readonly CharacterCameraStage m_CameraStage;
        readonly CharacterPipelineFrame m_Frame = new CharacterPipelineFrame();
        readonly CharacterRuntimeDebugProgram m_DebugProgram;

        RuntimeDiagnosticsTarget m_DiagnosticsTarget;

        ulong m_InputSequence;
        bool m_Active;
        bool m_HasLogicSample;
        bool m_Disposed;

        public CharacterPipeline(
            CharacterPipelineDefinition definition,
            string actorId,
            AnimancerComponent animancer,
            ICharacterLogicPosePort logicPosePort,
            ICharacterMotionExecutor motionExecutor,
            Transform visualRoot,
            ICameraRigAdapter cameraRigAdapter,
            Transform cameraFollowAnchor,
            Transform cameraAimAnchor,
            string cameraLookInputValueId,
            int logicTickRate,
            CharacterInputSource inputSource,
            CharacterMotionAuthority motionAuthority)
        {
            if (!definition)
                throw new ArgumentNullException(nameof(definition));
            m_ActorId = string.IsNullOrWhiteSpace(actorId)
                ? throw new ArgumentException("Character actor id is required.", nameof(actorId))
                : actorId.Trim();
            if (logicPosePort == null)
                throw new ArgumentNullException(nameof(logicPosePort));
            if (motionAuthority == CharacterMotionAuthority.LocalSolver && motionExecutor == null)
                throw new ArgumentNullException(nameof(motionExecutor));
            if (!logicPosePort.TryReadState(out CharacterLogicBodyState initialLogicState, out string poseError))
                throw new InvalidOperationException($"Logic pose port failed during pipeline creation: {poseError}");
            if (!initialLogicState.IsValid)
                throw new InvalidOperationException("Logic pose port returned an invalid initial body state.");

            var configurationErrors = new List<string>();
            if (!definition.CollectConfigurationErrors(configurationErrors))
                throw new InvalidOperationException(string.Join("\n", configurationErrors));
            if (!definition.GameplayEffectProfile.TryBuildRuntimeDefinition(
                    logicTickRate,
                    out GameplayEffectRuntimeDefinition gameplayEffectDefinition,
                    configurationErrors))
            {
                throw new InvalidOperationException(string.Join("\n", configurationErrors));
            }

            RunnableTree rootTree = definition.RootTree;
            m_DebugProgram = CharacterRuntimeDebugProgramBuilder.Build(definition);

            m_InputSource = inputSource;
            m_MotionAuthority = motionAuthority;
            m_InputStage = new CharacterInputStage(definition ? definition.InputProfile : null, inputSource);
            m_GameplayEffectAdapter = new CharacterGameplayEffectAdapter(m_ActorId, gameplayEffectDefinition);
            m_ActionRuntime = new ActionRuntime(
                m_GameplayEffectAdapter.TagReader,
                m_GameplayEffectAdapter.TagSourceSink);
            m_AnimationPlaybackCommands = new CharacterAnimationPlaybackCommandQueue();
            var presentationErrors = new List<string>();
            m_AnimationBindingIndex = CharacterAnimationPresentationBindingIndex.Build(
                definition?.AnimationPresentation,
                rootTree,
                presentationErrors);
            if (!m_AnimationBindingIndex.IsValid)
                throw new InvalidOperationException(string.Join("\n", presentationErrors));
            m_AnimationTracePublisher = new CharacterAnimationTracePublisher(() => m_GraphContext?.RuntimeDiagnostics);
            RegisterActionProfiles(definition.ActionProfiles);
            m_GraphContext = new CharacterGraphContext(
                m_ActorId,
                m_InputStage,
                m_ActionRuntime,
                logicPosePort,
                m_GameplayEffectAdapter.QueryPorts,
                m_GameplayEffectAdapter.CommandPorts);
            m_BTSMTLPhase = new CharacterBTSMTLPhase(
                rootTree,
                m_GraphContext,
                m_AnimationPlaybackCommands,
                m_AnimationBindingIndex);
            m_NetworkReceiveStage = new CharacterNetworkReceiveStage();
            m_NetworkSendStage = new CharacterNetworkSendStage();
            m_MotionStage = new CharacterMotionStage(
                logicPosePort,
                motionAuthority == CharacterMotionAuthority.LocalSolver ? motionExecutor : null,
                m_GraphContext,
                motionAuthority);
            m_PresentationStage = new CharacterPresentationStage(
                animancer,
                m_AnimationBindingIndex,
                logicPosePort,
                visualRoot,
                m_AnimationPlaybackCommands,
                m_GraphContext,
                m_AnimationTracePublisher);
            m_CameraStage = new CharacterCameraStage(
                m_GraphContext,
                cameraRigAdapter,
                initialLogicState,
                cameraFollowAnchor,
                cameraAimAnchor,
                cameraLookInputValueId);
        }

        public CharacterPipelineOutput Output => m_Frame.Output;
        public string ActorId => m_ActorId;
        public ActionRuntime ActionRuntime => m_ActionRuntime;
        public CharacterGraphContext GraphContext => m_GraphContext;
        public CharacterNetworkReceiveStage NetworkReceiveStage => m_NetworkReceiveStage;
        public CharacterNetworkSendStage NetworkSendStage => m_NetworkSendStage;
        public CharacterInputSource InputSource => m_InputSource;
        public CharacterMotionAuthority MotionAuthority => m_MotionAuthority;
        public RuntimeDiagnosticsTarget DiagnosticsTarget => m_DiagnosticsTarget;

        public void RegisterDiagnosticsTarget(string displayName, int hostInstanceId)
        {
            if (m_DiagnosticsTarget != null)
                return;

            Guid characterRuntimeId = Guid.NewGuid();
            var diagnosticsStore = new RuntimeDiagnosticsStore();
            var diagnostics = new RuntimeDiagnosticsContext(
                characterRuntimeId,
                Guid.NewGuid(),
                m_DebugProgram.Revision,
                m_DebugProgram.SourceMap,
                diagnosticsStore);
            m_GraphContext.SetRuntimeDiagnostics(diagnostics);
            m_DiagnosticsTarget = new RuntimeDiagnosticsTarget(displayName, hostInstanceId, diagnostics);
            RuntimeDiagnosticsTargetRegistry.Register(m_DiagnosticsTarget);
            diagnostics.PublishTarget(RuntimeTraceEventKind.TargetAttached, new RuntimeTracePayload { Name = displayName });
        }

        public void UnregisterDiagnosticsTarget()
        {
            if (m_DiagnosticsTarget == null)
                return;

            RuntimeDiagnosticsTarget target = m_DiagnosticsTarget;
            target.Context.PublishTarget(RuntimeTraceEventKind.TargetDetached, new RuntimeTracePayload { Name = target.DisplayName });
            target.Terminate();
            RuntimeDiagnosticsTargetRegistry.Unregister(target);
            target.Dispose();
            m_GraphContext.SetRuntimeDiagnostics(null);
            m_DiagnosticsTarget = null;
        }

        public void BeginRenderFrame(ulong renderFrame)
        {
            m_InputStage.BeginRenderFrame(renderFrame);
            m_CameraStage.CaptureRenderFrameInput(m_InputStage);
        }

        public void Activate()
        {
            if (m_Disposed || m_Active)
                return;

            m_GameplayEffectAdapter.Activate();
            m_InputStage.Activate();
            m_GraphContext.ResetPipelineBlackboard();
            m_PresentationStage.Activate();
            m_CameraStage.Reset();
            m_BTSMTLPhase.Activate();
            m_HasLogicSample = false;
            m_Active = true;
        }

        public void Deactivate()
        {
            if (!m_Active)
                return;

            m_Active = false;
            m_BTSMTLPhase.Deactivate();
            m_PresentationStage.Deactivate();
            m_CameraStage.Reset();
            m_AnimationPlaybackCommands.Clear();
            m_NetworkReceiveStage.Clear();
            m_NetworkSendStage.Clear();
            m_ActionRuntime.ResetExecution();
            m_GameplayEffectAdapter.Deactivate();
            m_InputStage.Deactivate();
            m_GraphContext.ClearActorPoseSnapshot();
            m_Frame.ClearTransient();
            m_HasLogicSample = false;
        }

        public void LogicTick(GameplayLogicTickContext context)
        {
            if (!m_Active || m_Disposed)
                return;

            m_ActionRuntime.BeginLogicTick();
            GameplayLogicTickContext logicContext = CreateTickContext(context);
            m_Frame.Begin(logicContext);
            m_GraphContext.BeginFrame(logicContext, m_Frame);
            m_NetworkReceiveStage.Collect(m_Frame);
            m_ActionLifecycleInputStage.Resolve(m_Frame.NetworkInput, m_ActionRuntime);
            m_GameplayEffectAdapter.BeginLogicTick(logicContext, m_Frame);
            m_InputStage.Update(logicContext, m_Frame);
            m_BTSMTLPhase.Tick(logicContext, m_Frame);
            m_MotionStage.Update(logicContext, m_Frame);
            m_GameplayEffectAdapter.CommitFacts(m_Frame.Output, m_GraphContext.RuntimeDiagnostics);
            m_NetworkSendStage.Collect(m_Frame);
            m_PresentationStage.CaptureLogicSample(logicContext, m_Frame);
            m_CameraStage.CaptureLogicSample(m_Frame);
            PublishActionDiagnostics();
            m_HasLogicSample = true;
        }

        public void PresentationFrame(GameplayPresentationFrameContext context)
        {
            if (!m_Active || m_Disposed)
                return;

            m_GraphContext.RuntimeDiagnostics?.BeginPresentationFrame(context.RenderFrame);
            m_PresentationStage.PrepareAnimationSampling();
            m_BTSMTLPhase.SamplePresentation(context, m_Frame, m_PresentationStage.DemandedPlaybacks);
            m_PresentationStage.Update(context, m_Frame);
            m_BTSMTLPhase.CompletePresentationFrame(m_PresentationStage.RetiredPlaybacks);
            if (m_HasLogicSample)
            {
                m_CameraStage.Update(context, m_Frame);
                PublishCameraDiagnostics();
            }
            m_GraphContext.EndFrame();
            m_Frame.ClearTransient();
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;

            Deactivate();
            m_BTSMTLPhase.Dispose();
            m_InputStage.Dispose();
            m_GraphContext.Dispose();
            m_PresentationStage.Dispose();
            m_AnimationPlaybackCommands.Clear();
            m_ActionRuntime.Reset();
            m_GameplayEffectAdapter.Dispose();
            UnregisterDiagnosticsTarget();
            m_Disposed = true;
        }

        void RegisterActionProfiles(IReadOnlyList<ActionProfile> profiles)
        {
            for (int i = 0; i < profiles.Count; i++)
            {
                ActionProfile profile = profiles[i];
                m_ActionRuntime.RegisterProfile(profile);
            }
        }

        GameplayLogicTickContext CreateTickContext(GameplayLogicTickContext context)
        {
            m_InputSequence++;
            return new GameplayLogicTickContext(
                context.FixedDeltaSeconds,
                context.RenderFrame,
                context.LocalLogicTick,
                m_InputSequence);
        }

        void PublishActionDiagnostics()
        {
            RuntimeDiagnosticsContext diagnostics = m_GraphContext.RuntimeDiagnostics;
            if (diagnostics != null)
            {
                RuntimeInstanceKey character = RuntimeInstanceKey.Character(diagnostics.CharacterRuntimeId);
                if (diagnostics.ShouldPublish(RuntimeTraceChannel.StateMachine, RuntimeTraceEventKind.ActionSnapshot))
                {
                    ActionContext context = m_ActionRuntime.ActionContext;
                    diagnostics.Publish(
                        RuntimeTraceChannel.StateMachine,
                        RuntimeTraceDomain.Logic,
                        RuntimeTraceEventKind.ActionSnapshot,
                        RuntimeSourceElementHandle.Invalid,
                        character,
                        new RuntimeTracePayload
                        {
                            Name = context.HasActiveInstance ? context.ActionId : string.Empty,
                            Status = context.HasActiveInstance ? $"{context.Phase}/{context.State}" : "None",
                            OwnerId = context.ActionInstanceId.ToString(),
                            RelatedElementId = context.PredictionKey.ToString(),
                            Time = context.StartLocalLogicTick,
                            SecondaryTime = context.InputSequence,
                            Flag = context.HasActiveInstance
                        });
                }

                PublishActivationRequests(diagnostics, character);
                PublishLifecycleTransitions(diagnostics, character);
                PublishActionOutputs(diagnostics, character);
            }

            m_ActionRuntime.ClearDiagnosticEvents();
        }

        void PublishActivationRequests(RuntimeDiagnosticsContext diagnostics, RuntimeInstanceKey character)
        {
            IReadOnlyList<ActionActivationRequest> values = m_ActionRuntime.DiagnosticActivationRequests;
            for (int i = 0; i < values.Count; i++)
            {
                ActionActivationRequest value = values[i];
                RuntimeSourceElementKey source = !string.IsNullOrEmpty(value.SourceGraphId) && !string.IsNullOrEmpty(value.SourceNodeId)
                    ? RuntimeSourceElementKey.Node(value.SourceGraphId, value.SourceNodeId)
                    : default;
                PublishOptionalSource(
                    diagnostics,
                    RuntimeTraceChannel.StateMachine,
                    RuntimeTraceDomain.Logic,
                    RuntimeTraceEventKind.ActionActivationRequested,
                    source,
                    character,
                    new RuntimeTracePayload
                    {
                        Name = value.ActionId,
                        Status = value.SourceInputRequestId,
                        Detail = value.SourceName,
                        RelatedElementId = value.TargetKey,
                        Time = value.InputSequence
                    });
            }
        }

        void PublishLifecycleTransitions(RuntimeDiagnosticsContext diagnostics, RuntimeInstanceKey character)
        {
            IReadOnlyList<ActionLifecycleTransition> values = m_ActionRuntime.DiagnosticLifecycleTransitions;
            for (int i = 0; i < values.Count; i++)
            {
                ActionLifecycleTransition value = values[i];
                RuntimeSourceElementKey source = !string.IsNullOrEmpty(value.SourceGraphId) && !string.IsNullOrEmpty(value.SourceNodeId)
                    ? RuntimeSourceElementKey.Node(value.SourceGraphId, value.SourceNodeId)
                    : default;
                PublishOptionalSource(
                    diagnostics,
                    RuntimeTraceChannel.StateMachine,
                    RuntimeTraceDomain.Logic,
                    RuntimeTraceEventKind.ActionLifecycleTransitioned,
                    source,
                    character,
                    new RuntimeTracePayload
                    {
                        Name = value.ActionInstanceId.ToString(),
                        Status = value.TransitionType.ToString(),
                        Cause = value.Reason,
                        Detail = value.SourceName,
                        Time = value.InputSequence,
                            SecondaryTime = value.SourceTick,
                        Flag = value.IsTerminal
                    });
            }
        }

        void PublishActionOutputs(RuntimeDiagnosticsContext diagnostics, RuntimeInstanceKey character)
        {
            IReadOnlyList<ActionWindowSample> windows = m_ActionRuntime.DiagnosticWindowSamples;
            for (int i = 0; i < windows.Count; i++)
            {
                ActionWindowSample value = windows[i];
                diagnostics.Publish(RuntimeTraceChannel.StateMachine, RuntimeTraceDomain.Logic, RuntimeTraceEventKind.ActionWindowSampled, RuntimeSourceElementHandle.Invalid, character,
                    new RuntimeTracePayload { Name = value.WindowId, Status = value.WindowType, OwnerId = value.ActionInstanceId.ToString(), Time = value.StartLocalLogicTick, SecondaryTime = value.EndLocalLogicTick, RelatedElementId = value.Digest.ToString() });
            }

            IReadOnlyList<GameplayCueFact> cues = m_ActionRuntime.DiagnosticCueEvents;
            for (int i = 0; i < cues.Count; i++)
            {
                GameplayCueFact value = cues[i];
                diagnostics.Publish(RuntimeTraceChannel.StateMachine, RuntimeTraceDomain.Logic, RuntimeTraceEventKind.GameplayCueSubmitted, RuntimeSourceElementHandle.Invalid, character,
                    new RuntimeTracePayload { Name = value.CueId, Status = value.CueType, Detail = value.BehaviorId, OwnerId = value.SourceActionInstanceId.ToString(), Time = value.LocalLogicTick });
            }

            IReadOnlyList<GameplayResultEvent> results = m_ActionRuntime.DiagnosticGameplayResults;
            for (int i = 0; i < results.Count; i++)
            {
                GameplayResultEvent value = results[i];
                diagnostics.Publish(RuntimeTraceChannel.StateMachine, RuntimeTraceDomain.Logic, RuntimeTraceEventKind.ActionResultSubmitted, RuntimeSourceElementHandle.Invalid, character,
                    new RuntimeTracePayload { Name = value.ResultType, Status = value.WindowId, Detail = value.TargetId, OwnerId = value.ActionInstanceId.ToString(), RelatedElementId = value.ResultId.ToString() });
            }
        }

        void PublishCameraDiagnostics()
        {
            RuntimeDiagnosticsContext diagnostics = m_GraphContext.RuntimeDiagnostics;
            if (diagnostics == null)
                return;

            RuntimeInstanceKey character = RuntimeInstanceKey.Character(diagnostics.CharacterRuntimeId);
            bool publishSnapshot = diagnostics.ShouldPublish(RuntimeTraceChannel.Animation, RuntimeTraceEventKind.CameraSnapshot);
            bool publishRequests = diagnostics.ShouldPublish(RuntimeTraceChannel.Animation, RuntimeTraceEventKind.CameraRequest);
            bool publishCues = diagnostics.ShouldPublish(RuntimeTraceChannel.Animation, RuntimeTraceEventKind.CameraCue);
            if (!publishSnapshot && !publishRequests && !publishCues)
                return;
            CameraDebugSnapshot debug = m_CameraStage.DebugSnapshot;
            if (publishSnapshot)
            {
                diagnostics.Publish(RuntimeTraceChannel.Animation, RuntimeTraceDomain.Presentation, RuntimeTraceEventKind.CameraSnapshot, RuntimeSourceElementHandle.Invalid, character,
                    new RuntimeTracePayload
                    {
                        Name = debug.Mode.ToString(),
                        Status = debug.SourceId,
                        Detail = debug.TargetSource,
                        OwnerId = debug.SourceActionInstanceId.ToString(),
                        Weight = debug.BlendProgress,
                        Value = DebugValueSnapshot.Capture(debug.PosePlan.Valid ? debug.PosePlan.FollowPoint : Vector3.zero),
                        Flag = debug.PosePlan.Valid
                    });
            }
            if (publishRequests)
            {
                for (int i = 0; i < debug.Requests.Count; i++)
                {
                    CameraDebugRequestEntry value = debug.Requests[i];
                    diagnostics.Publish(RuntimeTraceChannel.Animation, RuntimeTraceDomain.Presentation, RuntimeTraceEventKind.CameraRequest, RuntimeSourceElementHandle.Invalid, character,
                        new RuntimeTracePayload { Name = value.Mode.ToString(), Status = value.SourceId, OwnerId = value.SourceActionInstanceId.ToString(), Priority = value.Priority, Weight = value.Weight });
                }
            }
            if (publishCues)
            {
                for (int i = 0; i < debug.Cues.Count; i++)
                {
                    CameraDebugCueEntry value = debug.Cues[i];
                    diagnostics.Publish(RuntimeTraceChannel.Animation, RuntimeTraceDomain.Presentation, RuntimeTraceEventKind.CameraCue, RuntimeSourceElementHandle.Invalid, character,
                        new RuntimeTracePayload { Name = value.CueId, Status = value.CueKind.ToString(), Detail = value.SourceId, OwnerId = value.SourceActionInstanceId.ToString(), Weight = value.Intensity });
                }
            }
        }

        static void PublishOptionalSource(
            RuntimeDiagnosticsContext diagnostics,
            RuntimeTraceChannel channel,
            RuntimeTraceDomain domain,
            RuntimeTraceEventKind kind,
            RuntimeSourceElementKey source,
            RuntimeInstanceKey instance,
            RuntimeTracePayload payload)
        {
            if (source.IsValid)
                diagnostics.Publish(channel, domain, kind, source, instance, payload);
            else
                diagnostics.Publish(channel, domain, kind, RuntimeSourceElementHandle.Invalid, instance, payload);
        }
    }
}
