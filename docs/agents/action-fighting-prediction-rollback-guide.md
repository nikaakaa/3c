# 动作格斗预测回滚方案

本文用于实现动作格斗方向的网络同步、客户端预测和回滚前阅读。目标不是把 GGPO 原样搬进 Unity，而是吸收它的核心思想，把当前 3C demo 改造成固定 tick、输入驱动、可快照、可恢复、可重放的动作模拟。

## 结论

动作格斗适合提前按预测回滚友好的方式设计。

本项目当前不应该一步到位做完整 GGPO/P2P 帧同步，而应该先做：

```text
固定 tick 动作模拟
输入历史
状态快照历史
本地重放一致性测试
预测输入
权威输入或权威快照校正
回滚恢复与重放
表现事件去重
```

最终网络形态更推荐：

```text
客户端本地立即响应输入
客户端发送 input frames 到 Fantasy 服务端
服务端按 tick 做权威模拟或轻量校验
服务端回传 confirmed input / authoritative snapshot
客户端发现预测错误时从旧 tick 恢复并重放
```

不推荐在当前阶段做：

```text
直接同步 Transform
直接同步 Animancer state
网络回包里直接改角色位置
回滚系统绕过 PlayerLocomotionController 直接移动角色
为了网络新建第二套状态机
```

## 为什么动作格斗需要预测回滚

动作格斗对输入延迟非常敏感。闪避、弹反、打断、霸体、攻击 active frame、受击硬直这些规则通常只有几个 tick 的窗口。

如果采用保守帧同步：

```text
等待远端输入到齐
再推进 tick
```

手感会变黏。

预测回滚的做法是：

```text
远端输入没到时先预测
本地继续推进
真实输入到了以后检查是否猜错
猜错就回到旧 tick
用真实输入重放到当前 tick
```

所以预测回滚解决的是：

```text
低输入延迟
+ 多端判定一致
```

它特别适合角色数量少、判定窗口短、每 tick 状态可保存的实时对抗游戏。

## GGPO 核心模型

GGPO 的重点不是某个网络库，而是同步内核。参考：

- `Ref/ggpo/src/lib/ggpo/sync.cpp`
- `Ref/ggpo/src/lib/ggpo/input_queue.cpp`
- `Ref/ggpo/src/lib/ggpo/backends/synctest.cpp`
- `Ref/ggpo/src/include/ggponet.h`

GGPO 要游戏侧提供几个能力：

```text
save_game_state
load_game_state
advance_frame
synchronize_input
free_buffer
log_game_state
```

它内部做的事：

```text
AddLocalInput
  记录本地输入到当前 frame

SynchronizeInputs
  返回当前 frame 所有玩家输入
  远端输入缺失时用预测输入

IncrementFrame
  frame + 1
  保存当前状态

CheckSimulation
  检查输入队列是否发现预测错误

AdjustSimulation
  LoadFrame(错误帧)
  ResetPrediction
  advance_frame 多次追到当前帧
```

`InputQueue` 的关键点：

- 每个玩家一条输入队列。
- 输入必须按 frame 连续进入。
- 如果请求的 frame 没有真实输入，就预测上一帧输入。
- 真实输入到达后，如果和预测不一致，记录 first incorrect frame。
- 回滚从 first incorrect frame 开始。

`SyncTestBackend` 的关键点：

- 原始执行保存每帧 checksum。
- 过几帧后加载旧状态。
- 用同样输入重放。
- 对比 checksum。
- 不一致就输出原始状态和重放状态日志。

这个 synctest 对本项目非常重要。我们应该先做本地重放一致性测试，再接真实网络。

## 不要照搬 GGPO 的部分

GGPO 是为确定性 P2P lockstep 游戏设计的。Unity 动作 demo 不能直接套：

- Unity `CharacterController`、浮点、动画播放不是天然 bit-perfect。
- Animancer/Animator 运行时对象不能被二进制保存后跨环境恢复。
- Fantasy 服务端路线更适合服务端权威或半权威校正，不一定走纯 P2P。
- 当前项目强调统一状态机和现有运动主线，不能为了 GGPO 新建一条控制路径。

