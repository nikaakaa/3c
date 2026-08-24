# Change: 重构角色脚步与全身IK的统一姿态约束事务

## Why

本change的行为基准固定为`8fc704a74ed3548c3357eff5c2d45f52d8366a4b`。该版本已经形成一条可运行Foot Placement与唯一FBBIK链，但架构仍由`CharacterFootPlacementRuntime`直接编排Landing Lifecycle、Ground Path、Swing Builder、Effective Constraint、Primary Support和Pelvis；各对象分别持有Pending/Committed，状态、修正、诊断和Goal数据互相穿透。两个核心文件超过千行，Diagnostics大值传递还曾触发Mono `Passing an argument of size '10000'`。

本change只重构这些所有权和数据流，不优化Foot IK行为。8fc是行为Oracle而不是架构模板：旧`None/Acquiring/Locked/Sliding/Releasing`、Landing Lifecycle和Effective Constraint不会原样保留为正式Module，但它们的逐帧状态效果、公式、阈值、Anchor、Support、Pelvis和Goal结果必须由新架构准确重新解释。

任何相同输入下的脚修正、状态时机映射、Anchor、Primary Support、Pelvis或Goal差异都属于重构回归，不能解释为优化。新的Contact Plan、空间Swing、纯非负FootPath、有限SupportDomain、Sliding删除和Landing腿提前约束已经拆入后续`improve-character-foot-placement-behavior`，不得在本change实施。

## What Changes

- 建立唯一`CharacterPoseConstraintRuntime`双Bank根事务，统一拥有Foot Context、Ground Path页、Resolved Foot Pair、Primary Support/Pelvis、Goal Contribution/Goal Set、BendHistory/Solver Outcome与Diagnostics引用页。根Runtime只管理lineage、阶段顺序、页所有权和Seal/Discard/Invalidate，不实现Foot、Pelvis、Goal或Solver数学。
- 建立一个深`CharacterFootPlacementModule` Interface。外部只提交不可变Frame Input并读取一个Result，不知道Landing Prediction、Ground Path、左右脚、Support、Pelvis和Goal编码顺序。
- 每脚建立一个固定typed `CharacterFootStateContext`，由唯一`CharacterFootStateMachine`写入；Landing Lifecycle、Effective Constraint状态和输出历史迁入同一Context，不使用共享Dictionary、Gameplay Blackboard或可变Diagnostics。
- 用最终五状态重新解释8fc行为：未消费None为Swing，已消费None为UnlockedSupport，Acquiring为Landing，Locked和Sliding共同属于Locked且Sliding只成为内部Lock Response事实，Releasing保持Releasing。
- `LockResponse`只表达Locked内部当前使用完整Anchor还是8fc Sliding水平修正；它不拥有Event、Anchor、独立Transition或第二状态机。
- 保持8fc Prediction、Landing更新死区、Ground Path、Phase Swing、baseline height error、Path Residual、实时向上Floor、PlantConfidence准入/Ownership、Anchor创建、Sliding公式、Release HalfLife和完成条件逐值不变。
- 生成唯一且紧凑的`CharacterResolvedFootResult`与Resolved Foot Pair。正式下游合同包含Final Sole/Ankle、Effective Correction、Goal Weight、Contact Reference、Contact Ownership、Support Eligibility、Support Weight、Support Intent Weight、Support Horizontal Error、Support Event lineage、typed Pelvis Reach Reference与Outcome；State、Lock Response、Path、Residual和其它Context内部事实只进入Diagnostics。8fc重构阶段Support Intent逐值等于现有Support Weight，Pelvis Reach Reference只在现有Contact可用于Pelvis时指向同一点，因此不改变行为。
- State Machine按8fc映射直接发布`SupportEligibility = None | RetainOnly | AcquireAndRetain`：Swing/Landing/UnlockedSupport为None，Releasing为RetainOnly，Locked无论FullAnchor或Sliding Response均为AcquireAndRetain。Primary Support、Stride和Pelvis只能读取Eligibility及其它Resolved字段，不得读取State、Lock Response或Foot Context。
- Foot Placement与PoseBone来源输出typed Goal Contribution，唯一Assembler形成一个Goal Set，唯一FBBIK和Physical Writer继续消费同一结果；Foot Goal Encoder只能读取Resolved Correction和Goal Weight，Goal位置、权重、Slot和求解顺序保持8fc。
- FBBIK BendHistory、Solver Outcome与Diagnostics迁入根Bank引用页；运行方法不得按值传递完整Bank、Ground Path FixedList或Diagnostics聚合体。迁移前必须枚举Vendor FBBIK所有影响下一帧的隐式状态；若任一状态不能从Committed BendHistory精确重建，实施必须停止，不能用近似初始化改变基线。
- 删除旧Lifecycle、Effective Constraint对象、浅层Route/Reducer/Builder对外链、逐模块Seal、plural GoalSet兼容路径和运行Diagnostics反向依赖，不保留新旧双路径。

