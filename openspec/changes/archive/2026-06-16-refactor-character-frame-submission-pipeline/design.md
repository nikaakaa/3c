## Context
当前正式角色链路已经有固定 simulation phase，并且 `CharacterFramePipeline` 能按 `ReadInput -> UpdateInputBuffer -> GameplayDecision -> BuildMotion -> ExecuteMotion -> PresentationBridge -> WriteSnapshotAndEvents` 编排单个 FullBody 行为域。

旧问题是 `FullBodyFramePipeline` 同时承担最高一帧管线和 FullBody 领域实现两种身份。当前代码已经将最高调度提升为 `CharacterFramePipeline`，但仍需要用规范和测试固定：只要未来出现 UpperBody、LowerBody 或其它并列身体域，它们也只能提交数据，不能重新拥有 pipeline phase。

## Goals
- 只有一个正式角色帧管线拥有 phase 顺序。
- FullBody、Locomotion、Action 和后续 UpperBody、LowerBody 都只能提交纯数据结果。
- 状态机前的 request submission 与状态机后的 frame output submission 必须分离。
- 运动执行、动画提交、输入消费、runtime facts 写入、snapshot/events commit 只能由唯一管线的统一提交阶段执行。
- 第一阶段保持当前 FullBody-only 行为不变。
- 为后续并行身体域预留提交模型，不在本变更中实现并行状态机。

## Non-Goals
- 不实现 UpperBody、LowerBody、Facial、IK、Additive 或 AvatarMask layer。
- 不改变当前 Dodge、TurnBack、MoveStart、MoveLoop、MoveStop 的行为规则。
- 不新增第二套角色控制器、第二套状态机 runtime 或第二条 motion executor 路径。
- 不把行为树、AI 决策树或第三方状态机 engine 接入正式主线。

## Decisions

### Decision: `Pipeline` 只保留给 Character 层
正式代码中只有 `CharacterFramePipeline` 或等价角色级模块拥有 phase 顺序。FullBody、Locomotion、Action、UpperBody、LowerBody 只能以 submitter、builder、resolver、adapter 等角色存在。

理由：Pipeline 的 Interface 包含 phase 顺序、提交时机和副作用 commit 权限。如果多个域都叫 pipeline，维护者会很难判断谁能消费输入、谁能写 runtime facts、谁能执行 motion。

### Decision: 角色级 Pipeline 必须物理归属到 `Character/Pipeline`
`CharacterFramePipeline`、`CharacterFrameInput`、`CharacterFrameContext`、`CharacterFrameResult`、`CharacterFrameSubmission`、`CharacterFrameOutput` 和 `ICharacterFrameRuntimePort` MUST 位于 `Assets/Scripts/Character/Pipeline/Model|Runtime|Contracts/...` 或等价角色级目录。FullBody 目录只保留 `FullBodySubmissionBuilder`、`FullBodyRuntimePortAdapter`、FullBody request provider/factory/resolver 和 FullBody 领域配置/诊断适配。

理由：如果角色级类型继续住在 `Action/FullBody` 目录，即使类型名已经改成 Character，维护者仍会认为最高管线是 FullBody 私有实现，后续 UpperBody/LowerBody 会继续复制局部 pipeline。

### Decision: 直接重命名现有局部 pipeline
原 `FullBodyFramePipeline` 直接迁移为 `FullBodySubmissionBuilder`，不保留一个变更周期的 obsolete pipeline 外壳作为正式路径。原 `LocomotionFramePipeline` 直接迁移为 `LocomotionFrameBuilder`。

理由：继续保留局部 pipeline 名称会让维护者误以为这些 Module 仍拥有 phase。既然正式规则已经确定只有 Character 层拥有 pipeline，就应在迁移中直接改名并同步测试。

### Decision: 提交分为 request submission 和 frame output submission
状态机前只收集 request submission，例如 Dodge、TurnBack、Attack、Jump 或外部请求候选。这些请求进入统一请求/打断仲裁，产出 accepted `CharacterInputRequestFact` 或等价事实。状态机后只收集 frame output submission，例如状态帧、运动提案、动画提案、输入消费提案、runtime facts 和 diagnostics。

理由：请求提交解决“本帧想做什么、能不能进入”的准入问题；帧输出提交解决“状态已确定后本帧写什么”的合成问题。混成一个提交接口会让 request provider 有机会绕过状态机直接写副作用。

