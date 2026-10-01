using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEditor;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public sealed partial class CorinAttackGameplayAbilityAuthoringCode
    {
        static void ApplyAnimationTransitionTiming(BtsmtlAuthoringGenerationContext context)
        {
            const string policyPath = "Assets/Configs/Character/Corin/Pipeline/Presentation/Blend/Action/CorinActionBlendPolicy.asset";
            var policy = context.ResolveExternalAsset<CharacterAnimationBlendPolicy>(policyPath, 11400000L);
            var profile = context.ResolveExternalAsset<CharacterAnimationPresentationProfile>(
                "Assets/Configs/Character/Corin/Pipeline/Presentation/Profiles/CorinAnimationPresentationProfile.asset", 11400000L);
            context.RegisterAssetWrite(policyPath);
            ConfigureTransitionTiming(policy,
                "producer:10f4cb90-8b9a-4944-b77c-14efc9a3124d:0811fba7-c4c7-4cc3-9714-f93b9da4d4ab",
                "producer:21349b9d-8c58-4616-b8f3-6df7d560bb74:5847542b-70e3-49bf-98b9-26bb30d68c01", 0.05f);
            ConfigureTransitionTiming(policy,
                "producer:21349b9d-8c58-4616-b8f3-6df7d560bb74:5847542b-70e3-49bf-98b9-26bb30d68c01",
                "producer:c5761f3c-7517-4803-9e3b-019b66f52d41:c1c596d2-39fd-4c8b-825e-149b852e700c", 0.1f);
            ConfigureTransitionTiming(policy,
                "producer:3a57e427-7c35-4910-99ac-68fca87b055e:eff79947-1ee6-452b-91b0-6582c93e648e",
                "producer:cf8c408a-1d33-4368-ac5f-17c6bf4f1783:dc4fde81-bab5-405d-ad63-6bf864fd536d", 0.100000024f);
            policy.Configure(policy.PolicyId, "corin-attack-transition-timing-20261001",
                policy.StackPolicy, policy.DefaultTransition, policy.Overrides.ToArray(), profile.RigDefinition);
            EditorUtility.SetDirty(policy);
        }

        static void ConfigureTransitionTiming(CharacterAnimationBlendPolicy policy, string source, string target, float seconds)
        {
            CharacterAnimationBlendTransitionRule rule = policy.Overrides.Single(value =>
                value.SourceOwnerIdentity == source && value.TargetOwnerIdentity == target).Rule;
            rule.Configure(seconds, rule.BlendMode, rule.CustomBlendCurve, rule.BlendProfile, rule.BlendLogic);
        }
    }
}
