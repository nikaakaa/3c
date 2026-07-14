## 1. 已完成的播放权威基础

- [x] 1.1 定义 `AnimationHandoffRole.Unspecified`、`None` 与 `Driver`
- [x] 1.2 让 None 不保存有效 strategy payload
- [x] 1.3 让 Driver 显式保存 Immediate、ContributionCrossFade 或 Inertialization definition
- [x] 1.4 定义 `AnimationLayerOutputPolicy.Unspecified`、`RequireOutput` 与 `AllowEmpty`
- [x] 1.5 将动画 layer definition 收敛到 `CharacterPipelineDefinition`
- [x] 1.6 定义 `DesiredAnimationLayerCandidate`
- [x] 1.7 定义逐层 `AnimationLayerPlaybackOutput`
- [x] 1.8 删除 `AnimationTransitionDomainId`
- [x] 1.9 删除 `CharacterAnimationTransitionRuntime`
- [x] 1.10 让 StateMachine 发布 source/target logical owner 与 resolved presentation leaf
- [x] 1.11 让 StateMachine 发布 None/Driver handoff facts 与 `AnimationOwnerReady`
- [x] 1.12 保持 State/Timeline/Action 在逻辑 stop barrier 内结束
- [x] 1.13 建立每个 LayerId 的持久 Final/Held output 与唯一 ActiveHandoff
- [x] 1.14 让 Presenter 只消费逐层最终 output
- [x] 1.15 将 Corin Base 配置为 RequireOutput 并完成 None/Driver 资产迁移

## 2. 已完成的嵌套 Owner 生命周期基础

- [x] 2.1 区分 StateMachine execution path 与最后 presentation leaf
- [x] 2.2 记录嵌套 state activation 的 parent owner relation
- [x] 2.3 让祖先 scope 重入不覆盖最后 descendant presentation leaf
- [x] 2.4 让 Timeline 只在正式提交 animation contribution 时传播 producer leaf
- [x] 2.5 让 owner release 不破坏已经提交的 resolved handoff identity
- [x] 2.6 将 `AnimationOwnerReady` 明确为 activation 的单调执行事实
- [x] 2.7 让 ready 与 owner release 同批时不丢失已发生的 readiness
- [x] 2.8 保持 CrossFade/Inertialization 从当前最终视觉结果接管
- [x] 2.9 保持同一 layer 最多一个 ActiveHandoff
- [x] 2.10 保持 Timeline Preview 使用私有 Registry、Arbitrator、LayerRuntime 与 Presenter

## 3. Ordered Commit 合同

- [x] 3.1 定义 `AnimationLayerPlanKind.InitialSeed`
- [x] 3.2 定义 `AnimationLayerPlanKind.Update`
- [x] 3.3 定义 `AnimationLayerPlanKind.Hold`
- [x] 3.4 定义 `AnimationLayerPlanKind.Handoff`
- [x] 3.5 定义 `AnimationLayerPlanKind.Empty`
- [x] 3.6 定义 `AnimationLayerPlanKind.Invalid`
- [x] 3.7 定义每层唯一 `AnimationLayerPlan` 合同
- [x] 3.8 在 LayerPlan 中保存完整 DesiredCandidate
- [x] 3.9 在 LayerPlan 中保存 Hold/Invalid 原因
- [x] 3.10 定义 `AnimationHandoffPlan` 的 FromOwners、ToOwners 与 selected Driver
- [x] 3.11 在 HandoffPlan 中保存 strategy definition 与 supersede 标记
- [x] 3.12 在 HandoffPlan 中保存 ordered command 首末位置与 record identities
- [x] 3.13 定义 Selected、Coalesced、Retired 与 Conflict causal disposition
- [x] 3.14 定义 LayerRuntime 提供给 Arbitrator 的只读 playback snapshot
- [x] 3.15 保持 `AnimationLifecycleCommand` 为唯一 ordered envelope，不新增平行 wrapper

## 4. 有序 Handoff Ledger

- [x] 4.1 让 `CharacterAnimationLayerArbitrator` 私有拥有 handoff ledger
- [x] 4.2 让 ledger 接收完整 `LocalLogicTick + phase + Sequence`
- [x] 4.3 让 ledger 同时保留 Role=None 与 Role=Driver records
- [x] 4.4 让 ledger 按 activation generation 区分重复 State 进入
- [x] 4.5 让 logical target 与后续 logical source 精确连接
- [x] 4.6 让正式 Ready leaf 的 resolved target/source 精确连接
- [x] 4.7 拒绝按 display name、Graph 布局或共同祖先猜测连接
- [x] 4.8 拒绝逆序 record 形成因果边
- [x] 4.9 在 ledger 中保存 OwnerReady 的单调事实
- [x] 4.10 在 ledger 中保存 owner release 标记
- [x] 4.11 让 ready 与 release 同批时先参与 commit 再进入清理判断
- [x] 4.12 让跨 PresentationFrame 的未决链继续保留
- [x] 4.13 让 reset、deactivate 与 dispose 清理完整 ledger
- [x] 4.14 让 Preview seek 与 target switch 清理私有 ledger

## 5. 因果链归并与 Layer 仲裁