应该借鉴的是：

```text
固定 tick
输入队列
预测输入
状态快照 ring buffer
加载旧状态
重放输入
同步测试
```

不应该借鉴的是：

```text
把整个 Unity 场景二进制 save/load
直接把 GGPO 网络层塞进项目
让回滚系统成为新的角色控制器
```

## 当前项目可用基础

当前项目已经有几块对的地基：

```text
SimulationTick
SimulationTickRunner
SimulationTickPhaseOrder
UnitySimulationTickDriver
LocomotionTickAdapter
InputRequestBuffer
CharacterStateMachineRunner
CharacterStateMachineSnapshot
ActionRuntimeStateSnapshot
PresentationTransformInterpolator
```

现有 tick phase：

```text
ReadInput
UpdateInputBuffer
GameplayDecision
BuildMotion
ExecuteMotion
WriteSnapshotAndEvents
PresentationBridge
```

推荐映射：

```text
ReadInput
  采集本地输入
  写 PredictionInputHistory

UpdateInputBuffer
  更新预输入请求

GameplayDecision / BuildMotion / ExecuteMotion
  继续走统一状态机和 PlayerLocomotionController

WriteSnapshotAndEvents
  写 CharacterSimulationSnapshot 到 SnapshotHistory

PresentationBridge
  处理表现插值和事件去重
```

这说明预测回滚应该是 tick runner 外围的编排层，不是新 gameplay 主线。

## 目标架构

```text
Unity Input System
  |
  v
PredictionInputFrame
  |
  +--> PredictionInputHistory
  |
  v
SimulationTickRunner
  |
  +--> PlayerLocomotionController
  +--> CharacterStateMachineRunner
  +--> MotionExecutor
  |
  v
CharacterSimulationSnapshot
  |
  +--> PredictionSnapshotHistory
  |
  v
Reconciliation / Rollback Replay
  |
  +--> Restore snapshot
  +--> Replay inputs through existing tick runner
  |
  v
Presentation layer
  |
  +--> animation facade
  +--> visual interpolation
  +--> VFX/SFX/camera event sequence dedupe
```

网络接入后：

```text
Client
  本地输入立即进入预测模拟
  批量发送 input frames

Server
  接收 input frames
  推进权威 tick
  回传 confirmed tick / authoritative snapshot

Client
  对比本地 snapshot
  一致则丢弃旧历史
  不一致则 restore + replay
```

## 核心数据模型

### PredictionInputFrame

每 tick 一份输入事实。

建议字段：

```text
CharacterId
Tick
MoveX
MoveY
LookX
LookY
RunHeld
AttackPressed
AttackHeld
AttackReleased
DodgePressed
DodgeHeld
DodgeReleased
JumpPressed
JumpHeld
JumpReleased
InteractPressed
InteractHeld
InteractReleased
```

注意：

- Move/Look 可以先用 float，但要 clamp 和量化边界。
- 按钮事实必须区分 pressed/held/released。
- 输入帧只保存事实，不保存“这个输入导致了 Dodge”。
- 是否进入 Dodge 由状态机和打断规则决定。

### CharacterSimulationSnapshot

每 tick 一份可恢复状态。

建议字段：

```text
CharacterId
Tick
Position
Yaw
VerticalVelocity
Grounded
ActiveStateId
StateTimeTicks 或 StateElapsedSeconds
StateVariant
PendingTransitionId
ActionWorldDirection
RunLatch
LocomotionPhase
LocomotionGait
CurrentAnimationKey
AnimationNormalizedTime 或 AnimationElapsedTicks
LastPresentationEventSequence
Checksum
```

快照不能保存：

```text
Transform
GameObject
CharacterController
Animator
AnimationClip
AnimancerState
InputAction
MonoBehaviour 实例引用
```

可以保存：

```text
稳定 ID
枚举
数值
短字符串 key
量化向量
```

### PredictionInputHistory

类似 GGPO `InputQueue`，但第一版可以更简单：

```text
按 tick 写入
按 tick 查询
按区间读取
容量上限
确认 tick 后裁剪
缺失时输出诊断
```

