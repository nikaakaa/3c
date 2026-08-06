## ADDED Requirements

### Requirement: Linked Pose 不得恢复旧 Layer 运行时

受限 Linked Pose MUST 使用 Interface、Entry、Group、Implementation 与 typed Call identity，并作为编译 Pose DAG 的动态 dispatch fragment 运行。系统 MUST 不恢复旧 Layer catalog、LayerId、Profile layer order、Animancer layer index、Layer weight 仲裁或按顺序叠加未知 Graph。多个 Entry 共享 Group 的语义 MUST 只控制 Implementation 选择、generation 与事务，不表示额外渲染层或混合顺序。

#### Scenario: 同一 Interface 具有 Pose 与 Goals Entry

- **WHEN** 两个 Call 属于同一 Linked Group
- **THEN** 它们 MUST 共享 Implementation 选择与 generation，但各自保持 typed DAG 位置和独立节点状态
- **AND** Runtime MUST 不按 Entry 顺序把二者当作 Animancer layers 混合

#### Scenario: 作者需要多层 Pose 组合

- **WHEN** Implementation 内部需要上半身 Mask、Additive 或局部组合
- **THEN** 作者 MUST 使用正式 Blend、Layered Bone Blend 或 Additive Pose 节点表达
- **AND** MUST 不创建动态 Layer order 配置

### Requirement: Linked Pose 与有限 Action 必须继续保持职责分离

Linked Implementation MUST 只提供持续 Pose 逻辑或 Interface 声明的 typed Goals；有限 Action MUST 继续由 Timeline producer、Playback Lifecycle、ActionPlaybackInput 与 root AnimationSlot 拥有。Linked selector MAY 读取自身负责的 committed 业务状态，但 Linked core 与 Implementation MUST 不读取 Timeline operation 或接管 `AnimationPlaybackId` 生命周期。

#### Scenario: 切换武器时攻击仍在播放

- **WHEN** Equipment selector 变化且 root Slot 仍有活动有限 Action
- **THEN** Linked Group MUST 按新 selection generation 切换持续实现
- **AND** Action 如何继续、取消或交接 MUST 只由 Gameplay 与 Slot Routing 合同决定

### Requirement: Linked Implementation 必须表示粗粒度持续实现族

Linked Group SHOULD 用于装备、载具、受伤形态等会整体替换一组持续动画逻辑的业务状态。Idle、Run、Start、Stop 等高频细状态 MUST 继续由已选 Implementation 内的 `PoseStateMachine`、Motion Matching 或其它正式 Locomotion 节点管理，除非另一个明确 Interface 将它们定义为粗粒度实现边界。

#### Scenario: 角色在同一武器下从 Idle 进入 Run

- **WHEN** Equipment selector 的 ImplementationId 未变化而 Locomotion Fact 从 Idle 条件变为 Run 条件
- **THEN** 当前 Implementation 内部状态节点 MUST 处理该转换
- **AND** Linked Group MUST 不创建新 generation
