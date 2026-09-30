# Corin RushAttack 正式链路设计

## 证据口径

证据只来自 ZZZ 结构化 dump 与工程 Motion 对账：

- `docs/replication/corin-controller-map.md`：8 个状态、7 个独立 Clip、状态名与 Motion 非同名/共享关系。
- `D:\ZZZ_Dump\output\corin_replication\replication-guide\data\actions\sm0-*-Attack_Rush*.json`：总帧、转移 `m_ExitTime`、条件与 AnimatorEvent 帧。
- `Corin技能状态事件索引.txt` 与 GameplayEffect Profile：Rush 命中事件已有正式 `Corin_Attack_Rush*` key。

## 架构决策

### 1. 独立 Rush Ability，而不是并入 NormalAttack

- **方案 A（采用）**：新增 `CorinRushAttackGameplayAbilityDefinition`，拥有 8 个 Rush 状态。
- **方案 B**：把 Rush 状态追加进 NormalAttack FSM。
- 业务取舍：Rush 有 Dodge 入口、普通/强化分支、Loop、特殊中断和回归 Normal04，业务边界本来就不是普通五段连段。独立 Ability 能保持 NormalAttack 已收口链路不被改写，也让 BranchAttack 后续评估不被迫继承 Rush 细节。并入的唯一好处是少一个 Ability 文件，但会把准入、取消、回滚和状态命名搅在一起。

### 2. 每状态一个 Timeline，而不是一条多段大 Timeline

- **方案 A（采用）**：8 个状态各建 1 个 Timeline，动画 clip 按 7 个 Motion 复用。
- **方案 B**：把 8 个状态塞进 1 条 Timeline，用 Section 表达所有分支。
- 业务取舍：每状态一个 Timeline 使状态本地时间、Loop 重入、Explode 分支和 End 收尾都回到 Ability 状态边界，`StateId` 直接等于 ZZZ 状态名。大 Timeline 需要额外 branch marker 与跨 Section 跳转，会把 FSM 转移语义复制进 Timeline，制造第二状态机。

### 3. Pose 继续走 Timeline Action playback

- **方案 A（采用）**：Rush 的动画经正式 Timeline producer 提交 committed FullBodyAction playback，由现有 ActionPlaybackInput / Slot 混入 Pose。
- **方案 B**：为 8 个 Rush 状态扩 Locomotion PoseStateMachine。
- 业务取舍：现有 NormalAttack 已用 Action playback 表达有限动作；Rush 也是有限 Gameplay-owned 动作，走同一链路不产生分裂路径。Pose 状态机适合持续 locomotion，扩入 Rush 会让 Gameplay 状态和表现状态抢所有权，还要把 7 个动作源复制成 source 绑定。

### 4. 命中 cue 只发布事件，不携带攻击语义

Timeline 只保留原始 `CueId`、`CueType=AttackProperty`、`StateId`、`LocalFrame`、`BranchId` 和播放身份。命中形状、伤害、相机 shake、命中条件由 GameplayEffect / Ability / Camera domain 消费。Timeline 不解析 `AttackProperty`，也不在表现帧重发逻辑命中。

## Timeline / Motion / cue 对账

下表帧使用 ZZZ 源状态 1 基帧。正式 Timeline clip 使用 0 基 start；源帧 `N` 的 cue 落在 `N-1`，状态边界在文档和条件里保留 ZZZ 源帧。

| 3C Timeline | ZZZ 状态 | 实际 Motion | 源总帧 | AttackProperty cue | BranchId |
|---|---|---|---:|---:|---|
| `CorinAttackRushTimeline` | `Attack_Rush` | `Attack_Rush` | 70 | 32 | `Rush` |
| `CorinAttackRushExplodeTimeline` | `Attack_Rush_Explode` | `Attack_Rush_Explode` | 70 | 1 | `Rush` |
| `CorinAttackRushEndTimeline` | `Attack_Rush_End` | `Attack_Rush_End` | 80 | 0 | `Rush` |
| `CorinAttackRushEnhanceStartTimeline` | `Attack_Rush_Enhance` | `Attack_Rush_Enhance_Start` | 44 | 16 | `Rush_Enhance` |
| `CorinAttackRushEnhanceLoopTimeline` | `Attack_Rush_Enhance_Loop` | `Attack_Rush_Enhance_Loop` | 86 | 43 | `Rush_Enhance` |
| `CorinAttackRushEnhanceEndTimeline` | `Attack_Rush_Enhance_End` | `Attack_Rush_Explode` | 70 | 0 | `Rush_Enhance` |
| `CorinAttackRushEnhanceExplodeTimeline` | `Attack_Rush_Enhance_Explode` | `Attack_Rush_Enhance_Explode` | 70 | 1 | `Rush_Enhance` |
| `CorinAttackRushEnhanceExplodeEndTimeline` | `Attack_Rush_Enhance_Explode_End` | `Attack_Rush_Enhance_End` | 118 | 0 | `Rush_Enhance` |

## 状态 Timeline 内容与命中帧