远端输入缺失时，预测策略可以先用：

```text
重复上一 tick 输入
```

动作格斗里也可以针对按钮做更保守策略：

```text
方向保持
攻击/闪避 pressed 不重复预测
held 可以保持
released 缺失时按 held 处理或按策略处理
```

这块要测试，因为输入预测策略会影响误回滚频率。

### PredictionSnapshotHistory

类似 GGPO saved state ring buffer。

需要：

```text
写入 tick snapshot
查询 tick snapshot
查找最近可恢复 tick
裁剪已确认 tick
容量不足诊断
```

动作格斗一般可以先保留 8 到 20 tick。GGPO 默认最大预测帧是 8，但 Unity + Fantasy + 调试阶段可以先放宽。

## 状态恢复能力

这是最容易被低估的部分。

`CharacterStateMachineRunner` 现在有 `Snapshot`，但还需要恢复接口。恢复不只是：

```text
ActiveState
StateTime
```

还必须覆盖会影响下一 tick 输出的内部事实：

```text
currentNode
currentState
currentVariant
actionWorldDirection
pendingTransitionPath
animationRequestedForState
consumeRequestOnStateEnter
resetRunLatchOnStateEnter
setRunLatchOnTransition
StateTime
```

否则重放时会出现：

```text
状态名一样，但动画请求重复发
输入消费重复发生
动作方向丢失
Run latch 漂移
```

运动执行器也要恢复：

```text
真实根 position
真实根 yaw
vertical velocity
grounded facts
last world direction
current speed
```

如果 `CharacterController` 内部状态无法完整恢复，需要先记录问题，不要绕过系统强行改第二条移动路径。

## 表现层处理

表现层不是同步权威。

逻辑层产出：

```text
state id
animation key
event key
event sequence
pose snapshot
```

表现层消费：

```text
Animancer 播放
VFX
SFX
camera shake
UI
visual interpolation
```

回滚重放时要避免重复播放一次性事件：

```text
Tick + EventSequence + EventKey
```

如果已经播放过同一个 sequence：

```text
重放时不再次播放
```

如果回滚后产生新 sequence：

```text
允许播放
```

## 服务器权威怎么接

动作格斗可以有两条路线。

### 路线 A：P2P/GGPO 风格

```text
客户端互发输入
每端跑完整模拟
输入预测错误就回滚
checksum 检测分歧
```

优点：

```text
低延迟
服务器压力小
传统格斗成熟
```

缺点：

```text
反作弊弱
Unity 确定性压力大
NAT/连接复杂
```

### 路线 B：Fantasy 服务端权威

```text
客户端发送 input frames
服务端推进权威模拟
服务端回传 authoritative snapshot
客户端本地预测 + 回滚校正
```

优点：

```text
更适合项目现有 Fantasy 方向
反作弊更好
客户端可以不完全互信
```

缺点：

```text
服务端也要能跑核心动作逻辑
延迟校正更频繁
服务器成本更高
```

本项目建议路线 B，但内部借鉴 GGPO 的输入历史、快照历史、回滚重放。

## 分阶段实现路线

### 阶段 0：确认边界

目标：

```text
不新增第二套控制器
不绕过 PlayerLocomotionController
不让 Animancer 成为逻辑权威
不直接同步 Transform
```

输出：

```text
列出哪些状态必须进入快照
列出哪些表现不进入快照
列出当前无法恢复的运行时状态
```

### 阶段 1：本地输入历史

实现：

```text
PredictionInputFrame
PredictionInputHistory
Unity input adapter -> input frame
input frame -> BasicLocomotionInputSnapshot
```

测试：

```text
连续 tick 写入
同 tick 覆盖
按区间读取
容量裁剪
按钮 pressed/held/released round-trip
```

### 阶段 2：状态快照历史

实现：

```text
CharacterSimulationSnapshot
PredictionSnapshotHistory
WriteSnapshotAndEvents adapter
```

测试：

```text
快照构造
非法数值处理
ring buffer 裁剪
不引用 Unity Object
```

### 阶段 3：状态恢复

实现：

