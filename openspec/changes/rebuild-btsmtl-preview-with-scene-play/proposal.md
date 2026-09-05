## Why

BTSMTL 已明确收敛为技能编辑器，角色控制迁入 C#；完整预览需要在可配置独立 Unity 场景中运行相同的控制、动作服务、技能执行和表现链。本变更统一场景启动、技能与释放实例选择、运行观察、直接作者调参和退出恢复，并替换原先由作者窗口独立求值的预览路径；具体预览故障尚未复现，数据编译耗时也没有独立实测结论。

## What Changes

- **BREAKING**：完整角色预览统一为“独立场景 → 正式 Session/Actor → C# 控制选择技能 → 唯一 Action 服务准入并建立 ActionInstance → 技能 Root → 正式世界与表现输出”。窗口沿正式输入发起试验，不直接指定技能执行结果，不维护可编辑角色总控 RootTree。
- 作者选择以 Character Definition、SkillDefinition 和技能作者调用路径定位内容；运行观察以场景 generation、Session、Actor、ActionInstance、SkillProgram 和调用 generation 定位一次释放。技能可以为 Tree-only，也可以包含多个或嵌套 Timeline；Tree → Timeline → TreeClip → 子树完整保留。普通 Locomotion 与默认相机观察只需 Actor，不创建空技能或 ActionInstance。
- 建立唯一 editor-only 场景预览协调器，扩展已有场景启动入口处理检查、进入 Play、目标连接、暂停、继续、试验重建、停止和失败恢复。Graph、技能工作区、Timeline、Pose Graph、Blend Space 和 MM 页面复用同一次受控运行，各自保留本地页面和观察绑定。
- **BREAKING**：删除被替代的窗口级 Timeline/动画预览会话、Fact/Action/Query fixture、私有场景、时钟、独立 Motion 求值及其配置。共享作者交互、技能 Tree/局部状态机、正式编译和运行算法继续按主重构后的合同接入。
- 运行中修改合法参数直接写入并保留正式作者资产，经唯一 Capability、Mutation、Validator 和 Undo 管理。当前实例只采用正式运行端口支持的参数；作者已修改、运行待生效、已采用、需要构建分别显示。技能内容、控制代码/模块合同和状态布局更新经正式发布并由新 Session 采用，不通过预览热换 Program。
- 消费主重构的 Document v5，控制 binding/参数、SkillDefinition/技能内容与 Presentation 沿其唯一作者闭包同步；不恢复角色图正文、v4 兼容入口或并行 SkillInstance 生命周期。场景配置与运行状态不进入 Character/AI Document 的可写业务正文。
- 重复输入与支持的参数调整在一次 Play 内进行；产物仍匹配时，重建试验完整结束旧 Session 并重载独立场景。编辑游标与 Capture 历史只定位内容/已记录结果，不通过 seek 修改 Gameplay 状态。
- 本 change 唯一负责受控场景预览生命周期。Camera change 只提供正式 Runtime、Projection、Rig/目标/物理绑定、正式重置和只读诊断；相机 fixture 若保留为场景输入，不得形成独立命令源、播放器或状态 seek。
- 提供明确的构建并开始／构建并重启，检查控制模块 binding/版本、SkillProgram 目录与依赖、Program/State 布局、Numeric Target 和 Projection。分别显示实际的数据构建、分析、发布、Play 进入及 Session 准备耗时；代码构建等待与技能数据构建分开。
- 用主重构后有效的 Corin 正式资源交付独立预览场景。保护正确的 Motion/KCC/Pose/IK/相机/渲染算法，不修补 TrainingEnemy，也不借预览补齐未安装的战斗、VFX 或 Audio 能力。

## Capabilities

### New Capabilities

- `btsmtl-scene-play-preview`：独立场景运行所有权、正式控制与技能输入、作者/释放实例身份、多窗口观察、直接调参、试验重建、Camera 接口、构建阶段和来源版本校验。

### Modified Capabilities

