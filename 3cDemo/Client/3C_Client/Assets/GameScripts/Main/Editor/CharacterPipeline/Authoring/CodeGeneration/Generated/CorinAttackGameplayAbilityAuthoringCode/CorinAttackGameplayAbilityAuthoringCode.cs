using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinAttackGameplayAbilityAuthoringCode : IBtsmtlAuthoringGenerationEntry
    {
        public BtsmtlAuthoringGenerationResult Execute(BtsmtlAuthoringGenerationContext context)
        {
            var rootParts = BuildRoot(context);
            var attack = BuildAttack(rootParts, context);
            var attack1 = BuildAttack1(attack, rootParts, context);
            var attack4 = BuildAttack4(attack, rootParts, context);
            var attack2 = BuildAttack2(attack, rootParts, context);
            var attack3 = BuildAttack3(attack, rootParts, context);
            var attack5 = BuildAttack5(attack, rootParts, context);
            var attack5_End = BuildAttack5_End(attack, rootParts, context);
            var attack5_End2 = BuildAttack5_End2(attack, rootParts, context);
            CorinActionSteeringAuthoring.Apply(attack.timelineData, "142218e6-6644-479b-b721-19d91be03a15", "eb07c8dd-3527-4470-8bc4-c44f6665d937", new AnimationCurve(new Keyframe(0f, 60f, 0f, float.PositiveInfinity), new Keyframe(10f, 0f, 0f, 0f)), new AnimationCurve(new Keyframe(0f, 999f, 0f, 0f, 0.333333343f, 0.333333343f) { weightedMode = (WeightedMode)0 }, new Keyframe(1f, 999f, 0f, -99900.0991f, 0.333333343f, 0.333333343f) { weightedMode = (WeightedMode)0 }, new Keyframe(1.00999999f, 0f, -99900.0991f, 0f, 0.333333343f, 0.333333343f) { weightedMode = (WeightedMode)0 }, new Keyframe(100.000008f, 0f, 0f, 0f, 0.333333343f, 0.333333343f) { weightedMode = (WeightedMode)0 }) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever });
            CorinActionSteeringAuthoring.Apply(attack.timelineData1, "a56930a0-7209-427e-977e-81bb4488c2ad", "28fc08a4-055f-4a38-9371-77efd7c6914a", new AnimationCurve(new Keyframe(0f, 60f, 0f, float.PositiveInfinity), new Keyframe(9f, 0f, 0f, 0f)), new AnimationCurve(new Keyframe(0f, 999f, 0f, 0f, 0.333333343f, 0.333333343f) { weightedMode = (WeightedMode)0 }, new Keyframe(1f, 999f, 0f, -99900.0991f, 0.333333343f, 0.333333343f) { weightedMode = (WeightedMode)0 }, new Keyframe(1.00999999f, 0f, -99900.0991f, 0f, 0.333333343f, 0.333333343f) { weightedMode = (WeightedMode)0 }, new Keyframe(100.000008f, 0f, 0f, 0f, 0.333333343f, 0.333333343f) { weightedMode = (WeightedMode)0 }) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever });
            CorinActionSteeringAuthoring.Apply(attack.timelineData2, "373e6fc6-7fc7-490d-88ef-b4a48b87fcfd", "b7cc37e0-f20d-40a9-afd5-c6d2e659d99d", new AnimationCurve(new Keyframe(0f, 60f, 0f, float.PositiveInfinity), new Keyframe(32f, 0f, 0f, 0f)), new AnimationCurve(new Keyframe(0f, 6f, 0f, 0f, 0.333333343f, 0.333333343f) { weightedMode = (WeightedMode)0 }, new Keyframe(8f, 6f, 0f, 0f, 0.333333343f, 0.333333343f) { weightedMode = (WeightedMode)0 }, new Keyframe(16f, 0f, 0f, 0f, 0.333333343f, 0.333333343f) { weightedMode = (WeightedMode)0 }, new Keyframe(80f, 0f, 0f, 0f, 0.333333343f, 0.333333343f) { weightedMode = (WeightedMode)0 }) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever });
            CorinActionSteeringAuthoring.Apply(attack.timelineData3, "bd920a87-f138-4df5-aad9-c3763973bedb", "e28c94f9-544b-4d3c-8280-ac6cfb9f0a42", new AnimationCurve(new Keyframe(0f, 60f, 0f, float.PositiveInfinity), new Keyframe(18f, 0f, 0f, 0f)), new AnimationCurve(new Keyframe(0f, 6f, 0f, 0f, 0f, 0f) { weightedMode = (WeightedMode)0 }, new Keyframe(9f, 6f, 0f, 0f, 0f, 0f) { weightedMode = (WeightedMode)0 }, new Keyframe(18f, 0f, 0f, 0f, 0f, 0f) { weightedMode = (WeightedMode)0 }, new Keyframe(88f, 0f, 0f, 0f, 0f, 0f) { weightedMode = (WeightedMode)0 }) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever });
            CorinActionSteeringAuthoring.Apply(attack.timelineData4, "765bebe5-8d5d-4ce3-92a5-dc8a51dc1664", "86240efe-6475-4499-bc61-9a80a91c84b8", new AnimationCurve(new Keyframe(0f, 60f, 0f, float.PositiveInfinity), new Keyframe(25f, 0f, 0f, 0f)), new AnimationCurve(new Keyframe(0f, 6f, 0f, 0f, 0f, 0f) { weightedMode = (WeightedMode)0 }, new Keyframe(12f, 0f, 0f, 0f, 0f, 0f) { weightedMode = (WeightedMode)0 }, new Keyframe(124.000008f, 0f, 0f, 0f, 0f, 0f) { weightedMode = (WeightedMode)0 }) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever });
            FinalizeAuthoring(rootParts, attack, attack1, attack4, attack2, attack3, attack5, attack5_End, attack5_End2, context);
            return context.Complete(rootParts.graph);
        }
    }
}
