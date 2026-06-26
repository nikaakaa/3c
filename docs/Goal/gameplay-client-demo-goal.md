# Gameplay 客户端动作 Demo 目标文档

## 1. 目标定义

本项目是求职向 Gameplay 客户端动作 demo。

它不是轻量玩具切片，也不是只做一个攻击按钮的演示。最终要深入展示：

- 全套第三人称 locomotion。
- 深入动作状态机和完整连招树。
- Timeline 驱动的动作窗口、表现、位移和 foot phase。
- 角色 runtime pipeline。
- 服务端权威下的预测、插值、校正、combat rewind。
- 能被面试官看见、玩到、问代码链路的 Debug 工具。

它也不是完整商业 PvPvE 产品、MMO 或运营型网游。PvPvE 是业务压力来源，不是第一目标。目标口径是：

```text
Network-aware Third Person Action Combat Demo
```

要证明的是：

```text
我能做一个动作游戏客户端的核心系统，
动作数据来自干净的节点/Timeline authoring，
本地手感、动作深度和网络权威可以同时成立。
```

## 2. 当前情况

当前已归档为 current spec 的 Taco authoring 能力：

- `openspec/specs/taco-componentized-node-authoring/spec.md`
- `openspec/specs/taco-graph-core/spec.md`
- `openspec/specs/taco-input-action-node-authoring/spec.md`
- `openspec/specs/taco-runnable-timeline-node/spec.md`
- `openspec/specs/taco-sm-node-authoring/spec.md`

当前 active changes：

- `openspec/changes/add-taco-transition-rule-graph-authoring/`
- `openspec/changes/add-character-pipeline-runtime-entry/`
- `openspec/changes/add-tengine-hotupdate-foundation/`

当前代码状态：

- `3cDemo/Client/3C_Client/Assets/Scripts/Taco/` 是当前最重的 authoring 主线。
- `3cDemo/Client/3C_Client/Assets/Scripts/Camera/` 已有第三人称相机模型、solver、runtime adapter。
- `3cDemo/Client/3C_Client/Assets/Scripts/Rendering/` 已有动作表现相关后处理和 VFX 模块。
- `3cDemo/Client/3C_Client/Assets/Scripts/Character/Pipeline/` 当前只有目录/meta，runtime 还没真正落地。
- `3cDemo/Client/3C_Client/Assets/Scripts/Charactor/` 是旧拼写和旧路径，不作为新 runtime 主线。
- `3cDemo/Server/` 当前是 Fantasy skeleton，不恢复旧 FrameSyncAuthority。
- 工作树里有大量用户删除和迁移，不回退。

结论：

```text
现在不是“内容少所以只做小 demo”。
现在是 authoring 地基刚收口，runtime 和动作深度还没落地。
```

## 3. 总体业务逻辑

总链路：

```text
Taco Authoring
-> CharacterPipeline
-> Locomotion / Action / Combo / Timeline
-> Motion / Animation / Presentation
-> Local Prediction
-> Server Authority
-> Correction / Interpolation / Rewind
```

### 3.1 Authoring 业务链路

目标 authoring 数据流：

```text
RootTree
-> StateMachineNode
-> StateMachineGraph
-> StateNode
-> StateBehaviorSubTree / SubTree
-> TimelineNode
-> Timeline asset
-> GameplayWindow facts
```

职责：

- `RootTree`：角色行为图入口。
- `StateMachineNode`：父级行为图进入状态机图的入口。
- `StateMachineGraph`：状态拓扑、Transition 调度、AnyState/Enter/Exit。
- `StateNode`：普通状态和状态行为边界。
- `StateBehaviorSubTree`：状态生命周期，表达 `OnEnter`、`RootNode`、`OnExit`。
- `TimelineNode`：Graph 驱动 Timeline 的可执行节点。
- `Timeline asset`：编辑动画、窗口、位移、foot phase、表现 cue。
- `GameplayWindow facts`：运行时消费的动作事实。

### 3.2 动作数据业务链路

最终动作数据不是旧 SO/config，而是节点和 Timeline 共同表达：

```text
State / Action Node
-> TimelineNode
-> Timeline Tracks
-> GameplayWindow Facts
-> CharacterPipeline Frame
-> Motion / Animation / Hit / Cue Output
```

Timeline 负责：

