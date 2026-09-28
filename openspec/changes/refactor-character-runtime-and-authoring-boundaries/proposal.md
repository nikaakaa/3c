# 角色运行与作者底层职责整理

## Why

Pose 帧事务、Timeline 内容与播放、Session 历史恢复和作者工作区跨越多个职责；部分旧 TreeDesigner 类型、误导 UI 和重复校验尚未清理。此前把若干小步提交误报为整体完成，且一般重构计划没有对应的活跃 OpenSpec 入口，后续长任务需要一份能区分事实、候选、保留理由和证据缺口的统一方案。

## What Changes

- 建立本轮完整范围与状态台账：已提交基线、工作区未提交改动、待排查候选、明确保留的职责、跨 change 依赖分别记录。入口为本 proposal，详细排查协议和方案在 [design.md](design.md)。
- 收口 Pose 唯一帧事务的调用合同：外层提供输入并消费最终结果，由 FrameCoordinator 执行阶段门；Source、Constraint、FinalPublication 与图执行状态仍分别归原 owner。
- 核对已拆分的 Timeline 内容、表现图、快照转换和诊断模块，保留唯一播放状态、Action 时钟、TreeClip 精确身份及已编译技能执行链。
- 收口现有 SessionHistory 与 Foot AnimationSampler 未提交改动：分别分离历史恢复事务和 Unity 编辑器采样资源生命周期，不修改恢复业务规则或 Foot 数值算法。
- 整理 Pose Workspace：创建状态走现有 mutation；保存覆盖正式有／无 Profile 作者模式；校验报告携带定位信息；导航查询与页面操作分离；删除无消费者的 tuning 指纹机制。
- **BREAKING**：删除只做校验却宣称编译成功的 Pose Compile 入口和过时 Projection／Build 状态提示；经代码、序列化、资产、反射与生成代码证据确认无用的旧作者类型直接删除，不提供旧接口兼容层。
- 完整审计 TreeDesigner 的剩余依赖，区分无用 import、旧类型之间的编译依赖、反射发现和真实正式消费者。同步清理代码生成入口、程序集、命名和资产；不预设整个包必能删除。
- 整理 TimelineRuntimePreparation、FootSwingMotionBuilder 等多类型文件的归属；对 BlendStack、图和子图状态给出保留或调整的理由，不按行数强制拆类。
- 将闭环证据、Player 与 Editor 差异、0 GC 归因、探针校准、符号解析和文档偏差纳入排查视野；性能与算法专项继续由原 change 拥有，不复制工具或扩大本轮实施权限。

## Capabilities

### New Capabilities

无。不建立新框架、第二执行器、通用 Manager 或新的诊断控制面。

### Modified Capabilities

- `character-animation-pipeline`：修正现行条款中外层逐阶段消费 Pose 结果的结构描述，明确 FrameCoordinator 的整帧结果责任，保留阶段门、不可逆 Barrier 和各模块状态所有权。
- `graph-authoring-domain-framework`：明确 Pose 工作区操作、保存模式、单次领域校验报告与定位的职责，以及作者 UI 对校验、保存和实际运行采用状态的真实表达。

其余涉及能力原则上保持现行行为；Session、Foot 分析、Timeline、程序集及性能合同的对应关系和待解决偏差见 design，不为纯搬文件虚构新 capability。

## Impact

- Runtime：Character Presentation／Pose、Timeline Lifecycle、SimulationSessionHost／History，以及被证实为旧路径的作者类型和直接调用者。
- Editor：Pose Workspace、现有 mutation／validator／持久化职责、Foot AnimationAnalyzer／Sampler、C# 作者代码导出与直接程序集依赖。
- 文档：本 change 为统一规划入口；[原重构说明](../../../docs/architecture-refactoring-plan.md)改为指向这里。现行 specs 在后续明确的同步或归档流程中更新，本轮不把提案直接写成已实现合同。
- 相关专项：`eliminate-runtime-managed-allocations`、`add-compile-time-performance-instrumentation`、`design-btsmtl-authoring-runtime-workbench`、Pose 编辑预览／只读黑板、Foot 稳定化和 Timeline 时钟 change。交叉点按既有 owner 处理，保留其他窗口正确修改。
- 当前请求只授权规划文档。后续长任务首先只读排查、核对工作区并给出完整方案；本提案和 tasks 的存在不代表用户已经启动实施、性能采集、replay 或新 goal。