### Attack_Rush

- 命中 cue：
  - frame=8：`Corin_Attack_Rush_AttackProperty_01_01`。
  - frame=10,12,14...70：`Corin_Attack_Rush_AttackProperty_01_02`，共 31 个。
- 状态边界：
  - frame=13 且 `HoldAttackA=false`、`IsClicking=false`：进入 `Attack_Rush_Explode`。
  - frame=23 且 `Trigger_SawExplode`：进入 `Attack_Rush_Explode`。
  - frame=70 无条件进入 `Attack_Rush_Explode`。
  - Hit / Evade / Aid / ExQTE 等 `special_*` 中断归宿主 owner，不写成 Rush Timeline 内部状态。

### Attack_Rush_Explode

- frame=1：`Corin_Attack_Rush_AttackProperty_02`。
- frame=14 且 `Badge_S03`、`HoldAttackA`、`FrameCount mode4=44` 等正式转移条件成立：回归 `Attack_Normal_04`。
- frame=14 且 `PressAttackA`：回归 `Attack_Normal_04`。
- frame=70 无条件进入 `Attack_Rush_End`。
- Hit / Evade / Aid / AttackB / Switch / Move 等 `special_*` 路由不复制进 Rush Ability。

### Attack_Rush_End

- 无 AttackProperty cue。
- frame=80 为源状态收口；Hit、Evade、Aid、Attack、Switch、Move 等原 `special_*` 目标由正式宿主 owner 解释。

### Attack_Rush_Enhance（实际 Motion = Enhance_Start）

- frame=8：`Corin_Attack_Rush_Enhance_AttackProperty_01_03`。
- frame=10,12,14...38：`Corin_Attack_Rush_Enhance_AttackProperty_01_04`，共 15 个。
- frame=35（`0.785714 * 44` 的显式退出点）无条件进入 `Attack_Rush_Enhance_Loop`。
- Hit / Evade / Aid / ExQTE 中断归宿主 owner。

### Attack_Rush_Enhance_Loop

- frame=2,4...30 交替出现 `01_01` / `01_02`；frame=32,34...86 全部是 `01_02`，合计 43 个 cue。
- frame=43 且 `Trigger_SawExplode`：进入 `Attack_Rush_Enhance_Explode`。
- frame=86 且 `HoldAttackA=false`、`IsClicking=false`：进入 `Attack_Rush_Enhance_End`。
- frame=81 为 Loop 默认重入点；Ability 状态保持 `Attack_Rush_Enhance_Loop`，不伪造新业务状态。

### Attack_Rush_Enhance_End（实际 Motion = Rush_Explode）

- 无 AttackProperty cue。
- frame=40 且 `Badge_S03`、`HoldAttackA`、`FrameCount mode4=40`：回归 `Attack_Normal_04`。
- frame=55 且 `PressAttackA`：回归 `Attack_Normal_04`。
- frame=55 无条件进入 `Attack_Rush_End`。
- Hit / Evade / Aid / Attack / Switch / Move 中断归宿主 owner。

### Attack_Rush_Enhance_Explode

- frame=1：`Corin_Attack_Rush_Enhance_AttackProperty_02`。
- frame=55 且 `Badge_S03`、`HoldAttackA`、`FrameCount mode9=16`、`FrameCount mode4=44`：回归 `Attack_Normal_04`。
- frame=55 且 `PressAttackA`：回归 `Attack_Normal_04`。
- frame=55 无条件进入 `Attack_Rush_Enhance_Explode_End`。
- Hit / Evade / Aid / AttackB / Switch / Move 中断归宿主 owner。

### Attack_Rush_Enhance_Explode_End

- 无 AttackProperty cue。
- frame=118 为源状态收口；Hit、Evade、Aid、Attack、Switch、Move 等 `special_*` 目标不抄成图内状态，必须由正式 owner 消费。

## Control / Ability 边界

- Dodge 前摇满足原控制器条件后，由 Dodge/Control owner 提交 Rush 或 Rush Enhance activation；`Badge_S01` 决定普通/强化入口。
- Rush 内部只拥有 8 个状态；`Attack_Normal_04` 回归通过正式 Ability admission / cancel / handoff 完成，不直接操作 NormalAttack Timeline。
- `special_*` 是宿主层路由编码：Hit 归受击 owner，Evade 归 Dodge owner，Aid/ExQTE 归 Aid owner，Switch 归 Switch owner。当前缺失 owner 时必须显式失败或保持不可用，不用空状态冒充。

## Pose 接线

- 8 个 Timeline 的 AnimationTrack 绑定对应 Motion，继续使用 `FullBodyAction` channel / Slot。
- Pose Graph 只消费 committed Action playback；不为 Rush 增加 Locomotion 状态、第二 Slot 或 Timeline 解析器。
- `Attack_Rush_Enhance_End` 与 `Attack_Rush_Enhance_Explode_End` 的状态身份不同但 Motion 共享，必须保留两个 Timeline producer 和各自 StateId，不能用同一播放身份合并。