- 动画片段和混合段。
- AttackWindow。
- Hurtbox / Hitbox 激活窗口。
- CancelWindow。
- IFrame / Parry / Armor。
- Motion curve / root motion sample / warp marker。
- FootPhase。
- VFX / SFX / Camera cue。
- Hit stop / screen effect cue。

Timeline 不负责：

- 最终命中成立。
- 最终伤害。
- PvP 目标归属。
- 网络权威状态。

这些由 gameplay solver 或服务端裁决。

### 3.3 Locomotion 业务链路

Locomotion 最终要做完整，不是砍掉。

目标能力：

- Idle。
- Walk / Run。
- MoveStart。
- MoveLoop。
- MoveStop。
- Turn / TurnBack。
- Strafe / Lock-on locomotion。
- Dodge / step / roll 和 locomotion 的衔接。
- Foot phase 对齐。
- 动画驱动位移和输入驱动位移的权威边界。
- Root motion sample / motion curve / warp marker。
- 网络预测下的本地移动和服务端校正。

业务链路：

```text
Input Move/Aim
-> Locomotion StateMachineGraph
-> StateNode
-> TimelineNode 或 Motion Module
-> MotionProposal
-> CharacterMotionStage
-> PresentationStage
-> Prediction/Correction
```

关键取舍：

- 基础移动不能散落在多个 MonoBehaviour 里。
- 动画表现可以复杂，但最终位移必须进入统一 `MotionStage`。
- FootPhase 跟随动画和 Timeline 编辑，不恢复单独 footphase profile 数据源。
- Locomotion 可以是完整动作系统的一部分，但不能恢复旧 `LocomotionSO` 分裂配置。

### 3.4 连招和 Action 业务链路

完整连招树要做，而且是动作 demo 的核心深度。

目标能力：

- Light combo。
- Heavy / charged attack。
- Dodge cancel。
- Hit reaction。
- Parry / guard / armor。
- Branch / cancel / buffer。
- 输入缓冲。
- 连招段切换。
- 空挥、命中、被打断的不同分支。
- 动作资源消耗。
- 动作窗口 debug。

业务链路：

```text
InputAction
-> TransitionRuleGraph
-> StateMachineGraph
-> StateNode / Action State
-> TimelineNode
-> Window Facts
-> Combo Branch Decision
-> Motion / Hit / Presentation Output
```

关键取舍：

- 连招树不是旧 `ActionSO` catalog。
- 连招分支要能在节点/Timeline 体系里表达。
- Transition 条件下钻到 `TransitionRuleGraph`，不要让状态机图本层堆满 Bool 计算节点。
- 输入缓冲、cancel window、hit confirm 应该是可调试的 facts，不是写死在动画回调里。

### 3.5 Runtime 业务链路

目标 runtime：

```text
CharacterPipelineRunner
-> CharacterPipelineHost
-> CharacterPipeline
-> InputStage
-> GraphStage
-> TimelineStage
-> MotionStage
-> PresentationStage
-> NetworkStage
-> FrameEndCleanup
```

职责：

- `CharacterPipelineRunner`：统一 tick 源。
- `CharacterPipelineHost`：Unity 装配点，只收集引用、创建、注册、释放。
- `CharacterPipeline`：纯 C# 角色动作管线。
- `InputStage`：读取输入和网络 command。
- `GraphStage`：tick Taco RootTree / StateMachineGraph。
- `TimelineStage`：外部驱动 TimelinePlayer，收集窗口和 cue。
- `MotionStage`：统一位移出口。
- `PresentationStage`：动画、VFX、SFX、Camera、hit stop、后处理。
- `NetworkStage`：预测、快照、校正、远端插值接入点。
- `FrameEndCleanup`：清理一次性 facts。

`BaseGraph.User` 应该拿到正式上下文：

- `ITimelinePlayerProvider`
- `IInputActionValueSource`
- gameplay facts / tags / resources
- network tick / command context

## 4. 网络目标

网络不是“最后随便接一下”，而是动作 demo 的深度之一。

但网络深度不等于 MMO 后端深度。要做的是动作游戏客户端相关网络：

- client prediction。
- server reconciliation。
- snapshot interpolation。
- combat rewind。
- authority split。
- latency debug。
- action/window 同步。
- 命中裁决。
- 本地表现和服务器确认解耦。

### 4.1 同步边界

```text
本地玩家：预测移动、闪避、攻击启动、动画、特效、镜头
远端玩家：服务器快照 + 插值
PvP 命中：服务器权威 + combat rewind
PvE/目标点：服务器权威
校正：客户端平滑修正
```

