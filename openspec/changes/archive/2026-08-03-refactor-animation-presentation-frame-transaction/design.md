## Context

当前动画表现帧表面上具有事务边界，但事务内部先修改正式运行状态，再依赖完整旧状态恢复：

```text
BeginFrameTransaction
    -> Action/Slot/MM/Pose BeginMutation
    -> PosePlanExecutionRuntime.CaptureFrameState
    -> 原地推进所有Module
    -> Animancer Graph Evaluate并写Physical Bones
    -> ValidateCommit
    -> Commit后丢弃快照
```

`CaptureFrameState`继续递归捕获Native workspace、Inertialization、Final Pose page、Physical Source和真实Transform。该做法提供before-image rollback，却使所有成功帧支付异常恢复成本，并且Animancer Evaluate已经发生真实骨骼副作用后，外层仍然继续执行可能抛错的完成和提交逻辑。

项目已有可复用基础：Final Pose publisher已经具有Prepared/Published双Page，Native计划已经拥有固定容量workspace，Transition Routing使用typed request/completion，Gameplay Pipeline已经采用Prepare/Finalize/Commit职责分离。本change把这些局部边界统一成动画表现唯一事务，不引入第二运行链。

## Goals

- 正常无诊断interest的PresentationFrame不因事务或诊断创建托管数组、List、Dictionary、Clone或ToArray结果。
- 已提交状态在进入唯一Animancer Evaluate Barrier前保持不变。
- Dense Pose和每帧必然完整生成的数据直接写Pending页，不先复制Committed页。
- 稀疏生命周期变化写入固定容量journal，不复制完整Registry。
- Physical Bone只由Final Pose写入边界修改，不再保存整Rig旧Transform。
- Evaluate前的预期不可用状态用typed outcome表达，不使用异常触发回滚；Evaluate后出现的非Committed outcome进入Actor Fault边界。
- 不可预期异常产生清楚的Actor Presentation Fault，不提供恢复后继续运行的隐藏路径。
- Gameplay rollback与visual correction保持现行唯一链路。

## Final Architecture

```text
Committed Presentation State
    | read-only
    v
Begin Pending Frame
    +-- pending scalar state
    +-- fixed-capacity mutation journal
    +-- pending source lifecycle commands
    +-- pending Native/Pose page
    +-- pending Final Pose page
    |
    v
Prepare + Validate
    |
    v
Animancer Evaluate Barrier
    +-- source capture jobs
    +-- Pose Graph job writes Pending Pose
    +-- Final Writer validates whole result
    +-- valid: writes Pending Pose to Physical Bones
    +-- invalid: keeps Committed Pose and records non-Committed outcome
    |
    v
Seal
    +-- only Committed outcome swaps Committed/Pending page indices
    +-- apply prevalidated scalar/journal mutations
    +-- acknowledge command
    +-- publish release completion
    +-- execute deferred release
    +-- publish Final Pose
    +-- publish diagnostics only with interest
```

## State Classification

| 状态类别 | 正式实现 | 原因 |
|---|---|---|
| Dense local pose、velocity、weight、parameter、native result | Committed/Pending双页 | 每帧本来就完整生成，直接写Pending比复制旧页更便宜 |
| Inertialization history与residual | 双页或算法明确的current/next页 | 下一状态由上一状态计算，成功后交换页 |
| PoseState、Player、Slot、Transition小型状态 | 固定布局pending state | 编译期数量固定，不需要对象图快照 |
| Action registry、source ownership、release handshake | 固定容量mutation journal | 每帧变化稀疏，完整复制Registry浪费CPU |
| 新Playable/source资源 | prepared resource | Prepare可创建但不进入Committed owner；Discard时释放prepared资源 |
| 旧Playable/source释放 | deferred release command | 完整帧成功前不得销毁Committed资源 |
| Diagnostics | interest-gated预分配页 | 无interest时零复制，有interest时只读Committed结果 |
| Gameplay rollback history | 保持现有Simulation Snapshot/Input History | 不属于动画表现事务 |

## Decisions

### 1. Dense双页与稀疏journal组合，而不是所有状态整页复制

Pose和Native数据每帧自然会产生完整下一页，使用Ping-Pong页。Action Registry、Physical Source ownership和release队列只记录本帧变化，使用Projection容量约束的journal。journal在Prepare阶段校验identity、重复项、容量和依赖，Seal阶段按固定顺序执行已验证mutation。

