# character-animation-selection-runtime Specification

## Purpose

定义持续 Pose source、有限 Action playback、显式 Player、source-local 时间映射、连续性和释放之间的正式表现边界。Preview 只观察 ScenePlay 正式 Actor，不拥有另一套选择器或执行器。

## Requirements

### Requirement: 持续 Pose 与有限 Action 必须使用不同事实入口

持续 Idle、Start、Move、Stop、Turn 和 Motion Matching MUST 由 PoseStateMachine 的 state-local source 发布 `PresentationPoseSourceSample`。有限 Action Timeline MUST 由正式 Action playback 提交 `ActionAnimationPlaybackCommand`。两者 MUST 不互相伪装，持续 Pose MUST 不携带 Gameplay winner、Timeline window 或 AnimationPlaybackId。

#### Scenario: Attack playback 进入 Pose

- **WHEN** Ability 已在正式 Session 中激活有限 Attack Action
- **THEN** Action playback MUST 提交带 playback identity、generation 和 committed sample 的命令
- **AND** PoseStateMachine MUST 继续只负责持续 Pose source
- **AND** Pose UI MUST 只显示正式提交结果

### Requirement: Source readiness 必须显式表达

每个 Pose source MUST 报告 Pending、Ready、Invalid、Released 或 Faulted，并携带稳定 source identity、Player identity、generation 和 frame lineage。Pending 不得启动正式 transition；Invalid 或 Faulted MUST 阻止依赖它的 Final Publication。

#### Scenario: Source尚未就绪

- **WHEN** 当前 Action 请求的 Source 仍处于 Pending 或进入 Invalid
- **THEN** AnimationSlot MUST 保持请求等待或失败状态
- **AND** PoseStateMachine MUST 不提交依赖该 Source 的 transition 或 Final Publication

### Requirement: Source 生命周期必须只有一个物理 owner

唯一 Source Module MUST 拥有 Clip、Blend Space、Motion Matching source、Animancer/Playable source、prepared resource、capture binding、deferred release 和 release completion。PoseState、Action playback、Timeline UI、ScenePlay 和 Diagnostics MUST 不直接创建、销毁或复用物理 source。

#### Scenario: Action 淡出后释放

- **WHEN** Action 已离开 Gameplay membership 但 Slot 仍需要其 Pose
- **THEN** Source Module MUST 保留该 source 直到正式 usage 和 release 条件完成
- **AND** Timeline、Pose UI 或外层 Session MUST 不提前销毁 Playable

### Requirement: Player、Transition、Slot 和连续性各自只有一个 owner

PoseStateMachine MUST 拥有 state relevance、transition target 和 state-local continuation；Action playback/AnimationSlot MUST 拥有有限 Action 的 playback cursor、selection、retention 和 generation；Source Module 只执行采样和物理释放。任何 owner MUST 不重新仲裁其它 owner 的 winner 或时钟。

#### Scenario: Locomotion与Action同时存在

- **WHEN** Actor正在运行Locomotion且新的有限Action进入正式Playback
- **THEN** PoseStateMachine MUST 只决定 state relevance 与 transition target
- **AND** AnimationSlot MUST 只维护 Action cursor、selection 与 retention
- **AND** Source Module MUST 只负责采样和释放，不得重新选择Gameplay winner

### Requirement: Locomotion Phase 只来自正式素材和运行绑定

Locomotion Phase MUST 从原生 AnimationClip 注册曲线和正式 Profile/Source binding 读取。运行时 MUST 不读取 Editor 曲线、Foot Analysis artifact、当前窗口曲线或旧 Marker mapping 进行现场推断。

#### Scenario: 读取Locomotion Phase

- **WHEN** 正式 Actor 采样一个带已注册 Phase 曲线的 Locomotion source
- **THEN** Runtime MUST 通过当前 Profile/Source binding 读取该曲线的正式时间值
- **AND** MUST 不从 Editor artifact、窗口游标或 Marker mapping 现场推导 Phase

### Requirement: Preview 必须使用 ScenePlay 正式 Actor

Timeline、Pose Graph 和 Motion Matching 页面 MUST 不创建 `AnimationPreviewRuntime`、Fact Fixture、Query Fixture、独立 Action lifecycle、简化 Player、临时 PlayableGraph 或 Animancer direct Play。ScenePlay 启动正式 Session/Actor 后，页面只能绑定并读取该 Actor 的 Source、Action、Slot、Transition、Constraint 和 completion 事实。

