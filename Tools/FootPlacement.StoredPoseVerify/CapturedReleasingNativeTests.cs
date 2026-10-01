/*
目的：重现 Run 交给全身 Action 时，真实骨骼混合与脚部贡献选择的差异。
输入：2035～2046 连续录制的 clip 时间、全身 alpha 和贡献身份，以及正式发布的 ACL 资源与脚曲线。
链路：ACL 解码 → 正式虚拟骨推导 → Native Slot EvaluateFrame → 历史/当前完整 Action Slot → 正式 FootMotion 选择。
预期：两版骨骼姿势保持一致，历史贡献重现录制，当前在实际 Action 占优时采样 Action 曲线。
边界：BlendStack 计划权重为录制边界；本段不执行 Foot、物理查询或 FBBIK。未测试 Burst/Unity 调度与 FootFeatures 混合。
说明：docs/diagnostics/foot-placement/ik-tests/releasing-action-native.html。
*/
extern alias EditorAnimation;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.BlendStack;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public static class CapturedReleasingNativeTests
    {
        const string AssetsRoot = "Assets/Configs/Character/Corin/Pipeline/Presentation/";
        const string ResourcePath = "Assets/AssetRaw/Product/Gameplay/ACL/c7a7c1e3f7e64d81b5a04a90cbeb8d4e/acl-4ce133684f97a8812d744f6ee151d91c9eb234ee67e7b9d26b4f03fc4065565f.asset";

        public static JObject ExportComponentPoses(string inputPath, string outputPath)
        {
            JObject fixture = JObject.Parse(File.ReadAllText(inputPath, Encoding.UTF8));
            var rig = new CharacterAnimationRigPayload(AssetDatabase.LoadAssetAtPath<CharacterAnimationRigDefinition>(AssetsRoot + "Rig/CorinAnimationRigDefinition.asset"));
            var set = AssetDatabase.LoadAssetAtPath<CharacterPoseNativeDomainResourceSet>(AssetsRoot + "Profiles/CorinPoseNativeDomainResourceSet.asset");
            var resource = AssetDatabase.LoadAssetAtPath<CharacterAclAnimationResource>(ResourcePath);
            var runPlan = set.SourcePlans.Single(x => x.GroupClipIndex == 10);
            var actionPlan = set.ActionSourcePlans.Single(x => x.GroupClipIndex == 1);
            var captured = new[] { fixture["seed"] }.Concat((JArray)fixture["frames"]).ToArray();
            var frames = captured.Select(x => new Frame(x, (JObject)fixture["columns"], (JObject)fixture["sourceIds"], set, runPlan, actionPlan)).ToArray();
            var rows = new JArray();
            using (var geometry = new Geometry(rig))
            using (var runDecode = Decoder(resource, 10))
            using (var actionDecode = Decoder(resource, 1))
            using (var decoded = new NativeArray<CharacterAclNativeTransformSample>(rig.PhysicalBoneCount, Allocator.Persistent))
            using (var fullComponents = new NativeArray<CharacterComponentBonePose>(rig.PoseBoneCount, Allocator.Persistent))
            using (var run = new NativeSlot(rig))
            using (var action = new NativeSlot(rig))
            using (var output = new CharacterPoseNativeNodePoseBuffer(0, rig.PoseBoneCount, 1, 3))
            {
                var slot = new CurrentActionSlot();
                var components = fullComponents;
                ulong actionPrevious = 0;
                for (int i = 0; i < frames.Length; i++)
                {
                    Frame f = frames[i];
                    ulong completion = (ulong)i + 1;
                    using (var runPose = geometry.Decode(runDecode, resource.GetGroupManifest(10), f.RunTime, decoded))
                    {
                        var source = run.Evaluate(runPose, in f.RunSample, 1f, f.RunContinuity, f.Delta, completion, i == 0 ? 0 : completion - 1);
                        var actionWrite = action.Output.RequireWriteBinding(completion);
                        if (f.HasAction)
                        {
                            using (var actionPose = geometry.Decode(actionDecode, resource.GetGroupManifest(1), f.ActionTime, decoded))
                                actionWrite = action.Evaluate(actionPose, in f.ActionSample, f.ActionWeight, f.ActionContinuity, f.Delta, completion, actionPrevious);
                            actionPrevious = completion;
                        }
                        var sourceRead = new CharacterPoseNativePoseReadBinding(in source);
                        var actionRead = new CharacterPoseNativePoseReadBinding(in actionWrite);
                        var write = output.RequireWriteBinding(completion);
                        slot.Evaluate(in sourceRead, in actionRead, in write, f.HasAction);
                        Assert.That(write.Availability[0], Is.EqualTo(AnimationPoseAvailability.Pose));
                        var poses = new JArray();
                        for (int bone = 0; bone < rig.PoseBoneCount; bone++)
                        {
                            Assert.That(CharacterPoseConstraintMath.TryCreateComponent(write.DenseLocalPoses[bone], rig.GetPoseParentIndex(bone), fullComponents, out var component), Is.True);
                            components[bone] = component;
                            poses.Add(new JArray(component.Position.x, component.Position.y, component.Position.z, component.Rotation.x, component.Rotation.y, component.Rotation.z, component.Rotation.w,
                                component.Scale.x, component.Scale.y, component.Scale.z));
                        }
                        rows.Add(new JObject { ["frame"] = f.Number, ["componentPoses"] = poses });
                    }
                }
            }
            var report = new JObject { ["status"] = "passed", ["scope"] = "正式ACL/Native/Action Slot得到2034预滚与12帧完整组件姿势；未执行IK；两版骨骼混合已经校准一致", ["rigRevision"] = rig.RigRevision, ["rows"] = rows };
            File.WriteAllText(outputPath, report.ToString(Formatting.None), new UTF8Encoding(false));
            return new JObject { ["status"] = "passed", ["frames"] = frames.Length };
        }

        public static JObject Run(string inputPath, string outputPath, string nativeCommit)
        {
            var report = new JObject { ["status"] = "running", ["rows"] = new JArray(),
                ["scope"] = "真实ACL骨骼、虚拟骨、Native Slot状态提交与完整Action Slot；计划alpha为录制边界；没有执行Foot、Physics、FBBIK或Burst调度",
                ["nativeJobCommit"] = nativeCommit, ["historicalActionSlotCommit"] = "2c757a422",
                ["editorCompilationFailed"] = EditorUtility.scriptCompilationFailed };
            try
            {
                Assert.That(EditorApplication.isPlaying || EditorApplication.isCompiling, Is.False);
                var editorAnimation = typeof(CharacterAnimationRigPayload).Assembly;
                var localAssembly = typeof(CapturedReleasingNativeTests).Assembly;
                var stateTypes = new JArray();
                foreach (string name in new[] { "AnimationSlotBlendStoredPoseNativeState", "AnimationSlotBlendHistoryNativeState", "AnimationSlotBlendScratchNativeState" })
                {
                    string fullName = "ThirdPersonCharacter.Pipeline.Animation.BlendStack." + name;
                    var local = localAssembly.GetType(fullName);
                    var loaded = editorAnimation.GetType(fullName);
                    stateTypes.Add(new JObject { ["type"] = fullName,
                        ["executedTypeSizeBytes"] = System.Runtime.InteropServices.Marshal.SizeOf(local),
                        ["executedModuleMvid"] = local.Module.ModuleVersionId.ToString(),
                        ["editorLoadedTypeSizeBytes"] = System.Runtime.InteropServices.Marshal.SizeOf(loaded),
                        ["editorLoadedModuleMvid"] = loaded.Module.ModuleVersionId.ToString() });
                }
                report["nativeStateBindings"] = stateTypes;
                JObject fixture = JObject.Parse(File.ReadAllText(inputPath, Encoding.UTF8));
                var rig = new CharacterAnimationRigPayload(AssetDatabase.LoadAssetAtPath<CharacterAnimationRigDefinition>(AssetsRoot + "Rig/CorinAnimationRigDefinition.asset"));
                var set = AssetDatabase.LoadAssetAtPath<CharacterPoseNativeDomainResourceSet>(AssetsRoot + "Profiles/CorinPoseNativeDomainResourceSet.asset");
                var resource = AssetDatabase.LoadAssetAtPath<CharacterAclAnimationResource>(ResourcePath);
                Assert.That(resource.MatchesDescriptor(set.ResourceDescriptors.Single(x => x.ResourceIndex == 0)), Is.True);
                var runPlan = set.SourcePlans.Single(x => x.GroupClipIndex == 10);
                var actionPlan = set.ActionSourcePlans.Single(x => x.GroupClipIndex == 1);
                report["resourcePath"] = ResourcePath;
                report["resourceHash"] = resource.Manifest.GroupContentHash;
                report["rigRevision"] = rig.RigRevision;
                report["boneCount"] = rig.PoseBoneCount;
                report["runSourceSampleIdentity"] = ((ulong)runPlan.ContentRevision.GetHashCode()).ToString();
                report["actionSourceSampleIdentity"] = ((ulong)actionPlan.FullDependencyHash.GetHashCode()).ToString();
                var frames = ((JArray)fixture["frames"]).Select(x => new Frame(x, (JObject)fixture["columns"], (JObject)fixture["sourceIds"], set, runPlan, actionPlan)).ToArray();
                using (var geometry = new Geometry(rig))
                using (var runDecode = Decoder(resource, 10))
                using (var actionDecode = Decoder(resource, 1))
                using (var decoded = new NativeArray<CharacterAclNativeTransformSample>(rig.PhysicalBoneCount, Allocator.Persistent))
                {
                    var poses = new NativeArray<AnimationLocalBonePose>[frames.Length * 2];
                    try
                    {
                        for (int i = 0; i < frames.Length; i++)
                        {
                            poses[i * 2] = geometry.Decode(runDecode, resource.GetGroupManifest(10), frames[i].RunTime, decoded);
                            if (frames[i].HasAction) poses[i * 2 + 1] = geometry.Decode(actionDecode, resource.GetGroupManifest(1), frames[i].ActionTime, decoded);
                        }
                        var old = new Result[frames.Length];
                        var current = new Result[frames.Length];
                        using (var run = new NativeSlot(rig))
                        using (var action = new NativeSlot(rig))
                        using (var oldOutput = new CharacterPoseNativeNodePoseBuffer(0, rig.PoseBoneCount, 1, 3))
                        using (var newOutput = new CharacterPoseNativeNodePoseBuffer(0, rig.PoseBoneCount, 1, 3))
                        {
                            var oldSlot = new HistoricalActionSlot();
                            var newSlot = new CurrentActionSlot();
                            Execute(frames, poses, rig, geometry, run, action, oldOutput, newOutput, oldSlot, newSlot, old, current, 4096);
                            Execute(frames, poses, rig, geometry, run, action, oldOutput, newOutput, oldSlot, newSlot, old, current, 8192);
                            long before = GC.GetAllocatedBytesForCurrentThread();
                            Execute(frames, poses, rig, geometry, run, action, oldOutput, newOutput, oldSlot, newSlot, old, current, 12288);
                            report["allocatedBytesInWarmedSequence"] = GC.GetAllocatedBytesForCurrentThread() - before;
                        }
                        for (int i = 0; i < frames.Length; i++)
                        {
                            var f = frames[i];
                            var a = old[i];
                            var b = current[i];
                            ((JArray)report["rows"]).Add(new JObject { ["frame"] = f.Number, ["dt"] = f.Delta, ["actionAlpha"] = f.ActionWeight,
                                ["historicalWeight"] = a.SelectedWeight, ["currentWeight"] = b.SelectedWeight,
                                ["historicalActionWeight"] = a.ActionWeight, ["currentActionWeight"] = b.ActionWeight,
                                ["historicalActionDenseWeight"] = a.ActionDenseWeight, ["currentActionDenseWeight"] = b.ActionDenseWeight,
                                ["historicalSource"] = a.SelectedAction ? actionPlan.ClipIdentity : runPlan.DisplayName,
                                ["currentSource"] = b.SelectedAction ? actionPlan.ClipIdentity : runPlan.DisplayName,
                                ["historicalSample"] = Sample(a.Sample.Right), ["currentSample"] = Sample(b.Sample.Right),
                                ["currentLeftSample"] = Sample(b.Sample.Left), ["currentRightSample"] = Sample(b.Sample.Right),
                                ["historicalAnkle"] = Vector(a.Ankle), ["currentAnkle"] = Vector(b.Ankle),
                                ["recordedAnkle"] = Vector(f.RecordedAnkle), ["anklePositionError"] = Vector3.Distance(a.Ankle, f.RecordedAnkle),
                                ["ankleRotationErrorDegrees"] = Quaternion.Angle(a.Rotation, f.RecordedRotation),
                                ["oldNewAnkleDistance"] = Vector3.Distance(a.Ankle, b.Ankle), ["oldNewAnkleAngle"] = Quaternion.Angle(a.Rotation, b.Rotation),
                                ["maxOldNewLocalPositionError"] = b.MaxPoseError, ["maxOldNewLocalRotationError"] = b.MaxRotationError,
                                ["maxOldNewLocalScaleError"] = b.MaxScaleError, ["historicalValid"] = a.Valid, ["currentValid"] = b.Valid });
                        }
                        Assert.That((long)report["allocatedBytesInWarmedSequence"], Is.Zero);
                        for (int i = 0; i < frames.Length; i++)
                        {
                            Assert.That(old[i].Valid && current[i].Valid, Is.True, "Native Slot实际发布必须完成");
                            Assert.That(old[i].SelectedWeight, Is.EqualTo(frames[i].RecordedSelectedWeight).Within(.000001f));
                            Assert.That(current[i].MaxPoseError, Is.LessThan(.00001f));
                            Assert.That(current[i].MaxRotationError, Is.Zero);
                            Assert.That(current[i].MaxScaleError, Is.Zero);
                            Assert.That(Vector3.Distance(old[i].Ankle, frames[i].RecordedAnkle), Is.LessThan(.0002f), "解码/混合原踝须重现录制；未校准不可接Foot");
                            Assert.That(Quaternion.Angle(old[i].Rotation, frames[i].RecordedRotation), Is.LessThan(.1f));
                        }
                        Assert.That(current[Array.FindIndex(frames, x => x.Number == 2041)].SelectedAction, Is.True);
                        report["status"] = "passed";
                    }
                    finally { foreach (var pose in poses) if (pose.IsCreated) pose.Dispose(); }
                }
            }
            catch (Exception error) { report["status"] = "failed"; report["failure"] = error.ToString(); }
            File.WriteAllText(outputPath, report.ToString(Formatting.None), new UTF8Encoding(false));
            return new JObject { ["status"] = report["status"], ["rows"] = ((JArray)report["rows"]).Count, ["failure"] = report["failure"] };
        }

        static void Execute(Frame[] frames, NativeArray<AnimationLocalBonePose>[] poses, CharacterAnimationRigPayload rig, Geometry geometry,
            NativeSlot run, NativeSlot action, CharacterPoseNativeNodePoseBuffer oldOutput, CharacterPoseNativeNodePoseBuffer newOutput,
            HistoricalActionSlot oldSlot, CurrentActionSlot newSlot, Result[] old, Result[] current, ulong baseIdentity)
        {
            ulong actionPrevious = 0;
            for (int i = 0; i < frames.Length; i++)
            {
                Frame f = frames[i];
                ulong completion = baseIdentity + (ulong)i + 1;
                var runWrite = run.Evaluate(poses[i * 2], in f.RunSample, 1f, f.RunContinuity, f.Delta, completion, i == 0 ? 0 : completion - 1);
                var actionWrite = action.Output.RequireWriteBinding(completion);
                if (f.HasAction)
                {
                    actionWrite = action.Evaluate(poses[i * 2 + 1], in f.ActionSample, f.ActionWeight, f.ActionContinuity, f.Delta, completion, actionPrevious);
                    actionPrevious = completion;
                }
                var sourceRead = new CharacterPoseNativePoseReadBinding(in runWrite);
                var actionRead = new CharacterPoseNativePoseReadBinding(in actionWrite);
                var a = oldOutput.RequireWriteBinding(completion);
                var b = newOutput.RequireWriteBinding(completion);
                oldSlot.Evaluate(in sourceRead, in actionRead, in a, f.HasAction);
                newSlot.Evaluate(in sourceRead, in actionRead, in b, f.HasAction);
                old[i] = Collect(f, in a, rig, geometry);
                current[i] = Collect(f, in b, rig, geometry);
                for (int bone = 0; bone < rig.PoseBoneCount; bone++)
                {
                    current[i].MaxPoseError = Mathf.Max(current[i].MaxPoseError, Vector3.Distance(a.DenseLocalPoses[bone].Position, b.DenseLocalPoses[bone].Position));
                    current[i].MaxRotationError = Mathf.Max(current[i].MaxRotationError, Quaternion.Angle(a.DenseLocalPoses[bone].Rotation, b.DenseLocalPoses[bone].Rotation));
                    current[i].MaxScaleError = Mathf.Max(current[i].MaxScaleError, Vector3.Distance(a.DenseLocalPoses[bone].Scale, b.DenseLocalPoses[bone].Scale));
                }
            }
        }

        static Result Collect(Frame frame, in AnimationPlayerPoseNativeWriteBinding write, CharacterAnimationRigPayload rig, Geometry geometry)
        {
            var result = new Result { Valid = write.CompletedAt[0] == write.CompletionIdentity && write.Availability[0] == AnimationPoseAvailability.Pose };
            int count = write.ContributionCount[0];
            for (int i = 0; i < count; i++)
            {
                var value = write.Contributions[i];
                var id = i == 0 ? frame.RunId : frame.ActionId;
                frame.Contributions[i] = new AnimationPoseSourceContribution(new PoseNodeId(i == 0 ? "corin.locomotion.locomotion.sequence" : "corin.full-body-action.slot"),
                    value.Kind, id, i, value.ContributionContinuityIdentity, value.Weight, value.LeftFootWeight, value.RightFootWeight);
            }
            int selected = CurrentFootSelector.RequireFootMotionContribution(frame.Contributions, count);
            result.SelectedAction = selected == 1;
            result.SelectedWeight = write.Contributions[selected].Weight;
            result.Sample = write.Contributions[selected].FootMotion;
            if (count == 2) { result.ActionWeight = write.Contributions[1].Weight; result.ActionDenseWeight = write.DenseContributionWeights[rig.PoseBoneCount + rig.RightLeg.AnklePhysicalBoneIndex]; }
            var derive = CharacterVirtualBonePoseDerivation.Derive(rig.BoneCounts,
                new NativeSlice<AnimationLocalBonePose>(write.DenseLocalPoses, 0, rig.PhysicalBoneCount), geometry.Parents, geometry.Virtual,
                geometry.Components, write.DenseLocalPoses);
            result.Valid &= derive.Succeeded;
            var ankle = geometry.Components[rig.RightLeg.AnklePhysicalBoneIndex];
            result.Ankle = frame.RootPosition + frame.RootRotation * ankle.Position;
            result.Rotation = frame.RootRotation * ankle.Rotation;
            return result;
        }

        static CharacterAclDecoder Decoder(CharacterAclAnimationResource resource, int index)
        {
            var manifest = resource.GetGroupManifest(index);
            return CharacterAclDecoder.CreateGroup(resource.RequireGroupPayload(CharacterAclDataBlockKind.Transform), resource.RequireGroupPayload(CharacterAclDataBlockKind.Scalar),
                resource.RequirePayload(CharacterAclDataBlockKind.DatabaseHeader, index), resource.RequirePayload(CharacterAclDataBlockKind.BulkMedium, index),
                resource.RequirePayload(CharacterAclDataBlockKind.BulkLow, index), manifest.TransformTrackCount, manifest.ScalarTrackCount, index);
        }

        sealed class Geometry : IDisposable
        {
            readonly CharacterAnimationRigPayload rig;
            internal readonly NativeArray<int> Parents;
            internal readonly NativeArray<CharacterVirtualBoneDescriptor> Virtual;
            internal readonly NativeArray<CharacterComponentBonePose> Components;
            internal Geometry(CharacterAnimationRigPayload rig)
            {
                this.rig = rig;
                Parents = new NativeArray<int>(rig.PhysicalBoneCount, Allocator.Persistent);
                Virtual = new NativeArray<CharacterVirtualBoneDescriptor>(rig.VirtualBoneCount, Allocator.Persistent);
                Components = new NativeArray<CharacterComponentBonePose>(rig.PhysicalBoneCount, Allocator.Persistent);
                for (int i = 0; i < rig.PhysicalBoneCount; i++) Parents[i] = rig.GetPoseParentIndex(i);
                for (int i = 0; i < rig.VirtualBoneCount; i++) { var b = rig.VirtualBones[i]; Virtual[i] = new CharacterVirtualBoneDescriptor(new CharacterPoseBoneRuntimeId(b.VirtualBoneId), b.SourcePhysicalBoneIndex, b.TargetPhysicalBoneIndex, b.PoseBoneIndex); }
            }
            internal NativeArray<AnimationLocalBonePose> Decode(CharacterAclDecoder decoder, CharacterAclAnimationResourceManifest manifest, float time, NativeArray<CharacterAclNativeTransformSample> values)
            {
                decoder.SampleTransforms(time, values);
                var output = new NativeArray<AnimationLocalBonePose>(rig.PoseBoneCount, Allocator.Persistent);
                foreach (var binding in manifest.TransformBindings) output[binding.PoseBoneIndex] = values[binding.TrackIndex].ToPose();
                var result = CharacterVirtualBonePoseDerivation.Derive(rig.BoneCounts, new NativeSlice<AnimationLocalBonePose>(output, 0, rig.PhysicalBoneCount), Parents, Virtual, Components, new NativeSlice<AnimationLocalBonePose>(output));
                Assert.That(result.Succeeded, Is.True);
                return output;
            }
            public void Dispose() { Components.Dispose(); Virtual.Dispose(); Parents.Dispose(); }
        }

        sealed class NativeSlot : IDisposable
        {
            const int SourceCapacity = 3 * AnimationBlendSourcePoseWorkspace.PhysicalPageCount;
            readonly CharacterAnimationRigPayload rig;
            internal readonly CharacterPoseNativeNodePoseBuffer Output;
            readonly AnimationSlotBlendPoseWorkspace workspace;
            readonly NativeArray<AnimationLocalBonePose> pose;
            readonly NativeArray<AnimationBlendBoneVelocity> velocity;
            readonly NativeArray<float> parameters, scales;
            readonly NativeArray<byte> availability, hasFeatures;
            readonly NativeArray<AnimationFootFeatureSample> left, right;
            NativeArray<AnimationFootMotionSourceSample> foot;
            NativeArray<ulong> completed;
            readonly NativeArray<AnimationSourcePoseCaptureFailure> failures;
            readonly NativeArray<int> owners;
            internal NativeSlot(CharacterAnimationRigPayload rig)
            {
                this.rig = rig;
                Output = new CharacterPoseNativeNodePoseBuffer(0, rig.PoseBoneCount, 1, 3);
                var write = Output.RequireWriteBinding(1);
                workspace = new AnimationSlotBlendPoseWorkspace(2, in write);
                pose = new NativeArray<AnimationLocalBonePose>(rig.PoseBoneCount * SourceCapacity, Allocator.Persistent);
                velocity = new NativeArray<AnimationBlendBoneVelocity>(rig.PoseBoneCount * SourceCapacity, Allocator.Persistent);
                parameters = new NativeArray<float>(SourceCapacity, Allocator.Persistent); scales = new NativeArray<float>(SourceCapacity, Allocator.Persistent); scales[0] = 1;
                availability = new NativeArray<byte>(SourceCapacity, Allocator.Persistent); hasFeatures = new NativeArray<byte>(SourceCapacity, Allocator.Persistent);
                left = new NativeArray<AnimationFootFeatureSample>(SourceCapacity, Allocator.Persistent); right = new NativeArray<AnimationFootFeatureSample>(SourceCapacity, Allocator.Persistent);
                foot = new NativeArray<AnimationFootMotionSourceSample>(SourceCapacity, Allocator.Persistent); completed = new NativeArray<ulong>(SourceCapacity, Allocator.Persistent);
                failures = new NativeArray<AnimationSourcePoseCaptureFailure>(SourceCapacity, Allocator.Persistent); owners = new NativeArray<int>(SourceCapacity, Allocator.Persistent);
            }
            internal AnimationPlayerPoseNativeWriteBinding Evaluate(NativeArray<AnimationLocalBonePose> input, in AnimationFootMotionSourceSample motion, float weight, ulong continuity, float delta, ulong completion, ulong previous)
            {
                NativeArray<AnimationLocalBonePose>.Copy(input, 0, pose, 0, rig.PoseBoneCount); foot[0] = motion; completed[0] = completion;
                var write = Output.RequireWriteBinding(completion);
                workspace.BeginFrame();
                var preparation = workspace.PrepareInactivePage(in write, AnimationSlotBlendFramePlanKind.CrossFade,
                    AnimationSelectionAvailabilityPolicy.RequireSelection, rig.ScalePolicy, AnimationPoseAvailability.Pose, AnimationPoseNativeInvalidReason.None,
                    weight, 1, continuity, previous);
                var entry = new AnimationSlotBlendFramePlanEntry(0, 0, 1, AnimationPoseContributionKind.Live, 0, continuity, weight, weight, weight);
                workspace.SetPreparedEntry(in preparation, 0, in entry);
                for (int b = 0; b < rig.PoseBoneCount; b++) workspace.SetPreparedDenseBoneWeight(in preparation, 0, b, weight);
                workspace.ValidateInactivePage(in preparation); workspace.CommitInactivePage(in preparation);
                var sources = new AnimationBlendSourcePoseNativeReadBinding(rig.PoseBoneCount, 1, SourceCapacity, completion, pose, velocity, parameters, availability,
                    left, right, foot, scales, hasFeatures, completed, failures, owners);
                var job = new AnimationSlotBlendJob(workspace.RequireActiveBinding(), sources);
                job.EvaluateFrame(delta); workspace.CommitFrame();
                return write;
            }
            public void Dispose() { owners.Dispose(); failures.Dispose(); completed.Dispose(); foot.Dispose(); right.Dispose(); left.Dispose(); hasFeatures.Dispose(); availability.Dispose(); scales.Dispose(); parameters.Dispose(); velocity.Dispose(); pose.Dispose(); workspace.Dispose(); Output.Dispose(); }
        }

        sealed class Frame
        {
            internal readonly int Number;
            internal readonly float Delta, RunTime, ActionTime, ActionWeight, RecordedSelectedWeight;
            internal readonly bool HasAction;
            internal readonly ulong RunContinuity, ActionContinuity;
            internal readonly AnimationPoseSourceId RunId, ActionId;
            internal readonly AnimationFootMotionSourceSample RunSample, ActionSample;
            internal readonly AnimationPoseSourceContribution[] Contributions = new AnimationPoseSourceContribution[2];
            internal readonly Vector3 RootPosition, RecordedAnkle;
            internal readonly Quaternion RootRotation, RecordedRotation;
            internal Frame(JToken captured, JObject columns, JObject ids, CharacterPoseNativeDomainResourceSet set, CharacterPresentationPoseSourcePlan run, CharacterActionAnimationSourcePlan action)
            {
                Number = (int)captured["frame"];
                var row = new Row(captured["main"], columns["main"]);
                var sources = ((JArray)captured["sources"]).Select(x => new Row(x, columns["sources"])).ToArray();
                Row r = sources[0]; HasAction = sources.Length == 2;
                Delta = row.F("input/presentation-delta-seconds"); RunTime = r.F("clip-time"); RunContinuity = r.U("continuity");
                ActionWeight = HasAction ? 1f - r.F("weight") : 0;
                RecordedSelectedWeight = row.F("input/foot-step-observation/source-weight");
                RunId = ParseId((string)ids[r.S("node-id") + "|" + r.S("selection-generation") + "|0"]);
                RunSample = Curves(run.FootStepObservation, set.SourcePlans.ToList().IndexOf(run), (ulong)run.ContentRevision.GetHashCode(), r);
                if (HasAction) { var a = sources[1]; ActionTime = a.F("clip-time"); ActionContinuity = a.U("continuity");
                    ActionId = ParseId((string)ids[a.S("node-id") + "|" + a.S("selection-generation") + "|" + a.S("action-instance-id")]);
                    ActionSample = Curves(action.FootStepObservation, set.SourcePlans.Count + set.ActionSourcePlans.ToList().IndexOf(action), (ulong)action.FullDependencyHash.GetHashCode(), a); }
                RootPosition = row.V("physical-body/pose-root-world-position"); RootRotation = row.Q("physical-body/pose-root-world-rotation");
                RecordedAnkle = row.V("foot/source-ankle-position"); RecordedRotation = row.Q("foot/source-ankle-rotation");
            }
        }

        static AnimationFootMotionSourceSample Curves(AnimationFootStepObservationCurvePair observation, int index, ulong identity, Row clip)
        {
            int cycle = checked((int)Math.Floor(double.Parse(clip.S("continuous-clip-time"), CultureInfo.InvariantCulture) / clip.F("duration-seconds")));
            return new AnimationFootMotionSourceSample(index, identity, clip.I("clip-binding-index"), cycle, clip.F("normalized-time"),
                CopySample(observation.Left.Sample(clip.F("normalized-time"), cycle, clip.F("duration-seconds"), clip.B("loop"))),
                CopySample(observation.Right.Sample(clip.F("normalized-time"), cycle, clip.F("duration-seconds"), clip.B("loop"))));
        }

        static AnimationFootMotionRuntimeSample CopySample(EditorAnimation::ThirdPersonCharacter.Pipeline.Animation.AnimationFootMotionRuntimeSample sample) =>
            new AnimationFootMotionRuntimeSample(sample.FootHeight, sample.ToeHeight, sample.ToeSpeed, sample.PositionError, sample.RotationError,
                sample.Contact, sample.LockMode, sample.LockWeight, sample.Support, in sample.Events);

        static AnimationPoseSourceId ParseId(string text)
        {
            string[] v = text.Split('/'); int at = Array.FindIndex(v, x => x == "Timeline" || x == "Clip" || x == "MotionMatching" || x == "BlendSpace");
            var kind = (AnimationPoseSourceKind)Enum.Parse(typeof(AnimationPoseSourceKind), v[at]); var generation = new AnimationPoseSelectionGeneration(ulong.Parse(v[at + 1], CultureInfo.InvariantCulture));
            if (kind != AnimationPoseSourceKind.Timeline) return new AnimationPoseSourceId(new PresentationPoseSourceIndex(int.Parse(v[0], CultureInfo.InvariantCulture)), kind, generation);
            string[] track = v[1].Split('@'); var producer = new AnimationProducerId(v[0], track[0]);
            return new AnimationPoseSourceId(new AnimationPlaybackId(producer, ulong.Parse(track[1], CultureInfo.InvariantCulture)), kind, generation, ulong.Parse(v[at + 2].Split(':')[1], CultureInfo.InvariantCulture));
        }
        struct Result { internal bool Valid, SelectedAction; internal float SelectedWeight, ActionWeight, ActionDenseWeight, MaxPoseError, MaxRotationError, MaxScaleError; internal AnimationFootMotionSourceSample Sample; internal Vector3 Ankle; internal Quaternion Rotation; }
        sealed class Row
        {
            readonly Dictionary<string, string> values;
            internal Row(JToken data, JToken columns) => values = columns.Select((x, i) => new KeyValuePair<string, string>((string)x, (string)data[i])).ToDictionary(x => x.Key, x => x.Value);
            internal string S(string key) => values[key]; internal float F(string key) => float.Parse(S(key), CultureInfo.InvariantCulture); internal int I(string key) => int.Parse(S(key), CultureInfo.InvariantCulture);
            internal ulong U(string key) => ulong.Parse(S(key), CultureInfo.InvariantCulture); internal bool B(string key) => bool.Parse(S(key));
            internal Vector3 V(string key) => new Vector3(F(key + ".x"), F(key + ".y"), F(key + ".z")); internal Quaternion Q(string key) => new Quaternion(F(key + ".x"), F(key + ".y"), F(key + ".z"), F(key + ".w"));
        }
        static JArray Vector(Vector3 v) => new JArray(v.x, v.y, v.z);
        static JObject Sample(AnimationFootMotionRuntimeSample s) => new JObject { ["footHeight"] = s.FootHeight, ["toeHeight"] = s.ToeHeight, ["toeSpeed"] = s.ToeSpeed,
            ["positionError"] = s.PositionError, ["rotationError"] = s.RotationError, ["contact"] = s.Contact, ["lockMode"] = (int)s.LockMode, ["lockWeight"] = s.LockWeight, ["support"] = s.Support,
            ["currentContactIdentity"] = s.Events.CurrentContact.Identity.ToString(), ["sourceSampleIdentity"] = s.Events.CurrentContact.SourceSampleIdentity.ToString(), ["phase"] = (int)s.Events.Phase,
            ["timeToLanding"] = s.Events.TimeToLandingSeconds, ["swingProgress"] = s.Events.SwingProgress, ["approachProgress"] = s.Events.ApproachContactToLandingProgress,
            ["current"] = Event(s.Events.CurrentContact), ["next"] = Event(s.Events.NextLanding) };
        static JToken Event(AnimationFootMotionEventOccurrence value) => value.IsValid ? new JObject { ["ordinal"] = value.Ordinal, ["cycle"] = value.LandingCycle,
            ["normalizedTime"] = value.NormalizedTime, ["distance"] = value.Distance, ["point"] = Vector(value.RootLocalLanding),
            ["sourceSampleIdentity"] = value.SourceSampleIdentity.ToString(), ["continuity"] = value.ContributionContinuityIdentity.ToString(), ["identity"] = value.Identity.ToString() } : JValue.CreateNull();
    }
}
