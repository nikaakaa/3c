# Design: Corin Action 层级、后摇 Timeline 与 ownership 闭环

> 历史状态：本设计描述后继 change 之前的中间形态。当前 Corin 不包含 `RushAttack` 或 Attack5→Attack1 循环，窗口已收敛为 `ComboAccept`、`RecoveryEarly`、`RecoveryLate`、`RecoveryOpen`，ownership 已收敛为唯一 `HasActionLocomotionOwnership`。请勿据此恢复旧 Graph 或 Blackboard 数据。

## 目标作者视图

```text
Corin RootTree
└─ Runtime Loop
   └─ Gameplay Parallel
      ├─ Action StateMachine
      │  ├─ None
      │  ├─ Attack
      │  │  └─ Attack Combo StateMachine
      │  │     ├─ Attack1 → Attack1 Timeline
      │  │     ├─ Attack2 → Attack2 Timeline
      │  │     ├─ Attack3 → Attack3 Timeline
      │  │     ├─ Attack4 → Attack4 Timeline
      │  │     ├─ Attack5 → Attack5 Timeline
      │  │     └─ RushAttack → RushAttack Timeline
      │  └─ Dodge
      │     └─ Dodge Direction StateMachine
      │        ├─ DodgeBack
      │        └─ DodgeForward
      └─ Locomotion StateMachine
         ├─ Idle / Walk / Run / MovingTurn
         └─ ActionOverride
```

作者进入 Attack1..5 或 RushAttack Timeline 后即可看到完整的 Animation、Motion、Cue、Hit TreeClip、ComboCancel TreeClip 和 MoveCancel TreeClip。Dodge Timeline 中可以直接调整通用后摇取消窗口。动作分组、ownership、取消优先级和退出生命周期已经配置好，不要求作者为每次窗口调整修改 RootTree 或另建 SubTree asset。

## 当前问题的真实链路

```text
Attack1 main clip 0..49
Attack1 Timeline 0..80
main clip ExtraPolationMode=Hold

Locomotion 未收到 Attack ownership
        ↓
Attack Timeline 与 Locomotion Timeline 同时推进 Base producer
        ↓
主攻击动画末帧冻结，隐藏 RunEnd 继续推进
        ↓
Attack 完成并释放后，RunEnd 中间姿态重新可见
```

这不是 RunEnd edge 抢占 Attack State，也不是 Animancer 错误选择高 priority producer。根因是 Attack 没有进入 locomotion ownership 协议，同时 Attack Timeline 缺少真正的 End 动画段。

## 决策一：外层 Action 只表达动作大类

外层 Action StateMachine 固定为：

```text
None
Attack
Dodge
```

`Attack1..5`、`RushAttack` 与 `DodgeBack/DodgeForward` 都是各自动作大类的 leaf state。Leaf state 唯一拥有：

- Input request consume。
- ActionProfile 与 Action Context 创建。
- Timeline playback。
- Hit/Cancel/IFrame TreeClip。
- Complete、Cancel、Interrupt、Abort terminal lifecycle。

外层 `Attack`、`Dodge` 只拥有动作组级 locomotion ownership 和内联 StateMachineNode，不创建第二个 ActionInstance，也不提交重复 terminal transition。

Dodge 的命令接受与方向选择必须分开：外层 `None -> Dodge` 查询一次 Dodge request并建立动作组 ownership，内层 Entry 只以 MoveAxis threshold 在 DodgeBack/DodgeForward 之间完备二选一。内层不得重复查询瞬时 request；否则外层已进入 Dodge、内层却没有初始 state 时，通用 StateMachine 会返回 Failure并让 ownership 停留在 ActionOverride。目标 direction leaf仍负责唯一消费该 request和创建 Action Context。

### Tradeoff

- 好处：Action 图只展示动作大类，方向和连段细节均可下钻；以后增加 Attack3 或 SideDodge 不扩大外层图。
- 代价：作者需要多下一层查看具体 Dodge，但这正是层级状态机承担的组织职责。
- 不采用继续平铺：平铺会让外层同时承担动作大类选择、方向选择、leaf lifecycle 和 locomotion ownership，新增动作时持续扩张。

