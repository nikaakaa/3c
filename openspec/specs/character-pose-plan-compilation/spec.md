# character-pose-plan-compilation Specification

## Purpose

本项目不再把 Pose Graph 编译成独立 Pose IR、Program Image 或 Pose ABI。本文件保留原能力名，用于定义“没有加载期 Pose Compiler”时的作者数据校验、原生图绑定和运行时准备边界。

## Requirements

### Requirement: Pose Graph 不得生成第二套运行语言

Pose authoring MUST 保留 Graph、Node、Connection、端口、稳定 identity 和正式资源引用。系统 MUST 不生成 `CharacterPoseProgramImage`、Pose IR、万能 Operation payload、Pose 专属 Program ABI 或加载期转换器。Graph 的静态拓扑校验属于 authoring/初始化边界，不得成为另一种运行语言。

#### Scenario: 读取 Pose Graph

- **WHEN** 角色准备正式表现
- **THEN** Pose Host MUST 读取正式 FlowCanvas Graph、Node、Connection 和领域 binding
- **AND** MUST 创建 actor-local 原生图实例
- **AND** MUST 不从旧 Projection 或 `.csim` 提取 Pose 执行数据

### Requirement: Pose 节点必须在唯一原生图实例中执行

每个 Actor MUST 拥有隔离的 FlowCanvas Graph instance、节点状态、Source demand、Constraint 状态和最终输出绑定。共享作者资产 MUST 不保存 Actor 播放时间、IK 历史、Pending 帧或 tuning。宿主 MUST 通过既有 Presentation 时钟 Manual 驱动图，不得创建独立时钟。

#### Scenario: 两个 Actor 使用同一 Pose Graph

- **WHEN** 两个 Actor 绑定同一作者 Pose Graph
- **THEN** 两个 Actor MUST 拥有独立图实例和运行状态
- **AND** 一个 Actor 的 State、Source、Constraint 或 Tuning 变化 MUST 不修改另一个 Actor

### Requirement: Pose 运行顺序必须由表现宿主统一协调

根表现帧 MUST 按 Fact、PoseState、Source demand、资源准备、图求值、Local/Component Pose、Foot/Goal Contribution、唯一 Goal Assembler、唯一 FullBodyIK、Final Publication 的正式顺序推进。Pose 节点、Source、Constraint 和 Final Publication 各自只负责自己的阶段，不得互相扫描或重复求值。

#### Scenario: 中间阶段失败

- **WHEN** Source、Pose 节点、Foot、Goal、FBBIK 或 Final Publication 在当前帧失败
- **THEN** 根表现事务 MUST 丢弃本帧 Pending 结果
- **AND** MUST 不发布部分骨骼、不回退旧 Preview 结果、不创建第二 Writer

### Requirement: Preview 与正式运行只共享正式 Pose 实现

ScenePlay 中的 Preview MUST 使用正式 Actor 的 Pose Host、原生图实例、Source backend、Constraint、Final Publication 和诊断事实。Pose Editor MUST 不创建 Fact Fixture、Query Fixture、独立 Pose evaluator、临时 PlayableGraph 或第二个 Pose 时钟；未进入 ScenePlay 的窗口只能编辑和读取作者数据。

#### Scenario: ScenePlay 观察 Pose

- **WHEN** ScenePlay 正式 Session 已发布 Actor 的 Pose binding
- **THEN** Pose UI MUST 读取该 Actor 的 committed Pose/Source/Constraint 事实
- **AND** UI MUST 不自行采样 Clip、重建 Pose 或修改 Runtime 状态

### Requirement: Pose 变更必须通过正式作者和运行绑定生效

Graph、Node、Connection、Profile、Rig、Mask、Player 和资源变更 MUST 经正式 Mutation、Validator、Undo、revision 和 ScenePlay Prepare/Adopt 进入运行。运行中的 Actor MUST 不被窗口静默替换 Graph 实例；revision 不匹配时明确停止或重新准备。

#### Scenario: Graph revision 变化

- **WHEN** 作者修改 Pose Graph 后 revision 变化
- **THEN** 当前 Actor MUST 保持旧绑定直到正式 Reset/Prepare/Adopt
- **AND** 系统 MUST 不在当前帧动态编译或混用新旧节点状态
