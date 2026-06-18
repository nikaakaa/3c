## 0. 准备与边界确认
- [x] 0.1 重新读取 `animation-motion-source-pipeline`、`action-timeline-framework`、`character-frame-pipeline`、`formalize-animation-playback-rollback-authority`。
- [x] 0.2 重新读取 `ActionMotionResolver`、`ActionTimelineEvaluator`、`LocomotionMotionFactsProvider`、`TurnBackMotionResolver`、`CharacterFrameOutputRuntime`。
- [x] 0.3 确认实现不修改 `CharacterFramePipeline` phase 顺序。
- [x] 0.4 确认实现不新增 motion executor、animation presenter、blackboard writer 或 MonoBehaviour tick 入口。

## 1. 纯数据模型
- [x] 1.1 新增 Motion Warp policy id / mode / axis mask / rotation policy 的纯数据模型。
- [x] 1.2 新增 Warp target binding id 和当前 tick Warp target pose snapshot 的纯数据模型。
- [x] 1.3 新增 Motion Warp solver input，包含 source id、playback window、root pose snapshot、target snapshot 和 motion window。
- [x] 1.4 新增 Motion Warp solver result，表达本 tick delta、yaw、有效性、失败原因和 source step。
- [x] 1.5 增加模型 copyability 测试，确认不持有 Unity scene object、Animancer、Animator、AnimationClip、CharacterController 或 InputAction。
- [x] 1.6 增加模型测试，确认 target snapshot 不包含 target history、prediction state 或 Unity object reference。

## 2. Solver 合同
- [x] 2.1 新增纯 `MotionWarpSolver` 或批准等价 solver。
- [x] 2.2 实现无 target 时的正式无效结果，不使用默认前方点 fallback。
- [x] 2.3 实现基于当前 tick target pose snapshot 的攻击吸附 planar translation 修正。
- [x] 2.4 实现 facing correction yaw 对齐策略，限制为纯数据 yaw delta。
- [x] 2.5 实现 motion window inactive 时不输出 delta/yaw。
- [x] 2.6 增加 solver EditMode 测试覆盖 target 缺失、窗口关闭、攻击吸附平移修正、facing correction yaw 修正、边界帧。
- [x] 2.7 增加静态边界测试，确认 solver 不调用 motion executor、不写 Transform、不读取 Animancer runtime。
- [x] 2.8 增加确定性测试，确认 moving target 只通过每 tick 输入 snapshot 变化影响结果，solver 不预测、不缓存上一帧目标。

## 3. ActionTimeline Motion clip 接入
- [x] 3.1 扩展 ActionTimeline Motion payload，支持可选 warp policy 和 target binding。
- [x] 3.2 保持 `ActionTimelineEvaluator` 只输出 motion intent，不解析 target、不执行 solver。
- [x] 3.3 更新 ActionTimeline validator，缺失必需 warp payload 时报告配置错误。
- [x] 3.4 增加 ActionTimeline EditMode 测试覆盖 warp payload 命中、未命中、非法配置。
- [x] 3.5 增加静态边界测试，确认 evaluator 不引用 target provider、motion executor、Animancer 或 Transform。

## 4. Action motion resolve 接入
- [x] 4.1 扩展 `ActionMotionResolveInput`，允许传入 warp target snapshot 和播放窗口或等价纯数据。
- [x] 4.2 在 `ActionMotionResolver` 中保留现有 distance/duration 行为作为无 warp payload 路径。
- [x] 4.3 在有 warp payload 时调用共享 Motion Warp solver input/result 并转换为 `ActionMovementCommand`。
- [x] 4.4 增加测试确认现有 Dodge Directional / Backstep motion 输出不回退。
- [x] 4.5 增加测试确认 warp result 经 `ActionMotionResolveResult` 进入 `CharacterFrameSubmission`。
- [x] 4.6 增加攻击吸附和转向修正的 Action motion resolve 测试。

## 5. Locomotion motion source 接入
- [x] 5.1 为现有 TurnBack motion source 建立 adapter 对照，确认它能映射到 Motion Warp solver input/result 或保持兼容边界。
- [x] 5.2 保持 `TickSampledMotion` 播放窗口来自可恢复播放状态，不从 Presenter runtime state 直接采样。
- [x] 5.3 增加测试确认 TurnBack 仍通过 `BasicMovementMotionFacts` / `MovementCommand` 进入 motion executor。
- [x] 5.4 增加测试确认 Locomotion adapter 不读取 Action lifecycle 私有状态。
- [x] 5.5 增加测试确认 Locomotion 与 Action 共享 solver input/result，但不强行合并 `MovementCommand` / `ActionMovementCommand`。

## 6. Character frame 输出边界
- [x] 6.1 确认 motion warp 结果在 output apply 前进入候选 command，而不是由 output applier 临时求解。
- [x] 6.2 增加测试确认 `CharacterFramePlan` 压制 Locomotion 时 warp motion 不会绕过计划被执行。
- [x] 6.3 增加测试确认 output applier 仍只调用现有 motion executor。
- [x] 6.4 增加静态搜索测试，确认没有新增 `CharacterController.Move` 调用路径。

## 7. 验证
- [x] 7.1 运行 Motion Warp solver 定向 EditMode 测试。
- [x] 7.2 运行 ActionTimeline Motion clip 定向 EditMode 测试。
- [x] 7.3 运行 Action motion resolve 定向 EditMode 测试。
- [x] 7.4 运行 Locomotion TurnBack motion source 定向 EditMode 测试。
- [x] 7.5 运行 Character frame arbitration / output 定向 EditMode 测试。
- [x] 7.6 运行 `openspec validate add-motion-warping-solver-boundary --strict --no-interactive`。
