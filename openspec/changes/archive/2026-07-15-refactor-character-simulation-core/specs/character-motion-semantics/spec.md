# character-motion-semantics Specification

## MODIFIED Requirements

### Requirement: Motion 语义使用 Contribution、Intent、Modifier 和 Result

Compiled Graph、Timeline、Action 与 Effect operation MUST提交 portable MotionContribution；Evaluate MUST按 Program 固定顺序解析 contribution 与 modifier 并生成 WorldRequest；WorldSolver MUST返回 actual body result；Finalize MUST生成 MotionResult 与 body observation。任何阶段 MUST不直接写 Transform。

#### Scenario: Locomotion 与 Dodge 同 Tick 输出

- **WHEN** Locomotion 和 Dodge 都产生 contribution
- **THEN** Evaluate MUST按正式 channel/priority/blend 规则解析唯一 request
- **AND** WorldSolver MUST只消费解析后的 request

### Requirement: MotionModifier 来源可以来自 Timeline、Action、World、GameplayResult 或 Network

Program MAY从 Timeline、Action、GameplayResult ingress 或已编译 world observation 产生 model-neutral MotionModifier。Network packet、correction DTO 和 endpoint metadata MUST不直接成为 modifier；具体 Model 必须先转换为 typed ingress、restore request 或 OutputPlan policy。

#### Scenario: GameplayResult 产生 Knockback

- **WHEN** 当前 Actor 收到合法 Knockback GameplayResult ingress
- **THEN** compiled operation MAY产生 portable modifier/contribution
- **AND** MUST不读取来源 packet

### Requirement: MotionWarp 是 Move 前 modifier

MotionWarp MUST在 Evaluate 的 contribution resolve 之后、WorldRequest 生成之前运行。它 MUST只使用 Program state、portable target observation 与 Tick context，MUST不读取 Transform、Camera、Network Model 或 concrete Solver。

#### Scenario: 攻击对齐目标

- **WHEN** 当前 Action window 激活 MotionWarp
- **THEN** Evaluate MUST在提交 WorldRequest 前修改 portable intent

### Requirement: Motion modifier 第一阶段使用固定顺序

Program MUST将 modifier 顺序编译为稳定 operation order，并在相同 input/state 下产生相同 WorldRequest。Driver、Solver 和 Presentation MUST不重排 modifier。

#### Scenario: Turn、Warp 与 Dodge modifier 同时存在

- **WHEN** 当前 Tick 三类 modifier 都有效
- **THEN** Evaluate MUST按 Program 固定顺序应用

### Requirement: MotionWarp 必须保持为 Move 前 modifier

MotionWarp MUST保持为 Gameplay intent 的前置修正，不得直接修改 WorldSimulationState 或调用 WorldSolver。Solver actual result MUST仍然决定最终 body state。

#### Scenario: Warp 目标被墙阻挡

- **WHEN** Warp 后 request 穿过墙面
- **THEN** WorldSolver MAY截断实际位移
- **AND** Finalize MUST使用 actual result

### Requirement: Motion debug 必须解释仲裁结果

Structured Trace MUST分别记录 MotionContribution、resolve/modifier、WorldRequest、batch identity、WorldSolverResult 和 committed MotionResult，并通过 Source Map 关联 Program operation。Diagnostics MUST不读取 CharacterMotionStage 私有集合或 Solver mutable object。

#### Scenario: Dodge 覆盖 Locomotion

- **WHEN** Dodge contribution 消费较低 Locomotion channel
- **THEN** Trace MUST显示 source operation、仲裁原因、request 与 actual result

### Requirement: Timeline 必须支持直接 MotionCurve 位移轨

Compiled Timeline MotionCurve track MUST按 SimulationTick/canonical fraction 求值并产生 portable MotionContribution。Animation playback、Animancer fade 和 PresentationFrame MUST不产生或修改该 contribution。

#### Scenario: Dodge MotionCurve

- **WHEN** Dodge Timeline 进入位移区间
- **THEN** Evaluate MUST从 compiled curve 计算当前 Tick delta
- **AND** 该 delta MUST进入统一 motion resolve

### Requirement: Timeline 位移来源必须可追踪

MotionCurve constant、Timeline/Track/Clip identity、operation handle、ActionInstance 与 WorldRequest MUST通过 Source Map/Trace 保持关联。Runtime MUST不从 AnimationClip root motion 或 display name 推断来源。

#### Scenario: 查看 Attack1 位移

- **WHEN** diagnostics 选中某个 WorldRequest
- **THEN** MUST能定位原 Timeline MotionCurveClip 与 ActionInstance

### Requirement: MotionWarp 必须保持为目标对齐 modifier

MotionWarp MUST只表达当前 Gameplay target observation 下的 intent 对齐，不承担 pathfinding、collision、authority correction 或 animation root motion提取。目标缺失或无效时 MUST按正式 authoring 规则失败/不产生 modifier，不得查询 scene fallback。

#### Scenario: Target observation 缺失

- **WHEN** Warp operation 要求目标但当前 Character state 没有合法 target observation
- **THEN** MUST按 Program 配置产生明确无效结果
- **AND** MUST不按 GameObject name 搜索目标

## REMOVED Requirements

### Requirement: CharacterMotionStage 是 motion modifier 和 Move 的唯一边界

**Reason**：单角色 Stage 在 Actor Tick 内直接调用 Move，无法形成 session batch、世界原子状态或多 Actor 稳定顺序。

**Migration**：motion resolve 迁入 Kernel Evaluate，world mutation 迁入 ICharacterWorldSolver.ResolveBatch，result 应用迁入 Finalize。

#### Scenario: 删除 Stage

- **WHEN** Corin 切换到 compiled core
- **THEN** MUST不存在 CharacterMotionStage runtime path

### Requirement: Network correction 必须进入正式 correction phase

**Reason**：Correction 属于具体 Network Model 的 history/reconciliation 策略，不应作为公共 motion modifier phase。

**Migration**：删除 correction phase。后续模型只能使用 typed ingress、完整 snapshot restore 或自己的 OutputPlan/visual recovery。

#### Scenario: 删除公共 Correction

- **WHEN** 核心完成迁移
- **THEN** Motion operation MUST不接受 correction DTO