业务收益：普通动画帧不再复制未变化的Action、source和生命周期对象，同时保留同帧Action、Slot、Pose与release的原子归属。

代价：查询本帧状态时需要通过`Committed + Pending Journal`的统一只读view，Module合同必须明确区分Committed读取和Pending mutation。

完整状态双页也是可行方案，优点是实现简单；代价是每帧仍复制大量未变化Registry和history。纯mutation journal也可行，优点是常态写入少；代价是Dense Pose和Native Job无法高效通过稀疏日志表达。因此本change固定组合模型，不提供运行时选择。

### 2. Animancer Evaluate是唯一不可逆提交门槛

当前Pose结果和Final Stream有效性只有在Graph Evaluate后才能完整确定。为避免第二Animator、第二PlayableGraph或隐藏Sampling Rig，本change保留现有唯一Graph，并把`m_Animancer.Evaluate`定义为不可逆门槛。

门槛前必须完成所有托管identity、容量、source ownership、release依赖、Job binding和writer binding验证。门槛后的Seal不得执行可能因业务输入失败的查找、分配或编译，只允许固定页交换和已验证mutation提交。

业务收益：复用唯一正式Graph，不增加一套昂贵Sampling Rig，也不改变AnimationClip和Playable生命周期真相。

代价：若Unity Graph Evaluate或门槛后的内部不变量发生不可预期异常，系统不能证明Physical Stream未部分改变；对应Actor Presentation必须Fault并停止继续表现，不能假装恢复成功。

独立Sampling Rig可以让Graph Evaluate本身完全隔离，代价是每Actor复制Animator、骨架、Playable和资源生命周期，形成第二物理采样路径。本项目不采用该方案。

### 3. Final Writer同时读取Pending Pose和Committed Pose

Final Writer在写任何骨骼前先验证全部Physical handle、Pending Pose availability、continuity和completion identity。全部合法时写Pending Pose；typed Invalid或Pending未完成时保持同一已提交结果，并记录非Committed Frame Outcome。外层已经跨过Evaluate Barrier，因此不会交换Pending状态页，并把Actor Presentation置为Faulted。Writer不得先写部分骨骼后再返回失败。

业务收益：预期的source Pending、Pose Invalid或Job未完成不会让角色显示半套新Pose，也不需要提前复制真实Transform。

代价：运行时必须常驻一份可供writer读取的Committed Final Pose。该内存本来就是动画连续显示所需结果，不形成多Tick骨骼历史。

### 4. 资源创建可以Prepare，资源销毁只能Commit后执行

新Source Visual、Mixer、Capture Playable和Clip State可以在Prepare阶段创建为prepared resource，但在Seal前不得替换Committed ownership。Prepare失败时只销毁本帧新建资源。旧source的Disconnect、Destroy和workspace复用只能由成功帧产生的deferred release command执行。

业务收益：Action快速打断、Stored Pose和多个Slot不会因半帧失败提前销毁仍在使用的source。

代价：切换帧内新旧source可能同时存活，Projection必须提供明确的峰值容量，容量不足直接以配置错误失败，不动态扩容。

### 5. 预期失败使用typed outcome，不进入全量状态恢复

Missing Sample、Provider Pending、Readiness未满足、release completion未到达等运行条件必须在进入Evaluate前通过现有Availability、Outcome和Completion合同关闭Pending帧。若Pose Invalid只在Evaluate内才能确定，Final Writer保持Committed Physical Pose并返回非Committed outcome，外层随后使Actor Faulted；它仍不启动全量状态恢复。异常只用于配置身份失配、容量合同被破坏、非法调用顺序或Unity运行时失败。

业务收益：游戏中正常等待资源或切换动画不会触发昂贵异常路径，诊断可以显示准确业务原因。

代价：每个Module需要把当前仍通过throw表达的预期分支逐项归类，不能简单包一层catch继续运行。

### 6. 提交门槛后的异常使Actor Presentation Faulted

门槛前异常执行`DiscardPending`并保持Committed状态。门槛后异常记录一次结构化上下文，使`CharacterAnimationPresentationRuntime`进入Faulted，拒绝后续`Present`并把原异常向上抛出。系统不恢复Physical Transform、不继续旧动画、不自动重建Runtime。

业务收益：求职Demo中的真实配置或代码错误会明确暴露，不会由每帧几MiB备份成本掩盖。

代价：单个Actor的表现异常不再尝试同进程自愈；上层Session是否终止继续由现有异常传播边界决定。

