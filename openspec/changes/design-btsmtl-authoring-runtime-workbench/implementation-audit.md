# 预览迁移事实审计

## 基线与接口状态

| 追溯点 | 实际内容 | 本 change 可消费范围 |
|---|---|---|
| `d99093011f9ac7f6083e5c567ae2128b28bbea99` | BTSMTL 技能编辑器与角色控制职责的规划提交 | 只作为目标职责和迁移方向，不作为运行接口 |
| `3bf66c4ead5272367e35a6ec0484c7c5d172dcf8` | 控制状态机与技能激活独立的规划提交 | 只作为控制与技能分责依据，不作为已安装 SkillProgram 接口 |
| `cbcd7fa4264b504f4f37d06f1f10b2ac5ee71b9b` | 角色控制核心基础合同 | 当前基线可确认存在基础输入与控制类型，不能证明技能编译、ActionInstance、v5 或来源观察已闭合 |
| `9b6468bca790bda7c45ec69a14e5e5cbc8339c63` | 预览 change 的规划、设计和 delta spec 更新 | 已安装为规划材料，不代表实现已经存在 |
| `229d2a9d01082cfb3466e57ea8352a0ef6aa3976` | 本 worktree 实施基线 | 仍包含旧 RootTree 字段、v4 Agent authoring 和窗口级完整角色预览；后续小步已收口 Pose 入口 |

当前 worktree 仍没有可直接消费的 `Document v7` 正式服务、完整统一代码/operation 来源公开接口或非 Skill Timeline owner。`SkillDefinition`、`SkillProgram`、`ActionInstance/SkillExecutionState` 的正式类型已被当前作者工作区消费；本批次进一步闭合了有限 Timeline 的 route、ActionInstance、正式 Action 结果 Trace 与 Scene Play 观察，以及已有 Runtime Debug Camera 事件的只读显示，但 Tree-only 专用窗口表面、非 Skill 调用和 v7 作者能力仍未闭合。本 change 不复制 reader、迁移器、解释器或状态格式。

## 已确认的现行调用链

### 场景启动

历史上的 `GameplayLabEditorLauncher` 和 `GameplayLauncherWindow` 调用 `EditorPlayModeSceneLauncher.Play`。旧 Launcher 保存一个 active scene path，使用 `OpenSceneMode.Single` 打开目标场景，调用可选 `prepare`，进入 Play，退出后只按单一路径打开原 active scene。它没有保存完整 `SceneSetup[]`、启动场景设置、请求 identity 或场景 generation。当前实现已将两个调用者迁入 `EditorPlayModeSceneLauncher.Start`，旧 `Play` 入口已删除。

### 完整角色窗口预览

`CharacterPipelineHost` 当前直接承载正式 Session/Actor registration。窗口级 `TimelinePreviewTarget` 已删除；Timeline 工作区通过 `IBtsmtlScenePlayPreviewOperations` 进入统一场景生命周期。Pose 入口现在通过 `CharacterPoseObservationViewport` 保存本地 Scene/Context 选择，按 Context Actor roster 解析正式 Live target。

Pose Bottom Dock 不再创建独立 Preview Scene、隐藏相机、局部 Animation 时钟、Fact 输入或视觉根恢复；Live snapshot 和调参仍从 `AnimationPresentationRuntimeTarget` 的正式 provider 读取。Motion Matching Query 的独立查询 fixture仍消费共用 `AnimationPreviewRuntime`，不属于本轮完整角色 Pose 播放闭包。

### Pose、素材和诊断入口

- `CharacterPoseAuthoringBottomDock` 已改为 `CharacterPoseObservationViewport`，只声明 Scene/Context、调用统一 Scene Play 操作并按 Context Actor roster 绑定正式 Live target；现有 `LiveTuningLayout`、`SubmitLivePoseTuningCandidate`、`RuntimeDebugSession` 和 `AnimationPresentationRuntimeTarget` 保持为唯一观察/调参端口。
- `CharacterPoseResetObservationMcpTool` 已改为显式 Scene/Context/Actor 的轮询诊断：created/used 来自正式 committed snapshot，Reset 由 `IBtsmtlScenePlayPreviewOperations.Reset()` 执行，reset 只在新的 scene generation 和 runtime identity 登记后采样；旧 FBBIK hash、goal、output 与 bend-history 比较语义保留。
- `CharacterFootPlacementAnimationAnalyzer` 也使用独立 preview scene，但它是离线 AnimationClip/Foot Analysis 采样入口，保留其独立用途，不迁移成角色运行预览。
- 原生 Animation Window、曲线编辑、Blend Space 几何/采样点和模块诊断不属于完整角色播放器删除范围。

## 迁移与删除清单

### 迁入统一场景运行

- `EditorPlayModeSceneLauncher`：扩展为显式请求、完整编辑环境保存/恢复和场景 generation 生命周期。
- `GameplayLabEditorLauncher`、`GameplayLauncherWindow`：迁入同一显式 Launcher 请求，不保留第二套进入 Play 逻辑。
- `BtsmtlScenePlayPreviewCoordinator`：独占 Start/Pause/Resume/Reset/Stop 状态、请求和场景 generation，连接真实 Unity Play 状态。
- `BtsmtlScenePlayContext`：声明场景 Context，要求至少有正式 Character Session/Actor roster 或实现 `IBtsmtlScenePlayRuntimeOwner` 的内容 owner，并发布角色产物与 owner 就绪状态；释放只通过 Context 调用各正式 owner。
- `TimelineEditorWorkspaceView`：保留字段交互、几何和绘制，播放控制改为场景预览操作；编辑游标只保留本地视图状态。
- `CharacterPoseAuthoringBottomDock`、Action Workspace、Blend Space、MM 页面：保留作者面板和只读观察，完整角色运行统一绑定场景正式 Actor。

### 完整角色预览替代后删除

- `TimelinePreviewRuntimeSession`、`TimelinePreviewTarget` 及其完整角色窗口目标字段。当前 `TimelinePreviewRuntimeSession`、`TimelinePreviewTarget`、角色 Host 的 Pose Graph Preview Controller 和 Pose 专用 Fixture 已删除；`AnimationPreviewRuntime` 只因 MM Query 独立 fixture 仍消费而保留。
- `CharacterPipelineAuthoringPreviewController`、Pose 专用 `CharacterAnimationPreviewFixture`、Corin Fixture 资产以及 `CharacterPipelineHost` 中窗口级 Pose 求值、视觉根接管恢复、Pose 时钟和 Pose 调参桥接已删除。
- `AnimationPreviewRuntime`、`AnimationPreviewAdapters` 仍被 `MotionMatchingQueryFixture` 消费，但只保留 Pose Graph/MM 查询能力；窗口级 `PreviewSession`、`CharacterPipelinePreviewProgram` 和 Timeline Action 独立播放适配器已删除。
- Pose 页面旧 seek/reset 输入、直接 Fact 参数和临时目标选择已删除；正式 Actor 的 reset、Fault、Dispose 和 Scene Play 重建仍归各自 owner。

### 必须保留并隔离

- `CharacterFootPlacementAnimationAnalyzer` 的离线分析与校准 preview scene。
- 正式 `character.pose_reset_observation` 诊断使用 Scene Play Actor；离线 Foot 分析及 MM 查询 fixture仍各自保留，不作为统一场景播放入口，也不竞争正式 Actor 的物理和动画输出。
- 原生 Animation Window 的精确素材导航、AnimationClip 注册曲线编辑、Blend Space 几何绘制及独立模块诊断。
- `SimulationSessionHost` 的正式 Session、Quiesce、Release、Actor registration、Presentation 和 Diagnostics owner。

## 当前缺失与实现边界

- 已创建 `Assets/Scenes/Authoring/BtsmtlPreview.unity`，接入共享环境、CorinStandalonePlayer、Local Float32 Session Composition、Unity CharacterController World Solver、正式 Cinemachine 相机和唯一 `btsmtl-preview` Character Context。该场景只闭合角色运行目标；具体非 Skill Timeline owner、目标绑定、播放 identity、调用点和 generation 仍等待 Timeline owner 发布后消费。
- 主重构已发布 `a47532948` 的 Skill/Action identity、ActionInstance 和 generation runtime，以及 `ceb50eb9e` 的 Corin Rules、技能 authoring/编译输入；`0cdb043dd` 补齐三处 Unity 装配引用。本 worktree 已消费这三个正式提交，但预览尚未把它们接入作者选择、ActionInstance 观察和完整 v7 Capability，因此任务 2.3、4.7、5.3–5.4、6.8、7.6、10.1–10.4 仍未完成，不能复制其编译器或状态格式。
- 在早期 `229d2a9d0` 实施基线中，`CharacterPipelineDefinition` 仍保存 `RootTreeAsset`，Agent authoring 仍是 v4；该段只记录当时的主重构 owner 边界。随后 `d423510f0` 合入已删除旧 RootTree 运行入口，本 change 不恢复 v4 兼容路径。
- 现有 `SimulationSessionHost` 已提供正式 `Quiesce`、`ReleaseSessionRuntime` 和 `Stop`，场景重建协调器应调用这些公开生命周期，不直接清理 Program、Presentation、Foot、IK 或 Camera 内部状态。
- 现有 `LocalSimulationDebugControlService` 能提交 Tick 驱动命令，但它是 Simulation Debug 观察控制，不等于场景预览 owner；场景暂停必须由协调器连接真实 Unity Play 状态，不能把 Debug Tick 驱动包装成第二个预览时钟。

## 精确接口、提供提交与受阻任务表

| 功能 | 需要的正式接口 | 提供提交 / 当前来源 | 当前状态与受阻任务 |
|---|---|---|---|
| Pose Scene Play 生命周期 | `IBtsmtlScenePlayPreviewOperations`、`BtsmtlScenePlayStatus`、`BtsmtlScenePlayContextRegistry` | 公共合同来自 `8d635dd24`；协调器和 Context 预检收口于 `f748ddcc4`、`5ec9d3819` | 已由 `5b85a6d16` 接入 Pose 入口；不等待其它领域 |
| Pose Live committed snapshot / Pose Watch | `AnimationPresentationRuntimeTarget`、`AnimationPresentationRuntimeTargetRegistry`、`IAnimationPresentationRuntimeSnapshotProvider` | 帧级正式 target 在 `1d1ef64e0`，Foot/业务事件接线在 `7d3c6f713`；当前 provider 为 `CharacterSimulationPresentationRuntime` | 已由 `5b85a6d16` 按 Context Actor roster 绑定，继续使用 `4ecc673ee` 的正式 Reset 诊断；不再从全局列表取首个目标 |
| Pose 运行调参 | `CharacterPipelineHost.LiveTuningLayout`、`LiveActiveTuningBlock`、`SubmitLivePoseTuningCandidate` | 当前正式 Host/Presentation runtime；候选采用语义沿现有 Pose Tuning 合同 | 已接入并保留作者 Mutation、Undo、Next Frame/Next Activation 状态；v7资格统一仍属于任务 2.3、7.1–7.6、10.1–10.2 |
| Skill / Action 预览关联 | SkillDefinition/SkillProgram、唯一 Action service、ActionInstance、调用 generation 和正式来源观察 | 当前作者工作区消费主重构公开类型；本批次 `aeb057b18`、`37448e987`、`3eb95d5d2`、`31681ee94` | 已接入有限 Timeline 的精确 route、正式 Scene Actor、Runtime Debug、Animation Presentation 与 ActionInstance 选择；正式输入准入、Tree-only Action 结果和完整调用 generation 仍受阻，见 `ISSUE-SCENEPLAY-001` |
| Document v7 作者能力 | 唯一 v7 schema、Capability、Exporter/Codec/Reconciler/Mutation/Validator | 最新规划与 `btsmtl-agent-authoring` 已按 v7 定义；当前 worktree 仍可见 v4 Agent authoring | 当前工作树未消费 v7 正式服务；受阻任务 2.3、10.1–10.4；不以 v4 兼容路径代替 |
| 非 Skill Timeline 运行 | 独立内容根、业务 owner、播放 identity、目标绑定、调用点和 generation | `8e7bdbf4b` 位于 `codex/decouple-timeline-from-skill`；当前 worktree 只有公共 Scene Play owner 合同 | 提供者不在当前 worktree 的可消费实现中；受阻任务 2.2、2.5、4.3、4.5、6.2、6.8、8.1–8.2、10.4、11.2；不创建假角色或本地执行器 |
| Reset 诊断 | 正式 committed snapshot、Pose Watch、Scene Play Reset、generation/identity变化 | `AnimationPresentationRuntimeTarget` 当前接口 + `BtsmtlScenePlayPreviewCoordinator.Reset()`；消费提交 `4ecc673ee` | 已替代旧 fixture 诊断；诊断命令不再创建 Preview Scene，不把 Reset 直接下发给 Presentation 内部 |
| 原生 Animation Clip 编辑 | Profile/Clip 的 typed authoring target 与 `ClipCurves` 接收器 | 既有 `CharacterAnimationClipAuthoringService` 与 Profile Inspector；Pose Graph 入口 `d161d5971` 改为打开明确 Clip 资产，不从 Live Actor 反推 target | 保留原生素材编辑 owner；当前 Pose Scene Actor 只读观察，不安装素材接收器 |

## 本轮实现与验证记录

- `1735d4afd`：修正域重载后的请求恢复、StartPaused 和 scene generation；允许无角色目标的通用 Context；修正 Context 注册表按实例注销；角色 Session 释放收敛到 Context owner；Gameplay Lab 变体通过 Editor SessionState 传递，不改写场景作者值。
- `5996d322f`：修正 `generated-diagnostic-sampling` 正式本地包路径，并同步 lock 文件。
- `1c3f8e76e`：补齐正式 Runtime Owner 的 `Ready/Failed/Release` 合同，并持久化 Stop/Fault 状态，域重载不把失败或 Stopping 恢复成新的准备流程。
- `6a2f2b29d`：将 Runtime Owner 纯合同迁入不依赖角色的 `ThirdPersonGameplay` 公共程序集，允许非角色 authoring，并接入 Running/Paused 阶段的正式 owner/Session Fault 检查。
- `c2504eaf9`：允许非角色 Context 通过 `runtimeOwner` 直接配置，并保持至少一个正式 owner 的校验。
- `357ce835f`：将 Play 内重载调用收敛到统一 Launcher，并处理请求丢失、运行中 owner/session 失活和 Fault 清理。
- `b08a62378`：重载入口校验当前 pending request 的 requestId、owner、原场景和活动场景；请求丢失后的 Fault 以独立终态保存，避免域重载后清成 Idle。
- `8d635dd24`：将场景预览操作合同迁入不依赖角色的公共程序集，Timeline Editor 和 Action Animation Workspace 使用同一 Scene/Context 选择与 Start/Pause/Resume/Reset/Stop；时间轴只保留本地作者游标，删除已无调用的窗口级 `TimelinePreviewSession`。
- `a47532948`、`ceb50eb9e`、`0cdb043dd`：消费主重构已发布的 Skill/Action、Corin Rules 和 Unity 装配提交，不添加旧 namespace 别名或复制类。
- `d0d7bf2c4`：Action Animation Workspace 保存并恢复嵌套 Timeline 的 Scene/Context 选择，域重载或重新绑定不再丢失作者选择。
- `2029e63b5`：消费正式 `c2d9a203d` 的四文件控制端口闭包；当前分支已有的技能身份、动作代次和规则装配等价前置不重复摘取，未额外引入状态 codec 或 v5 运行路径。
- `c347b307a`：公开公共 Scene Play 请求的 `Prepare` 合同，并在 Timeline 与 Action Workspace 分开显示本地下一次启动选择和 `Status.Identity` 的实际场景/Context。
- `8c9784712`、`918181861`、`087515665`：将 Timeline 结构、Clip 时间、区段、轨道排序、Inspector 和 TreeClip 写入统一收敛到 Scene Play 感知的作者 Mutation 边界；Play 中只锁真实构建写入，不切换 Live Debug，不锁 Scene 操作和作者游标；同时补齐 Tree Editor 可见性和 Timeline Editor 工具程序集引用。
- `32138ccf3`、`bcd084bcd`：删除 `TimelinePreviewTarget` 源码、Host 继承和对应 Unity 元数据，保留现有 Host 的正式 Session/Presentation/诊断端口，清掉失效脚本入口。
- `7aa163eb3`、`aab466f11`、`c90d72181`、`2e49801c9`、`605c4f01a`、`b62a4b238`、`581d7d1e2`：增加明确的 BTSMTL Scene Play 场景构建入口，处理无标题编辑场景、场景切换后的 Composition 重新解析和保存后 Context 校验，发布 `BtsmtlPreview.unity` 场景及目录元数据，并把方向光作为场景条件持久化。构建器只引用现有 Corin Prefab、正式 Composition、共享环境与现有相机装配，不复制角色运行组合规则。
- `f748ddcc4`：开始 Scene Play 前对明确选择的 Scene 做一次正式 Context 预检，按 ContextId 拒绝缺失、重复或非法配置，不进入 Play、不搜索当前场景替代目标。
- `5b85a6d16`：将 Pose Authoring Viewport 接入显式 Scene/Context 选择、统一 Scene Play 生命周期和 Context Actor roster；Live 目标同时核对 Runtime Debug、Animation Presentation target、Definition、Projection 与 Pose Plan，选中后复用正式观察和调参端口。
- `4ecc673ee`：把 `character.pose_reset_observation` 从 Preview Fixture 同步诊断改为正式 Scene Play Actor 轮询诊断，跨两个 committed frame、统一 Reset、新 scene generation 和新 runtime identity 采样 FBBIK 结果。
- `d161d5971`：按实际调用闭包删除 Pose 专用 Fixture、完整角色 Preview Controller、Host 的旧 Preview 求值/视觉根恢复/调参桥接和窗口 seek/reset 输入；MM Query 独立 fixture仍保留其共用查询运行时。
- 定向 `git grep` 已确认 Pose Fixture、完整角色 Preview Controller、Host 旧 Preview API 和直接 Pose Preview 求值在当前入口中无调用者；MM Query 独立 fixture和原生素材编辑入口仍有各自调用链。
- `df2b7db73`、`25c775adb`：补齐正式 target 首次诊断 interest、Runtime Debug 变化后的 Scene Actor 列表刷新、Definition/Profile 精确匹配，并区分重复 Context 与未加载 Context。
- `7edc469d1`：保留 Pose Source 的原生 Animation Window 导航，Clip 入口只打开明确 AnimationClip，不从 Scene Actor 反推或安装素材接收器。
- Center `compile` Run `5207d8a35b6345c48e18d7dc05035754` 已执行。预览文件未产生编译错误；Run 因消费正式 Skill/Action 合同后，基线的 Fixed/Float32 `CharacterControlReadPort` 尚未实现新增 `IsSkillCompleted`、`CompletedSkillInstanceId`、`IsActionWindowActive` 三项接口而 Faulted，不能记作通过，也不能据此声称 Editor 代码整体通过。
- Center `compile` Runs `3d42743fbe10457ca6bd13327184023e`、`c98dd3cc76a6403e8ac7afdee9440ce7`、`b0adcb255c624aaea6ef7e38ae720031` 和 `be586a3b2caa489cbe26899977d30e8d` 已按当前提交依次执行。前两次暴露并已修复 Timeline Editor 的 `BTSMTL.Editor` using 与 Tree Editor 入口可见性；后两次及最新 Run 不再报告 Timeline、Scene Play 或 Tree Editor 编译错误，仍因现有 FinalIK/RootMotion 程序集缺失而 Faulted，不能记作整体 Compile 通过。
- `5996d322f` 的依赖路径只适用于当前 worktree 的目录层级：在 `D:\Unity_Project_1\3C-worktrees\btsmtl-scene-play-preview` 可解析到 `D:\Unity_Project_1\generated-diagnostic-sampling`；合入主工作树前必须由主协调者按其项目路径重新确认，不能当成所有 checkout 通用路径。
- 当前提交的直接 Unity Editor 构建日志已完成 `ThirdPersonClient.Editor` 与 `ThirdPersonGameplay.Lab.Editor` C# 编译，并执行正式场景构建入口，日志以 `Exiting batchmode successfully now!` 结束；Center Run `27bbff02c3a64c7e973432c4ac08b3de` 在相同提交的编译和 ILPostProcess 完成后因 Unity 退出宿主卡住而通过正式 `cancel` 收口，RunHost 仍未把旧 staging 状态转为 Completed，因此 `change-review` 也被该旧 Running 记录拒绝，不能把它写成 Center Completed 证据。此前 `d3ad9ce2a5c64c94873f010389e76afc` 是 `4a24dc4ba` 提交上的完整 Center Compile 通过记录，不能替代当前提交证据。
- 当前源码身份为 `7edc469d1`；直接编译日志 `D:\Unity_Project_1\3C-Artifacts\btsmtl-pose-final-head-compile.log` 在该源码内容完成 Tundra build success、无 CS 编译错误并以 `Exiting batchmode successfully now!` 结束。正式场景构建日志 `D:\Unity_Project_1\3C-Artifacts\btsmtl-pose-scene-build-current.log` 对应场景构建前的 `25c775adb` 内容，输出 `BTSMTL Scene Play Preview built: Assets/Scenes/Authoring/BtsmtlPreview.unity` 并正常退出；之后的 `7edc469d1` 只增加 Clip 的原生 Animation Window 导航，未改变场景构建器，当前场景文件仍保持提交版本。日志中的 Curl 42 是退出时 Package Manager 回调噪声，不作为源码编译结果。

## EXEC-SCENEPLAY-20260906-01 执行记录