### Decision: 提交者只提交纯数据
FullBody 首个 frame output 提交者输出当前已有的 `CharacterStateMachineFrame`、`BasicLocomotionFrame`、`ActionMotionResolveResult` 或等价聚合结果，但不得直接执行 motion、播放动画、消费输入缓冲或写 runtime blackboard。角色级提交结果命名为 `CharacterFrameSubmission` 或等价 Character 语义。

理由：提交模型把“局部计算”和“最终副作用”分开，后续 UpperBody/LowerBody 只需要新增提交来源，不需要复制 phase。

### Decision: Compose 先于 Apply
唯一管线必须先收集所有提交，再由 composer 选择最终 motion、animation、input consume、runtime facts、diagnostics 和 snapshot 写入内容，最后由 applier 执行副作用。

理由：即使第一阶段只有 FullBody 一个来源，也要先建立裁决位置。删除 composer 会让裁决逻辑回流到各提交者，未来并行身体域会再次分裂。

### Decision: 兼容入口保留但必须转发
`PlayerFullBodyActionController.Tick`、`FullBodyActionTickAdapter` 和 rollback 兼容入口可以保留，但它们必须进入同一个角色帧管线，不得继续分别驱动 FullBody phase 或 Locomotion-only phase。

理由：当前测试和场景依赖兼容入口。迁移要先稳定调用关系，再逐步改名和收敛内部 Module。

### Decision: 第一阶段只迁移结构，不引入并行语义
本变更的验收目标是 FullBody-only 行为输出一致、提交点唯一、静态边界清楚。UpperBody/LowerBody 的状态机、动画层和遮罩规则必须另开 OpenSpec。

理由：一次同时做唯一管线和并行身体域会扩大风险，也容易让新抽象未经验证就承载复杂行为。

## Risks / Trade-offs
- 风险：Locomotion 侧后续拆分仍可能把 builder、runtime 和 output 三种职责混回一个大 proposal。
  - Mitigation: 旧 `refactor-locomotion-frame-pipeline-mainline` 已拆为 `refactor-locomotion-frame-runtime-modules` 与 `refactor-locomotion-output-runtime-modules`；本变更只固定 `LocomotionFrameBuilder` 命名和 Character frame phase 归属。
- 风险：只做 FullBody-only 迁移时 composer 看起来像空壳。
  - Mitigation: composer 第一版只处理一个提交来源，但测试必须固定它是唯一副作用前的裁决点。
- 风险：直接重命名影响测试和调用点较多。
  - Mitigation: 实施前运行 GitNexus impact analysis，按调用链迁移，不保留并行正式入口。
- 风险：request submission 和 frame output submission 接口混淆。
  - Mitigation: spec 和静态测试必须证明 request provider 不执行副作用，frame output submitter 不做准入仲裁。

## Migration Plan
1. 建立 `CharacterFramePipeline` 的输入、上下文、提交和结果模型。
2. 建立 request submission 模型，把 Dodge、TurnBack 和外部请求候选收敛到统一请求/打断仲裁入口。
3. 建立 `CharacterFrameSubmission` 模型，把当前 FullBody 一帧输出包成角色级帧提交。
4. 固定 `FullBodySubmissionBuilder` 作为 FullBody 提交构建器。
5. 固定 `LocomotionFrameBuilder` 作为 Locomotion 局部帧构建器。
6. 把 `RunExecuteMotion` 和 `RunPresentationBridge` 的副作用调用迁入角色级 output composer/applier。
7. 让 `PlayerFullBodyActionController.Tick` 和 `FullBodyActionTickAdapter` 进入唯一角色帧管线。
8. 保留 FullBody-only 行为回归测试，证明迁移前后关键输出一致。

## Resolved Decisions
- `LocomotionFrameBuilder` 是 Locomotion 侧正式 builder 命名。
- `FullBodySubmissionBuilder` 是 FullBody 侧正式 submitter/builder 命名，不保留 obsolete pipeline 外壳作为正式路径。
- `CharacterFramePipeline`、角色帧模型和 `ICharacterFrameRuntimePort` 的正式物理目录是 `Assets/Scripts/Character/Pipeline/...`。
- 角色级帧输出提交命名采用 `CharacterFrameSubmission` 或等价 Character 语义。
- 外部请求、Dodge、TurnBack 和后续 Attack/Jump 进入统一 request submission，再进入请求/打断仲裁；它们不属于 frame output submission。
