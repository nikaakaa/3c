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

### Requirement: 原生Pose节点定义必须只有一个业务真相

Pose 节点的字段、默认值、端口、局部规则、资源引用和子图依赖 MUST由同一领域定义提供给原生运行、作者 UI 和 C# API。系统 MUST不复制编译节点定义、payload DTO 或按不同入口维护规则。运行缓存和缓冲位置 MUST不进入作者字段。

#### Scenario: 编辑并执行同一种节点
- **WHEN** UI 或 C# API 配置一个合法 Pose 节点
- **THEN** 原生运行实例 MUST消费相同字段和端口定义，不能使用另一套默认值或编译镜像

### Requirement: 原生Pose校验必须保留全局约束

显式图校验与实例绑定 MUST检查可达图引用、递归、悬空引用、typed 空间、唯一输出、目标槽重复、唯一 Goal Set／FBBIK、写冲突和实际绑定容量。局部节点规则 MUST不扫描整图；全局校验 MUST不生成 IR、operation plan 或持久化工作区计划。运行热路径 MUST不重复完整静态扫描。

#### Scenario: 子图形成递归
- **WHEN** 根图、状态子图或 Linked Pose 引用形成递归
- **THEN** 校验 MUST定位完整调用链并拒绝绑定，不得在运行时按最大深度截断

#### Scenario: 图类型合法但目标写入冲突
- **WHEN** 两个局部合法节点声明冲突目标或多个最终输出
- **THEN** 全局校验 MUST报告冲突来源，不能交给运行顺序决定结果

### Requirement: 原生Pose错误必须定位作者来源

静态与运行错误 MUST提供稳定图、节点、端口或调用实例身份，以及发生阶段和原因。运行观察 MUST直接关联原生图已完成结果，不为恢复旧 SourceMap 编译隐藏程序。

#### Scenario: 已绑定节点求值失败
- **WHEN** 原生节点因资源或 Constraint 输入失败
- **THEN** 诊断 MUST定位实际作者节点与本次调用，不显示另一个不可见编译操作作为唯一来源

### Requirement: 旧Pose编译产物必须退出正式入口

正式角色、预览、资源绑定和作者工具 MUST不再要求、创建或读取旧 Pose IR／ProgramImage。迁移 MUST先删除IR／Image及专属生成／执行链与旧生成产物，再接回保留消费者；不能等原生节点全部实现才退出旧链。原生图与资源版本 MUST被明确检查，未接完整的中间状态 MUST明确失败，不能读取旧产物作为回退。

#### Scenario: 只有旧PoseImage存在
- **WHEN** 角色缺少合法原生图但磁盘上仍有旧 Image
- **THEN** 实例准备 MUST报告缺失图，不能读取旧 Image 继续运行
