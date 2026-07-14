using UnityEngine;
using UnityEngine.InputSystem;

namespace TreeDesigner
{
    public interface IInputActionValueSource
    {
        bool TryReadButton(InputActionAsset sourceAsset, string actionId, out bool value);
        bool TryReadFloat(InputActionAsset sourceAsset, string actionId, out float value);
        bool TryReadVector2(InputActionAsset sourceAsset, string actionId, out Vector2 value);
    }
}
