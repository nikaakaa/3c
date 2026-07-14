## 1. Runnable 生命周期模型

- [x] 1.1 枚举所有 `RunnableNode.StopNode()` 调用点。
- [x] 1.2 枚举所有 `RunnableNode.OnStop()` override。
- [x] 1.3 将现有 override 分类为自然完成清理、graceful stop 或 ForceStop。
- [x] 1.4 新增非序列化 `NodeLifecyclePhase`。
- [x] 1.5 保持 `State` 只包含 None、Running、Success、Failure。
- [x] 1.6 新增稳定的 `NodeStopOriginCause`。
- [x] 1.7 新增 `NodeStopContext`。
- [x] 1.8 新增 `NodeStopStatus`。
- [x] 1.9 为 StopContext 保存 source edge/node identity。
- [x] 1.10 为 StopContext 保存可选 replacement edge/node identity。
- [x] 1.11 为 StopContext 保存 LocalLogicTick。
- [x] 1.12 为 StopContext 保存 initiator identity、immediate parent 和 propagation depth。
- [x] 1.13 保证 descendant 传播不覆盖 OriginCause。

## 2. RunnableNode 正式 API

- [x] 2.1 将自然 Success/Failure 终止改为调用 `OnCompleted(result)`。
- [x] 2.2 新增 `RequestStop(context)` 正式入口。
- [x] 2.3 新增 `UpdateStopping()` 正式入口。
- [x] 2.4 新增 `ForceStop(context)` 正式入口。
- [x] 2.5 新增 `OnStopRequested(context)` override 点。
- [x] 2.6 新增 `OnStopping(context)` override 点。
- [x] 2.7 新增 `OnStopped(context)` override 点。
- [x] 2.8 新增 `OnForceStopped(context)` override 点。
- [x] 2.9 让默认节点 stop request 同步返回 Completed。
- [x] 2.10 阻止 Stopping 节点继续调用正常 `OnUpdate()`。
- [x] 2.11 stop Completed 后将外部停止节点 State 设为 None、phase 设为 Dormant。
- [x] 2.12 stop Failed 后保留 failure debug 且不恢复正常 Update。
- [x] 2.13 删除旧 `StopNode()` 正式入口。
- [x] 2.14 删除旧 `OnStop()` 正式 override 点。
- [x] 2.15 删除旧 StopNode 兼容 alias 和旁路调用。
- [x] 2.16 将生命周期观察回调收敛为可显示 start/update/completed/stopping/stopped 的只读事件。

## 3. Composite pending stop

- [x] 3.1 为 Composite child slot 增加非序列化 pending-stop 状态。
- [x] 3.2 为 pending stop 保存 initiating StopContext。
- [x] 3.3 让 Selector Self abort 调用 RequestStop。
- [x] 3.4 让 Selector LowerPriority abort 调用 RequestStop。
- [x] 3.5 child stop Completed 时允许 Selector 本 Tick重新扫描。
- [x] 3.6 child stop Running 时禁止 Selector tick replacement。
- [x] 3.7 pending stop 完成后重新扫描所有当前条件。
- [x] 3.8 阻止 pending 旧 child 恢复正常 Update。
- [x] 3.9 child stop Failed 时让 Selector 返回 Failure。
- [x] 3.10 让 Sequence Self abort 等待当前 child stop。
- [x] 3.11 Sequence stop Completed 后返回 Failure。
- [x] 3.12 让 Parallel 单 child Self abort 等待该 child stop。
- [x] 3.13 让 Parallel 自身停止时向所有 active child 传播 stop。
- [x] 3.14 让 Parallel 等待全部 active child stop Completed。
- [x] 3.15 任一 Parallel child stop Failed 时让聚合 stop Failed。
- [x] 3.16 保持 BT edge condition graph 只做 Bool 求值。
- [x] 3.17 保持 AbortPolicy 只属于 edge 调度数据。

## 4. 通用节点传播

- [x] 4.1 将 Composite 自身 stop 迁移到统一协议。
- [x] 4.2 将 Decorator child stop 迁移到统一协议。
- [x] 4.3 将 RootNode child stop 迁移到统一协议。
- [x] 4.4 将 EnterNode child stop 迁移到统一协议。
- [x] 4.5 将 StateLifecycleNode child stop 迁移到统一协议。
- [x] 4.6 将 SubTreeNode runtime root stop 迁移到统一协议。
- [x] 4.7 让 SubTreeNode 等待内部 root pending stop。
- [x] 4.8 将 TriggerNode stop/reset 清理迁移到明确生命周期。
- [x] 4.9 将 Wait/Loop/Repeat/For 节点停止迁移到明确生命周期。
- [x] 4.10 保证未运行 child 不执行 stop callback。
- [x] 4.11 保证 ForceStop 可以递归释放所有 active descendants。

## 5. TimelineNode 生命周期

