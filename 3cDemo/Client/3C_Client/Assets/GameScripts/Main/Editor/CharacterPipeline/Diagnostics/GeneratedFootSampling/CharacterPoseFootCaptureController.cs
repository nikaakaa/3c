using System;
using ThirdPersonCharacter.Pipeline.Editor;
using System.IO;
using System.Threading.Tasks;
using KK.GeneratedDiagnosticSampling;
using KK.GeneratedDiagnosticSampling.Host;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.FootIkSampling.Editor
{
    public sealed class CharacterPoseFootCaptureController : IDisposable
    {
        readonly DiagnosticCaptureStartRequest m_Request;
        readonly CharacterFootIkCaptureMetadata m_Metadata;
        readonly DiagnosticEventTargetKey m_Target;
        readonly string m_SamplerId;
        readonly CharacterFootIkCoreCaptureProgram.DiagnosticLifecycle
            m_CoreLifecycle;
        readonly CharacterFootIkFullCaptureProgram.DiagnosticLifecycle
            m_FullLifecycle;
        Task<DiagnosticHostFinalizationResult> m_HostFinalizationTask;
        bool m_Started;
        bool m_Stopped;

        public CharacterPoseFootCaptureController(
            CharacterPoseSamplingTarget target,
            DiagnosticCaptureStartRequest request,
            string samplerId,
            in CharacterFootIkCaptureMetadata metadata)
        {
            if (!target.Identity.IsValid)
                throw new ArgumentNullException(nameof(target));
            m_Request = request ?? throw new ArgumentNullException(nameof(request));
            m_SamplerId = samplerId ?? throw new ArgumentNullException(nameof(samplerId));
            m_Metadata = metadata;
            m_Target = new DiagnosticEventTargetKey(
                CharacterPoseFootDiagnosticEvent.TargetTypeIdentity,
                target.RuntimeInstanceId);
            if (string.Equals(
                    m_SamplerId,
                    CharacterFootIkDiagnosticIdentity.CoreSamplerId,
                    StringComparison.Ordinal))
            {
                m_CoreLifecycle =
                    CharacterFootIkCoreCaptureProgram.CreateDiagnosticLifecycle();
            }
            else if (string.Equals(
                         m_SamplerId,
                         CharacterFootIkDiagnosticIdentity.FullSamplerId,
                         StringComparison.Ordinal))
            {
                m_FullLifecycle =
                    CharacterFootIkFullCaptureProgram.CreateDiagnosticLifecycle();
            }
            else
            {
                throw new ArgumentException(
                    $"Foot IK sampler is unknown: {m_SamplerId}.",
                    nameof(samplerId));
            }
        }

        public bool HasStarted => m_Started;
        public DiagnosticCaptureFailure? Failure =>
            m_CoreLifecycle != null
                ? m_CoreLifecycle.Failure
                : m_FullLifecycle.Failure;
        public int CapturedFrameCount => checked(
            (int)((m_CoreLifecycle != null
                ? m_CoreLifecycle.SubmittedSampleCount
                : m_FullLifecycle.SubmittedSampleCount) / 2));

        public bool Start(bool attach = true)
        {
            if (m_Started || m_Stopped)
                throw new InvalidOperationException(
                    "Foot IK capture controller already started.");
            return !attach || StartLifecycle();
        }

        public void Attach()
        {
            if (m_Started || m_Stopped)
                throw new InvalidOperationException(
                    "Foot IK capture window cannot be opened.");
            if (!StartLifecycle())
                throw new InvalidOperationException(
                    Failure?.Message ??
                    "Foot IK generated sampling failed to start.");
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
                        "Foot IK capture window was not opened.");
                m_Stopped = true;
                return true;
            }
            if (m_Stopped)
                return !Failure.HasValue;
            m_Stopped = true;
            return m_CoreLifecycle != null
                ? m_CoreLifecycle.Stop(in outcome)
                : m_FullLifecycle.Stop(in outcome);
        }

        public bool TryFinalize(
            string outputRoot,
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
                    schema = CharacterFootIkCoreCaptureProgram
                        .CreateDiagnosticSchemaLayout();
                    capability = CharacterFootIkCoreCaptureProgram
                        .CreateDiagnosticCapabilityBuildDescriptor(
                            m_Request.InterestIdentity,
                            m_Request.CadenceIdentity,
                            m_Request.LineageTypeIdentity,
                            m_Request.PacketCapacity,
                            m_Request.WriterTransportIdentity);
                }
                else
                {
                    schema = CharacterFootIkFullCaptureProgram
                        .CreateDiagnosticSchemaLayout();
                    capability = CharacterFootIkFullCaptureProgram
                        .CreateDiagnosticCapabilityBuildDescriptor(
                            m_Request.InterestIdentity,
                            m_Request.CadenceIdentity,
                            m_Request.LineageTypeIdentity,
                            m_Request.PacketCapacity,
                            m_Request.WriterTransportIdentity);
                }
                string finalOutputRoot = Path.GetFullPath(outputRoot);
                m_HostFinalizationTask = Task.Run(() =>
                    FinalizeHost(
                        capability,
                        artifact,
                        schema,
                        finalOutputRoot));
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
                ? m_CoreLifecycle.Start(
                    m_Request,
                    in m_Target,
                    in m_Metadata)
                : m_FullLifecycle.Start(
                    m_Request,
                    in m_Target,
                    in m_Metadata);
            if (!started)
            {
                return false;
            }
            m_Started = true;
            return true;
        }

        static DiagnosticHostFinalizationResult FinalizeHost(
            DiagnosticCapabilityBuildDescriptor capability,
            DiagnosticSealedArtifact artifact,
            DiagnosticSchemaLayout schema,
            string outputRoot)
        {
            DiagnosticHostFinalizationResult result =
                new DiagnosticHostFinalizer().Finalize(
                    capability,
                    DiagnosticArtifactStore.Open(artifact),
                    schema,
                    outputRoot);
            DiagnosticArtifactStore.Seal(
                Path.Combine(outputRoot, "capability.manifest.json"),
                result.EncodedManifest);
            return result;
        }
    }
}
