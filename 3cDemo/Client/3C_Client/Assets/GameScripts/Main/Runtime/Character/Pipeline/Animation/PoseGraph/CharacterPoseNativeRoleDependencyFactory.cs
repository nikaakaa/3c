using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonCharacter.Pipeline.Animation.Sources;
using ThirdPersonCharacter.Pipeline.Presentation;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal static class CharacterPoseNativeRoleDependencyFactory
    {
        internal static CharacterPoseNativeRoleDependencies Create(
            in CharacterPoseNativePreparedBinding preparedBinding,
            CharacterAnimationRigBinding rigBinding,
            CharacterRootHierarchyBinding rootHierarchy,
            CharacterPoseSourceModule source,
            CharacterPoseConstraintRuntime constraints,
            CharacterPoseNativeSourceHandlerComposition sourceHandlers,
            CharacterPoseNativeConstraintHandlerComposition constraintHandlers,
            CharacterPoseNativeManagedHandlerComposition managedHandlers,
            IReadOnlyList<CharacterPresentationAnimationPropertyBinding> animationProperties,
            IReadOnlyList<PoseNodeId> playerNodeIds,
            int contributionCapacity)
        {
            if (!preparedBinding.IsValid)
                throw new ArgumentException(
                    "Pose native role dependency binding is invalid.",
                    nameof(preparedBinding));
            if (!rigBinding || !rootHierarchy)
                throw new ArgumentException(
                    "Pose native role dependency rig binding is incomplete.");
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (constraints == null)
                throw new ArgumentNullException(nameof(constraints));
            if (sourceHandlers == null)
                throw new ArgumentNullException(nameof(sourceHandlers));
            if (constraintHandlers == null)
                throw new ArgumentNullException(nameof(constraintHandlers));
            if (managedHandlers == null)
                throw new ArgumentNullException(nameof(managedHandlers));
            CharacterPoseNativeHandlerFactoryComposition handlerFactory =
                new CharacterPoseNativeHandlerFactoryComposition(
                    preparedBinding.Profile,
                    preparedBinding.InputContract,
                    sourceHandlers,
                    constraintHandlers,
                    managedHandlers);
            CharacterFinalPoseNativePublication publication =
                new CharacterFinalPoseNativePublication(
                    in preparedBinding,
                    rigBinding,
                    rootHierarchy,
                    source,
                    animationProperties ??
                    throw new ArgumentNullException(nameof(animationProperties)),
                    playerNodeIds ??
                    throw new ArgumentNullException(nameof(playerNodeIds)),
                    contributionCapacity);
            return new CharacterPoseNativeRoleDependencies(
                source,
                constraints,
                handlerFactory,
                publication);
        }
    }
}
