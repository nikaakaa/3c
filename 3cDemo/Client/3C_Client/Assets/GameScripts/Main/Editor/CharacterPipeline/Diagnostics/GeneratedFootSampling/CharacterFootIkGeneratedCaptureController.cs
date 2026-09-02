using System;
using System.IO;
using KK.GeneratedDiagnosticSampling;
using KK.GeneratedDiagnosticSampling.Host;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.FootIkSampling.Editor
{
    public sealed class CharacterFootIkGeneratedCaptureController : IDisposable
    {
        readonly Guid m_OwnerId = Guid.NewGuid();
        readonly AnimationPresentationRuntimeTarget m_Target;
        readonly DiagnosticCaptureStartRequest m_Request;
        readonly CharacterFootIkGeneratedCapture m_Capture;
        bool m_Registered;

        public CharacterFootIkGeneratedCaptureController(
            AnimationPresentationRuntimeTarget target,
            DiagnosticCaptureStartRequest request,
            in CharacterFootIkCaptureMetadata metadata)
        {
            m_Target = target ?? throw new ArgumentNullException(nameof(target));
            m_Request = request ?? throw new ArgumentNullException(nameof(request));
            m_Capture = new CharacterFootIkGeneratedCapture(in metadata);
        }

        public Guid OwnerId => m_OwnerId;
        public DiagnosticCaptureFailure? Failure => m_Capture.Failure;
        public int CapturedFrameCount => m_Capture.CapturedFrameCount;

        public bool Start(bool attach = true)
        {
            if (!m_Capture.Start(m_Request))
                return false;
            if (attach)
                Attach();
            return true;
        }

        public void Attach()
        {
            if (m_Registered)
                return;
            try
            {
                m_Target.SetFootIkCapture(
                    m_OwnerId,
                    new CharacterFootIkCaptureInterest(1),
                    m_Capture);
            }
            catch (Exception failure)
            {
                m_Capture.CaptureFault(failure);
                throw;
            }
            m_Registered = true;
        }

        public void Detach()
        {
            if (!m_Registered)
                return;
            m_Target.RemoveFootIkCapture(m_OwnerId);
            m_Registered = false;
        }

        public bool Stop(in DiagnosticCaptureStopOutcome outcome)
        {
            Detach();
            return m_Capture.Stop(in outcome);
        }

        public bool TryFinalize(
            string outputRoot,
            out DiagnosticHostFinalizationResult result)
        {
            if (!m_Capture.TryGetRuntimeManifest(
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
            Detach();
            m_Capture.Dispose();
        }
    }
}
