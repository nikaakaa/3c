using System;

namespace ThirdPersonAction
{
    [Serializable]
    public readonly struct ActionAnimationCommand
    {
        public ActionAnimationCommand(ActionAnimationKey key, int sourceStep)
        {
            Key = key;
            SourceStep = sourceStep < 0 ? 0 : sourceStep;
        }

        public ActionAnimationKey Key { get; }
        public int SourceStep { get; }
        public bool HasKey => Key.IsValid;
    }
}
