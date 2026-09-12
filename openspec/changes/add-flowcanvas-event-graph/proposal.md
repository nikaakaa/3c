## Why

项目已有 FlowCanvas 的事件、变量、计算、分支和执行能力，但没有让动画作者直接用这些能力更新变量、再交给 PoseGraph 使用的正式接入。现在以动画为第一个应用建立可复用的可视化事件图：以后关卡流程等系统提供自己的事件和操作即可接入，不重复建设画布、节点或事件执行器。

## What Changes

- 直接复用原生 FlowScript、GraphEditor、事件入口、执行线、数值节点、分支、GetVariable/SetVariable、Blackboard 和 Macro；事件图由 FlowCanvas 原生执行，不增加事件图 Compiler、IR 或另一套指令执行器。
- 建立不依赖动画的宿主合同，明确事件、只读输入、可写变量、实例生命周期和错误结果；不同系统各有自己的图实例，不把通用事件图等同于全局广播总线。
- 首个动画宿主只接初始化和每帧更新、已有角色表现事实与本次更新时间。作者自己创建变量、计算与 Get/Set；不增加关卡宿主、动画播放指令、Gameplay 写入或额外事件集合。
- 动画变量唯一声明与存储沿用原生 Blackboard；PoseGraph 从同一声明读取一次完整更新后发布的只读值。每个动画实例拥有独立变量与原生节点状态，角色事实和素材曲线不变成它的可写变量。
- **BREAKING**：正式区分原生 EventGraph 输入生产与编译 PoseGraph 求值，修正共享框架“所有领域作者图必须编译”的约束；保留 Skill、Pose 的现有编译运行，不启用它们的原生作者图。
- **BREAKING**：以正式动画变量输出合同替换三个固定 motor 参数的生产接口；在全部消费者迁移后删除旧参数桥及补值路径。Pose Get、作用范围、typed 消费和曲线清理由独立 `refine-pose-graph-readonly-blackboard` 实现，不在两个 change 重复维护。
- 新图、原生变量、宿主引用和 Macro 闭包接入共享 Capability、现有 Document/Mutation 与正式资产事务；显式 Build 校验接口、发布依赖，运行不临时补建。

## Capabilities

### New Capabilities

- `flowcanvas-event-graph`：可复用的原生事件图、宿主合同、变量身份、实例执行与作者生命周期。
- `character-animation-event-graph`：动画初始化/更新事件、只读角色事实、动画变量输出与 Pose 消费侧的交接。

### Modified Capabilities

- `graph-authoring-domain-framework`：允许明确声明的原生 EventGraph 执行域，保留编译领域和统一作者语义的边界。
- `character-animation-pipeline`：加入唯一动画变量生产入口，区分输入更新完成与 Pose 提交完成，保持原 Pose 事务及最终写入链。
- `btsmtl-agent-authoring-document-sync`：事件图、变量与 Macro 进入现有 Character Presentation 文档闭包和同一资产事务。

## Impact

- 通用接入：原生 FlowCanvas 扩展点、领域无关宿主合同和实例驱动；动画数据只在 Character 动画宿主内出现。
- 动画生产侧：`CharacterPresentationRuntimeFactory`、`CharacterSimulationPresentationRuntime`、固定参数帧生产及生命周期、错误和输出映射。
- 作者与 Document：原生 GraphEditor/Blackboard 适配、共享 Capability、PresentationDocument、typed Mutation/Validator 及依赖修订。
- 关联边界：本 change 唯一拥有变量声明、更新和输出合同；[Pose 只读输入 change](../refine-pose-graph-readonly-blackboard/proposal.md)拥有 Get、输入范围、消费者编译与曲线迁移。两者完成接口接入前，不宣称动画闭环已完成。
- 不接管 Skill/FSM、关卡内容、Timeline/Montage、Foot/IK 算法、Source 资源改革或整个 Pose runtime。实际动画内容只使用明确的正式角色/Fixture，不为了示例改写 Corin 状态机。
- 本次仅产出 proposal、design、delta specs 和 tasks。文档待用户审阅；没有实施代码，不新增测试。现行规范冲突和未决内容集中列在 design。
