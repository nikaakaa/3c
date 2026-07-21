using System;
using TreeDesigner;
using UnityEngine;

namespace ThirdPersonCharacter.AI
{
    [Serializable]
    public struct AIActorIdValue
    {
        [SerializeField] string m_Value;

        public AIActorIdValue(string value)
        {
            m_Value = value ?? string.Empty;
        }

        public string Value => m_Value ?? string.Empty;
    }

    [Serializable]
    public struct AIActionTargetSnapshotValue
    {
        [SerializeField] AIActorIdValue m_Target;
        [SerializeField] Vector3 m_Position;
        [SerializeField] float m_Yaw;

        public AIActionTargetSnapshotValue(AIActorIdValue target, Vector3 position, float yaw)
        {
            m_Target = target;
            m_Position = position;
            m_Yaw = yaw;
        }

        public AIActorIdValue Target => m_Target;
        public Vector3 Position => m_Position;
        public float Yaw => m_Yaw;
    }

    [Serializable]
    public sealed class AIActorIdExposedProperty : BaseExposedProperty<AIActorIdValue>
    {
    }

    [Serializable]
    public sealed class AIActionTargetSnapshotExposedProperty : BaseExposedProperty<AIActionTargetSnapshotValue>
    {
    }

    [Serializable]
    public sealed class AIActorIdPropertyPort : PropertyPort<AIActorIdValue>
    {
    }

    [Serializable]
    public sealed class AIActionTargetSnapshotPropertyPort : PropertyPort<AIActionTargetSnapshotValue>
    {
    }
}