- `btsmtl-timeline-editor-preview`：窗口播放器改为技能实例下的真实 Timeline 观察，保留嵌套 TreeClip 及同 Tick Decision/Commit 顺序，区分作者游标、运行标记和历史位置。
- `graph-authoring-editor-shell`：共享外壳承载场景操作及本地观察；技能/AI/Pose 沿各自作者领域接入，C# 控制配置与代码来源不伪装成角色图。
- `graph-authoring-domain-framework`：领域提供作者字段编辑资格与运行采用合同，保持技能、代码控制配置、Pose 和只读观察的边界。
- `character-action-animation-authoring-workspace`：消费以 SkillDefinition 为根、Tree-only/多个/嵌套 Timeline 的工作区合同，增加受控场景中的精确 ActionInstance 观察。
- `character-presentation-pose-graph`：真实 Actor 的 Pose Plan 和 World Context 替代独立 Fact Preview，保留 Actor 级观察与直接作者调参。
- `character-pose-graph-runtime-architecture`：移除独立 Preview 装配和预览 seek 重置原因，使用真实 Actor 的正式参数、Fault 和 Dispose。
- `character-animation-pipeline`：完整预览使用同一控制、动作、技能和表现链，保留唯一正式 Timeline、Pose Plan 与帧事务。
- `character-animation-layer-runtime`：动画观察按真实 Actor/ActionInstance/技能调用区分，删除完整角色的独立输入 fixture。
- `character-animation-selection-runtime`：source、Action、Routing 和释放由正式 Actor 拥有，普通移动不要求技能实例。
- `character-motion-matching-presentation-module`：真实 Actor 的正式查询和结果观察替换完整角色 Query Fixture Preview。
- `character-pose-inertialization`：删除窗口 seek 输入，保留正式初始化、替换和重置的惯性处理。
- `gameplay-simulation-session-composition`：预览场景经正式 Composition 装配控制模块与技能目录，保持唯一 Session/Pipeline 和版本检查。
- `unity-simulation-assembly-ownership`：场景编排单向依赖正式公共合同，不新增 Preview Composer、代码控制解释器或反向 Editor 依赖。
- `btsmtl-agent-authoring-document-sync`：预览调参消费目标 Document v5，保持五个生命周期、整包事务、Play Mode 门禁和生成数据只读边界。

## Impact

- 共享接口基线：`refactor-btsmtl-authoring-architecture` 规划提交 `d99093011`、显式控制 StateMachine/技能独立澄清 `3bf66c4ea`；`cbcd7fa42` 只证明基础控制合同已提交。完整迁移仍在实施，本提案描述目标接口，实际接线应记录使用的精确后续提交和发布版本。
- 依赖分工：`btsmtl-compiled-simulation-program`、`btsmtl-runtime-diagnostics`、SkillProgram、控制 FSM、ActionInstance 状态和 Document v5 升级由主重构的共享 delta 拥有；本 change 消费它们，在预览规范中约束检查和观察行为，不重复定义 Program ABI、来源 schema 或第二 Document 迁移。
- 编辑器：共享 Graph Shell、BTSMTL 技能/Timeline 页面、Character 作者工作区和现有场景启动器。保持已完成的焦点、草稿、窗口恢复及单次订阅行为；运行视图不覆盖未提交作者输入。
- 数据与运行：独立场景引用主重构后有效的 Character composition、控制配置、技能目录和 Projection；仅新增场景运行编排与正式端口适配。预览控制不进入 portable Program/State ABI 或网络协议，控制与技能的快照/恢复继续归主重构。
- 规范组合：现行主 spec 的角色总控图、唯一 Timeline 和 Document v4 由主重构 delta 更新；本 change 的工作区重叠条款与该目标合并，不能后续安装旧版本覆盖新合同。预览场景启动与恢复可先按稳定合同开展，具体技能/来源/v5 消费只等待其所需接口提交，不要求整份主重构完成，也不阻塞主重构实施。
- Camera 对账：相机原 design 第 9 节、tasks 10.1–10.3 及 Timeline Preview delta 尚有旧会话/fixture/seek 表述，其 Document 段也需消费 v5；本 change 已明确唯一场景 owner 和相机提供者边界。相机窗口负责在自己的原 change 内修订，不由本次覆盖。
- 迁移完成后的比较：同版本产物和运行检查继续严格；跨重构的 Program/Layout/Event/source identity 允许改变，沿既有比较工具核对语义输入、Body、动作阶段、窗口和输出，不能仅按 hash 差异判断回归或忽略业务差异。
- 本次只修订原 change 的规划文件；不执行实现、构建、场景操作或修改其它任务的文件。旧代码/规范不一致及待接入接口在设计对账表中明确列出。
