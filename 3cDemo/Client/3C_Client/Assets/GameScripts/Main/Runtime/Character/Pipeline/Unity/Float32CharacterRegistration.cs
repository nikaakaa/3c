using System;
using System.Collections.Generic;
using BTSMTL.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    public sealed class Float32CharacterRegistration :
        ILocalSimulationActorRegistration,
        ISimulationActorStartGate,
        ISimulationPresentationCheckpointRuntime
    {
        readonly Float32CharacterRuntime m_CharacterRuntime;
        readonly SimulationActorBinding m_CharacterBinding;
        readonly IUnityCharacterControlSourceRuntime m_LocalControlSource;
        readonly ICharacterSimulationGameplayOutputPort m_GameplayOutput;
        readonly ICharacterPresentationRuntime m_PresentationRuntime;
        readonly ISimulationPresentationOutputPort m_PresentationOutput;
        readonly ISimulationDiagnosticsSink m_Diagnostics;
        readonly RuntimeDiagnosticsTarget m_DiagnosticsTarget;
        readonly AnimationPresentationRuntimeTarget m_AnimationDiagnosticsTarget;
        readonly CharacterPresentationFrameTarget m_PresentationTarget;
        readonly List<EquipmentVisualSelection> m_EquipmentVisualSelections =
            new List<EquipmentVisualSelection>();
        readonly Transform m_VisualRoot;
        bool m_Activated;
        bool m_InputActivated;
        bool m_DiagnosticsRegistered;
        bool m_AnimationDiagnosticsRegistered;
        bool m_PresentationRegistered;
        bool m_Disposed;
        ulong m_TrajectoryIntentSequence;

        public Float32CharacterRegistration(
            int ownerInstanceId,
            string ownerName,
            ActorId actorId,
            Float32CharacterRuntime characterRuntime,
            SimulationActorBinding characterBinding,
            CharacterPresentationProjection projection,
            Float32WorldBodyBinding worldBodyBinding,
            WorldBodyState initialBody,
            IUnityCharacterControlSourceRuntime localControlSource,
            ICharacterSimulationGameplayOutputPort gameplayOutput,
            ICharacterPresentationRuntime presentationRuntime,
            ISimulationDiagnosticsSink diagnostics,
            RuntimeDiagnosticsTarget diagnosticsTarget,
            UnityEngine.Transform visualRoot)
        {
            if (ownerInstanceId == 0 || string.IsNullOrWhiteSpace(ownerName) || !actorId.IsValid)
                throw new ArgumentException("Float32 Actor registration owner identity is incomplete.");
            if (characterRuntime == null || characterBinding == null)
                throw new ArgumentException("Float32 Actor registration requires a Character Runtime and binding.");
            if (!ReferenceEquals(
                    characterRuntime.Roster[characterRuntime.GetActorIndex(actorId)],
                    characterBinding))
            {
                throw new ArgumentException("Float32 Actor registration binding does not belong to the Character Runtime.", nameof(characterBinding));
            }
            if (characterBinding.ActorId != actorId || initialBody.ActorId != actorId ||
                !worldBodyBinding || worldBodyBinding.ActorId != actorId)
            {
                throw new ArgumentException("Float32 Actor registration body identity does not match ActorId.");
            }
            OwnerInstanceId = ownerInstanceId;
            OwnerName = ownerName.Trim();
            ActorId = actorId;
            m_CharacterRuntime = characterRuntime;
            m_CharacterBinding = characterBinding;
            PresentationProjection = projection ?? throw new ArgumentNullException(nameof(projection));
            WorldBodyBinding = worldBodyBinding;
            InitialBody = initialBody;
            m_LocalControlSource = localControlSource;
            m_GameplayOutput = gameplayOutput ?? throw new ArgumentNullException(nameof(gameplayOutput));
            m_PresentationRuntime = presentationRuntime ?? throw new ArgumentNullException(nameof(presentationRuntime));
            m_PresentationOutput = presentationRuntime as ISimulationPresentationOutputPort ??
                throw new ArgumentException("Float32 Presentation Runtime does not expose the Presentation output port.", nameof(presentationRuntime));
            m_Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
            m_DiagnosticsTarget = diagnosticsTarget ?? throw new ArgumentNullException(nameof(diagnosticsTarget));
            var animationSnapshotProvider = presentationRuntime as IAnimationPresentationRuntimeSnapshotProvider ??
                throw new ArgumentException("Float32 Presentation Runtime does not expose the Animation Presentation snapshot provider.", nameof(presentationRuntime));
            m_AnimationDiagnosticsTarget = new AnimationPresentationRuntimeTarget(
                diagnosticsTarget.CharacterRuntimeId,
                actorId,
                ownerInstanceId,
                OwnerName,
                new AnimationPresentationIdentity(projection),
                animationSnapshotProvider);
            m_PresentationTarget = new CharacterPresentationFrameTarget(presentationRuntime);
            m_VisualRoot = visualRoot ? visualRoot : throw new ArgumentNullException(nameof(visualRoot));
            VisualRootIdentity = BuildTransformIdentity(m_VisualRoot);
            OutputRoute = new SimulationOutputRouteDescriptor(
                $"character-output/{actorId.Value}",
                "character-simulation-output",
                1,
                actorId,
                StableHash.Compute(
                    actorId.Value,
                    m_CharacterRuntime.GameplayContentHash.ToString(),
                    m_CharacterBinding.GameplayContentHash.ToString(),
                    PresentationProjection.ProjectionRevision,
                    WorldBodyBinding.BindingId,
                    VisualRootIdentity));
        }

        public int OwnerInstanceId { get; }
        public string OwnerName { get; }
        public string OwnerIdentity => $"unity-character-host/{OwnerInstanceId}";
        public ActorId ActorId { get; }
        public Float32CharacterRuntime CharacterRuntime => m_CharacterRuntime;
        public SimulationActorBinding CharacterBinding => m_CharacterBinding;
        public Float32WorldBodyBinding WorldBodyBinding { get; }
        public WorldBodyState InitialBody { get; }
        public IUnityCharacterControlSourceRuntime LocalControlSource => m_LocalControlSource;
        public ICharacterSimulationGameplayOutputPort GameplayOutput => m_GameplayOutput;
        public CharacterPresentationProjection PresentationProjection { get; }
        public ICharacterPresentationRuntime PresentationRuntime => m_PresentationRuntime;
        public ISimulationPresentationOutputPort PresentationOutput => m_PresentationOutput;
        public ISimulationDiagnosticsSink SimulationDiagnostics => m_Diagnostics;
        public RuntimeDiagnosticsContext DiagnosticsContext => m_DiagnosticsTarget.Context;
        public RuntimeDiagnosticsTarget DiagnosticsTarget => m_DiagnosticsTarget;
        public string VisualRootIdentity { get; }
        public SimulationOutputRouteDescriptor OutputRoute { get; }
        public bool IsActivated => m_Activated;
        public bool SupportsPresentationCheckpointCapture =>
            m_PresentationRuntime is ICharacterPresentationCheckpointRuntime checkpoint &&
            checkpoint.SupportsCheckpointCapture;
        public bool SupportsPresentationCheckpointRestore =>
            m_PresentationRuntime is ICharacterPresentationCheckpointRuntime checkpoint &&
            checkpoint.SupportsCheckpointCapture &&
            checkpoint.SupportsCheckpointRestore;
        public bool IsSimulationStartReady => true;
        public string SimulationStartWaitReason => string.Empty;
        public StableHash DiagnosticsConfigurationHash => StableHash.Compute(
            m_CharacterRuntime.GameplayContentHash.ToString(),
            m_CharacterBinding.GameplayContentHash.ToString(),
            DiagnosticsContext.Revision.ToString());

        public bool TryCapturePresentationCheckpoint(
            SimulationSessionCheckpoint checkpoint,
            out string error)
        {
            if (m_PresentationRuntime is ICharacterPresentationCheckpointRuntime runtime)
                return runtime.TryCaptureCheckpoint(checkpoint, out error);
            error = "Character Presentation runtime does not expose checkpoint capture.";
            return false;
        }

        public bool TryRestorePresentationCheckpoint(
            SimulationSessionCheckpoint checkpoint,
            out string error)
        {
            if (m_PresentationRuntime is ICharacterPresentationCheckpointRuntime runtime)
                return runtime.TryRestoreCheckpoint(checkpoint, out error);
            error = "Character Presentation runtime does not expose checkpoint restore.";
            return false;
        }

        public void PublishCheckpoint(ulong tick, string snapshotIdentity, StableHash snapshotHash)
        {
        }

        public void BindExecutionBranch(Guid executionBranchId) =>
            DiagnosticsContext.SetExecutionBranch(executionBranchId);

        public void Activate()
        {
            RequireAlive();
            if (m_Activated)
                return;
            try
            {
                if (m_LocalControlSource != null)
                {
                    m_LocalControlSource.Activate();
                    m_InputActivated = true;
                }
                RuntimeDiagnosticsTargetRegistry.Register(m_DiagnosticsTarget);
                m_DiagnosticsRegistered = true;
                AnimationPresentationRuntimeTargetRegistry.Register(m_AnimationDiagnosticsTarget);
                m_AnimationDiagnosticsRegistered = true;
                m_PresentationTarget.Activate();
                m_PresentationRegistered = true;
                m_Activated = true;
            }
            catch (Exception exception)
            {
                var failures = new List<Exception> { exception };
                ReleaseActivation(failures);
                if (failures.Count == 1)
                    throw;
                throw new AggregateException(failures);
            }
        }

        public void Deactivate()
        {
            if (!m_Activated && !m_InputActivated && !m_DiagnosticsRegistered &&
                !m_AnimationDiagnosticsRegistered && !m_PresentationRegistered)
                return;
            var failures = new List<Exception>();
            ReleaseActivation(failures);
            if (failures.Count != 0)
                throw new AggregateException($"Float32 Actor '{ActorId}' activation resources failed to release.", failures);
        }

        public void CaptureRenderFrame(ulong renderFrame)
        {
            RequireAlive();
            if (!m_Activated)
                throw new InvalidOperationException($"Float32 Actor '{ActorId}' registration is not active.");
            m_LocalControlSource?.CaptureRenderFrame(renderFrame);
        }

        public void BeginLogicTick()
        {
            RequireAlive();
            m_GameplayOutput.BeginTick();
        }

        public void ObservePublished(SimulationActorTickResult result)
        {
            RequireAlive();
            if (result == null || result.ActorId != ActorId)
                throw new ArgumentException("Published Presentation result targets another Actor.", nameof(result));
            m_PresentationRuntime.CaptureBodyInterval(
                CharacterPresentationBodyInterval.FromFloat32(
                    result.BodySample,
                    m_CharacterRuntime.TickRate));
            if (m_PresentationRuntime.AcceptsTrajectoryIntent)
            {
                m_PresentationRuntime.CaptureTrajectoryIntent(
                    CharacterPresentationTrajectoryIntent.FromFloat32(
                        result,
                        checked(++m_TrajectoryIntentSequence),
                        m_PresentationRuntime.BodyResetSequence));
            }
            if (!result.State.TryGetEquipmentState(out EquipmentStateAggregate equipment))
                return;
            m_EquipmentVisualSelections.Clear();
            for (int i = 0; i < equipment.Slots.Count; i++)
            {
                m_EquipmentVisualSelections.Add(
                    equipment.Slots[i].CreateVisualSelection(ActorId, result.Tick.Value));
            }
            m_PresentationRuntime.CaptureEquipmentSelections(m_EquipmentVisualSelections);
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            var failures = new List<Exception>();
            TryRelease(Deactivate, failures);
            TryRelease(m_DiagnosticsTarget.Terminate, failures);
            TryRelease(m_DiagnosticsTarget.Dispose, failures);
            TryRelease(m_PresentationRuntime.Dispose, failures);
            if (m_LocalControlSource != null)
                TryRelease(m_LocalControlSource.Dispose, failures);
            if (failures.Count != 0)
                throw new AggregateException($"Float32 Actor '{ActorId}' registration failed to dispose completely.", failures);
        }

        void ReleaseActivation(List<Exception> failures)
        {
            if (m_PresentationRegistered)
            {
                TryRelease(m_PresentationTarget.Deactivate, failures);
                m_PresentationRegistered = false;
            }
            if (m_AnimationDiagnosticsRegistered)
            {
                TryRelease(() => AnimationPresentationRuntimeTargetRegistry.Unregister(m_AnimationDiagnosticsTarget), failures);
                m_AnimationDiagnosticsRegistered = false;
            }
            if (m_DiagnosticsRegistered)
            {
                TryRelease(() => RuntimeDiagnosticsTargetRegistry.Unregister(m_DiagnosticsTarget), failures);
                m_DiagnosticsRegistered = false;
            }
            if (m_InputActivated)
            {
                TryRelease(m_LocalControlSource.Deactivate, failures);
                m_InputActivated = false;
            }
            m_Activated = false;
        }

        static void TryRelease(Action release, List<Exception> failures)
        {
            try
            {
                release();
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(Float32CharacterRegistration));
        }

        static string BuildTransformIdentity(UnityEngine.Transform transform)
        {
            string path = transform.name;
            UnityEngine.Transform current = transform.parent;
            while (current)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            return $"{transform.gameObject.scene.path}:{path}";
        }
    }
}
