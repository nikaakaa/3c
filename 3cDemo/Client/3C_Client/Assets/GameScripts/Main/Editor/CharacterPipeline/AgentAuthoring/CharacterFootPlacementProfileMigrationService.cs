using System;
using ThirdPersonCharacter.Pipeline.Presentation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    static class CharacterFootPlacementProfileMigrationService
    {
        public static bool MigrateIfRequired(CharacterFootPlacementProfile profile)
        {
            if (!profile)
                return false;
            SerializedObject serialized = new SerializedObject(profile);
            SerializedProperty mask = serialized.FindProperty(
                "m_LyraCurrentGrounding.m_GroundLayerMask");
            if (mask == null || mask.intValue != 0)
                return false;
            int groundLayer = LayerMask.NameToLayer("Ground");
            int footPlacementLayer = LayerMask.NameToLayer("FootPlacementSurface");
            if (groundLayer < 0 || footPlacementLayer < 0)
                throw new InvalidOperationException(
                    "Formal Foot Grounding layers are not configured.");
            SetInt(serialized, "m_LyraCurrentGrounding.m_GroundLayerMask", (1 << groundLayer) | (1 << footPlacementLayer));
            SetInt(serialized, "m_LyraCurrentGrounding.m_HitCapacity", 16);
            SetFloat(serialized, "m_LyraCurrentGrounding.m_TraceAbove", 0.5f);
            SetFloat(serialized, "m_LyraCurrentGrounding.m_TraceBelow", 0.5f);
            SetFloat(serialized, "m_LyraCurrentGrounding.m_TraceRadius", 0.05f);
            SetFloat(serialized, "m_LyraCurrentGrounding.m_HitNormalSpringStrength", 8f);
            SetFloat(serialized, "m_LyraCurrentGrounding.m_HitNormalCriticalDamping", 1f);
            SetFloat(serialized, "m_LyraCurrentGrounding.m_FootOffsetSpringStrength", 2.5f);
            SetFloat(serialized, "m_LyraCurrentGrounding.m_FootOffsetCriticalDamping", 1f);
            SetFloat(serialized, "m_LyraCurrentGrounding.m_FootOffsetTargetVelocityAmount", 0.2f);
            SetFloat(serialized, "m_LyraCurrentGrounding.m_PelvisOffsetSpringStrength", 2.5f);
            SetFloat(serialized, "m_LyraCurrentGrounding.m_PelvisOffsetCriticalDamping", 1f);
            SetFloat(serialized, "m_StanceStabilization.m_MaximumSurfaceSlopeDegrees", 55f);
            SetFloat(serialized, "m_StanceStabilization.m_MaximumContactSurfaceDistance", 0.12f);
            SetFloat(serialized, "m_StanceStabilization.m_PlantSpeedThreshold", 0.6f);
            SetFloat(serialized, "m_StanceStabilization.m_UnalignmentSpeedThreshold", 2f);
            SetFloat(serialized, "m_StanceStabilization.m_PlantConfidenceEnter", 0.65f);
            SetFloat(serialized, "m_StanceStabilization.m_PlantConfidenceExit", 0.35f);
            SetFloat(serialized, "m_StanceStabilization.m_AnchorBlendSpeed", 8f);
            SetFloat(serialized, "m_StanceStabilization.m_MaximumAnchorDistance", 0.14f);
            SetFloat(serialized, "m_StanceStabilization.m_MaximumPelvisLowering", 0.32f);
            SetFloat(serialized, "m_StanceStabilization.m_MaximumPelvisRaising", 0.18f);
            SetFloat(serialized, "m_PredictiveExtension.m_MinimumLandingConfidence", 0.25f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            serialized.UpdateIfRequiredOrScript();
            SerializedProperty revision = serialized.FindProperty("m_Revision");
            if (revision == null)
                throw new InvalidOperationException("Foot Placement Profile revision field is missing.");
            revision.stringValue = profile.ComputeRevision();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            serialized.UpdateIfRequiredOrScript();
            return true;
        }

        static void SetInt(SerializedObject serialized, string path, int value)
        {
            SerializedProperty property = Require(serialized, path);
            property.intValue = value;
        }

        static void SetFloat(SerializedObject serialized, string path, float value)
        {
            SerializedProperty property = Require(serialized, path);
            property.floatValue = value;
        }

        static SerializedProperty Require(SerializedObject serialized, string path) =>
            serialized.FindProperty(path) ?? throw new InvalidOperationException(
                $"Foot Placement Profile property '{path}' is missing.");
    }
}
