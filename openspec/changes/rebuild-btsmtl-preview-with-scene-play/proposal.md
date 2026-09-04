## Why

当前 BTSMTL 的 Timeline、Pose Graph 和动作工作区分别通过编辑器预览会话提供表现输入，与正式场景运行的输入、Gameplay、物理和表现生命周期存在边界差异；用户希望通过一个可配置的独立 Unity 场景，直接运行正式角色来编辑、观察和调参。本变更统一完整角色预览的启动、目标、运行观察、参数生效和退出恢复，避免继续扩充另一套编辑器执行流程；目前没有复现具体预览故障，也没有数据证明 BTSMTL 编译耗时已经构成瓶颈。

## What Changes

- **BREAKING**：完整角色预览统一为“明确选择独立预览场景和角色 → Unity Play Mode → 正式 Simulation Session 与 Character Presentation → BTSMTL 观察、输入和调参 → 退出恢复原编辑环境”。场景配置提供环境、角色、目标与起始条件，角色仍引用正式 Prefab、Definition、Composition 和已发布产物。
- 建立唯一场景预览协调器，沿现有场景启动器处理启动前检查、进入运行、连接目标、暂停、继续、重建试验、停止和失败清理。Unity 实例内一次只有一个受控预览运行；Graph、Timeline、Pose Graph、Blend Space 和 Action Workspace 复用其运行上下文，各自保持本地选择和只读观察绑定。
- **BREAKING**：删除被替代的 `TimelinePreviewSession`、`TimelinePreviewTarget`、独立动画预览 Runtime、Fact/Action/Query fixture 及相关时钟、资源接管和恢复路径。保留共享作者画布、Timeline 数据编辑、曲线绘制、正式 Mutation、已发布运行算法及只读 Diagnostics；不重写 Gameplay、Pose、IK 或相机算法。
- **BREAKING**：运行中修改参数直接写入并保留正式作者资产，复用同一 Capability、领域 Mutation、Validator 和 Undo。只有领域正式支持的参数才提交给当前真实 Actor，分别显示作者数据已修改、运行中待生效、已生效或需要重新构建；不设置试用副本或“保存预览参数”第二入口。
- 重复试验复用同一次 Play，通过完整销毁并重新建立正式场景会话恢复初始条件。普通编辑游标和 Capture 历史浏览不改变运行状态；本次不承诺任意时间跳转或回放恢复，不以移动游标伪造 Gameplay 执行。
- 明确 Build 和 Play 是不同操作；提供显式“构建并开始／构建并重启”，保留精确 Definition 与 Numeric Target。记录数据编译、分析产物生成、发布、进入 Play、会话准备和运行连接的独立耗时，显示实际失败阶段，不自动安装热更新插件或修改 Enter Play Mode 设置。
- 提供一个以现有稳定 Corin 资产为基础的正式独立预览场景及配置，使用当前已闭合的运行能力；不迁移或修补受其它变更约束的 TrainingEnemy，也不借本次补齐尚未存在的相机、VFX、Audio 或战斗 consumer。

## Capabilities

### New Capabilities

- `btsmtl-scene-play-preview`：独立场景配置、唯一预览运行所有权、正式输入与会话生命周期、多窗口连接、直接作者调参、试验重建、退出恢复、耗时和诊断。

### Modified Capabilities

- `btsmtl-timeline-editor-preview`：将窗口拥有的表现预览会话改为场景运行绑定，明确真实 Timeline 时间、TreeClip 执行归属、编辑游标和运行控制。
- `graph-authoring-editor-shell`：由共享外壳承载领域提供的场景预览控制与状态，窗口关闭只释放本地视图，不接管运行时生命周期。
- `graph-authoring-domain-framework`：统一字段编辑影响与运行调参资格，保留共享 Capability、类型化 Mutation 和只读观察边界。
- `character-action-animation-authoring-workspace`：动作试验通过真实角色输入与 Action admission 产生，工作区观察正式 Gameplay、动画和相机结果。
- `character-presentation-pose-graph`：以场景真实 Actor 的 Pose Plan、World Context 和调参状态取代独立 Fact Preview，并保持精确 source mapping。
- `character-pose-graph-runtime-architecture`：移除独立 Preview Module 装配和预览专用重置原因，统一到真实 Actor 的构造、参数更新、Fault 和 Dispose。
- `character-animation-pipeline`：替换禁止场景预览执行 Gameplay 的旧约束，保持唯一正式 Timeline、Pose Plan、表现帧事务与最终写入。
- `character-animation-layer-runtime`：Preview 和 Live 使用真实运行事实，移除外部 Fact、Action、Query fixture 作为完整角色预览输入的合同。
- `character-animation-selection-runtime`：完整预览的 source、Action、Routing 和释放全部由正式角色拥有，不再由编辑器合成 Selection。
- `character-motion-matching-presentation-module`：用真实 Actor 的正式查询和结果观察替换完整角色 Query Fixture Preview。
- `character-pose-inertialization`：移除窗口非连续 seek 的触发合同，保留正式初始化、替换和重置的惯性处理。
- `gameplay-simulation-session-composition`：允许独立预览场景经正式公共 Composition 创建 Session，继续禁止第二套预览 Composer、Kernel 或模型推断。
- `unity-simulation-assembly-ownership`：明确 Editor 场景编排与正式 Runtime 装配的单向依赖，移除旧表现专用预览的程序集限制。
- `btsmtl-agent-authoring-document-sync`：场景预览调参进入现有作者数据同步，保持 Document v4、五个生命周期、Play Mode 写入门禁及严格生成数据只读边界。

## Impact

- 编辑器：`Main/Runtime/BTSMTL/TreeDesigner/Editor`、`BTSMTL/Timeline/Editor`、`Main/Editor/CharacterPipeline/Authoring`、`Main/Editor/ProductStartup/EditorPlayModeSceneLauncher.cs`。复用已有作者入口和启动链，不创建新的 Workbench。
- 运行与构建：`Main/Runtime/Character/Pipeline/Unity` 的预览适配和 Session 连接、正式 Live Tuning 接口、`Main/Editor/CharacterSimulation/Build` 的状态与耗时报告。预览控制不进入 portable Program ABI、Rollback snapshot 或网络协议。
- 数据：新增预览场景和正式预览配置；删除只服务旧角色预览的 fixture 配置、序列化窗口字段、引用和资源。Character 作者资产仍是唯一业务真相，运行中参数修改不会因退出 Play 被撤销。
- 规范对账：上述现行规范中的“Preview 不执行 Simulation”“每窗口独立动画会话”“Live 模式一律禁止作者字段修改”需按本提案替换；Diagnostics 本身只读、显式 Build、来源身份、模块所有权和主写入链继续保留。
- 并行变更：`rebuild-character-camera-from-zzz` 的表现专用 Preview fixture、独立命令源和 seek 合同与本方向冲突，必须按设计中的对账表改写后实施；其相机行为移植范围不变。`refactor-character-pose-graph-architecture` 已完成的 Runtime 分层、事务和 Tuning 工作直接复用；不覆盖其它任务未提交的代码和规范。
- 本次只产出提案、设计、delta specs 和实施任务，不修改项目代码、Unity 资产、现行主 spec 或其它 active change，不执行构建和运行，不新增测试。
