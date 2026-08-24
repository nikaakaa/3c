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
        const int MaximumPhaseValidationSamples = 1024 * 1024;

        public static byte[] Write(
            AnimationFootAnalysisArtifactIdentity identity,
            AnimationFootFeaturePair features,
            AnimationFootPhaseValidationDescriptor phaseValidation,
            AnimationFootMotionDataDescriptor motionData,
            out StableHash contentHash)
        {
            if (identity == null)
                throw new ArgumentNullException(nameof(identity));
            if (!features.IsValid)
                throw new ArgumentException("Animation Foot Analysis features are invalid.", nameof(features));
            if (phaseValidation == null)
                throw new ArgumentNullException(nameof(phaseValidation));
            phaseValidation.RequireValid();
            if (motionData == null)
                throw new ArgumentNullException(nameof(motionData));
            byte[] payload;
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                WriteIdentity(writer, identity);
                WriteFeaturePair(writer, features);
                WritePhaseValidationDescriptor(writer, phaseValidation);
                WriteMotionData(writer, motionData);
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
            AnimationFootPhaseValidationDescriptor phaseValidation =
                ReadPhaseValidationDescriptor(payloadReader);
            AnimationFootMotionDataDescriptor motionData = ReadMotionData(payloadReader);
            if (payloadStream.Position != payloadStream.Length)
                throw new InvalidDataException("Animation Foot Analysis payload has trailing bytes.");
            return new AnimationFootAnalysisArtifact(
                identity,
                features,
                phaseValidation,
                motionData,
                actualHash);
        }

        static void WriteIdentity(BinaryWriter writer, AnimationFootAnalysisArtifactIdentity value)
        {
            writer.Write(value.FormatVersion);
            WriteString(writer, value.ClipAssetGuid);
            WriteString(writer, value.ClipAnalysisInputHash);
            WriteString(writer, value.MotionReferenceClipAssetGuid);
            WriteString(writer, value.MotionReferenceClipAnalysisInputHash);
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

        static void WritePhaseValidationDescriptor(
            BinaryWriter writer,
            AnimationFootPhaseValidationDescriptor value)
        {
            value.RequireValid();
            writer.Write(value.SampleRate);
            writer.Write(value.DurationSeconds);
            WritePhaseValidationFoot(writer, value.Left);
            WritePhaseValidationFoot(writer, value.Right);
        }

        static AnimationFootPhaseValidationDescriptor ReadPhaseValidationDescriptor(
            BinaryReader reader) =>
            new AnimationFootPhaseValidationDescriptor(
                ReadFinite(reader, "Phase validation sample rate"),
                ReadFinite(reader, "Phase validation duration"),
                ReadPhaseValidationFoot(reader),
                ReadPhaseValidationFoot(reader));

        static void WritePhaseValidationFoot(
            BinaryWriter writer,
            AnimationFootPhaseValidationFootDescriptor value)
        {
            value.RequireValid();
            writer.Write(value.Samples.Count);
            for (int i = 0; i < value.Samples.Count; i++)
            {
                AnimationFootPhaseValidationSample sample = value.Samples[i];
                sample.RequireValid();
                writer.Write(sample.NormalizedTime);
                writer.Write(sample.RootLocalSolePlanarPosition.x);
                writer.Write(sample.RootLocalSolePlanarPosition.y);
                writer.Write(sample.CalibratedSoleHeight);
                writer.Write(sample.SoleLocalVelocity.x);
                writer.Write(sample.SoleLocalVelocity.y);
                writer.Write(sample.SoleLocalVelocity.z);
                writer.Write(sample.PlantConfidence);
                writer.Write(sample.LandingOnset);
            }
        }

        static AnimationFootPhaseValidationFootDescriptor ReadPhaseValidationFoot(
            BinaryReader reader)
        {
            int count = reader.ReadInt32();
            if (count < 3 || count > MaximumPhaseValidationSamples)
                throw new InvalidDataException("Foot Phase validation sample count is invalid.");
            var samples = new AnimationFootPhaseValidationSample[count];
            for (int i = 0; i < count; i++)
            {
                samples[i] = new AnimationFootPhaseValidationSample(
                    ReadFinite(reader, "Phase validation normalized time"),
                    new Vector2(
                        ReadFinite(reader, "Phase validation planar x"),
                        ReadFinite(reader, "Phase validation planar z")),
                    ReadFinite(reader, "Phase validation height"),
                    new Vector3(
                        ReadFinite(reader, "Phase validation velocity x"),
                        ReadFinite(reader, "Phase validation velocity y"),
                        ReadFinite(reader, "Phase validation velocity z")),
                    ReadFinite(reader, "Phase validation plant confidence"),
                    reader.ReadBoolean());
            }
            return new AnimationFootPhaseValidationFootDescriptor(samples);
        }

        static void WriteMotionData(BinaryWriter writer, AnimationFootMotionDataDescriptor value)
        {
            AnimationFootMotionRawPage raw = value.Raw;
            writer.Write(raw.SampleRate);
            writer.Write(raw.DurationSeconds);
            writer.Write(raw.GroundReferenceHeight);
            writer.Write(raw.RootSamples.Count);
            for (int i = 0; i < raw.RootSamples.Count; i++)
            {
                AnimationFootMotionRootSample sample = raw.RootSamples[i];
                writer.Write(sample.TimeSeconds);
                WriteVector(writer, sample.Position);
                WriteQuaternion(writer, sample.Rotation);
            }
            WriteMotionRawFoot(writer, raw.Left);
            WriteMotionRawFoot(writer, raw.Right);
            WriteMotionFoot(writer, value.Left);
            WriteMotionFoot(writer, value.Right);
        }

        static AnimationFootMotionDataDescriptor ReadMotionData(BinaryReader reader)
        {
            float sampleRate = ReadFinite(reader, "Foot Motion sample rate");
            float duration = ReadFinite(reader, "Foot Motion duration");
            float ground = ReadFinite(reader, "Foot Motion ground reference");
            int count = ReadSampleCount(reader, "Foot Motion root");
            var roots = new AnimationFootMotionRootSample[count];
            for (int i = 0; i < count; i++)
            {
                roots[i] = new AnimationFootMotionRootSample(
                    ReadFinite(reader, "Foot Motion root time"),
                    ReadVector(reader, "Foot Motion root position"),
                    ReadQuaternion(reader, "Foot Motion root rotation"));
            }
            AnimationFootMotionRawFootPage leftRaw = ReadMotionRawFoot(reader);
            AnimationFootMotionRawFootPage rightRaw = ReadMotionRawFoot(reader);
            return new AnimationFootMotionDataDescriptor(
                new AnimationFootMotionRawPage(sampleRate, duration, ground, roots, leftRaw, rightRaw),
                ReadMotionFoot(reader),
                ReadMotionFoot(reader));
        }

        static void WriteMotionRawFoot(BinaryWriter writer, AnimationFootMotionRawFootPage value)
        {
            writer.Write(value.RigLegLength);
            writer.Write(value.Samples.Count);
            for (int i = 0; i < value.Samples.Count; i++)
            {
                AnimationFootMotionRawSample sample = value.Samples[i];
                writer.Write(sample.TimeSeconds);
                WriteMotionPose(writer, sample.Hip);
                WriteMotionPose(writer, sample.Knee);
                WriteMotionPose(writer, sample.Ankle);
                WriteMotionPose(writer, sample.Heel);
                WriteMotionPose(writer, sample.Toe);
                WriteMotionPose(writer, sample.Sole);
                WriteVector(writer, sample.SoleVelocity);
                WriteVector(writer, sample.ToeVelocity);
                WriteVector(writer, sample.SoleAngularVelocity);
            }
        }

        static AnimationFootMotionRawFootPage ReadMotionRawFoot(BinaryReader reader)
        {
            float legLength = ReadFinite(reader, "Foot Motion leg length");
            int count = ReadSampleCount(reader, "Foot Motion raw foot");
            var samples = new AnimationFootMotionRawSample[count];
            for (int i = 0; i < count; i++)
            {
                samples[i] = new AnimationFootMotionRawSample(
                    ReadFinite(reader, "Foot Motion raw time"),
                    ReadMotionPose(reader),
                    ReadMotionPose(reader),
                    ReadMotionPose(reader),
                    ReadMotionPose(reader),
                    ReadMotionPose(reader),
                    ReadMotionPose(reader),
                    ReadVector(reader, "Foot Motion sole velocity"),
                    ReadVector(reader, "Foot Motion toe velocity"),
                    ReadVector(reader, "Foot Motion angular velocity"));
            }
            return new AnimationFootMotionRawFootPage(legLength, samples);
        }

        static void WriteMotionPose(BinaryWriter writer, AnimationFootMotionPose value)
        {
            WriteVector(writer, value.RootLocalPosition);
            WriteQuaternion(writer, value.RootLocalRotation);
            WriteVector(writer, value.MotionPosition);
            WriteQuaternion(writer, value.MotionRotation);
        }

        static AnimationFootMotionPose ReadMotionPose(BinaryReader reader) =>
            new AnimationFootMotionPose(
                ReadVector(reader, "Foot Motion root-local position"),
                ReadQuaternion(reader, "Foot Motion root-local rotation"),
                ReadVector(reader, "Foot Motion motion position"),
                ReadQuaternion(reader, "Foot Motion motion rotation"));

        static void WriteMotionFoot(BinaryWriter writer, AnimationFootMotionFootPage value)
        {
            writer.Write(value.Events.Count);
            for (int i = 0; i < value.Events.Count; i++)
            {
                AnimationFootMotionEvent footEvent = value.Events[i];
                writer.Write((byte)footEvent.Kind);
                writer.Write(footEvent.SampleIndex);
                writer.Write(footEvent.Ordinal);
                writer.Write(footEvent.CycleOffset);
                WriteVector(writer, footEvent.RootLocalSolePosition);
                WriteVector(writer, footEvent.MotionSolePosition);
                WriteQuaternion(writer, footEvent.SoleRotation);
            }
            writer.Write(value.Diagnostics.Count);
            for (int i = 0; i < value.Diagnostics.Count; i++)
            {
                writer.Write((byte)value.Diagnostics[i].Code);
                writer.Write(value.Diagnostics[i].SampleIndex);
            }
            writer.Write(value.Samples.Count);
            for (int i = 0; i < value.Samples.Count; i++)
            {
                AnimationFootMotionDerivedSample sample = value.Samples[i];
                writer.Write(sample.TimeSeconds);
                writer.Write(sample.Step.Available);
                writer.Write(sample.Step.LandingOrdinal);
                writer.Write(sample.Step.TimeSeconds);
                writer.Write(sample.Step.Distance);
                writer.Write(sample.Step.PathProgress);
                writer.Write(sample.Step.BaselineHeight);
                writer.Write(sample.Step.AnimationHeight);
                writer.Write(sample.Step.HeightAbovePath);
                writer.Write(sample.Filter.ToeHeight);
                writer.Write(sample.Filter.ToeSpeed);
                writer.Write(sample.Filter.PositionError);
                writer.Write(sample.Filter.RotationError);
                writer.Write(sample.Filter.Contact);
                writer.Write((byte)sample.Constraint.LockMode);
                writer.Write(sample.Constraint.LockWeight);
                writer.Write(sample.Constraint.SupportCandidate);
                writer.Write(sample.Constraint.Support);
            }
            WriteString(writer, value.Diagnostic);
        }

        static AnimationFootMotionFootPage ReadMotionFoot(BinaryReader reader)
        {
            int eventCount = reader.ReadInt32();
            if (eventCount < 0 || eventCount > MaximumPhaseValidationSamples)
                throw new InvalidDataException("Foot Motion event count is invalid.");
            var events = new AnimationFootMotionEvent[eventCount];
            for (int i = 0; i < eventCount; i++)
            {
                byte kind = reader.ReadByte();
                if (!Enum.IsDefined(typeof(AnimationFootMotionEventKind), kind))
                    throw new InvalidDataException("Foot Motion event kind is invalid.");
                events[i] = new AnimationFootMotionEvent(
                    (AnimationFootMotionEventKind)kind,
                    reader.ReadInt32(),
                    reader.ReadInt32(),
                    reader.ReadInt32(),
                    ReadVector(reader, "Foot Motion event root-local position"),
                    ReadVector(reader, "Foot Motion event motion position"),
                    ReadQuaternion(reader, "Foot Motion event rotation"));
            }
            int diagnosticCount = reader.ReadInt32();
            if (diagnosticCount < 0 || diagnosticCount > MaximumPhaseValidationSamples)
                throw new InvalidDataException("Foot Motion diagnostic count is invalid.");
            var diagnostics = new AnimationFootMotionDiagnostic[diagnosticCount];
            for (int i = 0; i < diagnosticCount; i++)
            {
                byte code = reader.ReadByte();
                if (!Enum.IsDefined(typeof(AnimationFootMotionDiagnosticCode), code))
                    throw new InvalidDataException("Foot Motion diagnostic code is invalid.");
                diagnostics[i] = new AnimationFootMotionDiagnostic(
                    (AnimationFootMotionDiagnosticCode)code,
                    reader.ReadInt32());
            }
            int sampleCount = ReadSampleCount(reader, "Foot Motion derived foot");
            var samples = new AnimationFootMotionDerivedSample[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float time = ReadFinite(reader, "Foot Motion derived time");
                var step = new AnimationFootMotionStepEvidence(
                    reader.ReadBoolean(),
                    reader.ReadInt32(),
                    ReadFinite(reader, "Foot Motion step time"),
                    ReadFinite(reader, "Foot Motion step distance"),
                    ReadFinite(reader, "Foot Motion path progress"),
                    ReadFinite(reader, "Foot Motion baseline height"),
                    ReadFinite(reader, "Foot Motion animation height"),
                    ReadFinite(reader, "Foot Motion height above path"));
                var filter = new AnimationFootMotionFilterEvidence(
                    ReadFinite(reader, "Foot Motion toe height"),
                    ReadFinite(reader, "Foot Motion toe speed"),
                    ReadFinite(reader, "Foot Motion position error"),
                    ReadFinite(reader, "Foot Motion rotation error"),
                    ReadFinite(reader, "Foot Motion contact"));
                byte mode = reader.ReadByte();
                if (mode > 2)
                    throw new InvalidDataException("Foot Motion lock mode is invalid.");
                var constraint = new AnimationFootMotionConstraintEvidence(
                    (AnimationFootLockMode)mode,
                    ReadFinite(reader, "Foot Motion lock weight"),
                    ReadFinite(reader, "Foot Motion support candidate"),
                    ReadFinite(reader, "Foot Motion support"));
                samples[i] = new AnimationFootMotionDerivedSample(time, step, filter, constraint);
            }
            return new AnimationFootMotionFootPage(events, diagnostics, samples, ReadString(reader));
        }

        static int ReadSampleCount(BinaryReader reader, string field)
        {
            int count = reader.ReadInt32();
            if (count < 3 || count > MaximumPhaseValidationSamples)
                throw new InvalidDataException($"{field} sample count is invalid.");
            return count;
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
            WritePredictedStepCurveSet(writer, value.IncomingPredictedStep);
        }

        static AnimationFootFeatureCurveSet ReadCurveSet(BinaryReader reader) =>
            new AnimationFootFeatureCurveSet(
                ReadCurve(reader), ReadCurve(reader), ReadCurve(reader), ReadCurve(reader), ReadCurve(reader),
                ReadPredictedStepCurveSet(reader),
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
            WriteCurve(writer, value.SourceLandingCycleOffset);
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
            WriteBiomechanicalStepCurveSet(writer, value.BiomechanicalStep);
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
            AnimationCurve sourceLandingCycleOffset = ReadCurve(reader);
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
            AnimationFootBiomechanicalStepCurveSet biomechanical =
                ReadBiomechanicalStepCurveSet(reader);
            return new AnimationPredictedFootStepCurveSet(
                confidence,
                timeToLanding,
                eventPhase,
                releasePhase,
                liftOffPhase,
                approachContactPhase,
                actionStepDurationSeconds,
                eventOrdinal,
                sourceLandingCycleOffset,
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
                animationClearanceHeight,
                biomechanical);
        }

        static void WriteBiomechanicalStepCurveSet(
            BinaryWriter writer,
            AnimationFootBiomechanicalStepCurveSet value)
        {
            value.RequireValid();
            WriteCurve(writer, value.LandingPhase);
            WriteCurve(writer, value.OpposingRootLocalSoleRotationX);
            WriteCurve(writer, value.OpposingRootLocalSoleRotationY);
            WriteCurve(writer, value.OpposingRootLocalSoleRotationZ);
            WriteCurve(writer, value.OpposingRootLocalSoleRotationW);
            for (int axis = 0; axis < 3; axis++)
                WriteBiomechanicalRoute(writer, index => value.GetRootLocalHeelRoute(axis, index));
            for (int axis = 0; axis < 3; axis++)
                WriteBiomechanicalRoute(writer, index => value.GetRootLocalToeRoute(axis, index));
            for (int axis = 0; axis < 3; axis++)
                WriteBiomechanicalRoute(writer, index => value.GetRootLocalKneeRoute(axis, index));
            for (int axis = 0; axis < 4; axis++)
                WriteBiomechanicalRoute(writer, index => value.GetRootLocalSoleRotationRoute(axis, index));
            for (int axis = 0; axis < 4; axis++)
                WriteBiomechanicalRoute(writer, index => value.GetRootLocalAnkleRotationRoute(axis, index));
            for (int axis = 0; axis < 3; axis++)
                WriteBiomechanicalRoute(writer, index => value.GetSupportKneeBendPlane(axis, index));
            for (int axis = 0; axis < 3; axis++)
                WriteBiomechanicalRoute(writer, index => value.GetSupportFootPivotPosition(axis, index));
            WriteBiomechanicalRoute(writer, value.GetConstraintWeight);
            WriteBiomechanicalRoute(writer, value.GetSupportWeight);
            WriteBiomechanicalRoute(writer, value.GetSupportLegLength);
            WriteBiomechanicalRoute(writer, value.GetSupportLegCompressionReserve);
            WriteBiomechanicalRoute(writer, value.GetSupportFootPivotWeight);
        }

        static AnimationFootBiomechanicalStepCurveSet ReadBiomechanicalStepCurveSet(
            BinaryReader reader)
        {
            AnimationCurve landingPhase = ReadCurve(reader);
            AnimationCurve opposingX = ReadCurve(reader);
            AnimationCurve opposingY = ReadCurve(reader);
            AnimationCurve opposingZ = ReadCurve(reader);
            AnimationCurve opposingW = ReadCurve(reader);
            var vectorRoutes = new AnimationCurve[23][];
            for (int i = 0; i < vectorRoutes.Length; i++)
                vectorRoutes[i] = ReadRouteCurves(reader);
            return new AnimationFootBiomechanicalStepCurveSet(
                landingPhase,
                opposingX,
                opposingY,
                opposingZ,
                opposingW,
                vectorRoutes,
                ReadRouteCurves(reader),
                ReadRouteCurves(reader),
                ReadRouteCurves(reader),
                ReadRouteCurves(reader),
                ReadRouteCurves(reader));
        }

        static void WriteBiomechanicalRoute(
            BinaryWriter writer,
            Func<int, AnimationCurve> resolve)
        {
            for (int i = 0; i < AnimationPredictedFootStepCurveSet.RouteSampleCount; i++)
                WriteCurve(writer, resolve(i));
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

        static void WriteVector(BinaryWriter writer, Vector3 value)
        {
            RequireFinite(value.x, "vector x");
            RequireFinite(value.y, "vector y");
            RequireFinite(value.z, "vector z");
            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.z);
        }

        static Vector3 ReadVector(BinaryReader reader, string field) =>
            new Vector3(
                ReadFinite(reader, field + " x"),
                ReadFinite(reader, field + " y"),
                ReadFinite(reader, field + " z"));

        static void WriteQuaternion(BinaryWriter writer, Quaternion value)
        {
            RequireFinite(value.x, "quaternion x");
            RequireFinite(value.y, "quaternion y");
            RequireFinite(value.z, "quaternion z");
            RequireFinite(value.w, "quaternion w");
            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.z);
            writer.Write(value.w);
        }

        static Quaternion ReadQuaternion(BinaryReader reader, string field) =>
            new Quaternion(
                ReadFinite(reader, field + " x"),
                ReadFinite(reader, field + " y"),
                ReadFinite(reader, field + " z"),
                ReadFinite(reader, field + " w"));

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
