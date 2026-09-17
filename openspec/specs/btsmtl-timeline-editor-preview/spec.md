# btsmtl-timeline-editor-preview Specification

## Purpose

定义 ScenePlay 中 Timeline UI 的作者、选择和观察边界。Timeline UI 是正式运行链的窗口入口，不是第二个 Runtime、播放器、求值器或独立时钟。正式 Timeline Runtime、Ability、Character、Pose、Camera 和 World 继续由各自领域 owner 负责。

## Requirements

### Requirement: ScenePlay 必须拥有预览生命周期

ScenePlay 协调器 MUST 统一拥有 Start、Pause、Resume、Reset、Stop、Build、Ability 请求、Live Debug、Capture、History、Restore 和 Replay。TimelineEditorWindow MUST 只提交作者选择、输入和请求，并消费正式 binding、状态和只读事实。

#### Scenario: 从编辑器开始一次 Ability 预览

- **WHEN** 作者在 ScenePlay 中选择 Character、Ability 和 Timeline 观察入口并点击 Start
- **THEN** ScenePlay MUST 加载声明的正式场景并启动唯一 Session/Actor
- **AND** Ability MUST 通过正式输入和请求入口启动
- **AND** Timeline UI MUST 绑定实际 playback identity 后开始观察

#### Scenario: 暂停与停止

- **WHEN** 作者点击 Pause、Resume、Reset 或 Stop
- **THEN** ScenePlay MUST 调用正式 Session/Presentation/Timeline 生命周期
- **AND** Timeline UI MUST 不自行推进时间、不清理角色状态、不创建新播放实例

### Requirement: Timeline UI 不得拥有运行时执行状态

TimelineEditorWindow MUST 不拥有 evaluator、独立时钟、playback command source、TimelinePreviewSession、AnimationPreviewRuntime、Preview Player、隐藏 Action runtime 或独立 PlayableGraph。窗口本地只保存 authoring selection、view state、观察绑定和显示过滤。

#### Scenario: 打开同一 Timeline

- **WHEN** 作者在不同页面打开同一 TimelineData
- **THEN** 每个页面 MAY 保存自己的 selection 和观察绑定
- **AND** TimelineData MUST 不保存窗口时间、目标、generation、播放状态或 GUI 游标
- **AND** 页面关闭只撤销 interest，不得结束正式 Session，除非作者明确调用 ScenePlay Stop

### Requirement: Timeline 预览必须消费正式 Runtime 事实

Timeline UI MUST 只读取正式 Timeline Runtime、Ability lifecycle、Action playback、Pose 结果和 ScenePlay diagnostics 发布的 binding、playback identity、generation、content revision、active Clip、窗口、TreeClip 阶段、Motion/Cue 结果和 completion trace。UI MUST 不从 Animancer weight、当前 authoring 游标或场景对象推断运行事实。

#### Scenario: 当前 Ability 没有执行该 Timeline

- **WHEN** 正式 Session 的 playback summary 不包含当前 Timeline identity
- **THEN** UI MUST 显示未执行或未绑定
- **AND** MUST 不调用预览求值器、不重采样 TimelineData、不猜测其它 Actor

#### Scenario: 同一 Timeline 有多个播放实例

- **WHEN** 正式 Runtime 同时存在多个 playback identity
- **THEN** UI MUST 要求作者显式 Pin 或 Follow 一个实例
- **AND** 不得自动选择第一个实例或按名称匹配

### Requirement: Timeline 内容直接由正式 TimelineData 驱动

正式 Timeline Runtime MUST 直接准备和调度 TimelineData、Track、Clip、Section、Window、Motion、MotionWarp、Decision、Cue 和内容资源引用。轨道和 Clip 不得编译为 Character Program operation、Timeline IR 或窗口专用执行语言。Skill/Ability 调用只提交内容 identity、调用 identity、参数和生命周期请求。

#### Scenario: 跨 Clip 和循环边界

- **WHEN** 一次正式 Step 跨越多个 Clip 或循环边界
- **THEN** Timeline Runtime MUST 按稳定顺序处理尾段、整循环、头段、Enter、采样和 Exit
- **AND** MUST 将候选结果交给同一调用方的 Commit/Discard 边界

#### Scenario: Timeline 依赖缺失

- **WHEN** 内容、资源、TreeClip 图或外部领域服务缺失或 revision 不匹配
- **THEN** Prepare 或 playback 创建 MUST 精确失败
- **AND** MUST 不创建空 Ability、假 Actor、默认资源或 fallback 播放器

### Requirement: Timeline 私有状态必须由 Timeline Runtime 拥有

Timeline Runtime MUST 保存自己的 committed cursor、循环/Section 位置、活动 Clip、窗口阶段、TreeClip 调用关联、停止原因、generation、内容 revision 和恢复所需的正式私有状态。Character 核心负责整体快照校验、Step 接受/丢弃和最终安装；Timeline UI、Ability 和 Pose 不得复制可写 Timeline 状态。

#### Scenario: 候选被丢弃

- **WHEN** 当前 Step 或正式事务失败、取消或停止
- **THEN** Timeline MUST 丢弃本次 Pending 候选并保留上一份 committed 状态
- **AND** 不得先清空已提交游标、窗口或调用关联

#### Scenario: 恢复播放

- **WHEN** 正式 Session 恢复 Timeline 状态
- **THEN** 恢复 MUST 校验内容 revision、schema、generation、NumericTarget 和服务关联
- **AND** 不兼容时明确失败，不读取当前作者资产猜测旧状态

### Requirement: Timeline UI 只编辑正式作者数据

