# 当前摄像机代码与交付边界

记录日期：2026-09-13。基于 D:/Unity_Project_1/3C 工作区调查，重写时 HEAD 为 0aa52f209。共享工作区并非干净提交快照，实施前需重读相关差异。本页是当前状态证据，不是第二份方案或端到端验收报告。

## 已有代码，保留而不重做

| 已有部分 | 源码/资产证据 | 边界 |
|---|---|---|
| 正式装配 | GameplayLabLocalFixedVariant → GameplayLabLocalFixed.prefab；FixedCharacterHost 绑定 CameraRig/Follow/Aim/LookAxis | 磁盘配置，不代表已取得当前 Scene 实例 |
| 输入 | Session.BeginRenderFrame → Registration.CaptureRenderFrame → UnityFixedCharacterInputAdapter；Look 来自 Pointer delta 且已反转 Y | 当前 Profile 是 Keyboard&Mouse |
| 表现时序 | CharacterSimulationPresentationRuntime.CompletePresentationFrame 在 CommitFinalPose 后调用 Camera.Present | 默认跟随依然是 Body visible pose + bind offset，不是默认读骨骼 |
| 默认构图 | CorinCameraDefaultSequence：三轨道、中间 ElevationRatio=0.5、PolarAngle=0、FOV=50 | 不等于原 Corin 配置选择与全部消费者已经还原 |
| 手动视角 | FramePlanner 使用 m_YawOffset/m_PitchOffset，ByTrack 明确加偏移 | 旧“轨道直接覆盖鼠标”的问题已不符合当前源码 |
| 请求和效果 | Sequence/Response/Target、转场与退出容器；Zoom/Stretch evaluator；Profile 各18资源 | 代码和资源存在不等于所有真实动作都触发/退出 |
| 输出 | RigAdapter.Apply → ForceCameraPosition → Brain.ManualUpdate → Result/Basis | 当前项目先求位姿再落地 |
| 生成产物 | CorinCharacterPipelineDefinition.PresentationProjection.asset 已有 m_Camera、default sequence、相机灵敏度等字段 | 旧记录“Generated 无 Camera”过时；未声明产物与当前所有源码一致 |

## 仍缺的代码或合同

1. CameraWorldBasicHistory 普通 Apply 调 SetCurrent，SetCurrent 清零全部 SmoothDamp 速度，连续历史不完整。
2. FramePlanner 固定采样 ElevationRatio；Offset 在 CameraWorldBasicData 中按空间长度计算，AspectRatio 未进入该公式。字段单位及源语义需对账。
3. Profile.DefaultOrbitGroup 与 Sequence.CameraOrbits 重复；其它无消费者字段不能仅凭哈希/校验被称为已生效。DefaultSphere/DefaultFOV 有实际初始化消费者，不能直接删。
4. Response 同 priority/weight 的前置 <= 分支跳过候选，后面的同权 generation/action/cycle 规则无法处理相等权重。
5. TargetResolver 提供明确绑定点，尚不是完整锁定、双点/多点/实体取景或选敌。
6. CameraOverrideProjectionCompiler、CameraShakeProjectionCompiler、CameraShotProjectionCompiler 明确拒绝未闭合消费者；对应 runtime owner 也抛不可用错误。
7. Collision.Enabled 在 CharacterCameraProjectionPayload.RequireValid 中明确抛出正式消费者未发布错误；当前输出链没有碰撞阶段。
8. CameraDebugSnapshot 主要提供 Plan、Result、TargetSource、ProjectionRevision；已有连续性/Cue 路由诊断算子，但不是完整输入/效果/碰撞解释。
9. ThirdPersonCameraController 仍存在；CharacterFixedInputTraceWorkflow 仍通过场景搜索该类型来记录/恢复 yaw。调查时该诊断文件已有用户修改，角色 prefab 也已修改。
10. 当前 C# 作者规范已替换旧 Agent 包；相机各资源和请求的完整 C# 导出/生成覆盖尚未逐项证明，不能因公共入口存在就勾选完成。

## 当前 Unity 证据

本轮早先 CLI 曾列出 3C_Client@1e41b3a3e2ded45f，Unity 2022.3.62f2c1；随后显式 hash 的 get_project_info 返回 HTTP 503 / No Unity instances connected。服务器 status 在线，instance list 为空。实例 project_path 尚未核对，因此没有取得当前场景、Play、Console、运行对象或画面。

该连接记录属于本次早先调查，不是文档重写时再次连接的结果。本次重写没有操作 Unity、编译或生成资产。历史 Editor.log 不替代当前 Console；没有端到端、回放 A/B 或视觉手感结论。

## 旧文件清理与保留

- 原 proposal/design/tasks 和 delta 本次按当前合同重写，去掉旧批次/旧 worktree 路由及 v4/v5 等已废弃计划。
- 删除独立 docs/character-camera-plan-2026-09-13.md，内容收敛到本 change 的设计与本页。
- 保留 source-baseline.md/source-behavior.md 的来源身份、数值、函数和未闭合项；它们是历史取证，不能当作今日实现进度。
- 未修改旧 camera-zzz worktree、其它任务的协调记录、相机代码、资产或 current spec。

## 源码跳转

- [FramePlanner：基准轨道与手动偏移](D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Camera/Solver/CharacterCameraFramePlanner.cs:23)
- [SequenceEvaluator：平滑与历史重置](D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Camera/Solver/CharacterCameraSequenceEvaluator.cs:61)
- [PresentationRuntime：姿态、请求与最终输出](D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Presentation/CharacterCameraPresentationRuntime.cs:308)
- [RigAdapter：应用与实际结果回读](D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Camera/Runtime/CinemachineCameraRigAdapter.cs:51)
- [Projection：尚未发布的碰撞消费者](D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/CameraContracts/Projection/CharacterCameraProjectionPayload.cs:176)
- [输入回放：仍引用旧 Controller](D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/Assets/GameScripts/Main/Editor/CharacterPipeline/Diagnostics/CharacterFixedInputTraceWorkflow.cs:430)