本节对应 `D:/Unity_Project_1/3C/openspec/changes/design-btsmtl-authoring-runtime-workbench/design.md` 的“执行协作与当前批次”。本批次只在 `D:/Unity_Project_1/3C-worktrees/btsmtl-scene-play-preview` 的 `codex/btsmtl-scene-play-preview` 分支继续原授权，不修改 `D:/Unity_Project_1/3C/docs/coordination-progress.md`，不整支合并 ACL、Timeline 或 Camera provider 分支，也没有建立第二条执行路径。

### 源码身份与依赖核对

- 当前代码提交为 `1d9fc3b9cd9061852c23f39d4f0ead5d30300fbe`；本分支与 `main` 的共同祖先为 `229d2a9d01082cfb3466e57ea8352a0ef6aa3976`，当前读取到的 `main` tip 为 `f61e27c34ef83c08e77baeec0b21f666cf88908c`。
- 主重构、ACL、独立 Timeline 和 Camera 的历史提交只用于追溯。实际消费以当前 worktree 的类型、成员、调用者和资源为准；对未在当前 worktree 发布的接口没有用同名类型、反射、字符串注册或本地副本补齐。
- 当前正式可消费的预览边界是 `IBtsmtlScenePlayPreviewOperations`、`BtsmtlScenePlayContextRegistry`、`CharacterPipelineHost.Registration`、`RuntimeDebugSession`、`AnimationPresentationRuntimeTargetRegistry` 以及 `AnimationPresentationRuntimeSnapshot`。Scene Play 只管理生命周期和作者操作，运行帧仍由正式 Session/Action/Presentation owner 产生。

### 本批次提交

| 提交 | 直接范围与结果 |
|---|---|
| `aeb057b181b827af3ea735f4526dedc541dd91b0` | Live Projection 按 `ActionInstanceId` 过滤；同一 producer 有多个 playback 时要求作者明确选择，不再按最新序列静默换绑。 |
| `9e323971e36c470da441f0665843423ef8f70865` | Timeline 窗口入口将 `Authoring Preview` 统一为 `Scene Play`，仍使用同一生命周期操作。 |
| `867e7e1f6703f86e6eb5f7e408f95771d5465160` | 删除无调用者的 `ActionAnimationWorkspacePreviewView` 与 `TryResolvePreview`；保留 MM Query 独立 fixture 所需的 `AnimationPreviewRuntime`。 |
| `37448e9879105f754028fbb24ae133c191afc081` | Action Workspace 增加 Timeline candidate/page state，按 Timeline route 选择多级调用；无有限 Timeline 时保留作者页并显示未绑定。 |
| `3eb95d5d2c6385135874a3c8efe4e004a9744219` | TimelineNode 打开 Action Workspace 时保留该节点的精确 route，不重新猜测其它 Timeline。 |
| `31681ee944ffe58f2e3b5d90916271ebaeb2323d` | Scene Play 页按 Scene、Context、Definition 解析唯一正式 Actor，附着正式 Runtime Debug 与 Animation Presentation target，显示真实 Action、时间、Slot 和 Final Pose。 |
| `dd1667e3ed71a04aa7daf9a443846f5590da9c06` | Scene Play/Live 只读显示已有 `CameraSnapshot`、`CameraRequest`、`CameraCue` 事件的名称、状态、owner、优先级、权重和最终值。 |
| `43840c23bd283768f46028d967b5273b11c34427` | Timeline 绘制层的游标模式统一为 `ScenePlay`，不改变几何或运行行为。 |
| `1d9fc3b9cd9061852c23f39d4f0ead5d30300fbe` | UXML 场景字段和 TreeClip 执行提示统一使用 `Scene Play` 文案。 |

### 输入、处理、输出与删除闭包

- 输入是作者页当前精确 `CharacterPipelineDefinition`、`ActionProfile`、Action call site、Timeline AuthoringId/route、Animation track/Slot，以及本地保存的 Scene/Context 选择和 ActionInstance 观察选择。
- 处理先由 resolver 建立 Definition → Action → call site → Timeline candidate → route → producer → Presentation binding → Slot 的 typed 关系；运行时再校验 Scene Play 实际 identity、Context descriptor、Actor registration、Runtime Debug source/fingerprint、Presentation projection revision 和正式 committed snapshot。
- 输出是作者页的正式目标、ActionInstance、逻辑/表现时间、Animation Slot、Transition Routing、Blend/Stored/Inertialization、Final Pose 与 Camera diagnostic 只读事实；绑定失败时输出精确原因，不执行 Graph、Timeline、Pose、Camera 或输入推进。
- Timeline 的 Start/Pause/Resume/Reset/Stop 仍全部来自共享 `IBtsmtlScenePlayPreviewOperations`；History 查看、正式 Restore 和输入 Replay 也通过同一 Session Host 合同处理。本批次没有增加 `Update`、业务 `Advance`、窗口播放器、手动相机帧或把历史查看伪装成 seek。
- `git grep` 的有效旧路径结果只剩 `MotionMatchingQueryFixture` 对独立 Pose/MM 查询 `AnimationPreviewRuntime` 的消费，以及通用 Graph Shell 的 `CreateGraphAuthoringPreviewView` 空扩展点；窗口级 `PreviewSession`、`CharacterPipelinePreviewProgram`、Timeline Action 独立播放适配器、完整角色 Action/Timeline 播放器、`TimelinePreviewTarget`、`TimelinePreviewRuntimeSession` 和 Pose 专用 Fixture 没有当前代码调用者。

### 验证证据

- 直接命令：`C:\Program Files\Unity\Hub\Editor\2022.3.62f2c1\Editor\Unity.exe -batchmode -quit -nographics -job-worker-count 2 -projectPath D:\Unity_Project_1\3C-worktrees\btsmtl-scene-play-preview\3cDemo\Client\3C_Client -logFile D:\Unity_Project_1\3C-Artifacts\btsmtl-preview-sceneplay-naming-compile.log`。
- 日志 `D:\Unity_Project_1\3C-Artifacts\btsmtl-preview-sceneplay-naming-compile.log` 对应 `43840c23bd283768f46028d967b5273b11c34427` 的 C# 内容，出现 `*** Tundra build success (14.89 seconds), 19 items updated, 1475 evaluated` 和 `Exiting batchmode successfully now!`，没有本批次新增的 CS error。
- 日志中的 `BlockImpactPostProcessRenderPass.profilingSampler` `CS0108` 是既有隐藏成员警告，不属于本批次改动；没有把它写成源码编译失败。其后的 `1d9fc3b9c` 只改 UXML/TreeClip 作者可见文案，不改变程序集逻辑。
- `git diff --check` 无输出，清理 Unity 自动生成的 `GraphicsSettings.asset`、Performance 元数据、URP 全局设置及端口 `.meta` 后工作树干净。
- 本批次没有新增测试代码，也没有把旧 Center Run、取消后仍为 Running 的 staging 或另一源码身份的日志冒充当前提交的 Completed 证据。Unity 编译、Center Run、正式产物发布和用户端到端验收仍分别计算。

## ISSUE-SCENEPLAY-001：正式技能输入与 Action 结果 Trace 已接入，Tree-only 窗口表面和运行验收待闭合

- **背景与准确操作**：Timeline 工作区从当前 Scene Context 的正式 SkillDefinition 构建 Skill 选项；作者选择 Actor/Skill 后，`RequestSkill` 向该 Actor 的正式 `CharacterControlSource` 排队已登记的 `SourceInputRequestId`。后续正式 Action Runtime 现在会用同一输入序号发布结构化 `action_result` Trace，覆盖拒绝、接受、完成、取消、中断、Abort 和修正；这不是窗口自己创建的 Action。Tree-only 的结果已进入公共 StateMachine Diagnostics，但当前 Timeline 窗口没有独立的 Tree-only 选中面板。
- **当前提交与真实接口**：`BtsmtlScenePlayPreviewCoordinator.RequestSkill` 校验运行状态、Actor、Skill、输入 identity 和同输入的歧义，再调用 `CharacterPipelineHost.TryQueueInputRequest`；`TimelineEditorView` 的 Skill 菜单与 Request 按钮只提交该正式请求。`ActionSkillExecutionRuntime`、Float32/Fixed Action Runtime 和对应 Diagnostics adapter 发布 `ActionResultSubmitted`；Live Debug 只按当前 Timeline playback 的 ActionInstance/Skill 显示结果。真正的 ActionInstance/SkillExecution 仍由正式 Control/Action/Skill Runtime 在后续 Logic Tick 产生，预览没有自行创建实例。
- **证据**：`D:/Unity_Project_1/3C-worktrees/btsmtl-scene-play-preview/3cDemo/Client/3C_Client/Assets/GameScripts/Main/Editor/CharacterPipeline/Preview/BtsmtlScenePlayPreviewCoordinator.cs` 的 `BuildSkillOptions`、`RequestSkill`；`D:/Unity_Project_1/3C-worktrees/btsmtl-scene-play-preview/3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Unity/CharacterPipelineHost.cs` 的 `TryQueueInputRequest`；`D:/Unity_Project_1/3C-worktrees/btsmtl-scene-play-preview/3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Simulation/Core/Execution/ActionSkillExecutionRuntime.cs` 的 Action 结果发布；`D:/Unity_Project_1/3C-worktrees/btsmtl-scene-play-preview/3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/BTSMTL/Timeline/Editor/Scripts/Tree/TimelineEditorMainWindow.cs` 的 StateMachine Trace 过滤和结果显示。
- **影响清单**：4.7、5.4、6.1–6.3、6.6、6.8，以及 Tree-only 场景的 11.2 适用验收；5.3 的正式 Control Source/Ingress 输入入口已完成。
- **已经尝试的正式处理**：只提交正式输入请求并显示 request sequence；Action 结果 Trace 按实际 ActionInstance、调用 generation、SourceMap 和输入序号建立，Timeline Live Debug 只显示当前 playback 的对应结果，不在窗口里直接创建 ActionInstance、Graph evaluator 或空 Timeline。
- **仍需提供**：主验收 Editor 需要验证真实 Scene Play 中请求、拒绝、替换、完成和 Tree-only lifecycle 的显示；若要求 Tree-only 在 Timeline 窗口直接可选，还需要正式 Skill/Action 观察面提供对应的选中入口，而不是把普通 Timeline 当作 Tree-only owner。
- **业务取舍**：当前按钮仍只在提交瞬间报告“已排队”，后续真实结果在诊断面按 ActionInstance 显示，避免把队列接受误报成技能激活；在预览侧补 Action command 会形成第二个实例和状态来源，按设计不采用。

## ISSUE-SCENEPLAY-002：非 Skill Timeline 的正式 owner 与调用绑定未在当前 worktree 发布

- **背景与准确操作**：共享或独立 Timeline 需要在非 Skill 场景中运行时，当前页面只能消费角色 Action call site 的有限 Timeline；不能凭 Timeline 资产本身猜业务 owner、播放 identity、目标、调用点或 generation。
- **当前提交与真实接口**：当前 worktree 有 `TimelineData`、公共 Scene Play 操作和角色 Action route 解析，但没有可被本任务直接调用的非 Skill owner/target/playback contract。`codex/decouple-timeline-from-skill` 的 `71947b4193d79e1deca4a640824274c119534914` 不在当前分支祖先中；历史提交不能替代当前可编译公开成员。
- **证据**：当前源码的 `ActionAnimationAuthoringWorkspaceResolver.ResolveTimelineCandidates` 只从角色 `CharacterAuthoringTopologyProjection` 解析；当前 `BtsmtlScenePlayContext` 的正式 Actor roster 也没有非 Skill Timeline owner 列表。当前分支与 provider 提交的祖先关系核对结果为 `71947b419 ancestor=False`。
- **影响清单**：2.2、2.5、4.3、4.5、6.2、6.8、8.1–8.2、10.4、11.2。
- **已经尝试的正式处理**：保留 Action 与 Timeline 的职责边界，只把 Scene/Context 生命周期和只读观察接入共享入口；没有伪造 Timeline owner、角色 Actor 或第二个执行器。
- **仍需提供**：Timeline owner 需要发布独立根、Build/Store/产物 revision、调用方和目标绑定、播放 identity/generation、Decision/Commit/输出诊断以及停止/teardown 端口。
- **业务取舍**：等待 owner 发布会让非 Skill Timeline 继续只能编辑不能运行，但能保持普通 Timeline 不被错误包装成技能；本地创建通用 owner 可缩短首个样例接入，却会把业务时钟、目标占用和释放责任从真正 owner 分裂出去，按设计不采用。

## ISSUE-SCENEPLAY-003：唯一 Document v7 与作者 Capability 尚未成为当前可消费合同

- **背景与准确操作**：当前 Action Workspace 能显示现有 Definition/Action/Timeline 关系，但不能把 v7 Capability、字段资格、整包事务、Exporter/Codec/Reconciler/Mutation/Validator 和正式采用状态接入预览作者参数。
- **当前提交与真实接口**：早期审计基线曾有 `RootTreeAsset`/v4 authoring 形态；随后 `d423510f0` 删除旧 RootTree 运行入口，但 `90a6607b07754c457398d95b6fec7b110e6af4a0` 的 v5 schema 与完整 Document v7 服务仍不属于当前预览消费合同。现有 Pose 调参合同可以继续使用，但不能被扩大解释成 v5 全域能力。
- **证据**：`D:/Unity_Project_1/3C-worktrees/btsmtl-scene-play-preview/3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Runtime/CharacterPipelineDefinition.cs` 的当前序列化定义；当前 Action Workspace 没有 v7 Capability/Exporter/Codec/Reconciler 调用，且本 worktree 未安装最新 v7 Document 服务。
- **影响清单**：2.3、7.1–7.6、10.1–10.4。
- **已经尝试的正式处理**：运行观察只读，Timeline/Clip 作者 Mutation 仍写其正式 owner；没有保存 NumericProfile、runtime state、场景对象绑定或 v4 兼容 reader。
- **仍需提供**：主重构 owner 需要发布当前 v5 schema、只读 context/capability、合法 Mutation/Undo/Validator 和精确 Build/采用状态；预览随后只连接这些端口。
- **业务取舍**：暂不把运行按钮伪装成热更新能保持产物和作者数据边界，但 v5 字段资格暂时不能在本窗口完整呈现；复制一套 v4/v5 adapter 能快速显示，却会产生双 schema 和错误的采用状态，按设计不采用。

## ISSUE-SCENEPLAY-004：Camera 本批次只完成诊断消费，完整 Camera provider 仍需正式发布

- **背景与准确操作**：Scene Play 场景已有正式相机装配，本批次只在 Action Workspace 的 Preview/Live bottom 读取当前正式 Runtime Debug Animation channel 中的 CameraSnapshot/Request/Cue。该观察能说明当前事件来源和权重，但不等于已接入 Camera Projection、Runtime/Factory、Rig、Shot target、物理绑定及重置/释放合同。
- **当前提交与真实接口**：`dd1667e3ed71a04aa7daf9a443846f5590da9c06` 的 `AddCameraObservation` 不调用 Cinemachine 或 Camera API；当前分支没有把 Camera provider 的完整请求/目标接口接进预览窗口。Camera provider 历史提交 `2b5ff41a0877ff2bf03645057b1209438052f48c` 不在当前分支祖先中。
- **证据**：`D:/Unity_Project_1/3C-worktrees/btsmtl-scene-play-preview/3cDemo/Client/3C_Client/Assets/GameScripts/Main/Editor/CharacterPipeline/Authoring/ActionWorkspace/ActionAnimationAuthoringWorkspaceWindow.cs` 的 `AddCameraObservation`；`D:/Unity_Project_1/3C-Artifacts/btsmtl-preview-sceneplay-naming-compile.log` 只证明本批次观察代码编译，不证明 Camera provider 已发布。
- **影响清单**：6.9、8.1–8.2、11.2 中 Camera 相关检查。
- **已经尝试的正式处理**：先消费已存在的 Runtime Debug event contract，明确只读；没有添加暂停相机帧、Camera seek、第二命令容器或历史状态清理。
- **仍需提供**：Camera owner 需要公开当前请求的完整 source/Projection/Rig/target/physics identity、正式 reset/release 生命周期和可观察诊断；预览只绑定当前 Scene Actor/请求并显示 unavailable 原因。
- **业务取舍**：当前事件观察先让作者看到“相机有没有由正式链路产生”，但不能代替完整 Camera 验收；在预览层补默认相机或手动 frame 会让画面看似可用，却掩盖正式相机资源/目标缺失，按设计不采用。

## ISSUE-SCENEPLAY-005：ACL 资源合同未作为本分支预览旁路消费

- **背景与准确操作**：本轮 MR 顺序包含 ACL，但预览分支不整支加入 ACL MR。当前预览只消费 Animation Presentation/Slot 的正式结果，不读取 ACL 文件、创建 ACL lease、解码资源或复制 ACL 诊断。
- **当前提交与真实接口**：`HEAD` 与当前 `main` 的源码树没有 `CharacterAcl*`、`Runtime/Character/Pipeline/Animation/ACL` 或 `Contracts/ACL` 可直接消费路径；ACL provider 分支 `codex/acl` 的当前 tip `eb3393ae225b775634970df555bf3a00674559e3` 不在当前分支祖先中。该事实只说明本批次不能认领 ACL provider 交付，不表示要在预览侧补一套实现。
- **证据**：`git ls-tree -r --name-only HEAD` 与 `git ls-tree -r --name-only main` 的 ACL 路径检索无当前可消费结果；`git merge-base --is-ancestor eb3393ae2 HEAD` 返回 false。现有窗口输出使用 `AnimationPresentationRuntimeSnapshot`，没有 ACL resource/lease API 调用。
- **影响清单**：若 6.9、8.1–8.2 或 11.2 要求展示 ACL resource identity/quality，当前只能报告未接入；本批次不把它误标为完成。
- **已经尝试的正式处理**：保留 Animation Presentation/Slot 作为唯一表现观察源，不从 ACL 文件名、数据库字节或当前 Selection 推断运行资源。
- **仍需提供**：ACL owner 需要发布正式 resource identity、版本/质量结果、lease/ready 状态及与 Presentation source 的只读关联；协调窗口需要确认哪些字段属于预览验收。
- **业务取舍**：等 ACL 正式发布能保证采样资源和运行 source 一致；预览直接读 ACL 资产虽能显示更多字段，但会绕开 resource lease/发布索引并制造第二个资源判断，按设计不采用。

## ISSUE-SCENEPLAY-006：整体审查、正式发布检查和用户验收尚未完成

- **当前状态**：本批次有直接 Unity C# 编译成功日志和静态引用检查，但没有新的 Center `Completed` Run、完整角色/技能端到端输入回放、非 Skill 正式样例发布、Document v7 校验或用户验收。当前任务仍不能写 `REVIEW_RESULT: APPROVED`。
- **已有正式证据**：当前批次 Unity 命令、项目路径、源码身份和日志已在本节记录；`git diff --check` 和工作树状态可复核。旧 Center Run、取消后仍为 Running 的 staging、旧源码日志均没有被复用为当前提交通过证据。
- **影响清单**：8.1–8.5、11.2、11.5，以及整体 `REVIEW-SCENEPLAY` 交付门禁。
- **仍需提供/执行**：待正式技能输入、非 Skill owner、v5、Camera/ACL 公开合同进入当前可编译主线后，按适用范围分别运行 Build/Store/场景/来源校验与 Center Run；用户仍需在主验收 Editor 做真实 Scene Play、Pause/Resume/Reset/Stop 和作者观察验收。
- **业务取舍**：现在声明整体通过会把“代码编译成功”误报成“正式运行链和资源发布完成”；保留分层证据可以明确当前能用的部分，也让后续失败归因到真实 owner，而不是回退已正确的代码。

### 本批次不作为问题的项目

- `AnimationPreviewRuntime` 仍被 `MotionMatchingQueryFixture` 用于独立 Pose/MM 查询，属于设计明确保留的模块诊断/查询用途；其原有 Timeline 采样、独立 Action command 和 seek/reset 逻辑已删除，不是完整角色播放器残留。
- 旧 RootMotion/FinalIK 缺失问题不作为本批次新的阻塞；当前直接 Unity 日志没有本批次新增 CS error，已有 warning 已按上文记录。
- `D:/Unity_Project_1/3C/docs/coordination-progress.md` 是协调窗口独占文档，本批次没有读取后写入、没有复制到功能提交，也没有通过其它窗口传递消息。

## 2026-09-09 ProgramEpoch、后台构建与历史投影增量

- `7d07f3042`：在不改变 Session/World/Clock 身份的前提下，把 `ProgramCatalogHash` 从 Composition 身份中分离，加入 `SimulationProgramEpoch`、公共 Program binding 和 Adoption result 合同。Float32/Fixed 的 Kernel、Program Runtime、Program Runtime Port、World State Store、Pipeline Transaction 和 Runtime Handle 都支持在 Logic Tick 边界替换 Program；Actor、World binding、Numeric Profile、Operation Set 或 `LayoutHash` 改变时拒绝采用，旧状态不被清空。
- `b409a5c98`：Session Host 增加 ProgramEpoch 排队和边界提交；角色 Registration 先保存 pending Program，Runtime adoption 成功后才一起提交 Program、ProgramIdentity、SourceMap revision 和诊断 SourceMap，失败则丢弃 pending。`CharacterPipelineHost.TryQueueCurrentProgramAdoption` 是正式入口。
- `b409a5c98`：增加 `CharacterSimulationBackgroundBuildService`。作者图/Timeline 发现和 Semantic IR 快照仍在 Unity 主线程完成；快照后的 Target Program lowering 通过 `Task.Run` 在后台执行；Unity wrapper、Semantic IR、AssetDatabase 发布和同 Session adoption 回到主线程。`BtsmtlScenePlayPreviewCoordinator.TryQueueBackgroundProgramBuild` 接入 Play 中的轮询发布，不退出 PlayMode。
- `b409a5c98`：Runtime execution history 在已有 Logic Tick 事件分组上增加真实 `SimulationStatePublished/SimulationRestore` checkpoint 引用；某 Tick 没有状态发布事件，历史被标记为不能完整恢复，不把事件列表冒充可恢复状态。