Timeline Editor MUST 通过 Timeline owner 的正式 Mutation、Validator 和 Undo 入口编辑 Action Track、AnimationClip Segment、Slot、Section、Window、Motion、MotionWarp、Decision、Cue 与 Timeline-local Curve。素材骨骼和注册表现曲线继续由 Unity Animation Window 及其正式 owner 编辑。

#### Scenario: 修改 Timeline 曲线

- **WHEN** 作者拖动 key 或修改曲线 Inspector 值
- **THEN** UI MUST 只生成一次正式 Mutation/Undo
- **AND** 修改后的内容 MUST 通过正式 revision/Build/Prepare 进入后续 ScenePlay
- **AND** 当前活动播放不得被窗口静默替换

### Requirement: 旧窗口预览路径必须删除

当前主线 MUST 不恢复 `TimelinePreviewSession`、`AnimationPreviewRuntime`、`TimelinePlayer`、Fact/Action/Query Fixture、独立 Motion evaluator、窗口时钟、独立 Scene Preview runtime 或独立 PlayableGraph。历史文档 MAY 记录这些路径作为迁移证据，但新代码和新规范不得引用它们作为入口。

#### Scenario: 搜索旧预览入口

- **WHEN** 检查 Timeline Editor、Pose Editor 和 ScenePlay 代码
- **THEN** 播放、暂停、重置和停止入口 MUST 指向 ScenePlay/正式 Runtime 合同
- **AND** MUST 不存在窗口级第二套执行路径或兼容别名

### Requirement: Timeline预览必须复用原生Pose实现

有限动作 Timeline Preview MUST继续使用自己的 session-local 动作状态与正式 Action adapter，绑定同一原生 Pose Factory、资源、Slot、Source 和最终输出规则。它 MUST不要求旧角色 Program／Pose Image，不执行 Gameplay 或树逻辑，也不读取活动角色私有状态；持续 Locomotion 预览继续归 Pose 入口。

#### Scenario: 在同一动画配置上预览有限动作
- **WHEN** 作者预览一个合法有限动作 Timeline
- **THEN** 预览 MUST通过动作请求进入原生 Pose 实例，保留动作时间与 Slot 混合语义
- **AND** MUST不创建另一套动画执行器或生成临时角色 Program

### Requirement: 预览接入必须以领域实际准备和采用事实为准

预览 MUST消费角色领域工厂提供的技能、Pose、Camera、Motion准备与采用事实，分别显示请求来源／版本、Pending／Ready／Missing／Invalid／Failed及精确原因，并显示当前actor真正采用的版本和实例。预览 MUST不重建Character Build／ProgramEpoch，不计算假全局版本，不实现Camera或Motion准备，不因作者保存或准备Ready就显示已采用。原独立作者预览的会话实现迁移由预览任务唯一负责；本任务只提供正式实例／输入／结果合同，不扩建窗口私有执行路径。

#### Scenario: Camera尚未采用新绑定
- **WHEN** Camera资源准备已Ready但当前actor仍使用旧BindingId
- **THEN** 预览 MUST明确显示当前实际BindingId和待采用状态，不显示整个角色已更新

### Requirement: 相机预览必须消费现有预览 owner 的正式输入

相机 MUST 向现有 Timeline Preview/ScenePlay owner 提供正式 Camera 资源、只读运行绑定、显式 Rig、目标/物理输入、Reset/替换和实际采用身份。预览消费的 Timeline 相机表达只有 TreeClip 相机 Node 与唯一效果轨道两类；MUST NOT 期待或兼容 CameraState/Response/Cue 触发型轨道及按效果类型拆分的四条效果轨道。会话、输入来源和历史重建继续由对应 owner 管理；相机 MUST 不创建独立 Preview 会话、第二角色运行链或自己的 seek 执行器。预览 MUST 不实现相机求值、不要求恢复角色全量 Build 或整包 Projection，也不得隐式搜索补齐输入。

#### Scenario: 预览需要碰撞环境

- **WHEN** 已启用碰撞的相机计划进入预览
- **THEN** owner MUST 提供明确物理输入和输出绑定
- **AND** 缺失时 MUST 报告不可用，不得静默按无碰撞运行

### Requirement: 相机历史重建必须走统一 owner

有状态镜头需要重新定位时，系统 MUST 通过现有 owner 的正式 Reset/历史重建合同重新生成状态；Timeline 游标移动不能直接改 Simulation 或插入第二更新循环。缺少重建能力时必须明确不可用，不得将相机预览临时接到旧 Controller。

#### Scenario: 重定位到有持续效果的时刻

- **WHEN** 预览 owner 支持该次正式重建
- **THEN** 相机 MUST 使用同一资源、请求和求值模块恢复对应状态
- **AND** MUST 不残留上一次时间位置的平滑或效果历史

### Requirement: Timeline 相机诊断必须读取实际运行快照

Timeline Live Debug MUST 读取正式相机快照中的来源身份、请求状态、响应、混合、效果、目标与输出，并导航到真实 TreeClip Node、唯一效果轨道 Clip 或资源。它 MUST 不根据游标位置伪造活动请求或重算另一份镜头结果。

实际采用的相机资源/内容版本、绑定实例/代际及失败原因 MUST 由 Camera 领域返回。预览 MUST 不把资源处理完成、技能编译完成、旧 Projection 或旧 Runtime DLL 的调用结果冒充新相机绑定已采用。

#### Scenario: 动作镜头被取消

- **WHEN** 相应请求已经进入正式退出
- **THEN** Live Debug MUST 显示真实退出状态与原因
- **AND** MUST 不因游标仍位于 Clip 范围而显示镜头继续生效
