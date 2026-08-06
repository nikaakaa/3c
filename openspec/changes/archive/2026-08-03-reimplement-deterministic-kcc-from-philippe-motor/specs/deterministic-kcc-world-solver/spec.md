## MODIFIED Requirements

### Requirement: Grounding 必须区分任意地面命中与稳定支持面

Grounding MUST以锁定版本Philippe KCC `EvaluateHitStability`与`ProbeGround`的行为顺序作为主要语义基准，并使用Fixed query重新表达。每个ground或movement hit MUST先按坡度得到base stability，再使用与reference一致的inner/outer向下ray probe形成`FoundInnerNormal`、`InnerNormal`、`FoundOuterNormal`、`OuterNormal`、ledge side、ledge distance与朝空侧运动状态；实现 MUST不使用第二个capsule landing代替reference ray probe。

Ground report MUST分别输出`FoundAnyGround`、`IsStableOnGround`、`SnappingPrevented`、support Surface/Primitive/Feature identity、Ground/Inner/Outer normal、distance与ledge state。陡坡 MAY成为collision contact但 MUST不成为stable ground。previous grounding、previous inner normal、当前inner/outer normal、ledge distance、movement direction与MaximumStableDenivelationAngle MUST共同决定是否允许继续snap；任何诊断不得反向覆盖该结果。

#### Scenario: 胶囊位于普通稳定踏面

- **WHEN** ground capsule sweep命中可站立表面且inner/outer probe没有形成禁止稳定的ledge或denivelation条件
- **THEN** Grounding MUST报告FoundAnyGround与IsStableOnGround
- **AND** MUST提交该surface的canonical support identity与Ground/Inner/Outer normal

#### Scenario: 台阶鼻部需要内外法线

- **WHEN** capsule contact本身位于台阶鼻部，但inner ray命中稳定踏面、outer ray命中空侧或非稳定面
- **THEN** Hit Stability MUST按Philippe ledge规则记录inner/outer证据
- **AND** MUST不因capsule contact法线不稳定直接丢弃合法step detection

#### Scenario: 角色越过允许站立的ledge距离

- **WHEN** 角色位于空侧、到ledge距离超过MaximumStableDistanceFromLedge或朝空侧运动触发正式限制
- **THEN** Grounding MUST取消stable snap或设置SnappingPrevented
- **AND** MUST不跨空侧保留previous support

#### Scenario: 下坡法线变化超过denivelation限制

- **WHEN** 当前inner与outer normal或previous inner与当前outer normal的下降夹角超过MaximumStableDenivelationAngle
- **THEN** Grounding MUST设置SnappingPrevented并保留movement结果
- **AND** MUST不通过独立Step Down重新吸附到该落点

### Requirement: Step Up必须由当前movement contact触发完整候选事务，下降必须由Ground Probe统一处理

Step Up MUST只由当前movement sweep的真实非稳定contact触发，并以锁定版本Philippe KCC的`DetectSteps`、`CheckStepValidity`与movement step commit顺序作为主要语义基准。Standard detection MUST使用MaximumStepHeight上方的capsule down sweep、候选最终位置capsule overlap、outer stable ray、实际rise upward capsule clearance和inner stable ray；Extra detection MUST只在Standard失败后按MinimumRequiredStepDepth建立补充capsule down sweep，并复用同一个validity流程。`MinimumRequiredStepDepth` MUST不再表达两个capsule landing必须同高。

Philippe `Collider`身份 MUST映射为collision artifact `SurfaceId`。Baker MUST为每个作者Collider生成一个稳定`SurfaceId`，同一MeshCollider或TerrainCollider生成的全部Primitive MUST共享该身份。Step detection与commit MUST选择相同`SteppedSurfaceId`，但movement blocker MAY属于另一个SurfaceId，且实现 MUST不要求相同PrimitiveId、triangle adjacency或outer/inner高度差落在QueryTolerance内。Detection候选位置与Commit最终位置 MUST分别按对应cast distance扣除`CollisionOffset`。

previous state稳定且hit normal本身不稳定时，Motor MUST按Philippe `GetObstructionNormal`从previous ground normal、hit normal与Up重算有效障碍法线。只有该有效obstruction近似垂直、previous state稳定grounded、当前请求没有明确向上脱离意图时才可commit。垂直障碍判断、Commit前进方向与普通constraint plane MUST共用该有效法线。Commit MUST从safe position沿障碍内侧加入SteppingForwardDistance，从MaximumStepHeight上方向下capsule cast到相同SurfaceId landing，验证最终无overlap并保留remaining movement进入同一Motor loop。

