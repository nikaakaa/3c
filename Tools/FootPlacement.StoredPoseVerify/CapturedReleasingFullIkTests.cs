/*
目的：在同一完整释放业务中校准真实FBBIK的六个腿关节点，再比较来源修正后的膝角和末端位移。
输入：正式Native完整组件姿势、真实双脚/骨盆Goal、录制种子与连续帧、正式Rig/Profile。
链路：真实种子Pose+Goal预滚初始化方向 → 连续SolvePrepared → 实际Hip/Knee/Ankle与膝角。
边界：原姿势全部满足正式可靠弯曲阈值；不伪造四个历史方向，不声称历史previous-dot/Revision已复现。
说明：releasing-action-native.html；正式Runner未执行，此入口为Unity内函数实验。
*/
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using ThirdPersonCharacter.Pipeline.Animation;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public static class CapturedReleasingFullIkTests
    {
        public static JObject Run(string fixturePath, string posePath, string goalPath, string outputPath, bool recordedLegPositions, bool recordedHistoricalGoals)
        {
            var report = new JObject { ["status"] = "running", ["scope"] = "真实Native全组件姿势和正式Foot Goal进入实际FBBIK；种子Pose+Goal预滚方向；未复现历史previous-dot等元数据" };
            report["recordedLegPositionDiagnostic"] = recordedLegPositions;
            report["recordedHistoricalGoalDiagnostic"] = recordedHistoricalGoals;
            try
            {
                Assert.That(EditorApplication.isPlaying || EditorApplication.isCompiling, Is.False);
                JObject fixture = JObject.Parse(File.ReadAllText(fixturePath, Encoding.UTF8));
                JObject poses = JObject.Parse(File.ReadAllText(posePath, Encoding.UTF8));
                JObject goals = JObject.Parse(File.ReadAllText(goalPath, Encoding.UTF8));
                Assert.That((string)poses["status"], Is.EqualTo("passed")); Assert.That((string)goals["status"], Is.EqualTo("passed"));
                const string root = "Assets/Configs/Character/Corin/Pipeline/Presentation/";
                var rig = new CharacterAnimationRigPayload(AssetDatabase.LoadAssetAtPath<CharacterAnimationRigDefinition>(root + "Rig/CorinAnimationRigDefinition.asset"));
                var profile = AssetDatabase.LoadAssetAtPath<CharacterFullBodyIkProfile>(root + "IK/CorinFullBodyIkProfile.asset");
                report["profileRevision"] = profile.Revision; report["rigRevision"] = rig.RigRevision;
                var captured = new[] { fixture["seed"] }.Concat((JArray)fixture["frames"]).ToArray();
                var frames = new Frame[captured.Length];
                using (var parents = new NativeArray<int>(rig.PoseBoneCount, Allocator.Persistent))
                using (var virtualBones = new NativeArray<CharacterVirtualBoneDescriptor>(rig.VirtualBoneCount, Allocator.Persistent))
                using (var pending = new NativeArray<AnimationLocalBonePose>(rig.PoseBoneCount, Allocator.Persistent))
                using (var workspace = new NativeArray<CharacterFullBodyIkGoal>(3, Allocator.Persistent))
                {
                    var parentWrite = parents; var virtualWrite = virtualBones;
                    for (int i = 0; i < rig.PoseBoneCount; i++) parentWrite[i] = rig.GetPoseParentIndex(i);
                    for (int i = 0; i < rig.VirtualBoneCount; i++) { var v = rig.VirtualBones[i]; virtualWrite[i] = new CharacterVirtualBoneDescriptor(new CharacterPoseBoneRuntimeId(v.VirtualBoneId), v.SourcePhysicalBoneIndex, v.TargetPhysicalBoneIndex, v.PoseBoneIndex); }
                    var solver = new CharacterFinalIkFullBodySolver(rig, profile, parents, virtualBones);
                    try
                    {
                        for (int i = 0; i < frames.Length; i++) frames[i] = new Frame(captured[i], fixture["columns"], poses["rows"][i], rig, recordedLegPositions);
                        foreach (bool current in new[] { false, true })
                        {
                            var results = new Result[frames.Length];
                            var goalFrames = new CharacterFullBodyIkGoal[frames.Length][];
                            goalFrames[0] = frames[0].SeedGoals;
                            var rows = (JArray)goals[current ? "current" : "historical"]["rows"];
                            for (int i = 1; i < frames.Length; i++) goalFrames[i] = !current && recordedHistoricalGoals ? frames[i].SeedGoals : new[] { Goal(rows[i-1]["leftGoal"]), Goal(rows[i-1]["rightGoal"]), Goal(rows[i-1]["pelvisGoal"]) };
                            Execute(frames, goalFrames, results, rig, solver, pending, workspace);
                            long before = GC.GetAllocatedBytesForCurrentThread();
                            Execute(frames, goalFrames, results, rig, solver, pending, workspace);
                            long allocated = GC.GetAllocatedBytesForCurrentThread()-before;
                            Execute(frames, goalFrames, results, rig, solver, pending, workspace, true);
                            var outputRows = new JArray();
                            report[current ? "current" : "historical"] = new JObject { ["allocatedBytes"] = allocated, ["rows"] = outputRows };
                            for (int i = 0; i < frames.Length; i++)
                            {
                                Frame f = frames[i]; Result r = results[i];
                                outputRows.Add(new JObject { ["frame"] = f.Sequence, ["preRoll"] = i==0, ["succeeded"] = r.Succeeded, ["failure"] = r.Failure.ToString(),
                                    ["leftHip"] = Vector(r.LeftHip), ["leftKnee"] = Vector(r.LeftKnee), ["leftAnkle"] = Vector(r.LeftAnkle),
                                    ["rightHip"] = Vector(r.RightHip), ["rightKnee"] = Vector(r.RightKnee), ["rightAnkle"] = Vector(r.RightAnkle),
                                    ["recordedJoints"] = new JArray(f.Expected.Select(Vector)),
                                    ["leftSolverDiagnostics"] = Leg(r.LeftDiagnostics), ["rightSolverDiagnostics"] = Leg(r.RightDiagnostics),
                                    ["leftBendDegrees"] = Bend(r.LeftHip,r.LeftKnee,r.LeftAnkle), ["rightBendDegrees"] = Bend(r.RightHip,r.RightKnee,r.RightAnkle),
                                    ["maxRecordedJointError"] = f.Expected.Select((v,j)=>Vector3.Distance(v,r.Point(j))).Max(),
                                    ["leftRecordedBend"] = f.LeftRecordedBend, ["rightRecordedBend"] = f.RightRecordedBend });
                            }
                            Assert.That(allocated, Is.Zero);
                            for (int i = 0; i < frames.Length; i++)
                            {
                                Assert.That(results[i].Succeeded, Is.True, "正式FBBIK必须实际完成");
                            }
                        }
                        foreach (JToken row in report["historical"]["rows"])
                            Assert.That((float)row["maxRecordedJointError"], Is.LessThan(.0002f), "历史六关节点必须先校准，候选数值仅保留为未校准诊断");
                        report["status"] = "passed";
                    }
                    finally { foreach (Frame f in frames) if (f != null) f.Dispose(); }
                }
            }
            catch (Exception error) { report["status"] = "failed"; report["failure"] = error.ToString(); }
            File.WriteAllText(outputPath, report.ToString(Formatting.None), new UTF8Encoding(false));
            return new JObject { ["status"] = report["status"], ["failure"] = report["failure"] };
        }

        static void Execute(Frame[] frames, CharacterFullBodyIkGoal[][] goals, Result[] results, CharacterAnimationRigPayload rig, CharacterFinalIkFullBodySolver solver,
            NativeArray<AnimationLocalBonePose> pending, NativeArray<CharacterFullBodyIkGoal> workspace, bool diagnostics = false)
        {
            solver.Reset(); CharacterFullBodyIkBendHistory history = default;
            for (int i = 0; i < frames.Length; i++)
            {
                Frame f=frames[i]; pending.CopyFrom(f.Pose);
                for (int g=0;g<3;g++) workspace[g]=goals[i][g];
                var result = solver.SolvePrepared(new NativeSlice<AnimationLocalBonePose>(pending), in f.Header, workspace, ref history, f.Sequence, f.Completion, diagnostics);
                results[i] = new Result { Succeeded=result.Succeeded, Failure=result.Failure,
                    LeftDiagnostics=diagnostics?solver.GetDiagnosticLimb(2).LegPose:default, RightDiagnostics=diagnostics?solver.GetDiagnosticLimb(3).LegPose:default,
                    LeftHip=f.World(pending[rig.LeftLeg.HipPhysicalBoneIndex].Position), LeftKnee=f.World(pending[rig.LeftLeg.KneePhysicalBoneIndex].Position), LeftAnkle=f.World(pending[rig.LeftLeg.AnklePhysicalBoneIndex].Position),
                    RightHip=f.World(pending[rig.RightLeg.HipPhysicalBoneIndex].Position), RightKnee=f.World(pending[rig.RightLeg.KneePhysicalBoneIndex].Position), RightAnkle=f.World(pending[rig.RightLeg.AnklePhysicalBoneIndex].Position) };
            }
        }
        sealed class Frame : IDisposable
        {
            internal readonly NativeArray<AnimationLocalBonePose> Pose;
            internal readonly Vector3 Root;
            internal readonly Quaternion Rotation;
            internal readonly ulong Sequence,Completion;
            internal readonly CharacterFullBodyIkGoalSetHeader Header;
            internal readonly CharacterFullBodyIkGoal[] SeedGoals;
            internal readonly Vector3[] Expected;
            internal readonly float LeftRecordedBend,RightRecordedBend;
            internal Frame(JToken captured,JToken columns,JToken pose,CharacterAnimationRigPayload rig,bool recordedLegPositions)
            {
                var values=((JArray)columns["main"]).Select((k,i)=>new {Key=(string)k,Value=(string)captured["main"][i]}).ToDictionary(x=>x.Key,x=>x.Value);
                var left=((JArray)columns["main"]).Select((k,i)=>new {Key=(string)k,Value=(string)captured["paired"]["main"][i]}).ToDictionary(x=>x.Key,x=>x.Value);
                float F(string k)=>float.Parse(values[k],CultureInfo.InvariantCulture); Vector3 V(string k)=>new Vector3(F(k+".x"),F(k+".y"),F(k+".z"));
                Sequence=ulong.Parse(values["foot/resolved/core/frame-sequence"],CultureInfo.InvariantCulture);Completion=ulong.Parse(values["foot/resolved/core/completion-identity"],CultureInfo.InvariantCulture);
                Assert.That((ulong)pose["frame"], Is.EqualTo(Sequence));
                Root=V("physical-body/pose-root-world-position");Rotation=new Quaternion(F("physical-body/pose-root-world-rotation.x"),F("physical-body/pose-root-world-rotation.y"),F("physical-body/pose-root-world-rotation.z"),F("physical-body/pose-root-world-rotation.w"));
                Pose=new NativeArray<AnimationLocalBonePose>(rig.PoseBoneCount,Allocator.Persistent);
                for(int i=0;i<rig.PoseBoneCount;i++){var p=pose["componentPoses"][i];Pose[i]=new AnimationLocalBonePose(new Vector3((float)p[0],(float)p[1],(float)p[2]),new Quaternion((float)p[3],(float)p[4],(float)p[5],(float)p[6]),new Vector3((float)p[7],(float)p[8],(float)p[9]));}
                Header=new CharacterFullBodyIkGoalSetHeader(Sequence,Completion,rig.RigId,rig.RigRevision,0,0,0,3,CharacterFullBodyIkGoalSetAvailability.Ready);
                SeedGoals=new[]{RecordedGoal(left,"foot/goal/"),RecordedGoal(values,"foot/goal/"),RecordedGoal(values,"pelvis-goal/")};
                if(recordedLegPositions)
                {
                    Vector3 shift=SeedGoals[2].ComponentPosition*SeedGoals[2].PositionWeight;
                    var writable=Pose;
                    void Replace(System.Collections.Generic.Dictionary<string,string> row, string name, int bone)
                    {
                        Vector3 p=new Vector3(float.Parse(row[name+".x"],CultureInfo.InvariantCulture),float.Parse(row[name+".y"],CultureInfo.InvariantCulture),float.Parse(row[name+".z"],CultureInfo.InvariantCulture))-shift;
                        var previous=writable[bone];writable[bone]=new AnimationLocalBonePose(in previous,p);
                    }
                    Replace(left,"leg/leg-pose/original-hip",rig.LeftLeg.HipPhysicalBoneIndex);Replace(left,"leg/leg-pose/original-knee",rig.LeftLeg.KneePhysicalBoneIndex);Replace(left,"leg/leg-pose/original-ankle",rig.LeftLeg.AnklePhysicalBoneIndex);
                    Replace(values,"leg/leg-pose/original-hip",rig.RightLeg.HipPhysicalBoneIndex);Replace(values,"leg/leg-pose/original-knee",rig.RightLeg.KneePhysicalBoneIndex);Replace(values,"leg/leg-pose/original-ankle",rig.RightLeg.AnklePhysicalBoneIndex);
                }
                Vector3 Point(System.Collections.Generic.Dictionary<string,string> row,string name)=>World(new Vector3(float.Parse(row[name+".x"],CultureInfo.InvariantCulture),float.Parse(row[name+".y"],CultureInfo.InvariantCulture),float.Parse(row[name+".z"],CultureInfo.InvariantCulture)));
                Expected=new[]{Point(left,"leg/leg-pose/solved-hip"),Point(left,"leg/leg-pose/solved-knee"),Point(left,"leg/leg-pose/solved-ankle"),Point(values,"leg/leg-pose/solved-hip"),Point(values,"leg/leg-pose/solved-knee"),Point(values,"leg/leg-pose/solved-ankle")};
                LeftRecordedBend=float.Parse(left["leg/leg-pose/solved-bend-degrees"],CultureInfo.InvariantCulture);RightRecordedBend=F("leg/leg-pose/solved-bend-degrees");
            }
            internal Vector3 World(Vector3 p)=>Root+Rotation*p;
            public void Dispose()=>Pose.Dispose();
        }
        struct Result
        {
            internal bool Succeeded;internal CharacterFullBodyIkFailure Failure;internal Vector3 LeftHip,LeftKnee,LeftAnkle,RightHip,RightKnee,RightAnkle;
            internal CharacterFullBodyIkLegPoseDiagnostics LeftDiagnostics,RightDiagnostics;
            internal Vector3 Point(int i)=>i==0?LeftHip:i==1?LeftKnee:i==2?LeftAnkle:i==3?RightHip:i==4?RightKnee:RightAnkle;
        }
        static CharacterFullBodyIkGoal Goal(JToken v)=>new CharacterFullBodyIkGoal((CharacterFullBodyIkEffectorSlot)(int)v["slot"],new Vector3((float)v["position"][0],(float)v["position"][1],(float)v["position"][2]),new Quaternion((float)v["rotation"][0],(float)v["rotation"][1],(float)v["rotation"][2],(float)v["rotation"][3]),(float)v["positionWeight"],(float)v["rotationWeight"],(CharacterFullBodyIkGoalApplication)(int)v["application"],(CharacterFullBodyIkGoalSourceKind)(int)v["sourceKind"],(int)v["metadataIndex"]);
        static CharacterFullBodyIkGoal RecordedGoal(System.Collections.Generic.Dictionary<string,string> r,string p)
        {
            float F(string k)=>float.Parse(r[p+k],CultureInfo.InvariantCulture);int I(string k)=>int.Parse(r[p+k],CultureInfo.InvariantCulture);
            return new CharacterFullBodyIkGoal((CharacterFullBodyIkEffectorSlot)I("slot"),new Vector3(F("component-position.x"),F("component-position.y"),F("component-position.z")),new Quaternion(F("component-rotation.x"),F("component-rotation.y"),F("component-rotation.z"),F("component-rotation.w")),F("position-weight"),F("rotation-weight"),(CharacterFullBodyIkGoalApplication)I("application"),(CharacterFullBodyIkGoalSourceKind)I("source-kind"),I("diagnostic-metadata-index"));
        }
        static float Bend(Vector3 h,Vector3 k,Vector3 a)=>180f-Vector3.Angle(h-k,a-k);
        static JArray Vector(Vector3 v)=>new JArray(v.x,v.y,v.z);
        static JObject Leg(CharacterFullBodyIkLegPoseDiagnostics d)=>new JObject {
            ["originalHip"]=Vector(d.OriginalHip),["originalKnee"]=Vector(d.OriginalKnee),["originalAnkle"]=Vector(d.OriginalAnkle),
            ["targetAnkle"]=Vector(d.TargetAnkle),["solvedHip"]=Vector(d.SolvedHip),["solvedKnee"]=Vector(d.SolvedKnee),["solvedAnkle"]=Vector(d.SolvedAnkle),
            ["effectiveBendDirection"]=Vector(d.EffectiveBendDirection),["stabilizationWeight"]=d.StabilizationWeight,["bendDirectionSource"]=d.BendDirectionSource.ToString(),
            ["originalBendDegrees"]=d.OriginalBendDegrees,["solvedBendDegrees"]=d.SolvedBendDegrees,["targetExtensionRatio"]=d.TargetExtensionRatio
        };
    }
}
