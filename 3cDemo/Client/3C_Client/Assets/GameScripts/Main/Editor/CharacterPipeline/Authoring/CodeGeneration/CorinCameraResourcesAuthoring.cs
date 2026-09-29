using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;
using ThirdPersonCamera;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public static class CorinCameraResourcesAuthoring
    {
        const string Folder = "Assets/Configs/Character/Corin/Pipeline/Presentation/Camera/";

        public static void Publish()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("Camera authoring requires an idle Editor.");
            var profile = AssetDatabase.LoadAssetAtPath<CharacterCameraProfile>(Folder + "CorinCharacterCameraProfile.asset");
            PublishPlaybackStacking(profile);
            var curves = new Dictionary<string, CameraCurveAsset>(StringComparer.Ordinal);
            foreach (var existing in profile.Curves) curves.Add(existing.CurveId, existing);
            var shakes = new Dictionary<string, CameraShakeAsset>(StringComparer.Ordinal);
            foreach (var existing in profile.Shakes) shakes.Add(existing.ShakeId, existing);
            var curve0 = LoadOrCreate<CameraCurveAsset>("Camera_ShakeDecay_Curve_02");
            curve0.Configure("Camera_ShakeDecay_Curve_02", new AnimationCurve(new Keyframe(0f, 1f, -3.07459331f, -3.07459331f), new Keyframe(0.999976099f, 4.64482873e-05f, 0f, 0f)), CameraTimeDomain.PresentationScaled, 0f, 1f, "normalized");
            curves["Camera_ShakeDecay_Curve_02"] = curve0;
            Save(curve0);
            var curve1 = LoadOrCreate<CameraCurveAsset>("Camera_ShakeDecay_Curve_04");
            curve1.Configure("Camera_ShakeDecay_Curve_04", new AnimationCurve(new Keyframe(0f, 1f, 0f, 0f), new Keyframe(1f, 1f, 0f, 0f)), CameraTimeDomain.PresentationScaled, 0f, 1f, "normalized");
            curves["Camera_ShakeDecay_Curve_04"] = curve1;
            Save(curve1);
            var curve2 = LoadOrCreate<CameraCurveAsset>("Camera_ShakeSpatial_Curve_01");
            curve2.Configure("Camera_ShakeSpatial_Curve_01", new AnimationCurve(new Keyframe(0f, 0.5f, 0f, 0f), new Keyframe(2f, 0.5f, 0f, 0f), new Keyframe(4f, 1f, 0f, 0f), new Keyframe(10f, 1f, 0f, 0f)), CameraTimeDomain.PresentationScaled, 0f, 1f, "normalized");
            curves["Camera_ShakeSpatial_Curve_01"] = curve2;
            Save(curve2);
            var shake0 = LoadOrCreate<CameraShakeAsset>("Corin_Attack_Branch_02_CamShake_E_01");
            shake0.Configure("Corin_Attack_Branch_02_CamShake_E_01", 0, 1, 300f, 10f, 0.150000006f, 0f, 0f, 0.600000024f, 20f, 0f, 0f, 0f, CameraSpace.Camera, false, 5, 0f, 0f, "Camera_ShakeSpatial_Curve_01", 0f, null, 0f, null, curves["Camera_ShakeDecay_Curve_02"], false, CameraEffectStackingType.Replace, 0, 0, "CamShake_E_02");
            shakes["Corin_Attack_Branch_02_CamShake_E_01"] = shake0;
            Save(shake0);
            var shake1 = LoadOrCreate<CameraShakeAsset>("Corin_Attack_Branch_02_CamShake_E_02");
            shake1.Configure("Corin_Attack_Branch_02_CamShake_E_02", 0, 1, 180f, 10f, 0.25f, 0f, 0f, 0.600000024f, 20f, 0f, 0f, 0f, CameraSpace.Camera, false, 5, 0f, 0f, "Camera_ShakeSpatial_Curve_01", 0f, null, 0f, null, curves["Camera_ShakeDecay_Curve_02"], false, CameraEffectStackingType.Replace, 0, 0, "CamShake_E_03");
            shakes["Corin_Attack_Branch_02_CamShake_E_02"] = shake1;
            Save(shake1);
            var shake2 = LoadOrCreate<CameraShakeAsset>("Corin_Attack_Branch_02_CamShake_E_03");
            shake2.Configure("Corin_Attack_Branch_02_CamShake_E_03", 0, 1, 260f, 10f, 0.150000006f, 0f, 0f, 0.600000024f, 20f, 0f, 0f, 0f, CameraSpace.Camera, false, 5, 0f, 0f, "Camera_ShakeSpatial_Curve_01", 0f, null, 0f, null, curves["Camera_ShakeDecay_Curve_02"], false, CameraEffectStackingType.Replace, 0, 0, "CamShake_E_02");
            shakes["Corin_Attack_Branch_02_CamShake_E_03"] = shake2;
            Save(shake2);
            var shake3 = LoadOrCreate<CameraShakeAsset>("Corin_Attack_Normal_01_CamShake_A_01");
            shake3.Configure("Corin_Attack_Normal_01_CamShake_A_01", 0, 1, -20f, 10f, 0.0250000004f, 0f, 0f, 0.600000024f, 12f, 0f, 0f, 0f, CameraSpace.Camera, false, 5, 0f, 0f, "Camera_ShakeSpatial_Curve_01", 0f, null, 0f, null, curves["Camera_ShakeDecay_Curve_02"], true, CameraEffectStackingType.Replace, 0, 0, "CamShake_A_01");
            shakes["Corin_Attack_Normal_01_CamShake_A_01"] = shake3;
            Save(shake3);
            var shake4 = LoadOrCreate<CameraShakeAsset>("Corin_Attack_Normal_02_CamShake_E_01");
            shake4.Configure("Corin_Attack_Normal_02_CamShake_E_01", 0, 1, 260f, 10f, 0.150000006f, 0f, 0f, 0.600000024f, 20f, 0f, 0f, 0f, CameraSpace.Camera, false, 5, 0f, 0f, "Camera_ShakeSpatial_Curve_01", 0f, null, 0f, null, curves["Camera_ShakeDecay_Curve_02"], false, CameraEffectStackingType.Replace, 0, 0, "CamShake_E_02");
            shakes["Corin_Attack_Normal_02_CamShake_E_01"] = shake4;
            Save(shake4);
            var shake5 = LoadOrCreate<CameraShakeAsset>("Corin_Attack_Normal_04_CamShake_E_01");
            shake5.Configure("Corin_Attack_Normal_04_CamShake_E_01", 0, 1, 80f, 10f, 0.150000006f, 0f, 0f, 0.600000024f, 20f, 0f, 0f, 0f, CameraSpace.Camera, false, 5, 0f, 0f, "Camera_ShakeSpatial_Curve_01", 0f, null, 0f, null, curves["Camera_ShakeDecay_Curve_02"], false, CameraEffectStackingType.Replace, 0, 0, "CamShake_E_02");
            shakes["Corin_Attack_Normal_04_CamShake_E_01"] = shake5;
            Save(shake5);
            var shake6 = LoadOrCreate<CameraShakeAsset>("Corin_Attack_Normal_04_CamShake_E_02");
            shake6.Configure("Corin_Attack_Normal_04_CamShake_E_02", 0, 1, 270f, 10f, 0.150000006f, 0f, 0f, 0.600000024f, 20f, 0f, 0f, 0f, CameraSpace.Camera, false, 5, 0f, 0f, "Camera_ShakeSpatial_Curve_01", 0f, null, 0f, null, curves["Camera_ShakeDecay_Curve_02"], false, CameraEffectStackingType.Replace, 0, 0, "CamShake_E_02");
            shakes["Corin_Attack_Normal_04_CamShake_E_02"] = shake6;
            Save(shake6);
            var shake7 = LoadOrCreate<CameraShakeAsset>("Corin_Attack_Normal_05_CamShake_E_01");
            shake7.Configure("Corin_Attack_Normal_05_CamShake_E_01", 0, 1, 90f, 10f, 0.100000001f, 0f, 0f, 0.600000024f, 20f, 0f, 0f, 0f, CameraSpace.Camera, false, 5, 0f, 0f, "Camera_ShakeSpatial_Curve_01", 0f, null, 0f, null, curves["Camera_ShakeDecay_Curve_02"], false, CameraEffectStackingType.Replace, 0, 0, "CamShake_E_01");
            shakes["Corin_Attack_Normal_05_CamShake_E_01"] = shake7;
            Save(shake7);
            var shake8 = LoadOrCreate<CameraShakeAsset>("Corin_Attack_Normal_05_CamShake_E_02");
            shake8.Configure("Corin_Attack_Normal_05_CamShake_E_02", 0, 1, 110f, 10f, 0.150000006f, 0f, 0f, 0.600000024f, 20f, 0f, 0f, 0f, CameraSpace.Camera, false, 5, 0f, 0f, "Camera_ShakeSpatial_Curve_01", 0f, null, 0f, null, curves["Camera_ShakeDecay_Curve_02"], false, CameraEffectStackingType.Replace, 0, 0, "CamShake_E_02");
            shakes["Corin_Attack_Normal_05_CamShake_E_02"] = shake8;
            Save(shake8);
            var shake9 = LoadOrCreate<CameraShakeAsset>("Corin_Attack_Normal_05_CamShake_E_03");
            shake9.Configure("Corin_Attack_Normal_05_CamShake_E_03", 0, 1, 280f, 10f, 0.150000006f, 0f, 0f, 0.600000024f, 20f, 0f, 0f, 0f, CameraSpace.Camera, false, 5, 0f, 0f, "Camera_ShakeSpatial_Curve_01", 0f, null, 0f, null, curves["Camera_ShakeDecay_Curve_02"], false, CameraEffectStackingType.Replace, 0, 0, "CamShake_E_02");
            shakes["Corin_Attack_Normal_05_CamShake_E_03"] = shake9;
            Save(shake9);
            var shake10 = LoadOrCreate<CameraShakeAsset>("Corin_Attack_Rush_CamShake_E_01");
            shake10.Configure("Corin_Attack_Rush_CamShake_E_01", 0, 0, 90f, 10f, 0.0399999991f, 0f, 0f, 0.300000012f, 20f, 0f, 0f, 0f, CameraSpace.Camera, false, 5, 0f, 0f, "Camera_ShakeSpatial_Curve_01", 0f, null, 0f, null, curves["Camera_ShakeDecay_Curve_04"], false, CameraEffectStackingType.Replace, 0, 0, null);
            shakes["Corin_Attack_Rush_CamShake_E_01"] = shake10;
            Save(shake10);
            var shake11 = LoadOrCreate<CameraShakeAsset>("Corin_Attack_Rush_CamShake_E_02");
            shake11.Configure("Corin_Attack_Rush_CamShake_E_02", 0, 0, 90f, 10f, 0.0399999991f, 0f, 0f, 1.5f, 20f, 0f, 0f, 0f, CameraSpace.Camera, false, 5, 0f, 0f, "Camera_ShakeSpatial_Curve_01", 0f, null, 0f, null, curves["Camera_ShakeDecay_Curve_02"], false, CameraEffectStackingType.Replace, 0, 0, null);
            shakes["Corin_Attack_Rush_CamShake_E_02"] = shake11;
            Save(shake11);
            var shake12 = LoadOrCreate<CameraShakeAsset>("Corin_Attack_Rush_Enhance_CamShake_E_01");
            shake12.Configure("Corin_Attack_Rush_Enhance_CamShake_E_01", 0, 0, 90f, 10f, 0.0399999991f, 0f, 0f, 0.300000012f, 20f, 0f, 0f, 0f, CameraSpace.Camera, false, 5, 0f, 0f, "Camera_ShakeSpatial_Curve_01", 0f, null, 0f, null, curves["Camera_ShakeDecay_Curve_04"], false, CameraEffectStackingType.Replace, 0, 0, null);
            shakes["Corin_Attack_Rush_Enhance_CamShake_E_01"] = shake12;
            Save(shake12);
            var branchHitShake = LoadOrCreate<CameraShakeAsset>("Corin_Attack_Branch_02_CamShake_A_01");
            branchHitShake.Configure("Corin_Attack_Branch_02_CamShake_A_01", 0, 1, 180f, 10f, 0.0250000004f, 0f, 0f, 0.600000024f, 12f, 0f, 0f, 0f, CameraSpace.Camera, false, 5, 0f, 0f, "Camera_ShakeSpatial_Curve_01", 0f, null, 0f, null, curves["Camera_ShakeDecay_Curve_02"], true, CameraEffectStackingType.Replace, 0, 0, "CamShake_A_01");
            shakes["Corin_Attack_Branch_02_CamShake_A_01"] = branchHitShake;
            Save(branchHitShake);
            var rushHitShake = LoadOrCreate<CameraShakeAsset>("Corin_Attack_Rush_CamShake_A_01");
            rushHitShake.Configure("Corin_Attack_Rush_CamShake_A_01", 0, 1, 90f, 10f, 0.0250000004f, 0f, 0f, 0.600000024f, 12f, 0f, 0f, 0f, CameraSpace.Camera, false, 5, 0f, 0f, "Camera_ShakeSpatial_Curve_01", 0f, null, 0f, null, curves["Camera_ShakeDecay_Curve_02"], true, CameraEffectStackingType.Replace, 0, 0, "CamShake_A_01");
            shakes["Corin_Attack_Rush_CamShake_A_01"] = rushHitShake;
            Save(rushHitShake);
            var normal2HitShake = LoadOrCreate<CameraShakeAsset>("Corin_Attack_Normal_02_CamShake_A_01");
            normal2HitShake.Configure("Corin_Attack_Normal_02_CamShake_A_01", 0, 1, 260f, 10f, 0.0250000004f, 0f, 0f, 0.600000024f, 12f, 0f, 0f, 0f, CameraSpace.Camera, false, 5, 0f, 0f, "Camera_ShakeSpatial_Curve_01", 0f, null, 0f, null, curves["Camera_ShakeDecay_Curve_02"], true, CameraEffectStackingType.Replace, 0, 0, "CamShake_A_01");
            shakes["Corin_Attack_Normal_02_CamShake_A_01"] = normal2HitShake;
            Save(normal2HitShake);
            var normal3HitShake = LoadOrCreate<CameraShakeAsset>("Corin_Attack_Normal_03_CamShake_A_01");
            normal3HitShake.Configure("Corin_Attack_Normal_03_CamShake_A_01", 0, 1, 90f, 10f, 0.0250000004f, 0f, 0f, 0.600000024f, 12f, 0f, 0f, 0f, CameraSpace.Camera, false, 5, 0f, 0f, "Camera_ShakeSpatial_Curve_01", 0f, null, 0f, null, curves["Camera_ShakeDecay_Curve_02"], true, CameraEffectStackingType.Replace, 0, 0, "CamShake_A_01");
            shakes["Corin_Attack_Normal_03_CamShake_A_01"] = normal3HitShake;
            Save(normal3HitShake);
            var normal4FirstHitShake = LoadOrCreate<CameraShakeAsset>("Corin_Attack_Normal_04_CamShake_A_01");
            normal4FirstHitShake.Configure("Corin_Attack_Normal_04_CamShake_A_01", 0, 1, 0f, 10f, 0.0250000004f, 0f, 0f, 0.600000024f, 12f, 0f, 0f, 0f, CameraSpace.Camera, false, 5, 0f, 0f, "Camera_ShakeSpatial_Curve_01", 0f, null, 0f, null, curves["Camera_ShakeDecay_Curve_02"], true, CameraEffectStackingType.Replace, 0, 0, "CamShake_A_01");
            shakes["Corin_Attack_Normal_04_CamShake_A_01"] = normal4FirstHitShake;
            Save(normal4FirstHitShake);
            var normal4SecondHitShake = LoadOrCreate<CameraShakeAsset>("Corin_Attack_Normal_04_CamShake_A_02");
            normal4SecondHitShake.Configure("Corin_Attack_Normal_04_CamShake_A_02", 0, 1, 80f, 10f, 0.0250000004f, 0f, 0f, 0.600000024f, 12f, 0f, 0f, 0f, CameraSpace.Camera, false, 5, 0f, 0f, "Camera_ShakeSpatial_Curve_01", 0f, null, 0f, null, curves["Camera_ShakeDecay_Curve_02"], true, CameraEffectStackingType.Replace, 0, 0, "CamShake_A_01");
            shakes["Corin_Attack_Normal_04_CamShake_A_02"] = normal4SecondHitShake;
            Save(normal4SecondHitShake);
            var normal5HitShake = LoadOrCreate<CameraShakeAsset>("Corin_Attack_Normal_05_CamShake_A_01");
            normal5HitShake.Configure("Corin_Attack_Normal_05_CamShake_A_01", 0, 1, 110f, 10f, 0.0250000004f, 0f, 0f, 0.600000024f, 12f, 0f, 0f, 0f, CameraSpace.Camera, false, 5, 0f, 0f, "Camera_ShakeSpatial_Curve_01", 0f, null, 0f, null, curves["Camera_ShakeDecay_Curve_02"], true, CameraEffectStackingType.Replace, 0, 0, "CamShake_A_01");
            shakes["Corin_Attack_Normal_05_CamShake_A_01"] = normal5HitShake;
            Save(normal5HitShake);
            var branchExplodeHitShake = LoadOrCreate<CameraShakeAsset>("Corin_Attack_Branch_02_CamShake_A_02");
            branchExplodeHitShake.Configure("Corin_Attack_Branch_02_CamShake_A_02", 0, 1, 180f, 10f, 0.0500000007f, 0f, 0f, 0.600000024f, 20f, 0f, 0f, 0f, CameraSpace.Camera, false, 5, 0f, 0f, "Camera_ShakeSpatial_Curve_01", 0f, null, 0f, null, curves["Camera_ShakeDecay_Curve_02"], true, CameraEffectStackingType.Replace, 0, 0, "CamShake_A_03");
            shakes["Corin_Attack_Branch_02_CamShake_A_02"] = branchExplodeHitShake;
            Save(branchExplodeHitShake);
            Undo.RecordObject(profile, "配置可琳相机资源");
            profile.ConfigureShakeResources(new List<CameraShakeAsset>(shakes.Values).ToArray(), new List<CameraCurveAsset>(curves.Values).ToArray());
            Save(profile);
        }

        static void PublishPlaybackStacking(CharacterCameraProfile profile)
        {
            const string sourceFolder = "D:/ZZZ_Dump/output/corin_replication/replication-guide/data/variants/";
            JToken zooms = JObject.Parse(File.ReadAllText(sourceFolder + "zoom-0.json", Encoding.UTF8))["cameraZooms"];
            JToken stretches = JObject.Parse(File.ReadAllText(sourceFolder + "stretch-0.json", Encoding.UTF8))["cameraStretchs"];
            foreach (CameraZoomAsset zoom in profile.Zooms)
            {
                Undo.RecordObject(zoom, "配置可琳推镜播放叠加");
                zoom.ConfigurePlaybackStacking(DecodePlaybackStacking((int)zooms[zoom.ZoomId]["PlayStackingType"]));
                Save(zoom);
            }
            foreach (CameraStretchAsset stretch in profile.Stretches)
            {
                Undo.RecordObject(stretch, "配置可琳位移镜头播放叠加");
                stretch.ConfigurePlaybackStacking(DecodePlaybackStacking((int)stretches[stretch.StretchId]["PlayStackingType"]));
                Save(stretch);
            }
        }

        static CameraEffectStackingType DecodePlaybackStacking(int value) => value switch
        {
            0 => CameraEffectStackingType.Replace,
            1 => CameraEffectStackingType.Add,
            _ => throw new NotSupportedException($"Unknown ConfigDataPlayStacking value '{value}'.")
        };

        static T LoadOrCreate<T>(string id) where T : ScriptableObject
        {
            string path = Folder + id + ".asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (!asset)
            {
                asset = ScriptableObject.CreateInstance<T>();
                asset.name = id;
                AssetDatabase.CreateAsset(asset, path);
            }
            Undo.RecordObject(asset, "配置相机资源");
            return asset;
        }

        static void Save(UnityEngine.Object asset)
        {
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);
        }
    }
}