下降 MUST不再使用独立Step Down candidate。合法下坡与下台阶 MUST由previous grounding控制的Ground Probe、SnappingPrevented和ledge/denivelation规则统一处理。Step与Ground Probe只改变position、applied displacement与support结果，MUST不写入或推导VerticalVelocity。

#### Scenario: Standard路径走上普通楼梯

- **WHEN** previous state稳定grounded、当前近似垂直obstruction存在合法outer capsule候选、outer与inner ray法线稳定、实际rise净空充足且commit找到相同SurfaceId landing
- **THEN** KCC MUST提交Step Up
- **AND** MUST把remaining movement继续送入同一movement loop

#### Scenario: Extra路径走上窄但合格踏面

- **WHEN** Standard detection没有合法候选，但在MinimumRequiredStepDepth位置的Extra capsule sweep通过同一validity流程
- **THEN** KCC MUST记录Extra来源的ValidStepDetected与SteppedSurfaceId
- **AND** Commit MUST继续遵守相同SurfaceId、垂直obstruction、最终overlap与remaining规则

#### Scenario: 胶囊鼻部接触不能替代ray probe

- **WHEN** outer或inner位置的capsule contact法线不稳定，但reference位置的point ray命中稳定顶部
- **THEN** Step Detection MUST使用ray probe结果判断踏面
- **AND** MUST不返回当前outer/inner capsule landing拒绝原因

#### Scenario: Mesh表面的不同triangle属于同一作者Collider

- **WHEN** blocker、validity landing与commit landing具有相同SurfaceId但PrimitiveId不同
- **THEN** KCC MAY接受该Step candidate
- **AND** MUST按canonical hit顺序选择最终PrimitiveId与FeatureId作为support

#### Scenario: movement blocker与合法踏面属于不同Collider

- **WHEN** 当前movement blocker与CheckStepValidity选中的踏面具有不同SurfaceId，但commit重新命中该SteppedSurfaceId
- **THEN** KCC MAY接受该Step candidate
- **AND** MUST不把blocker SurfaceId强加为SteppedSurfaceId

#### Scenario: 垂直墙没有合法顶部

- **WHEN** capsule down sweep、outer/inner ray、upward clearance、相同SurfaceId commit或最终overlap任一阶段失败
- **THEN** KCC MUST不提交任何Step位置
- **AND** MUST从原safe position与原contact继续普通multi-plane projection

#### Scenario: 连续走下合法楼梯

- **WHEN** previous grounding允许snap且扩展Ground Probe在正式距离内找到未被ledge或denivelation禁止的stable surface
- **THEN** KCC MUST通过Ground Probe提交下降后的稳定位置
- **AND** MUST不建立第二个Step Down事务

#### Scenario: 离开平台

- **WHEN** 扩展Ground Probe没有stable surface或当前ledge/denivelation规则设置SnappingPrevented
- **THEN** KCC MUST保留movement位置并报告非稳定ground
- **AND** MUST不跨悬崖吸附到MaximumStepHeight内的任意表面

### Requirement: Ground Probe必须受上一支持面和当前运动意图约束

Ground Snap MUST收敛为Philippe KCC Ground Probe的一部分，不得与独立Step Down并存。没有previous stable或last movement ground证据时，probe MUST只使用MinimumGroundProbingDistance；previous snap未被禁止且previous stable或LastMovementIterationFoundAnyGround成立时，probe距离 MUST为`max(Radius, MaximumStepHeight) + GroundDetectionExtraDistance`。非稳定hit MAY在固定GroundProbeReboundDistance与迭代预算内沿命中面继续探测；stable hit只有在`SnappingPrevented == false`且当前MotionRequest没有明确向上分量时才可提交位置。

#### Scenario: 上一帧稳定地面上的连续下坡

- **WHEN** previous stable成立且扩展Ground Probe找到合法stable surface
- **THEN** KCC MUST允许连续snap并更新完整ground report
- **AND** MUST保存下一Tick需要的inner/outer normal与SnappingPrevented状态

#### Scenario: 明确向上位移

- **WHEN** MotionRequest包含明确向上分量
- **THEN** KCC MUST不提交扩展ground snap
- **AND** MUST不通过Step Down或previous support覆盖该运动意图

