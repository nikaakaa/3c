# Design: 离散楼梯表现层

## Context

Ramp与真实台阶不需要两套KCC，也不需要同一个楼梯组件的运行时策略：

```text
Ramp楼梯
  CharacterTraversal Ramp
    -> Collision Artifact
    -> Fixed KCC连续坡面
  FootPlacementSurface真实踏面
    -> Foot Placement

离散楼梯
  Ground阶梯Collider
    -> Collision Artifact
    -> Fixed KCC现有Step/Ground Probe
    -> Foot Placement
```

两种内容可以同时存在于一个Scene和一个Collision Artifact。Motor看到的只是量化碰撞几何，不需要知道“楼梯模式”。真正缺失的是离散台阶的表现边界：Fixed Body一级一级改变Y，而当前Grounded visible pose也直接一级一级改变Y。

## Goals

- 不修改KCC的前提下正式接入连续真实台阶内容。
- 让Ramp和真实台阶共用同一Collision Artifact、角色配置和Runtime组合。
- 只对接地竖直不连续增加有限表现修正，不给普通地面和坡面增加持续拖尾。
- 让VisualRoot、Foot Placement与默认Camera继续消费同一个最终Body Frame。
- 保持所有表现修正不进入Gameplay、Snapshot、Hash、网络或动画事务。

## Non-Goals

- 不增加`StairTraversalPolicy`或第二个楼梯作者组件。
- 不修改Philippe Step Detection、Step Commit、Ground Probe、Ground Snap或KCC参数。
- 不改变角色胶囊尺寸。
- 不为真实台阶增加新Unity Layer。
- 不让Runtime识别对象名、Tag、Stair identity或Collision Artifact来判断楼梯。
- 不增加Foot Placement楼梯状态、第二IK、Camera独立Body filter或Animation Timeline事件。
- 不自动生成场景几何、Bake、Build或运行Unity。
- 不新增测试；用户负责Unity端到端验收。

## Decision 1: 真实台阶是普通Ground世界几何

Ramp楼梯继续使用现有`StairTraversalSurfaceAuthoring`。真实台阶不注册该组件，而是使用一组持久化阶梯形Collider代理：

| 内容 | Layer | Deterministic Surface owner | Fixed Artifact | Foot Placement |
|---|---|---|---|---|
| Ramp | `CharacterTraversal` | 恰好一个 | 包含 | 排除 |
| Ramp楼梯真实踏面 | `FootPlacementSurface` | 零个 | 排除 | 包含 |
| 离散楼梯阶梯代理 | `Ground` | 恰好一个 | 包含 | 包含 |

离散阶梯代理与可见踏面的上表面、rise、run、宽度和首尾平台保持一致。同一组Collider同时服务离线Deterministic Bake和运行时Unity PhysicsScene Foot Placement查询，不再复制Presentation-only踏面或隐藏Ramp。

Collision Baker不增加离散楼梯代码分支。它仍然只收集`DeterministicCollisionSurfaceAuthoring`拥有的Collider；真实台阶与普通地面使用同一规则。`StairTraversalSurfaceValidator`仍然只校验显式注册的Ramp楼梯绑定。

### Tradeoff

- 普通`Ground`：现有KCC、Baker和Foot LayerMask直接工作，场景差异就是唯一差异；代价是普通地面和真实台阶不能只靠Layer区分，需要清晰的场景根与Surface identity。
- 新增`CharacterStepSurface`：Layer视图更明显，但消费者与`Ground`完全相同，增加Physics与Profile配置而不增加隔离能力，不采用。
- 给`StairTraversalSurfaceAuthoring`增加双策略：工具看似统一，但真实台阶本来就是普通世界几何，会把不需要的Ramp字段、validator和策略分支带进简单内容，不采用。

## Decision 2: Gameplay Lab提供同尺寸Ramp与离散台阶内容

保留现有`LowStairs_Rise0.14_Run0.45` Ramp路线。共享环境Prefab增加一段持久化`DiscreteStairs_Rise0.14_Run0.45`，使用相同单级rise、run与可行走宽度，便于直接比较：