```text
CharacterStateMachineRunner.LoadSnapshot
MotionDriver restore adapter
Animation fact restore adapter
```

测试：

```text
恢复状态后下一 tick 输出一致
动画请求不重复
输入消费不重复
动作方向恢复
Run latch 恢复
```

### 阶段 4：本地 synctest

实现：

```text
跑 N tick
保存输入和 snapshot/checksum
加载旧 tick
用同一输入重放
比较最终 snapshot/checksum
```

这是最重要的质量门。

如果这个阶段过不了，不要接网络。因为接网络后问题只会更难查。

### 阶段 5：预测和回滚

实现：

```text
缺远端输入时预测
真实输入到达后比对
预测错误时找 first incorrect tick
restore snapshot
replay inputs
```

测试：

```text
预测正确不回滚
预测错误从错误 tick 回滚
缺输入历史时停止并诊断
回滚后 snapshot 收敛
```

### 阶段 6：权威快照校正

实现：

```text
authoritative snapshot
snapshot diff
ignore / snap / rollback 策略
```

测试：

```text
小误差忽略
中误差 snap
大误差 rollback
状态差异 rollback
```

### 阶段 7：Fantasy 协议

实现：

```text
C2G_InputFrameBatch
G2C_AuthoritativeSnapshot
DTO <-> model mapper
```

不要在 DTO 层直接操作 Unity 对象。

测试：

```text
input frame round-trip
snapshot round-trip
量化误差边界
```

## 第一版不要做的事

```text
不要做完整多人房间
不要做所有动作状态
不要做复杂 hitbox rollback
不要做 IK rollback
不要做物理场景整体 rollback
不要改成纯 P2P
不要删除现有 log
不要引入第二套状态机
```

第一版只需要证明：

```text
同一段输入可以重放出同一段动作状态和运动结果
```

## 动作格斗后续扩展

当移动、闪避、基础攻击能回滚后，再扩展：

```text
Hitbox/Hurtbox timeline facts
攻击 active window
无敌 window
格挡/弹反 window
命中确认
伤害和硬直
击退
受击状态
技能取消
资源消耗
冷却
```

这些都应该进入 tick 逻辑和快照，而不是由动画事件直接决定。

动画事件可以作为表现或采样来源，但最终要变成：

```text
tick N: hitbox active = true
tick N: invincible = false
tick N: cancel window = DodgeCancel
```

## 判断一个逻辑是否需要回滚

用这个标准：

```text
如果它影响胜负、位置、状态、命中、资源、是否能出招，就进入回滚核心。
如果它只是看起来怎样、听起来怎样，就在表现层，用 sequence 去重。
```

进入回滚核心：

```text
输入
状态机
动作窗口
打断规则
移动/朝向
hitbox/hurtbox
伤害
硬直
资源
冷却
逻辑事件
```

不进入回滚核心：

```text
Animancer state
AnimationClip 引用
粒子实例
音效实例
相机震动实例
UI 飘字实例
SkinnedMeshRenderer
```

## 推荐验收标准

实现前先定这些验收标准：

```text
1. 60 tick 下同输入重放 300 tick，状态 checksum 一致。
2. 移动和闪避重放后 position/yaw 在容差内一致。
3. 状态机 active state、variant、state time 一致。
4. 回滚后不会重复消费同一个预输入请求。
5. 回滚后不会重复播放同一个表现事件 sequence。
6. 禁用预测回滚时，现有本地动作 demo 行为不变化。
7. core 层不引用 Animancer、Cinemachine、Input System adapter、CharacterController。
```

## 最小技术路线图

```text
先本地，不联网
  InputFrame
  Snapshot
  Restore
  Replay
  Synctest

再单机模拟网络
  延迟远端输入
  预测远端输入
  输入纠错回滚

再接 Fantasy
  上传 input frames
  下发 confirmed/authoritative snapshots
  客户端 reconciliation

最后扩动作战斗
  hitbox
  hurtbox
  damage
  hitstun
  cancel windows
```

这条路线能保证每一步都能验证，也不会在网络层、表现层和角色控制层之间生成分裂路径。