本增量已经闭合“恢复 checkpoint 并在同一 Session 继续运行”的 `ResumeFromTick` 命令：恢复复用现有 SnapshotCodec 和 Runtime Handle，不创建新 Session，并生成新的执行分支身份；Timeline Live Debug 也已经显示真实总执行区间、Tick 数和 checkpoint 数。`ReplayRange` 仍未接入完整的录制输入/外部结果重放，因此不能把当前 ResumeFromTick 误称为完整历史 Replay。

当前 DebugControl 已消费本地 Fixed/Float32 Session 的 canonical input trace capability：可开始/停止录制，并从已完成 Trace 选择区间、恢复同一 Session checkpoint 后启动对应 Target 输入重放；外部结果和 PresentationFrame 的完整重放材料仍未统一接入，不能把当前输入 Replay 扩大描述为完整历史 Replay。

`CharacterSimulationBackgroundBuildResult` 现在保存每个 Target 的 ProgramId、SourceRevision、SemanticHash、ProgramHash、LayoutHash、SourceMap entry 数量和 Presentation ContractHash，作为 publication/adoption 前的正式身份报告材料；运行时仍只通过现有 Session adoption barrier 采用，不创建第二个 Program 来源。

- `7f90dc5ca`：把 Fixed 输入录制/区间重放放在公共 Session replay capability 后面，由 Fixed Runtime Handle 消费既有 canonical trace；通用 Client Runtime 不引用 Fixed target 类型，失败时保留当前 Session 状态。
- 当前未提交增量把同一 capability 扩展到 Float32 Local Input Source；两套 Target 都在输入读取边界记录/重映射 canonical input，外部结果仍必须由对应正式 owner 提供。

- `f3bfbcad4`：Runtime Debug Session 按事件携带的 `ProgramRevision` 保存和选择对应 SourceMap；ProgramEpoch 采用后刷新当前 Provider，旧 Capture 不再被新 Program SourceMap 解释；Debug target key 不再把可变 ProgramCatalogHash 放入稳定目标身份。

## 主线合入选择

本次不把预览分支整体合入主线。主线当前已经有自己的作者迁移流程，且工作区存在用户未提交修改；合入只能按下面的公共能力逐个摘取。

| 顺序 | 提交 | 选择 | 业务范围 | 合入条件 |
|---|---|---|---|---|
| 1 | `fd3f6bc9d` | 直接合入候选 | Fixed/Float32 程序对完整 SourceMap 字段做确定性排序，保证 Program 编解码后的 Hash、来源索引和 TreeClip 调用映射稳定 | 不带任何预览资产；主线先保留当前源码修改，再摘取这两个运行时文件 |
| 2 | `ac34da64c` | 直接合入候选 | 将已有 Runtime Trace 投影为按真实执行事件生成的 Graph、Node、State、Timeline、TreeClip、等待和分支区间 | 依赖主线已有 RuntimeDebugSession、RuntimeDebugViewModel、RuntimeTrace 和 SourceMap 合同；不依赖 Corin 资产 |
| 3 | `b9acb5a87` | 与上一个连续合入 | 按 Logic Tick 聚合已记录事件，输出技能历史 Tick 记录、事件保留状态和捕获完整性 | 必须在 `ac34da64c` 后合入；它扩展同一个 RuntimeExecutionTimeline 文件 |
| - | `8996c18ff` | 可选文档摘取 | 预览热替换、动态时间轴、历史查看和恢复/回放的方案对账 | 只在主线要同步方案文档时摘取，不作为代码前置 |

以下内容不直接摘取到主线：`63c8420b8`、`4cfbaea43` 的 Corin Native TreeClip 迁移和生成资产；`651fbf8b8`、`fdebc1345`、`12e312d2a` 的 Corin 技能资产、预览产物和生成产品；以及场景、Prefab、Scene Play 协调器和 Timeline 编辑器整段预览提交。主线已有 `BtsmtlSkillLegacyMigrationWorkflow`，Native TreeClip 应由主线正式迁移流程重新发布，不能把预览生成物当成迁移合同。

主线工作区清理并确认用户修改归属后，建议执行：

```text
git cherry-pick fd3f6bc9d
git cherry-pick ac34da64c
git cherry-pick b9acb5a87
```

这三笔只提供公共运行时 SourceMap/诊断能力，不等于完整预览交付；完整预览仍需要主线后续完成 ProgramEpoch、Play 内 Build/adoption、历史恢复/回放控制和 UI 接线。

静态三方合并检查结果：`fd3f6bc9d` 单独落到 `main` 无冲突；在其结果上落 `ac34da64c` 无冲突；在前两笔结果上落 `b9acb5a87` 无冲突。直接把 `b9acb5a87` 落到未包含 `ac34da64c` 的主线会产生预期的“新文件修改/删除”冲突，因此不能跳过 `ac34da64c`。

## 新增公共候选与预览候选分层

后续提交不改变前面的摘取顺序判断，新增选择如下：

| 提交 | 选择 | 原因 |
|---|---|---|
| `7d07f3042` | 主线公共候选，优先摘取 | 只改 Simulation Core 的 ProgramEpoch、Catalog/Kernel/State/Transaction/Runtime Handle；保持 Session identity 稳定，适合独立作为公共运行时合同 |
| `034c762e4` | 主线诊断候选，接在 `ac34da64c`、`b9acb5a87` 后 | 修正执行历史将 Lifecycle 的 StatePublished 纳入 Tick 分组，保证 checkpoint 引用来自真实状态发布 |
| `f3bfbcad4` | 主线诊断候选，接在 `ac34da64c`、`b9acb5a87`、`034c762e4` 后 | ProgramEpoch 变化时刷新 Debug Provider，按事件 ProgramRevision 选择旧/新 SourceMap，并保持 Session Debug target key 稳定 |
| `b409a5c98` | 不直接摘取 | 包含 Scene Play coordinator、后台 Editor Build、角色 Registration/诊断切换和对 `7d07f3042` 的增量；属于预览接入闭包，主线需要时应按正式作者/预览 owner 重新拆分 |
| `db6c1cb71` | 不直接摘取 | 混合了公共 Float32/Fixed checkpoint 实现、SimulationSessionHost 和 DebugControl；主线若已接受 `7d07f3042`，应再按主线的 Session/Debug owner 逐文件对账 |
| `6640c5eb4` | 预览专属 | Timeline Editor 的执行区间、Tick、checkpoint 展示和执行分支状态，不是公共 Simulation Core |
| `411fe00d9` | 可选文档/任务摘取 | 只更新本 change 的过期 SourceRevision 处理和任务勾选 |

因此，新的公共主线候选最小顺序是：`7d07f3042`；若主线需要执行历史投影，再接 `ac34da64c`、`b9acb5a87`、`034c762e4`、`f3bfbcad4`。`b409a5c98`、`db6c1cb71` 和 `6640c5eb4` 不作为整笔主线 MR。

## 2026-09-09 表现帧历史分层修正

之前 `RuntimeExecutionTimelineBuilder.BuildHistory` 为了得到 Logic Tick 历史，直接跳过了 `Presentation` 域事件。因此窗口虽然能从独立的实时诊断事件看到当前表现，却不能从 `RuntimeExecutionHistory` 取到已记录的表现帧；这与“历史查看只读实际记录”的合同不一致。

本次预览增量已修正为两条明确的历史序列：

- `RuntimeExecutionTickRecord` 只保存 Logic/Lifecycle 事件，保留 `SimulationTick`、`StatePublished`、checkpoint 以及 Network Model 结果标记。
- `RuntimeExecutionPresentationFrame` 按实际 `PresentationFrame` 位置保存该帧的全部已捕获表现事件；只有存在 `PresentationInterpolated` 事件时才标记 `IsAvailable`。它不把渲染帧错误映射成 Logic Tick，也不从当前 Timeline 重新采样。

Timeline 窗口现在显示逻辑 Tick、checkpoint、已捕获表现帧和是否出现外部 Network Model 记录。该修正只完成“历史查看材料被保留”的边界；PresentationFrame 的运行时恢复、外部结果注入和同 Session 完整 Replay 仍未完成，`4.9` 继续保持未勾选。

当前正式 Presentation runtime 在每个已提交表现帧发布已有 Action lifecycle snapshot；当历史选择绑定 `ActionInstance` 时，投影会用 ActionInstance 和同一 PresentationFrame 位置关联 `PresentationInterpolated` 事件。因此历史可以定位实际释放的表现帧；这仍不是表现状态 restore/replay，缺少表现记录时仍显示 unavailable。

每个 `RuntimeExecutionTickRecord` 现在还直接暴露该 Tick 已记录的 `SimulationNetworkModel` 外部结果事件，History 总览显示其数量。它复用现有 Runtime Trace，不新增录制格式；外部结果的 restore/replay 注入仍由正式 owner 提供，缺少注入合同时恢复继续仍不可用。

Float32/Fixed `ReplayRange` 现在在正式 Pipeline 包含 `ExternalSource` Pass 时直接拒绝，并报告需要外部结果 journal；不会恢复 checkpoint 后偷偷用实时外部输入继续。这只收紧了错误路径，Local 无外部 Source 的输入 Replay 行为不变。

Debug capability 的 `SupportsReplayArtifact` 已不再把输入 Trace capability 当作完整 Replay artifact；当前仍可报告和执行局部输入 Replay，但完整历史 Replay capability 明确为未支持。

## 2026-09-09 Replay 分支归属修正

此前 `SimulationSessionDebugControl` 的 `ReplayRange` 会让 Float32/Fixed Runtime Handle 直接恢复 checkpoint 并准备输入 Trace，但 `SimulationSessionHost` 没有在成功后提交 `ExecutionBranch`，也没有按恢复 Tick 裁剪后续 checkpoint。这会让状态看似恢复，调试状态却仍停留在旧分支。

当前链路调整为：Runtime Handle 仍负责正式 Snapshot Restore 与目标输入 Trace 准备；Host 在 Handle 成功返回后统一登记 `ParentExecutionBranchId`、新 `ExecutionBranchId`、`ExecutionBranchBaseTick`，并删除恢复点之后的可写 checkpoint。旧 Capture 历史仍只读。`ResumeFromTick` 与 `ReplayRange` 现在共用同一分支提交逻辑。

这只修正了现有本地输入 Replay 的分支一致性；外部结果日志、表现状态恢复和完整技能 Replay 材料仍未接入，不能将它描述成完整历史重放。

本次又修正了 Replay 的执行语义：Float32/Fixed Local Schedule 会根据 canonical Trace 输入把 `SimulationPipelineStepProvenance.ExecutionKind` 标成 `Replay`，并把 Trace identity 写入 `BaselineIdentity`；普通控制输入仍标成 `Forward`。因此 Runtime Trace、Pipeline diagnostics 和后续历史投影可以区分真实重放步骤与继续实时推进。

同时修正历史 SourceMap 的严格版本选择：当执行事件携带的 `ProgramRevision` 不在已保存的历史 SourceMap 集合中时，Timeline 投影现在显示 `unmapped`，不再退回当前 Program 的 SourceMap。这样旧 ProgramEpoch 的 TreeClip、Timeline 和 FlowCanvas handle 不会被新产物误解释；缺失版本仍只允许查看事实，不能假定来源映射存在。

时间轴选择过滤也改为按运行实例闭包工作：Timeline Playback 会带上同一 PlaybackId 的 TreeClip，并继续带上这些 TreeClip 产生的 Graph/Node runtime；TreeClip 选择保留其 Cycle 和 Graph runtime；ActionInstance/SkillExecution 使用 ActionInstance、CallSite 与 InvocationGeneration 关联子事件。过滤不按名称、路径或 operation index 猜测，也不会把其它 Playback 或并发 Action 混入当前记录。

按局部运行实例构建 History 时，投影现在会在所选实例的真实 Logic 区间内补回 Session 的 `SimulationTick`、`SimulationStatePublished`、`SimulationRestore`、`SimulationNetworkModel` 和 `SimulationFailure` 边界事件。因此局部 Timeline/TreeClip History 不会因为实例过滤而被错误标成缺少完整 Tick；这些边界来自已记录 Runtime Trace，不会推进或重算业务状态。

Replay capability 现在公开真实 `IsInputRecording` 状态，Float32/Fixed Trace Module、Runtime Handle、Session Host 和 Debug Status 使用同一状态源；窗口不会再把录制状态写死为未录制。

## 后续提交的主线摘取建议

在既有 `7d07f3042`、`ac34da64c`、`b9acb5a87`、`034c762e4`、`f3bfbcad4` 候选之后，本轮新增提交按职责拆分如下：

| 提交 | 选择 | 说明 |
|---|---|---|
| `03203b6bb` + `86b353692` | 主线诊断候选，按文件摘取 | 历史 SourceMap 缺失时保持 unmapped，并按 Playback/TreeClip/Action runtime identity 投影事件闭包；依赖已有 RuntimeExecutionTimeline 与 revision SourceMap 历史。 |
| `f91cc78d1` | 主线 Target 候选，按文件摘取 | Float32/Fixed Local Schedule 将输入 Trace 标记为 Replay provenance；只适用于主线已接受对应输入 Trace 合同。 |
| `90cd127fa` | 主线公共候选，按接口摘取 | 公开 Float32/Fixed 输入录制状态并贯通 Host/Debug Status；不包含预览窗口资产。 |
| `de5edf4f4` | 预览/Session owner 候选，不整笔摘取 | ReplayRange 成功后由 Session Host 登记 ExecutionBranch；需要主线 Session Debug owner 重新对账。 |
| `29a7ba4cf` | 诊断候选，按文件摘取 | 保留实际 PresentationFrame 历史并在 Timeline 窗口显示；不提供表现状态恢复。 |
| `37dbac100`、`92e8bb84a`、`5d1e499f0`、`dbb89640b` | 文档候选 | 只更新本 change 的验证边界、TreeClip 静态盘点和当前审查结论，不是运行时前置。 |

这些提交不能整体替代主线 owner 的 Session/Preview 合并；主线工作区仍需先清理用户修改，再按文件和接口顺序做三方对账。

## 2026-09-09 验证记录

- OpenSpec：`openspec validate design-btsmtl-authoring-runtime-workbench --type change --strict --json --no-interactive` 通过。
- 静态：`git diff --check` 通过，预览 worktree 当前 clean；当前功能提交最新为 `90cd127fa`，后续提交只更新审计、TreeClip 盘点和 review 文档。
- Unity：手动 batch `btsmtl-preview-replay-compile-20260909.log` 曾只记录到 Package Manager 注册完成，随后进程无进一步输出并被停止；该次不作为编译证据。此前正式 Center Run `c4c620ae0c4d4712812d591f0c5d3d56` 完成 `compile`。新增 Target identity report 初次编译由 `47aebafc4c664564832734ea8b2654ad` 抓到类型错误，已由 `af27a0aba`、`37a091e99` 修复；正式 Run `3df678e58c9f466ab93a4600ca5d5aad` 完成，Unity 日志出现 `*** Tundra build success` 并正常退出，未检出 `error CS`。局部 History 边界修正后的正式 Run `95e50007eb5d4d2381d4e4c612b22137` 同样完成并出现 `*** Tundra build success`。PresentationFrame Action snapshot 关联的 Run `3911cace9846414695b4a91abc42999f` 出现 `*** Tundra build success`、无 `error CS`，但 Unity 在 assembly reload 后未自行退出，已停止我启动的 batch 进程；其后的 `dc8472e1a` 只调整既有 TimelinePlayback identity 到同一 PresentationFrame 的选择闭包，尚未获得独立正式编译结论。直接 batch 也再次卡在 Package Manager 注册后，无新 CS 结论。RunHost 没有生成额外结果文件，因此这里只认定已编译部分完成，不扩大为当前最终 HEAD 完整业务通过。
- Center：继续使用 `79a0588a3f9047b4889ac40e9e698cc9`，已写入上述提交范围、验证缺口和未闭合项；没有新建第二条 Change 记录。

静态 TreeClip 盘点已写入 [treeclip-migration-audit.md](treeclip-migration-audit.md)：旧来源实际是 RootTree 19 个加 shared Attack1 Timeline 4 个，新目标为 7 条 Native Timeline 的 23 个。数量一致，但稳定 clip identity 和逐项行为映射证据缺失，`11.6` 仍未完成。

## 2026-09-10 SourceMap 所属 Graph 映射修正

此前 `CharacterRuntimeDebugProgramBuilder` 将包含 `GraphId` 的 Timeline、Track、Clip 和 TreeClip 来源转换成 `RuntimeSourceElementKey` 时丢弃了所属 Graph。新 Native SkillGraph 的运行事件因此无法从 SourceMap 精确回到技能 Graph，导航会继续尝试旧 RootTree 路径；这不是预览过滤，而是来源身份在桥接层被截断。

当前修正为：

- `RuntimeSourceElementKey.Timeline`、`Track`、`Clip` 和 `TreeClip` 保留 `GraphAuthoringId`；同一 Timeline 被多个 Graph 使用时，Graph/Timeline 联合身份不再合并。
- Runtime Debug Program 在创建 Timeline/Track/Clip 容器、计算内容 hash 和解析实际来源时都使用同一联合身份。
- Native SkillGraph 导航遇到 Timeline、Track、Clip 或 TreeClip 时，按所属 Graph 中唯一的 `BtsmtlSkillTimelineFlowNode` 打开正式 Timeline，再按 Track/Clip AuthoringId 定位；候选不唯一时拒绝导航，不按名称或数组位置猜测。
- Timeline 编辑器从已有 `BaseTreeWindow.Tree.GraphAuthoringId` 构造调试目标请求，因此从技能 Graph 打开的窗口可以命中同一 SourceMap 身份；独立 Timeline 没有 Graph 上下文时仍保持空 Graph 身份。

提交 `58933de7d` 完成运行时来源和 Native 导航，`36f79dd93` 修正 Timeline Editor 程序集边界。受影响的 `BTSMTL.Timeline.Tree.Editor` 使用正式参数构建结果为 0 警告、0 错误；前一次 `ThirdPersonClient.Runtime` 构建也已成功。Center 仍保留陈旧 `3911cace9846414695b4a91abc42999f` 的 Running 记录，记录的 Host/Unity 进程已不存在，官方 recover 无法接管，因此当前提交尚未取得新的 Center Run 证据。

## 2026-09-10 History checkpoint 真实性修正

此前 History 投影把每个 `SimulationStatePublished` 或 `SimulationRestore` 事件都包装成 checkpoint，但 Session Host 实际只按 30 个 Logic Tick 的间隔和最多 32 个点保存 Snapshot。这样的列表不能直接作为恢复请求来源。

当前 `SimulationSessionHost` 在正式 Snapshot Codec 捕获成功后，通过 `ISimulationCheckpointTracePublisher` 向每个 Actor Diagnostics 发布 `SimulationCheckpointCaptured`，携带 Tick、SnapshotId 和 SnapshotHash。`RuntimeExecutionHistory` 只从这个事件生成 checkpoint；`SimulationStatePublished`、`SimulationRestore` 仍保留在 Tick 历史中作为状态边界，不再冒充可恢复点。局部 Timeline/TreeClip History 将 checkpoint 作为 Session boundary 补回，所以选择局部实例仍能看到同一次 Session 的真实稀疏恢复点。

该接口已贯通 Character、Fixed、DeterministicRollback 和 Server Authoritative Actor registration，未增加第二份快照或每 Tick 全量保存。提交 `a1369e6cb` 完成代码和审计更新。`ThirdPersonSimulation.ServerAuthoritative.Unity` 与 `ThirdPersonClient.Editor` 的本地正式参数构建均无新增错误；后者保留既有第三方/旧代码警告。Center 的陈旧 `3911` 仍未被官方 recover 清理，因此这两次本地构建不能冒充新的 Center Run。

## 2026-09-10 Capture 范围与历史 SourceMap 入口统一

Timeline 窗口的完整 Capture 原先只订阅 Timeline、Animation 和 Motion，不能生成包含 Graph、分支、Blackboard、Action 和 Network 边界的总执行时间轴。当前按钮改为使用已有 `RuntimeTraceChannel.All` 和 `Continuous` detail；容量仍由既有 Runtime Capture Store 限制，发生 eviction 继续通过 `IsComplete`/`EvictedEvents` 暴露。

`RuntimeExecutionTimelineBuilder` 的无字典入口也已改为严格版本匹配：`RuntimeDebugSourceMapSnapshot` 保存自己的 `RuntimeProgramRevision`，事件版本与快照不相等时不解析 SourceHandle。这样当前窗口入口和其它诊断调用入口都不会用当前产物解释旧 Program 的 Timeline、TreeClip 或 FlowCanvas 节点。提交 `fa387235c` 完成 Capture 范围修正；后续严格 SourceMap 收紧已在当前提交中本地验证。

## 2026-09-10 Play 内 Timeline 编辑、Build 与 adoption 入口

此前 Timeline 工作区在 Scene Play 运行时把 authoring mutation 全部置为只读，后台 Build 服务虽然已经存在，但作者没有从窗口触发它的正式入口。这样不能完成“运行中修改 Timeline，Build 新 Program，保持当前 Play，再在 Logic Tick 边界采用”的工作流程。

当前提交 `b456e03e6` 完成以下接线：

