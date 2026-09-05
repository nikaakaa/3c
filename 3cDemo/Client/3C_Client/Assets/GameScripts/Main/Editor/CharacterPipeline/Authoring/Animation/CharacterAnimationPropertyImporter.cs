using System;
using ThirdPersonCharacter.Pipeline.Animation;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public static class CharacterAnimationPropertyImporter
    {
        public static CharacterAnimationPropertyImportPlan Analyze(
            CharacterPipelineDefinition definition,
            string rendererBindingId,
            string animationCurvePath) =>
            CharacterAnimationResourceConfigurationAnalyzer.Analyze(
                definition,
                rendererBindingId,
                animationCurvePath);

        public static CharacterAnimationPropertyImportPlan Apply(
            CharacterPipelineDefinition definition,
            string rendererBindingId,
            string animationCurvePath,
            string expectedPlanHash)
        {
            CharacterAnimationPropertyImportPlan plan = Analyze(
                definition,
                rendererBindingId,
                animationCurvePath);
            plan.RequirePlanHash(expectedPlanHash);
            new CharacterAnimationResourceConfigurationTransaction().Apply(plan);
            return plan;
        }
    }
}
