using System;
using System.Collections.Generic;
using System.Threading;

namespace ThirdPerson.GeneratedDiagnosticSampling
{
    public interface IDiagnosticPacketWriter : IDisposable
    {
        DiagnosticSealedArtifact Artifact { get; }
        void Begin(
            DiagnosticCapabilityBuildDescriptor capability,
            DiagnosticPacketLayout layout);
        void Write(DiagnosticCapturePacket packet);
        void Complete();
        void Fault(in DiagnosticCaptureFailure failure);
    }

    public sealed class DiagnosticCaptureSession : IDisposable
    {
        readonly object m_Gate = new object();
        readonly Queue<DiagnosticCapturePacket> m_Available;
        readonly Queue<DiagnosticCapturePacket> m_Pending;
        readonly int m_QueueCapacity;
        readonly AutoResetEvent m_Signal = new AutoResetEvent(false);
        readonly IDiagnosticPacketWriter m_Writer;
        readonly Thread m_WriterThread;
        bool m_StopRequested;
        bool m_Disposed;
        ulong m_LastSubmittedSequence;
        ulong m_SubmittedSampleCount;

        public DiagnosticCaptureSession(
            DiagnosticCapabilityBuildDescriptor capability,
            DiagnosticPacketLayout layout,
            int packetCapacity,
            int queueCapacity,
            IDiagnosticPacketWriter writer)
        {
            Capability = capability ?? throw new ArgumentNullException(nameof(capability));
            Layout = layout ?? throw new ArgumentNullException(nameof(layout));
            if (capability.Mode != DiagnosticCapabilityMode.Capture)
                throw new ArgumentException("Capture session requires a Capture capability.", nameof(capability));
            if (!string.Equals(
                    capability.PacketLayoutIdentity,
                    layout.Identity,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException("Capability and packet layout identities do not match.");
            }
            if (packetCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(packetCapacity));
            if (queueCapacity <= 0 || queueCapacity >= packetCapacity)
                throw new ArgumentOutOfRangeException(nameof(queueCapacity));
            m_QueueCapacity = queueCapacity;
            m_Writer = writer ?? throw new ArgumentNullException(nameof(writer));
            m_Available = new Queue<DiagnosticCapturePacket>(packetCapacity);
            m_Pending = new Queue<DiagnosticCapturePacket>(queueCapacity);
            for (int i = 0; i < packetCapacity; i++)
                m_Available.Enqueue(new DiagnosticCapturePacket(layout));
            Status = DiagnosticCaptureStatus.Prepared;
            m_WriterThread = new Thread(WriteLoop)
            {
                IsBackground = true,
                Name = $"DiagnosticCaptureWriter:{capability.CapabilityId}"
            };
        }

        public DiagnosticCapabilityBuildDescriptor Capability { get; }
        public DiagnosticPacketLayout Layout { get; }
        public DiagnosticCaptureStatus Status { get; private set; }
        public DiagnosticCaptureFailure? Failure { get; private set; }
        public ulong SubmittedSampleCount => m_SubmittedSampleCount;

        public void Start()
        {
            lock (m_Gate)
            {
                RequireStatus(DiagnosticCaptureStatus.Prepared);
                m_Writer.Begin(Capability, Layout);
                Status = DiagnosticCaptureStatus.Capturing;
                m_WriterThread.Start();
            }
        }

        public bool TryRent(in DiagnosticSampleKey sampleKey, out DiagnosticCapturePacket packet)
        {
            lock (m_Gate)
            {
                if (Status != DiagnosticCaptureStatus.Capturing)
                {
                    packet = null;
                    return false;
                }
                if (!string.Equals(
                        sampleKey.Lineage.TypeIdentity,
                        Capability.LineageTypeIdentity,
                        StringComparison.Ordinal))
                {
                    packet = null;
                    SetFaultLocked(new DiagnosticCaptureFailure(
                        DiagnosticCaptureFailureStage.Capture,
                        Capability.CapabilityId,
                        Capability.ProgramId,
                        string.Empty,
                        string.Empty,
                        "Diagnostic lineage type does not match the capability."));
                    return false;
                }
                if (m_Available.Count == 0)
                {
                    packet = null;
                    SetFaultLocked(new DiagnosticCaptureFailure(
                        DiagnosticCaptureFailureStage.Queue,
                        Capability.CapabilityId,
                        Capability.ProgramId,
                        string.Empty,
                        string.Empty,
                        "Diagnostic packet pool is exhausted."));
                    return false;
                }
                packet = m_Available.Dequeue();
                packet.Begin(sampleKey);
                return true;
            }
        }

        public bool Submit(DiagnosticCapturePacket packet)
        {
            if (packet == null)
                throw new ArgumentNullException(nameof(packet));
            lock (m_Gate)
            {
                if (Status != DiagnosticCaptureStatus.Capturing)
                {
                    ReturnLocked(packet);
                    return false;
                }
                if (!ReferenceEquals(packet.Layout, Layout))
                    throw new ArgumentException("Packet does not belong to this session.", nameof(packet));
                if (packet.SampleKey.Sequence <= m_LastSubmittedSequence)
                {
                    ReturnLocked(packet);
                    SetFaultLocked(new DiagnosticCaptureFailure(
                        DiagnosticCaptureFailureStage.Capture,
                        Capability.CapabilityId,
                        Capability.ProgramId,
                        string.Empty,
                        string.Empty,
                        "Diagnostic sample sequence is not strictly increasing."));
                    return false;
                }
                if (m_Pending.Count >= m_QueueCapacity)
                {
                    ReturnLocked(packet);
                    SetFaultLocked(new DiagnosticCaptureFailure(
                        DiagnosticCaptureFailureStage.Queue,
                        Capability.CapabilityId,
                        Capability.ProgramId,
                        string.Empty,
                        string.Empty,
                        "Diagnostic packet queue overflowed."));
                    return false;
                }
                m_LastSubmittedSequence = packet.SampleKey.Sequence;
                m_SubmittedSampleCount++;
                m_Pending.Enqueue(packet);
                m_Signal.Set();
                return true;
            }
        }

        public void Complete()
        {
            lock (m_Gate)
            {
                RequireStatus(DiagnosticCaptureStatus.Capturing);
                Status = DiagnosticCaptureStatus.Finalizing;
                m_StopRequested = true;
                m_Signal.Set();
            }
            m_WriterThread.Join();
        }

        public DiagnosticRuntimeManifest CreateRuntimeManifest(
            DiagnosticSealedArtifact schemaArtifact)
        {
            if (schemaArtifact == null)
                throw new ArgumentNullException(nameof(schemaArtifact));
            lock (m_Gate)
            {
                if (m_WriterThread.IsAlive)
                    throw new InvalidOperationException("Diagnostic writer is still running.");
                if (Status == DiagnosticCaptureStatus.Finalizing)
                {
                    return new DiagnosticRuntimeManifest(
                        Capability,
                        Status,
                        m_SubmittedSampleCount,
                        schemaArtifact,
                        m_Writer.Artifact,
                        null);
                }
                if (Status == DiagnosticCaptureStatus.Faulted)
                {
                    return new DiagnosticRuntimeManifest(
                        Capability,
                        Status,
                        m_SubmittedSampleCount,
                        schemaArtifact,
                        m_Writer.Artifact,
                        Failure);
                }
                if (Status == DiagnosticCaptureStatus.Cancelled)
                {
                    return new DiagnosticRuntimeManifest(
                        Capability,
                        Status,
                        m_SubmittedSampleCount,
                        schemaArtifact,
                        m_Writer.Artifact,
                        null);
                }
                throw new InvalidOperationException("Diagnostic session has not reached a terminal runtime state.");
            }
        }

        public void Cancel()
        {
            lock (m_Gate)
            {
                if (Status == DiagnosticCaptureStatus.Finalizing && !m_WriterThread.IsAlive)
                    return;
                if (Status != DiagnosticCaptureStatus.Prepared &&
                    Status != DiagnosticCaptureStatus.Capturing &&
                    Status != DiagnosticCaptureStatus.Finalizing)
                {
                    return;
                }
                Status = DiagnosticCaptureStatus.Cancelled;
                m_StopRequested = true;
                m_Signal.Set();
            }
            if (m_WriterThread.IsAlive)
                m_WriterThread.Join();
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            Cancel();
            m_Writer.Dispose();
            m_Signal.Dispose();
            m_Disposed = true;
        }

        void WriteLoop()
        {
            try
            {
                while (true)
                {
                    DiagnosticCapturePacket packet = null;
                    lock (m_Gate)
                    {
                        if (m_Pending.Count > 0)
                            packet = m_Pending.Dequeue();
                        else if (m_StopRequested)
                            break;
                    }
                    if (packet == null)
                    {
                        m_Signal.WaitOne();
                        continue;
                    }
                    m_Writer.Write(packet);
                    lock (m_Gate)
                        ReturnLocked(packet);
                }
                DiagnosticCaptureStatus status;
                DiagnosticCaptureFailure? failure;
                lock (m_Gate)
                {
                    status = Status;
                    failure = Failure;
                }
                if (status == DiagnosticCaptureStatus.Finalizing)
                    m_Writer.Complete();
                else if (status == DiagnosticCaptureStatus.Faulted && failure.HasValue)
                    m_Writer.Fault(failure.Value);
            }
            catch (Exception exception)
            {
                var failure = new DiagnosticCaptureFailure(
                    DiagnosticCaptureFailureStage.Writer,
                    Capability.CapabilityId,
                    Capability.ProgramId,
                    string.Empty,
                    string.Empty,
                    exception.Message);
                lock (m_Gate)
                    SetFaultLocked(failure);
                m_Writer.Fault(failure);
            }
        }

        void SetFaultLocked(in DiagnosticCaptureFailure failure)
        {
            if (Status == DiagnosticCaptureStatus.Faulted ||
                Status == DiagnosticCaptureStatus.Completed ||
                Status == DiagnosticCaptureStatus.Cancelled)
            {
                return;
            }
            Failure = failure;
            Status = DiagnosticCaptureStatus.Faulted;
            m_StopRequested = true;
            m_Signal.Set();
        }

        void ReturnLocked(DiagnosticCapturePacket packet) => m_Available.Enqueue(packet);

        void RequireStatus(DiagnosticCaptureStatus expected)
        {
            if (Status != expected)
                throw new InvalidOperationException($"Expected diagnostic status {expected}, got {Status}.");
        }
    }
}