- `TimelineEditorWorkspaceView.CanEditAuthoringMutation` 在 Running/Paused 时开放正式 Timeline mutation；Building、Resetting、Stopping 和 Live Debug 仍保持只读。
- 顶部新增 `Build` 按钮，要求作者先在当前 Scene Play context 中明确选择一个 Skill/Actor；不从多 Actor 或其它场景猜测目标。
- `IBtsmtlScenePlayPreviewOperations.Build(actorId)` 进入唯一协调器。协调器将状态切到 `Building`，保留原来的暂停意图，并复用 `CharacterSimulationBackgroundBuildService`。
- Build 期间 Semantic snapshot 在 Unity 主线程完成，Target lowering 使用既有后台任务；完成后回到主线程发布并调用 `CharacterPipelineHost.TryQueueCurrentProgramAdoption`。旧 Program 在此期间继续运行，adoption 仍由 Session Host 的 Logic Tick barrier 执行。
- Build 失败或 publication/adoption 排队失败时，协调器恢复原 Running/Paused 状态并显示 Product 阶段错误，不停止 Play，也不替换旧 Program。Build 成功时显示已发布且等待下一个 Logic Tick adoption 的状态。
- 停止、请求丢失或场景重载会清除协调器对未完成任务的引用，后台结果不能再绑定到已失效的 Scene Play 请求。

本步本地验证：

- `dotnet build 3cDemo/Client/3C_Client/ThirdPersonClient.Editor.csproj --disable-build-servers /nr:false /p:UseSharedCompilation=false` 成功，0 warning、0 error。
- 已执行 `dotnet build-server shutdown` 清理 MSBuild/C# Host。
- `git diff --check` 通过。

这一步只闭合“角色技能在 Play 内编辑并触发 Build/adoption”的入口，不等于完整恢复/回放、非 Skill owner、v5、Camera/ACL 或 Corin TreeClip 逐项迁移已经完成；对应未完成项仍保持未勾选。

## 2026-09-10 Tick 历史运行身份与状态材料

之前 Runtime Trace 的事件本身带有 SessionId 和 ProgramRevision，但没有带正式 Session 当前的 ProgramEpoch；边界事件的 CharacterStateHash/WorldHash在 Detail 文本中，输入序号主要放在通用 Value 中。这样历史虽能查事件，却不能直接按 Tick 对账运行版本、输入和状态 hash。

提交 `323db4d57` 完成正式运行身份贯通，提交 `0493a549a` 补上 checkpoint 失败可见性：

- `RuntimeDiagnosticsContext` 接收 Session Host 在初始准备和 adoption 后绑定的 ProgramEpoch；`RuntimeTraceEvent` 每次发布都携带该 Epoch。
- `ISimulationActorRegistration` 增加强制 `BindProgramEpoch` 合同，Character、Fixed、DeterministicRollback 和 Server Authoritative registration 全部实现；没有增加可选 fallback。
- `RuntimeTracePayload` 增加 typed `InputSequence`、`CharacterStateHash` 和 `WorldHash`；Float32/Fixed diagnostics adapter 从正式 `SimulationModelTraceRecord` 与 `SimulationBoundaryTraceRecord` 填入，不再要求历史解析 Detail 字符串。
- `RuntimeExecutionTickRecord` 直接汇总 Session、ProgramRevision、ProgramEpoch、Character runtime、RuntimeInstance、Skill、Action、CallSite、输入序号和 state/world hash，同时保留原始事件与外部结果事件。它只投影已有 Capture，不新增每 Tick 快照。
- checkpoint 捕获失败不再静默吞掉；Session Host 只在所有 Actor checkpoint 诊断发布成功后登记可恢复点，并通过现有 Debug Status 的 `LatestCheckpointFailure` 显示失败原因。

本步本地验证：`ThirdPersonClient.Editor.csproj` 按项目要求构建成功，0 error；本轮显示的 90 个 warning 均来自已有第三方/旧代码字段，随后已执行 `dotnet build-server shutdown`。`git diff --check` 通过。当前仍没有新的 Center Run，因此不把本地构建扩大为 Unity Scene Play 业务通过。

## 2026-09-10 Scene Play 同 Session 恢复与输入 Replay 入口

提交 `a14eedda4` 将正式 Session 的恢复控制暴露给 Scene Play 预览合同：

- `IBtsmtlScenePlayPreviewOperations` 新增 `ResumeFromTick` 和 `ReplayInputRange`，协调器只接受 Running/Paused 的当前请求，并使用当前 Context 的唯一 `SimulationSessionHost`。
- `ResumeFromTick` 直接调用 `SimulationSessionHost.TryRestoreCheckpoint`；`ReplayInputRange` 直接调用 `TryReplayInputRange`。checkpoint、Snapshot Codec、输入 Trace 和 ExecutionBranch 仍由 Session Host/Runtime Handle 所有，协调器不写 Simulation State、不调用业务 Advance。
- 两个操作成功后保持原 Session 和 Scene generation，解除 Unity Pause 并回到 Running；Host 已提交新的 ExecutionBranch，原 Capture 历史保持只读。区间无效、checkpoint 缺失、没有输入 replay runtime 或 ExternalSource 不能重放时，返回明确失败，不降级成移动 Timeline 时间或重新执行作者资产。

本步与现有 `SimulationSessionDebugControl` 共用同一 Host 入口；本地 `ThirdPersonClient.Editor.csproj` 构建成功，0 error，随后执行 `dotnet build-server shutdown`。仍未有新的 Center Run 或 Scene Play 端到端证据，因此只登记接口接线，不宣称恢复业务验收完成。

## 2026-09-10 Build/adoption 结果可见性

提交 `bdb855259` 补齐 Play 内 Build 的只读结果状态：

- `BtsmtlScenePlayBuildStatus` 暴露 Building、Published、Adopted、Failed，以及 Actor、SourceRevision、每个 Target 的 Program/Semantic/Layout/SourceMap/Presentation Contract 身份。
- Build 完成后先进入 Published，协调器用本次 adoption 请求的 `Requested.Value` 和 SourceRevision 双重关联 `SessionHost.LastProgramAdoption`；只有这一次 Build 对应的 Epoch 被正式采用才进入 Adopted，不能复用上一次同 revision 的结果。
- Build 失败、publication 失败、Session 没有返回 adoption 请求或 adoption 被拒绝时进入 Failed；旧 Program 和当前 Play 状态仍保持不变。
- Timeline 工作区状态栏显示 Build 阶段、Actor、Target 数量及首个 Target hash；完整 Target 列表通过正式只读合同保留，未把产物身份写入 Document。

本步本地 `ThirdPersonClient.Editor.csproj` 构建成功，0 error；本轮 90 个 warning 均为既有第三方/旧代码 warning，随后已执行 `dotnet build-server shutdown`。当前仍无新的 Center Run，因此只确认程序集和状态接线。

## 2026-09-10 ExecutionBranch 历史隔离

提交 `cc4fac601` 修正同 Session 恢复后历史分支的诊断身份：

- `RuntimeDiagnosticsContext` 由 Session Host 绑定当前 ExecutionBranch；每个 RuntimeTraceEvent 都携带 Branch identity。
- `ISimulationActorRegistration` 强制提供 Branch 绑定，初始 Session 和恢复/输入 Replay 创建新 Branch 后，Character、Fixed、Rollback 和 Server Authoritative registration 都同步更新。
- `RuntimeExecutionTickRecord`、`RuntimeExecutionCheckpoint`、`RuntimeExecutionSpan` 和 `RuntimeExecutionPresentationFrame` 暴露 Branch；PresentationFrame 按 `Frame + Branch` 分组，避免恢复后相同渲染帧互相覆盖。
- Span 配对键加入 Branch，旧分支未闭合的 Graph/Timeline/TreeClip 不会与新分支的同一 SourceHandle/RuntimeInstance 拼成一段；LiveState 等价判断也把 Branch 纳入比较。

本步本地构建已通过，0 error，并执行 `dotnet build-server shutdown`。这只修正历史数据不混支，不补齐外部结果或表现状态 replay；仍需 Scene Play 端到端验证。

## 2026-09-10 Timeline 历史查看与恢复控制分离

提交 `4f97e69a0` 将恢复控制接入 Timeline Editor 的 Live Debug 工具栏：

- 原有 History slider 继续只调整 `RuntimeDebugSession` 的 history cursor，读取已记录的 Capture，不改变 Session、World、Clock 或 Program。
- 新增 Restore Tick、Replay From/To 字段以及 Restore/Replay 按钮。Restore 只允许提交已记录的 checkpoint Tick；Replay 直接提交明确的输入 Tick 区间。
- 两个按钮调用 `IBtsmtlScenePlayPreviewOperations.ResumeFromTick` / `ReplayInputRange`，成功后让 Debug Session 回到 Live 观察新 Branch；不在 Timeline Editor 内重建状态或调用业务 Advance。
- 没有 checkpoint、Scene Play 不可控、区间非法或正式 Runtime 不支持对应能力时，按钮不可用或显示 Host 返回的失败原因；不会把拖动 history slider 误当成恢复。

本地 `ThirdPersonClient.Editor.csproj` 构建成功，0 error，随后执行 `dotnet build-server shutdown`。当前仍缺少新的 Center Run 和 Unity 端到端恢复画面证据。

## 2026-09-10 Program 兼容性失败不再击穿 Session

复核发现 Float32/Fixed Runtime Handle 在 `AdoptProgramEpoch` 内遇到 LayoutHash、ABI、Roster 或其它候选绑定不兼容时会抛 `InvalidOperationException`。这类结果属于合法的候选拒绝，不应把当前正在运行的 Session 置为 Faulted。

提交 `15fef9fb7` 在 Float32/Fixed Handle 的正式 adoption 边界将该异常转换为 `SimulationProgramAdoptionStatus.Rejected`，保留当前 Epoch、Program、World 和 Clock；Session Host 随后丢弃 pending binding，BuildStatus 显示拒绝原因。没有借此伪造结构不兼容的“下一次 Action 自动采用”，因为当前 Runtime Program Catalog 尚不支持同一 Actor 同时持有不同 Layout 的双版本执行；该能力仍由上游多版本 Program 合同提供。

本步 `ThirdPersonClient.Editor.csproj` 构建成功，0 warning、0 error，并执行 `dotnet build-server shutdown`。这只修正失败归属，不扩大当前热替换能力边界。

## 2026-09-10 历史分支与 SourceMap 快照收口

提交 `2ecdca5f7` 收口了历史诊断中两个容易互相污染的边界：

- `RuntimeTraceEvent`、`RuntimeExecutionSpan`、`RuntimeExecutionTickRecord`、`RuntimeExecutionCheckpoint` 和 `RuntimeExecutionPresentationFrame` 使用明确的 `ExecutionBranchId`。Session restore 或输入 Replay 切换分支时，Diagnostics Context 清掉旧 live state；Span 配对、Tick 聚合和 PresentationFrame 分组不会把相同的 RuntimeInstance、Tick 或 Frame 跨分支拼接。
- `RuntimeDebugSession` 保存每个 `ProgramRevision` 的 `RuntimeDebugSourceMapSnapshot`。Program 热换时先从旧 Provider 保存旧 revision 的快照，再登记新 Provider 的快照；历史 Capture 按事件自己的 revision 解析来源。若对应快照或 handle 已不存在，历史事件仍显示为 `unmapped`，不会拿当前 Program 的 Graph、Timeline、TreeClip 来源解释旧事件。
- Native 来源导航也按事件 revision 查询保存的 SourceMap，只有来源 key 与历史事件实际解析结果一致时才打开作者 Graph/Timeline；它不会因为当前目标仍在线就把旧事件强行导航到新资产。

本步 `ThirdPersonClient.Editor.csproj` 按项目要求构建成功，0 错误、35 个既有 ACL/旧代码字段警告；随后已执行 `dotnet build-server shutdown`。`git diff --check` 通过。该修正只保证历史事实的版本和分支隔离，不补齐外部结果 journal、Presentation/Camera restore，也不替代 Corin TreeClip 逐项行为对账或 Unity Scene Play 端到端验收。

## 2026-09-10 TreeClip 迁移身份门禁

提交 `82fdbf09d` 修正了 Corin TreeClip 迁移器原先“数量相等且已有 Native 图就认为迁移完成”的判断：

- 旧片段先读取 ActionWindow 类型、ID、Digest、Blackboard 作用域/生命周期/分类、写入值、完整时间范围、缓动参数、ClipIn 和执行阶段；同一 Timeline 内出现重复业务键会直接失败。
- 已存在的 Native TreeClip 必须按同一业务键逐项匹配，不能按旧/新数组下标对应；Native 图还必须通过正式 Skill Graph closure，只有一组 TimelineBody 生命周期锚点、一个 Boolean Blackboard setter，以及 Root 到 setter 的正式 Flow 连接。
- 新创建的 TimelineBody 图不再使用随机 graph identity，而是由旧 Timeline/TreeClip 与 ActionWindow 身份计算稳定 hash；现有已生成资产不被这次代码改写，避免无授权地改变已发布来源 identity。
- 当前 23 条静态对应关系保存在 [treeclip-migration-map.json](treeclip-migration-map.json)，包含旧/新 Timeline、Track、Clip、TreeGraph 身份和时间范围。该文件明确把行为 digest、运行时 Decision/Commit、SourceMap coverage 和输入回放标为未验证，因此 `11.6` 仍未勾选，也没有把迁移数量相等写成行为等价。

本步 `ThirdPersonClient.Editor.csproj` 按项目要求构建成功，0 错误、90 个既有 ACL/旧代码字段警告；随后已执行 `dotnet build-server shutdown`。未启动 Unity，也未改变当前 Corin 资产。

## 2026-09-10 正式 Runtime Trace 闭合 Graph 与 Timeline 区间

此前编译后的运行链只把 `timeline_logic_time`、TreeClip 生命周期和普通 operation 事件送进 Diagnostics，`RuntimeExecutionTimeline` 没有可靠的 Timeline 起点，也没有正式 Graph invocation 起止事件。这样只能显示节点事实，不能把一次真实 Graph/Timeline 调用投影成完整区间。

提交 `ce6a4f31d` 与 `eb5e4af06` 完成了这条正式接线：

- `TimelineControlRuntime` 在第一次正式 Tick 通过 Action Context 校验后发布 `timeline_started`；Character 与 Fixed Diagnostics adapter 将它映射为 `TimelineStarted`，并与原有 completed/cancelled/stopped 事件配对。
- 两套 Character Diagnostics adapter 从 Program SourceMap 的 `GraphInvocation` entry 建立 operation index 到 Graph source handle 的关系。Graph 根 operation 的 enter/complete/stop/force-stop 事件分别发布 `GraphCreated/GraphDestroyed`，来源直接使用对应 Graph handle、调用代次、ActionInstance 和 ParentInvocationGeneration。
- Timeline builder 接受 `TimelineStarted` 作为正式区间起点，不再要求预览根据第一个时间采样猜测开始时刻。Graph/Timeline/TreeClip 的 span 配对仍使用 SourceHandle、RuntimeInstanceKey 和 ExecutionBranch。

本步 `ThirdPersonClient.Editor.csproj` 按项目要求构建成功，0 错误、90 个既有 ACL/旧代码字段警告；随后已执行 `dotnet build-server shutdown`。这只补齐动态时间轴的运行事实来源，不补齐外部结果 journal、Presentation/Camera restore/replay，也不替代 Unity Scene Play 端到端验收。

## 2026-09-10 BTSMTL Document 版本对账

复核最新 BTSMTL 规划和 `btsmtl-agent-authoring` 技能后，预览 change 的目标文档合同已从旧的 v5 口径统一为 Document v7：v7 的整包 `Clean/TreeDirty/DocumentDirty/Conflict`、Capability、Exporter/Codec/Reconciler/Mutation/Validator、反向导出和五个生命周期仍由唯一正式作者服务拥有，预览不得复制这些路径。

这次只修正 change 的 proposal/spec/tasks/audit 用词与旧版本规范的矛盾，没有把当前 worktree 中仍可见的 v4 Agent authoring、缺失的 v7 服务或缺失的独立 Timeline owner 写成已接入。任务 `2.3`、`10.1–10.4` 和相关外部合同门禁继续保持未完成；运行时 Program、Tick History、checkpoint、ExecutionBranch 和 BuildStatus 仍不进入 Document 可写正文。

## 2026-09-10 Timeline playback 实例与真实表现时间闭合

提交 `9c4cd021f` 和 `293a919a8` 补上了 Timeline 诊断链中此前丢失的运行时身份和表现采样：

- `TimelineControlRuntime` 生成正式 `TimelineTraceOutput` 时携带父 Timeline operation、实际 playback generation、逻辑时间、cycle 和 retained ActionContext；Float32/Fixed Trace sink 将这些 typed 数据放入 `SimulationTraceRecord`，没有从 Detail 字符串反解析。
- Character Float32/Fixed Diagnostics adapter 对 Timeline lifecycle/logic 事件建立 `RuntimeInstanceKey.Timeline`，对 TreeClip lifecycle/decision 事件建立带 playback generation 与 cycle 的 `RuntimeInstanceKey.TreeClip`；普通 Graph operation 仍沿用原有 SkillExecution identity。
- `CharacterSimulationPresentationRuntime` 只在正式 `SampleProducer` 命令被消费后发布 `TimelineVisualTime`。来源通过命令 Activation 的 Program operation 精确解析到 Timeline SourceMap handle，并记录真实 sample time、visual time scale、weight、cycle、ActionInstance 和 producer generation。
- 因而 Timeline Live Debug 的 logic playhead、visual playhead、TreeClip phase 和总时间轴区间都来自同一次真实技能释放的 runtime 事件；没有新增 Timeline 时钟、预览解释器或第二份作者数据。

本步两次 `ThirdPersonClient.Editor.csproj` 本地构建均成功，0 error；每次都执行了 `dotnet build-server shutdown`，报告的 90 个 warning 仍来自既有第三方/旧代码字段。`git diff --check` 和 OpenSpec strict 校验通过。尚未取得新的 Center Run 或 Unity Scene Play 端到端结果，因此这里只确认编译和静态链路，不把表现画面、并行 Timeline 身份冲突或恢复后表现状态写成已验收。

## 2026-09-10 清理窗口级独立 Timeline 播放器并隔离并行实例

提交 `0b46fede4` 和 `76ef0fe62` 收口了两个仍会误导实现方向的边界：

- `PreviewSession`、`CharacterPipelinePreviewProgram`、`TimelineActionPreviewAdapter` 以及 `AnimationPreviewRuntime` 中的 Timeline Evaluate/RetireAndReset/独立动画 command 路径已删除；`MotionMatchingQueryFixture` 的正式查询 Pose 采样仍保留。角色技能预览不再有可调用的窗口级 Timeline 执行器。
- `RuntimeInstanceKey` 的 Timeline/TreeClip identity 增加正式 Timeline operation index 和 ActionInstance。Float32、Fixed、Presentation、Timeline UI 和历史选择均传递同一组合；同一 Session 中并行 Timeline 即使 playback generation 相同，也不会共用 Timeline 摘要、TreeClip 事件或历史区间。

本步 `ThirdPersonClient.Editor.csproj` 构建成功，0 error；首次清理 using 时发现并修复了 Pose 查询适配器的精确 Motion Matching 依赖，最终构建报告 90 个既有第三方/旧代码 warning，并已执行 `dotnet build-server shutdown`。`cc390cd7b` 进一步按 Timeline operation/playback/cycle 收紧历史筛选，避免同一 ActionInstance 的兄弟 Timeline 在 action、graph 和 Presentation 关联阶段重新混入；`411ae486e` 让 Corin 迁移器在写入前拒绝缺失、额外、重复 Timeline 以及最终 TreeClip 数量不一致，消除静默漏迁成功。仍未取得新的 Center Run 或 Unity Scene Play 端到端证据；并行实例和静态迁移覆盖的代码门禁已闭合，但真实画面、技能输入结果和恢复后表现仍需主验收 Editor 验证。

## 2026-09-10 TreeClip 静态迁移校验与 Unity asmdef 门禁

提交 `ee5bf3671` 增加不写资产的预览侧 TreeClip 静态对账入口，并复用旧侧身份、Native TimelineBody closure、TreeClip 业务键和覆盖检查。Unity 首次执行暴露 `BTSMTL.Timeline.Tree.Editor` 缺少 `ThirdPersonGameplay` asmdef 引用，随后在 `BTSMTL.Timeline.Tree.Editor.asmdef` 补齐正式依赖；第二次执行再暴露校验消息错误使用 Timeline 数据对象的 `.name`，已改为稳定 `AuthoringId`。

最终直接 Unity batchmode 使用当前预览项目路径执行校验，日志为 `D:/Unity_Project_1/3C-Artifacts/btsmtl-preview-treeclip-validation.log`，结果同时满足 `Tundra build success`、`Exiting batchmode successfully now!` 和 `Corin Native TreeClip validation passed: 7 Timeline(s), 23 TreeClip(s).` 这证明当前旧/新静态身份、Timeline 覆盖和 Native 图闭包在 Unity 实际程序集环境下通过；它仍不等价于 23 个 TreeClip 的运行时 Decision/Commit 输入回放，也不替代 Scene Play 端到端验收。

## 2026-09-10 正式 Action 结果观察接入

此前 Scene Play 的 Skill 按钮只能知道输入是否排入正式 Control Source，不能在同一条预览链上区分输入排队与 Action Runtime 的实际结果。提交 `afb2eb61f` 在公共 Action 执行合同中增加 `SimulationActionResultKind`，并由 `ActionSkillExecutionRuntime` 在正式准入、ActionInstance commit 和生命周期终止处写入结构化 `SimulationTraceRecord`。Float32 与 Fixed 的 Trace sink 都保留 ActionId、SkillId、ActionInstanceId、InputSequence 和结果种类，两个 Character Diagnostics adapter 将其映射为 `ActionResultSubmitted`。

