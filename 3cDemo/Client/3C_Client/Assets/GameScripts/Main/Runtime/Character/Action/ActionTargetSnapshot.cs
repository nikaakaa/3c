using UnityEngine;

namespace ThirdPersonCharacter.ActionSystem
{
    public readonly struct ActionTargetSnapshot
    {
        public static ActionTargetSnapshot None => new ActionTargetSnapshot(string.Empty, Vector3.zero, Quaternion.identity);

        public ActionTargetSnapshot(string targetId, Vector3 position, Quaternion rotation)
        {
            TargetId = targetId ?? string.Empty;
            Position = position;
            Rotation = rotation;
        }

        public string TargetId { get; }
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public bool HasTarget => !string.IsNullOrEmpty(TargetId);
    }
}
