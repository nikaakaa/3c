using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    internal sealed class CharacterPoseFamilyPayloadBindingRequest
    {
        internal CharacterPoseFamilyPayloadBindingRequest(
            CharacterPoseFamilyPayloadPlanPass.BindingBuilder state,
            CharacterPoseNodeDefinition handler,
            CharacterPoseIrNode irNode,
            CharacterPoseCanvasNode node,
            PoseNodeId scopedNodeId,
            string scope,
            string callChain,
            int linkedPoseFragmentIndex,
            string linkedPoseFragmentIdentity,
            int inputA,
            int controlInputOperationIndex,
            int playerIndex,
            int blendNodeIndex)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            Handler = handler ?? throw new ArgumentNullException(nameof(handler));
            IrNode = irNode ?? throw new ArgumentNullException(nameof(irNode));
            Node = node ?? throw new ArgumentNullException(nameof(node));
            ScopedNodeId = scopedNodeId;
            Scope = scope ?? string.Empty;
            CallChain = callChain ?? string.Empty;
            LinkedPoseFragmentIndex = linkedPoseFragmentIndex;
            LinkedPoseFragmentIdentity = linkedPoseFragmentIdentity ?? string.Empty;
            InputA = inputA;
            ControlInputOperationIndex = controlInputOperationIndex;
            PlayerIndex = playerIndex;
            BlendNodeIndex = blendNodeIndex;
        }

        internal CharacterPoseFamilyPayloadPlanPass.BindingBuilder State { get; }
        internal CharacterPoseNodeDefinition Handler { get; }
        internal CharacterPoseIrNode IrNode { get; }
        internal CharacterPoseCanvasNode Node { get; }
        internal PoseNodeId ScopedNodeId { get; }
        internal string Scope { get; }
        internal string CallChain { get; }
        internal int LinkedPoseFragmentIndex { get; }
        internal string LinkedPoseFragmentIdentity { get; }
        internal int InputA { get; }
        internal int ControlInputOperationIndex { get; }
        internal int PlayerIndex { get; }
        internal int BlendNodeIndex { get; }
    }

    internal readonly struct CharacterPoseFamilyPayloadBindingResult
    {
        internal CharacterPoseFamilyPayloadBindingResult(
            int boneMaskIndex = -1,
            int additiveReferenceIndex = -1,
            int modifyBoneIndex = -1,
            int rootOrientationWarpIndex = -1,
            int poseBoneIkGoalsIndex = -1,
            int footPlacementIndex = -1,
            int fullBodyIkIndex = -1,
            int inertializationIndex = -1,
            int clipPlayerIndex = -1,
            int stateMachineIndex = -1,
            int animationSlotIndex = -1)
        {
            BoneMaskIndex = boneMaskIndex;
            AdditiveReferenceIndex = additiveReferenceIndex;
            ModifyBoneIndex = modifyBoneIndex;
            RootOrientationWarpIndex = rootOrientationWarpIndex;
            PoseBoneIkGoalsIndex = poseBoneIkGoalsIndex;
            FootPlacementIndex = footPlacementIndex;
            FullBodyIkIndex = fullBodyIkIndex;
            InertializationIndex = inertializationIndex;
            ClipPlayerIndex = clipPlayerIndex;
            StateMachineIndex = stateMachineIndex;
            AnimationSlotIndex = animationSlotIndex;
        }

        internal int BoneMaskIndex { get; }
        internal int AdditiveReferenceIndex { get; }
        internal int ModifyBoneIndex { get; }
        internal int RootOrientationWarpIndex { get; }
        internal int PoseBoneIkGoalsIndex { get; }
        internal int FootPlacementIndex { get; }
        internal int FullBodyIkIndex { get; }
        internal int InertializationIndex { get; }
        internal int ClipPlayerIndex { get; }
        internal int StateMachineIndex { get; }
        internal int AnimationSlotIndex { get; }
    }

    internal interface ICharacterPoseFamilyPayloadAdapter
    {
        CharacterPoseOperationFamily Family { get; }

        CharacterPoseFamilyPayloadBindingResult Bind(
            CharacterPoseFamilyPayloadBindingRequest request);
    }

    internal abstract class CharacterPoseFamilyPayloadAdapter :
        ICharacterPoseFamilyPayloadAdapter
    {
        protected CharacterPoseFamilyPayloadAdapter(
            CharacterPoseOperationFamily family)
        {
            Family = family;
        }

        public CharacterPoseOperationFamily Family { get; }

        public abstract CharacterPoseFamilyPayloadBindingResult Bind(
            CharacterPoseFamilyPayloadBindingRequest request);

        protected static int CompileMask(
            CharacterPoseFamilyPayloadBindingRequest request)
        {
            CharacterAnimationBoneMaskAsset boneMask =
                request.Handler.BoneMask(request.IrNode.Payload);
            return boneMask
                ? CharacterPoseFamilyPayloadPlanPass.CompileMask(
                    boneMask,
                    request.State.Rig,
                    request.State.Masks,
                    request.State.MaskIndices)
                : -1;
        }
    }

    internal sealed class CharacterPosePlayerPayloadAdapter :
        CharacterPoseFamilyPayloadAdapter
    {
        internal CharacterPosePlayerPayloadAdapter()
            : base(CharacterPoseOperationFamily.Player)
        {
        }

        public override CharacterPoseFamilyPayloadBindingResult Bind(
            CharacterPoseFamilyPayloadBindingRequest request)
        {
            int clipPlayerIndex = request.Handler.Requires(
                    CharacterPoseNodeRuntimeRequirement.ClipPlayer)
                ? CharacterPoseFamilyPayloadPlanPass.CompileClipPlayer(
                    CharacterPoseFamilyPayloadPlanPass.RequirePayload<
                        CharacterClipPlayerPosePayload>(request.IrNode),
                    request.ScopedNodeId,
                    request.PlayerIndex,
                    request.State)
                : -1;
            return new CharacterPoseFamilyPayloadBindingResult(
                boneMaskIndex: CompileMask(request),
                clipPlayerIndex: clipPlayerIndex);
        }
    }

    internal sealed class CharacterPoseStateMachinePayloadAdapter :
        CharacterPoseFamilyPayloadAdapter
    {
        internal CharacterPoseStateMachinePayloadAdapter()
            : base(CharacterPoseOperationFamily.StateMachine)
        {
        }

        public override CharacterPoseFamilyPayloadBindingResult Bind(
            CharacterPoseFamilyPayloadBindingRequest request)
        {
            int index = request.Handler.Requires(
                    CharacterPoseNodeRuntimeRequirement.StateMachine)
                ? CharacterPoseFamilyPayloadPlanPass.CompileStateMachine(
                    request.State.GraphAsset,
                    CharacterPoseFamilyPayloadPlanPass.RequirePayload<
                        CharacterPoseStateMachineNodePayload>(request.IrNode),
                    request.ScopedNodeId,
                    request.State,
                    request.Scope,
                    request.CallChain,
                    request.LinkedPoseFragmentIndex,
                    request.LinkedPoseFragmentIdentity)
                : -1;
            return new CharacterPoseFamilyPayloadBindingResult(
                stateMachineIndex: index,
                boneMaskIndex: CompileMask(request));
        }
    }

    internal sealed class CharacterPoseAnimationSlotPayloadAdapter :
        CharacterPoseFamilyPayloadAdapter
    {
        internal CharacterPoseAnimationSlotPayloadAdapter()
            : base(CharacterPoseOperationFamily.AnimationSlot)
        {
        }

        public override CharacterPoseFamilyPayloadBindingResult Bind(
            CharacterPoseFamilyPayloadBindingRequest request)
        {
            int boneMaskIndex = CompileMask(request);
            int index = request.Handler.Requires(
                    CharacterPoseNodeRuntimeRequirement.AnimationSlot)
                ? CharacterPoseFamilyPayloadPlanPass.CompileAnimationSlot(
                    CharacterPoseFamilyPayloadPlanPass.RequirePayload<
                        CharacterAnimationSlotPosePayload>(request.IrNode),
                    request.ScopedNodeId,
                    request.InputA,
                    request.ControlInputOperationIndex,
                    request.PlayerIndex,
                    request.BlendNodeIndex,
                    request.State)
                : -1;
            return new CharacterPoseFamilyPayloadBindingResult(
                animationSlotIndex: index,
                boneMaskIndex: boneMaskIndex);
        }
    }

    internal sealed class CharacterPoseInertializationPayloadAdapter :
        CharacterPoseFamilyPayloadAdapter
    {
        internal CharacterPoseInertializationPayloadAdapter()
            : base(CharacterPoseOperationFamily.Inertialization)
        {
        }

        public override CharacterPoseFamilyPayloadBindingResult Bind(
            CharacterPoseFamilyPayloadBindingRequest request)
        {
            int index = request.Handler.Requires(
                    CharacterPoseNodeRuntimeRequirement.Inertialization)
                ? CharacterPoseFamilyPayloadPlanPass.CompileInertialization(
                    request.State)
                : -1;
            return new CharacterPoseFamilyPayloadBindingResult(
                inertializationIndex: index,
                boneMaskIndex: CompileMask(request));
        }
    }

    internal sealed class CharacterPoseCompositionPayloadAdapter :
        CharacterPoseFamilyPayloadAdapter
    {
        internal CharacterPoseCompositionPayloadAdapter()
            : base(CharacterPoseOperationFamily.Composition)
        {
        }

        public override CharacterPoseFamilyPayloadBindingResult Bind(
            CharacterPoseFamilyPayloadBindingRequest request)
        {
            int boneMaskIndex = CompileMask(request);
            int additiveIndex = request.Handler.Requires(
                    CharacterPoseNodeRuntimeRequirement.Additive)
                ? CharacterPoseFamilyPayloadPlanPass.CompileAdditiveReference(
                    CharacterPoseFamilyPayloadPlanPass.RequirePayload<
                        CharacterAdditivePosePayload>(request.IrNode),
                    request.State.Rig,
                    request.State.AdditiveReferences)
                : -1;
            return new CharacterPoseFamilyPayloadBindingResult(
                boneMaskIndex: boneMaskIndex,
                additiveReferenceIndex: additiveIndex);
        }
    }

    internal sealed class CharacterPoseComponentControlPayloadAdapter :
        CharacterPoseFamilyPayloadAdapter
    {
        internal CharacterPoseComponentControlPayloadAdapter()
            : base(CharacterPoseOperationFamily.ComponentControl)
        {
        }

        public override CharacterPoseFamilyPayloadBindingResult Bind(
            CharacterPoseFamilyPayloadBindingRequest request)
        {
            int boneMaskIndex = CompileMask(request);
            int modifyIndex = request.Handler.Requires(
                    CharacterPoseNodeRuntimeRequirement.ModifyBone)
                ? CharacterPoseFamilyPayloadPlanPass.CompileModifyBone(
                    CharacterPoseFamilyPayloadPlanPass.RequirePayload<
                        CharacterModifyBonePosePayload>(request.IrNode),
                    request.State)
                : -1;
            int rootOrientationWarpIndex = request.Handler.Requires(
                    CharacterPoseNodeRuntimeRequirement.RootOrientationWarp)
                ? CharacterPoseFamilyPayloadPlanPass.CompileRootOrientationWarp(
                    CharacterPoseFamilyPayloadPlanPass.RequirePayload<
                        CharacterRootOrientationWarpPosePayload>(request.IrNode),
                    request.ScopedNodeId,
                    request.InputA,
                    request.State)
                : -1;
            return new CharacterPoseFamilyPayloadBindingResult(
                boneMaskIndex: boneMaskIndex,
                modifyBoneIndex: modifyIndex,
                rootOrientationWarpIndex: rootOrientationWarpIndex);
        }
    }

    internal sealed class CharacterPoseGoalContributionPayloadAdapter :
        CharacterPoseFamilyPayloadAdapter
    {
        internal CharacterPoseGoalContributionPayloadAdapter()
            : base(CharacterPoseOperationFamily.GoalContribution)
        {
        }

        public override CharacterPoseFamilyPayloadBindingResult Bind(
            CharacterPoseFamilyPayloadBindingRequest request)
        {
            int boneMaskIndex = CompileMask(request);
            int poseBoneIkGoalsIndex = request.Handler.Kind ==
                    CharacterPoseNodeKind.PoseBoneIKGoals
                ? CharacterPoseFamilyPayloadPlanPass.CompilePoseBoneIkGoals(
                    CharacterPoseFamilyPayloadPlanPass.RequirePayload<
                        CharacterPoseBoneIkGoalsPayload>(request.IrNode),
                    request.ScopedNodeId,
                    request.State)
                : -1;
            int footPlacementIndex = request.Handler.Kind ==
                    CharacterPoseNodeKind.FootPlacement
                ? CharacterPoseFamilyPayloadPlanPass.CompileFootPlacement(
                    CharacterPoseFamilyPayloadPlanPass.RequirePayload<
                        CharacterFootPlacementPosePayload>(request.IrNode),
                    request.ScopedNodeId,
                    request.State)
                : -1;
            return new CharacterPoseFamilyPayloadBindingResult(
                boneMaskIndex: boneMaskIndex,
                poseBoneIkGoalsIndex: poseBoneIkGoalsIndex,
                footPlacementIndex: footPlacementIndex);
        }
    }

    internal sealed class CharacterPoseFullBodyIkPayloadAdapter :
        CharacterPoseFamilyPayloadAdapter
    {
        internal CharacterPoseFullBodyIkPayloadAdapter()
            : base(CharacterPoseOperationFamily.FullBodyIk)
        {
        }

        public override CharacterPoseFamilyPayloadBindingResult Bind(
            CharacterPoseFamilyPayloadBindingRequest request)
        {
            int boneMaskIndex = CompileMask(request);
            int index = request.Handler.Kind == CharacterPoseNodeKind.FullBodyIK
                ? CharacterPoseFamilyPayloadPlanPass.CompileFullBodyIk(
                    request.ScopedNodeId,
                    request.State)
                : -1;
            return new CharacterPoseFamilyPayloadBindingResult(
                boneMaskIndex: boneMaskIndex,
                fullBodyIkIndex: index);
        }
    }

    internal sealed class CharacterPoseFamilyPayloadAdapterCatalog
    {
        readonly Dictionary<CharacterPoseOperationFamily,
            ICharacterPoseFamilyPayloadAdapter> m_Adapters;

        internal CharacterPoseFamilyPayloadAdapterCatalog()
        {
            m_Adapters = new ICharacterPoseFamilyPayloadAdapter[]
            {
                new CharacterPosePlayerPayloadAdapter(),
                new CharacterPoseStateMachinePayloadAdapter(),
                new CharacterPoseAnimationSlotPayloadAdapter(),
                new CharacterPoseInertializationPayloadAdapter(),
                new CharacterPoseCompositionPayloadAdapter(),
                new CharacterPoseComponentControlPayloadAdapter(),
                new CharacterPoseGoalContributionPayloadAdapter(),
                new CharacterPoseFullBodyIkPayloadAdapter()
            }.ToDictionary(value => value.Family);
        }

        internal CharacterPoseFamilyPayloadBindingResult Bind(
            CharacterPoseOperationFamily family,
            CharacterPoseFamilyPayloadBindingRequest request)
        {
            if (!m_Adapters.TryGetValue(family, out ICharacterPoseFamilyPayloadAdapter adapter))
            {
                CharacterAnimationBoneMaskAsset boneMask =
                    request.Handler.BoneMask(request.IrNode.Payload);
                return new CharacterPoseFamilyPayloadBindingResult(
                    boneMaskIndex: boneMask
                        ? CharacterPoseFamilyPayloadPlanPass.CompileMask(
                            boneMask,
                            request.State.Rig,
                            request.State.Masks,
                            request.State.MaskIndices)
                        : -1);
            }
            return adapter.Bind(request);
        }
    }
}
