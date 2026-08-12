## MODIFIED Requirements

### Requirement: Pose State transition必须显式编译Routing并从source binding推导同步

每条Transition MUST显式配置source、target、priority、Rule、`Standard Blend | Inertialization`、duration、`Linear | EaseIn | EaseOut | EaseInOut | Custom` Blend Mode、条件式强类型Custom Curve Asset与强类型Blend Profile，MUST不保存target reset、SourceSyncMode、Sync Time Mapping或Foot Phase pair引用。Custom MUST引用合法Curve Asset，非Custom MUST不保存Custom引用，Standard Blend的零duration MUST表示Hard Cut，Inertialization MUST使用正duration。每个State MUST显式配置`Always Reset on Entry`，并由StateMachine在该State provider获得entry relevance之前统一执行或跳过重置；Sequence Player MUST不拥有第二份重进配置。

Projection Compiler MUST把Blend Mode降低为canonical curve index、把Blend Profile降低为匹配同一Rig的dense profile index，并为transition生成固定Routing Plan、workspace、generation、capture/release layout。Compiler MUST检查两侧State唯一的Sequence或BlendSpace provider；只有两侧source binding共享同一canonical MarkerGroup和同一Time Mapping时才生成Source Sync Plan，无共同组时生成None，多于一个同步候选、角色冲突、策略冲突或同组topology不兼容 MUST失败。`GeneratedFootPhase`计划 MUST引用精确relation-local warp plan。Marker topology和effective sample映射 MUST属于该Transition的source-local plan，Pose Graph MUST不创建MarkerSync或FootPhase节点。Runtime与Preview MUST只执行匹配Projection revision的计划，不得现场重新编译。

需要持续Movement raw time的source MUST显式编译为`CommittedMovement` clock binding。该binding只允许消费Presentation Fact中的committed Movement clock，并在Player entry锁定完整owner与generation；不得读取Gameplay Timeline、Locomotion operation或直接使用authority tick。Action source MUST编译为独立Action clock binding。

#### Scenario: Walk到Run启用GeneratedFootPhase

- **WHEN** Transition两侧唯一source binding共享canonical SyncGroup与GeneratedFootPhase
- **THEN** Source Sync Plan MUST在共同可见期间持续映射marker occurrence、leader fraction与warped follower fraction
- **AND** MUST不创建BaseLocomotion Animation Selection或脚步Transition条件

#### Scenario: State选择重进归零

- **WHEN** `Always Reset on Entry`为true的State再次获得entry relevance
- **THEN** StateMachine MUST在采样前重置该State的全部provider与relation cursor
- **AND** Transition与Sequence Player MUST不参与决定是否重置

#### Scenario: Target选择Inertialization

- **WHEN** target Ready且compiled route为Inertialization
- **THEN** transition owner MUST提交typed capture/release request
- **AND** source time同步与branch-local residual MUST由各自计划和节点分开完成

#### Scenario: Standard Blend使用每骨骼Profile

- **WHEN** Transition的Blend Profile为不同Pose Bone配置不同duration multiplier
- **THEN** Native Pose evaluator MUST对每根Physical与Virtual Bone使用同一canonical curve和各自duration求值
- **AND** Source Sync Time Mapping MUST不读取或改变per-bone blend weight

#### Scenario: Movement Player进入source relevance

- **WHEN** 编译为CommittedMovement的Sequence Player首次获得entry relevance
- **THEN** Player MUST锁定当前committed owner与generation并以该raw anchor采样
- **AND** 后续Marker Sync MUST只计算effective time，不得替换Player的clock owner
