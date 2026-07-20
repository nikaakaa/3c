# 动作角色移动同步方案调研

## 研究问题

当前业务要求是两个玩家在近身动作战斗中保持本地即时响应，同时角色身体可以互相阻挡。问题分成两层：

1. 网络层如何让各端收敛到同一移动结果。
2. WorldSolver如何计算静态世界与Actor身体接触。

第二层不是网络协议。无论选择哪种同步方案，角色硬接触都必须由该方案认可的唯一Gameplay真相计算；渲染插值只能平滑已经确定的Body结果，不能替代碰撞裁决。

## 官方方案事实

### 服务端权威预测与纠偏

Unreal CharacterMovement的标准流程是：owner客户端立即模拟并保存move，服务端用同一移动逻辑重演；结果不一致时服务端发送修正，owner恢复后重放未确认move；远端角色作为Simulated Proxy消费服务端复制状态并做network smoothing。资料：

- https://dev.epicgames.com/documentation/unreal-engine/understanding-networked-movement-in-the-character-movement-component-for-unreal-engine?lang=en-US

Unity Netcode for Entities同样把客户端预测描述为“客户端不等待服务端结果，先用自己的输入模拟”，并提供prediction smoothing处理纠偏。它同时明确区分预测时间线与远端插值时间线：远端缓冲两个已知快照之间的结果会增加延迟，但能吸收网络抖动。资料：

- https://docs.unity.cn/Packages/com.unity.netcode%401.4/manual/prediction.html
- https://docs.unity.cn/Packages/com.unity.netcode%401.5/manual/interpolation.html

这类方案不要求所有平台bit-exact确定，但要求客户端预测与Authority使用相同业务移动语义。客户端不知道远端未来输入时，远端Actor接触通常只能使用最近权威Body的插值/有限外推，随后接受一次服务端纠偏。

### 确定性输入同步与Rollback

Photon Quantum的正式模型是：客户端只交换玩家输入，每端运行相同确定模拟；本地可以先预测，确认输入到达后如预测错误则恢复旧Frame并重演。服务器组件负责输入分发、时钟和延迟管理，但不要求把完整Gameplay状态持续复制给客户端。资料：

- https://doc.photonengine.com/quantum/v3/quantum-intro
- https://doc.photonengine.com/quantum/v3/manual/frames

这类方案要求所有影响结果的Gameplay状态、世界碰撞、Actor接触、RNG和时间推进都可确定重演，并进入Snapshot/Hash。角色互相碰撞时，不能只回滚本地owner；同一Tick的完整Actor batch必须按相同顺序求解。

### 延迟Lockstep

Quantum也提供不执行rollback的lockstep模式，并建议用较大的input delay等待输入；其Session配置把lockstep、rollback window、checksum interval和input delay明确区分。资料：

- https://doc.photonengine.com/quantum/v3/manual/config-files

Lockstep的一致性最直接，但等待全部玩家输入会把网络延迟直接转换为操作延迟。它适合回合制、慢节奏或能接受明显input delay的业务，不适合作为当前第三人称动作Demo的主手感方案。

## 可选方案与业务取舍

| 方案 | Owner响应 | Remote表现 | Actor硬接触 | 主要代价 | 本项目位置 |
|---|---|---|---|---|---|
| ServerAuthoritative + prediction/reconciliation | 立即 | 权威快照插值/有限外推 | Authority完整求解，owner预测使用ObservedKinematic远端Body | 远端突然改向会纠偏；Authority和Prediction必须共享接触语义 | 作品主线，现有Float32/DotRecast change |
| Deterministic predict/rollback | 立即 | 本地完整世界预测，确认后重演 | 所有Peer同一Fixed batch求解 | 确定性、Snapshot、Hash和重演成本最高 | 当前隔离对比Demo |
| Delayed deterministic lockstep | 延迟input delay | 所有端同Tick | 所有Peer同一确定solver | 动作响应变慢 | 不选择 |
| Authority state sync，不做owner prediction | 等待网络 | 快照插值 | Authority唯一求解 | 实现简单但owner手感最差 | 不适合玩家角色，可用于非核心对象 |
| 取消/软化Actor硬碰撞 | 取决于网络模型 | 最容易平滑 | 不建立硬接触真相 | 玩家可重叠/穿过，玩法改变 | 只有业务允许时才可选 |

## 对当前Rollback Demo的决定

当前change继续选择Deterministic predict/rollback，不改成ServerAuthoritative，也不在同一Session里混入Authority correction。原因不是“移动同步只能这么做”，而是该Demo本身要证明Fixed Program、完整World Snapshot和restore/replay/hash链路。

在这个选择下，Actor硬接触必须补进`DeterministicKccWorldSolver.ResolveBatch`：

```text
CanonicalInputBundle
  -> Fixed Program Evaluate all Actors
  -> all static-world candidates
  -> stable ActorId pair sweep/contact
  -> static-world reconstraint
  -> atomic all-Actor BodyResult
  -> Fixed World Snapshot + Hash
  -> Presentation interpolation
```

不能采用以下路径：

- 每个Peer用Unity Physics或CharacterController独立碰撞后只同步Transform。
- Presentation Collider把视觉位置反写Gameplay Body。
- 只在冲刺/攻击节点增加特殊防穿透逻辑。
- 复用DotRecast Float32 `ActorContactSolver`实现或数据类型。
- Actor接触失败时退化成无碰撞继续推进。

## Fixed Actor接触范围

当前2v2vE技术Demo选择最小但完整的`SolidBodyBlock`：

- fixed capsule/cylinder contact shape。
- stable ActorId pair order。
- 垂直区间过滤。
- 相对位移连续平面sweep，防止高速穿透。
- 静止目标阻挡主动移动者但不被隐式推行。
- 双方移动时裁剪相对闭合法向并保留切向移动。
- 接触修正后重新约束静态世界。
- 固定容量、固定迭代、最终间距验证和原子提交。

不包含质量、冲量、弹性、动态刚体、moving platform、RVO、队伍穿透、ghost、攻击击退和通用物理。击退属于Gameplay产生的显式MotionRequest，不属于接触层隐藏副作用。

## 扩展判断

该pair求解是O(n²)，对2v2vE的小固定roster可接受，而且比空间分区更容易证明稳定顺序。若将来扩展到几十或上百Actor，应单独设计确定性spatial broadphase及其canonical bucket order，不能直接把当前小roster实现宣称为通用大规模方案。

当前方案只能在同build、同Fixed ABI和匹配World/KCC identity下声明确定性。跨CPU、Mono/IL2CPP和跨平台确定性仍需要golden replay与checksum证据，不能由“使用定点数”自动推出。
