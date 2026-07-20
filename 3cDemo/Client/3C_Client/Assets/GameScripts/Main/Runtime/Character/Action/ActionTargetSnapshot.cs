using System;
using TreeDesigner;
using UnityEngine;

namespace ThirdPersonCharacter.ActionSystem
{
    [Serializable]
    public struct ActionTargetSnapshot
    {
        public static ActionTargetSnapshot None => new ActionTargetSnapshot(string.Empty, Vector3.zero, Quaternion.identity);

        [SerializeField] string m_TargetId;
        [SerializeField] Vector3 m_Position;
        [SerializeField] Quaternion m_Rotation;

        public ActionTargetSnapshot(string targetId, Vector3 position, Quaternion rotation)
        {
            m_TargetId = targetId ?? string.Empty;
            m_Position = position;
            m_Rotation = rotation;
        }

        public string TargetId => m_TargetId ?? string.Empty;
        public Vector3 Position => m_Position;
        public Quaternion Rotation => m_Rotation;
        public bool HasTarget => !string.IsNullOrEmpty(TargetId);
    }

    [Serializable]
    public sealed class ActionTargetSnapshotExposedProperty : BaseExposedProperty<ActionTargetSnapshot>
    {
    }
}
