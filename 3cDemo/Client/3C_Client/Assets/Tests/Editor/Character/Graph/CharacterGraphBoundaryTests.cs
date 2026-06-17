using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Tests.Editor.Character.Graph
{
    public sealed class CharacterGraphBoundaryTests
    {
        static readonly string[] RuntimeRoots =
        {
            "Scripts/Character/Graph",
            "Scripts/Character/Action/Branch",
            "Scripts/Character/Action/Timeline"
        };

        static readonly string[] BannedTokens =
        {
            "TreeRunner",
            "TimelinePlayer",
            "PlayableGraph",
            "MonoBehaviour",
            "Transform",
            "CharacterController",
            "Animator",
            "InputAction",
            "GameObject",
            "Instantiate(",
            "Destroy(",
            "Resources.",
            "Taco.",
            "TreeDesigner",
            "CharacterRuntimeBlackboard ",
            "DodgeAction",
            "WriteLocomotionFacts",
            "WriteActionFacts",
            "WriteAnimationFacts",
            "WriteLocomotionPreemptionFact",
            "OutputApplier",
            "AnimationPresenter",
            "MotionExecutor"
        };

        [Test]
        public void CharacterGraphRuntimeContractsDoNotReferenceBannedRunnersOrUnitySideEffects()
        {
            List<string> violations = new List<string>();
            foreach (string root in RuntimeRoots)
            {
                string absoluteRoot = Path.Combine(Application.dataPath, root);
                if (!Directory.Exists(absoluteRoot))
                    continue;

                foreach (string file in Directory.GetFiles(absoluteRoot, "*.cs", SearchOption.AllDirectories))
                {
                    string text = File.ReadAllText(file);
                    for (int i = 0; i < BannedTokens.Length; i++)
                    {
                        if (text.Contains(BannedTokens[i]))
                            violations.Add($"{file}:{BannedTokens[i]}");
                    }
                }
            }

            Assert.IsEmpty(violations);
        }
    }
}
