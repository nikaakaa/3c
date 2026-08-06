# corin-foot-plant-lock Specification

## Purpose

定义 Corin 持续 Idle 的正式脚底锁定内容策略，以及该策略在唯一 Foot Placement、FullBodyIK 和 Character Build 链中的发布边界。该能力只安装现有通用 Foot Placement，不新增独立 IK、脚相位或动画播放路径。

## ADDED Requirements

### Requirement: Corin Idle必须使用正式脚锁策略

Corin 唯一 `CharacterFootPlacementProfile` MUST 将脚锁策略设置为 `PivotAroundToe`。Corin Idle source binding MUST保留全区间 `Foot Placement Weight = 1`，并引用与其 Rig、Calibration、Foot Analysis identity 精确匹配的正式表现输入。Corin MUST不以 `Unlocked` 作为Idle发布配置，也 MUST不通过状态名、Clip名或额外Blackboard字段强制脚锁。

#### Scenario: Corin Idle 双脚进入接触

- **WHEN** Idle source输出最终Component Pose，Foot Analysis左右脚Plant Confidence达到进入阈值，Body处于Grounded且Current Support合法
- **THEN** PredictiveFootPlacement MUST按现有Free到Locked生命周期捕获左右脚的Surface-local anchor
- **AND** FullBodyIK MUST在同一帧使用Foot Goals保持双脚接触，同时允许骨盆和上半身继续输出Idle动画运动

#### Scenario: Corin Idle脚锁释放

- **WHEN** Body离地、脚速超过释放阈值、脚超出腿长可达范围、Surface失效或发生Presentation Reset
- **THEN** Foot Placement MUST按现有释放原因进入Free或Sliding收口
- **AND** Runtime MUST不保留旧Idle世界锚点、不瞬移到旧锁点，也 MUST不创建Idle专用恢复分支

### Requirement: Corin脚锁必须复用唯一表现链

Corin脚锁 MUST只位于 `Presentation Fact -> PoseStateMachine -> state-local source -> Component Pose -> PredictiveFootPlacement Goal Source -> FullBodyIK -> OutputPose` 链。Foot Placement MUST继续是唯一world query、plant/release和Surface anchor owner；FullBodyIK MUST是唯一Physical Bone solver。Corin MUST不启用GASP、Animation Rigging、FinalIK component、TwoBoneIK、LegIK或图外脚锁路径。

#### Scenario: Pose Graph 发布 Corin Foot Goals

- **WHEN** Corin Pose Plan同时拥有Component Pose、PredictiveFootPlacement Goals和FullBodyIK stage
- **THEN** Goals MUST携带同帧Completion、Rig identity和Profile revision，并由唯一FullBodyIK消费
- **AND** Runtime MUST不从Animator Transform、AnimationClip或第二个Grounding结果重建脚目标

### Requirement: Corin脚锁变更必须通过显式Character Build发布

Corin Foot Placement Profile、Idle source binding、Rig、Calibration或Foot Analysis依赖改变时，系统 MUST先将受影响Projection和Program标记为Stale。资产选择、Inspector、`OnValidate`、Preview、重绘和普通dirty操作 MUST不运行Foot Analysis、Projection编译、Program编译或发布。只有显式Character Build MUST在完成输入identity校验后，原子发布匹配Profile revision的Projection、请求的Target Program、Pose tuning layout和Unity wrapper。

#### Scenario: 作者修改Corin脚锁模式

- **WHEN** 作者把Corin Profile从Unlocked改为PivotAroundToe
- **THEN** Profile revision和Presentation dependency MUST改变，已发布Projection MUST变为Stale
- **AND** 选择资产或Inspector回调 MUST不启动重分析、编译或Build

#### Scenario: 显式Build发布脚锁变更

- **WHEN** 作者通过Character Build显式请求Corin，并且Foot Analysis、Rig、Calibration、Pose Graph、FullBodyIK与Profile identity全部匹配
- **THEN** Build MUST原子发布新的Projection、请求的Target Program、tuning layout和wrapper
- **AND** 任一阶段失败 MUST保留旧发布组，不写入Unlocked兼容值、旧Projection或半套generated产物

### Requirement: Corin Idle脚锁验证必须在重操作前失败

显式Character Build MUST在执行Foot Analysis以外的重编译前验证Corin Idle binding、完整Foot Placement Weight、Foot Analysis identity、Profile lock policy和当前Pose/IK contract。验证失败 MUST返回精确错误并阻止后续重操作。该验证 MUST不放入`OnInspectorGUI`、`OnValidate`、selection或Preview热路径。

#### Scenario: Corin Idle缺少脚锁输入

- **WHEN** Idle binding缺失、Weight曲线不是全区间1、Foot Analysis artifact缺失或Profile仍为Unlocked
- **THEN** Character Build MUST拒绝该请求并报告具体资产和字段
- **AND** MUST不生成新的Projection、Program、tuning layout或Unity wrapper
