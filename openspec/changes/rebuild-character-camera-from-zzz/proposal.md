## Why

当前 3C 相机已经进入唯一 Character Presentation 调用链，但实际仍以固定 FOV、有限模式和单个 FreeLook 为主，无法承载 ZZZ 的基础构图、状态序列、真实转场、技能相机效果和完整作者配置。用户要求按照 ZZZ 完整移植本轮角色相机核心，并在 SkillProgram Root、技能局部 Graph、TreeClip/Timeline 和 Character 编辑器内完成配置、预览与排查；本变更以原行为和依赖资源全部有对应实现为完成条件。来源证据未闭合的部分必须保持明确失败，不能以近似画面冒充完成。

## Scope

本变更的运行归属是单个 Character 的正式 Presentation Camera Runtime，以及 SkillProgram Root、技能局部 Graph、TreeClip/Timeline 提供的相机请求。相机可以对同帧明确提供的敌人、Boss 或多个目标进行锁定和构图，但不实现队伍切人、主控 Actor 切换、跨角色接管相机或换人生命周期。C# Locomotion 控制拓扑不提供 Camera 图节点，也不维护平行控制图。来源中的 `ChangeAvatar`、`SwitchIn`、`SwitchOut` 等身份只作为待后续设计的来源依赖，不自动转成当前框架的运行能力。

本轮 `CameraSequenceAsset`、Stage 类型和 Timeline 相机 Clip 是当前工程的适配方案，不宣称它们已经还原 ZZZ 原版作者编排层。Timeline 的相机时间、循环和事件归属在不扩大单角色范围的前提下留作后续设计确认。Preview 会话、独立预览命令源、fixture 执行器和 seek 重建统一归 `rebuild-btsmtl-preview-with-scene-play`；本变更只提供正式 Camera Runtime、Projection、Rig/目标/物理绑定、Reset 和只读诊断，不建立 Camera 自有 Preview 会话。

## What Changes

- 建立 ZZZ 角色相机的版本、原资源身份、字段、函数、调用顺序、时间语义与输出实例对应表，追通 `PipelineCamera / CameraSequence -> WorldBasicCameraData -> Cinemachine`。资料缺失是待解决的实施前置条件，不将字段解码、类型存在或近似画面当成完整移植。
- 完整迁入角色 Profile、球面/轨道构图、单点/多点与实体取景、手动输入、锁定、位置/旋转阻尼、状态转场、OverrideTrack、Zoom、Stretch/回弹、Shake、Shot、碰撞和中断/退出行为，以及它们实际可达的曲线、资源与规则。
- **BREAKING**：以模块化相机核心替换当前有限模式相机求值和效果协调器的不完整执行语义和固定模式 FOV；保持唯一 Presentation 入口、同帧 Body/最终动画输入、显式目标和最终 Cinemachine Adapter。原算法与 Cinemachine 各自负责的计算必须逐项确定，不叠加第二次阻尼、混合或独立更新。
- **BREAKING**：建立正式 `CharacterCameraProfile` 及强类型 Sequence、Override、Zoom、Stretch、Shake、Shot、Curve 资源；Definition 只装配 Profile，SkillProgram Root、技能局部 Graph 与 TreeClip/Timeline 只声明已提交的请求。资源通过现有 Character Build 生成同一 Presentation Projection 中的不可变相机计划，Float32/Fixed 共用；删除旧裸字符串资源引用、万能 Cue、未消费字段和旧配置路径。
- 相机作者功能进入保留的 SkillProgram Root、技能局部 Graph 与 TreeClip/Timeline 工作区：资源选择、参数单位、曲线、依赖导航、Undo/Redo、明确 Build 和只读 Live Debug 全部接通。Camera finite producer 通过 SkillProgram 与新的 SourceMap 衔接，消费已提交 PresentationCommand、ActionInstance 来源和 producer generation。当前 CameraSequence 只作为相机构图求值适配配置，不恢复已删除的动画 Sequence、Timeline Sequence 模式或 C# Locomotion 平行图，也不把它当成已确认的 ZZZ 原版作者结构。
- 将已确认的 v4 Camera 语义完整迁入最终 BTSMTL Document v5 的 Camera domain：Camera domain 继续拥有 Profile、Sequence、Effect、Curve、结构化引用语义和 typed Mutation；v5 owner 负责整包装配、manifest、schema、codec、hash、事务与反向发布。当前已安装 v4 仍是 current truth，本变更不先建立一套 v4 Camera 分片、v4 专用 codec 或第二套 Mutation。BTSMTL v5 实际接口尚未提交前，只记录正式依赖，不写占位 v5 代码。
- Preview 会话、独立命令源、fixture 执行器和 seek 重建统一归 `rebuild-btsmtl-preview-with-scene-play`。Timeline 游标只定位作者内容或观察历史；Gameplay 状态变化只能走正式运行或受控试验重建，seek 不得直接改 Simulation，也不得形成第二角色执行链。Camera 只接入统一 owner 提供的正式输入并发布只读结果。
- 完成 Corin 已解码 81 项 Shake、18 项 Zoom、18 项 Stretch、4 项 Override 及全部可达相机依赖和属于单角色动作演出的事件正式迁移；AssaultAid 与 ParryAid 作为当前链路对照，SwitchInAttack 只作为来源事件证据，换人/跨角色归属待后续设计确认。清理旧资源、旧入口、旧生成产物合同并同步现行规范。

