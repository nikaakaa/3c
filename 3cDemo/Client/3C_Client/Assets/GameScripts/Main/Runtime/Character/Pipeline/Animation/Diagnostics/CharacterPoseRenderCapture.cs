using KK.GeneratedDiagnosticSampling;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.Diagnostics
{
    public readonly struct CharacterPoseRenderCaptureFrame
    {
        internal CharacterPoseRenderCaptureFrame(int unityFrame, int cameraId, bool beforeAvailable,
            bool afterAvailable, double writtenTime, double beforeTime, double afterTime,
            CharacterPoseRenderBoneCaptureRow[] bones)
        {
            UnityFrame = unityFrame;
            CameraInstanceId = cameraId;
            BeforeRenderAvailable = beforeAvailable;
            AfterRenderAvailable = afterAvailable;
            WrittenTime = writtenTime;
            BeforeRenderTime = beforeTime;
            AfterRenderTime = afterTime;
            Bones = new CharacterPoseRenderBoneCapturePage(bones);
        }
        [DiagnosticField, DiagnosticGroup("animation-render")]
        public int UnityFrame { get; }
        [DiagnosticField, DiagnosticGroup("animation-render")]
        public int CameraInstanceId { get; }
        [DiagnosticField, DiagnosticGroup("animation-render")]
        public bool BeforeRenderAvailable { get; }
        [DiagnosticField, DiagnosticGroup("animation-render")]
        public bool AfterRenderAvailable { get; }
        [DiagnosticField, DiagnosticGroup("animation-render")]
        public double WrittenTime { get; }
        [DiagnosticField, DiagnosticGroup("animation-render")]
        public double BeforeRenderTime { get; }
        [DiagnosticField, DiagnosticGroup("animation-render")]
        public double AfterRenderTime { get; }
        [DiagnosticTable("render-bones", 1, 13), DiagnosticGroup("animation-render")]
        public CharacterPoseRenderBoneCapturePage Bones { get; }
    }

    public readonly struct CharacterPoseRenderBoneCapturePage
    {
        readonly CharacterPoseRenderBoneCaptureRow[] m_Rows;
        internal CharacterPoseRenderBoneCapturePage(CharacterPoseRenderBoneCaptureRow[] rows) => m_Rows = rows;
        public int Count => m_Rows == null ? 0 : m_Rows.Length;
        public CharacterPoseRenderBoneCaptureRow this[int index] => m_Rows[index];
    }

    internal readonly struct CharacterPoseRenderTransformSnapshot
    {
        internal CharacterPoseRenderTransformSnapshot(Transform transform)
        {
            LocalPosition = transform.localPosition;
            LocalRotation = transform.localRotation;
            LocalScale = transform.localScale;
            WorldPosition = transform.position;
            WorldRotation = transform.rotation;
        }
        internal Vector3 LocalPosition { get; }
        internal Quaternion LocalRotation { get; }
        internal Vector3 LocalScale { get; }
        internal Vector3 WorldPosition { get; }
        internal Quaternion WorldRotation { get; }
    }

    public readonly struct CharacterPoseRenderBoneCaptureRow
    {
        readonly CharacterPoseRenderTransformSnapshot m_Written;
        readonly CharacterPoseRenderTransformSnapshot m_Before;
        readonly CharacterPoseRenderTransformSnapshot m_After;
        internal CharacterPoseRenderBoneCaptureRow(string bone, bool beforeAvailable, bool afterAvailable,
            in CharacterPoseRenderTransformSnapshot written, in CharacterPoseRenderTransformSnapshot before,
            in CharacterPoseRenderTransformSnapshot after)
        {
            Bone = bone;
            BeforeRenderAvailable = beforeAvailable;
            AfterRenderAvailable = afterAvailable;
            m_Written = written;
            m_Before = before;
            m_After = after;
        }
        [DiagnosticField, DiagnosticGroup("animation-render")]
        public string Bone { get; }
        [DiagnosticField, DiagnosticGroup("animation-render")]
        public bool BeforeRenderAvailable { get; }
        [DiagnosticField, DiagnosticGroup("animation-render")]
        public bool AfterRenderAvailable { get; }
        [DiagnosticField, DiagnosticGroup("animation-render")]
        public Vector3 WrittenLocalPosition => m_Written.LocalPosition;
        [DiagnosticField, DiagnosticGroup("animation-render")]
        public Quaternion WrittenLocalRotation => m_Written.LocalRotation;
        [DiagnosticField, DiagnosticGroup("animation-render")]
        public Vector3 WrittenLocalScale => m_Written.LocalScale;
        [DiagnosticField, DiagnosticGroup("animation-render")]
        public Vector3 WrittenWorldPosition => m_Written.WorldPosition;
        [DiagnosticField, DiagnosticGroup("animation-render")]
        public Quaternion WrittenWorldRotation => m_Written.WorldRotation;
        [DiagnosticField, DiagnosticGroup("animation-render")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Member, nameof(BeforeRenderAvailable))]
        public Vector3 BeforeLocalPosition => m_Before.LocalPosition;
        [DiagnosticField, DiagnosticGroup("animation-render")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Member, nameof(BeforeRenderAvailable))]
        public Quaternion BeforeLocalRotation => m_Before.LocalRotation;
        [DiagnosticField, DiagnosticGroup("animation-render")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Member, nameof(BeforeRenderAvailable))]
        public Vector3 BeforeLocalScale => m_Before.LocalScale;
        [DiagnosticField, DiagnosticGroup("animation-render")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Member, nameof(BeforeRenderAvailable))]
        public Vector3 BeforeWorldPosition => m_Before.WorldPosition;
        [DiagnosticField, DiagnosticGroup("animation-render")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Member, nameof(BeforeRenderAvailable))]
        public Quaternion BeforeWorldRotation => m_Before.WorldRotation;
        [DiagnosticField, DiagnosticGroup("animation-render")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Member, nameof(BeforeRenderAvailable))]
        public float BeforePositionError => Vector3.Distance(m_Written.LocalPosition, m_Before.LocalPosition);
        [DiagnosticField, DiagnosticGroup("animation-render")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Member, nameof(BeforeRenderAvailable))]
        public float BeforeRotationErrorDegrees => Quaternion.Angle(m_Written.LocalRotation, m_Before.LocalRotation);
        [DiagnosticField, DiagnosticGroup("animation-render")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Member, nameof(BeforeRenderAvailable))]
        public float BeforeScaleError => Vector3.Distance(m_Written.LocalScale, m_Before.LocalScale);
        [DiagnosticField, DiagnosticGroup("animation-render")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Member, nameof(AfterRenderAvailable))]
        public Vector3 AfterLocalPosition => m_After.LocalPosition;
        [DiagnosticField, DiagnosticGroup("animation-render")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Member, nameof(AfterRenderAvailable))]
        public Quaternion AfterLocalRotation => m_After.LocalRotation;
        [DiagnosticField, DiagnosticGroup("animation-render")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Member, nameof(AfterRenderAvailable))]
        public Vector3 AfterLocalScale => m_After.LocalScale;
        [DiagnosticField, DiagnosticGroup("animation-render")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Member, nameof(AfterRenderAvailable))]
        public Vector3 AfterWorldPosition => m_After.WorldPosition;
        [DiagnosticField, DiagnosticGroup("animation-render")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Member, nameof(AfterRenderAvailable))]
        public Quaternion AfterWorldRotation => m_After.WorldRotation;
        [DiagnosticField, DiagnosticGroup("animation-render")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Member, nameof(AfterRenderAvailable))]
        public float AfterPositionError => Vector3.Distance(m_Written.LocalPosition, m_After.LocalPosition);
        [DiagnosticField, DiagnosticGroup("animation-render")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Member, nameof(AfterRenderAvailable))]
        public float AfterRotationErrorDegrees => Quaternion.Angle(m_Written.LocalRotation, m_After.LocalRotation);
        [DiagnosticField, DiagnosticGroup("animation-render")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Member, nameof(AfterRenderAvailable))]
        public float AfterScaleError => Vector3.Distance(m_Written.LocalScale, m_After.LocalScale);
    }
}
