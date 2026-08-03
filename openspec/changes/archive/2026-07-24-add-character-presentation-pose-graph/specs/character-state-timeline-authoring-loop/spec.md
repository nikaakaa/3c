## MODIFIED Requirements

### Requirement: Corin 必须由逻辑层按Animation Channel提交唯一playback selection

Corin MUST正式声明`BaseLocomotion`与`FullBodyAction`两个AnimationChannelId，并通过Pose Graph分别绑定`AnimationSelectionInput(BaseLocomotion, RequireSelection)`与`AnimationSelectionInput(FullBodyAction, AllowEmpty)`。Locomotion producer MUST只在BaseLocomotion内完成唯一选择；Attack、Dodge与其它明确全身Action producer MUST只在FullBodyAction内完成唯一选择。AnimationTrack Priority、Presentation Driver、Pose Graph节点、Bone Mask与Runtime arbitration MUST不参与同channel winner选择。

#### Scenario: Locomotion正常运行

- **WHEN** 当前没有FullBodyAction
- **THEN** BaseLocomotion MUST选择当前Locomotion State的正式Timeline playback
- **AND** FullBodyAction MUST提交None或保持合法Empty生命周期
- **AND** OutputPose MUST来自BaseLocomotion Player分支

#### Scenario: Run中进入Dodge

- **WHEN** Dodge获得Action ownership
- **THEN** FullBodyAction MUST选择Dodge playback
- **AND** BaseLocomotion MUST继续保持当前合法selection
- **AND** Pose Graph MUST通过全身Mask显示Dodge

#### Scenario: Dodge返回Locomotion

- **WHEN** Dodge完成
- **THEN** FullBodyAction MUST提交None并由显式Action BlendStack淡出到NoPose
- **AND** Animation module MUST不根据历史sample猜测Base目标
- **AND** BaseLocomotion MUST按自身状态逻辑继续输出Run或Idle

#### Scenario: Attack1进入Attack2

- **WHEN** nested Attack StateMachine切换到Attack2
- **THEN** FullBodyAction channel MUST更新为Attack2 playback
- **AND** BaseLocomotion selection MUST不被该transition覆盖

#### Scenario: 同tick多个channel变化

- **WHEN** 同一logic tick内MovingTurn与Attack ownership均变化
- **THEN** Program MUST分别提交BaseLocomotion和FullBodyAction的最终selection
- **AND** 每个channel内部Complete/Release MUST继续保序

### Requirement: Corin 全部 AnimationTrack 必须显式选择 Marker Sync 策略

Corin每个可达AnimationTrack MUST显式配置None或MarkerGroup，不得保留Unspecified。选择 MUST根据producer真实动画语义、Timeline Once/Loop call site、AnimationChannelId、Selection Input和完整marker coverage作出，不得按状态名称硬编码。没有AnimationTrack的状态 MUST不创建伪Timeline、伪clip或伪marker。

#### Scenario: 打开Corin完整作者清单

- **WHEN** Compiler或Agent Validator遍历Corin全部Graph、StateMachine与Timeline
- **THEN** 每个可达AnimationTrack MUST拥有明确sync mode与AnimationChannelId
- **AND** 任一Unspecified track MUST阻止发布

#### Scenario: WalkEnd没有动画资源

- **WHEN** WalkEnd没有AnimationTrack并依赖BaseLocomotion channel保持或切换正式producer
- **THEN** 迁移 MUST不创建一次性Timeline或fallback clip
- **AND** Marker Sync inventory MUST不制造不存在的producer

### Requirement: Corin WalkLoop 与 RunLoop 必须共享 Locomotion.Gait

Corin WalkLoop与RunLoop AnimationTrack MUST属于同一`BaseLocomotion` Animation Channel并进入同一BaseLocomotion Selection Input，配置为`MarkerGroup/Cyclic`并共享`Locomotion.Gait` SyncGroupId。两者 MUST按各自真实动画frame配置完整有向marker segment，不得假设normalized time或动画长度相同。Pose Graph、FullBodyAction共同可见期、Locomotion状态transition时刻、motion request与WorldSolver结果 MUST不改变该同步合同。

#### Scenario: WalkLoop切换RunLoop

- **WHEN** BaseLocomotion从WalkLoop进入RunLoop
- **THEN** RunLoop MUST在同channel handoff期间跟随WalkLoop marker segment
- **AND** Gameplay状态与运动 MUST在原logic tick立即切换

#### Scenario: Action覆盖期间切换步态

- **WHEN** FullBodyAction全身覆盖时BaseLocomotion从Walk切换到Run
- **THEN** BaseLocomotion Selection MUST继续完成Marker Sync，并由其后显式Player节点处理discontinuity
- **AND** Pose Graph隐藏其骨骼贡献 MUST不停止或改写该channel时间

### Requirement: Corin 有限动作只能在资源满足时加入 Marker Group

RunStart、RunEnd、MovingTurn及其它BaseLocomotion one-shot MAY在资源满足时加入BaseLocomotion Marker Group。Attack1至Attack5、Dodge及其它FullBodyAction producer只有在同一FullBodyAction channel和Selection Input内真实共享完整Marker契约时 MAY建立独立Action Marker Group；不得跨BaseLocomotion与FullBodyAction同步。资源不满足时 MUST显式None并保留普通Timeline sample与图中显式Player连续性。

#### Scenario: Action退出到Locomotion

- **WHEN** Action producer为None同步模式并结束
- **THEN** FullBodyAction显式BlendStack MUST使用正式CrossFade transition淡出到NoPose
- **AND** BaseLocomotion MUST继续使用自身effective time
- **AND** MUST不从Action名称或Pose Graph mask伪造Locomotion phase
