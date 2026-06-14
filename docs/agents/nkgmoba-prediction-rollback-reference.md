# NKGMobaBasedOnET 预测回滚借鉴文档

本文记录 `Ref/NKGMobaBasedOnET` 中预测回滚实现对当前 3C demo 的可借鉴点。它不是 OpenSpec proposal，也不是实现任务清单；真正开工前仍需要在对应 OpenSpec change 中写中文 proposal、design、spec delta、细粒度 tasks 和验证方式，审批后再实现。

## 结论

可以借鉴它的主结构：

```text
本地输入预测
服务端权威回包
按帧缓存输入和状态
收到回包后做一致性检查
不一致则组件级回滚
从回滚帧后追帧重放
```

不要照搬它的代码和运行时对象模型。当前项目必须继续复用：

```text
SimulationTickRunner
PlayerFullBodyActionController
PlayerLocomotionController
InputRequestBuffer
CharacterRuntimeBlackboard
CharacterSimulationSnapshot
PredictionInputHistory
PredictionSnapshotHistory
FullBodyRollbackSimulation
```

也就是说，NKGMoba 的价值是“同步编排和组件级职责划分”，不是把 ET 的 Room、Unit、LSF_Component 或命令派发器搬进项目。

## 参考工程核心流程

NKGMoba 的预测回滚主入口集中在：

```text
Ref/NKGMobaBasedOnET/Unity/Assets/Hotfix/NKGMOBA/Battle/LockStepStateFrameSync/LSF_ComponentUtilities.cs
```

它的流程可以拆成 6 步。

### 1. 客户端输入进入预测缓冲

客户端调用 `AddCmdToSendQueue` 时，会把输入命令写到下一帧。

同一条命令会进入两处：

```text
PlayerInputCmdsBuffer
  用于客户端本地预测和之后回滚重放

FrameCmdsToSend
  用于本帧末尾发送给服务端
```

这点值得借鉴：输入历史必须保存原始输入事实，不能保存“已经进入某状态”的结果。

对应到当前项目：

```text
PredictionInputFrame
PredictionInputHistory
InputRequestBuffer
FullBodyFrameInput
```

### 2. 客户端本地立即执行预测

每帧 Tick 时，客户端先查 `PlayerInputCmdsBuffer[currentFrame]`，如果有输入命令就立即派发执行。

这就是玩家操作不等服务端的原因。

对应到当前项目：

```text
ReadInput / UpdateInputBuffer 阶段写入输入事实
GameplayDecision 阶段消费输入请求
FullBodyFramePipeline 继续作为唯一动作主线
```

预测不应该直接改 Transform，也不应该绕过 `PlayerFullBodyActionController` 或 `PlayerLocomotionController`。

### 3. 服务端按权威帧处理并广播

服务端收到 `C2M_FrameCmd` 后只做一件事：把命令放进待处理队列。

随后服务端固定 Tick 推进，处理对应帧命令，再通过 `M2C_FrameCmd` 把服务端结果广播给客户端。

这点值得借鉴：网络接收不直接改变角色状态，只进入帧队列；真正改变状态发生在固定 tick 里。

对应到当前项目未来 Fantasy 接入：

```text
C2G_InputFrameBatch
  只入队输入帧

Server simulation tick
  消费输入帧并生成权威快照或确认帧

G2C_AuthoritativeSnapshot / ConfirmedInput
  客户端收到后进入 reconciliation 队列
```

### 4. 客户端收到回包后先校验

客户端收到 `M2C_FrameCmd` 后刷新服务端帧信息，并把服务端命令放到 `FrameCmdsToHandle`。

下一次本地 tick 时，客户端取最早的服务端回包帧进行一致性检查：

```text
远程玩家命令
  直接执行

本地玩家命令
  与本地历史快照或增量做一致性检查
```

只有本地玩家预测过的内容才需要回滚。

对应到当前项目：

```text
本地角色预测状态
  进入 strict / predictive 比较

远端角色或纯表现状态
  不能随便触发本地 FullBody 回滚
```