#### Scenario: 页面 seek

- **WHEN** 作者移动 Timeline UI 的 authoring 游标
- **THEN** UI MAY 更新显示和待提交编辑状态
- **AND** MUST 不通过 seek 修改正式 Actor 的 playback cursor、Pose history 或 Gameplay state

### Requirement: Float32 与 Fixed 共享表现业务边界

Float32 与 Fixed MAY 使用不同数值表示和资源准备，但 MUST 共享 Action selection、Source readiness、Slot、Transition、release、Pose commit 和 ScenePlay 观察语义。任一目标的 binding、资源或能力缺失 MUST 在 Prepare 阶段明确失败，不得降级到旧 Projection 或另一目标的运行数据。

#### Scenario: 两个数值目标选择同一Action

- **WHEN** Float32 与 Fixed 使用相同输入和同一 Action binding
- **THEN** 两个目标 MUST 采用相同的 selection、readiness、Slot 和 completion 语义
- **AND** 任一目标缺少正式 binding 或资源时 MUST 在 Prepare 阶段明确失败

### Requirement: Animancer必须只负责source采样

唯一 Source Module MUST 只消费完整 Action playback 或 Presentation Pose source identity，以及编译后的资源、effective sample、loop、play rate 和 source-local clip weight。每个动画资源 MUST 在 Build 时唯一选择 NativeClip 或 ACL backend，Action、Direct Clip、Blend Space、Motion Matching 和 Preview MUST 解析同一份 dense 资源合同；同一资源的不同使用点 MUST 不绕回另一个隐式 backend。NativeClip 仍由 Animancer Clip/ManualMixer 采样；ACL MUST 使用声明的资源数据，MUST 不依赖同素材 AnimationClip 的运行时强引用或备用播放器。

原生 Pose 图运行时 MUST 继续拥有 PoseState、Player endpoint、ActionPlaybackInput lifecycle、Transition、Slot、Blend Stack 和 Inertialization 逻辑。Source Module MUST 统一管理物理 source、capture、usage 对接和 release completion，MUST 不仲裁 State 或 Action winner、不推进 Action lifecycle、不读取运行时 AnimationClip 作者曲线、不选择 Phase leader、不计算跨 source weight、不执行 Foot Placement、Goal Assembly、FBBIK 或 Final Publication。有效时间 MUST 由原生图运行时一次确定，backend MUST 不重复累计时钟或再次应用 play rate。

资源准备 MUST 在表现帧外取得进展；预期 Pending MUST 通过正式 outcome 返回。已有合法 source 时，候选 target Pending MUST 由原生图运行时按既有语义保持当前 source 并采样本帧；Entry Pending MUST 不发布 Final Pose。Source MUST 不以历史 sample、bind pose、默认 Idle 或其它 backend 伪造 Ready。

#### Scenario: PoseState transition共同采样两个source

- **WHEN** 原生图中的 Standard Blend 要求 source 与 target 同时可见，且各自需要的资源已经 Ready
- **THEN** 原生 Pose 图运行时 MUST 发布两份 typed Demand 与各自 effective sample，Source Module MUST 在同一表现 Barrier 提供两份 capture
- **AND** source 间 weight、Transition clock 和 release permission MUST 仍由原生图运行时计算

#### Scenario: Source backend尝试选择State

- **WHEN** Source readiness 或 Playable 状态发生变化
- **THEN** Source Module MUST 只发布 Pending、Ready、Invalid 或 release completion 结果
- **AND** MUST 不直接修改 PoseState、Player generation、Transition 或 OutputPose

#### Scenario: 同一ACL资源被多个使用点引用

- **WHEN** 同一 ACL 动画同时出现在 Direct Clip、Action 或 Blend Space 的编译闭包中
- **THEN** 所有使用点 MUST 解析同一资源版本，各个活跃 source 保持自己的时间与 generation
- **AND** 运行包 MUST 不因某个使用点仍引用旧 Clip binding 而携带该素材的展开播放替代品

#### Scenario: 候选ACL资源仍在加载

- **WHEN** target 的所需 payload 或质量数据尚未完成准备
- **THEN** backend MUST 返回 Pending，加载任务 MUST 能独立于该表现帧的 Seal 继续完成
- **AND** 当前合法 source 的继续采样 MUST 保持既有节点语义，不能被误判为 target 已经 Ready
