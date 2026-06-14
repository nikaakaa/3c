using System;
using System.Collections.Generic;
using System.Linq;
using KINEMATION.RetargetPro.Runtime;
using KINEMATION.RetargetPro.Runtime.Features;
using KINEMATION.RetargetPro.Runtime.Features.BasicRetargeting;
using KINEMATION.RetargetPro.Runtime.Features.IKRetargeting;
using KINEMATION.Shared.KAnimationCore.Runtime.Rig;
using UnityEditor;
using UnityEngine;
using ThirdPersonDiagnostics;

public static class HasteCourierRuskRetargetProfileConfigurator
{
    public const string MenuPath = "Tools/Haste/Configure Courier To Rusk Retarget Profile";

    static readonly ChainMapping[] ActiveMappings =
    {
        new("Hip", new[] { "Hip" }, "rusk_Hips", new[] { "rusk_Hips" }, 1f, 0f),
        new("Hip_L", new[] { "Hip_L", "Leg_L", "Knee_L", "Foot_L" }, "rusk_LeftUpperLeg", new[] { "rusk_LeftUpperLeg", "rusk_LeftLowerLeg", "rusk_LeftFoot" }, 0f, 1f),
        new("Hip_R", new[] { "Hip_R", "Leg_R", "Knee_R", "Foot_R" }, "rusk_RightUpperLeg", new[] { "rusk_RightUpperLeg", "rusk_RightLowerLeg", "rusk_RightFoot" }, 0f, 1f),
        new("Spine_2", new[] { "Spine_2", "Spine_3" }, "rusk_Spine", new[] { "rusk_Spine", "rusk_Chest" }, 0f, 0f),
        new("Neck", new[] { "Neck", "Head" }, "rusk_Neck", new[] { "rusk_Neck", "rusk_Head" }, 0f, 0f),
        new("Shoulder_L", new[] { "Shoulder_L", "Arm_L", "Elbow_L", "Hand_L" }, "rusk_LeftShoulder", new[] { "rusk_LeftShoulder", "rusk_LeftUpperArm", "rusk_LeftLowerArm", "rusk_LeftHand" }, 0f, 1f),
        new("Shoulder_R", new[] { "Shoulder_R", "Arm_R", "Elbow_R", "Hand_R" }, "rusk_RightShoulder", new[] { "rusk_RightShoulder", "rusk_RightUpperArm", "rusk_RightLowerArm", "rusk_RightHand" }, 0f, 1f),
        new("Toe_L1", new[] { "Toe_L1" }, "rusk_LeftToeBase", new[] { "rusk_LeftToeBase" }, 0f, 0f),
        new("Toe_R1", new[] { "Toe_R1" }, "rusk_RightToeBase", new[] { "rusk_RightToeBase" }, 0f, 0f),
    };

    [MenuItem(MenuPath, true)]
    static bool ValidateConfigureSelectedProfile()
    {
        return Selection.activeObject is RetargetProfile;
    }

