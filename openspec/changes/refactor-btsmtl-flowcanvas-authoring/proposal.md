## Why

BTSMTL技能需要成熟的节点、端口、参数化子图和运行观察能力，同时保留既有技能编译、ActionInstance和状态恢复链。本变更只负责技能侧：FlowCanvas保存唯一正式作者图，编译产物执行，真实诊断返回编辑器显示。

PoseGraph独立由[PoseGraph提案](../refactor-character-pose-graph-architecture/proposal.md)及其[FlowCanvas文档](../refactor-character-pose-graph-architecture/flowcanvas-experiment.md)管理。本变更不决定Pose执行方案，不包含其迁移或验收任务。

## What Changes

- **BREAKING**：技能执行图、局部状态机与规则页使用FlowCanvas正式节点和连接，编译直接读取，不生成旧BaseGraph作者镜像。
- 保留`技能图 -> Semantic IR -> Numeric Program -> Session执行`，保留ActionInstance、数值后端和中断恢复；不启动FlowCanvas委托、协程或自动Update执行技能。
- 复用原生端口、节点交互、Macro接口、调用节点和导航。Capability统一提供字段、类型、role及编译合同，未登记能力不开放。
- 私有Macro由技能根拥有，共享Macro显式引用，端口身份稳定，接口变更核对调用闭包；拒绝递归和跨领域调用。
- 建立技能节点、端口、边及调用位置到编译操作和状态的同版本映射，支持精确Actor、ActionInstance、generation及调用实例观察。
- 复用原生视觉显示经过、运行、等待、完成、中断及采集值，不为高亮执行第二份图。
- 预览消费唯一Scene Play协调器；本变更只负责技能观察绑定，不建立播放器、时钟、历史seek或指令级断点。
- **BREAKING**：Document v6增加技能Macro接口、owner和调用闭包，五生命周期和整包事务不分裂；非技能领域业务语义保持不变。
- 原子迁移精确技能闭包，删除被替代且无消费者的旧作者入口；共用框架改动不得迁移或破坏其他领域。

## Capabilities

### New Capabilities

- `btsmtl-flowcanvas-authoring`：技能正式图、领域能力、Macro、直接编译、迁移与唯一写入。
- `btsmtl-flowcanvas-runtime-observation`：技能来源映射、运行实例、子图定位、原生视觉及Scene Play观察。

### Modified Capabilities

- `btsmtl-graph-core`：技能退出旧BaseGraph与Tree窗口实现要求，未迁移领域继续原合同。
- `graph-authoring-domain-framework`：技能采用原生作者基础，保持领域数据、校验与运行隔离。
- `graph-authoring-editor-shell`：技能使用原生GraphEditor及同窗口领域区域，其他领域不随之迁移。
- `btsmtl-agent-authoring-document-sync`：v6直接读写正式技能图及Macro；非技能分片不改变业务意义。

## Impact

影响技能工作区、SharedGraph能力入口、Gameplay编译前端、AgentAuthoring、技能诊断及必要框架扩展。不修改Pose作者模型、Pose编译器、Pose runtime、动画算法、IK和最终输出。

规范和在途变更对账见[design.md](design.md)。本文件由原混合变更`unify-flowcanvas-authoring-and-compiled-debug`拆出并更名，原目录移除；Pose任务不在这里以“暂缓”形式保留。
