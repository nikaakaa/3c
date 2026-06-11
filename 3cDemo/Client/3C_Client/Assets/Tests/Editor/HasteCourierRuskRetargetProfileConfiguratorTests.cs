using System.Collections.Generic;
using System.Linq;
using KINEMATION.RetargetPro.Runtime;
using KINEMATION.RetargetPro.Runtime.Features.BasicRetargeting;
using KINEMATION.RetargetPro.Runtime.Features.IKRetargeting;
using KINEMATION.Shared.KAnimationCore.Runtime.Rig;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HasteAnimation.Tests
{
    public sealed class HasteCourierRuskRetargetProfileConfiguratorTests
    {
        const string SourceRigPath = "Assets/Rig_Courier_Retake.asset";
        const string TargetRigPath = "Assets/Rig_Rusk_ver1.1.asset";

        readonly List<Object> createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (RetargetProfile profile in createdObjects.OfType<RetargetProfile>())
            {
                foreach (Object feature in profile.retargetFeatures)
                {
                    if (feature != null && !createdObjects.Contains(feature))
                        Object.DestroyImmediate(feature);
                }
            }

            foreach (Object createdObject in createdObjects)
            {
                if (createdObject != null)
                    Object.DestroyImmediate(createdObject);
            }

            createdObjects.Clear();
        }

        [Test]
        public void ConfigureFixesLegChainsAndDisablesUnmappedChains()
        {
            RetargetProfile profile = CreateProfile();
            IKRetargetFeature wrongLeftLegFeature = CreateFeature(
                "UpperHand_L",
                new[] { "UpperHand_L" },
                "rusk_LeftUpperLeg",
                new[] { "rusk_LeftUpperLeg" });
            IKRetargetFeature fingerFeature = CreateFeature(
                "None",
                new string[0],
                "rusk_RightRingProximal",
                new[] { "rusk_RightRingProximal" });
            profile.retargetFeatures.Add(wrongLeftLegFeature);
            profile.retargetFeatures.Add(fingerFeature);

            HasteCourierRuskRetargetProfileConfigurator.ConfigureResult result =
                HasteCourierRuskRetargetProfileConfigurator.Configure(profile);

            Assert.That(result.ConfiguredFeatures, Is.EqualTo(9));
            Assert.That(fingerFeature.featureWeight, Is.EqualTo(0f));
            AssertIkChain(profile, "rusk_LeftUpperLeg",
                new[] { "Hip_L", "Leg_L", "Knee_L", "Foot_L" },
                new[] { "rusk_LeftUpperLeg", "rusk_LeftLowerLeg", "rusk_LeftFoot" });
            AssertIkChain(profile, "rusk_RightUpperLeg",
                new[] { "Hip_R", "Leg_R", "Knee_R", "Foot_R" },
                new[] { "rusk_RightUpperLeg", "rusk_RightLowerLeg", "rusk_RightFoot" });
            AssertIkChain(profile, "rusk_LeftShoulder",
                new[] { "Shoulder_L", "Arm_L", "Elbow_L", "Hand_L" },
                new[] { "rusk_LeftShoulder", "rusk_LeftUpperArm", "rusk_LeftLowerArm", "rusk_LeftHand" });
            AssertIkChain(profile, "rusk_RightShoulder",
                new[] { "Shoulder_R", "Arm_R", "Elbow_R", "Hand_R" },
                new[] { "rusk_RightShoulder", "rusk_RightUpperArm", "rusk_RightLowerArm", "rusk_RightHand" });
            AssertRotationChain(profile, "rusk_Hips",
                new[] { "Hip" },
                new[] { "rusk_Hips" },
                1f);
            AssertRotationChain(profile, "rusk_Spine",
                new[] { "Spine_2", "Spine_3" },
                new[] { "rusk_Spine", "rusk_Chest" },
                0f);
            AssertRotationChain(profile, "rusk_LeftToeBase",
                new[] { "Toe_L1" },
                new[] { "rusk_LeftToeBase" },
                0f);
            AssertRotationChain(profile, "rusk_RightToeBase",
                new[] { "Toe_R1" },
                new[] { "rusk_RightToeBase" },
                0f);
        }

        [Test]
        public void ConfigureCreatesMissingMainFeatures()
        {
            RetargetProfile profile = CreateProfile();

            HasteCourierRuskRetargetProfileConfigurator.Configure(profile);

            foreach (string targetChain in HasteCourierRuskRetargetProfileConfigurator.GetConfiguredTargetChains())
            {
                BasicRetargetFeature feature = FindFeature(profile, targetChain);
                Assert.NotNull(feature, targetChain);
                Assert.That(feature.featureWeight, Is.EqualTo(1f), targetChain);
            }

            Assert.That(FindFeature(profile, "rusk_Hips").translationWeight, Is.EqualTo(1f));
            Assert.That(((IKRetargetFeature)FindFeature(profile, "rusk_LeftUpperLeg")).ikWeight, Is.EqualTo(1f));
            Assert.That(((IKRetargetFeature)FindFeature(profile, "rusk_RightUpperLeg")).ikWeight, Is.EqualTo(1f));
            Assert.That(((IKRetargetFeature)FindFeature(profile, "rusk_LeftShoulder")).ikWeight, Is.EqualTo(1f));
            Assert.That(((IKRetargetFeature)FindFeature(profile, "rusk_RightShoulder")).ikWeight, Is.EqualTo(1f));
            Assert.That(((IKRetargetFeature)FindFeature(profile, "rusk_Spine")).ikWeight, Is.EqualTo(0f));
            Assert.That(((IKRetargetFeature)FindFeature(profile, "rusk_LeftToeBase")).ikWeight, Is.EqualTo(0f));
        }

        RetargetProfile CreateProfile()
        {
            RetargetProfile profile = ScriptableObject.CreateInstance<RetargetProfile>();
            profile.sourceRig = LoadRequired<KRig>(SourceRigPath);
            profile.targetRig = LoadRequired<KRig>(TargetRigPath);
            createdObjects.Add(profile);
            return profile;
        }

        IKRetargetFeature CreateFeature(string sourceChainName, string[] sourceElements, string targetChainName, string[] targetElements)
        {
            IKRetargetFeature feature = ScriptableObject.CreateInstance<IKRetargetFeature>();
            RetargetProfile profile = createdObjects.OfType<RetargetProfile>().Last();
            feature.sourceRig = profile.sourceRig;
            feature.targetRig = profile.targetRig;
            feature.sourceChain = BuildChain(profile.sourceRig, sourceChainName, sourceElements);
            feature.targetChain = BuildChain(profile.targetRig, targetChainName, targetElements);
            feature.featureWeight = 1f;
            createdObjects.Add(feature);
            return feature;
        }

        static void AssertIkChain(RetargetProfile profile, string targetChainName, string[] sourceElements, string[] targetElements)
        {
            AssertConfiguredChain(profile, targetChainName, sourceElements, targetElements, 0f, 1f);
        }

        static void AssertRotationChain(
            RetargetProfile profile,
            string targetChainName,
            string[] sourceElements,
            string[] targetElements,
            float translationWeight)
        {
            AssertConfiguredChain(profile, targetChainName, sourceElements, targetElements, translationWeight, 0f);
        }

        static void AssertConfiguredChain(
            RetargetProfile profile,
            string targetChainName,
            string[] sourceElements,
            string[] targetElements,
            float translationWeight,
            float ikWeight)
        {
            IKRetargetFeature feature = FindFeature(profile, targetChainName) as IKRetargetFeature;
            Assert.NotNull(feature, targetChainName);
            Assert.That(feature.featureWeight, Is.EqualTo(1f), targetChainName);
            Assert.That(feature.scaleWeight, Is.EqualTo(1f), targetChainName);
            Assert.That(feature.translationWeight, Is.EqualTo(translationWeight), targetChainName);
            Assert.That(feature.ikWeight, Is.EqualTo(ikWeight), targetChainName);
            CollectionAssert.AreEqual(sourceElements, feature.sourceChain.elementChain.Select(element => element.name).ToArray(), targetChainName);
            CollectionAssert.AreEqual(targetElements, feature.targetChain.elementChain.Select(element => element.name).ToArray(), targetChainName);
        }

        static BasicRetargetFeature FindFeature(RetargetProfile profile, string targetChainName)
        {
            return profile.retargetFeatures
                .OfType<BasicRetargetFeature>()
                .FirstOrDefault(feature => feature.targetChain != null && feature.targetChain.chainName == targetChainName);
        }

        static KRigElementChain BuildChain(KRig rig, string chainName, IEnumerable<string> elementNames)
        {
            KRigElementChain chain = new KRigElementChain { chainName = chainName };
            foreach (string elementName in elementNames)
                chain.elementChain.Add(rig.GetElementByName(elementName));
            return chain;
        }

        static T LoadRequired<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.NotNull(asset, path);
            return asset;
        }
    }
}