- [x] 5.1 保留现有 contribution priority allocation 与 DesiredCandidate 计算
- [x] 5.2 从当前 playback snapshot 到 DesiredCandidate 搜索唯一有向因果路径
- [x] 5.3 让 None record 只桥接 topology 而不提供 strategy
- [x] 5.4 选择通往最终目标路径中的最后一个 Driver
- [x] 5.5 将同一路径内更早 Driver 标记 Coalesced
- [x] 5.6 在路径没有 Driver 时生成 Invalid plan
- [x] 5.7 在同一组件存在不唯一末端路径时生成 Invalid plan
- [x] 5.8 按当前 FinalOutput source priority 计算 source-side component authority
- [x] 5.9 按 DesiredCandidate target priority 计算 target-side component authority
- [x] 5.10 在两端都可见时取组件最高 endpoint authority
- [x] 5.11 将较低 authority 的独立组件标记 Retired
- [x] 5.12 将相同最高 authority 的多个独立组件标记 Conflict
- [x] 5.13 禁止用 Sequence 决定独立组件胜负
- [x] 5.14 在 target 未 Ready 时输出 Hold plan并保留因果链
- [x] 5.15 在 RequireOutput incoming 未形成时输出 Hold plan
- [x] 5.16 为 InitialSeed、同 owner Update 与 AllowEmpty 生成无 Handoff 的正式 plan
- [x] 5.17 为可见 owner 变化生成且只生成一个 HandoffPlan
- [x] 5.18 在所有 layer 不再引用 record/ready/release 后确定性清理 ledger

## 6. LayerRuntime 播放执行收口

- [x] 6.1 将 LayerRuntime 入口改为逐层 Apply `AnimationLayerPlan`
- [x] 6.2 让 InitialSeed plan 建立第一份合法 FinalOutput
- [x] 6.3 让 Update plan 更新同 owner visual samples
- [x] 6.4 让 Hold plan 保持 HeldOutput
- [x] 6.5 让 Invalid plan 保持最后合法 output 并公开错误
- [x] 6.6 让 Empty plan只处理正式 AllowEmpty 输出
- [x] 6.7 让 Handoff plan执行 Immediate
- [x] 6.8 让 Handoff plan执行 ContributionCrossFade
- [x] 6.9 让 Handoff plan执行 Inertialization
- [x] 6.10 让 supersede plan从当前 FinalOutput 重新 capture
- [x] 6.11 保持同一 layer 最多一个 ActiveHandoff
- [x] 6.12 删除 LayerRuntime `m_FrameDrivers`
- [x] 6.13 删除每层 `m_PendingDrivers` 与 `m_MatchingDrivers`
- [x] 6.14 删除 LayerRuntime endpoint-only `TryGetMatchAuthority`
- [x] 6.15 删除 LayerRuntime ReadyLeaves 与 ReleasedReadyOwners
- [x] 6.16 删除 raw intent、owner ready 与 owner release 的 Resolve 参数
- [x] 6.17 删除按 source/target 任一端命中就启动 handoff 的路径

## 7. PresentationStage、Preview 与 Diagnostics

- [x] 7.1 删除 Stage 的裸 `m_HandoffIntents` buffer
- [x] 7.2 让 Stage 将完整 ordered command records 交给 Arbitrator
- [x] 7.3 让 Stage 在 Registry snapshot 后一次生成全部 LayerPlans
- [x] 7.4 让 Stage 每层只调用一次 LayerRuntime Apply
- [x] 7.5 保持 Presenter 每个表现帧只应用一次最终 outputs
- [x] 7.6 保持 command batch 在 plan/output 成功提交后才 acknowledge
- [x] 7.7 更新 Preview runtime 使用相同 LayerPlan commit 链路
- [x] 7.8 让 Preview 连续播放使用 Update plan
- [x] 7.9 让 Preview 非连续 seek 使用 reset 后 InitialSeed
- [x] 7.10 更新 AnimationLayerFrameSnapshot 展示 ordered record range
- [x] 7.11 更新 snapshot 展示 causal components 与 disposition
- [x] 7.12 更新 snapshot 展示 LayerPlan kind、selected policy 与 Hold/Invalid 原因
- [x] 7.13 保留 ActiveHandoff elapsed、duration、FinalOutput 与 weights 调试
- [x] 7.14 删除裸 PendingDriver/DriverIds 调试口径
- [x] 7.15 更新 animation runtime trace 使用 record、component、plan 与 playback 四层事件
- [x] 7.16 更新 Host Inspector 使用新的 layer plan 观察合同

## 8. 清理、文档与静态校验

- [x] 8.1 使用 `rg` 确认 LayerRuntime 不再引用 `PendingDriver`
- [x] 8.2 使用 `rg` 确认不存在 endpoint-only Driver matcher
- [x] 8.3 使用 `rg` 确认 raw HandoffIntent 列表不再进入 LayerRuntime
- [x] 8.4 使用 `rg` 确认没有新增 fallback、兼容或第二套 handoff runtime
- [x] 8.5 更新 `openspec/project.md` 的动画表现主链路
- [x] 8.6 更新 active diagnostics change 的旧 TransitionRuntime/Driver trace 术语
- [x] 8.7 使用 `dotnet build 3C_Client.sln --disable-build-servers /nr:false /p:UseSharedCompilation=false` 编译受影响 assemblies
- [x] 8.8 编译结束后立即执行 `dotnet build-server shutdown`
- [x] 8.9 处理编译发现的旧接口引用并再次使用 required flags 编译
- [x] 8.10 再次编译结束后立即执行 `dotnet build-server shutdown`
- [x] 8.11 运行 `openspec validate refactor-animation-layer-playback-authority --strict --no-interactive`
