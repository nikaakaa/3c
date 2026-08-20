## MODIFIED Requirements

### Requirement: Foot Placement必须是唯一Goal事务

`CharacterFootPlacementRuntime` MUST只消费同帧Component Pose、左右原子Biomechanical Step Read Page、Body Presentation、Locomotion Motion Timeline、正式Future Body Translation、当前PhysicsScene，以及同一Foot Placement事务内部产生的Predictive与Reactive Proposal，并只输出Pelvis、LeftFoot、RightFoot三个Goal。一次Frame只能拥有一个Pending结果，并且必须由外层表现事务`Seal`或`Discard`。

系统 MUST不提供第二Grounded、第二Support Lock、第二Pelvis、第二GoalTransition、LegIK、TwoBoneIK、默认地面、固定高度、fallback、兼容Goal链、Animator IK写入或FBBIK后处理。Reactive Proposal MUST只是同一事务内的候选输入，不得成为独立Pose节点、MonoBehaviour、Prefab开关、Solver或Physical Bone writer。

#### Scenario: 一帧完成预测与响应裁决

- **WHEN** Foot Placement完成左右脚Predictive与Reactive Proposal、每脚唯一裁决、Support Lock和最终GoalTransition
- **THEN** Runtime MUST发布同Frame、Completion与Rig identity的三个Goal
- **AND** 外层事务 MUST对Landing、Measurement、Owner、Lock、Transition、Pelvis和Goal Pending结果执行一次统一`Seal`或`Discard`

#### Scenario: 响应式模块不可用

- **WHEN** Reactive Proposal因Phase、World Context或查询失败而Rejected
- **THEN** Arbiter MUST按正式来源所有权合同处理该脚
- **AND** 系统 MUST不启用原始`FootIK.OnAnimatorIK`、传统Grounding、默认平面或第二Goal链补洞

### Requirement: Ground Path必须使用上一已提交落点与下一事件落点

每只脚 MUST按Landing Event identity跟踪预测 `NextSwingLanding` 与最终 `Resolved Contact`。PreSwing或Swing阶段的每个有效表现帧 MUST执行一次且仅一次正式Future Landing SphereCast；同一事件的合法预测按正式更新死区更新唯一NextSwingLanding和Ground Path。响应式Footprint Query是独立的当前接触Proposal，不得替代Future Landing SphereCast、预测下一事件或直接写Ground Path终点。

事件完成时，Runtime MUST把该事件最后一个具有完整Frame、Completion、Rig、Event与Owner lineage的Resolved Contact原值晋级为`LastLanding`，包括点、法线、SurfaceIdentity、Resolved Source与Proposal Revision。若Reactive Proposal不可用但Predictive Proposal仍按相位和lineage保持正式Owner，Resolved Contact MAY来自Predictive Proposal；若没有合法Resolved Contact，MUST不晋级虚构LastLanding。完成帧查询、Animated Sole、默认点和旧Committed测量不得覆盖晋级事实。

Ground Path MUST只使用已提交LastLanding与当前预测NextSwingLanding构造输入。没有LastLanding时 MUST发布`CurrentLandingUnavailable`；不得用Animated Sole、Transform、固定高度、旧响应测量或默认地面补起点。支撑锁定后，LastLanding作为唯一Contact Anchor，不得因当前动画脚下的新响应测量逐帧移动。

#### Scenario: 同一Landing Event接近真实踏面

- **WHEN** NextSwingLanding保持预测事实且同一事件的Reactive Proposal在接触阶段成为Resolved Owner
- **THEN** 当前脚Goal MAY按正式曲线和handoff合同转向Reactive Contact
- **AND** Ground Path终点在事件完成前 MUST继续只表达预测NextSwingLanding

#### Scenario: 下一Swing Event完成并存在响应接触

- **WHEN** 事件完成前最后Resolved Contact来自具有完整lineage的Reactive Proposal
- **THEN** Runtime MUST把该Reactive点、法线、Surface与Proposal Revision原值晋级为LastLanding
- **AND** 下一Ground Path MUST从该已提交真实支撑点出发

#### Scenario: 下一Swing Event完成但响应查询失败

- **WHEN** Reactive Proposal为Rejected但Predictive Proposal仍是该事件合法Resolved Owner
- **THEN** Runtime MAY晋级该Predictive Resolved Contact
- **AND** diagnostics MUST明确记录Resolved Source为Predictive，不得伪装成响应命中

#### Scenario: 完成时没有合法Resolved Contact

- **WHEN** Predictive与Reactive Proposal都不具备该事件正式所有权
- **THEN** Runtime MUST不晋级LastLanding
- **AND** 后续Ground Path MUST发布typed rejection而不是使用旧目标或默认点

