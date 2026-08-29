## ADDED Requirements

### Requirement: Animation Pipeline必须发布唯一正式Foot Motion Runtime Frame

在`build-character-foot-motion-data-foundation`归档后，Projection Compiler MUST从原生AnimationClip Catalog的完整Foot Motion Curve组和匹配Foot Analysis lineage生成唯一typed Runtime payload。Payload MUST包含左右脚Step Time、Step Distance、Foot Height、Contact、Lock Mode、Lock Weight、Support及稳定Landing Event table；Event table MUST把Contact-only Landing与Predictive Landing显式分型。Contact-only Landing只可成为Current Contact且三个Swing提前时间 MUST全部为0；Predictive Landing才可成为Next Landing，并 MUST保存与同Source/Cycle/Side/ordinal一致的PreSwing、Swing、Approach Contact与Landing边界，使Runtime可以发布typed `InApproachContactToLanding`事实。Approach Contact边界 MUST取同一Foot Motion Artifact在正式LiftOff之后、Landing之前最后一次Contact由零进入正值的首个正值采样；缺少该Contact边沿时Projection Build MUST拒绝该Source，不得用LiftOff、旧Feature Phase或固定提前时间补全。非循环片段的首个Landing没有前置LiftOff时，0秒正式Contact为零才可把片段起点声明为被裁剪Swing的明确起点；0秒已经Contact时该Landing MUST作为Contact-only Event，不能进入Next Landing。若Landing本身就在0秒，同样只建立起始Current Contact。Toe、Ground Pose证据 MAY进入只读诊断字段，但不得形成第二行为输入。

每个表现帧 MUST从与Component Pose相同的选中Live Contribution采样一个`Foot Motion Runtime Frame`，并携带Program、Projection、Completion、Node、Source、Contribution Continuity、Clip、Cycle、Normalized Time、Event lineage与按Event table求出的PreSwing/Swing/Approach Contact/Landing阶段事实。离散Lock Mode、Landing Event与Approach Contact阶段 MUST不跨Source混合；多Source混合时 MUST使用Pose贡献链已经选定的同一正式Source，而不是按Foot字段另行择优。

Foot Placement MUST只消费这一份Frame。缺失完整Curve、Event table、Contribution归属、非有限值或stale lineage时 MUST使依赖Foot Placement的当前Pose帧typed invalid；不得读取Library Artifact、旧隐藏Foot Feature、默认Curve或另一Source作为fallback。

对应消费者迁移完成时，旧Step、Constraint、PlantConfidence和Support Projection字段及reader MUST删除。系统 MUST不长期保存新旧Foot Motion Frame并在运行时选择输出。

#### Scenario: 混合中的正式Foot Motion Source

- **WHEN** Pose由多个Live Animation Source贡献且Foot Placement需要正式Foot Motion Frame
- **THEN** Runtime MUST使用Pose贡献链选定的同一Source、Cycle、Normalized Time和Completion采样完整左右脚Frame
- **AND** MUST不分别混合Step Time、Lock Mode或Support生成不存在于任一AnimationClip的组合

#### Scenario: 正式曲线或Event table缺失

- **WHEN** 选中Source缺少任一必需Foot Motion Curve、稳定Event table或匹配Registered Curve Hash
- **THEN** Projection Build或Runtime准备 MUST拒绝该Source
- **AND** MUST不回退旧Foot Analysis Feature、PlantConfidence、默认值或另一Source

#### Scenario: Approach Contact边界驱动Plant目标准备

- **WHEN** 选中Source的同一Landing Event从Swing进入Event table声明的Approach Contact区间
- **THEN** Foot Motion Runtime Frame MUST发布同Source、Cycle、Side、ordinal与Event identity的`InApproachContactToLanding`
- **AND** Runtime MUST用该事实进入持续Plant目标准备，不得按固定秒数、脚高、PlantConfidence或另一Source提前冻结世界落点