### 4.2 关键数据

```text
ClientCommand
InputSequence
ServerSnapshot
ConfirmedEvent
Correction
ActionState
GameplayWindow
CombatHistoryFrame
ActorSnapshot
RemoteInterpolationBuffer
```

### 4.3 为什么不用其它方案

不用纯服务器权威：

- 动作手感会被 RTT 卡住。
- Gameplay 客户端 demo 会显得迟钝。

不用全局帧同步：

- Unity 3D、AI、物理、动画窗口、Timeline、PvE 目标很难全确定。
- 一个客户端卡顿会影响所有人。

不用完整世界 rollback：

- 完整 rollback 适合小规模强确定性格斗。
- 本项目有 Timeline、PvE、目标点、表现事件和 Unity 物理，回滚成本过高。

不用客户端权威：

- PvP 和目标争夺不能信客户端。
- 命中、伤害、目标归属必须权威裁决。

当前选择：

```text
Server-authoritative result
+ client prediction
+ snapshot interpolation
+ combat rewind
+ correction smoothing
```

## 5. 逻辑取舍

### 5.1 为什么仍然不是完整 PvPvE 产品

要深入做动作和网络，但不等于做完整商业 PvPvE。

不优先做：

- 账号。
- 匹配。
- 赛季。
- 大背包。
- 长线装备池。
- 大地图内容量。
- 商业反作弊。
- 完整断线重连。

会做的是 PvPvE 的关键业务压力：

```text
玩家和玩家会互相影响
玩家和 PvE 目标争夺同一局内资源
服务端裁决动作结果和目标结果
延迟下仍然保持本地手感
```

### 5.2 为什么先做 TransitionRuleGraph

当前最靠近 authoring 的缺口是 Transition 条件。

如果不先收口：

- 状态机本层会混入条件计算节点。
- 连招条件会越堆越乱。
- Locomotion 和 Action 都会重复造条件表达。
- 网络 facts / tags 接入时没有正式读取点。

先做规则图，可以为后续 locomotion、连招、网络状态都提供统一判断入口。

### 5.3 为什么 CharacterPipeline 是下一步主线

没有 `CharacterPipeline`，动作 demo 会变成：

- 输入自己 tick。
- Timeline 自己 tick。
- 状态机自己 tick。
- Motion 自己改 Transform。
- 网络校正到处插。

`CharacterPipeline` 的意义是给动作深度一个正式运行时容器。

### 5.4 为什么 TEngine 是底座但不能抢主线

TEngine 提供客户端工程完整度：

- HybridCLR。
- YooAsset。
- UniTask。
- Procedure。
- Pool / Event。

但它不能替代：

- Taco StateMachineGraph。
- CharacterPipeline。
- Fantasy 网络边界。
- Timeline 动作数据。

TEngine 做启动和资源底座，Gameplay 主线仍然是 Taco + CharacterPipeline。

## 6. 路径规划

### 6.1 现在先看清的三条 active change

当前 active changes：

```text
openspec/changes/add-taco-transition-rule-graph-authoring/
openspec/changes/add-character-pipeline-runtime-entry/
openspec/changes/add-tengine-hotupdate-foundation/
```

它们不是互相替代关系：

```text
TransitionRuleGraph = 状态/连招/locomotion 条件表达
CharacterPipeline = 动作 runtime 主入口
TEngine = 客户端工程底座
```

### 6.2 第一阶段：TransitionRuleGraph

路径：

```text
openspec/changes/add-taco-transition-rule-graph-authoring/
3cDemo/Client/3C_Client/Assets/Scripts/Taco/TreeDesigner/Scripts/
```

完成目标：

- Transition edge 保存规则图引用和 priority。
- 删除旧 BoolPort 条件路径。
- `AnyState` 必须有规则图。
- `TransitionRuleGraph` 只能创建纯值、输入、谓词、逻辑、结果节点。
- Runtime 通过规则图求值 Transition。

### 6.3 第二阶段：CharacterPipeline runtime

路径：

```text
openspec/changes/add-character-pipeline-runtime-entry/
3cDemo/Client/3C_Client/Assets/Scripts/Character/Pipeline/
```

不走：

```text
3cDemo/Client/3C_Client/Assets/Scripts/Charactor/
```

完成目标：