## ADDED Requirements

### Requirement: 每脚预测与响应Proposal必须经过唯一所有权裁决

左右脚 MUST各自只拥有一个Proposal Arbiter。Predictive与Reactive Proposal MUST先转换为相对同帧Original Ankle/Sole的Component空间修正，并验证Frame、Completion、Rig、Foot Side、Event与Proposal lineage。几何兼容时，Arbiter MAY按Reactive Ownership Weight混合修正；不得直接混合不同Frame、不同Original Pose或不同Rig的绝对世界目标。

兼容必须要求Surface相同或点位水平/垂直差位于正式Profile阈值内，并要求法线点积与腿部可达合同有效。不兼容时Arbiter MUST延续仍合法的唯一Committed Owner；只有Reactive Weight到达曲线终点、响应接触位于正式获取范围且lineage完整时，才可执行typed Predictive-to-Reactive handoff。Handoff只切换Raw Resolved Proposal，最终骨骼连续性 MUST继续由裁决后的唯一GoalTransition处理。原Owner失效且另一Proposal不满足接管合同时 MUST发布typed rejection，不得使用旧Goal、默认地面或隐藏fallback。

#### Scenario: 两个Proposal命中兼容表面

- **WHEN** Predictive与Reactive Proposal属于同一Frame、Foot、Event且Surface几何兼容
- **THEN** Arbiter MUST按Reactive Ownership Weight混合二者相对Original Pose的修正
- **AND** MUST只发布一个Resolved Proposal

#### Scenario: 两个Proposal命中不同台阶

- **WHEN** Predictive与Reactive Proposal的Surface或点位差超过正式兼容阈值
- **THEN** Arbiter MUST不在两个绝对世界点之间插值
- **AND** MUST延续合法Committed Owner或执行满足正式接管条件的typed handoff

#### Scenario: 所有候选均无所有权

- **WHEN** Predictive与Reactive Proposal都Rejected或都不满足当前相位所有权
- **THEN** 该脚 MUST发布typed Resolved rejection和零权重原始Goal
- **AND** MUST不沿用上一帧Resolved目标补洞

### Requirement: 响应所有权曲线必须按每脚生物力学相位独立采样

Foot Placement MUST只使用同一权威Biomechanical Step的EventPhase、ReleasePhase、LiftOffPhase、ApproachContactPhase与LandingPhase计算每脚原始接触权重，并分别用左右脚权重采样同一正式`ReactiveOwnershipCurve`。曲线输入与输出 MUST位于`[0,1]`，端点 MUST为`(0,0)`与`(1,1)`，并且 MUST单调不减。系统 MUST不新增响应式独立时钟、Clip全局左右脚共用相位或第二Lock Curve。

`ReactiveOwnershipCurve` MUST只决定Predictive与Reactive Proposal的目标来源。现有`animation.foot-placement-weight` MUST继续表达最终Foot Placement总强度；系统 MUST不把Reactive Ownership Weight直接当作FinalIK Position Weight，也 MUST不改写Lock Preparation或Unlock时间。

#### Scenario: 左脚支撑而右脚摆动

- **WHEN** 左脚生物力学接触权重为1且右脚仍处于无接触摆动区间
- **THEN** 左脚Reactive Ownership MUST为曲线终点值1
- **AND** 右脚Reactive Ownership MUST为曲线起点值0
- **AND** 两脚 MUST不因共享同一曲线资产而共享同一个采样相位

#### Scenario: 右脚接近落地

- **WHEN** 右脚EventPhase从ApproachContactPhase推进到LandingPhase
- **THEN** 右脚Reactive Ownership MUST按正式曲线从0连续推进到1
- **AND** 最终Foot Placement总强度 MUST继续由同帧`animation.foot-placement-weight`决定

### Requirement: 支撑锁定与盆骨必须只消费裁决后的唯一脚事实

Landing完成时，Support Lock MUST只获取该事件的Resolved Contact作为唯一Contact Anchor。锁定后响应式测量不得按当前动画脚位置逐帧搬动Anchor；Surface或接触失效时 MUST进入正式Unlock或Reacquire合同，不得创建iStep glue状态。下一Ground Path MUST从该已提交Anchor对应的LastLanding出发。

左右脚完成Proposal裁决、Support Lock与唯一GoalTransition后，Runtime MUST先把最终Position Weight应用到Ankle并同步投影到Sole，再由唯一Pelvis Builder消费这两个最终Sole。Predictive与Reactive原始Proposal MUST不分别驱动Pelvis，不得增加第二Pelvis Target、第二Spring或交叉淡入淡出。

#### Scenario: 支撑脚已经锁定

