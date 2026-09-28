# 角色运行与作者底层职责整理

## Why

Pose 帧事务、Timeline 内容与播放、Session 历史恢复和作者工作区跨越多个职责；部分旧 TreeDesigner 类型、误导 UI 和重复校验尚未清理。此前把若干小步提交误报为整体完成，且一般重构计划没有对应的活跃 OpenSpec 入口，后续长任务需要一份能区分事实、候选、保留理由和证据缺口的统一方案。

## What Changes

- R1—R10 已完成本轮只读排查，证据与实施决定见 design 文末；未实施代码、未运行 replay，实施任务保持未勾选。
- 建立本轮完整范围与状态台账：已提交基线、工作区未提交改动、待排查候选、明确保留的职责、跨 change 依赖分别记录。入口为本 proposal，详细排查协议和方案在 [design.md](design.md)。
- 收口 Pose 唯一帧事务的调用合同：外层提供输入并消费最终结果，由 FrameCoordinator 执行阶段门；Source、Constraint、FinalPublication 与图执行状态仍分别归原 owner。
- 核对已拆分的 Timeline 内容、表现图、快照转换和诊断模块，保留唯一播放状态、Action 时钟、TreeClip 精确身份及已编译技能执行链。
- 收口现有 SessionHistory 与 Foot AnimationSampler 未提交改动：分别分离历史恢复事务和 Unity 编辑器采样资源生命周期，不修改恢复业务规则或 Foot 数值算法。
- 整理 Pose Workspace：创建状态走现有 mutation；保存覆盖正式有／无 Profile 作者模式；校验报告携带定位信息；导航查询与页面操作分离；删除无消费者的 tuning 指纹机制。
- **BREAKING**：删除只做校验却宣称编译成功的 Pose Compile 入口和过时 Projection／Build 状态提示；按完整引用证据退役 TreeDesigner 整包、七个包外旧节点文件及三个 BaseTreeAsset 旧作者菜单，不提供旧接口兼容层。保留正式 Flow 节点、共用业务接口和 Snapshot 值类型。
- 完整审计 TreeDesigner 的剩余依赖，区分无用 import、旧类型之间的编译依赖、反射发现和真实正式消费者。同步清理代码生成入口、程序集、命名和资产；本轮静态证据支持完整旧簇退役，实施前仍核对新增消费者。
- 整理 TimelineRuntimePreparation、FootSwingMotionBuilder 等多类型文件的归属；对 BlendStack、图和子图状态给出保留或调整的理由，不按行数强制拆类。
- 将闭环证据、Player 与 Editor 差异、0 GC 归因、探针校准、符号解析和文档偏差纳入排查视野；性能与算法专项继续由原 change 拥有，不复制工具或扩大本轮实施权限。
- 补充排查发现的完整迁移内容：History 失败释放借用引用及既有失败优先级；新建 Graph 的统一 Undo；C# 生成实际写入 Profile 的保存／回退集合；四个失效 TimelineHost 场景组件；Pose 已提交后外围业务失败及释放的统一故障归属。

## Capabilities

### New Capabilities

无。不建立新框架、第二执行器、通用 Manager 或新的诊断控制面。

### Modified Capabilities

- `character-animation-pipeline`：修正外层逐阶段消费及 Dense 状态的旧 Program 表述，明确 FrameCoordinator 的 Pose 整帧责任和 Presentation 外围业务故障，保留阶段门、不可逆 Barrier 和各模块状态所有权。
- `graph-authoring-domain-framework`：明确 Pose 工作区操作、保存模式、UI／C# 实际写 owner 的事务、单次校验报告与定位，以及校验、保存和运行采用状态的真实表达。

其余涉及能力原则上保持现行行为；Session、Foot 分析、Timeline、程序集及性能合同的对应关系见 design。本次已有两份 delta 完整修订；Native Pose、BlendStack 和 Foot 的 companion 同步清单已给出精确 requirement 与保留约束，后续对应规划阶段再展开为 delta，不为纯搬文件虚构新 capability，也不直接覆盖现行 spec。

## Impact

- Runtime：Character Presentation／Pose、Timeline Lifecycle、SimulationSessionHost／History，以及被证实为旧路径的作者类型和直接调用者。
- Editor：Pose Workspace、现有 mutation／validator／持久化职责、Foot AnimationAnalyzer／Sampler、C# 作者代码导出与直接程序集依赖。
- 文档：本 change 为统一规划入口；[原重构说明](../../../docs/architecture-refactoring-plan.md)改为指向这里。现行 specs 在后续明确的同步或归档流程中更新，本轮不把提案直接写成已实现合同。
- 相关专项：`eliminate-runtime-managed-allocations`、`add-compile-time-performance-instrumentation`、`design-btsmtl-authoring-runtime-workbench`、Pose 编辑预览／只读黑板、Foot 稳定化和 Timeline 时钟 change。交叉点按既有 owner 处理，保留其他窗口正确修改。
- 用户已启动完整实施 goal；排查结果是实施输入，不能替代代码交付。完成全部清单、直接调用方与旧路径清理及必要检查后才结束 goal；不运行 replay，不新增测试，不创建 worktree。