- `CharacterPipelineRunner`
- `CharacterPipelineHost`
- `CharacterPipeline`
- `CharacterPipelineGraphContext`
- `CharacterInputStage`
- `CharacterGraphStage`
- `CharacterMotionStage`
- `CharacterPresentationStage`
- TimelinePlayer 外部 tick 权威

### 6.4 第三阶段：Locomotion 深度

路径：

```text
3cDemo/Client/3C_Client/Assets/Scripts/Character/Locomotion/
3cDemo/Client/3C_Client/Assets/Scripts/Character/Pipeline/Motion/
3cDemo/Client/3C_Client/Assets/Scripts/Taco/
```

目标：

- Idle / Start / Loop / Stop。
- Run / Walk / Strafe。
- Turn / TurnBack。
- Lock-on locomotion。
- FootPhase 轨道。
- Motion curve / root motion sample。
- 预测和校正下的 locomotion 表现。

### 6.5 第四阶段：Action 和完整连招树

路径：

```text
3cDemo/Client/3C_Client/Assets/Scripts/Character/Action/
3cDemo/Client/3C_Client/Assets/Scripts/Character/Combat/
3cDemo/Client/3C_Client/Assets/Scripts/Taco/Timeline/
```

目标：

- Light combo。
- Heavy / charged attack。
- Dodge cancel。
- Hit confirm。
- Input buffer。
- Cancel window。
- Parry / guard / armor。
- Hit reaction。
- Combo branch debug。

### 6.6 第五阶段：表现和可视化

路径：

```text
3cDemo/Client/3C_Client/Assets/Scripts/Camera/
3cDemo/Client/3C_Client/Assets/Scripts/Rendering/
3cDemo/Client/3C_Client/Assets/Scripts/Character/Presentation/
```

目标：

- Camera cue。
- Hit stop。
- Screen effect。
- VFX/SFX cue。
- Debug overlay。
- Timeline window visualization。
- State / Transition / Combo / Network debug。

### 6.7 第六阶段：网络深入

路径：

```text
3cDemo/Client/3C_Client/Assets/Scripts/Network/Fantasy/
3cDemo/Server/
3cDemo/Tools/FrameSyncLiveSmoke/
```

目标：

- ClientCommand。
- ServerSnapshot。
- Prediction buffer。
- Reconciliation。
- Remote interpolation buffer。
- Combat history。
- Rewind hit validation。
- Correction smoothing。
- Latency/debug overlay。

### 6.8 TEngine 底座并行推进

路径：

```text
openspec/changes/add-tengine-hotupdate-foundation/
3cDemo/Client/3C_Client/Packages/com.alex.tengine
3cDemo/Client/3C_Client/Packages/UniTask
3cDemo/Client/3C_Client/Packages/YooAsset
3cDemo/Client/3C_Client/Assets/Scripts/Bootstrap
3cDemo/Client/3C_Client/Assets/Scripts/HotUpdate
```

约束：

- TEngine Procedure 只负责启动和资源。
- 不创建第二套 gameplay tick。
- 不导入 TEngine 示例 GameLogic。
- 不导入 TEngine 示例网络。
- 不用 TEngine FSM 替代 Taco 状态机。

## 7. 不恢复清单

这些不是“以后永远不做对应功能”，而是不恢复旧数据源和旧路径：

- 旧 `LocomotionSO`。
- 旧 `ActionSO`。
- 旧 bodyclaim policy SO。
- 旧 footphase profile SO。
- 旧 AnimationPresentationPolicy。
- 旧 Workbench。
- 旧 `Charactor` runtime 主线。
- 旧 FrameSyncAuthority。
- TEngine 示例业务。
- BBB 代码状态机主线。

对应功能仍然要做，但走新主线：

```text
Locomotion -> Taco/Timeline/CharacterPipeline
Action/Combo -> Taco/Timeline/CharacterPipeline
FootPhase -> Timeline track
Body claim / action authority -> gameplay facts / pipeline arbitration
Network -> Fantasy + prediction/interpolation/rewind
```

## 8. 判断规则

每次新增能力前先问：

```text
它是否让动作 demo 更深入？
它是否服务 locomotion / combo / network / presentation 中的一个核心展示？
它是否沿 Taco authoring -> CharacterPipeline -> Presentation/Network 主线？
它是否引入旧 SO/config、第二套状态机、第二套端口或第二套 tick？
它能不能在 Unity 里被玩到、看到、调试到、讲清代码链路？
```

如果是动作 demo 核心能力，就规划进去；如果只是旧路径、分裂路径或商业产品外围，就砍掉。
