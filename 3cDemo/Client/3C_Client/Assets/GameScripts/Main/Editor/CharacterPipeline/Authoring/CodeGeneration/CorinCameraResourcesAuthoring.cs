using System;
using System.Collections.Generic;
using System.Globalization;
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
            PublishDefaultOrbit(profile);
            PublishDelay(profile);
            PublishShakeProcessing(profile);
            PublishInput(profile);
            PublishEffectSettings(profile);
            PublishCollision(profile);
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

        public static void PublishDefaultOrbit(CharacterCameraProfile profile)
        {
            JToken source = ReadAvatarConfiguration();
            JToken sphere = source["DEFAULTSPHEREDATA"];
            var orbitSource = (JArray)sphere["Orbits"];
            var orbits = new CameraTrackOrbitDescriptor[orbitSource.Count];
            for (int i = 0; i < orbits.Length; i++)
                orbits[i] = new CameraTrackOrbitDescriptor((float)orbitSource[i]["m_Height"], (float)orbitSource[i]["m_Radius"]);
            JToken screen = sphere["ScreenYTrack"];
            var screenOffsets = new[]
            {
                new Vector2(0f, (float)screen["Bottom"]),
                new Vector2(0f, (float)screen["Middle"]),
                new Vector2(0f, (float)screen["Top"])
            };
            CameraSequenceAsset sequence = profile.DefaultSequence;
            JToken topOrbit = sphere["TopOrbit"];
            JToken followOffset = sphere["DEFAULT_FOLLOWOFFSET"];
            JToken aimOffset = sphere["DEFAULT_LOOKATOFFSET"];
            Undo.RecordObject(sequence, "从解包配置可琳基础轨道");
            var track = (CameraFrameOnePointByTrackStage)sequence.Stages[0];
            track.ConfigureOrbit(orbits, screenOffsets, (float)sphere["CAMERA_FOV"],
                (float)source["ELEVATION_ANGLE"], (float)sphere["CAMERA_LOCATE_RADIUSRATIO"],
                new CameraTrackOrbitDescriptor((float)topOrbit["m_Height"], (float)topOrbit["m_Radius"]),
                (float)sphere["TopCurvature"],
                new Vector3((float)followOffset["x"], (float)followOffset["y"], (float)followOffset["z"]),
                new Vector3((float)aimOffset["x"], (float)aimOffset["y"], (float)aimOffset["z"]));
            Save(sequence);
        }

        public static void PublishDelay(CharacterCameraProfile profile)
        {
            JToken source = ReadAvatarConfiguration();
            var modeSource = (JObject)source["DELAYDATAS"];
            var modes = new CameraDelayModeSettings[modeSource.Count];
            int modeIndex = 0;
            foreach (JProperty property in modeSource.Properties())
            {
                JToken mode = property.Value;
                modes[modeIndex++] = new CameraDelayModeSettings(
                    int.Parse(property.Name, CultureInfo.InvariantCulture),
                    (float)mode["mFOV"], (float)mode["CAM_MINDISRATIO"],
                    ReadDelayOrbit(mode["mBottomCameraDelayData"]),
                    ReadDelayOrbit(mode["mMiddleCameraDelayData"]),
                    ReadDelayOrbit(mode["mTopCameraDelayData"]));
            }
            var blendSource = (JArray)source["DELAY_CustomBlendDatas"]["m_CustomBlends"];
            var blends = new CameraDelayBlendSettings[blendSource.Count];
            for (int i = 0; i < blends.Length; i++)
            {
                JToken entry = blendSource[i];
                JToken blend = entry["m_Blend"];
                blends[i] = new CameraDelayBlendSettings(
                    (int)entry["m_From"], (int)entry["m_To"], (int)blend["m_Style"],
                    (float)blend["m_Time"], (float)blend["m_StableTime"], ReadDelayCurve(blend["m_CustomCurve"]));
            }
            var settings = new CameraDelaySettings(
                (bool)source["MUTE_DELAY_USING"], (bool)source["DELAY_ISAUTOCHANGECAMERASTATE"],
                (bool)source["DELAY_ISCHANGEFOLLOWANIM"], (int)source["DELAY_CameraDelayMoveMode"],
                (float)source["DELAY_SPEED_SMOOTH_TIME"], (float)source["DragConfig"]["NapCamOrbitLerpTime"],
                (float)source["camOverAxisProtectRadius"], modes, blends,
                new CameraVerticalDelaySettings((float)source["camUpVelocityY"], (float)source["camUpDumperY"],
                    (float)source["camUpDumperTimer"], ReadDelayCurve(source["CamUpDumperCurve"])),
                new CameraVerticalDelaySettings((float)source["camDropVelocityY"], (float)source["camDropDumperY"],
                    (float)source["camDropDumperTimer"], ReadDelayCurve(source["CamDropDumperCurve"])));
            Undo.RecordObject(profile, "从解包配置可琳相机跟随延迟");
            profile.ConfigureDelay(settings, (float)source["DEFAULT_SMOOTH_TIME"]);
            Save(profile);
        }

        static CameraDelayOrbitSettings ReadDelayOrbit(JToken source) => new CameraDelayOrbitSettings(
            (float)source["Delay_FollowRotateCoef"],
            new Vector3((float)source["DELAY_FOLLOW_X_DUMPING"], (float)source["DELAY_FOLLOW_Y_DUMPING"],
                (float)source["DELAY_FOLLOW_Z_DUMPING"]),
            new Vector3((float)source["DELAY_FOLLOW_PITCH_DUMPING"], (float)source["DELAY_FOLLOW_YAW_DUMPING"],
                (float)source["DELAY_FOLLOW_ROLL_DUMPING"]),
            ReadDelayDirection(source["DELAY_FOLLOW_MOVEDIRCETION_RADIO"]),
            ReadDelayAnimation(source["DELAY_FOLLOW_ANIMSTATE_RADIO"]),
            new Vector2((float)source["DELAY_HorizontalDamping"], (float)source["DELAY_VerticalDamping"]),
            (float)source["DELAY_ROTATE_DUMPING"],
            new Vector2((float)source["DELAY_ScreenX"], (float)source["DELAY_ScreenY"]),
            new Vector2((float)source["DELAY_DeadZoneWidth"], (float)source["DELAY_DeadZoneHeight"]),
            new Vector2((float)source["DELAY_SoftZoneWidth"], (float)source["DELAY_SoftZoneHeight"]),
            new Vector2((float)source["DELAY_BiasX"], (float)source["DELAY_BiasY"]),
            ReadDelayDirection(source["DELAY_LOOKAT_MOVEDIRCETION_RADIO"]),
            ReadDelayAnimation(source["DELAY_LOOKAT_ANIMSTATE_RADIO"]));

        static CameraDelayDirectionSettings ReadDelayDirection(JToken source) => new CameraDelayDirectionSettings(
            (float)source["RATIO_CAMERA_DIRECTION_IDLE"], (float)source["RATIO_CAMERA_DIRECTION_SIDE"],
            (float)source["RATIO_CAMERA_DIRECTION_FORWARD"], (float)source["RATIO_CAMERA_DIRECTION_BACKWARD"]);

        static CameraDelayAnimationSettings ReadDelayAnimation(JToken source)
        {
            var stateSource = (JObject)source["RATIO_CAMERA_STATE"];
            var states = new CameraDelayStateRatio[stateSource.Count];
            int index = 0;
            foreach (JProperty entry in stateSource.Properties())
                states[index++] = new CameraDelayStateRatio(entry.Name, (float)entry.Value);
            var tagSource = (JObject)source["RATIO_CAMERA_TAG"];
            var tags = new CameraDelayTagRatio[tagSource.Count];
            index = 0;
            foreach (JProperty entry in tagSource.Properties())
                tags[index++] = new CameraDelayTagRatio(int.Parse(entry.Name, CultureInfo.InvariantCulture), (float)entry.Value);
            return new CameraDelayAnimationSettings(states, tags);
        }

        static AnimationCurve ReadDelayCurve(JToken source)
        {
            var keysSource = (JArray)source["keys"];
            var keys = new Keyframe[keysSource.Count];
            for (int i = 0; i < keys.Length; i++)
            {
                JToken key = keysSource[i];
                keys[i] = new Keyframe((float)key["m_Time"], (float)key["m_Value"],
                    (float)key["m_InTangent"], (float)key["m_OutTangent"],
                    (float)key["m_InWeight"], (float)key["m_OutWeight"])
                {
                    weightedMode = (WeightedMode)(int)key["m_WeightedMode"]
                };
            }
            return new AnimationCurve(keys)
            {
                preWrapMode = (WrapMode)(int)source["preWrapMode"],
                postWrapMode = (WrapMode)(int)source["postWrapMode"]
            };
        }

        public static void PublishInput(CharacterCameraProfile profile)
        {
            string snapshotPath = Path.GetFullPath(Path.Combine(Application.dataPath,
                "../../../../docs/diagnostics/camera/camera-basis-runtime-20260929/pointer-input-snapshot.json"));
            JToken source = ReadAvatarConfiguration();
            JToken snapshot = JObject.Parse(File.ReadAllText(snapshotPath, Encoding.UTF8));
            JToken settings = snapshot["player_fields"];
            JToken x = source["DragConfig"]["Drag_XAxis"];
            JToken y = source["DragConfig"]["Drag_YAxis"];
            Vector2 playerGain = new Vector2((float)settings["0x150"], (float)settings["0x19c"]);
            float referenceRate = (float)snapshot["constants"]["device_normalization_rate"]["value"];
            float prepareScale = (float)snapshot["constants"]["prepare_rate"]["value"];
            Vector2 processorScale = new Vector2((float)snapshot["action"]["processor_scale"]["x"],
                (float)snapshot["action"]["processor_scale"]["y"]);
            Undo.RecordObject(profile, "从解包和829玩家配置导入相机输入轴");
            profile.Input.ConfigureMovementInput("MoveAxis");
            profile.Input.ConfigureAxes(
                new Vector2((float)x["m_MaxSpeed"], (float)y["m_MaxSpeed"]),
                new Vector2((float)x["m_AccelTime"], (float)y["m_AccelTime"]),
                new Vector2((float)x["m_DecelTime"], (float)y["m_DecelTime"]),
                Vector2.Scale(processorScale, playerGain) * ((float)settings["0x1bc"] * prepareScale / referenceRate),
                playerGain * ((float)settings["0x1a8"] * prepareScale),
                new Vector2((float)settings["0x17c"], (float)settings["0x194"]),
                new Vector2((float)settings["0x164"], (float)settings["0x174"]),
                new Vector2((bool)x["m_InvertInput"] ^ (bool)settings["0x1c4"] ? -1f : 1f,
                    (bool)y["m_InvertInput"] ^ (bool)settings["0x19b"] ? -1f : 1f),
                new Vector2((float)source["DRAG_ELEVATION_REGIOIN"]["x"], (float)source["DRAG_ELEVATION_REGIOIN"]["y"]));
            float activationThreshold = (float)snapshot["constants"]["drag_activation_threshold"]["value"];
            profile.Input.ConfigureDrag(
                playerGain * (prepareScale * activationThreshold),
                playerGain * ((float)settings["0x1a8"] * prepareScale * activationThreshold),
                (float)source["DragConfig"]["DRAG_TO_EXIT_DURATION"]);
            Save(profile);
        }

        public static void PublishShakeProcessing(CharacterCameraProfile profile)
        {
            JToken source = ReadAvatarConfiguration();
            Undo.RecordObject(profile, "从解包配置相机震动处理开关");
            profile.ConfigureShakeProcessing((bool)source["MUTE_CAMERA_SHAKE"],
                (bool)source["MUTE_CAMERA_SHAKE_ADVANCED_PROCESS"]);
            Save(profile);
        }

        public static void PublishCollision(CharacterCameraProfile profile)
        {
            JToken source = ReadAvatarConfiguration();
            JToken collision = source["CinemachineCollisionConfig"];
            Undo.RecordObject(profile, "从解包配置可琳相机碰撞");
            profile.Collision.Configure(!(bool)source["MUTE_CAMERA_COLLIDER"],
                (float)collision["COLLIDER_CAMERARADIUS"],
                (float)collision["COLLIDER_MINDISFROMTARGET"],
                (float)collision["COLLIDER_DISTANCELIMIT"],
                (float)collision["m_Damping"]);
            Save(profile);
        }

        static JToken ReadAvatarConfiguration()
        {
            const string sourcePath = "D:/ZZZ_Dump/output/corin_replication/replication-guide/analysis/camera-data/Pipeline_Camera_Avatar_Config__1021078955_DFB680A125EE4808.json";
            return JObject.Parse(File.ReadAllText(sourcePath, Encoding.UTF8))["cameraAvatarGroup"]["Default_Normal"];
        }

        public static void PublishEffectSettings(CharacterCameraProfile profile)
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
                JToken source = stretches[stretch.StretchId];
                stretch.ConfigureStacking(
                    DecodePlaybackStacking((int)source["PlayStackingType"]), (int)source["StackingType"]);
                JToken pointSource = source["RuntimeCamFollowYPoints"];
                string[] followPoints = pointSource.Type == JTokenType.Null
                    ? Array.Empty<string>()
                    : pointSource.ToObject<string[]>();
                stretch.ConfigureFollowPoints(followPoints);
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
