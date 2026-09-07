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
    public sealed class CharacterPresentationReplicationGeneratedCaptureController :
        IDisposable
    {
        readonly AnimationPresentationRuntimeTarget m_TargetRuntime;
        readonly DiagnosticCaptureStartRequest m_Request;
        readonly CharacterPresentationReplicationCaptureMetadata m_Metadata;
        readonly DiagnosticEventTargetKey m_Target;
        readonly Guid m_InterestOwnerId = Guid.NewGuid();
        readonly string m_SamplerId;
        readonly CharacterPresentationReplicationCoreCaptureProgram.DiagnosticLifecycle
            m_CoreLifecycle;
        readonly CharacterPresentationReplicationFullCaptureProgram.DiagnosticLifecycle
            m_FullLifecycle;
        Task<DiagnosticHostFinalizationResult> m_HostFinalizationTask;
        bool m_InterestAttached;
        bool m_Started;
        bool m_Stopped;

        public CharacterPresentationReplicationGeneratedCaptureController(
            AnimationPresentationRuntimeTarget target,
            DiagnosticCaptureStartRequest request,
            string samplerId,
            in CharacterPresentationReplicationCaptureMetadata metadata)
        {
            m_TargetRuntime = target ?? throw new ArgumentNullException(nameof(target));
            m_Request = request ?? throw new ArgumentNullException(nameof(request));
            m_SamplerId = samplerId ?? throw new ArgumentNullException(nameof(samplerId));
            m_Metadata = metadata;
            m_Target = new DiagnosticEventTargetKey(
                CharacterPresentationReplicationDiagnosticEvent.TargetTypeIdentity,
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
            m_Stopped = true;
            bool result = m_CoreLifecycle != null
                ? m_CoreLifecycle.Stop(in outcome)
                : m_FullLifecycle.Stop(in outcome);
            RemoveInterest();
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

        public void Dispose()
        {
            RemoveInterest();
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
            try
            {
                m_TargetRuntime.SetDiagnosticsInterest(
                    m_InterestOwnerId,
                    AnimationPresentationDiagnosticsInterest.Capture);
                m_InterestAttached = true;
                m_Started = true;
                return true;
            }
            catch
            {
                DiagnosticCaptureStopOutcome outcome =
                    DiagnosticCaptureStopOutcome.Cancelled();
                if (m_CoreLifecycle != null)
                    m_CoreLifecycle.Stop(in outcome);
                else
                    m_FullLifecycle.Stop(in outcome);
                throw;
            }
        }

        void RemoveInterest()
        {
            if (!m_InterestAttached)
                return;
            m_TargetRuntime.RemoveDiagnosticsInterest(m_InterestOwnerId);
            m_InterestAttached = false;
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


