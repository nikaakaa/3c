# 性能探针覆盖与旧采集完整指标

本记录区分源码探针、实际织入与已采样结果。2026-09-29 补充：只修改探针声明、指标目录和织入程序集名单，未构建、编译或重新采样；以下数字全部来自旧 Player，不能解释为优化后的收益。

## 已有证据

- Capture：`capture.20260928-161732.1d0ec10f9dd94ede8de4ab5b205bc7f2`，状态 `Completed`。
- Player：`fe981685443dab7200db321d`；模式：`Span`。
- 原始 manifest 与 summary 位于 `3cDemo/Client/3C_Client/Library/Performance/Captures/capture.20260928-161732.1d0ec10f9dd94ede8de4ab5b205bc7f2/`；原报告为 `Library/Performance/Reports/overall-performance-20260929.md`。
- 双角色，1920×1080，VSync 关闭，FPS 无上限。业务父阶段包含子阶段；不同 Scope 的均值不能相加或直接当作一帧预算。
- 下表逐项转录原 summary 中全部 27 个已汇总指标，没有重算采样或筛掉低耗时条目。毫秒为包含子调用/等待的经过时间；Bytes 为整帧托管分配，不是模块归因。

| 指标 | 范围 | 样本数 | 均值 | P95 | P99 | 单位 |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| `unity.gc-allocated-in-frame` | Counter | 4304 | 102193.5987 | 170325.0000 | 346189.0000 | Bytes |
| `unity.main-thread` | RenderFrame | 4305 | 9.8182 | 13.1668 | 16.0340 | Milliseconds |
| `gameplay.presentation` | RenderFrame | 4304 | 5.4444 | 7.3132 | 8.3915 | Milliseconds |
| `presentation.animation` | RenderFrame | 4304 | 5.4387 | 7.3065 | 8.3861 | Milliseconds |
| `presentation.animation.pose-graph.evaluate` | RenderFrame | 4304 | 2.7951 | 4.5968 | 6.4256 | Milliseconds |
| `gameplay.logic` | RenderFrame | 2528 | 2.2566 | 3.3802 | 6.2455 | Milliseconds |
| `session.logic-tick` | LogicTick | 2536 | 2.2423 | 3.3452 | 6.2056 | Milliseconds |
| `simulation.pipeline.transaction` | LogicTick | 2536 | 2.2036 | 3.2775 | 5.8919 | Milliseconds |
| `simulation.pipeline.evaluate` | LogicTick | 2536 | 0.7869 | 1.2115 | 3.6854 | Milliseconds |
| `presentation.animation.foot-placement` | RenderFrame | 4304 | 0.4637 | 1.0688 | 1.6114 | Milliseconds |
| `presentation.animation.pose-graph.prepare` | RenderFrame | 4304 | 0.5528 | 0.9482 | 1.6886 | Milliseconds |
| `simulation.pipeline.finalize` | LogicTick | 2536 | 0.5056 | 0.7805 | 1.3975 | Milliseconds |
| `simulation.pipeline.world-resolve` | LogicTick | 2536 | 0.4223 | 0.6316 | 1.1206 | Milliseconds |
| `simulation.operation.ability-tick` | LogicTick | 2536 | 0.2371 | 0.3838 | 2.8517 | Milliseconds |
| `simulation.pipeline.ingress` | LogicTick | 2536 | 0.1867 | 0.2929 | 0.4732 | Milliseconds |
| `presentation.animation.full-body-ik` | RenderFrame | 4304 | 0.2179 | 0.2825 | 0.6913 | Milliseconds |
| `simulation.pipeline.external-commit` | LogicTick | 2536 | 0.1238 | 0.1765 | 0.3972 | Milliseconds |
| `presentation.animation.source-barrier` | RenderFrame | 4304 | 0.0810 | 0.1188 | 0.2036 | Milliseconds |
| `presentation.body` | RenderFrame | 4304 | 0.0559 | 0.0838 | 0.1309 | Milliseconds |
| `simulation.pipeline.schedule` | LogicTick | 2536 | 0.0296 | 0.0392 | 0.0881 | Milliseconds |
| `simulation.pipeline.checkpoint-capture` | LogicTick | 2536 | 0.0205 | 0.0276 | 0.0689 | Milliseconds |
| `gameplay.input` | RenderFrame | 4304 | 0.0204 | 0.0265 | 0.0524 | Milliseconds |
| `simulation.pipeline.egress` | LogicTick | 2536 | 0.0190 | 0.0251 | 0.0659 | Milliseconds |
| `presentation.animation.pose-graph.commit` | RenderFrame | 4304 | 0.0163 | 0.0233 | 0.0459 | Milliseconds |
| `session.input` | RenderFrame | 4304 | 0.0167 | 0.0215 | 0.0476 | Milliseconds |
| `simulation.pipeline.commit-freeze` | LogicTick | 2536 | 0.0082 | 0.0112 | 0.0347 | Milliseconds |
| `simulation.pipeline.state-publish` | LogicTick | 2536 | 0.0028 | 0.0033 | 0.0072 | Milliseconds |