## Baseline Equivalence

相同Frame Input和映射后的上一帧Context必须满足：

```text
Swing Progress / Baseline / Envelope相同
Vertical Correction / Output Correction相同
PlantCycleConsumed与Anchor建立帧相同
Contact Progress / Ownership相同
完整Anchor与Sliding修正结果相同
Release Residual与完成帧相同
Support Eligibility / Support Weight / Primary Support相同
Pelvis Target / Spring Output相同
Foot与Pelvis Goal位置、权重相同
FBBIK输入、BendHistory与Physical Writer输入相同
```

旧状态名不要求保留，但必须满足design中的确定映射。Diagnostics名称和存储可以重构，不能改变正式结果。

## Impact

- Affected specs: `character-foot-placement-presentation`、`character-animation-pipeline`、`character-presentation-pose-graph`
- Affected runtime: Foot Placement Module、Foot Context/State Machine、Resolved Foot Pair、Primary Support/Pelvis、Pose Constraint根Bank、Goal ABI、FBBIK BendHistory、Physical Writer、Diagnostics
- Affected editor: Projection Goal拓扑、CSV、Gizmo、Pose Watch与Live Diagnostics的数据来源
- Affected simulation interface: Future Body Translation改为调用方预分配Workspace写入，不改变KCC预测数学
- 不修改Foot Analysis Artifact、动画曲线、Gameplay Body、KCC、VisualRoot、网络状态或rollback snapshot

## Current Spec Comparison

- current `character-foot-placement-presentation`仍表达Swing-only阶段并禁止Foot Lock与Pelvis。本change安装8fc已经运行的双脚锁定、Support和Pelvis行为，但用统一Module和Context重新解释。
- current Ground Path的Prediction更新、死区和Envelope行为保持不变，只迁移状态归属与调用Interface。
- current `character-animation-pipeline`仍允许Foot、Goal和Bend分散事务；本change用一个根Bank统一提交，不改变Pose求值结果。
- current `character-presentation-pose-graph`仍使用plural Goal Set输入；本change迁移为Goal Contribution与唯一Assembler，但保持8fc最终Goal集合和FBBIK顺序。
- `character-animation-foot-analysis-artifact`不受本change修改；Runtime继续消费8fc已有Step、Constraint、PlantConfidence和Support事实。

## Non-Goals

- 不新增或修改Foot Analysis、Contact Plan、Landing marker、Progress或动画曲线。
- 不改变Swing进度、FootPath公式、实时Floor、PlantConfidence、Anchor、Sliding、Release、Support、Pelvis或Goal行为。
- 不增加SupportDomain、Current Trace、Heel/Toe双点、旋转、移动平台、Reactive、传统IK、iStep或专用楼梯动画。
- 不保留8fc旧类作为fallback、兼容层、对照Runtime或运行时开关。
- 不新增自动测试；实施阶段只执行项目规定的编译、静态检查和OpenSpec严格校验，用户负责端到端行为对比。

## Success Criteria

```text
外部只存在一个深CharacterFootPlacementModule Interface
每脚只存在一个CharacterFootStateMachine和一个CharacterFootStateContext
旧状态通过确定映射重新解释为Swing/Landing/Locked/Releasing/UnlockedSupport
Sliding只属于Locked内部Lock Response，不形成第二状态机
全脚只有一个Effective Correction与一个Output历史Owner
Landing Lifecycle事实与Constraint历史属于同一Foot Context
Resolved Foot是紧凑正式结果，不复制Context或充当第二Blackboard
Primary Support与Pelvis只读取Support Eligibility及其它Resolved字段
正式下游不读取Foot State、Lock Response、Path或Residual
Foot、Pelvis、Goal、Bend、Solver与Diagnostics只提交一个根Bank identity
根Runtime只管理事务，不包含Foot、Pelvis、Goal或Solver数学
每帧只有一个Goal Set、一次FBBIK和一个Physical Writer
根Bank与大页使用预分配引用，不形成巨型值参数
Vendor跨帧状态可由Committed BendHistory精确重建
相同输入下的8fc基线结果逐帧等价
不存在新Contact Plan、空间Swing、SupportDomain或其它行为升级
不存在旧Lifecycle、Effective Constraint对象、逐模块Seal或并行Runtime路径
```
