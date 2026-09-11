using TreeDesigner.Authoring;
using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Editor.CharacterSimulation;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;
using ThirdPersonCharacter.Pipeline.Presentation;
using TreeDesigner.Editor;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    [InitializeOnLoad]
    static class CharacterGraphAuthoringCapabilityBootstrap
    {
        static CharacterGraphAuthoringCapabilityBootstrap()
        {
            _ = new BtsmtlGraphAuthoringCapabilities();
            CharacterPoseGraphAuthoringCapabilities.EnsureRegistered();
        }
    }

}