- 不挂`StairTraversalSurfaceAuthoring`。
- 不包含`CharacterTraversal` Ramp。
- 阶梯Collider全部位于`Ground`。
- 阶梯Collider被唯一明确的Deterministic Surface作者拥有。
- 入口、顶平台和出口继续使用普通`Ground`。
- 不通过`GameplayLabAssetBuilder`、Collision Baker或Runtime程序化生成。

现有`StepCapabilityCourse`继续保留。它证明0.14m、0.24m Step准入与0.40m拒绝；新增连续离散楼梯负责表现与连续通行内容，不取代能力课程。

## Decision 3: Body Runtime增加独立竖直不连续阶段

现有`CharacterVisualTrajectoryFollower`继续负责Body branch replacement的position/yaw有界纠偏，并保留Grounded branch replacement只纠正水平位置的合同。新增`CharacterGroundedVerticalTrajectoryFollower`由同一个`CharacterBodyPresentationRuntime`拥有，执行位置固定为：

```text
Committed/Selected Body interval
  -> CharacterBodyTargetFrame采样
  -> CharacterVisualTrajectoryFollower
  -> CharacterGroundedVerticalTrajectoryFollower
  -> CharacterBodyPresentationFrame
  -> VisualRoot / Foot Placement / Camera
```

新阶段不是MonoBehaviour，也没有独立Tick、Transform或Body历史。它只保存当前竖直offset、offset velocity、当前interval identity和有限诊断状态。

### 不连续分类

普通Append interval满足全部条件时分类为接地竖直不连续：

```text
GroundedBefore == true
GroundedAfter == true
abs(CurrentPosition.y - PreviousPosition.y) >= DiscontinuityThreshold
当前Body reset sequence没有变化
当前更新不是CommittedBranchReplacement或SelectedStreamReset
```

正值为`Up`，负值为`Down`。分类描述Body轨迹形状，不推断KCC Step、楼梯对象或Surface类型。因此合法路沿和其它接地高度突变也获得同一视觉响应；普通Ramp和坡面只要每个interval的高度变化低于阈值就继续直接采样。

### 竖直目标与收敛

不连续interval第一次进入时：

1. 竖直target立即使用current Body endpoint Y，不再把高度跳变线性摊进一个Simulation Tick。
2. 从上一表现帧最终visible Y与新target Y建立唯一offset。
3. 保留有限的当前visible竖直速度，按临界阻尼half-life把offset收敛到零。
4. offset超过Maximum Error时夹紧并移除继续向外的速度。
5. offset与速度进入Settle Distance后归零。

同一个interval的后续表现帧只推进该状态。前一次台阶尚未settle又进入下一不连续interval时，使用当前最终visible Y和velocity重新定向同一个状态，不叠加新弹簧、不排队固定时长任务。残余修正在后续普通Grounded interval中叠加到连续target Y并继续衰减，因此不会阻止角色同时沿普通坡面移动。

输出visible vertical velocity由连续target vertical velocity与correction velocity合成；不直接把单Tick Step产生的actual Body velocity Y当作模型竖直脉冲。

### Reset与Airborne

- Initialization直接锚定初始target。
- SelectedStream Reset清除旧offset并锚定正式reset target。
- CommittedBranchReplacement不分类为台阶，并清除旧竖直台阶offset；既有Body branch follower继续执行自己的水平/yaw纠偏。
- 任一端Airborne时清除接地台阶offset，Jump与Landing继续使用正式Body采样，不借用台阶平滑。
- dispose与Body Runtime Reset清除全部阶段状态。

### Tradeoff

- 独立竖直阶段：不改变既有branch follower含义，职责和诊断清晰；代价是Body Runtime多维护一个很小的表现状态。
- 直接删除现有Grounded Y清零：会让所有branch correction、坡面和普通Grounded移动都进入通用position弹簧，形成长期拖尾，不采用。
- 从KCC Step diagnostics触发：能知道物理原因，但会让通用Presentation依赖具体Solver、ABI和网络传播，不采用。
- 只靠Foot Placement移动骨盆：脚可能稳定，但VisualRoot和Camera仍然按级跳，不采用。

## Decision 4: 竖直响应配置独立于网络纠偏配置

`CharacterBodyPresentationProfile`增加独立有限配置：