## 决策二：逻辑 ownership 先于 Locomotion 选择

使用两个 root-owned、内部显示的 pipeline blackboard Bool declaration：

```text
HasActionLocomotionOwnership
ResumeLocomotionThroughRunEnd
```

动作组行为：

| 动作组 | OnEnter | 活跃期间 | OnExit |
|---|---|---|---|
| Attack | `Ownership=true`, `ResumeRunEnd=false` | nested combo 运行 | `Ownership=false` |
| Dodge | `Ownership=true`, `ResumeRunEnd=true` | nested direction 运行 | `Ownership=false` |

`ResumeLocomotionThroughRunEnd` 保存最近一次 ownership 的返回策略，直到下一次动作组 OnEnter 覆盖。它不是网络策略，也不是动画 priority；它只回答 Locomotion 在 ownership 释放后无输入时进入 Idle 还是 RunEnd。

Gameplay Parallel 的 flow order 改为 Action 在前、Locomotion 在后：

```text
Action tick
  → 更新 ownership / leaf action / Timeline
Locomotion tick
  → 读取同 tick ownership
  → 选择 ActionOverride 或正常 locomotion state
Finalize
  → Base 只有一个 selected producer
```

### Tradeoff

- 好处：同一 tick 完成 ownership 交接，不需要动画层比较 priority，也不会让隐藏 Locomotion producer继续推进。
- 代价：RootTree 明确存在 Action-before-Locomotion 的业务执行顺序；该顺序必须保存在 edge flow order 和 spec 中，不能当成无序并行。
- 不采用动画仲裁：让表现层从两个 producer 中选 winner 会恢复已经删除的第二套业务优先级。
- 不采用 `IsAttacking OR IsDodging`：两个 subtype Bool 只能说明当前是否活跃，无法在清零后保留 Dodge 与 Attack 不同的返回策略，并会随着动作类型增加继续堆变量。

## 动画资源盘点

五段普通攻击的正式候选来自 `WithWeaponInplace`，均为 60fps、非循环：

| 段 | 主攻击 | 主段帧数 | 独立 End | End 帧数 |
|---|---|---:|---|---:|
| Attack1 | `Normal_01` | 49 | `Normal_01_End` | 119 |
| Attack2 | `Normal_02` | 48 | `Normal_02_End` | 125 |
| Attack3 | `Normal_03` | 81 | `Normal_03_End` | 125 |
| Attack4 | `Normal_04` | 89 | `Normal_04_End` | 193 |
| Attack5 | `Normal_05` | 125 | `Normal_05_End` | 87 |

另有 `Normal_03_Explode` 37 帧与 `Normal_05_B` 185 帧。文件名只能证明它们是特殊资源，不能证明其输入条件、能量条件或与普通五连的先后关系，因此本 change 不把它们猜测为默认 clip 或 transition。

最终 RushAttack 作者身份为：State `aa73abdb-f5a7-4bdb-a405-eae8baf7c987`，inline Timeline `48340869-ef14-4fc1-a38c-31c3acb2d9da`，AnimationTrack `46ada3b9-9aaa-4a5a-95e9-6110ddd49fe0`。规范化主段/End 动画 GUID 分别为 `64c2e8fcb3d40744f8728f513cfef052`、`d755c1356df159543a0c6d3b2230648f`；主段/End root-motion curve GUID 分别为 `4c670b5f9ef04014f81832e9292144c7`、`0e3261715be607a4592c8526d536528b`。

当前 `Corin_Pipeline_Attack1/2_Inplace` 与 `WithWeaponInplace` 具有相同的 3328 个 curve path，差异包含根节点初始 X/Z 归零；新增 Pipeline 资产必须沿用该规则，不能误用普通 `Inplace` 版本。

## 决策三：每段攻击在同一 Timeline 内包含主攻击与后摇

每个 AttackN AnimationTrack：