提交 `f8ca23bf7` 将 Timeline Live Debug 的 StateMachine interest 接入已有 Runtime Diagnostics，并按当前 Timeline playback 的 ActionInstanceId；没有实例时再按同一 SkillId筛选最新结果。窗口只消费正式 Trace，不创建第二个 ActionInstance 或执行器；结果可显示 Accepted、Rejected、Completed、Cancelled、Interrupted、Aborted 和 Corrected。该步骤的本地 `ThirdPersonClient.Editor.csproj` 构建成功，0 错误，随后执行了 `dotnet build-server shutdown`。

仍未把这一步写成端到端验收：当前没有新的 Center Completed Run，Tree-only 尚无 Timeline 窗口专用选中面板，真实 Scene Play 中各类 Action 结果和替换分支仍需主验收 Editor 观察。

## 2026-09-10 真实注册暴露的旧图与新图 SourceMap 冲突

第一次在独立预览 Unity Editor 加载 `BtsmtlPreview` 并进入 Play 时，`CharacterPipelineHost.EnsureRegistration` 在构造 `CharacterRuntimeDebugProgram` 处失败。具体冲突是同一个 `GraphAuthoringId` `b2328afd-40a8-467d-91f8-784c461f6137` 同时来自旧 TreeDesigner RootTree 和新 FlowCanvas SkillGraph：前者使用 16 位 `GraphAuthoringFingerprint`，后者使用 64 位 `BtsmtlSkillGraphFingerprint`。如果继续共用一个 `RuntimeSourceElementKey.Graph`，SourceMap 的内容 hash、Program target 和导航都会不确定，因此没有放宽冲突检查或取首个 hash。

历史提交 `62aff1d95` 曾在 Runtime SourceMap builder 中按正式调用路径和来源类型为旧 RootTree Graph 使用 `legacy:<GraphId>` 命名空间，并同步旧 TreeDesigner 导航；该版本不能作为当前预览能力证据。随后预览合入主线运行时的 `d423510f0`，移除了这段 legacy resolver 和旧 RootTree 导航，当前 `CharacterRuntimeDebugProgramBuilder` 使用原始 GraphAuthoringId，且预览源码已不存在 `RootTreeAsset` 运行入口。当前旧 RootTree 只保留在迁移审计快照，不能作为 runtime 输入或 fallback。

此前日志 `D:/Unity_Project_1/3C-Artifacts/btsmtl-preview-runtime-sourcemap-validation-20260910-v6.log` 只证明当时 `62aff1d95` 版本的 SourceMap 构造和程序集加载通过，不能外推到 `d423510f0` 之后的当前 HEAD。当前版本是否在实际加载的旧/新来源集合下保持无冲突，需新的静态入口或主验收 Editor 证据；在此之前不把 legacy 命名空间或 3265 条记录写成当前已验证能力。

## 2026-09-10 预览正式资源启动入口

真实 Scene Play 已经越过 SourceMap 注册冲突，但原预览场景直接激活 Character Session/Actor，没有先启动正式 YooAsset 资源包，表现层会在首帧报 `YooAssets not initialize`。本步提交 `e78175812` 接入现有 `IBtsmtlScenePlayRuntimeOwner` 合同：

- `BtsmtlScenePlayResourceRuntimeOwner` 只调用正式 `IResourceModule.Initialize()` 和 `ProjectSceneResourcePreparation.PrepareAsync`，不新增资源加载器、技能执行器或时钟。
- 预览场景把 Context 放在 Startup 根，把 Character Session/Actor 放在默认关闭的 Runtime 根。资源包准备完成后 Owner 置为 `Ready` 并激活同一组正式 Session/Actor；准备失败时 Context 暴露正式失败状态，不能以未初始化资源继续播放。
- 预览场景构建器从既有 `ProductStartupProfile` 写入 Owner，Session、Actor、Context 和 Owner 仍属于同一个保存场景；恢复和停止继续由 Context/Session Host 的既有释放链负责。

验证事实：Unity batchmode 已重新生成 `Assets/Scenes/Authoring/BtsmtlPreview.unity` 并正常退出；`3C_Client.sln` 本地编译成功，0 error、113 个既有/第三方 warning，随后执行 `dotnet build-server shutdown`。重启预览 Editor 后，正式资源准备实际进入 `AssetBundleCollectorSetting` 校验，当前阻塞是 HEAD 中已有收集项引用不存在的 `Assets/Scenes/Product/ProductShell.unity`，不是预览代码错误。没有在预览里改收集路径或增加 fallback 配置，因此正式资源配置补齐前，Scene Play 的 Session/技能/Timeline/TreeClip 端到端结果仍未验收。

## 2026-09-10 sparse checkout 资源准备复核

上一节把 `ProductShell` 描述成 worktree 中不存在是不准确的：该文件和 `StandaloneGameplay` 已被 Git 跟踪，只是预览 worktree 的 sparse-checkout 初始规则没有 materialize `Scenes/Product`、`Scenes/Standalone` 以及 `AssetRaw/Product/Core`、`OptionalHD`、`HotUpdate/DLL`。本次只通过本地 sparse 规则取出已有 HEAD 文件，没有提交产品资源或修改收集配置。

清理并重启预览 Editor 后，正式 `DefaultPackage` 被正确识别，资源准备继续完成；官方 CLI 进入同一预览实例的 Play 后，Scene hierarchy 显示 `BTSMTL Scene Play Runtime` 已激活，Unity console 错误数为 0，日志出现正式 ACL 资源 `ValidateRuntimeOnce`。这证明资源 Owner、正式资源包和初始 Presentation 帧已接通；它仍不是 coordinator 通过 Timeline 窗口提交技能、ActionResult、动态 Timeline/TreeClip 历史和 restore/replay 的完整验收。

过程中 YooAsset 在 sparse 路径未 materialize 时曾由 `SettingLoader` 生成空设置文件；已将 `AssetBundleCollectorSetting.asset`、目录 `.meta` 和 Unity 自动生成物恢复为 HEAD/未跟踪清理状态，最终不把这次本地 checkout 现象带入提交。剩余 Scene Play 端到端验证应从正式 Coordinator 入口开始，而不是重复直接 Editor Play。

## 2026-09-11 合入 Slate 后的运行入口收口

提交 `f606981f7` 将主线已提交的 Slate Timeline projection、Timeline owner 和 Pose 资源路径合入预览 worktree。旧 UI Toolkit Timeline 的 UXML/USS、Field/Canvas/Rendering/Interaction 文件按主线删除；静态搜索没有发现 `TimelineEditorView`、旧 Timeline Field 或旧 Timeline Player 的 C# 消费者。`TimelineEditorMainWindow` 保留主线 Slate 作者表面，并接入预览已有的动态执行时间轴、History 查看和 Restore/Replay 命令；这些命令只调用正式 Scene Play Operations，不写运行状态。

提交 `2ace76b69` 删除预览侧 `CorinPoseGraphPortMigration` 和 `CorinSkillTimelineReferenceMigration`。两者分别指向已迁移的旧 Pose 路径和预览侧一次性作者写入入口，没有运行时消费者；TreeClip 的 23 条映射及静态证据继续保留在 change artifact，不作为 runtime fallback 或第二个 Mutation owner。

## 2026-09-11 后台 Build owner 收口

提交 `a53dfd505` 将后台 authoring snapshot、Target lowering、publication 和 adoption 排队实现从 `CharacterSimulationBackgroundBuildService` 收回现有 `CharacterSimulationBuildOrchestrator` 的 partial owner。Scene Play Coordinator 现在只调用 `StartBackgroundCharacter` 和 `PublishBackgroundAndQueueAdoption`；后台阶段仍只消费不可变 Semantic IR，Unity 资产发布和 Session adoption 仍在正式发布边界完成。没有复制另一套编译器或运行时。

本轮没有执行 Build、Play 或测试。上述结论来自提交内容和静态引用审计；主线工作树仍有未提交的 Agent metadata/v7 改动，预览任务 `0.2`、`10.1–10.4` 继续未完成。

## 2026-09-11 主线 Slate change 完成后的再基线

主线已推进到 `9c7766d32`，`restyle-timeline-editor-slate-style` 的 projection 字段、上下文、键盘/菜单事务、播放隔离、Unavailable 处理、时间编辑后的时长重算、轨道顺序 identity 回写和文档任务均已完成。提交 `a9bf0e5f7` 将这些已提交接口合入预览；预览继续保留自己的运行历史与 Scene Play 控制，但不恢复主线删除的旧 Timeline UI。

因此当前主线依赖只剩未提交的 `refactor-agent-authoring-attribute-driven`：它仍在删除 Agent 专用 Capability/Mutation 分支并下沉到正式 metadata。预览不复制这批工作树改动，待其形成稳定提交后再做一次 v7 owner 静态对账。

`7b90bdd45` 同时收紧了 Timeline 上层程序集依赖：Scene Play 合同由已经使用它的 Tree Editor 消费，`BTSMTL.Timeline.Editor` 不再额外引用 `ThirdPersonGameplay`。本轮没有执行 Build、Play 或测试。

本轮随后执行 `openspec validate "design-btsmtl-authoring-runtime-workbench" --type change --strict`，返回 `Change 'design-btsmtl-authoring-runtime-workbench' is valid`。该结果只证明当前 change 文档结构和 delta 格式有效，不替代 Unity/C# 编译、Scene Play 或用户端到端验收。

提交 `9eddc87c1` 修正空 Capture/无匹配实例的诊断状态：`RuntimeExecutionTimeline` 与 `RuntimeExecutionHistory` 只有在实际收到事件时才标记为完整，避免把空记录当成可查看或可恢复历史。该修正不改变捕获容量、checkpoint 或正式 Session 状态。

提交 `5b28e8b20` 将 Tick 历史的聚合键从单独 `Tick` 收紧为 `(Tick, ExecutionBranchId)`，并让按实例补入的 Session 边界事件只来自该实例所在分支；MCP 历史结果同时返回每条 TickRecord 的分支 identity。这样恢复后相同 Tick 的旧/新分支不会合成一条历史记录。

提交 `cb8f48d42` 让 Timeline 窗口的显式 Capture 和 Live Debug interest 使用完整 `RuntimeTraceChannel.All`，确保 Graph、State、Blackboard、Branch、Timeline、TreeClip 和表现事实都能进入总时间轴；提交 `a72f24df6` 只在诊断 revision、播放实例或历史游标变化时重建执行时间轴，普通窗口刷新复用已有结果，避免历史长度直接放大 UI 重绘开销。采集仍由作者主动开启并受现有容量限制。

提交 `528e18c27` 将同一 Timeline playback 的 `TimelineRequested` 与 `TimelineStarted` 合并为一个 span，避免正式请求和启动事件被显示成两个区间；提交 `33538a642` 让未闭合的 Timeline/TreeClip span 使用同实例最新的逻辑/表现采样作为尾部，而不是在 Capture 截止时退回开始 tick。提交 `193c3f29a` 将 `ProgramEpoch` 和 `ProgramRevision` 显式加入 span、checkpoint、Timeline 窗口和 MCP 输出，作者可以直接区分不同产物版本的执行记录。

## 2026-09-11 拒绝主线独立 Timeline Preview 回退

主线后续提交 `47a219bc0` 在 Slate projection 上恢复了 `TimelinePreviewSession`、`TimelinePreviewTarget`、窗口级 `EditorApplication.update` 和按编辑时间 `SetTime` 的播放路径。该路径不是 Scene Play 的正式 Session/ActionInstance/SkillGraph 执行，也不是 Runtime Trace 的历史查看；若与预览合入，会重新产生第二个 Timeline 时钟和独立求值链。

本预览明确不合入该提交，保留 `9c7766d32` 之后的 Slate 作者表面和真实运行观察。若主线需要独立 Timeline 作者工具，必须在主线单独说明其非技能、非角色运行边界；它不能替代本 change 的真实 Scene Play，也不能被预览窗口调用。该冲突记录在任务 `0.5`，当前不复制 main 的未提交 Authoring 重构。

## 2026-09-11 等待区间投影修正

提交 `8f3cba268` 修正动态执行时间轴对 `NodeWaiting` 的处理。此前等待事件被当作零长度 Point，无法表达节点在多个 Logic Tick 中持续等待；现在同一 SourceHandle、RuntimeInstance 和 ExecutionBranch 下的连续等待事件形成 `Wait` span，正式运行、完成或停止事件闭合该 span，Capture 在等待中结束则保留 `completed=false`。同时等待期间会延长对应 Node span 的尾部，Graph/Node/Timeline/TreeClip 的父子时间关系不再被等待点截断。

该步只修改 Runtime Trace 的只读 span projection，不增加业务推进、第二时钟或额外历史存储；尚未执行 Build、Play 或测试。

提交 `954449184` 让正式 `TimelineControlRuntime` 在一次 Timeline 从 Dormant 进入执行时先发布 `TimelineRequested`，再按原有链路捕获 Action Context 并发布 `TimelineStarted`；Float32/Fixed diagnostics adapter 使用同一事件和 Timeline channel 映射。这样请求失败也会在总时间轴中保留“请求→取消”的真实区间，成功调用则保留“请求→启动”的入口，不把请求阶段改成窗口推断。

提交 `d4e3eac28` 将 `TreeClipDestroyed` 纳入 Float32/Fixed 的 Timeline channel，与其它 TreeClip 生命周期事件保持同一订阅边界；窄 Timeline 观察不会因通道落到 Graph 而遗漏强制终止事实。

本轮新增 `SimulationSessionHost.TryRestoreToTick`：精确命中 checkpoint 时复用原子 Snapshot Restore；目标 Tick 不是 checkpoint 时只选择不晚于目标的最近 checkpoint，并要求正式 input replay runtime 覆盖该区间，再由 Runtime Handle 准备回放和由 Host 创建新 ExecutionBranch。Scene Play Coordinator、Local Session Debug Control 和 MCP restore 统一使用该入口；没有输入覆盖或正式回放能力时直接报告不可恢复，不把编辑时间当作恢复结果。本轮未执行 Build、Play 或测试。

提交 `1e4ac8157` 将本次 Build 的 `RequestedProgramEpoch` 与实际 `AdoptedProgramEpoch` 加入 Scene Play BuildStatus，并从 MCP status 输出。Building/Failed 只有请求身份，Published 只显示本次待采用 Epoch，Adopted 同时显示请求与正式当前 Epoch；状态不会复用上一次 Build 的结果。

提交 `759a35e53` 为 `RuntimeExecutionTimeline` 和 `RuntimeExecutionHistory` 增加按 `ProgramRevision` 查找 SourceMap 的完整性判断与 `UnmappedEventCount`。带合法 SourceHandle 但缺少对应历史 map 的事件会让历史标记为不完整，并在 Timeline/MCP 总览显示数量；历史仍可查看已有事实，不会用当前新 Program 的 map 解释旧事件。

提交 `990736f9d` 清理主线 Slate 合并后的来源导航残留：`RuntimeDebugSourceNavigator` 改用现行单参数 `TimelineEditorWindow.Open(TimelineAsset)`，不再调用已删除的 GraphId 重载。静态调用签名与主线 Timeline owner 一致；本轮未执行 Build。

提交 `7da4431f4` 将既有 Session 输入录制能力接入同一 Scene Play Operations：Coordinator、MCP 和 Timeline Live Debug 共用 `record_start/record_stop`，并在 status 暴露 replay capability/recording 状态。录制仍是作者主动开启的紧凑输入 trace，不默认记录所有运行 Tick；非 checkpoint 目标恢复只有在这份正式输入 trace 覆盖时才会准备 replay。

提交 `e95238b3a` 将总执行时间轴的最近 512 条 span 渲染为只读定位按钮，使用事件所属版本已经解析出的 `RuntimeSourceElementKey` 聚焦当前 Slate Timeline 的 Track/Clip/TreeClip；更早区间仍保留在时间轴对象和 MCP，只在窗口显示省略计数，避免大量历史事件造成 UI 行级实例化开销。跨 Timeline 或缺 map 的区间不会静默换绑。

提交 `eda1219f8` 增加 Timeline 窗口到技能 Graph 的正式导航桥：窗口只发 `RuntimeExecutionSpan`，已有 `BtsmtlSkillObservationSession` 交给 `RuntimeDebugSourceNavigator`，后者再次校验 span 的 Session、ProgramRevision、SourceHandle 和历史 SourceMap 后打开唯一 FlowCanvas Graph/Node。Timeline Editor 程序集不引用 Character Editor，Tree/Timeline/Graph 仍由各自 owner 管理。

提交 `92a11e7f3` 在 Session Host 恢复前核对 checkpoint 的 `ProgramCatalogHash` 与当前活动 `ProgramEpoch`。Program 热替换后选择旧 Catalog 的历史点会直接返回版本不兼容；不会调用 Snapshot Restore、准备回放或创建新分支，当前运行和旧 Capture 均保持不变。

提交 `1411154fd` 让 Float32/Fixed 正式 Session Runtime 在 Dispose 时调用各自已有 Input Trace Module 的 Stop，清理异常、Reset 和 Stop 后残留的 Recording/Replay 模式；已完成输入 trace 仍保留为下一次同 Session replay 的材料，未引入新的录制格式。

提交 `504b3c895` 将结束清理收紧为 `ClearCompletedTrace`：Session Runtime Dispose 同时清除静态活动模式和已完成输入 trace，避免新 Session 因复用相同 ActorId 而误拿旧 Session 的 replay 材料。Capture/Runtime Diagnostics 历史不受影响；同一 Session 必须在当前生命周期内重新录制输入后才可做非 checkpoint Tick replay。

提交 `484145f02` 给 Coordinator 的后台 Build 结果消费和 adoption 轮询增加生命周期门禁。Build 任务完成后若请求已进入 Stop/Reset/Fault 等状态，结果直接丢弃；Published adoption 只在同一请求仍处于 Running/Paused 时接受，避免停止或场景重载过程中发布到失效 Session。

提交 `b5983a295` 让 Timeline Live Debug 的输入录制按钮在正式操作成功后立即刷新自身状态，暂停时也能看到 Recording/Stop 变化；它不增加采样或业务 Tick。

提交 `e1990cec9` 修正 Session Host 在收集 Program adoption binding 失败时的事务清理：现在会同时丢弃所有 Actor registration 的 pending candidate，再清空 pending Epoch，避免失败后的候选污染下一次采用。

提交 `39c03fd21` 修正 Runtime Debug status、Timeline span 和 checkpoint MCP 输出对 `RuntimeProgramRevision` 的访问，统一使用完整 revision identity 的 `ToString()`；静态调用不再依赖已不存在的 `.Value`。

提交 `3f3126aa2` 将当前 `ProgramEpoch` 加入 Diagnostics Target、Debug ViewModel、Graph 状态栏、Timeline 状态栏和 MCP debug 摘要；作者可在不展开总时间轴时也确认当前运行产物版本。

提交 `a0e772c64` 让 Runtime Debug 来源导航消费 `TimelinePlaybackProvenance.SourceNodeAuthoringId`。同一 Graph 内多个节点引用同一个 shared Timeline 时，先按正式调用节点收窄，再按 Timeline identity 定位；没有唯一 caller 仍拒绝导航，不按数组顺序选择。

提交 `a6d67fab4` 收紧 Float32/Fixed 输入 replay 覆盖检查：选中的 trace 必须从 checkpoint 后的 `fromTick + 1` 连续覆盖到 `toTick`，录制过晚或提前结束都拒绝。之前仅检查所选帧内部连续，可能把晚开始的第一帧错误重映射到 checkpoint 后第一 Tick；现不会用不完整 trace 推进历史。

## 2026-09-11 主线 Slate 曲线增量接入

主线已推进到 `b3aa565b7`，其中与预览当前 Slate owner 不冲突的 `cf795cd03` 只负责 Slate 原生曲线关键帧的投影、曲线 snapshot/diff 和正式 Timeline 曲线回写。该提交已以 `f1586da49` cherry-pick 到预览；预览继续不消费主线 `47a219bc0` 的独立 `TimelinePreviewSession` 播放链，也不带入主线 dirty Agent authoring 重构。后续 Pose 作者增量和 Agent v7 owner 等待各自稳定合同后再做精确对账。

提交 `a38692299` 修正等待 span 的闭合查找：等待开始和结束事件的 `RuntimeExecutionSpanKind` 不同，结束时现在按独立 `Wait` key（SourceHandle、RuntimeInstance、ExecutionBranch）查找并闭合，避免等待区间错误地延续到 Capture 末尾。

## 2026-09-11 Tick 历史记录正式来源时钟

本步补齐 Tick 历史中来源时钟只能出现在 `Detail` 文本的问题。`RuntimeTracePayload` 现在携带 typed `SourceClockId` 与 `SourceTickKind`；Float32/Fixed Diagnostics adapter 在接收正式 simulation boundary 和 pipeline trace 时，从 record 的 `SimulationTickSourceIdentity` 写入这两个字段。`RuntimeExecutionTickRecord` 按同一 `(Tick, ExecutionBranchId)` 汇总去重后的来源时钟和来源类型，MCP history 直接输出它们。

这条链记录的是正式 Session 使用过的来源身份，不改变业务 Tick、私有时钟或回放材料，也不增加每 Tick 的完整状态快照。其它没有新 simulation source 的诊断事件沿用当前 Session 已确认的来源身份；下一条正式 boundary/pipeline trace 到达时更新为该记录的 source。Program adoption 或 ExecutionBranch 切换会先清掉缓存的来源身份，避免新 revision/分支继承旧来源。`RuntimeDiagnosticsStore` 的状态等价判断也比较这两个 typed 字段，避免恢复/回放诊断把不同来源误判为同一状态。

本步未执行 Build、Play 或测试；只做静态 diff 检查。该补充不替代真实 Scene Play 端到端输入、Presentation restore/replay 或跨 Actor 历史验收。