### 7. Prediction correction只重基线表现，不保存骨骼窗口

Rollback Window继续只保存Gameplay/World/Pipeline Snapshot和input history。回滚或hard recovery提交新Body/Intent、Action EventId和Playback identity后，Presentation通过现有ResetSequence、branch replacement、Action sample anchor和Player continuity重新求值。需要视觉接管时只读取当前Committed Pose作为Blend/Inertialization起点，不创建逐PresentationFrame Pose历史。

业务收益：Gameplay立即恢复正确真相，角色位置和动画可以从当前可见状态有界收敛，不把网络纠偏成本乘以骨骼数量和渲染帧数。

代价：窗口外hard recovery不能逐帧复现旧视觉历史；离散Action必须立即按新真相Replace/Retire，视觉层只负责平滑结果。

### 8. Diagnostics必须先有interest再复制

Runtime Target Registry向Publisher提供当前interest位集。无Live、Capture、Pose Watch或Candidate Detail interest时，Publisher不调用`CopyOperations`、`CopyFinal`、`CopyPoseWatches`或逐骨骼contribution复制。有interest时从成功提交的Committed页复制到既有诊断双页。

业务收益：正常游玩不为关闭的Live Debug支付约两毫秒复制成本，打开调试时仍能读取与正式提交一致的数据。

代价：开始关注前的PresentationFrame没有诊断快照；若需要历史，Capture必须从显式interest建立后开始记录。

## Failure Matrix

| 阶段 | 失败类型 | 正式结果 |
|---|---|---|
| Begin/Prepare | identity、容量、调用顺序异常 | Discard Pending，Committed不变，向上抛错 |
| Prepare | typed Pending/Unavailable | 关闭Pending帧或保持Committed Pose，不消费command |
| Validate | source/release依赖不完整 | typed Invalid或配置错误，禁止进入Evaluate |
| Evaluate前 | Job binding无效 | Discard Pending，真实Rig不变 |
| Evaluate | Pending Pose typed Invalid | Final Writer保持Committed Pose，Pending不提交，Actor Presentation Faulted |
| Evaluate | Unity/Animancer不可预期异常 | Actor Presentation Faulted，向上抛错，不全量恢复 |
| Seal | 已验证mutation提交 | 页交换、ack、lifecycle、release、final publication固定完成 |
| Seal | 内部不变量异常 | Actor Presentation Faulted，禁止后续帧 |

## Migration

1. 先建立统一Frame Phase、Pending Page、Journal、Prepared Resource和Fault合同，不接入第二调用路径。
2. 把Final Pose Publisher现有Prepared/Published Page提升为正式Committed/Pending结果页。
3. 把Native Workspace和Inertialization迁移为current/next页，让Job只写Pending。
4. 按Action、Marker、PoseState、Player、Slot、Routing、MM顺序把每个`CaptureState/RestoreState`替换为pending state或journal。
5. 把Physical Source注册、连接和释放迁移到prepared resource/deferred release。
6. 修改Final Writer在写骨骼前完成整Rig验证，并读取Committed/Pending Pose。
7. 将Animancer Evaluate设为唯一Animancer Evaluate Barrier，收紧前后可执行操作。
8. 删除外层`FrameState`、Physical Transform capture/restore和所有只为旧rollback存在的Clone/ToArray。
9. 接入interest-gated diagnostics，删除无interest复制。
10. 删除旧BeginMutation/Rollback语义和无引用State类型，最终只保留新唯一事务。

迁移过程不得保留Runtime开关、旧事务adapter、双写或可选兼容链。任一中间状态不能形成可运行的第二动画路径；实施应在同一change中完成代码切换与旧路径删除。

## Spec Conflicts And Resolution

- current specs未要求全量动画快照，且明确禁止Pose进入Gameplay rollback；本change没有与现行Prediction/Presentation设计冲突。
- current `character-animation-pipeline`的“外层事务原子消费”缺少具体存储语义。本change增加双页、journal、Animancer Evaluate Barrier和Fault要求，原要求继续成立。
- completed `refactor-animation-control-boundaries`把“真实staged transaction”和“Evaluate失败回滚全部模块”同时写入任务，实际实现选择了before-image restore。本change把“回滚”统一解释为门槛前Discard Pending，删除门槛后的状态恢复。
- current diagnostics requirement允许Snapshot但没有interest门禁。本change修改为只从成功提交页按显式interest发布。
