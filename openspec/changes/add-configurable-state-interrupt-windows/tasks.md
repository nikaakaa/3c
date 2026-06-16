## 1. Scope Lock
- [ ] 1.1 读取本变更 `proposal.md`、`design.md` 和全部 spec delta。
- [ ] 1.2 确认本变更只负责 `StateTimelinePolicy`、timeline facts 数据模型、状态请求策略数据和校验。
- [ ] 1.3 确认单帧 current/projected/target facts 使用归属 `refactor-state-timeline-facts-authority`。
- [ ] 1.4 确认 transition evaluator 插拔归属 `refactor-transition-condition-evaluators`。
- [ ] 1.5 确认 action motion output 数学归属 `refactor-state-action-motion-output`。

## 2. State Timeline Policy Model
- [ ] 2.1 定义或收口纯数据 `StateTimelinePolicyDefinition`。
- [ ] 2.2 policy 使用稳定 state id。
- [ ] 2.3 window 使用稳定 window id。
- [ ] 2.4 window 支持 normalized time domain。
- [ ] 2.5 window 支持 seconds time domain。
- [ ] 2.6 window 支持 motion、input lock、interrupt/cancel、exit kind。
- [ ] 2.7 window 支持 request type 过滤。
- [ ] 2.8 window 支持 priority、resistance、min priority 和 force。
- [ ] 2.9 window 支持 `TimelineFactId` 或等价稳定 fact id。
- [ ] 2.10 policy 和 window 不引用 MonoBehaviour、Transform、Animator、Animancer、AnimationClip、TransitionAsset、CharacterController 或场景实例。

## 3. Timeline Facts
- [ ] 3.1 定义或收口 `StateTimelineWindowFacts`。
- [ ] 3.2 facts 表达 state id、normalized time、elapsed seconds。
- [ ] 3.3 facts 表达 active window ids。
- [ ] 3.4 facts 表达 active fact ids。
- [ ] 3.5 facts 表达 request window ids。
- [ ] 3.6 facts 表达 request fact ids。
- [ ] 3.7 facts 表达 priority、resistance、min priority 和 force。
- [ ] 3.8 增加 `Contains(TimelineFactId)` 或等价查询。
- [ ] 3.9 增加 active facts 枚举测试。
- [ ] 3.10 增加 window 边界采样测试。

## 4. Request Policy Data
- [ ] 4.1 定义或收口状态请求策略数据源。
- [ ] 4.2 策略表达 from state。
- [ ] 4.3 策略表达 target state。
- [ ] 4.4 策略表达 request type。
- [ ] 4.5 策略表达 min priority。
- [ ] 4.6 策略表达 force。
- [ ] 4.7 策略表达 required fact id。
- [ ] 4.8 旧 elapsed timing rule 仅作为迁移兼容保留。
- [ ] 4.9 编译后的 runtime policy 不引用 Unity 对象。
- [ ] 4.10 Presenter、MotionExecutor、Locomotion pipeline 不直接读取策略 SO。

## 5. Validation
- [ ] 5.1 校验空 state id。
- [ ] 5.2 校验空 window id。
- [ ] 5.3 校验空 fact id。
- [ ] 5.4 校验非法 time domain。
- [ ] 5.5 校验 end 早于 start。
- [ ] 5.6 校验 normalized end 大于 1。
- [ ] 5.7 校验 request window 缺 request type。
- [ ] 5.8 校验重复 window id 并报告 warning。
- [ ] 5.9 校验 TurnBack policy 缺 motion window。
- [ ] 5.10 校验 TurnBack policy 缺 exit window。
- [ ] 5.11 校验策略 required fact id 在目标 timeline policy 中可找到。
- [ ] 5.12 校验未知 required fact id 报错，不使用 fallback。

## 6. Arbiter Compatibility
- [ ] 6.1 仲裁器继续只消费纯数据 request、context、policy 和 timeline facts。
- [ ] 6.2 仲裁器不读取 Animancer、Animator、AnimationClip 或 timeline policy asset。
- [ ] 6.3 required fact 未激活时拒绝请求。
- [ ] 6.4 required fact 激活且 priority/resistance 通过时接受请求。
- [ ] 6.5 Dodge 现有策略兼容测试保持。
- [ ] 6.6 TurnBack 请求策略兼容测试保持。

## 7. Diagnostics And Docs
- [ ] 7.1 诊断日志输出 request type、from state、target state、priority、resistance、matched policy、window id、fact id 和 rejected reason。
- [ ] 7.2 文档说明 transition priority、request priority、state resistance、window min priority 和 force 的区别。
- [ ] 7.3 文档说明 window id 用于编辑/诊断，required fact id 用于 runtime policy。
- [ ] 7.4 文档说明 visual fade、clip、speed、start time 和 TransitionAsset 不参与逻辑窗口。

## 8. Validation Commands
- [ ] 8.1 运行 `openspec validate add-configurable-state-interrupt-windows --strict --no-interactive`。
- [ ] 8.2 运行 `dotnet build .\Assembly-CSharp.csproj --no-restore /m:1 /nr:false /p:UseSharedCompilation=false /clp:ErrorsOnly`。
- [ ] 8.3 运行 `dotnet build .\Assembly-CSharp-Editor.csproj --no-restore /m:1 /nr:false /p:UseSharedCompilation=false /clp:ErrorsOnly`。
- [ ] 8.4 运行相关 StateTimelinePolicy / ActionInterrupt EditMode 测试。
- [ ] 8.5 不运行 Unity batchmode。