## 2026-09-11 技能请求拒绝重复作者候选

提交 `c531180f2` 收紧 `BtsmtlScenePlayPreviewCoordinator.RequestSkill`：同一 Actor 的同一 `SkillId` 若对应多个 `SkillOption`，请求现在返回 `RejectedSkillAmbiguous`，不会因列表顺序取第一个定义。唯一 SkillDefinition 通过后，仍独立检查多个技能共享同一正式 `SourceInputRequestId` 的歧义，再由 Character Control Source 排队输入。

这只修正请求绑定的确定性，不创建 ActionInstance、不解释 SkillGraph，也不把输入排队结果冒充 Action Runtime 的最终结果；实际准入、替换、完成和 Tree-only 生命周期仍由正式 Runtime Trace 验收。

提交 `7930fc670` 将同一正式 `CharacterRuntimeDebugProgramBuilder` 接入 Float32/Fixed Target lowering。后台纯 Program lowering 现在先构造 Runtime SourceMap 并检查 Graph/Timeline/Track/Clip/TreeClip 容器及来源身份；SourceMap 冲突或非法时 Target product 直接失败，不会等到 Play 中 Actor 注册才发现，也不会进入 publication/adoption 队列。

提交 `8216139c6` 将 History 的来源时钟汇总改为配对的 `RuntimeExecutionSourceClock` identity；一个 Tick 同时经过多个来源时，`ClockId` 与 `TickKind` 不会被两个独立数组拆散，MCP history 直接输出配对对象。

提交 `079dd0d84` 将 Scene Play 合同增加只读 `ActorIds`，MCP status 直接列出当前正式 Actor，Slate Timeline Live Debug 增加按 ActorId 选择的 Build 菜单。菜单只调用 `IBtsmtlScenePlayPreviewOperations.Build`，后台执行与 ProgramEpoch adoption 仍归 Coordinator/Build Orchestrator。

提交 `465265656` 在同一 Timeline Live Debug 工具栏增加 Skill 菜单。选项来自 Scene Play Context 当前正式 SkillDefinition，点击后只调用 `IBtsmtlScenePlayPreviewOperations.RequestSkill`；窗口得到的是输入排队结果，Action 准入、ActionInstance、Timeline/TreeClip 执行和最终结果仍从正式 Runtime Trace 观察。

提交 `80c945951` 在 Timeline 重新绑定时清除 Build/Skill 菜单缓存，避免 Editor 页面复用上一条 Timeline 的操作选项；不改变 Scene Play 或 Runtime 状态。

## 2026-09-11 主线 Pose 清理提交对账

复核主线最新提交 `2815af4de`：在预览已消费的 `b3aa565b7` 之后，唯一改变预览运行/作者依赖边界的提交是 `4b1673e60`，它从 `CharacterPresentationPoseGraphAsset` 删除旧 Typed Graph/Canvas migration 数据结构。预览当前源码没有其它调用这些内部 migration 类型，已将该提交以 `f087cc1a3` 精确 cherry-pick；主线随后两个 Pose 资源审计文档提交不属于预览运行合同，未复制。

主线工作树仍包含未提交的 Agent v7 metadata、Projection、Mutation 和资源变更，预览没有从 dirty main 合并，也没有把其未稳定类型写入预览入口。该对账只更新集成基线和已消费的删除提交，不表示主线 Agent change 已完成。

提交 `dd9c47767` 让 Timeline Live Debug 读取 StateMachine channel 的 `ActionResultSubmitted`，只按当前 Timeline playback 的 `ActionInstanceId` 显示实际 Action 结果。输入排队反馈和运行时最终结果保持分开；没有相同 ActionInstance 的事件不会被窗口混入。

提交 `32e186c46` 修正正式 Program adoption 被拒绝时的状态传播：BuildStatus 和 Coordinator 主状态同时保留 adoption 错误码/消息，旧 Program 继续运行，不把一次候选版本失败伪装成 Session Fault。

提交 `1bd36db8e` 将 BuildStatus 阶段和消息接入 Timeline Live Debug 状态栏及 Build 菜单 tooltip，Build 的 Building/Published/Adopted/Failed 结果不会被播放摘要覆盖。

提交 `bef63bdaa` 收紧 Float32/Fixed 输入回放事务：Handle 在正式恢复前捕获调用前 checkpoint，恢复或回放准备/启动失败时恢复原 Session 状态；输入录制进行中直接拒绝回放。临时 checkpoint 只在用户发起恢复时使用，不改变每 Tick 历史容量。

提交 `52adf5805` 修正回放生命周期：Float32 Session replay 在最后一帧后释放 replay 模式；Fixed 只有 Session Host 使用的 checkpoint replay 采用继续运行标记，完成后释放 Fixed start gate，性能采集等其它 replay 保留 Completed 语义。恢复分支可在 replay 后继续接收实时输入，也可以再次发起同一 Session replay。

提交 `05ecd31fd` 让 Character Actor 的 ProgramEpoch 准备阶段比较新旧 Presentation producer 数量、索引、identity、AnimationChannel、来源 identity 和输出类型。随后 `fd28a93ee` 将这条判断下沉到共享 Presentation semantic contract，并让 Float32/Fixed registration 共用。表现 producer 拓扑变化会在 Session 请求前明确拒绝，旧 Presentation runtime 不会被新 Program 误用；同一 producer 拓扑下的逻辑版本仍可留在稳定 Session 中继续采用。

## 2026-09-11 主线最新提交对账

主线当前已提交到 `f2e5effd7`。`a777da720` 增加正式 `IAnimationPresentationRuntimeResetController` capability，预览以 `d07479b0e` 对齐到 `AnimationPresentationRuntimeTarget` 和 `CharacterSimulationPresentationRuntime`；这只是公开同一个 Presentation Runtime 的已有 Reset，不接入窗口级 Pose evaluator。主线 `14f9926d9`、`23e414871` 新增的 Pose reset MCP 调度器属于主线诊断任务，预览已有自己的 Scene Play reset observation job；整份搬入会形成第二个观察调度入口，因此没有复制。主线后续形成完整 Pose Resource Slot authoring/compilation 链，预览已按职责摘取其核心代码；主线 `47a219bc0` 的独立 Timeline Preview 播放链和 `775f42bbe` 删除的 preview adoption 接口没有带入。主线工作树仍有未提交的 Agent v7 metadata、Projection、Mutation、Pose 和资源重构，不作为预览依赖。

主线 `47a219bc0` 仍恢复了独立 `TimelinePreviewSession` 播放链，预览继续拒绝该语义冲突。主线工作树在本次复核时仍有约 220 个未提交文件，包含 Agent v7 metadata、Projection、Mutation、Timeline 和资源变化；预览没有从 dirty main 合并。

## 2026-09-11 Program 采用后的技能入口刷新

提交 `d4d153037` 在正式 ProgramEpoch adoption 成功后重新从当前 Scene Context 读取 SkillDefinition，更新 Coordinator 的 Actor/Skill/Input request 目录；Timeline Live Debug 的技能菜单 key 同时纳入 Actor、Skill、入口 Graph、Action Profile 和 `SourceInputRequestId`。因此作者在 Play 期间修改技能定义并完成 Build/adoption 后，窗口不会继续使用旧输入请求映射。该步没有创建 ActionInstance、SkillGraph 解释器或第二个 Control Source；实际技能准入和 Action 结果仍由正式 Runtime Trace 负责。

提交 `e25d6491d` 将已有的正式 `SimulationProgramAdoptionResult` 映射为 Gameplay 层只读 `BtsmtlScenePlayProgramAdoptionReport`，挂入 `BtsmtlScenePlayBuildStatus`，并从 Timeline 状态栏和 MCP status 输出 Deferred/Applied/Rejected、错误码、当前/请求 Epoch、SourceRevision 和 ProgramCatalogHash。这样 Build 阶段、发布等待和采用拒绝共用同一 Session 结果，旧 Program 保持运行；Gameplay 公共程序集不反向引用 Simulation Core，Center 还没有对应的持久 report 结果。

提交 `56f6a5ce4` 让 MCP `build` 命令的即时响应也携带同一 `BuildStatus` 投影，避免作者提交 Build 后必须依赖下一次 status 查询才能看到本次采用身份；`status` 与 `build` 共用 `DescribeBuild`，没有新增 MCP 状态源。

提交 `55103f5f2` 为 `BtsmtlScenePlaySkillRequestResult` 增加提交时正式 Session 的 ProgramEpoch 和 SourceRevision。MCP 的 `request_skill` 响应现在能把输入序号、场景 generation 与 active Program 版本一起交给后续 Runtime Trace 对账；它仍只表示 Control Source 已排队，不冒充 ActionInstance 已创建。

提交 `fe3adc5b1` 收紧 Build 早期失败的 adoption report：只有 Session `LastProgramAdoption.Requested.SourceRevision` 与本次后台 artifact 的 SourceRevision 完全相同，才把正式拒绝结果挂入 BuildStatus；发布失败或旧请求不会借用上一轮 adoption 身份。

提交 `04bd2fd90` 修正总时间轴对 `tree_clip_decision` 的 span 投影。该事件仍由正式 Runtime Trace 映射为 `ClipActive`，但现在明确作为已完成 Point；Decision 不再被误画成一个没有结束事件的未完成 TreeClip 区间，Commit TreeClip 的真实 Enter/Update/Exit 生命周期保持原有 span。

提交 `6c0f29ca0` 修正 TreeClip 运行实例身份：父 Timeline operation 继续作为 playback 归属，TreeClip 自身的 operation index 作为 child identity 写入 `RuntimeInstanceKey`。Float32/Fixed adapter、Timeline/TreeClip 历史筛选和 MCP 输出统一使用该字段，同一父 Timeline 同一 cycle 下的兄弟 TreeClip 不再共享实例 key；此前 `7da6844a8` 只落了 MCP 输出字段，不能单独证明运行时身份已经隔离。

提交 `58dc57439` 将 `StartBackgroundCharacter` 的候选结果补成完整角色闭包：在不可变 Semantic IR 完成后，现有正式 Presentation 编译入口产出与同一 SourceRevision/semantic contract 对应的 Projection 和 ACL 动画目录，再由后台 Target lowering 产出 Program。`c05aa7784` 让每个 Target product 在返回前核对 Projection contract，`4f971243d` 将准备、发布、采用拆开；`CharacterPipelineHost` 在发布前接收候选 Projection，正式 publication stage 成功后才请求稳定 Session 的 ProgramEpoch，任何发布失败都会丢弃候选 Presentation runtime。

这闭合了“Timeline 改动只换 Program、表现继续读旧 Projection”的业务错误，但没有把现有 Unity-bound Semantic/Projection 编译器伪装成全后台：当前 `StartBackgroundCharacter` 仍在 Editor 主线程执行 authoring 读取和 Projection 编译，只有 immutable IR 之后的 Target lowering 在 `Task.Run`。主线尚未提供可消费的 immutable authoring snapshot，因此 8.6 的完整后台性能目标仍未完成；预览没有把 Unity API 放入 worker，也没有复制第二套 compiler。

## 2026-09-11 主线 Motion Matching 资源槽提交对账

主线当前已提交到 `f2e5effd7`。预览已摘取 `7a53e62e8` 的 Motion Matching Pose Resource Slot、`e60c2ebfd` 的 StateMachine BlendCurve/BlendProfile Slot、`e5928500a` 的通用 Presentation Pose Source Slot、`e3b9f81ab`/`0f31c2f33`/`38bf4b42c` 的 Graph Resource Slot Mutation 分发与删除顺序，以及 `12073c826`、`368f71cda` 的正式作者创建/绑定入口。对应预览提交为 `d186b096d`、`d90017052`、`633ff8af8`、`eefac1229`、`47bc08f0d`、`33be51bb5`；重复的 `CreateResourceSlot` 入口已由 `6b691c619` 删除，统一使用 `CharacterPoseResourceAuthoringService`。

预览当前 Pose 编译/作者链不再读取 Motion Matching Pose 的旧 `Binding`、`JumpBlendPolicy` 字段；它们统一从 Resource Slot 解析，StateMachine transition 的 BlendCurve/BlendProfile 也经正式 Resource catalog 解析。主线 `775f42bbe` 删除的 `CharacterPresentationSemanticContract.TryMatchProducerTopology` 与 Animation target replacement 会破坏预览稳定 Session 的 Projection adoption，因此保留预览自己的正式实现，不将该删除当作兼容迁移完成。

只读 `git merge-tree` 进一步显示，主线已提交版本还会与预览的 `TimelineEditorMainWindow`、`BtsmtlSlateTimelineProjection`、`CharacterSimulationPresentationRuntime`、`RuntimeDebugSession`、`RuntimeDebugTargetProvider`、`RuntimeDebugViewModel`、`RuntimeDiagnosticsContext`、`RuntimeDiagnosticsContracts`、`RuntimeDiagnosticsStore`、`RuntimeDiagnosticsTargetRegistry` 以及 Scene Play Preview 文件发生删除或双改冲突。主线侧删除的是预览尚未进入主线的动态时间轴/历史实现，不是可以接受的自动删除；本 worktree 保留预览真实执行链，等待主线作者与 Timeline owner 明确合并边界后再摘取提交。

## 2026-09-11 Tree-only Action 观察入口

提交 `2fdd7a7e7` 在 Scene Play MCP 的 `status` 诊断结果中加入当前 `RuntimeDebugViewModel` 的正式 `ActionResultSubmitted` 列表。该列表只读 StateMachine channel 已记录的 trace，保留 Tick、ExecutionBranch、ProgramEpoch、ProgramRevision、SkillId、ActionInstanceId、InputSequence、结果状态、原因、来源和完整 `RuntimeInstanceKey`；它不创建 ActionInstance，也不把输入排队结果冒充为运行结果。这样 Tree-only 技能即使没有 Timeline playback，也可以从同一正式 Diagnostics 入口观察实际 Action 结果。

提交 `91f34efaa` 在同一 Scene Play MCP `status` 中加入正式 `SimulationSessionHost` 的历史运行状态：当前 ProgramEpoch/SourceRevision、ExecutionBranch 及父分支、分支基准 Tick、checkpoint 数量与范围、最后一次 checkpoint 错误。该信息只从当前 Scene Context 读取，不在 Coordinator 或 MCP 中复制 checkpoint、推进 Tick 或推算恢复能力。

提交 `dff98cbb4` 让 MCP `timeline` span 输出 `RuntimeExecutionSpan.SourceHandle` 的数值、类型和有效性。作者 SourceKey 仍保留用于 UI 定位，SourceHandle 用于同一 ProgramRevision 的机器级 SourceMap 对账；缺失 handle 仍按现有 unmapped 规则报告，不从当前 revision 猜测。

提交 `b555bbdd9` 修正 Coordinator 的 adoption 通知边界：正式 Session 在 Logic Tick 应用新 Program 后，Coordinator 更新 `BuildStatus` 并触发 `StatusChanged`，让只订阅 Scene Play 状态的 UI 也能看到 Adopted，不改变实际 adoption 或 Tick 调度。

提交 `36715a77a` 将 Presentation Factory 的外部初始化副作用作为显式参数：初次 Actor 创建仍初始化正式相机/装备状态，Program adoption 的候选 Runtime 只构造不可变绑定和模块，不在 Prepare 阶段重置相机或禁用当前 renderer。候选只有通过 Projection contract、Factory 返回的 Projection identity 和正式输出接口校验后，才进入 Logic Tick commit。

提交 `58dc57439`、`c05aa7784` 之后，后台角色 Build 的可对账身份包括 Semantic IR root、每个 Target 的 Program/SourceRevision/Semantic/Layout/SourceMap/Presentation Contract/Projection revision，以及同一候选的 Projection payload 和 ACL animation catalog。Projection publication 复用完整角色 Build 的正式 stage，不新增预览专用 wrapper；候选 runtime 仍只在 Logic Tick adoption commit 时替换。

## 2026-09-11 主线 Pose Resource Slot 选择性接入

预览以主线已提交 `f2e5effd7` 为对账基线，选择性接入以下正式代码：`7a53e62e8` 的 Motion Matching Pose Resource Slot、`e60c2ebfd` 的 StateMachine BlendCurve/BlendProfile Slot、`e5928500a` 的通用 Presentation Pose Source Slot、`e3b9f81ab`/`0f31c2f33`/`38bf4b42c` 的 Graph Resource Slot Mutation 分发和删除事务，以及 `12073c826`、`368f71cda` 的作者创建/绑定入口。预览对应提交为 `d186b096d`、`d90017052`、`633ff8af8`、`eefac1229`、`47bc08f0d`、`33be51bb5`；重复的资源槽创建入口由 `6b691c619` 删除，统一保留 `CharacterPoseResourceAuthoringService`。

静态调用核对确认 Pose 编译/作者链不再读取 Motion Matching Pose 的旧 `Binding`/`JumpBlendPolicy` 字段；StateMachine transition 的 BlendCurve/BlendProfile 经过 Resource catalog 解析。主线 `47a219bc0` 恢复的独立 Timeline Preview 播放链没有接入；主线 `775f42bbe` 删除的 `CharacterPresentationSemanticContract.TryMatchProducerTopology` 和 Animation target replacement 也没有接入，因为预览稳定 Session 的 Projection adoption 正式依赖这两个能力。主线工作树仍包含未提交 Agent/v7/Projection/Pose/资源改动，不能作为本 MR 的编译依赖。

提交 `2a0bf491d` 给仍保留的 Motion Matching 查询预览补上 ACL 资源准备推进；它不恢复技能 Timeline evaluator。提交 `71c2ec745` 将 Scene Play 构建状态统一命名为 Character Build，使 UI 说明和实际的 Program+Projection+ACL 完整候选一致。

提交 `0f279c1dc` 为恢复链增加统一的表现 checkpoint capability：Session Host 在 Simulation restore/replay 前要求所有 Actor 提供 Presentation restore，Scene Play/MCP 暴露同一只读能力，Timeline Restore/Replay 控件只在能力存在时启用。当前 `CharacterSimulationPresentationRuntime` 明确返回 unsupported，因此不会只恢复 Simulation、清空表现或按作者 Timeline 重算；动画、IK、Camera 和其它表现状态的真实 snapshot restore 仍等待正式表现 owner 实现。

## 2026-09-11 主线 Pose 引用扫描精确接入

主线当前已提交到 `6de43ab99`。其中 `5a2b4adc6` 为 Pose Resource Slot 删除前增加 StateMachine transition 的 `CustomBlendCurveSlot` 与 `BlendProfileSlot` 引用扫描；预览当前 `ceda942dd` 仅补入这一段独立校验。主线后续 `AgentAuthoringPresentationReconciler` 的 v7 投影、转换、删除时序和资源文档仍属于主线 authoring owner，未复制到预览，避免把未完成的 Agent 领域实现变成预览依赖。

本步只改变 Pose 作者资源删除的引用门禁，不改变 Program、Session、Timeline、TreeClip 或 Presentation runtime；未执行 Build、Play 或测试。

## 2026-09-11 checkpoint 运行时身份门禁

提交 `38ec5dae2` 收紧 `SimulationSessionHost` 的直接输入回放入口：除 Active Session 和有效 Tick 范围外，Host 现在复用 `IsCheckpointCompatible` 核对 checkpoint 的 Session、当前 Program Catalog、Pipeline Hash、Execution Backend Id 和 Backend 语义版本。`RestoreCheckpoint`、`RestoreToTick` 与 `ReplayInputRange` 因此不会跨 Session 或跨运行时合同消费旧 checkpoint；没有完整 Presentation checkpoint capability 时仍在正式 Simulation restore 前拒绝，不改变当前表现状态。

本步只复用已有 Composition Descriptor、Program Epoch 和 checkpoint 字段；未执行 Build、Play 或测试。

对 `treeclip-migration-map.json` 按实际 `entries` schema 做结构复核：23 条记录，源/目标 TreeClip 各 23 个唯一值，必需字段缺失数 0，源/目标重复数均为 0；所有 23 条记录仍保留 `requires_runtime_and_graph_digest_verification`，因此不把结构闭合当作行为等价。

提交 `dba3a9112` 修正 History checkpoint 的可恢复标记：`RuntimeExecutionCheckpoint` 现在只有在整段 History 没有 eviction、每个 Logic Tick 同时有 SimulationTick/StatePublished 且 SourceMap coverage 完整时才报告 `CanRestore`。MCP 额外输出 `history_complete`；残缺记录继续允许查看，但不会被误报为可恢复。

提交 `b6ae7bb2f` 修正 Timeline Editor 的运行目标身份：从技能 `TimelineNode` 打开的窗口保存 `Owner.GraphAuthoringId`，构造 `RuntimeSourceElementKey.Timeline` 时与 TimelineAuthoringId 一起使用；聚焦执行区间时也拒绝不同 Graph 的同名 Timeline，交给精确来源导航处理。独立 Timeline 没有调用方 Graph 时保持空 Graph 身份。

提交 `3bc112d2b` 将该联合身份继续传入 Runtime Debug ViewModel：Timeline playback 实例、summary、当前事件、绑定的 follow/pin 选择和 Timeline 菜单都按 GraphAuthoringId 与 TimelineAuthoringId 一起筛选，避免 shared Timeline 的不同技能调用在窗口中混合。

提交 `7d062422f` 修正 Timeline Debug 的内容版本：窗口不再把 `TimelineAuthoringFingerprint` 当作 Program SourceMap 的 content hash，而是复用正式 `TimelineContentDiscovery` 的 `TimelineContentUnit.ContentHash`；内容变化时刷新 target request，校验失败时保持 invalid，不用旧 hash 继续观察旧产物。

## 2026-09-11 表现 checkpoint 捕获与恢复成对门禁

