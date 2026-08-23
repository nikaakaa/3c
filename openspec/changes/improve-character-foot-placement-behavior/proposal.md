# Change: 改进角色Foot Placement接触行为

## Why

本change只描述统一姿态约束事务完成后的行为改进，不承担架构迁移。前置`refactor-character-pose-constraint-transaction`必须先把`8fc704a74ed3548c3357eff5c2d45f52d8366a4b`行为重新解释进唯一Foot Placement Module、typed Foot Context、Resolved Foot Pair和根Bank，并由用户完成端到端验收；此前不得实施本change，也不得在旧Runtime旁接入任何新算法。

8fc行为基线虽然能保持较高Anchor提交覆盖率，但仍存在四个已重复观测的问题：Swing按动画Phase采样并叠加未来Landing高度，Path Revision可直接改变脚高；PlantConfidence并非单调接触进度；Prediction Point、接触面与世界Anchor没有严格分型；Sliding和实时地面下限在正确性与连续性之间反复补偿。实验记录证明这些问题不能继续通过半衰期、固定0.12秒、5mm准入或提前冻结Prediction XYZ解决。

本change在干净架构内部替换Foot行为政策，不修改Module Interface、根事务、Goal ABI、FBBIK拓扑或Physical Writer所有权。

## What Changes

- Foot Analysis/Projection为每脚原子发布`AnimationFootContactPlanSample`，包含Event、权威Plan Source、晚期LandingStarted、单调LandingHeightProgress、唯一PlantStarted、Support和单调Release计划；Runtime不再解释原始Constraint或PlantConfidence。
- Swing Event入口捕获一次Swing Origin Sole，空间进度改为Animated Sole在Swing Origin到Next Landing方向上的投影；Raw Path修正只保留非负`Envelope - Baseline`高度增量。
- 同Event Path变化只替换唯一Path Target，保留Effective Correction/Velocity并使用临界阻尼连续追踪；Path Stable只是诊断事实，不决定Landing准入。
- Ground Path从包含Next Landing的连续同Surface接触段生成有限SupportDomain；Landing只冻结Patch和SupportDomain，不提前冻结Prediction XYZ为Anchor。
- Landing只在权威Landing Height窗口内保留动画XZ并沿Component Up交接；PlantStarted当帧把Current Effective Sole投影到SupportDomain内创建Anchor。
- Locked严格输出Anchor约束；删除8fc的Sliding水平削弱。Anchor超距、不可达或Grounded丢失进入正式Safety Release。
- 正常Release只消费Projection的ReleaseStarted/ReleaseProgress；不再用PlantConfidence、Constraint下降或固定Duration推断。
- Pelvis从Landing开始同时约束旧支撑腿和落地腿可达，但仍只消费Resolved Foot Pair。
- Diagnostics替换为Contact Plan、空间Swing、SupportDomain、唯一Correction、状态Trigger、Goal/Solved/Physical残差事实。

## Dependency

- 必须先完成并归档`refactor-character-pose-constraint-transaction`。
- 本change不得与架构重构并行实施，不得保留8fc与新行为的运行时开关、双状态机、双Goal或fallback。
- 实施时直接替换Foot Placement Module内部政策，外部Interface与根事务保持不变。

## Impact

- Affected specs: `character-animation-foot-analysis-artifact`、`character-foot-placement-presentation`
- Affected runtime: Foot Contact Plan读取、Swing Path Target、Landing/Plant/Release政策、Contact Patch、Pelvis可达输入
- Affected editor: Foot Analyzer、Artifact/Projection codec、Build质量校验、CSV与Gizmo
- 不修改Goal Contribution ABI、Goal Assembler、FBBIK拓扑、Physical Writer、Gameplay、KCC、网络或VisualRoot

## Non-Goals

- 不重新设计Foot Placement Module、根Bank、Goal ABI或Diagnostics事务所有权。
- 不实现Heel/Toe双点、脚掌旋转、移动平台、Reactive、传统IK、iStep、Current Trace或专用上下楼动画。
- 不保留8fc行为开关、兼容字段或第二实现路径。
- 不新增自动测试；实施阶段只执行项目规定的编译、静态检查和OpenSpec严格校验。

## Success Criteria

```text
权威Foot Contact Plan来自同一Event与Plan Source
Blend Space不平均不同Event的Trigger或Progress
Swing Progress来自Swing Origin空间投影
Raw Swing Correction逐值等于非负(Envelope - Baseline)
同Event Path变化不重置Effective Correction或Velocity
Landing前不存在世界Anchor
Frozen Patch拥有有限SupportDomain且不Clamp边界
Plant时Anchor XZ来自Current Effective Sole
Locked不再使用Sliding水平削弱
Runtime不解释Constraint或PlantConfidence生成Landing/Plant/Release
Pelvis在Landing阶段同时保证旧支撑腿与落地腿可达
不存在与8fc并行的状态机、Goal、配置或fallback
```
