## MODIFIED Requirements

### Requirement: Blend Policy必须属于明确transition owner

Blend Policy MUST属于明确的transition owner：PoseState Transition edge、AnimationSlot、直接Player下游Inertialization或保留的显式BlendStack。PoseState edge MUST内联保存该edge的`StandardBlend | Inertialization`、duration、`Linear | EaseIn | EaseOut | EaseInOut | Custom` Blend Mode、条件式强类型Custom Curve Asset引用与强类型Blend Profile引用；它 MUST不保存可自由输入的curve/profile字符串、target reset policy或SourceSyncMode。AnimationSlot MUST按全部可达Action endpoint物化完整exact rule table；直接Player Inertialization MUST只在没有其它上游transition owner时物化完整exact rule；普通BlendStack MUST继续只管理连接到自身的多source历史。Timeline、Gameplay State edge、SequencePlayer、ActionProfile与Prefab MUST不保存第二份transition表，Animancer backend MUST不决定fade。

#### Scenario: Locomotion State transition

- **WHEN** 作者配置Start到Locomotion的Blend Logic、Duration、Blend Mode和Blend Profile
- **THEN** 数学配置 MUST由该PoseState Transition edge唯一拥有
- **AND** BTSMTL Locomotion edge与下游Inertialization response policy MUST不保存第二份时间数学

#### Scenario: FullBodyAction Slot

- **WHEN** 作者配置Attack到Dodge exact rule
- **THEN** Policy MUST由FullBodyAction Slot引用
- **AND** Action Timeline MUST不保存该Pose transition

#### Scenario: StateMachine edge选择BlendStack

- **WHEN** 作者尝试把Pose State Transition的Blend Logic设为BlendStack
- **THEN** Capability与Mutation MUST拒绝该值
- **AND** 作者 MUST通过显式Pose Graph BlendStack节点表达多entry history

### Requirement: Pose State必须唯一拥有Player进入生命周期

每个Pose State MUST显式保存`AlwaysResetOnEntry`。开启时，StateMachine MUST在每次进入该State前重新初始化其全部可达Player；关闭时，StateMachine MUST保留这些Player的既有clock与properties。Sequence Player、Blend Space Player与Transition MUST不保存第二份Reset On Entry作者字段。初始Player clock属于Player初始化，后续State进入重置只由StateMachine执行。

#### Scenario: One-shot State重新进入

- **WHEN** 作者为Start State启用`AlwaysResetOnEntry`
- **THEN** 每次进入Start MUST让其全部Player回到正式initial time
- **AND** 作者 MUST不逐个Player配置相同Reset

#### Scenario: 循环State保留播放状态

- **WHEN** 作者为循环State关闭`AlwaysResetOnEntry`
- **THEN** 离开并返回时 MUST继续该State既有Player状态
- **AND** MUST不由任一入边改写该决定

## ADDED Requirements

### Requirement: Custom Blend Curve必须是统一强类型作者资产

系统 MUST提供`CharacterAnimationBlendCurveAsset`作为Custom transition curve的唯一作者资产。资产 MUST拥有稳定CurveId、revision和可视化CurveField正文，并固定time/value域为`[0,1]`。首尾key MUST为`(0,0)`与`(1,1)`，key time MUST严格递增，value MUST单调不降，全部值与切线 MUST有限，曲线 MUST能无歧义降低为项目canonical Hermite segments。Pose Transition、AnimationSlot、BlendStack与直接Player Inertialization Policy MUST复用同一Blend Mode与Curve Asset合同，不得并存第二种inline key作者格式。

#### Scenario: 作者编辑Custom Curve

- **WHEN** 作者打开一个Custom Blend Curve资产并拖动key或切线
- **THEN** Inspector MUST显示真实CurveField并通过正式资产Mutation提交合法曲线
- **AND** 该编辑 MUST不自动Compile、Build或发布Projection

#### Scenario: 非Custom模式携带Curve资产

- **WHEN** Transition使用EaseOut但仍提交Custom Curve引用
- **THEN** Mutation或Validator MUST拒绝不适用字段
- **AND** MUST不静默保留一份未生效曲线

#### Scenario: Custom Curve发生过冲

- **WHEN** Curve Asset在任一segment越过`[0,1]`或破坏单调性
- **THEN** Validator MUST定位资产与非法key/segment
- **AND** Projection Build MUST不生成近似、clamp或默认曲线
