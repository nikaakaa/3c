# Proposal: 重构角色输入边界命名

## Why

当前输入 spec 把三件不同层级的东西混在了一起：

- 输入采样层需要保存本 tick 的连续输入值和离散触发。
- BTSMTL 玩法图只应该读取玩法输入值、查询或消费动作请求。
- 网络和预测层才需要把本 tick 输入打包成 `ClientCommand`、`InputSequence` 和 `SyncFacts`。

现有 `signal` 口径把连续输入值包装成一个不直观的概念，又把 `continuous command` 暴露到 `CharacterInputFrame`、Graph 和网络说明里，导致作者会误以为 BTSMTL 需要理解 network command。移动配置也因此看起来像是 `signal -> motion intent` 的特殊硬编码，而不是运动模块读取输入值后提交 motion 来源。

这会伤害两个业务目标：

- 动作 demo 需要能清楚解释输入、状态图、运动模块、网络事实各自职责。
- 后续做移动、闪避、攻击 root motion、受击击退和校正时，不能让输入命名污染 motion authoring 和 sync fact 边界。

## What Changes

本变更清理角色输入边界的正式口径：

- 删除作者可见和 Graph 语义中的 `signal` 词。
- 删除 BTSMTL/Graph 语义中的 `command` 词；`ClientCommand` 只属于 SyncFacts/Network 打包层。
- `CharacterInputProfile` 改为表达两类配置：typed input value 和 action request。
- 连续或保持型输入统一称为 input value，例如 `MoveAxis`、`LookAxis`、`SprintHeld`。
- 离散且需要 buffer/consume 的输入继续称为 action request，例如 `Attack`、`Dodge`。
- BTSMTL 只读取 input value、查询 action request 或在正式接受点消费 action request。
- BTSMTL authoring MUST 能从 InputSystem/InputProfile 自动生成或刷新对应输入信息节点；节点身份来自输入层配置，节点本身不成为第二份输入配置。
- Motion/Locomotion 模块读取 input value 后提交 `MotionContribution`，不读取 network command。
- NetworkSendStage 从输入帧和 pipeline 输出收集 `ClientCommandFrame` 或等价 sync fact，但 Graph 不产出 input command。

## Non-Goals

- 不实现代码迁移。
- 不新增测试。
- 不重写 Unity Input System 资产。
- 不恢复旧 locomotion SO/config。
- 不新增输入专用 Graph、Workbench 路径或 fallback 配置。
- 不把 Motion 仲裁规则并入本变更；motion channel 和 blend 仍由 `refactor-character-motion-arbitration` 处理。

## 当前代码事实

- `CharacterInputProfile` 目前有 `m_Signals` 和 `m_ActionRequests`。
- `CharacterInputStage` 目前把连续输入锁存后写入 `CharacterInputFrame`，内部命名使用 command。
- `CharacterGraphContext.TryReadInputVector2("Move")` 可以让 BTSMTL 读取当前输入值。
- `SetMotionIntentFromInputNode` 当前读取 `Move` 并直接写 `MotionIntent`。
- `NetworkSendStage` 通过 `SyncFacts` 收集输入帧和动作输出。

## 与现有 Spec 的关系

- `character-input-pipeline` 当前把 `signal` 和 `continuous command` 写成主概念，本变更将其改为 input value 和 action request。
- `character-input-node-authoring` 当前允许把 signal/request 拖入 BTSMTL，本变更将 signal 改为 input value，并禁止 ClientCommand 成为 BTSMTL 节点语义。
- `character-network-sync-domain-contract` 当前写到 Graph 或 InputStage 产出 input command，本变更收敛为 InputStage/NetworkSendStage 从输入帧形成网络命令，Graph 只产出 gameplay/motion/action 输出。
- `refactor-character-motion-arbitration` 继续负责让 locomotion 输入变成 `Locomotion` channel 的 `MotionContribution`；本变更只规定它读取的是 input value，不是 signal 或 network command。
