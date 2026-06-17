## 1. Scope Lock
- [x] 1.1 读取本变更 `proposal.md`、`design.md` 和全部 spec delta。
- [x] 1.2 确认本变更只负责 `StateTimelinePolicy`、timeline facts 数据模型、状态请求策略数据和校验。
- [x] 1.3 确认单帧 current/projected/target facts 使用归属当前 `animation-phase-timeline-facts` 稳定规格。
- [x] 1.4 确认 transition evaluator 插拔归属 `refactor-transition-condition-evaluators`。
- [x] 1.5 确认 action motion output 数学归属当前 `fullbody-action-framework` 和 `character-runtime-blackboard` 稳定规格。

## 2. State Timeline Policy Model
- [x] 2.1 定义或收口纯数据 `StateTimelinePolicyDefinition`。
- [x] 2.2 policy 使用稳定 state id。
- [x] 2.3 window 使用稳定 window id。
- [x] 2.4 window 支持 normalized time domain。
- [x] 2.5 window 支持 seconds time domain。
- [x] 2.6 window 支持 motion、input lock、interrupt/cancel、exit kind。
- [x] 2.7 window 支持 request type 过滤。
- [x] 2.8 window 支持 priority、resistance、min priority 和 force。
- [x] 2.9 window 支持 `TimelineFactId` 或等价稳定 fact id。
- [x] 2.10 policy 和 window 不引用 MonoBehaviour、Transform、Animator、Animancer、AnimationClip、TransitionAsset、CharacterController 或场景实例。

## 3. Timeline Facts
- [x] 3.1 定义或收口 `StateTimelineWindowFacts`。
- [x] 3.2 facts 表达 state id、normalized time、elapsed seconds。
- [x] 3.3 facts 表达 active window ids。
- [x] 3.4 facts 表达 active fact ids。
- [x] 3.5 facts 表达 request window ids。
- [x] 3.6 facts 表达 request fact ids。
- [x] 3.7 facts 表达 priority、resistance、min priority 和 force。
- [x] 3.8 增加 `Contains(TimelineFactId)` 或等价查询。
- [x] 3.9 增加 active facts 枚举测试。
- [x] 3.10 增加 window 边界采样测试。

## 4. Request Policy Data
- [x] 4.1 定义或收口状态请求策略数据源。
- [x] 4.2 策略表达 from state。
- [x] 4.3 策略表达 target state。
- [x] 4.4 策略表达 request type。
- [x] 4.5 策略表达 min priority。
- [x] 4.6 策略表达 force。
- [x] 4.7 策略表达 required fact id。
- [x] 4.8 旧 elapsed timing rule 仅作为迁移兼容保留。
- [x] 4.9 编译后的 runtime policy 不引用 Unity 对象。
- [x] 4.10 Presenter、MotionExecutor、Locomotion pipeline 不直接读取策略 SO。

## 5. Validation
- [x] 5.1 校验空 state id。
- [x] 5.2 校验空 window id。
- [x] 5.3 校验空 fact id。
- [x] 5.4 校验非法 time domain。
- [x] 5.5 校验 end 早于 start。
- [x] 5.6 校验 normalized end 大于 1。
- [x] 5.7 校验 request window 缺 request type。
- [x] 5.8 校验重复 window id 并报告 warning。
- [x] 5.9 校验 TurnBack policy 缺 motion window。
- [x] 5.10 校验 TurnBack policy 缺 exit window。
- [x] 5.11 校验策略 required fact id 在目标 timeline policy 中可找到。
- [x] 5.12 校验未知 required fact id 报错，不使用 fallback。

## 6. Arbiter Compatibility
- [x] 6.1 仲裁器继续只消费纯数据 request、context、policy 和 timeline facts。
- [x] 6.2 仲裁器不读取 Animancer、Animator、AnimationClip 或 timeline policy asset。
- [x] 6.3 required fact 未激活时拒绝请求。
- [x] 6.4 required fact 激活且 priority/resistance 通过时接受请求。
- [x] 6.5 Dodge 现有策略兼容测试保持。
- [x] 6.6 TurnBack 请求策略兼容测试保持。

## 7. Diagnostics And Docs
- [x] 7.1 诊断日志输出 request type、from state、target state、priority、resistance、matched policy、window id、fact id 和 rejected reason。
- [x] 7.2 文档说明 transition priority、request priority、state resistance、window min priority 和 force 的区别。
- [x] 7.3 文档说明 window id 用于编辑/诊断，required fact id 用于 runtime policy。
- [x] 7.4 文档说明 visual fade、clip、speed、start time 和 TransitionAsset 不参与逻辑窗口。

## 8. Validation Commands
- [x] 8.1 运行 `openspec validate add-configurable-state-interrupt-windows --strict --no-interactive`。
- [x] 8.2 运行 `dotnet build .\Assembly-CSharp.csproj --no-restore /m:1 /nr:false /p:UseSharedCompilation=false /clp:ErrorsOnly`。
- [x] 8.3 运行 `dotnet build .\Assembly-CSharp-Editor.csproj --no-restore /m:1 /nr:false /p:UseSharedCompilation=false /clp:ErrorsOnly`。
- [x] 8.4 运行相关 StateTimelinePolicy / ActionInterrupt EditMode 测试。
- [x] 8.5 不运行 Unity batchmode。
