# 当前摄像机代码与交付边界

记录日期：2026-09-13；最新设计口径：2026-09-17 v5。基于 D:/Unity_Project_1/3C 工作区调查，重写时 HEAD 为 0aa52f209。共享工作区并非干净提交快照，实施前需重读相关差异。本页是当前状态证据，不是第二份方案或端到端验收报告。

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

1. CameraWorldBasicHistory 普通 Apply 保留 SmoothDamp 速度，只有 Reset/初始化边界清零；当前仍未取得运行时手感证据。
2. FramePlanner 固定采样 ElevationRatio；Offset 在 CameraWorldBasicData 中按空间长度计算，AspectRatio 未进入该公式。字段单位及源语义需对账。
3. DefaultSequence 是默认轨道 owner；重复 DefaultSphere/DefaultOrbitGroup 存储已清理，DefaultSphere/DefaultFOV 由正式 Projection stage 消费。生成 Projection 仍需正式重发布，不能用旧 v1 产物证明闭合。
4. Response 同 priority/weight 的裁决已继续比较 generation/action/cycle/source/event，当前没有再用前置相等权短路。
5. TargetResolver 已提供显式目标槽、双点/多点/实体取景和运行中目标失效退出；不自动选敌，ZZZ 实体构图公式仍未闭合。
6. Camera Override/Shake/Shot 仍要求正式资源与 Projection 闭包；Shot 缺 prefab/VCam 或不支持的时钟会明确失败，不能用占位资源补齐。
7. Collision 已有正式 PhysicsScene 查询、收缩/恢复、起点重叠、无合法空间和输出诊断链；Corin 当前碰撞开关与 Unity 动态运行仍未验收。
8. CameraDebugSnapshot 与 CaptureFrame 已覆盖帧/逻辑 Tick、输入/响应、Sequence/Target 退出、效果、Reset、碰撞和最终输出；连续性/Cue 算子已消费退出合同，运行采样仍未取得。
9. 工程源码中已无 ThirdPersonCameraController/旧 FreeLook 朝向写入；CharacterFixedInputTraceWorkflow 现在通过正式 CharacterSimulationPresentationRuntime 的 CameraBasis/InitialState 记录与恢复 yaw。
10. 当前 C# 作者规范已替换旧 Agent 包；相机各资源和请求的完整 C# 导出/生成覆盖尚未逐项证明，不能因公共入口存在就勾选完成。
11. 动作相机请求的正式目标已改为技能 Graph 内的 TreeClip 特殊 Node；当前 Corin 仍是通用 ActionCueClip，尚无正式 TreeClip/Node ResourceId 请求，因此本次设计更新不等于资产迁移完成。

## 当前 Unity 证据

本轮早先 CLI 曾列出 3C_Client@1e41b3a3e2ded45f，Unity 2022.3.62f2c1；随后显式 hash 的 get_project_info 返回 HTTP 503 / No Unity instances connected。服务器 status 在线，instance list 为空。实例 project_path 尚未核对，因此没有取得当前场景、Play、Console、运行对象或画面。

该连接记录属于本次早先调查，不是文档重写时再次连接的结果。本次重写没有操作 Unity、编译或生成资产。历史 Editor.log 不替代当前 Console；没有端到端、回放 A/B 或视觉手感结论。

## 旧文件清理与保留

- 原 proposal/design/tasks 和 delta 已按当前合同收敛；2026-09-17 v5 进一步明确动作相机请求统一走 TreeClip 特殊 Node，旧 CameraCueClip/ActionCueClip(CueType: Camera) 仅作为待迁移历史状态。
- 删除独立 docs/character-camera-plan-2026-09-13.md，内容收敛到本 change 的设计与本页。
- 保留 source-baseline.md/source-behavior.md 的来源身份、数值、函数和未闭合项；它们是历史取证，不能当作今日实现进度。
- 未修改旧 camera-zzz worktree、其它任务的协调记录、相机代码、资产或 current spec。

## 源码跳转

- [FramePlanner：基准轨道与手动偏移](D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Camera/Solver/CharacterCameraFramePlanner.cs:23)
- [SequenceEvaluator：平滑与历史重置](D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Camera/Solver/CharacterCameraSequenceEvaluator.cs:61)
- [PresentationRuntime：姿态、请求与最终输出](D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Presentation/CharacterCameraPresentationRuntime.cs:308)
- [RigAdapter：应用与实际结果回读](D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Camera/Runtime/CinemachineCameraRigAdapter.cs:51)
- [Projection：尚未发布的碰撞消费者](D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/CameraContracts/Projection/CharacterCameraProjectionPayload.cs:176)
- [输入回放：正式 Camera Presentation 初始状态](D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/Assets/GameScripts/Main/Editor/CharacterPipeline/Diagnostics/CharacterFixedInputTraceWorkflow.cs:430)
