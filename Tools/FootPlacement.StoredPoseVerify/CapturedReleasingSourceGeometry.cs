/*
目的：在同一完整释放业务内并列记录每个实际贡献源的脚底几何与作者FootHeight基准。
输入：正式ACL源姿势、真实Native混合贡献、正式脚跟/脚尖标定；2024至2056及2023种子。
链路：ACL解码 → 完整Native/Action混合 → 正式PoseRig CaptureFoot → 组件脚底中点与每脚贡献基准。
边界：只记录事实和算术关系，不将root-relative高度视为地面，不修改曲线或生产代码；未判新方案通过。
说明：releasing-action-native.html。
*/
using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Presentation;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public static partial class CapturedReleasingNativeTests
    {
        public static JObject ObserveSourceSoles(string inputPath,string outputPath)
        {
            var fixture=JObject.Parse(File.ReadAllText(inputPath,Encoding.UTF8));
            var rig=new CharacterAnimationRigPayload(AssetDatabase.LoadAssetAtPath<CharacterAnimationRigDefinition>(AssetsRoot+"Rig/CorinAnimationRigDefinition.asset"));
            var set=AssetDatabase.LoadAssetAtPath<CharacterPoseNativeDomainResourceSet>(AssetsRoot+"Profiles/CorinPoseNativeDomainResourceSet.asset");
            var resource=AssetDatabase.LoadAssetAtPath<CharacterAclAnimationResource>(ResourcePath);
            var calibration=AssetDatabase.LoadAssetAtPath<CharacterFootPlacementRigCalibration>(AssetsRoot+"FootPlacement/CorinFootPlacementRigCalibration.asset");
            var runPlan=set.SourcePlans.Single(x=>x.GroupClipIndex==10);
            var actionPlan=set.ActionSourcePlans.Single(x=>x.GroupClipIndex==1);
            var captured=new[]{fixture["seed"]}.Concat((JArray)fixture["frames"]).ToArray();
            var frames=captured.Select(x=>new Frame(x,(JObject)fixture["columns"],(JObject)fixture["sourceIds"],set,runPlan,actionPlan)).ToArray();
            var rows=new JArray();
            var root=new GameObject("Releasing component geometry observation"){hideFlags=HideFlags.HideAndDontSave};
            root.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            try
            {
                using(var geometry=new Geometry(rig))
                using(var runDecode=Decoder(resource,10))
                using(var actionDecode=Decoder(resource,1))
                using(var decoded=new NativeArray<CharacterAclNativeTransformSample>(rig.PhysicalBoneCount,Allocator.Persistent))
                using(var components=new NativeArray<CharacterComponentBonePose>(rig.PoseBoneCount,Allocator.Persistent))
                using(var componentPoses=new NativeArray<AnimationLocalBonePose>(rig.PoseBoneCount,Allocator.Persistent))
                using(var run=new NativeSlot(rig))
                using(var action=new NativeSlot(rig))
                using(var output=new CharacterPoseNativeNodePoseBuffer(0,rig.PoseBoneCount,1,3))
                {
                    var componentWrite=components;var poseWrite=componentPoses;
                    var slot=new CurrentActionSlot();ulong previousAction=0;
                    for(int i=0;i<frames.Length;i++)
                    {
                        var f=frames[i];ulong completion=(ulong)i+1;
                        using(var runPose=geometry.Decode(runDecode,resource.GetGroupManifest(10),f.RunTime,decoded))
                        {
                            var runFeet=Capture(runPose);
                            var baseWrite=run.Evaluate(runPose,in f.RunSample,1f,f.RunContinuity,f.Delta,completion,i==0?0:completion-1);
                            var actionWrite=action.Output.RequireWriteBinding(completion);
                            var actionFeet=default(CharacterFootPlacementAnimatedPose);
                            if(f.HasAction)
                            {
                                using(var actionPose=geometry.Decode(actionDecode,resource.GetGroupManifest(1),f.ActionTime,decoded))
                                {
                                    actionFeet=Capture(actionPose);
                                    actionWrite=action.Evaluate(actionPose,in f.ActionSample,f.ActionWeight,f.ActionContinuity,f.Delta,completion,previousAction);
                                }
                                previousAction=completion;
                            }
                            var baseRead=new CharacterPoseNativePoseReadBinding(in baseWrite);var actionRead=new CharacterPoseNativePoseReadBinding(in actionWrite);
                            var write=output.RequireWriteBinding(completion);slot.Evaluate(in baseRead,in actionRead,in write,f.HasAction);
                            var final=Capture(write.DenseLocalPoses);
                            var sources=new JArray();
                            for(int source=0;source<write.ContributionCount[0];source++)
                            {
                                var c=write.Contributions[source];var feet=source==0?runFeet:actionFeet;
                                sources.Add(new JObject{["sourceName"]=source==0?runPlan.DisplayName:actionPlan.AuthoringClipIdentity.name,
                                    ["clipIdentity"]=source==0?runPlan.ClipIdentity:actionPlan.ClipIdentity,["groupClipIndex"]=source==0?10:1,
                                    ["clipTime"]=source==0?f.RunTime:f.ActionTime,["contributionWeight"]=c.Weight,["leftWeight"]=c.LeftFootWeight,["rightWeight"]=c.RightFootWeight,
                                    ["leftFootHeight"]=c.FootMotion.Left.FootHeight,["rightFootHeight"]=c.FootMotion.Right.FootHeight,
                                    ["leftGeometry"]=Foot(feet.Left),["rightGeometry"]=Foot(feet.Right)});
                            }
                            rows.Add(new JObject{["frame"]=f.Number,["preRoll"]=i==0,["rootWorldPosition"]=Vector(f.RootPosition),
                                ["rootWorldRotation"]=new JArray(f.RootRotation.x,f.RootRotation.y,f.RootRotation.z,f.RootRotation.w),["sources"]=sources,
                                ["finalLeftGeometry"]=Foot(final.Left),["finalRightGeometry"]=Foot(final.Right)});
                        }
                    }
                    CharacterFootPlacementAnimatedPose Capture(NativeSlice<AnimationLocalBonePose> local)
                    {
                        for(int bone=0;bone<rig.PoseBoneCount;bone++)
                        {
                            if(!CharacterPoseConstraintMath.TryCreateComponent(local[bone],rig.GetPoseParentIndex(bone),components,out var c))throw new InvalidOperationException("正式组件转换失败");
                            componentWrite[bone]=c;poseWrite[bone]=new AnimationLocalBonePose(in c);
                        }
                        var left=calibration.Left;var right=calibration.Right;
                        var l=rig.LeftLeg;var r=rig.RightLeg;
                        return new CharacterFootPlacementAnimatedPose(1,componentPoses[rig.PelvisPhysicalBoneIndex].Position,
                            CapturedFootPoseRigFunctions.CaptureFoot(root.transform,componentPoses[l.HipPhysicalBoneIndex],componentPoses[l.KneePhysicalBoneIndex],componentPoses[l.AnklePhysicalBoneIndex],componentPoses[l.ToePhysicalBoneIndex],left.HeelContactLocalOffset,left.ToeContactLocalOffset,left.SoleFrameLocalRotation,in left),
                            CapturedFootPoseRigFunctions.CaptureFoot(root.transform,componentPoses[r.HipPhysicalBoneIndex],componentPoses[r.KneePhysicalBoneIndex],componentPoses[r.AnklePhysicalBoneIndex],componentPoses[r.ToePhysicalBoneIndex],right.HeelContactLocalOffset,right.ToeContactLocalOffset,right.SoleFrameLocalRotation,in right));
                    }
                }
            }
            finally{Object.DestroyImmediate(root);}
            var report=new JObject{["status"]="observed",["scope"]="同一33帧组件空间每源和最终混合脚底几何；只观测，不判新方案通过",
                ["rootBonePolicy"]=rig.RootBonePolicy.ToString(),["scalePolicy"]=rig.ScalePolicy.ToString(),["coordinateSpace"]="Component；临时PoseRoot为单位变换，原世界PoseRoot单列；中点由正式标定heel/toe得到，不代表已知地面",
                ["calibrationRevision"]=calibration.ContentRevision,["rigRevision"]=rig.RigRevision,["executedModuleMvid"]=typeof(CapturedReleasingNativeTests).Module.ModuleVersionId.ToString(),["rows"]=rows};
            File.WriteAllText(outputPath,report.ToString(Formatting.None),new UTF8Encoding(false));return new JObject{["status"]="observed",["frames"]=frames.Length};
        }
        static JObject Foot(CharacterFootPlacementAnimatedFootPose f)=>new JObject{["heel"]=Vector(f.HeelPosition),["toe"]=Vector(f.ToePosition),["soleMidpoint"]=Vector((f.HeelPosition+f.ToePosition)*.5f),["ankle"]=Vector(f.AnklePosition)};
    }
}