当前项目已有 `prediction-rollback-authority-scopes`，后续必须用权威域和比较域判断：

```text
StrictGameplay
PredictiveGameplay
PresentationDrift
Ignored
```

### 5. 不一致时组件级回滚

NKGMoba 不做完整世界二进制快照。它的抽象是：

```text
ILSF_TickHandler
ALSF_TickHandler<T>
OnLSF_CheckConsistency
OnLSF_RollBackTick
```

每个组件自己决定：

```text
每帧记录什么
如何和服务端回包比较
不一致时怎么恢复
```

移动组件最典型：

```text
MoveComponentTicker.TickEnd
  每帧记录 LSF_MoveCmd 历史

MoveComponentTicker.CheckConsistency
  比较服务端位置、目标点、速度、停止状态

MoveComponentTicker.RollBackTick
  停止当前移动
  恢复服务端位置和旋转
  重新导航到之前目标
```

行为树和 Buff 则使用全量快照加 delta：

```text
FrameSnaps_Whole
FrameSnaps_DeltaOnly
GetDifference
Check
```

这点值得借鉴：回滚框架只负责调度，状态恢复逻辑归各模块所有。

对应到当前项目：

```text
CharacterSimulationSnapshot
  聚合可恢复纯数据

PlayerLocomotionController snapshot/restore
  负责运动状态恢复

PlayerFullBodyActionController capture/restore
  负责 FullBody 动作状态恢复

CharacterRuntimeBlackboard snapshot/restore
  负责 typed facts 恢复

CharacterSimulationSnapshotComparer
  负责 strict/presentation 差异分类
```

不要新增每个玩法模块自己的旁路回滚入口。模块可以实现 capture/restore 和 compare 贡献，但 replay 必须仍通过同一条 FullBody tick 主线。

### 6. 回滚后追帧重放

NKGMoba 发现不一致后：

```text
CurrentFrame = serverFrame
RollBack(serverFrame, serverCmd)
CurrentFrame++
while CurrentFrame < CurrentArrivedFrame
  LSF_TickManually()
  CurrentFrame++
最后再执行当前帧 tick
```

这点是预测回滚的核心：恢复旧状态后，必须用输入历史重放回来，而不是只把当前位置改成服务端位置。

对应到当前项目：

```text
restore snapshot at tick N
read PredictionInputHistory from N+1 to current
FullBodyRollbackSimulation.Advance(...)
compare CharacterSimulationSnapshot
apply correction 或仅输出 F6/F8 诊断
```

## 可以借鉴的设计点

### 统一帧同步组件

NKGMoba 用 `LSF_Component` 管理：

```text
CurrentFrame
FrameCmdsToHandle
FrameCmdsToSend
PlayerInputCmdsBuffer
ServerCurrentFrame
CurrentArrivedFrame
AheadOfFrame
```

当前项目不要新增一个同名“大组件”，但可以借鉴它的职责切分：

```text
PredictionInputHistory
  管输入历史

PredictionSnapshotHistory
  管快照历史

LocalRollbackSynctestRunner / future ReconciliationRunner
  管恢复、重放、比较

Latency/Reconciliation simulator
  管服务端确认帧、预测错误和校正策略
```

### 命令和状态都是纯数据

NKGMoba 的 `ALSF_Cmd` 是可序列化纯数据，里面有：

```text
Frame
LockStepStateFrameSyncDataType
UnitId
PassingConsistencyCheck
```

当前项目未来 Fantasy DTO 也必须保持纯数据：

```text
tick
character stable id
input frame
authoritative snapshot
checksum 或 compare summary
```

不能同步：

```text
Transform
GameObject
MonoBehaviour
AnimancerState
Animator
AnimationClip
InputAction
```

### TickStart / Tick / TickEnd 三段式

NKGMoba 的组件 tick 分三段：

```text
TickStart
  初始化本帧记录容器

Tick
  推进逻辑

TickEnd
  收集快照、增量、同步命令
```

当前项目已有 tick phase，建议继续使用现有分层：

