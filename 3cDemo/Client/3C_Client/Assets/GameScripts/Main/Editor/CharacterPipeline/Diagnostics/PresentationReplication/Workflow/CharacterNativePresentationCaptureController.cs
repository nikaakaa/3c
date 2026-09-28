using ThirdPersonCharacter.Pipeline.Editor;
using System;
using System.IO;
using System.Threading.Tasks;
using KK.GeneratedDiagnosticSampling;
using KK.GeneratedDiagnosticSampling.Host;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Diagnostics.PresentationReplication;
using ThirdPersonCharacter.Pipeline.Presentation;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.PresentationReplication.Editor
{
    public sealed class CharacterNativePresentationCaptureController :
        IDisposable
    {
        readonly DiagnosticCaptureStartRequest m_Request;
        readonly CharacterPresentationReplicationCaptureMetadata m_Metadata;
        readonly DiagnosticEventTargetKey m_Target;
        readonly string m_SamplerId;
        readonly CharacterPresentationReplicationCoreCaptureProgram.DiagnosticLifecycle
            m_CoreLifecycle;
        readonly CharacterPresentationReplicationFullCaptureProgram.DiagnosticLifecycle
            m_FullLifecycle;
        Task<DiagnosticHostFinalizationResult> m_HostFinalizationTask;
        bool m_Started;
        bool m_Stopped;

        public CharacterNativePresentationCaptureController(
            CharacterPoseSamplingTarget target,
            DiagnosticCaptureStartRequest request,
            string samplerId,
            in CharacterPresentationReplicationCaptureMetadata metadata)
        {
            m_Request = request ?? throw new ArgumentNullException(nameof(request));
            m_SamplerId = samplerId ?? throw new ArgumentNullException(nameof(samplerId));
            m_Metadata = metadata;
            m_Target = new DiagnosticEventTargetKey(
                CharacterNativePresentationDiagnosticEvent.TargetTypeIdentity,
                target.RuntimeInstanceId);
            if (string.Equals(
                    m_SamplerId,
                    CharacterPresentationReplicationDiagnosticIdentity.CoreSamplerId,
                    StringComparison.Ordinal))
            {
                m_CoreLifecycle =
                    CharacterPresentationReplicationCoreCaptureProgram
                        .CreateDiagnosticLifecycle();
            }
            else if (string.Equals(
                         m_SamplerId,
                         CharacterPresentationReplicationDiagnosticIdentity.FullSamplerId,
                         StringComparison.Ordinal))
            {
                m_FullLifecycle =
                    CharacterPresentationReplicationFullCaptureProgram
                        .CreateDiagnosticLifecycle();
            }
            else
            {
                throw new ArgumentException(
                    $"Presentation replication sampler is unknown: {m_SamplerId}.",
                    nameof(samplerId));
            }
        }

        public bool HasStarted => m_Started;
        public DiagnosticCaptureFailure? Failure =>
            m_CoreLifecycle != null
                ? m_CoreLifecycle.Failure
                : m_FullLifecycle.Failure;

        public int CapturedFrameCount => checked(
            (int)(m_CoreLifecycle != null
                ? m_CoreLifecycle.SubmittedSampleCount
                : m_FullLifecycle.SubmittedSampleCount));

        public bool Start(bool attach = true)
        {
            if (m_Started || m_Stopped)
                throw new InvalidOperationException(
                    "Presentation replication capture controller already started.");
            return !attach || StartLifecycle();
        }

        public void Attach()
        {
            if (m_Started || m_Stopped)
                throw new InvalidOperationException(
                    "Presentation replication capture window cannot be opened.");
            if (!StartLifecycle())
                throw new InvalidOperationException(
                    Failure?.Message ??
                    "Presentation replication generated sampling failed to start.");
        }

        public void Detach()
        {
            if (!m_Started || m_Stopped)
                return;
            DiagnosticCaptureStopOutcome outcome =
                DiagnosticCaptureStopOutcome.Completed();
            Stop(in outcome);
        }

        public bool Stop(in DiagnosticCaptureStopOutcome outcome)
        {
            if (!m_Started)
            {
                if (outcome.Kind != DiagnosticCaptureStopKind.Cancelled)
                    throw new InvalidOperationException(
                        "Presentation replication capture window was not opened.");
                m_Stopped = true;
                return true;
            }
            if (m_Stopped)
                return !Failure.HasValue;
            CharacterPoseRenderCaptureRuntime.FlushPending(m_Target.GuidValue);
            m_Stopped = true;
            bool result = m_CoreLifecycle != null
                ? m_CoreLifecycle.Stop(in outcome)
                : m_FullLifecycle.Stop(in outcome);
            return result;
        }

        public bool TryFinalize(
            string outputDirectory,
            out DiagnosticHostFinalizationResult result)
        {
            if (m_HostFinalizationTask == null)
            {
                bool available = m_CoreLifecycle != null
                    ? m_CoreLifecycle.TryGetRuntimeManifest(
                        out DiagnosticRuntimeManifest manifest,
                        out DiagnosticSealedArtifact artifact)
                    : m_FullLifecycle.TryGetRuntimeManifest(
                        out manifest,
                        out artifact);
                if (!available)
                {
                    result = null;
                    return false;
                }
                DiagnosticSchemaLayout schema;
                DiagnosticCapabilityBuildDescriptor capability;
                if (m_CoreLifecycle != null)
                {
                    schema = CharacterPresentationReplicationCoreCaptureProgram
                        .CreateDiagnosticSchemaLayout();
                    capability = CharacterPresentationReplicationCoreCaptureProgram
                        .CreateDiagnosticCapabilityBuildDescriptor(
                            m_Request.InterestIdentity,
                            m_Request.CadenceIdentity,
                            m_Request.LineageTypeIdentity,
                            m_Request.PacketCapacity,
                            m_Request.WriterTransportIdentity);
                }
                else
                {
                    schema = CharacterPresentationReplicationFullCaptureProgram
                        .CreateDiagnosticSchemaLayout();
                    capability = CharacterPresentationReplicationFullCaptureProgram
                        .CreateDiagnosticCapabilityBuildDescriptor(
                            m_Request.InterestIdentity,
                            m_Request.CadenceIdentity,
                            m_Request.LineageTypeIdentity,
                            m_Request.PacketCapacity,
                            m_Request.WriterTransportIdentity);
                }
                string finalOutputDirectory = Path.GetFullPath(outputDirectory);
                m_HostFinalizationTask = Task.Run(() =>
                    FinalizeHost(
                        capability,
                        artifact,
                        schema,
                        finalOutputDirectory));
                result = null;
                return false;
            }

            if (!m_HostFinalizationTask.IsCompleted)
            {
                result = null;
                return false;
            }

            result = m_HostFinalizationTask.GetAwaiter().GetResult();
            m_HostFinalizationTask = null;
            return result.Manifest.Status == DiagnosticCaptureStatus.Completed;
        }

        public DiagnosticHostFinalizationResult FinalizeBeforeReload(string outputRoot)
        {
            if (!m_Started)
                return null;
            CharacterPoseRenderCaptureRuntime.FlushPending(m_Target.GuidValue);
            if (m_CoreLifecycle != null)
                m_CoreLifecycle.Dispose();
            else
                m_FullLifecycle.Dispose();
            TryFinalize(outputRoot, out DiagnosticHostFinalizationResult result);
            return result ?? m_HostFinalizationTask?.GetAwaiter().GetResult();
        }

        public void Dispose()
        {
            if (m_HostFinalizationTask != null)
            {
                try
                {
                    m_HostFinalizationTask.GetAwaiter().GetResult();
                }
                catch
                {
                }
                m_HostFinalizationTask = null;
            }
            if (m_CoreLifecycle != null)
                m_CoreLifecycle.Dispose();
            else
                m_FullLifecycle.Dispose();
        }

        bool StartLifecycle()
        {
            bool started = m_CoreLifecycle != null
                ? m_CoreLifecycle.Start(m_Request, in m_Target, in m_Metadata)
                : m_FullLifecycle.Start(m_Request, in m_Target, in m_Metadata);
            if (!started)
                return false;
            m_Started = true;
            return true;
        }

        static DiagnosticHostFinalizationResult FinalizeHost(
            DiagnosticCapabilityBuildDescriptor capability,
            DiagnosticSealedArtifact artifact,
            DiagnosticSchemaLayout schema,
            string outputDirectory)
        {
            DiagnosticHostFinalizationResult result =
                new DiagnosticHostFinalizer().Finalize(
                    capability,
                    DiagnosticArtifactStore.Open(artifact),
                    schema,
                    outputDirectory);
            DiagnosticArtifactStore.Seal(
                Path.Combine(outputDirectory, "capability.manifest.json"),
                result.EncodedManifest);
            return result;
        }
    }
}