原始调用点还存在零样本：FactProjection、Restore、StepOther，以及未执行的另一数值域 AbilityTick/Transaction 实现。零样本表示本次没有观测到该调用点，不能填成零成本；同一 metric 的其它实现可以有样本。

## 本次补齐的源码覆盖

| 指标 | 入口及范围 | 当前证据 |
| --- | --- | --- |
| presentation.event-graph | CharacterAnimationEventGraphHost.Update；事实输入到本帧变量输出 | 已加声明，未织入验证/采样 |
| presentation.timeline | CharacterTimelineHost.Present；采样与表现图执行 | 同上 |
| presentation.timeline.commit | CharacterTimelineHost.CommitPresentationFrame；本帧 Timeline 发布与提交 | 同上 |
| presentation.camera | CharacterCameraDomainRuntime.Present；相机计划、环境约束、Rig 应用 | 同上；不含独立 Begin/Commit 入口 |
| presentation.equipment | CharacterEquipmentDomainRuntime.Present；装备视觉同步 | 同上；不代表逻辑换装总成本 |
| simulation.character.evaluate | Fixed/Float32 CharacterEvaluationRuntime.Evaluate | 同上；两域共用指标、调用点分别记录 |
| simulation.gameplay-effect.advance | 两域 GameplayEffectTarget.Advance；周期推进 | 同上；不是 Apply/Remove 等全部 GE 成本 |
| simulation.snapshot.world-capture | 两域 SimulationWorldSnapshot.Capture；角色与世界快照生成 | 同上；可由不同父阶段调用，不指定固定父级；Tick 上下文之外的调用不能冒充逻辑 Tick 样本 |
| simulation.world.kcc | DeterministicKccWorldSolver.ResolveBatch | 同上；新增 KCC 程序集合同引用和正式织入名单 |

9 个指标对应12个方法声明。沿现有 AOP 同时服务 MarkerOnly 与 Span，Disabled 不执行业务探针。指标目录参与 Revision 哈希，新构建的探针身份会变化；原报告不可补出这些阶段的历史耗时，也不能绕过正式比较器对不同探针身份的拒绝。下一次必须确认 instrumentation manifest 实际含这些方法，再解释采集数据。

## 仍未覆盖或不能从当前数据推断

- 当前 Span 只记录时间、调用点、Actor/Tick/Frame 等信息，没有每模块 GC 字节。整帧 GC 数值不能按耗时比例分摊；分配归因需读取 Unity Profiler 的 GC.Alloc 调用栈或另行实现正式分配记录合同。本次未添加自定义 GC 计数器。
- GPU、Render Thread、渲染批次、显存、物理引擎内部、ACL native 内部、任务等待不因新增 C# Probe 自动得到完整归因，应使用相应 Unity 原生指标和 Profiler/ETW 证据。
- 当前网络、资源加载、UI、音频/特效没有本次新增的独立业务阶段探针；已有周期采集场景也未证明这些业务实际执行。需要与实际场景和关键入口对应，不能将缺失指标记成0。
- KCC 只新增批次总入口，Motor/接触细分仍未单独插桩。后续依据批次结果决定是否细分，避免直接对每骨骼/每接触/每纯函数都打桩而引入大量探针成本。
- 新探针会增加采集开销和 Span 数量；预分配缓冲容量及实际吞吐未验证，保留现有溢出报错，不修改采集门禁或限帧。

## 下次报告要求

报告必须列出所有已汇总指标，并标明未执行、没有探针、缺失数据和无法归因的范围。表中应区分 RenderFrame 与 LogicTick、包含耗时与独占采样、整帧分配与模块分配。先核对正式织入 manifest，再核对 summary 覆盖，不能只挑动画条目或把 presentation.animation 全部称为骨骼动画；该旧指标实际覆盖整个角色表现事务。