- [x] 5.1 将 Timeline 自然 Succeeded 映射到 OnCompleted。
- [x] 5.2 保证自然完成不提交 cancel request。
- [x] 5.3 将 graceful stop 映射为 CancelActivePlayback。
- [x] 5.4 让 Timeline graceful stop 同步返回 Completed。
- [x] 5.5 将 ForceStop 映射为直接取消并释放 handle。
- [x] 5.6 保持 TimelineNode 不提交 Action lifecycle。
- [x] 5.7 保持 Loop playback 在 graceful stop 时取消而非 Succeeded。

## 6. StateMachine graceful exit

- [x] 6.1 为 `StateMachineGraphRuntime` 抽取统一 source-exit 内核。
- [x] 6.2 让 State Transition 使用统一 source-exit 内核并携带 target。
- [x] 6.3 为 SMNode parent stop 增加无 target 的 RequestExit。
- [x] 6.4 让 SMNode stop pending 时返回 NodeStopStatus.Running。
- [x] 6.5 source exit 开始时停止 StateBehaviorSubTree Root。
- [x] 6.6 pending exit 期间只 tick State.OnExit。
- [x] 6.7 阻止 pending exit 期间 source Root 恢复 tick。
- [x] 6.8 阻止 pending exit 期间 target State 提前 tick。
- [x] 6.9 State Transition exit 完成后发布 owner transition并进入 target。
- [x] 6.10 Parent Tree abort exit 完成后发布 owner release并结束 SMNode。
- [x] 6.11 OnExit Failed 时让 SMNode stop Failed。
- [x] 6.12 将 SMNode ForceStop 保留为 Shutdown/Dispose/强制 Reset 路径。
- [x] 6.13 保证 ForceStop 不运行 State.OnExit。
- [x] 6.14 删除 SMNode 旧 hard-stop gameplay abort 路径。

## 7. StateExitContext 与条件读取

- [x] 7.1 新增 transient `StateExitContext`。
- [x] 7.2 写入 source State identity。
- [x] 7.3 写入可选 target State identity。
- [x] 7.4 写入可选 State Transition edge identity。
- [x] 7.5 写入 parent Tree stop source/replacement identity。
- [x] 7.6 在 State.OnExit execution scope 暴露当前 StateExitContext。
- [x] 7.7 新增纯 `StateExitCauseInfoNode`。
- [x] 7.8 将 `StateExitCauseInfoNode` 接入 ConditionRuleGraph 创建与序列化。
- [x] 7.9 新增纯 `ActionContextActiveInfoNode`。
- [x] 7.10 将 `ActionContextActiveInfoNode` 接入 ConditionRuleGraph 创建与序列化。
- [x] 7.11 新增通用 `SucceedNode` Runnable leaf。
- [x] 7.12 保证 reader node 不写黑板、不消费输入、不提交 lifecycle。

## 8. Timeline 决策事实阶段

- [x] 8.1 为 scheduler 增加 current-tick decision buffer。
- [x] 8.2 计算与正式推进一致的 playback 目标时间段。
- [x] 8.3 decision prepare 只采样 ActionWindowTrack。
- [x] 8.4 decision prepare 不修改 playback time。
- [x] 8.5 decision prepare 不修改 cycle index。
- [x] 8.6 decision prepare 不修改 presentation segment。
- [x] 8.7 decision prepare 不提交 motion、cue、camera 或 animation。
- [x] 8.8 每个 Logic Tick 开始清空 decision buffer。
- [x] 8.9 为 loop playback 保持跨 duration 边界语义。
- [x] 8.10 新增 CharacterGraphContext 当前 Tick ActionWindow typed query。
- [x] 8.11 query 按 ActionInstanceId、WindowType 和可选 WindowId 过滤。
- [x] 8.12 新增纯 `ActionWindowActiveInfoNode`。
- [x] 8.13 将 `ActionWindowActiveInfoNode` 接入 ConditionRuleGraph。

## 9. Timeline 正式提交屏障

- [x] 9.1 将 BTSMTLPhase 调整为 decision prepare、RootTree、Timeline commit。
- [x] 9.2 RootTree 后先处理 graceful/force cancel status。
- [x] 9.3 decision Window 每 Tick最多提交一次到 SyncFacts。
- [x] 9.4 decision Window 每 Tick最多提交一次到 ActionRuntime Debug。
- [x] 9.5 被取消 playback 不提交本 Tick motion。
- [x] 9.6 被取消 playback 不提交本 Tick cue/camera。
- [x] 9.7 被取消 playback 不产生新的 animation sample。
- [x] 9.8 存活旧 playback 推进到 decision prepare 对应目标时间。
- [x] 9.9 正式推进不重复提交预采样 Window。
- [x] 9.10 本 Tick新 playback 进入正式 scheduler 且最多推进一次。
- [x] 9.11 保持 outgoing pose 只来自上一正式 presentation plan。

## 10. Corin Locomotion 条件

- [x] 10.1 复用 MoveMagnitude 输入节点。
- [x] 10.2 复用 move/stop threshold ExposedProperty。
- [x] 10.3 复用 run threshold ExposedProperty。
- [x] 10.4 复用 moving-turn angle threshold ExposedProperty。
- [x] 10.5 用 Compare/And/Or/Not 组合 Stop 区间。
- [x] 10.6 用 Compare/And/Or/Not 组合 Walk 区间。
- [x] 10.7 用 Compare/And/Or/Not 组合 Run 区间。
- [x] 10.8 用通用节点组合 MovingTurn 条件。
- [x] 10.9 不新增 Corin/Locomotion 专用条件节点。

