using BTSMTL.Authoring.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Editor.CharacterSimulation;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;
using ThirdPersonCharacter.Pipeline.Presentation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    [InitializeOnLoad]
    static class CharacterGraphAuthoringCapabilityBootstrap
    {
        static CharacterGraphAuthoringCapabilityBootstrap()
        {
            CharacterPoseGraphCapabilityProjector.EnsureRegistered();
        }
    }

}