提交 `ce7726705` 将表现 checkpoint 合同从单一 restore 扩展为成对的 capture/restore。`SimulationSessionHost` 捕获正式 Simulation checkpoint 后，若所有 Actor 的 Presentation runtime 同时声明 capture 与 restore capability，则逐个完成对应表现快照捕获；当前 `CharacterSimulationPresentationRuntime` 两项均明确返回 unsupported，因此 Simulation checkpoint 仍可进入历史记录，但不会被 Scene Play 当作完整表现恢复点。

本步不把 Pose diagnostics snapshot 塞入 SimulationWorldSnapshot，也不使用 Reset、Transform 或重算作者 Timeline 作为恢复替代；未执行 Build、Play 或测试。

提交 `48518400f` 让 Timeline Restore/Replay 控件在表现 capability 缺失时显示明确 tooltip；UI 仍只读取 Scene Play 合同，不自己判断或补造恢复能力。

提交 `34d991223` 补齐恢复事务的失败回滚：`ISimulationSessionInputReplayRuntime` 增加正式 `TryCancelInputReplay`，Float32/Fixed runtime 分别连接已有 trace module 的 replay 状态；`SimulationSessionHost` 在表现 checkpoint restore 失败时取消未消费输入并恢复调用前 Simulation checkpoint，避免在新分支登记前留下半恢复或继续 replay 的 Session。精确 checkpoint restore 失败也恢复调用前 Simulation 状态。

## 2026-09-11 Timeline 菜单 revision 的调用方隔离

提交 `aa0158c85` 将 Timeline playback 成员和菜单 revision 从单独 `TimelineAuthoringId` 收紧为 `TimelineAuthoringId + GraphAuthoringId` 的复合身份。此前 playback summary、当前事件和绑定选择已经按调用方 Graph 过滤，但菜单 revision 仍是 Timeline 全局计数；共享 Timeline 被另一个 Skill Graph 调用时，会导致当前窗口无意义地重建菜单。现在窗口读取与其打开来源相同的复合 revision，另一个调用方的播放不会使当前窗口失效。

这一步只收紧 Editor 诊断索引，没有增加第二个播放实例、时钟或执行器；实际 playback 仍由 `RuntimeInstanceKey` 和正式 Timeline provenance 标识。未执行 Build、Play 或测试。

## 2026-09-11 不完整 checkpoint 保留为历史事实

提交 `5d22891a0` 修正历史投影的语义边界：`RuntimeExecutionHistory` 现在保留所有正式 `SimulationCheckpointCaptured` 事件，`RuntimeExecutionCheckpoint.CanRestore` 单独表达该点是否同时满足完整 History、SourceMap coverage 和 snapshot identity。容量淘汰、缺少 `StatePublished` 或来源未映射时，checkpoint 仍能在 History/MCP 中显示为 `history_complete=false`、`can_restore=false`，不会被误报成“没有 checkpoint”。

Timeline 窗口默认选择最近一个可恢复点；若作者手动选择没有完整材料的 Tick，Restore/Replay 在进入 Coordinator 前直接显示不可用原因。实际恢复仍只调用正式 Session Host，不从历史事件重建 Simulation 或表现状态。本步未执行 Build、Play 或测试。

## 2026-09-11 热替换跨 Program 版本的 span 边界

提交 `0b4e555d4` 将 `ProgramEpoch` 和 `ProgramRevision` 纳入 `RuntimeExecutionTimeline` 的 span 配对键。此前 span 已按 `ExecutionBranch`、SourceHandle 和 RuntimeInstance 隔离，但同一节点在兼容 Program adoption 前后仍可能共享其它身份；现在旧 Program 的未闭合 Graph/Node/Wait/Timeline/TreeClip 区间不会再由新版本结束事件闭合。

该修正只影响只读历史投影，保留每个事件自身的版本和对应 SourceMap 解析，不增加运行时状态、第二时钟或额外 capture。未执行 Build、Play 或测试。

## 2026-09-11 Session checkpoint 的跨 Actor 去重

提交 `eda2cdad2` 修正多 Actor Session 的 checkpoint 投影：Session Host 对每个注册 Actor 发布同一个 Session snapshot 的诊断事件，History 现在按 `Tick + ExecutionBranch + ProgramEpoch + ProgramRevision + SnapshotIdentity` 去重为一个 Session checkpoint。Actor 各自的 Tick、状态和表现事件仍保留在原始记录中，不把 checkpoint 误当成 Actor 私有状态。

该步只改变 Editor History 的聚合，不改变 Session Host 的 checkpoint 捕获或恢复协议；未执行 Build、Play 或测试。

## 2026-09-11 Timeline Action 结果的 Actor 隔离

提交 `129382853` 收紧 Timeline Live Debug 详情中的 `ActionResultSubmitted` 过滤：除了 `ActionInstanceId`，还必须匹配当前 playback 的 `CharacterRuntimeId`。多 Actor 运行时可以各自从相同的实例序号开始，不能只用数字把结果关联到当前 Timeline；本步不改变正式 Action Trace 或 Session 行为。未执行 Build、Play 或测试。

提交 `4b6eddf9c` 对同一详情面板的 Motion channel 做相同的 Actor 收口：`world_result_applied` 和 Timeline Motion 事件先匹配当前 Logic Tick，再匹配当前 playback 的 `CharacterRuntimeId`。只读观察不再把共享 Timeline 的其它角色运动事实放入当前窗口。

提交 `c00ba9331` 修正按 Timeline/TreeClip 实例查看历史时的恢复上下文：History projection 会在同一 `ExecutionBranch` 中寻找目标事件之前最近的正式 `SimulationCheckpointCaptured`，并把该点到目标区间的 Session 边界事件带入当前历史。这样 Timeline 窗口不会因实例筛选丢掉技能前最近恢复基线；状态快照仍只由正式 Session Host 持有，未增加历史副本或第二推进路径。

提交 `efc51c6a0` 将未闭合的执行区间纳入总时间轴 `IsComplete`：Capture 在 Graph、Node、Wait、State、Timeline 或 TreeClip 尚未收到正式终止事件时，仍保留该区间并标记 `completed=false`，但整体 `complete=false`。这区分了“事件可查看”和“整次执行已闭合”，不增加 capture 数据。

提交 `fb48d4667` 让 `BuildHistory` 复用同一次有限 Capture 的事件展开结果，实例筛选和 Session 边界补入不再重复创建相同事件列表。该步只降低历史刷新临时分配，不改变 Capture 容量、历史内容或运行时执行。

提交 `565c048ff` 收紧历史来源导航：打开 History/Execution span 前，Runtime Debug Navigator 现在按来源类型复用正式 Skill Graph fingerprint 或 `TimelineContentDiscovery`，核对当前作者内容与历史 `DebugSourceMapEntry.ContentHash`。来源内容已被作者修改、重复或无法解析时直接拒绝定位，不把旧 Program 的事件静默换绑到当前新资产。

提交 `a42e38969` 修正 Timeline Live Debug 的表现显示边界：只有真实 `TimelineVisualTime` 到达时才向 Slate projection 写入运行游标和 active clip/track；缺少表现时间时清空运行标记并显示 `visual unavailable`，不再用 Logic Tick 或零时间重建画面。

提交 `770af1214` 将“开始 Play 前不能使用过期产物”落实到 Coordinator：显式 Start 会在加载目标场景、读取 Context 后，按每个 Actor 核对 Definition 的当前 `SourceRevision`、ProgramId、Program SourceRevision 和 Projection SourceRevision。任何不一致都在 Product 阶段拒绝启动，并要求显式 Character Build；不会自动 Build，也不会先启动旧 Program 再补报过期。

提交 `9f7ef406a` 补上磁盘版本边界：显式 Start 和 Play 内 Character Build 会扫描 Definition 的正式 authoring 依赖，发现 `EditorUtility.IsDirty` 的未保存资产就拒绝继续，并指出具体路径。Program/Projection generated asset 被排除在该扫描之外；系统不自动 SaveAssets，避免把内存 authoring 修改挂到旧 SourceRevision 或保存无关用户改动。

## 2026-09-11 主线 HEAD 与预览有效消费基线

只读核对确认主线当前最新已提交 HEAD 为 `7f8fa937a`。其后续已提交内容主要落在 Pose/Agent authoring 校验、FlowCanvas 原生工具栏、界面说明和 Slate 焦点；预览运行链不直接依赖这些尚未形成新公共运行合同的提交。预览继续以已对账的 `6de43ab99` 及选定 Pose Resource Slot、Presentation Reset、StateMachine transition 引用扫描提交为有效消费基线，并保留自己的 Scene Play、ProgramEpoch、Diagnostics、History 和 Projection adoption 链。

主线工作树仍有 Agent v7 metadata、Projection、Mutation、Pose、Timeline 资产和生成产物等未提交改动；本轮没有从 dirty main 合并文件，也没有把这些改动写成预览依赖。`0.2`、`0.4`、`10.1–10.4` 继续等待主线 authoring owner 形成稳定公共合同。旧 RootTree 与新 SkillGraph 来源集合在当前 `d423510f0` 合入后的版本尚未取得新的运行时 SourceMap 校验，不以历史 `62aff1d95` 结果代替。

主线最新的 `1b21eae58`、`b0c64fba8`、`7f8fa937a` 还包含 Slate 焦点和异常清理修正；其中 `1b21eae58` 同时恢复独立 Timeline PreviewSession，预览没有整笔合入。提交 `6b5ebbe98` 只摘取无独立播放器的 Slate projection 初始化清理和 Slate 窗口焦点行为，继续由真实 Scene Play/Runtime Trace 提供技能执行。

同日重新扫描当前预览资产：`Pipeline/Skills/Timelines` 下有 7 条目标 Timeline，序列化 `TreeClip` 为 23 个，非零 `m_AssetTree` 引用为 23 个；`treeclip-migration-map.json` 也是 7 条目标 Timeline、23 个目标 TreeClip。旧 `Pipeline/Graphs/SharedTimelines/CorinAttack1Timeline.asset` 的 4 个 inline TreeClip 仍是迁移审计来源，不计入新 Skill Timeline 的运行资产。该扫描只证明当前文件结构，不替代 Program 重建后的 SourceMap、Decision/Commit 和输入回放验收。

继续按 `.meta` GUID 反查 Definition 引用，7 条目标 Timeline 各被一个 `CorinCharacterPipelineDefinition.Skill.*.asset` 引用；旧 shared `CorinAttack1Timeline.asset` GUID 在 3 个当前 Skill Definition 中均无引用。由此可以确认“旧 shared 文件还在”不是当前技能运行链仍指向它，但生成 Program/Projection 是否已经按这 7 条目标资产重新发布，仍必须通过当前 Build 结果和 SourceMap 版本核对。

## 2026-09-11 Slate Timeline 单窗口 Surface 对账

主线当前最新已提交 HEAD 为 `5eeaf7c2174057059e6dc48267915afb8cd8d88d`。本次只消费其中已经提交且边界清楚的 Slate 单窗口能力：

- 主线 `622ca053b` 为 `CutsceneEditor` 增加 embedded 初始化、指定尺寸绘制、重绘请求和清理接口；预览已由 `a3eda34cc` 精确 cherry-pick。
- 主线 `5bbb9d434` 的单窗口装配语义已在预览由 `a40bb3c3c` 完成。`TimelineEditorWindow` 现在把 `IMGUIContainer` 放在自己的 toolbar、ownership 和 debug details 之间，并调用 projection 的 `DrawEmbeddedGUI`。
- `BtsmtlSlateTimelineProjection` 负责 embedded `CutsceneEditor` 的创建、重建和释放；Timeline owner 仍拥有 Slate 的 selection、geometry、交互和 Mutation，预览没有复制这些逻辑。
- `EditorApplication.update` 在预览窗口中只调用 `IMGUIContainer.MarkDirtyRepaint`。embedded Slate 自己跳过 editor-time playback update，因此没有第二个时钟；技能执行、Timeline/TreeClip 调度和运行时间仍来自正式 Scene Play/Runtime Trace。

主线 `5bbb9d434` 同时携带的 `TimelinePreviewSession`、`TimelinePreviewTarget` 和独立 authoring 播放控制没有带入。该选择与本 change 的“真实 Play、唯一 Session、删除窗口播放器”条款一致。当前只完成源码与 diff 对账，按用户要求未执行 Build、Play 或 UI 端到端验证；因此这一步不能证明嵌入 Surface 在 Unity 中已经可操作或覆盖拖拽/overlay 的全部行为。

## 2026-09-11 Timeline 调用方身份刷新对账

提交 `f347874dc` 修正 FlowCanvas 技能入口的来源闭包：`RuntimeDebugSourceNavigator`、`BtsmtlSkillObservationSession` 和动画 Producer 导航打开 Timeline 时，现在都传入 `GraphAuthoringId` 与源 Timeline 节点身份。`TimelineEditorWindow` 的新打开入口把该联合身份保存到本地窗口状态，后续 Runtime Debug target、playback summary、History 和 TreeClip 聚焦继续按调用方 Graph 过滤；shared Timeline 被多个 Skill Graph 使用时，不会因为只传 `TimelineAsset` 而退化为空 Graph。

提交 `838705b10` 让 Timeline 窗口订阅同一个 `IBtsmtlScenePlayPreviewOperations.StatusChanged`。暂停状态完成 Build、Program adoption 或失败时，即使没有新的 Logic Tick，窗口也会刷新 BuildStatus、技能菜单和状态栏；`aba12ae0a` 又在 registry owner 变化时刷新/清除 Live Debug，避免旧 Scene Play 状态留在窗口。

这些提交只补作者观察绑定和状态刷新，不创建 ActionInstance、Timeline 播放器、时钟或第二个 Scene Play 状态源；本轮仍未执行 Build、Play 或端到端 UI 验证。

提交 `6cf42ba28` 和 `e26fe6ca0` 收口技能按钮到实际运行实例的观察绑定：窗口保存正式 `RequestSkill` 的 InputSequence、ProgramEpoch、SourceRevision 和 ExecutionBranchId；收到同 Actor、同 Skill、同版本、同分支的 `ActionResultSubmitted` 后，才按 ActionInstanceId 查找当前 Graph/Timeline 的唯一 playback 并 Pin。没有 playback、多个 playback、Tree-only 或用户已经手动 Pin 时不自动选第一个，显式保留未绑定/歧义状态。该链只改变 Editor 观察选择，ActionInstance 和 Timeline 仍由正式 Session 执行。

提交 `02eef6da8` 让 `SimulationSessionHost` 的恢复事务在正式 Presentation checkpoint capability 可用后仍保持原子：恢复/输入 Replay 前捕获调用前表现基线，目标表现恢复失败或异常时，在 Simulation/输入回滚后再恢复同一表现基线，并保留表现回滚错误。当前角色 Presentation runtime 仍明确 unsupported，所以本步没有把不完整能力伪装成可恢复。

提交 `872629320` 和 `1c93bd686` 修正 Capture History 回到 Live 的边界：`ResumeLive` 切回当前正式 Runtime Store，且无论活动 Provider 是否同时有新事件都通知观察窗口；恢复新 ExecutionBranch 后不再把旧分支当作当前 Live，也不等待下一 Tick 才刷新。提交 `6cc1bdc91` 保留旧 Capture 作为只读历史，只有 `CaptureHistory/Ended` 视图读取它，Live 视图读取当前 Store；作者仍可重新切回旧历史。提交 `dfd25bb06` 又让 Execution Timeline/History 只在 CaptureVersion、HistoryOffset 或目标/完整同步变化时重建，普通逐 Tick overlay 不重复扫描同一有限记录。

提交 `a74ef3c59` 让 Timeline Replay 控件要求精确可恢复 checkpoint 作为 `fromTick`，与 Session Host 的正式输入回放合同一致；Restore 目标仍可以从不晚于目标 Tick 的最近 checkpoint 回拉。本轮这些代码均只做静态审计，未执行 Build、Play 或测试。

提交 `70b32be56` 增加 Diagnostics Session 的成对 Execution Projection 入口，Timeline/History 共用一次有限 Capture snapshot，避免两个独立入口重复冻结和展开同一记录。提交 `3c356cd6c` 在运行 overlay 不可用或离开 Live Debug 时同时清除 Slate 投影时间，作者游标不会沿用旧 VisualTime。

提交 `de188caf3` 收紧后台 Build 生命周期：Scene Play 请求在后台 Character Build 未完成时清理，不再丢掉任务引用；下一次 Build 继续受同一 Task 门禁，完成后的旧结果在无活动请求时被丢弃，避免 Stop/重新 Start 产生并行 lowering 或旧候选采用。

提交 `c167e341e` 让 Scene Play 角色上下文就绪时自动启动现有 Diagnostics Store 的有界 Boundary capture，并在正式 Skill request 进入 Control Source 前升级同一 capture 为 Continuous。固定容量沿用 Store 的 512 segment/32768 event 上限；capture 失败会明确拒绝技能请求，不建立第二套历史存储，也不复制完整 Session 或表现对象。

提交 `4f6a69a32` 将该自动捕获的目标解析下沉到 `RuntimeDiagnosticsTargetRegistry.TryGetByHost`，预览协调器只消费明确 Host identity，不遍历 target 列表推测角色目标。

提交 `d01c42121` 让多 Actor Scene Play 的 Boundary capture 先完整解析所有 Host target，再统一启动，任一 target 缺失时不留下部分初始化的历史捕获。

提交 `b65ae95cd` 让 Session Host 记录最近完成的 Logic Tick，并提供正式 `TryCaptureCheckpointNow`；Scene Play 在 Skill 输入进入 Control Source 前，通过同一 Snapshot Codec 登记调用前 checkpoint。该基线与定时 checkpoint、Restore/Replay 共用 `m_Checkpoints`，不由预览复制状态对象。

提交 `af457372d` 曾尝试在 Session Preparation 完成时登记 checkpoint；静态对账发现该阶段没有正常的 `SimulationTick/StatePublished` 历史边界。随后提交 `454a6e7de` 收紧为：只在 Preparation 后启动 Boundary capture，等待第一个真实 Active Logic Tick 的正式 checkpoint 成功后才进入 Running/开放 Skill request，不伪造准备阶段 Tick。

提交 `38ccd983f` 将该 checkpoint Tick 通过 `BtsmtlScenePlaySkillRequestResult` 和 MCP request response 返回，并与 InputSequence、ProgramEpoch、SourceRevision、ExecutionBranchId 一起保留，方便后续 ActionResult/History 对账。

提交 `b9284f9cf` 让 Timeline Action 自动绑定进一步要求 `ActionResultSubmitted` 的 Logic Tick 不早于请求返回的 checkpoint 基线，避免同分支同输入序号的历史事件被重用。

提交 `23bcf1ce0` 让同一个正式 Build 菜单在 Play 的 Authoring Preview 模式也可见；作者可以保持 Slate 可编辑，直接提交 Scene Play 的 Character Build，Building 时菜单保留真实阶段但不允许重复提交。它没有增加 Authoring Timeline 播放器或第二个 Build 入口。

提交 `ca758d399` 将 GraphAuthoringId 纳入 Timeline Runtime Debug authoring fingerprint，shared Timeline 更换 FlowCanvas 调用方时不会复用旧调试 request；`b17f233c6` 补上 Timeline 窗口晚于 Scene Play 打开时的 Build menu 初始化。

提交 `b701e2f10` 收紧多 Actor Boundary capture 的启动事务：先解析并保留全部正式 Diagnostics target；本次启动的 Store 若在后续 target 上失败，会按逆序结束本次新开的 capture，已经存在的 capture 不会被预览误结束。这样 target 缺失或 Store 启动失败都不会留下半初始化的 Scene Play 历史捕获。

提交 `72859e5e8` 收紧 Build/adoption 期间的技能输入边界：Build 为 `Building` 或已发布但等待 Logic Tick adoption 的 `Published` 时，Coordinator 返回 `RejectedProgramAdoptionPending`，Timeline 技能菜单同步禁用。正式 adoption 完成后才重新接受请求，避免请求返回旧 ProgramEpoch、实际输入却在下一 Tick 被新 Program 消费。

当前 MCP History 进一步区分 `history_can_restore` 与完整 `can_restore`：前者只表示正式 Simulation checkpoint、历史完整性和 SourceMap 材料齐全；后者还要求当前 Scene Play 的 Presentation checkpoint capture/restore capability，并按 RuntimeDebug 当前附着的 Host identity 对账当前 Context 的 Actor。当前 `CharacterSimulationPresentationRuntime` 仍报告 unsupported，外部 Play 的历史也不会借用另一场 Scene Play 的恢复能力。

BuildStatus 现在记录 `BuildElapsedSeconds`、`AdoptionWaitSeconds` 和 `TotalElapsedSeconds`。后台任务完成时锁定实际构建耗时，进入 Published 后单独累计等待正式 LogicTick adoption 的时间；Timeline 状态栏和 MCP build/status 暴露三者，不把 Editor 轮询时间冒充编译耗时。

构建中和 Published 等待中，Coordinator 以 0.25 秒下限低频刷新同一只 BuildStatus；这只更新只读进度和窗口状态，不启动第二个构建、时钟或运行路径。

提交 `258ebe314` 收紧 Program SourceMap bridge：`CharacterRuntimeDebugProgramBuilder` 在 `DebugSourceMap.Seal()` 后逐个检查嵌套 Graph invocation 的父 Node/Edge caller handle；来源不存在时直接拒绝 Debug Program，避免 RuntimeGraphInvocation 保留一个无法回到 FlowCanvas 的 caller。

后续同一门禁继续检查 `ProgramInvocationCallerKind.TimelineClip` 的 `callerClipId`：它必须在父 Graph 的 Program SourceMap 中对应 `TreeClip` 来源，不能只凭 Graph Node caller 建立一个缺少具体 TreeClip 的映射。

