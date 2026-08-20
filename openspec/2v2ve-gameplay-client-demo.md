# 2v2vE Gameplay 客户端技术演示需求

## 文档地位

本文记录项目稳定的业务展示目标，用于约束后续 proposal、实现和验收范围。它不是 current spec，也不表示尚未实现的能力已经存在。任何新增命中求解、跨角色结果、Bot、怪物或多人表现能力，仍须通过正式 OpenSpec change 设计和实施。

## 作品定位

本项目是 Gameplay 客户端程序求职 Demo。2v2vE 是用于施加真实业务压力的演示场景，不是要制作一套完整 PvPvE 产品。

演示首先证明三件事：

1. 第三人称动作的动画表现稳定、连续且能够解释。
2. 本地输入、移动、攻击、闪避、格挡、打断与动作交接具有可靠手感。
3. 多角色参与同一战斗事件时，命中对象、格挡结果、伤害、硬直、生命值与死亡结算正确，并能在两个客户端保持一致。

网络、Bot 和中立怪用于验证上述 Gameplay 能力在多人压力下仍成立，不取代 Gameplay 客户端成为主要展示内容。

## 演示场景

固定角色构成为：

```text
Team A
  - 真人 Client A
  - Ally Bot A

Team B
  - 真人 Client B
  - Ally Bot B

Environment
  - 中立怪 E
```

- 四名玩家角色必须使用同一份角色 Authoring、Program、Motion、Combat 和 Presentation 管线。
- Bot 只能替换 Input Source 或高层行为意图，不能拥有第二套移动、动作、伤害或动画逻辑。
- 中立怪是不属于 Team A 或 Team B 的正式 Actor。其 AI 复杂度不是展示重点，但其移动、攻击、受击、伤害与死亡必须进入同一 Session、Result 和 Presentation 边界。
- 技术演示只强制启动两名真人客户端，不要求四个真人客户端。
- 角色、美术、地图和怪物种类的数量不是验收重点；允许复用同一角色配置并使用最小演示场地。

### 当前实现边界

项目已经安装通用BTSMTL AI authoring、portable AI Program、独立AI State、committed Actor Observation与Local Float32 Control Source事务链。AI只产生与玩家相同类型的`CharacterSimulationInput`，不会直接执行Action、Timeline、Motion、伤害或动画。

统一Agent authoring合同已经通过`CharacterController`/`AIController` domain覆盖AI Definition、Graph、Blackboard、Perception和Intent。具体schema与工具生命周期以current specs和`openspec/project.md`为准，不在本文固定版本。Standalone中的Corin训练敌人已经具备正式AI Definition、RootTree、Perception和行为authoring：它读取同一Session的committed `LocalActor`观察，只输出MoveAxis、ActionTarget与Attack request，并继续复用Corin角色Program、WorldSolver和Presentation。Rig v4、Foot Analysis与Character Program前置产物已经重建；当前仍需由`add-corin-training-ai-demo`重新发布并验证AI Program与Document v3，在这些任务完成前不将该配置描述为已闭合Local Float32 AI产品。本文中的Ally Bot、中立怪AI、Team/Faction感知和Authority AI仍未完成；后续场景资产必须从正式AI Definition编译并进入同一Session，不能用MonoBehaviour Bot、Scene查询或客户端本地推断代替。

## 核心战斗范围

玩家角色的最小可演示能力包括：

- 第三人称移动、朝向和转身。
- 攻击与基础连段。
- 闪避及其有效窗口。
- 持续普通格挡。
- 受击、普通打断、硬直、生命值变化和死亡。
- 动作 Timeline 中的动画、窗口、位移与表现事实。

不要求实现《荣耀战魂》式三方向防御。普通格挡采用角色正面有效角度：

- 防御者处于有效格挡状态，且攻击来自其正面有效范围时，结果为 `Blocked`。
- 攻击来自背后或格挡条件不成立时，正常进入命中和伤害结算。
- 精确角度、体力消耗和数值公式属于后续可调配置，不在本文固定。

## 队友实时互动

必须展示的合作行为是“替队友格挡攻击”。它不是独立支援技能，而是普通空间关系与普通格挡规则自然组合出的结果：

1. 敌方攻击指向或将命中一名玩家。
2. 其队友进入攻击路径，并使用普通格挡。
3. 正式命中求解首先确认实际接触和有效防御者。
4. 攻击结果变为拦截者的 `Blocked`，被保护的队友不受到该次伤害。
5. 攻击者、拦截者、被保护者和两个客户端观察到一致结果与表现。

系统不得为这一行为新增 `ProtectTeammate` 专用状态、节点、网络消息或旁路伤害规则。它必须复用普通攻击、格挡窗口、空间命中、GameplayResult 和 Presentation 链路。

其它救援行为只使用正常战斗规则。例如，玩家正常命中正在攻击队友的敌人后，可按普通受击规则打断敌人；不需要额外的“队友救援打断”能力。

## 伤害正确性

一次攻击结果必须能够回答：