## 11. Corin Locomotion Transition

- [x] 11.1 保持 `Idle -> WalkStart` 使用 Walk 区间。
- [x] 11.2 保持 `Idle -> RunStart` 使用 Run 区间并配置稳定 priority。
- [x] 11.3 新增 `WalkStart -> RunStart`。
- [x] 11.4 新增 `WalkStart -> WalkEnd`。
- [x] 11.5 将 `WalkStart -> WalkLoop` 配置为 Completed AND Walk。
- [x] 11.6 保持 `WalkLoop -> RunStart`。
- [x] 11.7 保持 `WalkLoop -> WalkEnd`。
- [x] 11.8 新增 `WalkEnd -> RunStart`。
- [x] 11.9 新增 `WalkEnd -> WalkStart`。
- [x] 11.10 将 `WalkEnd -> Idle` 配置为 Completed AND Stop。
- [x] 11.11 新增 `RunStart -> RunEnd`。
- [x] 11.12 新增 `RunStart -> WalkLoop`。
- [x] 11.13 将 `RunStart -> RunLoop` 配置为 Completed AND Run。
- [x] 11.14 保持 `RunLoop -> RunEnd`。
- [x] 11.15 新增 `RunLoop -> WalkLoop`。
- [x] 11.16 将 `RunLoop -> MovingTurn` 限制为 Run AND Turn。
- [x] 11.17 新增 `RunEnd -> RunStart`。
- [x] 11.18 新增 `RunEnd -> WalkStart`。
- [x] 11.19 将 `RunEnd -> Idle` 配置为 Completed AND Stop。
- [x] 11.20 新增 `MovingTurn -> RunEnd`。
- [x] 11.21 新增 `MovingTurn -> WalkLoop`。
- [x] 11.22 将 `MovingTurn -> RunLoop` 配置为 Completed AND Run。
- [x] 11.23 为所有同 source Transition 配置稳定 priority。
- [x] 11.24 没有 WalkEnd 独立动画时删除伪 Timeline/clip。
- [x] 11.25 使用 Transition blend 表达无独立动画的视觉衔接。

## 12. Corin Action 连段生命周期

- [x] 12.1 将 `Attack1 -> Attack2` 配置为 Attack1Cancel AND Attack request。
- [x] 12.2 删除 `Attack1 -> Attack2` 对 StateRootCompleted 的依赖。
- [x] 12.3 新增 `Attack2 -> Attack1` 和 inline ConditionRuleGraph。
- [x] 12.4 将 `Attack2 -> Attack1` 配置为 Attack2Cancel AND Attack request。
- [x] 12.5 保持 ConditionRuleGraph 中 request 查询非消费。
- [x] 12.6 保持 target activation 为 request 唯一消费点。
- [x] 12.7 保持 `Attack1/Attack2 -> None` 使用正常完成条件。
- [x] 12.8 Attack1 OnExit 使用 ActionContextActive 条件提交 `Cancel(ComboWindow)`。
- [x] 12.9 Attack2 OnExit 使用 ActionContextActive 条件提交 `Cancel(ComboWindow)`。
- [x] 12.10 正常 Complete 后 OnExit 走无条件 Succeed。
- [x] 12.11 保证 source Action terminal 后才激活 target Action。
- [x] 12.12 将 Attack1Cancel 起点配置到动画有效区间。
- [x] 12.13 将 Attack2Cancel 起点配置到动画有效区间。
- [x] 12.14 保持 CancelWindow 时间只由 Timeline authoring。

## 13. Debug 与清理

- [x] 13.1 Debug 显示 NodeLifecyclePhase。
- [x] 13.2 Debug 显示 NodeStopCause 和 pending stop elapsed ticks。
- [x] 13.3 Debug 显示 source/replacement edge/node identity。
- [x] 13.4 Debug 显示 active/exiting/target State 和 StateExitContext。
- [x] 13.5 Debug 显示 Timeline terminal status 和 Action terminal lifecycle。
- [x] 13.6 删除旧 StopNode/OnStop 调用和命名。
- [x] 13.7 删除任何 active-window 黑板 Bool 或持久化 registry。
- [x] 13.8 删除任何状态专用打断代码节点。
- [x] 13.9 保持 RootTree 不平铺具体打断业务节点。
- [x] 13.10 保持新增状态 body 和规则图 inline-first。
- [x] 13.11 确认没有创建一次性 SubTree/ConditionRuleGraph asset。

## 14. 校验

- [x] 14.1 执行项目现有非 Unity batchmode C# 编译入口并使用禁用 build server 参数。
- [x] 14.2 编译结束后执行 `dotnet build-server shutdown`。
- [x] 14.3 运行 `openspec validate add-state-interruption-authoring-closure --strict --no-interactive`。
- [x] 14.4 实现全部完成后再将 tasks 更新为 `[x]`。
