# Design

## 目标结构
正式输入层应成为 Unity InputSystem、BTSMTL 图、动作管线和网络输出之间的共同语言。

```text
InputActionAsset
-> CharacterInputProfile
-> CharacterInputStage
-> CharacterInputFrame
   -> Continuous Commands
   -> CharacterInputRequestBuffer
   -> CharacterInputHistory
-> CharacterGraphContext
-> GraphStage / TransitionRuleGraph / MotionStage / NetworkOutput
```

`InputActionValueNode` 仍存在，但它不是角色预测和网络的主输入模型。它负责 raw 输入读取、快速调试和简单条件。角色主链路使用 `CharacterInputFrame`、continuous command 和 action request。

## 关键对象

### CharacterInputProfile
`CharacterInputProfile` 是输入层 authoring 数据源。它引用一个正式 `InputActionAsset`，并把 action 映射为 gameplay 语义：

- `CharacterInputSignalDefinition`：连续或保持型输入，例如 Move、Look、SprintHeld。
- `CharacterActionRequestDefinition`：离散动作请求，例如 Attack、Dodge、Jump。

每个定义使用稳定 semantic id。InputAction 的 action id 用于绑定来源；semantic id 用于 gameplay、Tree 和网络命令。这样改键位或 action 重命名不会改变 gameplay 请求语义。

### CharacterInputFrame
`CharacterInputFrame` 是每个 simulation tick 的输入事实。它至少包含：

- `SimulationTick`
- `InputSequence`
- `AuthorityMode`
- 连续命令集合
- 本 tick 新产生的动作请求集合

移动预测和网络发送都应读取这份 frame，而不是直接读取 `InputAction`。

### CharacterInputRequestBuffer
`CharacterInputRequestBuffer` 保存离散动作请求。请求至少包含：

- request id
- created tick
- created sequence
- buffer duration 或 expire tick
- priority
- consumed 状态

预输入属于 request buffer。Transition 条件可以查询 request 是否存在，但不能消费。消费必须发生在状态行为、动作管线或后续正式 action accept 点。

### CharacterInputHistory
`CharacterInputHistory` 保存最近若干 tick 的 `CharacterInputFrame`，用于本地预测校正后的重放。第一版只建立正式数据结构和写入路径，不实现完整 correction replay。

### ClientCommand
`ClientCommand` 应承载一个 tick 的 gameplay 输入，而不是 raw InputAction 事件。第一版可包含：

- input sequence
- simulation tick
- continuous commands 摘要
- action requests 列表

`NetworkSendStage` 只收集这些 command，不直接发送 Fantasy 消息。

## 数据流

本地预测角色：

```text
NetworkReceiveStage
-> InputStage 采样 CharacterInputProfile
-> 写入 CharacterInputFrame
-> 写入 RequestBuffer 和 InputHistory
-> GraphContext 暴露当前 frame/request
-> GraphStage / TransitionRuleGraph 查询输入
-> MotionStage 使用连续命令或 MotionIntent
-> NetworkOutput 收集 ClientCommand
```

远端代理角色：

```text
NetworkReceiveStage
-> 注入 ServerSnapshot / ConfirmedEvent / Correction
-> InputStage 不采样本地 InputAction
-> Graph 或表现层消费快照/插值数据
```

## Decision: 使用 CharacterInputProfile，而不是让 Host 直接暴露 InputActionAsset
业务取舍：

- 直接使用 `InputActionAsset` 最快，但 Tree、网络和动作管线都会看到 Unity InputSystem 语义，后续 AI、回放、远端输入和改键位都会不舒服。
- `CharacterInputProfile` 多一层配置，但它把“按键来源”和“角色意图”分开。Gameplay 只认 Move、Attack、Dodge 这些语义，不关心键鼠、手柄或 action 名字。

结论：Host 应引用 `CharacterInputProfile`。Profile 引用 `InputActionAsset`。

## Decision: 连续命令和离散请求分开
业务取舍：

- 把所有输入都做成 request 会让 Move/Look 这种每帧值变得难用，还会引入无意义消费。
- 把所有输入都做成 bool/value 会丢掉 Attack/Dodge/Jump 需要的预输入、过期、优先级和消费。

结论：Move/Look/SprintHeld 是 continuous command；Attack/Dodge/Jump 是 action request。

## Decision: 预输入放在 RequestBuffer，不放在 InputAction 节点
业务取舍：

- 在 InputAction 节点里做预输入会让值节点产生副作用，Transition 条件一旦求值就可能改变输入状态。
- 在 RequestBuffer 中做预输入能统一支持状态机、动作管线、网络命令和预测重放。

结论：InputAction 节点只读值；预输入和消费归 `CharacterInputRequestBuffer`。

## Decision: TransitionRuleGraph 只能查询 request，不能消费 request
业务取舍：

- 条件图消费 request 会导致多条 Transition 按优先级求值时出现隐性副作用，调图困难。
- 查询和消费分开后，TransitionRuleGraph 保持纯 Bool 求值。真正进入状态或动作 accept 点时再消费。

结论：规则图节点只提供 `HasRequest`、`CanConsumeRequest` 这类非消费查询。消费发生在状态行为或后续 action pipeline stage。

## Decision: 网络发送 ClientCommand，不发送 InputAction
业务取舍：

- 发送 InputAction 名称或 performed 事件会把客户端输入设备细节泄漏给网络协议。
- 发送 `ClientCommand` 能让服务器、回放、AI 和调试入口都使用同一 gameplay 语言。

结论：`NetworkOutput` 收集 `ClientCommand`。真实 transport 后续再接。

## 与现有 InputAction 节点的关系
现有 `InputActionValueNode` 不废弃。它继续负责：

- raw 输入值调试。
- 简单状态条件。
- 还没有 semantic profile 节点时的过渡性图内读取。

但主输入层不应继续加强 `InputActionValueNode` 的业务职责。后续 Tree 应优先拖入 `CharacterInputProfile` 中的 signal/request 定义。

## 与 add-character-pipeline-runtime-entry 的关系
该变更不替换 `CharacterPipeline` 主入口。它细化其中的 InputStage、GraphContext、NetworkOutput 和 ClientCommand：

- `CharacterInputStage` 从 raw asset reader 变为 semantic sampler。
- `CharacterInputSnapshot` 演进为 `CharacterInputFrame`。
- `CharacterGraphContext` 读取同一 frame/request buffer。
- `NetworkSendStage` 收集从 frame 生成的 `ClientCommand`。

## 风险
- `CharacterInputProfile` 会增加一层 authoring 资产，需要后续编辑器 UX 支持。
- request buffer 如果过早接入消费节点，容易破坏 TransitionRuleGraph 纯求值边界。
- `add-character-pipeline-runtime-entry` 已完成并把代码迁移到 `Assets/GameScripts/Main/Runtime/Character/Pipeline`；实施时必须沿用 `CharacterGraphContext` 和新路径，不再回写旧路径或旧命名。
