## ADDED Requirements

### Requirement: Corin 全部 AnimationTrack 必须显式选择 Marker Sync 策略

Corin每个可达AnimationTrack MUST显式配置为`None`或`MarkerGroup`，不得保留Unspecified。选择 MUST根据该producer真实动画语义、Timeline Once/Loop call site和完整marker coverage作出，不得按Locomotion、Attack、Dodge、Turn等状态名称硬编码。没有AnimationTrack的状态 MUST不创建伪Timeline、伪clip或伪marker。

#### Scenario: 打开Corin完整作者清单

- **WHEN** Compiler或Agent Validator遍历Corin全部RootTree、nested StateMachine与inline/shared Timeline
- **THEN** 每个可达AnimationTrack MUST拥有明确sync mode
- **AND** 任一Unspecified track MUST阻止发布

#### Scenario: WalkEnd没有动画资源

- **WHEN** WalkEnd继续只依赖Animancer transition blend且没有AnimationTrack
- **THEN** 迁移 MUST不为WalkEnd创建一次性Timeline或fallback clip
- **AND** Marker Sync inventory MUST不制造不存在的producer

### Requirement: Corin WalkLoop 与 RunLoop 必须共享 Locomotion.Gait

Corin WalkLoop与RunLoop AnimationTrack MUST配置为`MarkerGroup/Cyclic`并共享`Locomotion.Gait` SyncGroupId。两者 MUST按各自真实动画frame配置至少覆盖左右支撑两个方向的marker segment，不得假设normalized time或动画长度相同。Locomotion状态transition时刻、motion request和WorldSolver结果 MUST不因Marker Sync改变。

#### Scenario: WalkLoop切换RunLoop

- **WHEN** Corin从WalkLoop进入RunLoop
- **THEN** Base层RunLoop MUST在整个共同可见fade期间持续跟随WalkLoop当前marker segment
- **AND** Gameplay状态与运动 MUST在原logic tick立即切换

#### Scenario: RunLoop切回WalkLoop

- **WHEN** Corin从RunLoop进入WalkLoop
- **THEN** WalkLoop MUST读取RunLoop当帧effective phase
- **AND** MUST不使用上一次WalkLoop activation留下的offset或cycle

### Requirement: Corin 有限动作只能在资源满足时加入 Marker Group

RunStart、RunEnd、MovingTurn、Attack1至Attack5、Dodge及其它one-shot producer MAY配置为`MarkerGroup/Finite`，但仅当真实clip能够从frame 0到DurationFrame提供完整marker coverage，且同Layer同组directed pair契约成立。资源不满足时 MUST显式配置None并保留普通Timeline sample + Animancer fade；不得伪造支撑marker。Attack combo、recovery、cancel、IFrame与damage MUST继续由Action Context、TreeClip window、ConditionRule和State transition决定，不能由Marker Sync代替。

#### Scenario: RunEnd具有完整步态marker

- **WHEN** RunEnd真实动画能够表达Locomotion.Gait全部有向segment并覆盖完整Timeline
- **THEN** 作者 MAY将其配置为`MarkerGroup/Finite`
- **AND** RunLoop进入RunEnd时 MUST使用通用Cyclic到Finite映射

#### Scenario: Attack动画没有共同姿态契约

- **WHEN** Attack1与Attack2是顺序连段动作但没有同组完整marker语义
- **THEN** 两者AnimationTrack MUST显式为None
- **AND** Attack1到Attack2 MUST继续由ComboAccept窗口、State transition与目标Timeline ClipIn控制

#### Scenario: 一组Action变体确实需要同步

- **WHEN** 多个Action producer真实共享同一姿态marker语义与完整coverage
- **THEN** 作者 MAY为它们建立独立Action Marker Group
- **AND** Runtime MUST复用通用MarkerSyncRuntime，不得增加Attack专用matcher

#### Scenario: 动作退出到Locomotion

- **WHEN** Action producer为None并结束到Locomotion
- **THEN** Animation Runtime MUST使用普通Animancer transition与target raw Timeline time
- **AND** MUST不从Action名称或上一状态伪造Locomotion.Gait phase

### Requirement: Corin Marker Sync 配置必须通过正式 Agent v14 迁移

Corin AnimationTrack的sync mode、group、topology、SyncRole与marker，以及Timeline Clip已登记的Curve Channel MUST通过v14 `export_snapshot -> dry_run_patch -> apply_patch -> export_snapshot -> validate`流程写入。实现 MUST不直接修改CorinPlayableRootTree或shared Timeline YAML，不创建一次性migrator。迁移完成后 MUST重新生成匹配source revision的CharacterPresentationProjection及Float32/Fixed Program wrapper。

#### Scenario: 迁移Corin资产

- **WHEN** apply流程配置Corin全部AnimationTrack
- **THEN** dry-run与apply MUST消费同一immutable typed command plan
- **AND** 再次导出的Snapshot MUST显示全部可达track不再是Unspecified
- **AND** generated Projection MUST包含canonical group与segment occurrence索引

#### Scenario: generated artifact重建

- **WHEN** marker作者数据改变source revision
- **THEN** Float32/Fixed Program wrapper与Projection MUST通过正式编译流程重建
- **AND** Program Gameplay operation MUST不包含marker sync payload
