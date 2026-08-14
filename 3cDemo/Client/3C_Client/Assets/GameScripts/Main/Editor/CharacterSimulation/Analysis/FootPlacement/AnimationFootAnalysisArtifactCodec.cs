using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public static class AnimationFootAnalysisArtifactCodec
    {
        const int Magic = 0x31414643;
        const int MaximumPayloadBytes = 64 * 1024 * 1024;
        const int MaximumStringBytes = 16 * 1024;
        const int MaximumKeysPerCurve = 1024 * 1024;
        const int MaximumSynchronizationSamples = 1024 * 1024;

        public static byte[] Write(
            AnimationFootAnalysisArtifactIdentity identity,
            AnimationFootFeaturePair features,
            AnimationFootSynchronizationDescriptor synchronization,
            out StableHash contentHash)
        {
            if (identity == null)
                throw new ArgumentNullException(nameof(identity));
            if (!features.IsValid)
                throw new ArgumentException("Animation Foot Analysis features are invalid.", nameof(features));
            if (synchronization == null)
                throw new ArgumentNullException(nameof(synchronization));
            synchronization.RequireValid();
            byte[] payload;
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                WriteIdentity(writer, identity);
                WriteFeaturePair(writer, features);
                WriteSynchronizationDescriptor(writer, synchronization);
                writer.Flush();
                payload = stream.ToArray();
            }
            contentHash = Hash(payload);
            using var artifactStream = new MemoryStream();
            using var artifactWriter = new BinaryWriter(artifactStream, Encoding.UTF8, true);
            artifactWriter.Write(Magic);
            artifactWriter.Write(AnimationFootAnalysisArtifactIdentity.CurrentFormatVersion);
            artifactWriter.Write(payload.Length);
            artifactWriter.Write(payload);
            WriteString(artifactWriter, contentHash.Value);
            artifactWriter.Flush();
            return artifactStream.ToArray();
        }

        public static AnimationFootAnalysisArtifact Read(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                throw new InvalidDataException("Animation Foot Analysis artifact is empty.");
            using var stream = new MemoryStream(bytes, false);
            using var reader = new BinaryReader(stream, Encoding.UTF8, true);
            if (reader.ReadInt32() != Magic)
                throw new InvalidDataException("Animation Foot Analysis artifact magic is invalid.");
            int version = reader.ReadInt32();
            if (version != AnimationFootAnalysisArtifactIdentity.CurrentFormatVersion)
                throw new InvalidDataException($"Animation Foot Analysis artifact format '{version}' is unsupported.");
            int payloadLength = reader.ReadInt32();
            if (payloadLength <= 0 || payloadLength > MaximumPayloadBytes || payloadLength > stream.Length - stream.Position)
                throw new InvalidDataException("Animation Foot Analysis artifact payload length is invalid.");
            byte[] payload = reader.ReadBytes(payloadLength);
            if (payload.Length != payloadLength)
                throw new EndOfStreamException("Animation Foot Analysis artifact payload is truncated.");
            StableHash expectedHash = new StableHash(ReadString(reader));
            if (stream.Position != stream.Length)
                throw new InvalidDataException("Animation Foot Analysis artifact has trailing bytes.");
            StableHash actualHash = Hash(payload);
            if (!actualHash.Equals(expectedHash))
                throw new InvalidDataException("Animation Foot Analysis artifact payload hash does not match.");

            using var payloadStream = new MemoryStream(payload, false);
            using var payloadReader = new BinaryReader(payloadStream, Encoding.UTF8, true);
            AnimationFootAnalysisArtifactIdentity identity = ReadIdentity(payloadReader);
            AnimationFootFeaturePair features = ReadFeaturePair(payloadReader);
            AnimationFootSynchronizationDescriptor synchronization =
                ReadSynchronizationDescriptor(payloadReader);
            if (payloadStream.Position != payloadStream.Length)
                throw new InvalidDataException("Animation Foot Analysis payload has trailing bytes.");
            return new AnimationFootAnalysisArtifact(
                identity,
                features,
                synchronization,
                actualHash);
        }

        static void WriteIdentity(BinaryWriter writer, AnimationFootAnalysisArtifactIdentity value)
        {
            writer.Write(value.FormatVersion);
            WriteString(writer, value.ClipAssetGuid);
            WriteString(writer, value.ClipDependencyHash);
            WriteString(writer, value.AnalysisSourceAssetGuid);
            WriteString(writer, value.AnalysisSourceDependencyHash);
            WriteString(writer, value.AnalysisSourceId);
            writer.Write(value.AnalysisVersion);
            WriteString(writer, value.RigAssetGuid);
            WriteString(writer, value.RigId);
            WriteString(writer, value.RigRevision);
            WriteString(writer, value.RigContentHash);
            WriteString(writer, value.SamplingRigAssetGuid);
            WriteString(writer, value.SamplingRigDependencyHash);
            WriteString(writer, value.CalibrationAssetGuid);
            WriteString(writer, value.CalibrationId);
            writer.Write(value.CalibrationSchemaVersion);
            WriteString(writer, value.CalibrationRevision);
            WriteString(writer, value.GeometryValidationIdentity);
            WriteString(writer, value.GeometryValidationContentHash);
            WriteString(writer, value.ContactScheduleHash);
            writer.Write(value.SampleRate);
            writer.Write(value.PlantEnterContactSpeed);
            writer.Write(value.PlantExitContactSpeed);
            writer.Write(value.PlantEnterHeight);
            writer.Write(value.PlantExitHeight);
            writer.Write(value.MinimumLandingSegmentSeconds);
            writer.Write(value.MaximumLandingSearchSeconds);
            writer.Write(value.VelocityTolerance);
            writer.Write(value.HeightTolerance);
            writer.Write(value.ConfidenceTolerance);
            writer.Write(value.LandingDelayTolerance);
            writer.Write(value.LandingOffsetTolerance);
            WriteString(writer, value.AlgorithmVersion);
            WriteString(writer, value.IdentityHash.Value);
        }

        static AnimationFootAnalysisArtifactIdentity ReadIdentity(BinaryReader reader)
        {
            int version = reader.ReadInt32();
            if (version != AnimationFootAnalysisArtifactIdentity.CurrentFormatVersion)
                throw new InvalidDataException($"Animation Foot Analysis payload format '{version}' is unsupported.");
            var identity = new AnimationFootAnalysisArtifactIdentity(
                ReadString(reader),
                ReadString(reader),
                ReadString(reader),
                ReadString(reader),
                ReadString(reader),
                reader.ReadInt32(),
                ReadString(reader),
                ReadString(reader),
                ReadString(reader),
                ReadString(reader),
                ReadString(reader),
                ReadString(reader),
                ReadString(reader),
                ReadString(reader),
                reader.ReadInt32(),
                ReadString(reader),
                ReadString(reader),
                ReadString(reader),
                ReadString(reader),
                ReadFinite(reader, "sample rate"), ReadFinite(reader, "plant enter contact speed"),
                ReadFinite(reader, "plant exit contact speed"), ReadFinite(reader, "plant enter height"),
                ReadFinite(reader, "plant exit height"), ReadFinite(reader, "minimum landing segment"),
                ReadFinite(reader, "maximum landing search"), ReadFinite(reader, "velocity tolerance"),
                ReadFinite(reader, "height tolerance"), ReadFinite(reader, "confidence tolerance"),
                ReadFinite(reader, "landing delay tolerance"), ReadFinite(reader, "landing offset tolerance"),
                ReadString(reader));
            StableHash storedHash = new StableHash(ReadString(reader));
            if (!storedHash.Equals(identity.IdentityHash))
                throw new InvalidDataException("Animation Foot Analysis identity hash does not match its fields.");
            return identity;
        }

        static void WriteFeaturePair(BinaryWriter writer, AnimationFootFeaturePair value)
        {
            WriteCurveSet(writer, value.Left);
            WriteCurveSet(writer, value.Right);
        }

        static AnimationFootFeaturePair ReadFeaturePair(BinaryReader reader) =>
            new AnimationFootFeaturePair(ReadCurveSet(reader), ReadCurveSet(reader));

        static void WriteSynchronizationDescriptor(
            BinaryWriter writer,
            AnimationFootSynchronizationDescriptor value)
        {
            value.RequireValid();
            writer.Write(value.SampleRate);
            writer.Write(value.DurationSeconds);
            WriteSynchronizationFoot(writer, value.Left);
            WriteSynchronizationFoot(writer, value.Right);
        }

        static AnimationFootSynchronizationDescriptor ReadSynchronizationDescriptor(
            BinaryReader reader) =>
            new AnimationFootSynchronizationDescriptor(
                ReadFinite(reader, "synchronization sample rate"),
                ReadFinite(reader, "synchronization duration"),
                ReadSynchronizationFoot(reader),
                ReadSynchronizationFoot(reader));

        static void WriteSynchronizationFoot(
            BinaryWriter writer,
            AnimationFootSynchronizationFootDescriptor value)
        {
            value.RequireValid();
            writer.Write(value.Samples.Count);
            for (int i = 0; i < value.Samples.Count; i++)
            {
                AnimationFootSynchronizationSample sample = value.Samples[i];
                sample.RequireValid();
                writer.Write(sample.NormalizedTime);
                writer.Write(sample.RootLocalSolePlanarPosition.x);
                writer.Write(sample.RootLocalSolePlanarPosition.y);
                writer.Write(sample.CalibratedSoleHeight);
                writer.Write(sample.SoleLocalVelocity.x);
                writer.Write(sample.SoleLocalVelocity.y);
                writer.Write(sample.SoleLocalVelocity.z);
                writer.Write(sample.PlantConfidence);
            }
        }

        static AnimationFootSynchronizationFootDescriptor ReadSynchronizationFoot(
            BinaryReader reader)
        {
            int count = reader.ReadInt32();
            if (count < 3 || count > MaximumSynchronizationSamples)
                throw new InvalidDataException("Foot synchronization sample count is invalid.");
            var samples = new AnimationFootSynchronizationSample[count];
            for (int i = 0; i < count; i++)
            {
                samples[i] = new AnimationFootSynchronizationSample(
                    ReadFinite(reader, "synchronization normalized time"),
                    new Vector2(
                        ReadFinite(reader, "synchronization planar x"),
                        ReadFinite(reader, "synchronization planar z")),
                    ReadFinite(reader, "synchronization height"),
                    new Vector3(
                        ReadFinite(reader, "synchronization velocity x"),
                        ReadFinite(reader, "synchronization velocity y"),
                        ReadFinite(reader, "synchronization velocity z")),
                    ReadFinite(reader, "synchronization plant confidence"));
            }
            return new AnimationFootSynchronizationFootDescriptor(samples);
        }

        static void WriteCurveSet(BinaryWriter writer, AnimationFootFeatureCurveSet value)
        {
            value.RequireValid();
            WriteCurve(writer, value.SoleLocalVelocityX);
            WriteCurve(writer, value.SoleLocalVelocityY);
            WriteCurve(writer, value.SoleLocalVelocityZ);
            WriteCurve(writer, value.SoleHeight);
            WriteCurve(writer, value.PlantConfidence);
            WritePredictedStepCurveSet(writer, value.PredictedStep);
        }

        static AnimationFootFeatureCurveSet ReadCurveSet(BinaryReader reader) =>
            new AnimationFootFeatureCurveSet(
                ReadCurve(reader), ReadCurve(reader), ReadCurve(reader), ReadCurve(reader), ReadCurve(reader),
                ReadPredictedStepCurveSet(reader));

        static void WritePredictedStepCurveSet(
            BinaryWriter writer,
            AnimationPredictedFootStepCurveSet value)
        {
            value.RequireValid();
            WriteCurve(writer, value.Confidence);
            WriteCurve(writer, value.TimeToLandingSeconds);
            WriteCurve(writer, value.EventPhase);
            WriteCurve(writer, value.ReleasePhase);
            WriteCurve(writer, value.LiftOffPhase);
            WriteCurve(writer, value.ApproachContactPhase);
            WriteCurve(writer, value.ActionStepDurationSeconds);
            WriteCurve(writer, value.EventOrdinal);
            WriteCurve(writer, value.OpposingLandingDelaySeconds);
            WriteCurve(writer, value.OpposingEventOrdinal);
            WriteCurve(writer, value.OpposingLandingCycleOffset);
            WriteCurve(writer, value.OpposingRootLocalLandingX);
            WriteCurve(writer, value.OpposingRootLocalLandingY);
            WriteCurve(writer, value.OpposingRootLocalLandingZ);
            for (int i = 0; i < AnimationPredictedFootStepCurveSet.RouteSampleCount; i++)
                WriteCurve(writer, value.GetRootLocalFootRouteX(i));
            for (int i = 0; i < AnimationPredictedFootStepCurveSet.RouteSampleCount; i++)
                WriteCurve(writer, value.GetRootLocalFootRouteY(i));
            for (int i = 0; i < AnimationPredictedFootStepCurveSet.RouteSampleCount; i++)
                WriteCurve(writer, value.GetRootLocalFootRouteZ(i));
            for (int i = 0; i < AnimationPredictedFootStepCurveSet.RouteSampleCount; i++)
                WriteCurve(writer, value.GetRootLocalAnkleRouteX(i));
            for (int i = 0; i < AnimationPredictedFootStepCurveSet.RouteSampleCount; i++)
                WriteCurve(writer, value.GetRootLocalAnkleRouteY(i));
            for (int i = 0; i < AnimationPredictedFootStepCurveSet.RouteSampleCount; i++)
                WriteCurve(writer, value.GetRootLocalAnkleRouteZ(i));
            for (int i = 0; i < AnimationPredictedFootStepCurveSet.RouteSampleCount; i++)
                WriteCurve(writer, value.GetRootLocalHipRouteX(i));
            for (int i = 0; i < AnimationPredictedFootStepCurveSet.RouteSampleCount; i++)
                WriteCurve(writer, value.GetRootLocalHipRouteY(i));
            for (int i = 0; i < AnimationPredictedFootStepCurveSet.RouteSampleCount; i++)
                WriteCurve(writer, value.GetRootLocalHipRouteZ(i));
            for (int i = 0; i < AnimationPredictedFootStepCurveSet.RouteSampleCount; i++)
                WriteCurve(writer, value.GetAuthoredFootPlanarRouteX(i));
            for (int i = 0; i < AnimationPredictedFootStepCurveSet.RouteSampleCount; i++)
                WriteCurve(writer, value.GetAuthoredFootPlanarRouteZ(i));
            for (int i = 0; i < AnimationPredictedFootStepCurveSet.RouteSampleCount; i++)
                WriteCurve(writer, value.GetAnimationClearanceHeight(i));
        }

        static AnimationPredictedFootStepCurveSet ReadPredictedStepCurveSet(BinaryReader reader)
        {
            AnimationCurve confidence = ReadCurve(reader);
            AnimationCurve timeToLanding = ReadCurve(reader);
            AnimationCurve eventPhase = ReadCurve(reader);
            AnimationCurve releasePhase = ReadCurve(reader);
            AnimationCurve liftOffPhase = ReadCurve(reader);
            AnimationCurve approachContactPhase = ReadCurve(reader);
            AnimationCurve actionStepDurationSeconds = ReadCurve(reader);
            AnimationCurve eventOrdinal = ReadCurve(reader);
            AnimationCurve opposingLandingDelaySeconds = ReadCurve(reader);
            AnimationCurve opposingEventOrdinal = ReadCurve(reader);
            AnimationCurve opposingLandingCycleOffset = ReadCurve(reader);
            AnimationCurve opposingRootLocalLandingX = ReadCurve(reader);
            AnimationCurve opposingRootLocalLandingY = ReadCurve(reader);
            AnimationCurve opposingRootLocalLandingZ = ReadCurve(reader);
            AnimationCurve[] routeX = ReadRouteCurves(reader);
            AnimationCurve[] routeY = ReadRouteCurves(reader);
            AnimationCurve[] routeZ = ReadRouteCurves(reader);
            AnimationCurve[] ankleRouteX = ReadRouteCurves(reader);
            AnimationCurve[] ankleRouteY = ReadRouteCurves(reader);
            AnimationCurve[] ankleRouteZ = ReadRouteCurves(reader);
            AnimationCurve[] hipRouteX = ReadRouteCurves(reader);
            AnimationCurve[] hipRouteY = ReadRouteCurves(reader);
            AnimationCurve[] hipRouteZ = ReadRouteCurves(reader);
            AnimationCurve[] authoredFootPlanarX = ReadRouteCurves(reader);
            AnimationCurve[] authoredFootPlanarZ = ReadRouteCurves(reader);
            AnimationCurve[] animationClearanceHeight = ReadRouteCurves(reader);
            return new AnimationPredictedFootStepCurveSet(
                confidence,
                timeToLanding,
                eventPhase,
                releasePhase,
                liftOffPhase,
                approachContactPhase,
                actionStepDurationSeconds,
                eventOrdinal,
                opposingLandingDelaySeconds,
                opposingEventOrdinal,
                opposingLandingCycleOffset,
                opposingRootLocalLandingX,
                opposingRootLocalLandingY,
                opposingRootLocalLandingZ,
                routeX,
                routeY,
                routeZ,
                ankleRouteX,
                ankleRouteY,
                ankleRouteZ,
                hipRouteX,
                hipRouteY,
                hipRouteZ,
                authoredFootPlanarX,
                authoredFootPlanarZ,
                animationClearanceHeight);
        }

        static AnimationCurve[] ReadRouteCurves(BinaryReader reader)
        {
            var result = new AnimationCurve[AnimationPredictedFootStepCurveSet.RouteSampleCount];
            for (int i = 0; i < result.Length; i++)
                result[i] = ReadCurve(reader);
            return result;
        }

        static void WriteCurve(BinaryWriter writer, AnimationCurve curve)
        {
            if (curve == null || curve.length <= 0 || curve.length > MaximumKeysPerCurve)
                throw new InvalidDataException("Animation Foot Analysis curve key count is invalid.");
            writer.Write((int)curve.preWrapMode);
            writer.Write((int)curve.postWrapMode);
            writer.Write(curve.length);
            Keyframe[] keys = curve.keys;
            float previous = float.NegativeInfinity;
            for (int i = 0; i < keys.Length; i++)
            {
                Keyframe key = keys[i];
                RequireFinite(key.time, "key time");
                RequireFinite(key.value, "key value");
                RequireCurveTangent(key.inTangent, "key in tangent");
                RequireCurveTangent(key.outTangent, "key out tangent");
                RequireFinite(key.inWeight, "key in weight");
                RequireFinite(key.outWeight, "key out weight");
                if (key.time <= previous || !Enum.IsDefined(typeof(WeightedMode), key.weightedMode))
                    throw new InvalidDataException($"Animation Foot Analysis curve key #{i} is unordered or has invalid weighting.");
                previous = key.time;
                writer.Write(key.time);
                writer.Write(key.value);
                writer.Write(key.inTangent);
                writer.Write(key.outTangent);
                writer.Write(key.inWeight);
                writer.Write(key.outWeight);
                writer.Write((int)key.weightedMode);
            }
        }

        static AnimationCurve ReadCurve(BinaryReader reader)
        {
            WrapMode pre = ReadWrapMode(reader.ReadInt32());
            WrapMode post = ReadWrapMode(reader.ReadInt32());
            int count = reader.ReadInt32();
            if (count <= 0 || count > MaximumKeysPerCurve)
                throw new InvalidDataException("Animation Foot Analysis curve key count is invalid.");
            var keys = new Keyframe[count];
            float previous = float.NegativeInfinity;
            for (int i = 0; i < count; i++)
            {
                float time = ReadFinite(reader, "key time");
                float value = ReadFinite(reader, "key value");
                float inTangent = ReadCurveTangent(reader, "key in tangent");
                float outTangent = ReadCurveTangent(reader, "key out tangent");
                float inWeight = ReadFinite(reader, "key in weight");
                float outWeight = ReadFinite(reader, "key out weight");
                int weightedModeValue = reader.ReadInt32();
                if (time <= previous || !Enum.IsDefined(typeof(WeightedMode), weightedModeValue))
                    throw new InvalidDataException($"Animation Foot Analysis curve key #{i} is unordered or has invalid weighting.");
                previous = time;
                keys[i] = new Keyframe(time, value, inTangent, outTangent, inWeight, outWeight)
                {
                    weightedMode = (WeightedMode)weightedModeValue
                };
            }
            return new AnimationCurve(keys) { preWrapMode = pre, postWrapMode = post };
        }

        static WrapMode ReadWrapMode(int value)
        {
            if (!Enum.IsDefined(typeof(WrapMode), value))
                throw new InvalidDataException($"Animation Foot Analysis wrap mode '{value}' is invalid.");
            return (WrapMode)value;
        }

        static float ReadFinite(BinaryReader reader, string field)
        {
            float value = reader.ReadSingle();
            RequireFinite(value, field);
            return value;
        }

        static float ReadCurveTangent(BinaryReader reader, string field)
        {
            float value = reader.ReadSingle();
            RequireCurveTangent(value, field);
            return value;
        }

        static void RequireCurveTangent(float value, string field)
        {
            if (!float.IsFinite(value) && !float.IsPositiveInfinity(value))
                throw new InvalidDataException($"Animation Foot Analysis {field} is invalid.");
        }

        static void RequireFinite(float value, string field)
        {
            if (!float.IsFinite(value))
                throw new InvalidDataException($"Animation Foot Analysis {field} is not finite.");
        }

        static void WriteString(BinaryWriter writer, string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
            if (bytes.Length > MaximumStringBytes)
                throw new InvalidDataException("Animation Foot Analysis string is too long.");
            writer.Write(bytes.Length);
            writer.Write(bytes);
        }

        static string ReadString(BinaryReader reader)
        {
            int length = reader.ReadInt32();
            if (length < 0 || length > MaximumStringBytes || length > reader.BaseStream.Length - reader.BaseStream.Position)
                throw new InvalidDataException("Animation Foot Analysis string length is invalid.");
            byte[] bytes = reader.ReadBytes(length);
            if (bytes.Length != length)
                throw new EndOfStreamException("Animation Foot Analysis string is truncated.");
            return new UTF8Encoding(false, true).GetString(bytes);
        }

        static StableHash Hash(byte[] bytes)
        {
            using SHA256 sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(bytes);
            var builder = new StringBuilder(hash.Length * 2);
            for (int i = 0; i < hash.Length; i++)
                builder.Append(hash[i].ToString("x2"));
            return new StableHash(builder.ToString());
        }
    }
}
