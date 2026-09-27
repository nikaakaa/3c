# ZZZ Corin RushAttack Ability / FSM 对照

本页记录当前作者链；原始证据见 `zzz-corin-controller-map.md` 和 `D:/ZZZ_Dump/output/corin_replication/20260903_controller_structured_v6/Avatar_Female_Size01_Corin_Controller__1291803240_00335DAA.json`。状态名不能用来猜动画名。

## 正式入口

- Control 的移动攻击、闪避接招提交 `RushAttack`；没有独立 Rush 按键。
- `CorinRushAttackGameplayAbilityDefinition.asset` 通过 `CorinRushAdmissionProfile.asset` 准入，执行 `CorinRushAttackGameplayAbilityAuthoringCode` 生成的 FSM。
- `Badge_S01` 由正式 GameplayTag 输入选择强化分支；普通分支和强化分支都保留。
- 普通 Rush 当前由 `RushRelease` 加 `MoveAxis` 判断提前释放；这与原始 Controller 的 `Bool_HoldAttackA` / `Bool_IsClicking` 条件尚不完全等价，不能称为原版行为已全部还原。
- 强化循环以 `AttackHeld` 松开进入 End，以正式 `SawExplode` ActionEvent 进入 Explode。后者是否有实际事件生产者，必须另核运行证据。
- `RushAttackHandoff` 内再次收到 `Attack`，Control 走现有替换链，进入普通攻击第四段。窗口本身不合成输入请求。

## 状态链

普通：`Attack_Rush → Attack_Rush_Explode → Attack_Rush_End → Exit`。

强化：`Attack_Rush_Enhance → Attack_Rush_Enhance_Loop`，循环有两条出口：

| 循环出口 | 后续状态 |
|---|---|
| 松开普攻 | `Attack_Rush_Enhance_End → Attack_Rush_End → Exit` |
| SawExplode 事件 | `Attack_Rush_Enhance_Explode → Attack_Rush_Enhance_Explode_End → Exit` |

强化 End 复用普通 Rush Explode 动画，不能绑定 Enhance End 动画，也不能直接跳 Exit。具体时长与窗口见 `zzz-corin-rush-timeline-map.md`。

## 作者与运行边界

- FSM 的状态主体只播放对应 shared Timeline；完成条件读取状态主体完成，不在 FSM 条件中读取无 TreeClip 上下文的 `TimelineTime`。
- Timeline 的逻辑 TreeClip 通过 Frame scope / Frame lifetime Blackboard 声明发布真实 ActionWindow。
- 旧的空 Boundary 图不表示可用接招窗口，已从强化作者链清理。
- 所有动作复用 `FullBodyAction` / `corin.full-body-action`，不增加独立 Pose 动作系统。
- `AbortRequested`、`InterruptRequested`、`ExecutionCompleted` 和 `RushMoveExit` 沿现有宿主结束规则处理。

## 发布与证据

`PublishSelected` 同时生成 RushAttack Fixed/Float32 产物。动画源已在正式 Profile、Blend Policy 和动画 Domain Resource Set 注册。

2026-09-27，录制 `757f243033414fc7b123c97e2fcb0d70` 在本轮强化退出修正之前已完成 2716 帧，消除了原第 776 帧的运行阻断。它包含 60 次 Attack、3 次 Branch、3 次 Dodge，AttackHeld 为真 251 帧、BranchHeld 为真 164 帧。输入存在只证明有覆盖候选，不能证明每个状态分支和视觉退出都正确。
2026-09-27 17:11，强化退出修正后的同输入回放完成 2716 帧，运行证据 `docs/diagnostics/corin-rush-runtime-20260927-171133.json`。实际观察到强化起手→循环→松手End→普通RushEnd，以及普通1→2→3→4→5→5End→1。动画时间倒退和同帧选择冲突均为0。新旧输入一致，角色轨迹有1644帧变化，最早为录制相对帧1072；这是行为版本变化，不据此声称所有普攻表现无回归。Rush窗口接招、强化爆发事件分支、E循环行走仍缺运行完成证据。

2026-09-27 18:04，补齐强化Rush/E位移、悬空权重、E循环播放配置及Attack5退出窗口后的同输入回放完成2716帧。证据为 `docs/diagnostics/corin-action-runtime-20260927-180403.json`；实际观察到普通1→2→3→4→5→1以及两次E起手→爆发→收招，无运行失败。此次观察器在回放开始后接入，只保留播放顺序，不能用其创建事件的0位置推导转移时刻。

### 接招链路与现有录制覆盖核对

- `CorinCharacterControlModule.ResolveAbilityOutputs` 在Attack请求、Rush活动实例、RushAttackHandoff窗口三者同时成立时调用 `SubmitAttack`，传入被替换实例及 `Attack4` 入口；该分支先于移动退出执行。
- `CorinAttackAdmissionProfile` 的取消标签包含Rush，最大实例数为1；普通攻击作者图的 `Attack4 Activation Entry` 消费 `activationEntryId=Attack4`。这证明静态链路配置齐全，不代表已观察到窗口接招成功。
- 现有最新录制的Attack请求发生于793、1090、连续普通连招输入、1622、1868、2047等原始输入tick；不能把用于触发Rush的那次普攻当成窗口内的第二次普攻。当前运行观察仍未出现Rush直接进入Attack4。
- 最新录制的三次Branch按住区间为 `[2234,2266)`（32帧、无移动）、`[2374,2462)`（88帧、其中36帧移动）、`[2558,2602)`（44帧、全程移动）。当前E起手进入循环需66帧。中间88帧按住虽具备输入候选，但Control在Branch活动期间忽略新的Branch请求；结合仅观察到两次起手、均进入爆发的证据，可推断这次请求落在上一技能收招期间。它不能证明Loop/Walk已执行，也不能仅凭此断言Loop/Walk损坏。
- 已按当前 `RollbackInputCodec` v3 的二进制字段布局离线检查现存录制。早于最新2716帧的录制均未提供AttackHeld和BranchHeld，不能不加说明地作为当前按住技能的完整回归输入。汇总见 `diagnostics/corin-existing-input-coverage-20260927.json`。
