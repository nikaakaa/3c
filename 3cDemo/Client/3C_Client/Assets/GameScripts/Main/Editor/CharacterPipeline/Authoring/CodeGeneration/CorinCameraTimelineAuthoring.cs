using System;
using BTSMTL.Timeline;
using ThirdPersonCamera;
using ThirdPersonCharacter.Control.Authoring;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    internal static class CorinCameraTimelineAuthoring
    {
        const string Folder = "Assets/Configs/Character/Corin/Pipeline/Presentation/Camera/";

        internal static void Apply(TimelineData data, string pattern, decimal start, decimal end)
        {
            foreach (var cue in Entries(pattern))
            {
                decimal time = start + cue.Frame / 60m;
                if (time >= end) continue;
                string seed = data.AuthoringId + ":camera:" + cue.Id;
                var catalog = TimelineTreeContractComposition.Create();
                if (cue.Kind == CameraEffectKind.Shake)
                {
                    var asset = AssetDatabase.LoadAssetAtPath<CameraShakeAsset>(Folder + cue.Resource + ".asset");
                    if (!asset) throw new InvalidOperationException("Missing Camera Shake: " + cue.Resource);
                    var graph = data.SerializedOwner switch
                    {
                        TimelineAsset owner => BtsmtlSkillAuthoringCode.EnsureTimelineGraph(owner, Id(seed + ":graph"), cue.Resource),
                        FlowCanvas.FlowGraph owner => BtsmtlSkillAuthoringGraphCreationContract.EnsureOwnedGraph<BtsmtlSkillFlowGraph>(owner, Id(seed + ":graph"), typeof(BtsmtlSkillFlowGraph), BtsmtlSkillFlowGraphRole.TimelineBody, cue.Resource),
                        _ => throw new InvalidOperationException("Camera TreeClip has no formal graph owner.")
                    };
                    BtsmtlSkillAuthoringCode.EnsureFlowNode(graph, typeof(BtsmtlSkillRootFlowNode), Id(seed + ":root"), "持续执行", Vector2.zero);
                    var enable = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph, typeof(BtsmtlSkillTimelineEnableFlowNode), Id(seed + ":enable"), "片段启用", new Vector2(0f, 100f));
                    var request = BtsmtlSkillAuthoringCode.EnsureFlowNode(graph, typeof(RequestCameraEffectNode), Id(seed + ":request"), cue.Resource, new Vector2(220f, 0f));
                    BtsmtlSkillAuthoringContract.Apply(request, new[] { new BtsmtlSkillAuthoringFieldValue("requestId", cue.Id), new BtsmtlSkillAuthoringFieldValue("effectKind", CameraEffectKind.Shake), new BtsmtlSkillAuthoringFieldValue("resourceId", cue.Resource) });
                    BtsmtlSkillAuthoringCode.EnsureFlowConnection(graph, enable, "Output", request, "Input", Id(seed + ":connection"));
                    var track = BtsmtlSkillAuthoringCode.EnsureTrack(data, catalog, typeof(TreeTrack), Id(data.AuthoringId + ":camera.nodes"), "Camera Triggers", TimelineExecutionDomain.Presentation);
                    BtsmtlSkillAuthoringCode.EnsureClip(data, catalog, track, Id(seed + ":clip"), time, graph, Math.Min(end, time + 1m / 60m), 0m, 0m, 0m);
                }
                else
                {
                    var asset = AssetDatabase.LoadAssetAtPath<CameraEffectAsset>(Folder + cue.Resource + ".asset");
                    if (!asset) throw new InvalidOperationException("Missing Camera Effect: " + cue.Resource);
                    var track = BtsmtlSkillAuthoringCode.EnsureTrack(data, catalog, typeof(CameraEffectTrack), Id(data.AuthoringId + ":camera.effects"), "Camera Effects", TimelineExecutionDomain.Presentation);
                    BtsmtlSkillAuthoringCode.EnsureClip(data, catalog, track, Id(seed + ":clip"), time, asset, end, 0m, 0m, 0m);
                }
            }
        }

        static string Id(string value) => new Guid(BtsmtlSkillGraphAssetFactory.StableIdentity(value)).ToString("D");

        readonly struct Cue
        {
            public readonly string Id;
            public readonly int Frame;
            public readonly CameraEffectKind Kind;
            public readonly string Resource;
            public Cue(string id, int frame, CameraEffectKind kind, string resource)
            { Id = id; Frame = frame; Kind = kind; Resource = resource; }
        }

        static Cue[] Entries(string pattern) => pattern switch
        {
            "Corin_Attack_Branch_02" => new[] { new Cue("0402de0f1aab47a5a388bc1f809d66bc", 10, CameraEffectKind.Shake, "Corin_Attack_Branch_02_CamShake_E_01"), new Cue("c41001047cb24379a4d2c065dfeaba1c", 0, CameraEffectKind.Stretch, "Corin_Attack_Branch_02_CamStretch_01"), new Cue("570790dca1bf4425aa5624cf87228548", 0, CameraEffectKind.Zoom, "Corin_Attack_Branch_02_CamZoom_01") },
            "Corin_Attack_Branch_02_Explode" => new[] { new Cue("4ba9ccdb0a8e450c8f7d29718db00688", 7, CameraEffectKind.Shake, "Corin_Attack_Branch_02_CamShake_E_02"), new Cue("054e9e7550fc459c9397abc4a23c2b39", 18, CameraEffectKind.Shake, "Corin_Attack_Branch_02_CamShake_E_03"), new Cue("9839d7d186a346718afc818ac508be09", 0, CameraEffectKind.Stretch, "Corin_Attack_Branch_02_CamStretch_02"), new Cue("bef5f6d8398d4eb3b6071397cd1f1675", 0, CameraEffectKind.Zoom, "Corin_Attack_Branch_02_CamZoom_02") },
            "Corin_Attack_Normal_02" => new[] { new Cue("78774fce67cc40bcbae9c6170598515b", 17, CameraEffectKind.Shake, "Corin_Attack_Normal_02_CamShake_E_01") },
            "Corin_Attack_Normal_03" => new[] { new Cue("01baa150980f49b8a1bbc6ad9618d8b5", 0, CameraEffectKind.Stretch, "Corin_Attack_Normal_03_CamStretch_01"), new Cue("f2922310ada24890aacdaedf0d2cee3a", 0, CameraEffectKind.Zoom, "Corin_Attack_Normal_03_CamZoom_01") },
            "Corin_Attack_Normal_04" => new[] { new Cue("6044070d385347ab814df65bcafb26df", 37, CameraEffectKind.Shake, "Corin_Attack_Normal_04_CamShake_E_01"), new Cue("116b15c9b1904c0fbe9841174463fbef", 48, CameraEffectKind.Shake, "Corin_Attack_Normal_04_CamShake_E_02") },
            "Corin_Attack_Normal_05" => new[] { new Cue("81c03e6246af4a42bcd7d1033bad93a2", 11, CameraEffectKind.Shake, "Corin_Attack_Normal_05_CamShake_E_01"), new Cue("94dc02ce68984a2a929cebc3a81363ac", 31, CameraEffectKind.Shake, "Corin_Attack_Normal_05_CamShake_E_02"), new Cue("7517e7c37316481ab6218338087d98e6", 0, CameraEffectKind.Stretch, "Corin_Attack_Normal_05_CamStretch_01"), new Cue("565603cae93c4954837b7e1bddf05f3a", 0, CameraEffectKind.Zoom, "Corin_Attack_Normal_05_CamZoom_01") },
            "Corin_Attack_Normal_05_End_2" => new[] { new Cue("852f9df220cc4571b4ce70745d4b8343", 10, CameraEffectKind.Shake, "Corin_Attack_Normal_05_CamShake_E_03"), new Cue("700af8a1d721406a913978eaceda4633", 0, CameraEffectKind.Stretch, "Corin_Attack_Normal_05_CamStretch_02"), new Cue("3e881e7f970640689798f109d2f9d6db", 0, CameraEffectKind.Zoom, "Corin_Attack_Normal_05_CamZoom_02") },
            "Corin_Attack_Rush" => new[] { new Cue("c499d8c7db1140bd8575fba4b2755b2f", 8, CameraEffectKind.Shake, "Corin_Attack_Rush_CamShake_E_01"), new Cue("99f66ecc95ad45f9a442d8281c5be9e0", 22, CameraEffectKind.Shake, "Corin_Attack_Rush_CamShake_E_01"), new Cue("bb271ce3d52c4c8792ec5201ea48d453", 38, CameraEffectKind.Shake, "Corin_Attack_Rush_CamShake_E_01"), new Cue("ce1c5c57d28e404db380538d0001fe19", 54, CameraEffectKind.Shake, "Corin_Attack_Rush_CamShake_E_01") },
            "Corin_Attack_Rush_Enhance" => new[] { new Cue("c499d8c7db1140bd8575fba4b2755b2f", 8, CameraEffectKind.Shake, "Corin_Attack_Rush_Enhance_CamShake_E_01"), new Cue("99f66ecc95ad45f9a442d8281c5be9e0", 22, CameraEffectKind.Shake, "Corin_Attack_Rush_Enhance_CamShake_E_01"), new Cue("bb271ce3d52c4c8792ec5201ea48d453", 38, CameraEffectKind.Shake, "Corin_Attack_Rush_Enhance_CamShake_E_01") },
            "Corin_Attack_Rush_Enhance_Explode" => new[] { new Cue("47d440cddaae4e0a9857fd8e15f9ef26", 1, CameraEffectKind.Shake, "Corin_Attack_Rush_CamShake_E_02") },
            "Corin_Attack_Rush_Enhance_Loop" => new[] { new Cue("6389062f28824031838f413c9f9e8c23", 8, CameraEffectKind.Shake, "Corin_Attack_Rush_Enhance_CamShake_E_01"), new Cue("76e6a15bd10c4cbdb954d651c47525f2", 30, CameraEffectKind.Shake, "Corin_Attack_Rush_Enhance_CamShake_E_01"), new Cue("295794ddc2da490aa137ce264a1df980", 52, CameraEffectKind.Shake, "Corin_Attack_Rush_Enhance_CamShake_E_01"), new Cue("3a98dd480a6343e79b15f0a4e6cfcc8d", 74, CameraEffectKind.Shake, "Corin_Attack_Rush_Enhance_CamShake_E_01") },
            "Corin_Attack_Rush_Explode" => new[] { new Cue("47d440cddaae4e0a9857fd8e15f9ef26", 1, CameraEffectKind.Shake, "Corin_Attack_Rush_CamShake_E_02") },
            _ => Array.Empty<Cue>()
        };
    }
}
