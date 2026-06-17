## 1. Characterization
- [x] 1.1 阅读 `CharacterFrameSubmitterGraph` 的 request/output submitter 顺序。
- [x] 1.2 阅读 `FullBodyActionFrameSubmitter` 的 Action lifecycle started/completed 路径。
- [x] 1.3 阅读 `DefaultBodyArbiter` 与 `CharacterFrameOutput` 的 Locomotion suppress 行为。
- [x] 1.4 阅读 `LocomotionFrameSubmitter` 构建 state decision 和 motion frame 的路径。
- [x] 1.5 阅读 `TurnBackMotionResolver` 与 `LocomotionMotionFactsProvider` 的 TurnBack motion 采样条件。
- [x] 1.6 阅读 Corin Locomotion graph 的 TurnBack 入口、自然出口和 timeline windows。
- [x] 1.7 新增失败测试，复现 TurnBack 中被 Dodge 抢占后不应恢复 TurnBack motion。
- [x] 1.8 新增失败测试，复现 TurnBack 中被 Dodge 抢占且无移动输入后应回 Idle。
- [x] 1.9 新增失败测试，复现 TurnBack 中被 Dodge 抢占且有移动输入后应回 MoveLoop。

## 2. Preemption Data Contract
- [x] 2.1 定义纯数据 Locomotion preemption fact。
- [x] 2.2 在 fact 中记录 source locomotion state。
- [x] 2.3 在 fact 中记录 source action id。
- [x] 2.4 在 fact 中记录 source step。
- [x] 2.5 在 fact 中记录 preemption reason。
- [x] 2.6 提供 empty / none fact。
- [x] 2.7 保证 fact 不引用 Unity object、Animancer runtime、InputAction 或 executor。
- [x] 2.8 为 fact 默认值和字段语义补 EditMode 测试。

## 3. Pipeline Wiring
- [x] 3.1 在 frame submission 或等价角色级数据合同中承载 preemption candidate。
- [x] 3.2 在 frame plan/output 中承载最终 preemption fact。
- [x] 3.3 保持 BodyArbiter 只表达输出压制和选择，不直接切 Locomotion state。
- [x] 3.4 在 FullBody Action output 构建中识别 Action lifecycle started。
- [x] 3.5 在 full-body claim 成立时提交 preemption candidate。
- [x] 3.6 限制首版 preemption source 为 `Locomotion.TurnBack`。
- [x] 3.7 确认非 transient Locomotion state 不产生 preemption fact。
- [x] 3.8 为 plan/output 携带 preemption fact 补 EditMode 测试。
- [x] 3.9 为 FullBody Action started + TurnBack 条件补 EditMode 测试。

## 4. Runtime Facts And Ports
- [x] 4.1 为 character runtime port 增加传递 preemption fact 的最小端口或等价 runtime facts 写入点。
- [x] 4.2 在 runtime adapter 中实现 preemption fact 传递。
- [x] 4.3 将 preemption fact 纳入 Locomotion graph context 可读 facts。
- [x] 4.4 保证 preemption fact 一次性消费。
- [x] 4.5 保证 preemption fact 参与 snapshot/restore 或等价 replay 输入。
- [x] 4.6 为一次性消费补 EditMode 测试。
- [x] 4.7 为 snapshot/restore 稳定性补 EditMode 测试。

## 5. Locomotion Consumption
- [x] 5.1 新增或扩展 transition condition 以判断 Locomotion preemption fact。
- [x] 5.2 保证 condition evaluator 只读取纯数据 context facts。
- [x] 5.3 在 Corin Locomotion graph 增加 `TurnBack -> MoveLoop` 抢占 transition。
- [x] 5.4 在 Corin Locomotion graph 增加 `TurnBack -> Idle` 抢占 transition。
- [x] 5.5 将抢占 transition 优先级设置高于 TurnBack 自然出口。
- [x] 5.6 保证抢占后 MoveLoop gait 仍由 Locomotion intent / Run latch 决定。
- [x] 5.7 消费抢占事实时清除 pending TurnBack intent。
- [x] 5.8 消费抢占事实时重置 TurnBack motion playback window。
- [x] 5.9 为无输入抢占退出到 Idle 补 EditMode 测试。
- [x] 5.10 为有输入抢占退出到 MoveLoop/Run latch 语义补 EditMode 测试。
- [x] 5.11 为正常 TurnBack 自然出口不回退补 EditMode 测试。

## 6. Boundary Validation
- [x] 6.1 增加静态边界测试，确认 `TurnBackMotionResolver` 不读取 Action lifecycle 或 Dodge 状态。
- [x] 6.2 增加静态边界测试，确认 FullBody Action submitter 不直接调用 Locomotion output runtime 清理状态。
- [x] 6.3 增加静态边界测试，确认 Locomotion graph 不包含 `Action.Dodge` 节点。
- [x] 6.4 增加静态边界测试，确认没有新增 fallback 配置字段。
- [x] 6.5 增加诊断测试，确认 preemption fact 可追踪 source state、source action 和 step。

## 7. Tooling Validation
- [x] 7.1 运行 `openspec validate add-locomotion-preemption-contract --strict --no-interactive`。
- [x] 7.2 运行覆盖 Character frame pipeline 的定向 EditMode 测试。
- [x] 7.3 运行覆盖 FullBody Action lifecycle / arbitration 的定向 EditMode 测试。
- [x] 7.4 运行覆盖 Locomotion state graph 的定向 EditMode 测试。
- [x] 7.5 运行覆盖 TurnBack root motion 的定向 EditMode 测试。
- [x] 7.6 运行覆盖 rollback/replay 或 snapshot restore 的相关 EditMode 测试。