History/Replay 能力也已拆开：若记录中包含 `SimulationNetworkModel` 外部结果，当前 Float32/Fixed runtime 又没有 replay journal，Timeline Replay 按正式能力显示不可用，MCP 输出 `replay_supported=false`；精确 Restore 不因此错误地被禁用。

本轮对 Presentation owner 的静态盘点确认：`CharacterSimulationPresentationRuntime` 暴露的 `CharacterPresentationRuntimeDiagnosticsSnapshot` 只包含已提交表现的观察数据；`CharacterPoseStateMachineRuntime.CreateSnapshot()` 也没有配对的 restore/apply 合同。当前不存在可供 `SimulationSessionHost` 复用的正式表现 checkpoint payload，因此继续保持成对 capability 为 unsupported；不把诊断快照、Reset 或作者 Timeline 重算接入恢复路径。

## 2026-09-11 Timeline UI责任边界重新收口

本轮文档对账确认：Timeline 不是 Scene Play 控制面。Start/Pause/Resume/Reset/Stop、Build、Skill request、Live Debug、Capture、History、Restore 和 Replay 必须由 SkillGraph/Graph Shell 调用唯一 Scene Play coordinator；Timeline 只承载 Slate authoring Surface、正式 Mutation/Undo 和只读 Runtime Trace overlay。

Scene Play 期间 Timeline 仍然可编辑。Clip/Curve/Section 修改通过正式 authoring Mutation 产生新 revision，由 Graph Shell 发起 Build；旧 Program 在 Build 期间继续运行，兼容候选在同一 Session 的 ProgramEpoch adoption barrier 采用，不兼容当前 Action 则延迟到下一次 Action。Timeline 不创建独立 `TimelinePreviewSession`、evaluator 或 clock。

提交 `ab2498864` 已把 Graph Shell 控制 Surface 接入共享 `GraphAuthoringEditorShell` 扩展入口，并从 `TimelineEditorMainWindow` 删除 Scene Play、Build、Skill、Live Debug、Capture、History、Restore 和 Replay 的本地 toolbar/状态；Timeline 现在只保留 Slate authoring Surface、owner/source 导航和素材/TreeClip 下钻。任务 6.0/6.2 已完成，6.1 仍保留 Graph Shell 的领域信息展示与最终 UI 验收。

本步验证边界：主线 Unity Editor 已重新编译且 Console 为 0 error；Scene Play worktree 当前没有连接的 Unity 实例。按项目统一 dotnet 命令编译时，被 worktree 既有的缺失 `BTSMTL.Editor/DragManipulator.cs` 以及 `RuntimeExecutionTimeline.cs` 的既有局部变量/访问级别错误阻断，输出没有出现本次新文件的错误；未把该结果扩大为 worktree Unity/Scene Play 端到端通过。

提交 `b90f74a47` 修正实际 Slate Embedded Surface 的第二轮问题：嵌入模式不再绘制 Slate 播放栏、Slate toolbar、Actor ObjectField 或 Add Actor Group；Clip 窗口使用外层 EditorWindow 的 `BeginWindows/EndWindows` 宿主；投影默认展开曲线轨道并选中首个 Clip；Surface 销毁时清理 proxy selection，Cutscene Inspector 遇到失效 target 不再抛 InvalidCastException。主线对应提交为 `7eb3c7a3d`、`1b431eba7`、`4bdad8556`。

## 2026-09-11 主线单线化

用户决定本change转入主线单线执行：分支`codex/btsmtl-scene-play-preview`（tip `4b7f7544e`，"同步预览曲线轨道聚焦优化"）与其worktree自当日起停止接收新提交，只作为尚未复制内容的来源和历史追溯。本文件自当日起落在主线`openspec/changes/design-btsmtl-authoring-runtime-workbench/`并作为唯一实现记录；此前897行历史记录原样保留自worktree。

后续由用户按范围指定、把分支成果逐块复制进主线（cherry-pick或文件级复制）。每批复制在此登记「分支提交↔主线提交」对应关系，并同步主线版tasks.md勾选；复制前确认主线tip与目标文件无其它窗口的未提交改动。单线化时主线tip为`f8bc2d853`（"补齐Timeline脚本绑定meta并清理已删除资源引用"）。

原COMM-20260906-01多窗口文档协作与EXEC-SCENEPLAY-20260906-01批次表随双线模式作废；其中仍然有效的业务约束（唯一场景运行、正式Session/owner边界、不新增窗口级播放器、证据先行、问题一次写全）已由design.md正文与specs承载。

## 2026-09-11 任务0.5：清除主线47a219bc0恢复的窗口级Timeline预览契约

主线提交 `47a219bc0` 恢复了 `TimelinePreviewSession`、`TimelinePreviewTarget` 与窗口级预览路径，与本change目标模型冲突。经用户当日本对话确认边界：预览只有两种——纯Timeline预览与SkillGraph预览，都属Scene Play协调器范畴；Timeline窗口只保留作者编辑Surface与被动Runtime Trace overlay，不自有预览会话。该决定即0.5要求的"主线明确边界"，结论是不拆分消费、直接清除。

主线实际状态核查：`TimelinePreviewSession`（文件名`TimelinePreviewRuntimeSession.cs`）在全主线无任何调用方，为孤儿代码；`TimelinePreviewTarget`的唯一继承者是`CharacterPipelineHost`（`Runtime/Character/Pipeline/Unity/CharacterPipelineHost.cs`），其四个override（`CanPreviewTimeline`/`PreviewStatus`/`EvaluateTimelinePreview`/`ClearTimelinePreview`）在全主线零消费方；窗口`TimelineEditorMainWindow.OnEditorUpdate`仅调用`m_SlateSurface?.MarkDirtyRepaint()`，不驱动预览——0.5所述"窗口级Editor update/SetTime"中SetTime路径在主线已无接线。

主线原生清除内容（非分支复制；分支侧等价工作在9.1，由`ab2498864`系提交完成，本条与其互为记录）：
- `CharacterPipelineHost`基类由`TimelinePreviewTarget`改为`MonoBehaviour`，删除上述四个override；`CanPreviewPoseGraph`改为内联原`CanPreviewTimeline`表达式，宿主自身Pose预览链（`EvaluatePoseGraphPreview`、`ClearAllTimelinePreviews`、`m_PreviewController`系）保持不变，不属本条范围。
- 删除`Timeline/Editor/Scripts/Preview/TimelinePreviewRuntimeSession.cs`（含meta）与`Timeline/Scripts/TimelinePreviewTarget.cs`（含meta）及清空后的`Preview`目录meta。

验证边界：三处目标文件编辑前工作区干净，符合清单头部复制前条件；编辑后全库`.cs`对`TimelinePreviewSession`/`TimelinePreviewTarget`引用为零；未跑Unity编译与端到端，按项目规则由用户验收。9.2–9.4所述`AnimationPreviewEngine/Controller`、Pose fixture等完整角色预览路径的删除仍待后续批次，本条不覆盖。

## 2026-09-11 任务0.4：Scene Play 分支↔主线类型/程序集/owner 静态对账

对账基线：主线 tip（本日 `af58da853` 时点）vs 分支 `codex/btsmtl-scene-play-preview` 停写 tip `4b7f7544e`（含其对主线 `d07479b0e` 正式 Presentation Reset capability 等提交的消费，作为过渡审计快照）。GameScripts 范围 `git diff --name-status`：A=40（其中 14 个属 AgentAuthoring，按 0.2 归其它 change，不复制）、M=196、D/R 为主线侧后续演进与分支重命名，不作为复制对象。

分支独有 Scene Play 核心面（A 类，主线不存在，可整文件复制的类型清单与程序集归属）：

| 程序集/目录 | 类型 | 任务 |
|---|---|---|
| Editor/CharacterPipeline/Preview | `BtsmtlScenePlayPreviewCoordinator : IBtsmtlScenePlayPreviewOperations`（唯一协调器） | 4.x |
| 同上 | `BtsmtlScenePlayGraphShellToolbarExtension : IGraphAuthoringShellToolbarExtension`、`BtsmtlScenePlayGraphShellToolbar : VisualElement` | 6.0/6.1 |
| 同上 | `BtsmtlScenePlayPreviewMcpTool` | 诊断/MCP |
| Runtime/Gameplay | `BtsmtlScenePlayRequest`、`BtsmtlScenePlayProgramAdoptionReport`、`IBtsmtlScenePlayPreviewOperations`、`IBtsmtlScenePlayRuntimeOwner` | 2.1 |
| Runtime/Character/Pipeline/Unity | `BtsmtlScenePlayContext : MonoBehaviour`（+`BtsmtlScenePlayContextRegistry`）、`BtsmtlScenePlayResourceRuntimeOwner` | 2.2 |
| Runtime/Simulation/Core | `SimulationProgramEpoch`（Composition）、`Float32CharacterInputTraceModule`（Float32/Diagnostics） | 2.6/5.3 |
| Runtime/BTSMTL Timeline Editor | `TimelineEditorCursorState`（编辑游标视图数据） | 6.2 |
| Runtime/BTSMTL Diagnostics Editor | `RuntimeExecutionTimeline`（动态执行时间轴） | 6.10/6.11 |
| Editor/CharacterSimulation/Build | `CharacterSimulationBuildOrchestrator.Background.cs` | 8.6 |

复制顺序约束（M 重叠文件，必须按分支提交 cherry-pick，禁止整文件覆盖）：
- `IGraphAuthoringShellToolbarExtension` 主线不存在，定义在分支 `Runtime/BTSMTL/TreeDesigner/Editor/Scripts/Window/GraphAuthoringEditorShell.cs`；Graph Shell 工具条（A）复制前必须先落该扩展点段（6.1）。
- `Editor/ProductStartup/EditorPlayModeSceneLauncher.cs` 及调用方 `GameplayLabEditorLauncher.cs`/`GameplayLauncherWindow.cs` 双侧演进，3.x 启动器重构按提交摘取。
- `TimelineEditorMainWindow.cs`、`TimelineEditorSessionContext.cs` 双侧演进（主线已 Slate 化），6.2 观察接线按提交摘取。
- `AnimationPreviewEngine.cs`/`AnimationPreviewAdapters.cs` 双侧都有，为 9.2 删除目标，到达 9.x 批次时按分支提交删除，不提前。

owner 结论：合同在 Runtime/Gameplay 与 Simulation/Core，场景上下文在客户端 Unity 边界，编排只在 Editor 程序集——A 文件分布符合 design 决策 8 的单向依赖，无公共 Simulation/Timeline 程序集反向引用 Editor 的新增。已勾任务（2.6/4.8/4.9/5.3/6.0/6.2/6.10/6.11/8.1/8.7/9.1）的实现均落在上述 A+M 集合，勾选为分支记账，复制落地时逐批转正为主线进度。逐类型签名级对账随每批复制的「分支提交↔主线提交」登记执行，本条完成文件级与类型级对账。

## 2026-09-11 复制批次1：场景合同、上下文与Graph Shell控制面落地主线

按0.4对账执行第一批复制，来源为分支停写tip `4b7f7544e` 的checkout path（文件级；分支侧6.0的 lineage 提交为 `ab2498864` 系）。复制前核查：merge-base `9c7766d32` 之后主线侧未触碰本批任何M文件（启动器、SessionHost、GraphAuthoringEditorShell及两个调用方），整文件取分支版即安全超集；目标文件工作区干净。

落地内容（24文件，+6533行）：
- 运行时合同（2.1）：`Runtime/Gameplay/BtsmtlScenePlayPreviewContracts.cs`（`BtsmtlScenePlayRequest`/`BtsmtlScenePlayProgramAdoptionReport`/`IBtsmtlScenePlayPreviewOperations`，namespace `ThirdPersonGameplay.ScenePlay`）、`BtsmtlScenePlayRuntimeOwner.cs`。
- 场景上下文（2.2）：`Runtime/Character/Pipeline/Unity/BtsmtlScenePlayContext.cs`（+`BtsmtlScenePlayContextRegistry`）、`BtsmtlScenePlayResourceRuntimeOwner.cs`（`IBtsmtlScenePlayRuntimeOwner`实现）。
- ProgramEpoch（2.6主线落位）：`Runtime/Simulation/Core/Composition/SimulationProgramEpoch.cs`。
- 启动器重构代码（3.x）：`Editor/ProductStartup/EditorPlayModeSceneLauncher.cs`（显式启动请求、`TryGetPendingRequest`/`Start`/`ReloadInPlayMode`/`StateChanged`）及调用方 `GameplayLabEditorLauncher.cs`、`GameplayLauncherWindow.cs` 迁移；3.1–3.6暂不勾选，与分支口径一致，待用户端到端验收。
- Graph Shell控制面（6.0）：`GraphAuthoringEditorShell.cs`（含 `IGraphAuthoringShellToolbarExtension` 扩展点）、`Editor/CharacterPipeline/Preview/BtsmtlScenePlayGraphShellToolbar.cs`、`BtsmtlScenePlayPreviewCoordinator.cs`（唯一协调器）、`BtsmtlScenePlayPreviewMcpTool.cs`。
- 动态执行时间轴呈现（6.10表面）：`Runtime/BTSMTL/Diagnostics/Editor/Scripts/RuntimeExecutionTimeline.cs`。

依赖闭合验证：协调器引用的 `EditorPlayModeSceneLauncher` 新API、`SessionHost.ProgramEpoch`（主线原SessionHost无此属性，本批整取分支版补齐）、`RuntimeDebugSession`（主线与分支该文件逐字节一致）、`RuntimeDebugSourceMapSnapshot`（主线已有）全部在主线可解析；启动器主线调用方仅本批三处，已全部随批迁移；未复制 `TimelineEditorCursorState`/`Float32CharacterInputTraceModule`（本批无引用，留待6.2/5.3批次）。

边界与未含：Timeline窗口侧的被动观察接线（6.2窗口段）、领域信息展示与最终UI验收（6.1）、协调器状态机的验收级勾选（4.1–4.7，分支亦未勾）、Build后台拆分（8.6）不在本批。未跑Unity编译与端到端，按项目规则由用户验收：编译 + 打开任一Graph Shell确认Scene Play工具条出现。

## 2026-09-11 复制批次2：诊断/模拟合同层批量同步与编译收口

批次1编译暴露的依赖瀑布按层闭合后，对剩余差异执行原则化批量同步：分支相对主线全部差异文件中，主线自合并基线 `9c7766d32` 后未演进且工作区干净的整取分支版，共83文件；主线演进的104个（Pose/相机/表现等主线新工作）与工作区脏的8个（含用户未提交工作）一律排除。同步覆盖 Simulation 合同（ActorRegistration含Checkpoint/ProgramEpoch/ExecutionBranch端口、ActionSkillExecution、TimelineControl）、诊断层（Context/Store/ViewModel/TargetRegistry/合同）、表现运行时（Body/相机/设备/工厂）、Build后台与Graph Shell/Timeline编辑面。

批次1自己的遗留也一并清账：
- `RuntimeExecutionTimeline.cs` 三处分支自带错误修复（`active`/`key`作用域重名、`PendingSpan.Start`改public）；
- 两个分支半成品Inspector（`TimelineClipInspectorView`/`TreeClipInspectorView`引用从未存在的`EditorView`）回退主线版；
- `AnimationPreviewEngine`/`AnimationPreviewAdapters`为9.2半删除状态（引擎引用已删adapter），回退主线版，待9.x批次按任务统一删除；
- `BtsmtlSkillObservationSession`/`RuntimeDebugSourceNavigator`主线本有实现，误以分支版覆盖又误删，已恢复主线版；其分支增量（三参`Open`重载、`FocusSource`、`FlowGraph.AuthoringId`）依赖的窗口导航API属6.2批次实现，本批不含；
- 主线演进文件的手术移植：`CharacterPipelineHost`补4个adoption/input成员（TryPrepare/Discard/TryQueueProgramAdoption、TryQueueInputRequest，并剔除随range拖入的重复LiveTuning两属性）并以主线新工厂实现`presentationRuntimeFactory`；`AnimationPresentationRuntimeTarget.Replace`移植（m_Provider去readonly、ProgramIdentity改private set）；`CharacterSimulationPresentationRuntime.TryGetLatestBody`移植并升public；`CharacterSimulationTickTargets.ReplaceRuntime`升public（Fixed程序集跨界调用，分支同名internal为分支自带错误）；
- `MotionMatchingQueryFixture`按主线ctor补`TimelineData`空参（引擎显式处理null）；
- `ThirdPersonSimulation.Fixed.Unity.asmdef`补`ThirdPersonSimulation.Float32`引用（分支asmdef同样缺失，分支自带错误）。

验证：主线Unity Editor全量编译 **0 error**（MCP read_console确认），涵盖此前13处报错的全部修复。任务11.1编译门禁据此勾选。排除文件（主线演进104个）与6.2窗口接线、9.x删除批次、4.1-4.7验收级勾选为后续范围。

## 2026-09-11 MovingTurn root motion 修复：重建控制运动曲线载体

用户运行时验收发现 MovingTurn 无 root motion 位移（in-place 动画正常播放）。取证：`6f5a99fff`（09-09"清理移动转身的旧曲线引用"）把控制模块 MovingTurn 的运动请求从旧 RootTree 内嵌 Timeline（`CorinMovingTurnRootMotionTimeline`，authoring id `8a6491b4`，MotionCurveClip `MovingTurn180` 0..28 帧）的 `SourceCurve` 位移改成 `ConstantSpeed` 顶账——旧 RootTree 退役后曲线载体与编译入口一起消失，迁移基线（refactor-btsmtl-authoring-architecture/baseline.md）规划的"迁为控制模块静态 typed Body Motion descriptor"只做了 descriptor 一半。

本批按迁移意图补完：
- 从历史 `4fad93882` 的 RootTree 嵌入数据完整抢救 `MovingTurn180` 曲线（Weight/PositionX/Y/Z/Yaw/EaseIn/EaseOut，28 帧采样），重建独立资产 `CorinMovingTurnRootMotionTimeline.asset`（AuthoringId 沿用 `8a6491b4`/`e04f4e26`，保证控制模块身份字符串不变）；
- `CharacterPipelineDefinition` 新增 `m_ControlMotionTimelines`（TimelineData[]）序列化字段与访问器，Corin Definition 资产引用上述 Timeline；
- 编译器：`CharacterControlMotionCompilationDiscovery` 增加控制运动 Timeline 直采（identity `timeline:{id}/clip:{id}`，Graph/Timeline 允许 null），`CharacterSemanticEmitter` 跳过无图 motion，新增 `CharacterControlMotionCatalogEmitter` 按 Timeline/TimelineTrack/MotionCurve 三级 Catalog 契约声明曲线（SemanticDataWriter VRUC 格式与 TimelineSemanticEmitterRegistry.BakeCurve 一致），`CharacterSemanticFrontendCompiler` 接线；
- 控制模块恢复 `s_MovingTurnSourceMotion` + `SourceCurve`（与 6f5a99fff 之前逐字节同义）。

验证：主线 Unity Editor 全量编译 0 error。运行时验证（Build 后转身弧线位移回归）由用户端到端执行。

## 2026-09-11 相机上下反向修复：Look鼠标绑定补Y反相处理器

用户运行时验收报告相机上下操作反向。取证：Corin InputProfile 的 LookAxis 绑定到 `InputSystem.inputactions` Player map 的 Look action，其 `<Pointer>/delta`（鼠标增量，Unity 约定 Y 正方向朝下）与 `<Gamepad>/rightStick`（Y 正方向朝上）共用且均无反相处理器——鼠标向上推产生负 y，FramePlanner `m_PitchOffset += look.y * Sensitivity.y` 后相机朝下，上下反向；手柄摇杆方向正确。

修复：`<Pointer>/delta` 绑定加 `InvertVector2(invertX=false,invertY=true)` 处理器（仅鼠标绑定，手柄摇杆不反相）。人物不居中为俯仰反向的伴随症状（对抗反向鼠标把 pitch 拉到 ±70 限位导致极端机位），随本修复重测；若重测后仍偏，再对照 dump 内 CameraZooms/CameraCutscenes 数据调构图（标准跟随相机的距离参数不在 dump 范围内）。

## 2026-09-12 相机构图修复：按 ZZZ dump 参数校正 Default 序列轨道采样

对照 ZZZ dump 提取表（rebuild-character-camera-from-zzz/evidence/source-behavior.md 引用的 基础镜头.md）发现两处实差并修复：

- `Default_Normal` 的 `ELEVATION_ANGLE` 为 `0.60000002`，生成投影里 Default 序列 stage 误授权为 `0.5`，已改为 `0.60000002`；
- 轨道/ScreenOffsets 采样原实现为按下标 Floor 跳变，ZZZ 原始行为是 keys 0.0/0.5/1.0 之间的连续插值（evidence line 37 明确记录 Floor/RoundToInt 不能代表原采样边界），`CharacterCameraFramePlanner.SampleTrack` 两个重载改为连续插值并带单元素防御。

ZZZ Default_Normal 参考值（供后续手感调参）：FOV 50、默认仰角 0.6、CameraLocateRadius 3.75、平滑时间 0.15、轨道 (2.225,2.5)/(0.225,3.75)/(-0.775,2.2)、ScreenY Top 0.5/Middle 0.5/Bottom 0.35、阻尼 Horizontal/Vertical 0.5、Drag 轴 X MaxSpeed 1550/Y MaxSpeed 25（AccelTime 0.2/DecelTime 0.1，InvertInput false）。当前投影轨道与 ScreenY 数据与 dump 一致；灵敏度 0.12/0.12 为唯一无 dump 依据的授权值，手感仍不满意时优先调它并考虑按 ZZZ CameraAxis 模型补平滑。

验证：Unity 全量编译 0 error；构图/手感回归由用户端到端执行。