```text
Corin_Pipeline_AttackN_Inplace
        + 短重叠/ease
Corin_Pipeline_AttackN_End_Inplace
```

表现与 motion 来源分别固定为：

- Animation：`WithWeaponInplace/Corin_Attack_Normal_0N[_End]_WithWeaponInplace.anim`
- Motion：`WithWeaponRootmotion/Corin_Attack_Normal_0N[_End]_WithWeaponRootmotion.anim`

迁移时创建规范化 PipelineInplace 资产，保持 60fps、完整时长、非循环和现有根节点归零口径。主 clip 改为 `ExtraPolationMode=None`，避免 End clip 开始后主 clip 继续 Hold 并参与 mixer。Gameplay motion继续只由 Timeline MotionCurveClip 提交，不从 Animancer pose反推。

Combo 语义改为显式循环，并把连段取消与移动取消分开：

```text
Attack1Cancel active + Attack request → Attack2
Attack2Cancel active + Attack request → Attack3
Attack3Cancel active + Attack request → Attack4
Attack4Cancel active + Attack request → Attack5
Attack5Cancel active + Attack request → Attack1
AttackNMoveCancel active + MoveAxis > StopThreshold 且无 Attack request → Exit → RunLoop
没有有效取消 → 当前 End clip → Complete → None
```

连段窗口可以早于移动取消窗口。持续方向输入不会在连段窗口刚开始时抢走攻击；若同一 Tick 同时出现 Attack request 和有效移动输入，Attack transition 使用稳定更高优先级。

MotionCurve、Hit、Cancel、Cue 保持各自现有时间事实；End clip 超出 motion 区间的部分不隐式产生位移。

最终 Decision TreeClip 范围如下，均为 root-owned `Frame/Frame + SyncFact + ActionWindow` declaration：

| 动作 | ComboCancel | MoveCancel | 其他 |
|---|---:|---:|---:|
| Attack1 | 50..93 | 73..162 | - |
| Attack2 | 49..92 | 72..167 | - |
| Attack3 | 82..125 | 105..200 | - |
| Attack4 | 90..133 | 113..276 | - |
| Attack5 | 126..169 | 149..206 | - |
| RushAttack | 72..115 | 95..145 | Hit 18..45 |
| DodgeBack | - | - | DodgeRecoveryCancel 45..141 |
| DodgeForward | - | - | DodgeRecoveryCancel 46..142 |

普通 AttackN 的 Hit/ComboCancel/MoveCancel digest 分别使用 `N001/N002/N003`；RushAttack 使用 `6001/6002/6003`；DodgeRecoveryCancel 使用 `7002`。

### Tradeoff

- 好处：每段动作的启动、命中、连段接受和失败后摇都在一页 Timeline 中；调整窗口时能直接对照完整动画。
- 代价：Timeline 时长会从约 80 帧增加到完整 End 动画结束，未连段时动作锁定时间更长；这正是后摇业务语义，后续由作者调整 Cancel TreeClip，而不是缩短逻辑生命周期掩盖。
- 不采用独立 Recovery State：独立 State 会把每段攻击拆成 Attack 与 Recovery 两个图状态，连段窗口和动画段分散，作者需要跨页面调同一动作。
- 不采用 Hold：Hold 只能冻结姿态，不能表达后摇动画，也会与后续 clip 形成持续重叠。

## 决策四：Dodge 后摇统一为可取消阶段

DodgeBack 与 DodgeForward 的主段及 IFrame 期间不响应普通 Attack、Dodge 或移动请求。`DodgeRecoveryCancel` TreeClip active 后，状态机按以下顺序处理：

```text
Attack request → 外层 Dodge-to-Attack → RushAttack
Dodge request + MoveAxis → 内层当前方向-to-新方向 Dodge
MoveAxis > StopThreshold → 内层 Exit → 外层 None → Locomotion RunLoop
无请求 → 当前 Dodge Timeline 自然完成
```

RushAttack 是 Attack category 内的独立 leaf，使用 `Attack_Rush` 主段与 `Attack_Rush_End` 后摇。它不计入普通五段编号：RushAttack 后段 Attack request 进入 Attack1，移动输入退出到 locomotion。`Attack_Rush_Explode` 在没有正式业务条件前保持未连接。

