# Design: Locomotion 与 FullBody 归属口径清理

## Context

现有规格曾经把角色状态表达成 FullBody 分层/HFSM 主树。后续重构已经把帧输出推向 `CharacterFramePipeline`，并出现了 `LocomotionFrameSubmitter` 与 `FullBodyActionFrameSubmitter` 这样的 sibling submitter。旧规格如果不先改，会让实现阶段继续把 Locomotion 塞回 FullBody 父级。

本设计把目标拆成三个外部 seam：

- Locomotion module interface：输入移动意图和 runtime facts，输出移动领域 snapshot 与移动候选输出。
- Action module interface：输入动作请求和 runtime facts，输出 action facts、body/channel claim 与动作候选输出。
- Character frame pipeline interface：接收领域提交，仲裁并产出 `CharacterFramePlan`。

FullBody 不再是一个外部 gameplay state module。它是 Action module 输出的 body/channel claim 维度，也可能对应动画表现层命名。

## Decisions

### 1. 不再以统一层级状态机作为目标

Decision: 正式目标不是一棵角色级状态树，也不是 FullBody 根 runner。

Rationale: 统一树会把移动、动作生命周期、身体占用和帧输出合成压进同一个 interface。这个 module 会变浅：调用者需要知道太多树路径和 owner 规则，implementation 的复杂度也会泄漏到测试里。

Consequence: 规格中的“FullBody HFSM”“统一状态机”“Locomotion 子树”只作为遗留口径被清理，不再作为新增 implementation 的目标。

### 2. 使用领域状态 ID，而不是树路径 ID

Decision: 正式 ID 使用 `Locomotion.Idle`、`Locomotion.MoveStart`、`Locomotion.MoveLoop`、`Locomotion.MoveStop`、`Locomotion.TurnBack`、`Action.Dodge` 这类领域状态 ID。

Rationale: 这些 ID 表达状态属于哪个 module，不暗示它们共享同一棵树。它们也更适合保存、回放和测试。

Consequence: 旧 `FullBody/Locomotion/...` 与 `FullBody/Action/...` 可以被迁移工具识别，但不能作为正式配置、正式断言或新增测试目标。

### 3. Locomotion 是移动领域 module

Decision: Locomotion 负责移动状态演进、移动事实和移动候选输出。

Rationale: 删除 Locomotion module 后，移动状态复杂度会回到输入解析、动作模块和帧管线多个调用点；它是有 depth 的 module。它不需要 FullBody 父级才能成立。

Consequence: Locomotion 可以内部使用状态图，但这个状态图是 Locomotion implementation，不是 Character 级统一树。

### 4. Action 是动作领域 module，不等于“状态机”

Decision: Action module 负责动作请求解析、打断、生命周期、body/channel claim 和动作候选输出。某个 action 可以内部有状态，但 Action 本身不被规定为统一状态机分支。

Rationale: 连招、受击、闪避、蓄力、技能时间线的 implementation 形态不同。把它们全部压成树叶子，会把树路径变成错误的外部 interface。

Consequence: `Action.Dodge` 是稳定 action state 或 resolved action id；它的 full-body 语义由 body/channel claim 表达，而不是由路径层级表达。

### 5. CharacterFramePipeline 是唯一合成 module

Decision: 所有领域 submitter 必须提交纯数据候选输出，最终帧输出由 `CharacterFramePipeline` 仲裁。

Rationale: 这是保持 locality 的关键 seam。否则每个领域都会开始直接写 animator、root motion 或 movement，形成多个输出权威。

Consequence: 如果实现需要绕过 pipeline 才能工作，必须停下来重新审视规格，而不是新增临时路径。

## Migration Order

1. 更新 OpenSpec：先退役 FullBody 主树、统一层级状态机、Locomotion 子树这些过时规格。
2. 更新测试词汇：断言领域 ID 和 pipeline 输出，而不是 FullBody 树路径。
3. 更新 runtime types：删除正式 `FullBodyOwnerKind.Locomotion` 语义，保留必要迁移兼容。
4. 更新 submitter graph：确认 Locomotion submitter 和 Action submitter 是 sibling。
5. 更新配置资产：迁移旧路径到领域 ID。
6. 更新诊断 view：保留只读兼容，不作为状态权威。

## Risks

- 当前 active changes 可能仍按旧 `FullBody/Action/...` 或统一 runner 编写；实现前必须 rebase 语义。
- 如果过早删除兼容 view，旧测试和调试面会同时失效；需要用静态验证确认旧路径只存在于迁移测试或文档中。
- 如果 Action module 暴露过多内部生命周期细节，它会变成浅 module；测试应优先跨 Action interface 验证 facts 和 claims。

## Open Questions

- 旧配置资产是否允许一次性强迁移，还是需要一个只读迁移器把旧路径转换成领域 ID？
- `FullBodyStateView` 是否继续保留一个版本周期作为诊断 view，还是在本次 implementation 中直接删除？
- 当前 active changes 中仍残留的旧 runner 或旧 FullBody path 语义是否直接并入本 change 清理，还是单独 rebase 后再实施？
