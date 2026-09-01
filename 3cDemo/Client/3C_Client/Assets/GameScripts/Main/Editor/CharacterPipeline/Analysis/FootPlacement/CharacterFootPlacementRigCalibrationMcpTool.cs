using System;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;
using UnityEditor;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    [McpForUnityTool(
        "character.foot_rig_calibration",
        Description = "Publish one exact Foot Placement Rig Calibration and its Sampling Rig geometry validation through the existing authoring service.",
        StructuredOutput = true,
        AutoRegister = true,
        RequiresPolling = false,
        HasBehaviorAnnotations = true,
        ReadOnlyHint = false,
        DestructiveHint = true,
        IdempotentHint = true,
        OpenWorldHint = false)]
    public static class CharacterFootPlacementRigCalibrationMcpTool
    {
        public sealed class Parameters
        {
            [ToolParameter("Action: publish.", Required = true)]
            public string action { get; set; }

            [ToolParameter(
                "Exact Assets/... path to one CharacterFootPlacementAnalysisSource asset.",
                Required = true)]
            public string analysis_source_asset_path { get; set; }
        }

        public static object HandleCommand(JObject parameters)
        {
            try
            {
                string action = parameters?["action"]?.Value<string>() ?? string.Empty;
                if (!string.Equals(action, "publish", StringComparison.Ordinal))
                    return new ErrorResponse("unsupported_action", new { action });
                string path = parameters?["analysis_source_asset_path"]?.Value<string>() ??
                              string.Empty;
                CharacterFootPlacementAnalysisSource source =
                    AssetDatabase.LoadAssetAtPath<CharacterFootPlacementAnalysisSource>(path);
                if (!source ||
                    !string.Equals(AssetDatabase.GetAssetPath(source), path, StringComparison.Ordinal))
                {
                    return new ErrorResponse(
                        "analysis_source_unavailable",
                        new { analysis_source_asset_path = path });
                }

                CharacterFootPlacementRigCalibration calibration = source.RigCalibration;
                calibration.Configure(
                    calibration.CalibrationId,
                    source.RigDefinition,
                    calibration.CurrentSupportFootprint,
                    calibration.Left,
                    calibration.Right);
                EditorUtility.SetDirty(calibration);
                AssetDatabase.SaveAssetIfDirty(calibration);
                CharacterFootPlacementRigGeometryValidationIdentity identity =
                    CharacterFootPlacementSamplingRigAuthoringService
                        .RebuildGeometryValidation(source);
                return new SuccessResponse(
                    "Foot Placement Rig Calibration published.",
                    new
                    {
                        analysisSource = path,
                        calibrationId = calibration.CalibrationId.Value,
                        calibrationSchemaVersion = calibration.SchemaVersion,
                        calibrationRevision = calibration.ContentRevision,
                        geometryIdentity = identity.IdentityHash,
                        geometryContent = identity.GeometryContentHash
                    });
            }
            catch (Exception exception)
            {
                return new ErrorResponse(
                    "foot_rig_calibration_publish_failed",
                    new { error = exception.Message });
            }
        }
    }
}