- **WHEN** 支撑脚已经从Resolved Contact获取Anchor且本帧响应查询点随动画脚发生变化
- **THEN** Support Lock MUST保持已提交Anchor
- **AND** MUST不让响应模块追随动画脚重写世界锁点

#### Scenario: 左右脚完成不同来源的裁决

- **WHEN** 左脚Resolved Source为Reactive且右脚Resolved Source为Predictive
- **THEN** Pelvis Builder MUST只消费两脚各自唯一最终GoalTransition输出
- **AND** MUST只发布一个PelvisPreSolveTranslation

### Requirement: 预测响应裁决诊断必须保持同一Completion谱系

Foot Placement只读诊断、Gizmo与CSV MUST记录每脚响应查询执行状态、Reject Reason、Footprint、主命中、法线修复、Committed/Pending Measurement Revision、生物力学接触权重、Reactive Ownership Curve值、Predictive/Reactive Proposal availability与修正、兼容结果、Committed/Pending Owner、Resolved Source/Surface/Contact/Correction、handoff reason以及晋级LastLanding的来源和revision。

所有字段 MUST来自最近一次成功Seal，并与最终Goal、FullBodyIK solved position和Physical Bone writer保持同一Frame、Completion与Rig lineage。Gizmo和CSV MUST不重新查询Physics、重算曲线、执行Arbiter、推进状态或执行FBBIK。

#### Scenario: 查看预测到响应的所有权交接

- **WHEN** 某脚在成功Seal帧执行Predictive-to-Reactive handoff
- **THEN** diagnostics MUST同时记录交接前Committed Owner、两个输入Proposal、兼容结果、曲线值、handoff reason和Resolved Proposal
- **AND** 最终Goal、FBBIK与Physical Bone字段 MUST能按同一Completion对账该Resolved结果

#### Scenario: 响应查询失败但预测继续拥有

- **WHEN** Reactive Proposal Rejected且Predictive Proposal按正式合同保持Resolved Owner
- **THEN** diagnostics MUST明确记录Reactive Reject Reason与Resolved Source为Predictive
- **AND** MUST不记录伪造Reactive Surface或默认接触点

### Requirement: Foot Placement调试面板必须通过同一Arbiter切换来源对比

现有Foot Placement调试面板 MUST在Editor/Development diagnostics session提供`Predictive Only`、`Reactive Only`与`Hybrid`三种Proposal Mode。`Predictive Only` MUST把同一Arbiter的Effective Reactive Ownership设为0；`Reactive Only` MUST在响应相位有效时设为1，Reactive Proposal无效时发布typed rejection而不得回退Predictive Proposal；`Hybrid` MUST使用左右脚各自的生物力学接触权重和正式ReactiveOwnershipCurve。

三种模式 MUST继续使用同一个FootPlacement节点、Predictive/Reactive Proposal合同、Arbiter Pending/Committed状态、Support Lock、GoalTransition、Pelvis、GoalSet、FullBodyIK和final writer。模式切换 MUST形成typed Debug Proposal Mode Handoff，并从上一Committed最终修正通过唯一GoalTransition连续收敛；不得创建第二节点、第二Profile、第二Solver、旧目标队列或骨骼直写。

Proposal Mode MUST只属于当前Editor/Development diagnostics session。面板关闭、Runtime重建或diagnostics interest释放时 MUST恢复Hybrid；选择 MUST不写入Foot Placement Profile、Projection、Prefab、Character State、Snapshot、网络packet或正式Player业务UI。正式角色配置 MUST只有Hybrid真相。

#### Scenario: 面板切到Predictive Only

- **WHEN** 用户在Foot Placement调试面板选择`Predictive Only`
- **THEN** 左右脚Arbiter MUST只让合法Predictive Proposal拥有Resolved结果
- **AND** Reactive Proposal MAY继续显示为只读对照但 MUST不影响Goal、Pelvis或FinalIK

#### Scenario: 面板切到Reactive Only

- **WHEN** 用户在Foot Placement调试面板选择`Reactive Only`
- **THEN** 响应相位有效的脚 MUST只让合法Reactive Proposal拥有Resolved结果
- **AND** Reactive Proposal无效时 MUST发布typed rejection且不得回退预测目标

#### Scenario: 面板切到Hybrid

- **WHEN** 用户在Foot Placement调试面板选择`Hybrid`
- **THEN** 左右脚 MUST分别按各自生物力学相位和同一正式曲线计算Effective Ownership
- **AND** 最终结果 MUST继续只通过同一Arbiter和唯一Goal链发布

#### Scenario: 关闭调试面板

- **WHEN** 当前diagnostics session释放Proposal Mode控制
- **THEN** Runtime MUST恢复Hybrid
- **AND** MUST不在Profile、Projection、Prefab或Gameplay状态中保留上一次调试选择
