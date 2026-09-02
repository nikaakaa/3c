using System;
using System.IO;
using KK.GeneratedDiagnosticSampling;
using KK.GeneratedDiagnosticSampling.Host;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.FootIkSampling.Editor
{
    public sealed class CharacterFootIkGeneratedCaptureController : IDisposable
    {
        readonly DiagnosticCaptureStartRequest m_Request;
        readonly CharacterFootIkCaptureMetadata m_Metadata;
        readonly DiagnosticEventTargetKey m_Target;
        readonly CharacterFootIkFullCaptureProgram.DiagnosticLifecycle
            m_Lifecycle;
        bool m_Started;
        bool m_Stopped;

        public CharacterFootIkGeneratedCaptureController(
            AnimationPresentationRuntimeTarget target,
            DiagnosticCaptureStartRequest request,
            in CharacterFootIkCaptureMetadata metadata)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            m_Request = request ?? throw new ArgumentNullException(nameof(request));
            m_Metadata = metadata;
            m_Target = new DiagnosticEventTargetKey(
                CharacterFootIkCommitDiagnosticEvent.TargetTypeIdentity,
                target.RuntimeInstanceId);
            m_Lifecycle =
                CharacterFootIkFullCaptureProgram.CreateDiagnosticLifecycle();
        }

        public DiagnosticCaptureFailure? Failure => m_Lifecycle.Failure;
        public int CapturedFrameCount => checked(
            (int)(m_Lifecycle.SubmittedSampleCount / 2));

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
            return m_Lifecycle.Stop(in outcome);
        }

        public bool TryFinalize(
            string outputRoot,
            out DiagnosticHostFinalizationResult result)
        {
            if (!m_Lifecycle.TryGetRuntimeManifest(
                    out DiagnosticRuntimeManifest manifest,
                    out DiagnosticSealedArtifact artifact))
            {
                result = null;
                return false;
            }
            DiagnosticSchemaLayout schema =
                CharacterFootIkFullCaptureProgram.CreateDiagnosticSchemaLayout();
            DiagnosticCapabilityBuildDescriptor capability =
                CharacterFootIkFullCaptureProgram
                    .CreateDiagnosticCapabilityBuildDescriptor(
                        m_Request.InterestIdentity,
                        m_Request.CadenceIdentity,
                        m_Request.LineageTypeIdentity,
                        m_Request.PacketCapacity,
                        m_Request.WriterTransportIdentity);
            result = new DiagnosticHostFinalizer().Finalize(
                capability,
                DiagnosticArtifactStore.Open(artifact),
                schema,
                outputRoot);
            DiagnosticArtifactStore.Seal(
                Path.Combine(
                    Path.GetFullPath(outputRoot),
                    "capability.manifest.json"),
                result.EncodedManifest);
            return result.Manifest.Status == DiagnosticCaptureStatus.Completed;
        }

        public void Dispose()
        {
            m_Lifecycle.Dispose();
        }

        bool StartLifecycle()
        {
            if (!m_Lifecycle.Start(
                    m_Request,
                    in m_Target,
                    in m_Metadata))
            {
                return false;
            }
            m_Started = true;
            return true;
        }
    }
}