```text
ReadInput
UpdateInputBuffer
GameplayDecision
BuildMotion
ExecuteMotion
PresentationBridge
WriteSnapshotAndEvents
```

借鉴点是：快照写入必须在本帧所有权威逻辑结束后发生，避免记录半帧状态。

### 只回滚本地预测过的权威域

NKGMoba 只对本地玩家命令执行回滚流程，远程玩家命令直接处理。

当前项目要更精细：不是“本地玩家所有东西都 strict”，而是按权威域分类：

```text
position/yaw
  StrictGameplay

FullBody active state/action facts
  StrictGameplay

TurnBack profile-driven motion window
  StrictGameplay

MoveLoop 视觉 normalized time
  PresentationDrift

Action animation normalized time
  默认 PresentationDrift，除非业务声明为 LogicTimed
```

这与现有 `prediction-rollback-authority-scopes` 方向一致。

## 不应照搬的部分

### 不照搬 ET Room/Unit 架构

当前项目已经有自己的角色聚合点和 FullBody 主线。不要为了借鉴 NKGMoba 创建第二套：

```text
Room
Unit
LSF_Component
LSF_TickComponent
LSF_CmdDispatcherComponent
```

如果需要调度器，也应挂在现有 simulation/reconciliation 体系内。

### 不照搬服务端状态脏数据方式

NKGMoba 中一些模块默认服务端计算、本地直接判不一致，例如属性同步。

当前项目在 Fantasy 接入前已经有本地 synctest 和 FullBody replay。第一阶段仍应先保持：

```text
同输入本地重放必须确定
F6/F8 strict gameplay mismatch 必须可定位
表现漂移只诊断
```

不能因为未来服务端权威，就跳过本地 replay 收敛质量门。

### 不做“局部直接修正”替代 replay

NKGMoba 的移动回滚会直接恢复位置，然后重新导航。当前项目可以有最终 correction apply，但调试和验收必须保留：

```text
restore old snapshot
replay input history
compare snapshot
输出 first mismatch
```

如果只在网络回包里直接改位置，会绕过当前 FullBody/Locomotion/motion executor 主线，后续动作、输入消费、状态机和表现事件都会分裂。

### 不把行为树/Buff 做法原样套给动画

NKGMoba 的行为树黑板和 Buff 适合全量快照加 delta。当前项目动画层要先区分：

```text
动画事实是否影响 gameplay
动画进度是否驱动 profile motion
只是视觉进度还是逻辑窗口
```

只有被声明为 `LogicTimed` 或 `ProfileDriven` 的动画事实才进入 strict。纯表现 drift 进入诊断，不阻塞 replay。

## 映射到当前项目的推荐结构

### 当前项目等价模块

| NKGMoba 概念 | 当前项目建议对应 |
| --- | --- |
| `LSF_Component.CurrentFrame` | `SimulationTick` / runner 当前 tick |
| `PlayerInputCmdsBuffer` | `PredictionInputHistory` |
| `FrameCmdsToSend` | 未来 Fantasy input batch queue |
| `FrameCmdsToHandle` | future confirmed input / authoritative snapshot queue |
| `ALSF_Cmd` | `PredictionInputFrame` / authoritative snapshot DTO |
| `ILSF_TickHandler` | capture/restore/compare contributor 或现有 adapter |
| `OnLSF_CheckConsistency` | `CharacterSimulationSnapshotComparer` + authority scope |
| `OnLSF_RollBackTick` | `LoadSnapshot` + `FullBodyRollbackSimulation.Advance` |
| `HistroyMoveStates` | `PredictionSnapshotHistory` 中的 locomotion restore state |
| `FrameSnaps_DeltaOnly` | runtime facts delta 或未来网络压缩层 |

### 当前项目主线

后续实现应保持：

```text
本地输入
  -> PredictionInputFrame
  -> PredictionInputHistory
  -> InputRequestBuffer
  -> PlayerFullBodyActionController
  -> FullBodyFramePipeline
  -> PlayerLocomotionController / MotionExecutor
  -> CharacterRuntimeBlackboard
  -> CharacterSimulationSnapshot
  -> PredictionSnapshotHistory
```

