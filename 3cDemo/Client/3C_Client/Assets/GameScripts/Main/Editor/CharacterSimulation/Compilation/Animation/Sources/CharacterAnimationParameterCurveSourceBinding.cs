using System;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEditor;

namespace ThirdPersonCharacter.Editor.CharacterSimulation.Compilation.Animation
{
    internal sealed class CharacterAnimationParameterCurveSourceBinding
    {
        internal CharacterAnimationParameterCurveSourceBinding(
            PoseParameterId parameterId,
            EditorCurveBinding curveBinding)
        {
            if (!parameterId.IsValid)
                throw new ArgumentException("Animation parameter identity is invalid.", nameof(parameterId));
            if (curveBinding.type == null || string.IsNullOrWhiteSpace(curveBinding.propertyName))
                throw new ArgumentException("Animation parameter curve binding is invalid.", nameof(curveBinding));
            ParameterId = parameterId;
            CurveBinding = curveBinding;
        }

        internal PoseParameterId ParameterId { get; }
        internal EditorCurveBinding CurveBinding { get; }
    }
}
