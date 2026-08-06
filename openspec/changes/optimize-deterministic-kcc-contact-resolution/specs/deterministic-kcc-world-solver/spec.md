# Deterministic KCC World Solver Specification Delta

## MODIFIED Requirements

### Requirement: KCC 必须使用确定数值与固定查询顺序

Deterministic KCC MUST保留完整 earliest-TOI canonical contact set，并按Fixed displacement、effective obstruction normal和stable contact identity选择唯一movement obstruction representative。完整Hit Stability与Step Detection MUST每个movement iteration最多对该representative执行一次；所有其它contact MUST继续参与collision classification、active constraint、remaining projection与final validation。代表选择与query fast path MUST不改变canonical hit identity、tie-break、BodyResult或StateHash。

#### Scenario: 同一 TOI 返回墙面与支持面多个 contact

- **WHEN** capsule cast在同一最早TOI返回多个canonical contact
- **THEN** KCC MUST按固定规则选择唯一movement obstruction representative
- **AND** MUST只对该representative执行一次完整Hit Stability与Step Detection
- **AND** 所有contact MUST继续进入active constraint求解

### Requirement: Grounding 必须区分任意地面命中与稳定支持面

Ground Probe MUST继续按Philippe inner/outer probe与previous grounding规则完整评估。Movement contact set MUST不把每个contact都当作独立Ground/Step事务；movement representative负责movement policy，非representative contact的基础稳定性、碰撞分类与约束继续参与同一Motor loop。Grounding result MUST不由transient diagnostics或query optimization覆盖。

#### Scenario: 墙面接触同时包含稳定地面 contact

- **WHEN** earliest-TOI contact set同时包含stable ground与vertical wall
- **THEN** Ground Probe与stable support结果 MUST保持原有语义
- **AND** Step Detection MUST只针对唯一wall obstruction representative执行一次
- **AND** stable ground contact MUST仍参与constraint与collision summary

### Requirement: Step Up 必须由当前 movement contact 触发完整候选事务

Step Up MUST由唯一movement obstruction representative触发，并继续使用Standard-first、Extra、candidate overlap、outer stable ray、upward clearance、inner stable ray、SteppedSurfaceId与commit landing的完整阶段。Detection MUST在进入Standard/Extra CastAll前通过previous stable、upward intent、closing direction、vertical obstruction和固定obstacle height envelope admission。Admission失败 MUST只记录确定性rejection并回到普通multi-plane projection，不得构造第二Step路径。

#### Scenario: 角色持续顶住超过最大高度的墙

- **WHEN** movement contact是非稳定垂直wall且固定obstacle height envelope证明其不可跨越
- **THEN** KCC MUST在Standard/Extra CastAll前拒绝Step Detection
- **AND** MUST保持safe position、普通wall slide或BlockedNoProgress结果
- **AND** MUST不改变Ground Probe、Body Motion或下一Tick VerticalVelocity

#### Scenario: 合法 Step 只有一个代表性 obstruction

- **WHEN** previous stable、无向上意图、vertical obstruction与Standard或Extra candidate均合法
- **THEN** KCC MUST只执行一次完整Detection并按相同SteppedSurfaceId执行Commit
- **AND** MUST继续把remaining movement送回同一Motor loop

### Requirement: Fixed Query Kernel 必须具有稳定 Feature 身份和 canonical contact set

Query Kernel MAY使用closest-only selection、candidate context与重复排序消除，但 MUST保持stable PrimitiveId、FeatureId、TOI、normal、witness、separation、candidate/contact capacity和deterministic failure contract。Query context MUST只存在当前Move/Query workspace，不得进入Snapshot、StateHash、跨Tick cache或第二Solver。

#### Scenario: closest-only Raycast 使用快速选择

- **WHEN** Raycast只需要closest hit
- **THEN** Kernel MAY不对全部ray hit执行Array.Sort
- **AND** MUST按原stable distance、PrimitiveId、FeatureId tie-break返回相同closest result与summary语义

### Requirement: Deterministic KCC 热路径必须有界且无隐式扩容

Resolver MUST在Session创建时预分配representative、candidate context、contact、step与diagnostic workspace。单次Move内相关query MAY复用同一稳定candidate context，但每个子query MUST按自身bounds重新过滤。热路径 MUST不使用LINQ、动态集合、字符串或隐式扩容；任何capacity overflow MUST产生确定性失败。

#### Scenario: 多次 Step 子查询共享预分配候选工作集

- **WHEN** Standard/Extra validity在同一Move内执行多个相关query
- **THEN** Query Kernel MAY复用当前Move candidate context
- **AND** 每个query MUST得到与独立BVH查询相同的canonical contact/result
- **AND** context MUST在Move结束后失效

### Requirement: KCC 策略身份必须覆盖接触代表与查询策略

KccId或WorldConfigurationHash MUST包含movement obstruction representative selection、Step admission与Query Kernel execution strategy版本。不同策略、准入边界或tie-break的Peer MUST在Session Active前拒绝互连。

#### Scenario: Peer 使用不同 representative strategy

- **WHEN** 两个Peer的KccId包含不同representative或admission strategy revision
- **THEN** Rollback handshake MUST因KCC identity不匹配而拒绝Session