回滚时：

```text
权威回包或测试触发
  -> 找到 restore tick
  -> 从 PredictionSnapshotHistory 恢复 CharacterSimulationSnapshot
  -> 读取 PredictionInputHistory
  -> FullBodyRollbackSimulation.Advance 重放
  -> CharacterSimulationSnapshotComparer 比较
  -> 输出 strict differences / presentationDifferences
```

## 建议第一阶段借鉴范围

第一阶段只借鉴移动预测回滚闭环，不碰完整技能、Buff、行为树或真实网络。

建议目标：

```text
在本地模拟服务端确认帧
制造 position/yaw 或输入预测差异
从历史 tick 恢复
重放 Move/Run/Dodge
确认 strict mismatch 能失败
确认收敛时 F6/F8 能 PASS
确认 presentation drift 不导致 strict fail
```

第一阶段必须明确不做：

```text
不接 Fantasy 真实 transport
不改 proto
不新增服务端输入队列
不新增第二套角色控制器
不绕过 FullBodyFramePipeline
不直接用网络回包写 Transform
不删除现有 log
```

## 后续分阶段借鉴

### 阶段 A：移动和动作输入确认

目标：

```text
Move
Run held
Dodge pressed
基础 Action pressed
```

重点：

```text
输入事实记录
请求消费恢复
状态机恢复
运动根恢复
strict/presentation 比较分类
```

### 阶段 B：本地延迟和确认帧模拟

目标：

```text
模拟服务端晚 N tick 确认输入
模拟预测输入错误
回滚到 first incorrect tick
重放到 current tick
```

重点：

```text
PredictionCorrection
ReplayNondeterminism
两类日志必须区分
```

### 阶段 C：Fantasy DTO 边界

目标：

```text
定义 input frame batch
定义 authoritative snapshot 或 confirmed input
定义稳定 id 和纯数据 mapper
```

重点：

```text
DTO 不引用 Unity 对象
协议生成单独 OpenSpec 审批
客户端和服务端都能自动测试 round-trip
```

### 阶段 D：技能、Buff、Hitbox

目标：

```text
把技能窗口、hitbox/hurtbox、伤害、硬直、资源、冷却变成 tick facts
```

重点：

```text
进入 gameplay 的事实必须可快照、可恢复、可比较
表现事件必须用 sequence 去重
```

## 测试和验证要求

真正实现时，每一步都必须有自动测试和手动验证。

自动测试建议：

```text
输入历史写入、覆盖、裁剪、区间读取
快照历史写入、恢复、裁剪
FullBody restore 后下一 tick 收敛
Move/Run/Dodge 同输入 replay 收敛
预测错误触发 rollback + replay
strict mismatch 导致失败
presentation drift 不导致失败
first mismatch 记录最早分叉 tick
DTO 纯数据 round-trip
```

手动验证建议：

```text
Sandbox Play Mode
F6 短窗口 synctest
F8 soak
WASD/Run/Dodge/Action 手感不回退
Console 搜索 strict differences 和 presentationDifferences
确认 hidden replay 不永久污染角色、visual root 或相机
```

当前已有验证文档：

```text
docs/agents/action-fighting-prediction-rollback-guide.md
docs/agents/local-rollback-soak-verification.md
```

## 借鉴原则

后续做 OpenSpec 或实现时，按这些原则判断是否偏航：

```text
借鉴同步编排，不借鉴项目架构
借鉴组件级职责，不创建分裂控制器
借鉴输入缓冲，不保存动作结果
借鉴状态/增量校验，不跳过本地 replay 质量门
借鉴追帧重放，不用直接修正替代回滚
借鉴服务端权威，不让网络回包绕过 tick 主线
```

最小正确形态是：

```text
输入可回放
状态可恢复
重放走正式 FullBody 主线
差异按权威域分类
测试能复现 first mismatch
手动验证能确认画面不被 hidden replay 污染
```
