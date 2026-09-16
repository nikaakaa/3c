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

### Requirement: Source 生命周期必须只有一个物理 owner

唯一 Source Module MUST 拥有 Clip、Blend Space、Motion Matching source、Animancer/Playable source、prepared resource、capture binding、deferred release 和 release completion。PoseState、Action playback、Timeline UI、ScenePlay 和 Diagnostics MUST 不直接创建、销毁或复用物理 source。

#### Scenario: Action 淡出后释放

- **WHEN** Action 已离开 Gameplay membership 但 Slot 仍需要其 Pose
- **THEN** Source Module MUST 保留该 source 直到正式 usage 和 release 条件完成
- **AND** Timeline、Pose UI 或外层 Session MUST 不提前销毁 Playable

### Requirement: Player、Transition、Slot 和连续性各自只有一个 owner

PoseStateMachine MUST 拥有 state relevance、transition target 和 state-local continuation；Action playback/AnimationSlot MUST 拥有有限 Action 的 playback cursor、selection、retention 和 generation；Source Module 只执行采样和物理释放。任何 owner MUST 不重新仲裁其它 owner 的 winner 或时钟。

### Requirement: Locomotion Phase 只来自正式素材和运行绑定

Locomotion Phase MUST 从原生 AnimationClip 注册曲线和正式 Profile/Source binding 读取。运行时 MUST 不读取 Editor 曲线、Foot Analysis artifact、当前窗口曲线或旧 Marker mapping 进行现场推断。

### Requirement: Preview 必须使用 ScenePlay 正式 Actor

Timeline、Pose Graph 和 Motion Matching 页面 MUST 不创建 `AnimationPreviewRuntime`、Fact Fixture、Query Fixture、独立 Action lifecycle、简化 Player、临时 PlayableGraph 或 Animancer direct Play。ScenePlay 启动正式 Session/Actor 后，页面只能绑定并读取该 Actor 的 Source、Action、Slot、Transition、Constraint 和 completion 事实。

#### Scenario: 页面 seek

- **WHEN** 作者移动 Timeline UI 的 authoring 游标
- **THEN** UI MAY 更新显示和待提交编辑状态
- **AND** MUST 不通过 seek 修改正式 Actor 的 playback cursor、Pose history 或 Gameplay state

### Requirement: Float32 与 Fixed 共享表现业务边界

Float32 与 Fixed MAY 使用不同数值表示和资源准备，但 MUST 共享 Action selection、Source readiness、Slot、Transition、release、Pose commit 和 ScenePlay 观察语义。任一目标的 binding、资源或能力缺失 MUST 在 Prepare 阶段明确失败，不得降级到旧 Projection 或另一目标的运行数据。