```text
GroundedVerticalResponseMode
  Direct
  BoundedDiscontinuityCorrection

DiscontinuityThresholdMeters
HalfLifeSeconds
MaximumErrorMeters
SettleDistanceMeters
```

`CharacterVisualTrajectoryMode.Direct/BoundedCorrection`继续只决定target branch变化时的水平与yaw响应。Grounded Vertical Response单独决定普通canonical interval中的竖直不连续表现。两者不得互相推断，也不得由SourceMode、Network Model、Camera ownership或Actor名称选择。

Corin Direct、Rollback与Observed三个正式Body Profile都显式配置`BoundedDiscontinuityCorrection`。初始作者值与现有Foot Placement突变口径对齐：阈值0.05m、最大误差0.30m；half-life与settle distance作为Body Profile显式值保存，不从KCC MaximumStepHeight或Foot Profile运行时读取。

### Tradeoff

- 独立设置：本地插值、网络branch correction和楼梯表现可分别解释；代价是Body Profile增加一组字段。
- 复用PositionHalfLife与MaximumHorizontalError：字段更少，但会把网络水平纠偏手感和楼梯竖直响应绑定，不采用。
- 从KCC配置读取MaximumStepHeight：参数看似一致，但Presentation开始依赖具体World Solver配置，不采用。

## Decision 5: Predictive Foot Placement和Camera不增加第二套楼梯逻辑

普通FootGrounding与可选Predictive Modifier通过唯一world-query backend查询`Ground | FootPlacementSurface`并排除`CharacterTraversal`：

- Ramp楼梯让FootGrounding的Lyra current Sphere Trace命中`FootPlacementSurface`，合法stance脚 MAY基于该命中建立surface-local anchor，可选Modifier只为未被anchor拥有的Swing脚得到Future Landing。
- 离散楼梯让两个阶段从同一组`Ground`阶梯Collider得到各自归属的current hit或Future支撑。

Body竖直阶段改变最终visible delta；普通FootGrounding读取正式Body Frame并按Lyra current trace、foot offset/normal smoothing、contact/anchor稳定和唯一Pelvis resolve生成Baseline Goals，可选Modifier只从Future Support/Envelope修改未被anchor拥有的Swing脚。两者都不得读取竖直阶段私有offset、维护第二Body filter或根据它切换Surface。

默认Camera继续由`CharacterCameraPresentationRuntime`使用最终`CharacterBodyPresentationFrame.VisiblePosition`和既有bind offset生成follow point。它不读取logic body、KCC diagnostics或场景楼梯，也不保存第二份台阶vertical filter。Cinemachine adapter现有阻尼仍只是CameraRig实现细节。

## Failure Policy

以下情况必须在作者校验、runtime创建或Body Presentation帧明确失败：

- Body Profile的竖直响应Mode未知或有界参数非法。
- 离散楼梯Collider未处于`Ground`、没有唯一Deterministic Surface owner或同时存在Ramp。
- 离散楼梯误挂`StairTraversalSurfaceAuthoring`或复制`FootPlacementSurface`碰撞副本。
- Body interval位置、Grounded、Tick或Reset identity不完整。
- 竖直阶段产生非有限offset、velocity或visible结果。

系统不得自动补默认Profile值、生成楼梯Collider、回退读取logic Transform、从KCC diagnostics重建事件或在Runtime改Layer。

## Migration

1. 增加Grounded Vertical Response有限配置与runtime settings。
2. 显式迁移Corin Direct、Rollback与Observed Body Profile。
3. 实现独立`CharacterGroundedVerticalTrajectoryFollower`及其有限结果。
4. 将该阶段接入Body Runtime唯一Present路径、Reset路径与diagnostics。
5. 保持Foot Placement和Camera只消费最终Body Frame并补齐只读诊断字段。
6. 在共享环境Prefab持久化作者`DiscreteStairs_Rise0.14_Run0.45`及其唯一Ground Surface owner。
7. 通过现有显式菜单重新Bake唯一Collision Artifact。
8. 更新current specs、`openspec/project.md`和KCC implementation inventory中的楼梯内容与Artifact身份。
9. 删除“所有连续楼梯都必须注册Ramp作者绑定”的宽泛文档口径，不删除现有Ramp工具或Step Capability Course。
