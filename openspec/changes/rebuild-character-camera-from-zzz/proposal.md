## Why

当前 3C 相机已经进入唯一 Character Presentation 调用链，但实际仍以固定 FOV、有限模式和单个 FreeLook 为主，无法承载 ZZZ 的基础构图、状态序列、真实转场、技能相机效果和完整作者配置。用户要求按照 ZZZ 完整移植本轮角色相机核心，并在现有 Graph、Timeline 和 Character 编辑器内完成配置、预览与排查；本变更以原行为和依赖资源全部有对应实现为完成条件。

## What Changes

- 建立 ZZZ 角色相机的版本、原资源身份、字段、函数、调用顺序、时间语义与输出实例对应表，追通 `PipelineCamera / CameraSequence -> WorldBasicCameraData -> Cinemachine`。资料缺失是待解决的实施前置条件，不将字段解码、类型存在或近似画面当成完整移植。
- 完整迁入角色 Profile、球面/轨道构图、单点/多点与实体取景、手动输入、锁定、位置/旋转阻尼、状态转场、OverrideTrack、Zoom、Stretch/回弹、Shake、Shot、碰撞和中断/退出行为，以及它们实际可达的曲线、资源与规则。
- **BREAKING**：以模块化相机核心替换当前 `CameraStateResolver`、`CameraModifierResolver` 的不完整执行语义和固定模式 FOV；保持唯一 Presentation 入口、同帧 Body/最终动画输入、显式目标和最终 Cinemachine Adapter。原算法与 Cinemachine 各自负责的计算必须逐项确定，不叠加第二次阻尼、混合或独立更新。
- **BREAKING**：建立正式 `CharacterCameraProfile` 及强类型 Sequence、Override、Zoom、Stretch、Shake、Shot、Curve 资源；Definition 只装配 Profile，Graph/Timeline 只声明已提交的请求。资源通过现有 Character Build 生成同一 Presentation Projection 中的不可变相机计划，Float32/Fixed 共用；删除旧裸字符串资源引用、万能 Cue、未消费字段和旧配置路径。
- 相机作者功能进入现有 Character/Graph 工作区和 Timeline：资源选择、参数单位、曲线、依赖导航、Undo/Redo、明确 Build、同运行实现的 Preview、只读 Live Debug 全部接通。CameraSequence 是相机构图求值配置，不恢复已删除的动画 Sequence 或 Timeline Sequence 模式。
- 扩展统一 Agent Document v4 的 Character Camera 分片、Catalog、Exporter、严格 Codec、Reconciler、Mutation、Validator 与反向导出，沿既有五个生命周期工具完成同一资产事务，不增加相机专用编辑通道。
- 完成 Corin 已解码 81 项 Shake、18 项 Zoom、18 项 Stretch、4 项 Override 及全部可达相机依赖和事件的正式迁移；AssaultAid、ParryAid、SwitchInAttack 作为明确的链路对照，不将它们作为缩减其它已纳入行为的理由。清理旧资源、旧入口、旧生成产物合同并同步现行规范。

## Capabilities

### New Capabilities

- `character-camera-source-parity`：定义 ZZZ 相机完整移植的证据、依赖闭包、逐字段/逐行为对应、正式导入和完成判定。
- `character-camera-authoring`：定义相机 Profile 与各类资源在现有工作区、Graph、Timeline、曲线与诊断中的完整作者流程。

### Modified Capabilities

- `character-camera-pipeline`：将不完整有限模式执行改为按原行为求值的唯一相机核心，明确生命周期、时间、目标、效果、Cinemachine 分工与最终 basis。
- `btsmtl-compiled-simulation-program`：增加相机请求合同与独立于 Numeric Target 的相机 Projection 编译、依赖身份及原子发布要求。
- `btsmtl-timeline-editor-preview`：让相机与动画在同一 Preview 生命周期下运行，支持有状态镜头的 seek、显式场景绑定和独立只读 Live Debug，并迁移旧 CameraStateClip 的曲线 Catalog 合同。
- `btsmtl-agent-authoring-document-sync`：将 Camera 作者资源和引用纳入现有 v4 整包语义与唯一事务，禁止在 Inspector、Document 和导入器之间形成分裂写入。

## Impact

- Runtime：`3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Camera/`、`Pipeline/Presentation/CharacterCameraPresentationRuntime.cs`、Factory、Definition、Projection、相机输入/basis 接口，以及当前依赖 FreeLook 的显式重置和采样调用者。
- 编译与作者层：Camera Graph nodes、`BTSMTL/Timeline/Scripts/Timeline.Camera.cs`、Semantic emitter/两个 Numeric Target、Projection Compiler、Character 作者工作区、Timeline Curve/Preview/Live、Agent Document 与正式 Mutation。
- 内容与装配：Corin Camera Profile/资源、相关 Graph/Timeline、Prefab/Scene 中显式 Rig/目标/物理场景绑定，以及精确 Character Build 的 generated products。相机仍只属于本地表现能力，不改变网络模型和角色动作事实的所有权。
- 规范冲突：现行 `character-camera-pipeline` 禁止 resolver 计算位置、旋转和轨道，并强制 Cinemachine 负责全部 orbit/damping；本变更必须以原调用链分工替换这两项要求，同时替换固定模式和旧 Cue/曲线合同。共享 Graph Shell、现有 Simulation/Presentation 分离、最终 Body pose 与显式 Build 边界继续成立。
- 并行变更：`add-discrete-stair-presentation` 修改默认 Camera 跟随的同一 requirement。本变更保留其“消费最终 visible Body，不另做台阶修正”的约束，不修改 Body 求解。当前工作区中其它已修改的 spec、代码和已删除的 `openspec/AGENTS.md` 不由本提案覆盖。
- 本次只生成规划文档；不修改代码、Unity 资产、现行主 spec 或 generated products，不新增测试，也不把用户手动验证写成实施任务。