    [MenuItem(MenuPath)]
    static void ConfigureSelectedProfile()
    {
        RetargetProfile profile = Selection.activeObject as RetargetProfile;
        if (profile == null)
            return;

        Undo.RecordObject(profile, "Configure Courier To Rusk Retarget Profile");
        Configure(profile);
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
         RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(RuntimeDiagnosticLogCategory.Editor, RuntimeDiagnosticLogLevel.Info, "retarget-profile-configured", "", "", 0, Time.frameCount, $"[HasteCourierRuskRetargetProfileConfigurator] Configured {profile.name}."));
    }

    public static ConfigureResult Configure(RetargetProfile profile)
    {
        if (profile == null)
            throw new ArgumentNullException(nameof(profile));
        if (profile.sourceRig == null)
            throw new InvalidOperationException("Retarget profile is missing Source Rig.");
        if (profile.targetRig == null)
            throw new InvalidOperationException("Retarget profile is missing Target Rig.");

        Undo.RecordObject(profile, "Configure Courier To Rusk Retarget Profile");

        foreach (RetargetFeature feature in profile.retargetFeatures)
        {
            if (feature == null)
                continue;

            Undo.RecordObject(feature, "Configure Courier To Rusk Retarget Feature");
            feature.featureWeight = 0f;
            EditorUtility.SetDirty(feature);
        }

        int configured = 0;
        foreach (ChainMapping mapping in ActiveMappings)
        {
            IKRetargetFeature feature = FindOrCreateFeature(profile, mapping.TargetChainName);
            Undo.RecordObject(feature, "Configure Courier To Rusk Retarget Feature");
            ApplyMapping(profile, feature, mapping);
            configured++;
        }

        EditorUtility.SetDirty(profile);
        return new ConfigureResult(configured, profile.retargetFeatures.Count - configured);
    }

    public static IReadOnlyList<string> GetConfiguredTargetChains()
    {
        return ActiveMappings.Select(mapping => mapping.TargetChainName).ToArray();
    }

    static IKRetargetFeature FindOrCreateFeature(RetargetProfile profile, string targetChainName)
    {
        foreach (RetargetFeature feature in profile.retargetFeatures)
        {
            if (feature is not BasicRetargetFeature basicFeature)
                continue;
            if (basicFeature.targetChain != null && basicFeature.targetChain.chainName == targetChainName)
                return EnsureIkFeature(profile, basicFeature);
        }

        IKRetargetFeature newFeature = ScriptableObject.CreateInstance<IKRetargetFeature>();
        newFeature.name = typeof(IKRetargetFeature).Name;
        newFeature.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
        profile.retargetFeatures.Add(newFeature);

        if (EditorUtility.IsPersistent(profile))
            AssetDatabase.AddObjectToAsset(newFeature, profile);

        Undo.RegisterCreatedObjectUndo(newFeature, "Create Courier To Rusk Retarget Feature");
        return newFeature;
    }

    static IKRetargetFeature EnsureIkFeature(RetargetProfile profile, BasicRetargetFeature basicFeature)
    {
        if (basicFeature is IKRetargetFeature ikFeature)
            return ikFeature;

        IKRetargetFeature replacement = ScriptableObject.CreateInstance<IKRetargetFeature>();
        replacement.name = typeof(IKRetargetFeature).Name;
        replacement.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;

        int index = profile.retargetFeatures.IndexOf(basicFeature);
        profile.retargetFeatures[index] = replacement;

        if (EditorUtility.IsPersistent(profile))
            AssetDatabase.AddObjectToAsset(replacement, profile);

        Undo.RegisterCreatedObjectUndo(replacement, "Create Courier To Rusk Retarget Feature");
        Undo.DestroyObjectImmediate(basicFeature);
        return replacement;
    }

    static void ApplyMapping(RetargetProfile profile, IKRetargetFeature feature, ChainMapping mapping)
    {
        feature.sourceRig = profile.sourceRig;
        feature.targetRig = profile.targetRig;
        feature.sourceChain = BuildChain(profile.sourceRig, mapping.SourceChainName, mapping.SourceElements);
        feature.targetChain = BuildChain(profile.targetRig, mapping.TargetChainName, mapping.TargetElements);
        feature.featureWeight = 1f;
        feature.scaleWeight = 1f;
        feature.translationWeight = mapping.TranslationWeight;
        feature.offset = Vector3.zero;
        feature.ikWeight = mapping.IkWeight;
        feature.effectorOffset = Vector3.zero;
        feature.poleOffset = Vector3.zero;
        EditorUtility.SetDirty(feature);
    }

    static KRigElementChain BuildChain(KRig rig, string chainName, IReadOnlyList<string> elementNames)
    {
        KRigElementChain chain = new KRigElementChain { chainName = chainName };
        foreach (string elementName in elementNames)
        {
            KRigElement element = FindRequiredElement(rig, elementName);
            chain.elementChain.Add(element);
        }

        return chain;
    }

    static KRigElement FindRequiredElement(KRig rig, string elementName)
    {
        foreach (KRigElement element in rig.rigHierarchy)
        {
            if (element.name == elementName)
                return element;
        }

        throw new InvalidOperationException($"Rig `{rig.name}` is missing `{elementName}`.");
    }

    readonly struct ChainMapping
    {
        public readonly string SourceChainName;
        public readonly string[] SourceElements;
        public readonly string TargetChainName;
        public readonly string[] TargetElements;
        public readonly float TranslationWeight;
        public readonly float IkWeight;

        public ChainMapping(
            string sourceChainName,
            string[] sourceElements,
            string targetChainName,
            string[] targetElements,
            float translationWeight,
            float ikWeight)
        {
            SourceChainName = sourceChainName;
            SourceElements = sourceElements;
            TargetChainName = targetChainName;
            TargetElements = targetElements;
            TranslationWeight = translationWeight;
            IkWeight = ikWeight;
        }
    }

    public readonly struct ConfigureResult
    {
        public readonly int ConfiguredFeatures;
        public readonly int DisabledFeatures;

        public ConfigureResult(int configuredFeatures, int disabledFeatures)
        {
            ConfiguredFeatures = configuredFeatures;
            DisabledFeatures = disabledFeatures;
        }
    }
}
