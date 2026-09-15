using System;
using System.Collections.Generic;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterPoseNativeHandlerFactoryComposition :
        ICharacterPoseNativeNodeHandlerFactory
    {
        readonly CharacterAnimationPresentationProfile m_Profile;
        readonly CharacterAnimationInputContract m_InputContract;
        readonly CharacterPoseNativeSourceHandlerComposition m_Source;
        readonly CharacterPoseNativeConstraintHandlerComposition m_Constraint;
        readonly CharacterPoseNativeManagedHandlerComposition m_Managed;
        readonly CharacterPoseNativeNodeHandlerRegistry m_Registry;

        internal CharacterPoseNativeHandlerFactoryComposition(
            CharacterAnimationPresentationProfile profile,
            CharacterAnimationInputContract inputContract,
            CharacterPoseNativeSourceHandlerComposition source,
            CharacterPoseNativeConstraintHandlerComposition constraint,
            CharacterPoseNativeManagedHandlerComposition managed)
        {
            m_Profile = profile ? profile : throw new ArgumentNullException(nameof(profile));
            m_InputContract = inputContract ?? throw new ArgumentNullException(nameof(inputContract));
            m_Source = source ?? throw new ArgumentNullException(nameof(source));
            m_Constraint = constraint ?? throw new ArgumentNullException(nameof(constraint));
            m_Managed = managed ?? throw new ArgumentNullException(nameof(managed));
            m_Registry = new CharacterPoseNativeNodeHandlerRegistry();
            m_Registry.Register(m_Profile, m_InputContract);
            m_Source.Register(m_Registry);
            m_Constraint.Register(m_Registry);
            m_Managed.Register(m_Registry);
        }

        public IReadOnlyList<ICharacterPoseNativeNodeHandler> Create(
            in CharacterPoseNativePreparedBinding preparedBinding,
            in CharacterPoseNativeInstanceContext context) =>
            m_Registry.Create(in preparedBinding, in context);
    }
}
