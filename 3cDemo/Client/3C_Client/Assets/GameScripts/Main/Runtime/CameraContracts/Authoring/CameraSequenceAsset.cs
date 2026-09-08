using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ThirdPersonCamera
{
[CreateAssetMenu(fileName = "CameraSequence", menuName = "3C/Character/Camera/Sequence")]
    public sealed class CameraSequenceAsset : ScriptableObject
    {
        public const string SchemaVersion = "character-camera-sequence/v1";

        [SerializeField] string m_Schema = SchemaVersion;
        [SerializeField] string m_SequenceId = string.Empty;
        [SerializeReference] CameraSequenceStage[] m_Stages = Array.Empty<CameraSequenceStage>();
        [SerializeField] CameraTimeDomain m_TimeDomain = CameraTimeDomain.PresentationScaled;

        public string Schema => m_Schema ?? string.Empty;
        public string SequenceId => m_SequenceId ?? string.Empty;
        public IReadOnlyList<CameraSequenceStage> Stages => m_Stages ?? Array.Empty<CameraSequenceStage>();
        public CameraTimeDomain TimeDomain => m_TimeDomain;

        public void RequireValid()
        {
            if (!string.Equals(Schema, SchemaVersion, StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(SequenceId) || Stages.Count == 0 ||
                !Enum.IsDefined(typeof(CameraTimeDomain), TimeDomain))
                throw new InvalidOperationException($"Camera Sequence Asset '{name}' is incomplete.");
            for (int i = 0; i < Stages.Count; i++)
            {
                CameraSequenceStage stage = Stages[i];
                if (stage == null)
                    throw new InvalidOperationException($"Camera Sequence Asset '{name}' stage #{i} is missing.");
                stage.RequireValid($"{name}.Stages[{i}]");
            }
        }
    }
}