## Capabilities

### New Capabilities

- `character-camera-source-parity`：定义 ZZZ 相机完整移植的证据、依赖闭包、逐字段/逐行为对应、正式导入和完成判定。
- `character-camera-authoring`：定义相机 Profile 与各类资源在 SkillProgram Root、技能局部 Graph、TreeClip/Timeline、曲线与诊断中的完整作者流程。

### Modified Capabilities

- `character-camera-pipeline`：将不完整有限模式执行改为按原行为求值的唯一相机核心，明确生命周期、时间、目标、效果、Cinemachine 分工与最终 basis。
- `btsmtl-compiled-simulation-program`：增加相机请求合同与独立于 Numeric Target 的相机 Projection 编译、依赖身份及原子发布要求。
- `btsmtl-timeline-editor-preview`：让相机消费 `rebuild-btsmtl-preview-with-scene-play` 统一拥有的 ScenePlay 会话、fixture 与 seek 重建，Camera 只提供正式运行模块、Projection、Rig/目标/物理绑定、Reset 和只读 Live Debug，并迁移旧 CameraSequenceClip 的曲线 Catalog 合同。
- `btsmtl-agent-authoring-document-sync`：把已确认 Camera v4 语义迁入最终 Document v5 的 Camera domain；v5 owner 统一整包装配与唯一 codec/Mutation，禁止在 v4 Camera、Inspector、Document 和导入器之间形成分裂写入。

## Impact

- Runtime：`3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Camera/`、`Pipeline/Presentation/CharacterCameraPresentationRuntime.cs`、Factory、Definition、Projection、相机输入/basis 接口，以及当前依赖 FreeLook 的显式重置和采样调用者。
- 编译与作者层：SkillProgram Root、技能局部 Graph、TreeClip/Timeline 的 Camera producer、`BTSMTL/Timeline/Scripts/Timeline.Camera.cs`、Semantic emitter/两个 Numeric Target、Projection Compiler、Character 作者工作区、Timeline Curve/统一 Preview/Live、Agent Document v5 与正式 Mutation。
- 内容与装配：Corin Camera Profile/资源、相关 SkillProgram/Graph/TreeClip/Timeline、Prefab/Scene 中显式 Rig/目标/物理场景绑定，以及精确 Character Build 的 generated products。相机仍只属于本地表现能力，不改变网络模型和角色动作事实的所有权。
- 规范冲突：现行 `character-camera-pipeline` 禁止 resolver 计算位置、旋转和轨道，并强制 Cinemachine 负责全部 orbit/damping；本变更必须以原调用链分工替换这两项要求，同时替换固定模式和旧 Cue/曲线合同。共享 Graph Shell、现有 Simulation/Presentation 分离、最终 Body pose 与显式 Build 边界继续成立。
- 并行变更：`add-discrete-stair-presentation` 修改默认 Camera 跟随的同一 requirement。本变更保留其“消费最终 visible Body，不另做台阶修正”的约束，不修改 Body 求解。当前工作区中其它已修改的 spec、代码和已删除的 `openspec/AGENTS.md` 不由本提案覆盖。
- 本变更包含运行时、编译器、作者合同和规范对账；不修改 Body、动画、Gameplay 或网络的既有正确行为，不新增测试，也不把用户手动验证写成实施任务。v5 接口未实际提交前，不把 v5 schema、codec、Mutation 或 Preview bridge 写成 current truth。来源未闭合项必须保留失败诊断，直到后续取证或设计明确。