- 谁发起攻击。
- 攻击属于哪个 ActionInstance 和 Hit Window。
- 哪个 Actor 实际成为目标。
- 目标与来源的队伍关系是什么。
- 结果是命中、格挡、闪避、打断还是无效。
- 是否以及何时修改 Health。
- 是否产生硬直、击退、死亡和对应表现事实。

同一有效命中不得因为预测、权威确认、重放、可靠事件重发或多个表现消费者而重复扣血。被格挡的攻击不得先扣血再通过表现层补偿。Presentation 不得反向决定命中、格挡或伤害。

正式业务链应保持为：

```text
Character Input / Bot Intent
  -> Program Action、StateMachine 与 Timeline
  -> ActionInstance 与 Hit/Guard/Dodge Window
  -> Session 内的跨 Actor 命中求解
  -> 唯一 GameplayResult
  -> GameplayEffect / Attribute 的 Health 与状态变化
  -> committed Presentation 与 Network Model 输出
```

## 动画表现与手感

本地角色、远端玩家角色、Bot 和中立怪必须通过同一动画 Producer、Projection、Playback Lifecycle 与 Animancer 执行边界表现动作。

演示重点包括：

- 移动循环动画连续，不随逻辑 Tick 呈阶梯推进。
- 攻击、连段、闪避、格挡、受击、打断和死亡能够在正确时间进入与退出。
- Root motion 与输入移动交接时不出现明显方向跳变或一帧残留姿势。
- 动画切换不闪烁、不丢失、不因网络确认重复开始，也不在动作结束后卡住。
- 逻辑 Tick 决定 Gameplay 事实，表现帧使用真实帧时间推进动画，并对逻辑 Body sample 插值。
- 纠偏修改逻辑位置时，视觉层负责平滑展示，但不得隐藏持续的模拟错误。
- 两个客户端应观察到相同的动作身份和战斗结果；网络不逐帧同步 AnimationClip 或骨骼姿势。

## 网络压力口径

2v2vE 场景至少由两个独立 Unity 客户端运行。每个客户端必须同时表现：

- 自己控制的本地预测 Actor。
- 另一名真人玩家的 Remote Presentation Actor。
- 两名玩家角色 Bot。
- 中立怪 Actor。

网络模型可以使用隔离 Scene 和独立 Composition 做技术对比，但不得改变角色业务语义。Local、ServerAuthoritative Unity、ServerAuthoritative DotRecast 或未来 Deterministic Rollback 可以有不同 Source、Pipeline、Solver 与纠偏方式；它们必须消费同一 Authoring 语义，并产出相同种类的 Action、GameplayResult、Attribute 和 Presentation 结果。

该演示不以网络协议、服务器数量或框架代码量为卖点。网络验收只回答：高实时战斗互动是否能够持续运行、及时反馈、正确确认，并在纠偏时保持可接受的视觉连续性。

## 最小演示流程

一次完整演示应能够连续展示：

1. Client A 与 Client B 分属不同队伍并进入同一战斗场地。
2. 四名玩家角色与中立怪都由正式 Actor/Session 管线运行和显示。
3. 两名真人玩家分别移动、转身、攻击、连段、闪避和格挡。
4. 普通攻击能够命中玩家、Bot 或中立怪，正确扣除生命并触发受击表现。
5. 普通命中能够按配置打断正在执行的动作。
6. 一名真人玩家能够移动到队友攻击路径前并成功替队友格挡。
7. 两个客户端观察到相同目标、相同格挡或命中结果、相同 Health 变化和一致的关键动画状态。
8. 持续操作期间不出现重复伤害、远端角色长期停住、循环动画丢失、攻击动画卡死或明显的逐 Tick 视觉顿挫。

## 不做的内容

本技术演示不要求：

- 完整胜负、积分、占点、资源搬运或经济循环。
- 匹配、账号、背包、养成、赛季或商业化系统。
- 大地图、多职业、大量技能或大量怪物种类。
- 完整怪物 AI、复杂仇恨系统或完整关卡策划。
- 四名真人客户端同时参与。
- 完整反作弊、断线重连、观战或正式线上运营能力。
- 为展示某个场景而复制 Character Runtime、Combat Runtime、Animation Runtime 或 Network Runtime。

## 完成标准

该业务目标只有在以下条件同时成立时才算闭环：

- 2v2vE 中每个 Actor 都走同一条正式 Gameplay Session 主线，没有角色脚本旁路。
- 本地操作具备连续、可调的动作手感，表现层与逻辑 Tick 正式分离。
- 两个客户端持续看到完整 roster，并能观察彼此及 AI Actor 的连续运动和动作。
- 队友空间拦截加普通格挡能够稳定保护原目标。
- 命中、格挡、打断、伤害、Health 与死亡由唯一 GameplayResult/Effect/Attribute 链处理。
- 同一战斗事件在两个客户端具有一致身份，可通过 diagnostics 追溯到输入、ActionInstance、Window、Result、Attribute 和 Presentation。
- 没有为了 Demo 新增 fallback、兼容路径、临时桥接、双写入口或第二套玩法逻辑。
