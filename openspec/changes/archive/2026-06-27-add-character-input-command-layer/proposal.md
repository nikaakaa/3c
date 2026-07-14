# Change: add-character-input-command-layer

## Summary
新增正式角色输入命令层规划，让 `Assets/GameScripts/Main/Runtime/Character/Pipeline/Input` 不只保存 `InputActionAsset` 的 raw 读取状态，而是产出可被本地预测、BTSMTL 图、动作请求缓存和网络输出共同使用的 gameplay 输入数据。

该变更建立两条输入产物：

- 连续命令：`Move`、`Look`、`SprintHeld` 等每 tick 覆盖的数据。
- 离散请求：`Attack`、`Dodge`、`Jump` 等可预输入、可过期、可消费、可进网络命令的数据。

该变更不是替代 `add-character-pipeline-runtime-entry`，而是在它定义的 `CharacterInputStage`、`CharacterGraphContext`、`NetworkOutput` 位置上细化输入层。它也不是废弃现有 BTSMTL `InputActionValueNode`，而是把它降为 raw 输入读取和调试入口；角色主链路应消费输入层产出的语义 command/request。

## Why
项目已经选择本地移动预测和服务端校正，输入层必须产出可记录、可发送、可重放的 gameplay command，而不是让 Tree、网络和 motion 各自读取 Unity InputAction。攻击、闪避、跳跃也需要预输入、过期、优先级和消费状态，这些语义不能放进 raw 输入值节点里。

## What Changes
- 新增 `CharacterInputProfile`，把 InputAction 来源映射为 gameplay semantic signal/request。
- 将输入采样结果建模为每 tick 的 `CharacterInputFrame`。
- 将 Move、Look、SprintHeld 等连续输入建模为 command。
- 将 Attack、Dodge、Jump 等离散动作建模为 request，并放入 request buffer。
- 新增 input history 边界，为预测校正后的输入重放准备数据。
- 调整 `ClientCommand` 语义，使网络输出发送 gameplay command/request，而不是 raw InputAction 事件。
- 规划 semantic input/request 节点，让 BTSMTL 图和 TransitionRuleGraph 消费输入层产物。
- 保留现有 InputAction ValueNode 作为 raw 输入读取和调试入口。

## Motivation
当前 `add-character-pipeline-runtime-entry` 已规划 `InputStage -> CharacterInputSnapshot -> CharacterPipelineFrame -> GraphStage -> NetworkOutput`，并且代码中已经出现 `CharacterInputStage`、`CharacterInputSnapshot` 和 `ClientCommand` 的薄实现。但当前输入快照只表达 action asset、input sequence 和启用状态；`ClientCommand` 也还是 `move + actionId` 形状，尚未表达移动预测需要的输入历史、离散动作请求、预输入缓存和消费边界。

项目已经选择本地玩家移动预测和服务端校正。移动预测需要每个 simulation tick 的输入帧可记录、可发送、可重放；攻击、闪避、跳跃需要在动作窗口未打开时缓存为预输入，并在窗口打开后由动作管线消费。若 Tree 或状态机直接读取 `InputAction`，这些能力会分散在节点、状态、网络和 motion 里，后续会形成分裂路径。

## Goals
- 在 `Assets/GameScripts/Main/Runtime/Character/Pipeline/Input` 下定义正式输入层模型。
- 使用 `CharacterInputProfile` 将 Unity InputAction 映射为 gameplay semantic signal/request。
- 让 `CharacterInputStage` 每 tick 产出 `CharacterInputFrame`，而不是只暴露 raw action asset。
- 将连续输入和离散请求分开建模。
- 为离散请求定义 `CharacterInputRequestBuffer`，支持预输入、过期、优先级和消费状态。
- 为本地预测定义 `CharacterInputHistory`，保存按 tick/sequence 采样的输入帧。
- 让 `NetworkOutput` 收集基于 `CharacterInputFrame` 的 `ClientCommand`，而不是 raw `InputAction` 事件。
- 让 `CharacterGraphContext` 从同一输入帧和请求缓存提供图内读取入口。
- 让 BTSMTL 图后续能拖入输入层产出的 semantic signal/request，而不是长期直接依赖 Unity InputAction。
- 保留现有 `InputActionValueNode` 作为 raw 输入值节点和调试入口。

## Non-Goals
- 不实现真实 Fantasy transport、服务器 handler、完整服务端校验或 correction replay。
- 不实现完整回滚系统、完整世界 rollback 或全局帧同步。
- 不把 Tree 或 TransitionRuleGraph 变成输入系统。
- 不让 TransitionRuleGraph 中的条件查询消费 request。
- 不恢复旧 `PlayerSO/LocomotionSO/ActionSO` 或 BBB 代码状态机。
- 不新增独立 Input Graph、Workbench 输入路径或 fallback 配置。
- 不在本 change 中要求新增测试；用户会做 Unity 端到端验证。

## Impact
- 新增 `character-input-pipeline` 能力规格。
- 新增 `character-input-node-authoring` 能力规格。
- 后续实现会调整当前 `CharacterInputSnapshot` 和 `ClientCommand` 的形状。
- 后续实现会让 Host 引用 `CharacterInputProfile`，profile 再引用正式 `InputActionAsset`。
- 后续实现会让 `CharacterGraphContext` 同时提供 semantic input/request 读取和现有 `IInputActionValueSource` raw 读取。
- 后续实现会新增输入层语义节点或拖拽入口，使 Tree/TransitionRuleGraph 能消费 `CharacterInputProfile` 中的 signal/request 定义。

## Dependencies
- 依赖 `add-character-pipeline-runtime-entry` 提供 `CharacterPipelineRunner`、`CharacterPipelineHost`、`CharacterPipeline`、`CharacterGraphContext`、`NetworkOutput` 和基础 stage 调度。
- 依赖 `btsmtl-input-action-node-authoring` 的 raw InputAction 值节点继续通过正式 `IInputActionValueSource` 读取。
- 依赖 `add-btsmtl-transition-rule-graph-authoring` 中的规则图边界，确保 Transition 条件只做纯 Bool 求值。

## Open Questions
- 第一版 `CharacterInputProfile` 是否直接内置 `Move/Look/Attack/Dodge/Jump` 约定 ID，还是完全由用户配置 ID。倾向完全配置，但 demo 模板可以预置正式 profile 资产。
- 第一版 request 消费节点是否随本 change 一起实现，还是先只实现 buffer 与非消费查询。倾向先实现 buffer、查询和 pipeline API，再让动作/状态行为节点单独接入消费。
- `CharacterInputHistory` 第一版保存多少 tick。倾向提供正式容量字段，不做 hidden fallback 默认路径。
