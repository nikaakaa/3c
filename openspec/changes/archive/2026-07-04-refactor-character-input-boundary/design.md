# Design: 输入值、动作请求和网络命令分层

## 目标

把输入链路拆成三层清楚的业务语言：

```text
Unity InputAction
  -> CharacterInputFrame 输入值 / 动作请求
  -> BTSMTL 读取输入值或请求
  -> Motion / Action 产出正式 gameplay output
  -> SyncFacts / Network 打包 ClientCommandFrame
```

Graph 可以读取输入，但不应该理解 network command。网络层可以记录 command，但不应该反过来污染 Graph authoring。

## 术语

| 术语 | 所属层 | 含义 |
| --- | --- | --- |
| InputValue | 输入层和 BTSMTL 读取层 | 当前 tick 的 typed 输入值，例如 `MoveAxis: Vector2`、`SprintHeld: bool` |
| ActionRequest | 输入层、状态行为、Action 管线 | 需要 buffer、查询和消费的离散动作请求，例如 `Attack`、`Dodge` |
| ClientCommandFrame | SyncFacts/Network 层 | 从本 tick 输入帧和请求打包出来的网络/预测事实 |
| InputSequence | 输入历史和网络/预测层 | 本地输入帧序号，用于关联预测、确认和校正 |

`signal` 不再作为正式术语。`command` 不再作为 BTSMTL 或输入配置术语，只保留在 `ClientCommandFrame` 这类网络事实名称中。

## 输入层

`CharacterInputProfile` 持有正式输入配置：

- `InputValues`：稳定 action identity 到 typed gameplay input id 的映射。
- `ActionRequests`：稳定 action identity 到 buffered action request id 的映射。

输入层负责在表现帧锁存当前 InputAction 状态，在 logic tick 产出 `CharacterInputFrame`。Frame 保存 input values、新产生的 action requests、local logic tick、input sequence 和 authority mode。

连续输入值不消费；动作请求可以查询、过期和消费。

## BTSMTL 层

BTSMTL authoring 允许两类输入信息节点：

- `InputValueInfo` 节点：由输入层配置生成或刷新，读取当前 `CharacterInputFrame` 的 typed value。
- `ActionRequestInfo` 节点：由输入层配置生成或刷新，查询 request buffer；消费仍只能发生在状态行为或动作接受点，不能发生在纯 TransitionRuleGraph 条件节点。

这些节点是输入层配置在 Graph authoring 里的投影：`MoveAxis`、`LookAxis`、`Attack` 等输入项可以自动生成对应输入信息节点，节点保存的是输入定义稳定身份或引用，不保存第二份 InputAction 配置。输入绑定、action identity、value type 和 request buffer 策略仍以 `CharacterInputProfile`/InputSystem 配置为准。

编辑器可以提供“从 InputProfile 同步输入信息节点”的操作，也可以在拖拽 InputAction/InputProfile 项时创建对应节点。同步必须使用稳定身份更新已有节点，不能靠显示名创建重复节点。

BTSMTL 节点字段和 UI 不使用 `signal` 或 `command`。例如：

- `MoveAxis Input`
- `Has Attack Request`
- `Consume Attack Request`

## Motion 和 Action 层

Locomotion 不是输入系统的一部分。它读取 `MoveAxis` 等 input value，结合角色运动配置，提交 `Locomotion` channel 的 `MotionContribution`。

Action 激活不是输入系统的一部分。它可以查询或消费 `Attack` 这类 action request，然后提交 `ActionActivationRequest`。

这样输入层只负责“玩家这 tick 给了什么输入”，Motion/Action 层负责“这些输入在当前状态下意味着什么”。

## SyncFacts 和 Network 层

`ClientCommandFrame` 由 NetworkSendStage 或等价 sync fact 收集阶段从 `CharacterInputFrame` 生成。它可以包含 input value 摘要、action request 摘要、input sequence 和 local logic tick。

Graph 不创建 `ClientCommandFrame`，也不把 `command` 当作节点输出。Graph 产生的是 motion contribution、action activation、gameplay result、presentation cue 等 gameplay output。

## 决策和 Tradeoff

### 方案 A：保留 signal 和 continuous command

- 优点：当前代码改动最少。
- 缺点：作者需要同时理解 signal、command、request、InputAction 和 SyncFacts，概念重复；移动节点看起来像输入层直接决定 motion。
- 业务取舍：不利于求职 demo 讲清楚输入、运动和网络边界。

### 方案 B：Graph 直接读取 raw InputAction

- 优点：节点直观，少一层 profile 映射。
- 缺点：Graph 依赖 Unity InputAction 资产名和设备输入，网络历史、请求 buffer、远端代理和预测重放都难以统一。
- 业务取舍：短期调移动很快，但后续动作请求、预输入和网络校正会分裂路径。

### 方案 C：InputValue + ActionRequest + ClientCommandFrame 分层

- 优点：Graph 看到的是玩法输入，网络看到的是可同步事实，Motion/Action 能独立解释输入。
- 缺点：需要重命名现有 profile 字段、节点字段和文档，并迁移已有资产。
- 业务取舍：最适合第三人称动作客户端 demo，能支撑移动、动作、预测和 debug 的统一解释链路。

本 proposal 选择方案 C。
