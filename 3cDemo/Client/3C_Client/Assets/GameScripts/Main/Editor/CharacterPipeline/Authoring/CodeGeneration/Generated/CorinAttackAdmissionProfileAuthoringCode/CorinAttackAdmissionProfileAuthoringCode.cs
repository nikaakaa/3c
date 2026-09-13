using System;
using ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration;
using TimelineAnimationClip = BTSMTL.Timeline.AnimationClip;
using UnityObject = UnityEngine.Object;
using UnityAnimationClip = UnityEngine.AnimationClip;
using BTSMTL.Timeline;
using FlowCanvas;
using FlowCanvas.Macros;
using FlowCanvas.Nodes;
using NodeCanvas.Framework;
using ThirdPersonCamera;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;
using ThirdPersonGameplay.Attributes;
using ThirdPersonGameplay.Effects;
using ThirdPersonGameplay.Tags;
using ThirdPersonSimulation;
using TreeDesigner;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinAttackAdmissionProfileAuthoringCode : IBtsmtlAuthoringGenerationEntry
    {
        public BtsmtlAuthoringGenerationResult Execute(BtsmtlAuthoringGenerationContext context)
        {
            var generation = new GenerationState();
            BuildCreateRoot0(generation, context);
            return context.Complete(generation.admissionProfile);
        }

        static void BuildCreateRoot0(GenerationState generation, BtsmtlAuthoringGenerationContext context)
        {
            generation.admissionProfile = BtsmtlSkillAuthoringCode.EnsureAdmissionProfileRoot(context, "Attack", "Attack", "Local", new[] { new GameplayTagId("Attack") }, new GameplayTagQuery(Array.Empty<GameplayTagId>(), Array.Empty<GameplayTagId>(), Array.Empty<GameplayTagId>()), new GameplayTagQuery(Array.Empty<GameplayTagId>(), Array.Empty<GameplayTagId>(), Array.Empty<GameplayTagId>()), new GameplayTagQuery(Array.Empty<GameplayTagId>(), new[] { new GameplayTagId("Attack"), new GameplayTagId("Dodge") }, Array.Empty<GameplayTagId>()), ActionTargetRequirement.OptionalSnapshot, 1, "Assets/Configs/Character/Corin/Pipeline/Actions/Attack/CorinAttackActionProfile.asset");
        }

        sealed class GenerationState
        {
            internal GameplayAbilityAdmissionProfile admissionProfile;
        }
    }
}