进入 RushAttack 不增加持久路由变量。Timeline decision phase 在当前逻辑 Tick 提交 Frame/Frame `DodgeRecoveryCancel` fact；外层 `Dodge -> Attack` 与内层 `Enter -> RushAttack` 在同一 Tick 读取该 fact 和尚未消费的 Attack request，最终只由 RushAttack target activation 消费 request。下一 Tick fact 自动失效，因此不会留下需要清理的入口状态。

### Tradeoff

- 好处：Shift 的主段仍有承诺感，后摇又能自然接攻击、再闪避或移动；所有精细时点仍在 Timeline 中调整。
- 代价：Dodge 内层需要方向间重入边，外层与 Attack 内层入口必须共同遵守同一 Tick 的 Frame fact 与 request 消费顺序。
- 不采用外层平铺 RushAttack：RushAttack 是具体攻击 leaf，平铺会重新把动作大类与具体动作混在一起。
- 不采用“只要有输入就停止 Dodge”：该方案无法区分主段与后摇，也无法保证同 Tick Attack、Dodge、移动的稳定顺序。

## 打断职责边界

本 change 中“打断”分成四个明确动作：

1. State transition 决定从 Attack1 切到 Attack2、从 leaf 完成到 Exit，或从外层 Action 回到 None。
2. 通用 Runnable stop 将 source State body、TimelineNode 和 active descendant 停止。
3. Leaf Action lifecycle 根据离开原因提交一次 `Complete`、`Cancel(RecoveryCancel)`、`Cancel(DodgeRecoveryCancel)`、`Interrupt` 或 `Abort`。
4. AnimationPlaybackLifecycle 只将已释放 producer 交给 Animancer fade，不解释为什么离开。

Locomotion 进入/离开 `ActionOverride` 是 ownership 交接，不是它打断 Action。Parent Tree abort 仍使用现有通用分层停止协议；本 change 不新增 `AttackInterruptNode`、Dodge 专用 runtime 或动画 priority。

## 数据迁移与清理

- 移动现有 DodgeBack/DodgeForward StateNode、inline state body、Timeline ownership 和 rule graph 到唯一内联 Dodge Direction StateMachine，不克隆。
- 保留 Attack1/Attack2 与 Dodge 各 Timeline、TreeClip、declaration、ActionProfile 和 producer authoring identity。
- 新建 Attack3/4/5 与 RushAttack leaf state、inline Timeline、window declaration、TreeClip、Cue、motion 与 presentation binding；不得克隆 Attack1/2 identity。
- 为 Attack1..5 增加 MoveCancel declaration 与 Decision TreeClip，并为 Attack5 增加显式 ComboCancel declaration 与 loop transition。
- 将 `CanDodgeMoveCancel` 迁移为 `DodgeRecoveryCancel`，删除旧名称和只允许移动的条件语义。
- 删除外层旧 Dodge states/edges/rules、`IsDodging` declaration 与全部引用。
- Attack1/2 新增 End animation clip identity但保持原 AnimationTrack producer identity；Attack3/4/5 使用新的 Timeline/AnimationTrack producer identity并增加正式 Presentation binding。
- 重新编译唯一 Semantic IR、Float32 Program 和 Presentation Projection，不保留旧 Program/Projection fallback。

## 非目标

- 不新增命中求解、伤害结算、受击反应或攻击转向。
- 不猜测 `Normal_03_Explode` 与 `Normal_05_B` 的触发条件；它们在业务语义确认前不进入普通五连。
- 不新增普通 Attack-to-Dodge cancel window；本 change 只允许 Dodge recovery 内再次 Dodge，以及 Dodge recovery-to-RushAttack。
- 不修改网络模型、Solver、Animancer transition library 或 Timeline runtime。
- 不创建一次性 SubTree asset、外部 TimelineAsset、旧 WindowTrack 或特殊 Action interrupt 节点。