#### Scenario: 非稳定hit后找到稳定地面

- **WHEN** 第一次ground sweep命中非稳定面，但固定rebound范围内沿命中面继续探测得到稳定地面
- **THEN** Ground Probe MAY提交该稳定地面
- **AND** 查询次数与方向变化 MUST服从锁定budget和canonical顺序

### Requirement: Fixed KCC 必须保持可移植并遵守第三方来源边界

Fixed KCC runtime MUST只依赖portable Core与Fixed数值模块，MUST不引用UnityEngine、Unity Physics、CharacterController、DotRecast或第三方Unity KCC assembly。角色movement policy MUST主要基于proposal锁定版本的Philippe St-Amand Kinematic Character Controller，并逐项记录Sweep、Raycast、Overlap、Hit Stability、Step、Ground Probe、Ledge/Denivelation和remaining movement的Fixed语义映射。正式实现 MUST使用项目命名、Fixed类型、canonical identity和collision artifact重写，不得把未跟踪reference源码、Unity对象或third-party assembly复制进正式Runtime。

Rapier/Parry与PhysX MAY继续作为底层shape query、penetration和数值稳健性参考，但 MUST不覆盖Philippe movement policy。OpenKCC MAY继续作为静态测试课程参考，但 MUST不成为runtime movement算法或fallback。

#### Scenario: 构建portable Fixed KCC

- **WHEN** 编译Deterministic KCC程序集及其portable依赖
- **THEN** 依赖图 MUST不包含`com.janooba.kcc`、`KinematicCharacterMotor`、Unity Physics或OpenKCC runtime
- **AND** movement policy文档 MUST能追溯到锁定package版本和源文件哈希

#### Scenario: reference查询类型被映射

- **WHEN** Philippe branch调用CharacterCollisionsSweep、CharacterCollisionsRaycast或CharacterCollisionsOverlap
- **THEN** Fixed实现 MUST分别调用语义对应的capsule cast、raycast或capsule overlap
- **AND** MUST不为减少查询类型而改变该branch的准入意义

#### Scenario: 本地reference版本变化

- **WHEN** 本地package版本或锁定SHA-256与proposal不一致
- **THEN** 实施 MUST停止并先更新reference对账文档
- **AND** MUST不静默按新文件继续实现或保留旧行为fallback

### Requirement: Deterministic KCC必须约束已积分位移而不私有拥有重力

Deterministic KCC MUST消费Fixed Body Motion Integrator产生的完整XYZ `CharacterMotionRequest`，继续以唯一Motor执行continuous cast、slide、step、Grounding与Ground Snap，并准确返回applied displacement、稳定Grounded及方向性Above/Below。previous state稳定且本Tick没有明确向上意图时，Motor MUST在movement sweep前把平面请求重定向到previous ground tangent，并约束已积分位移中的负Y分量；角色不稳定或离地时 MUST继续消费原始负Y位移。只有现有`IsStableOnGround`语义可以映射为portable Grounded；`FoundAnyGround`、非稳定陡坡或普通下方接触 MUST不冒充稳定Grounded。KCC Motor、query kernel、Solver Definition与collision artifact MUST不保存GravityAcceleration、MaximumFallSpeed或私有VerticalVelocity积分规则。Deterministic KCC只有在调用Fixed唯一Body Motion Finalize提交VerticalVelocity后才能声明`AirborneVerticalMotion`。

#### Scenario: 稳定地面上的已积分重力位移

- **WHEN** previous state稳定、MotionRequest包含平面移动与负Y重力位移且没有明确向上意图
- **THEN** Motor MUST把平面移动重定向到previous ground tangent并约束负Y分量
- **AND** MUST继续由Ground Probe更新最终support，而不是让ground TOI零接触抢占台阶obstruction

#### Scenario: Fixed Actor离开悬崖

- **WHEN** Prepare产生向下gravity delta且KCC找不到稳定支持面
- **THEN** KCC MUST报告Airborne而不执行跨断崖Ground Snap
- **AND** Fixed Finalize MUST保存candidate VerticalVelocity
- **AND** KCC MUST不自行再次应用Gravity

#### Scenario: Fixed Actor向上撞顶

- **WHEN** 最终request向上且continuous capsule query命中上方阻挡
- **THEN** KCC MUST报告Above并返回受约束的applied displacement
- **AND** Fixed Finalize MUST按统一规则清零向上VerticalVelocity
