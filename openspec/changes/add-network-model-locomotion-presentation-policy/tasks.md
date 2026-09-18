# Tasks

2026-09-18 规划：本 change 只收口 locomotion 表现层的时钟与策略装配，不修改 locomotion 模拟、KCC、Timeline 或战斗逻辑。本清单不新增独立手动验证任务。

## 1. 表现计划与准备合同

- [x] 1.1 原地扩展唯一的 `CharacterLocomotionPresentationPlan` 与 plan identity，只保留 `ClockMode`、`BodySource` 和严格模式的事实要求；删除任何新建同义 plan 或 adapter 的方案。
- [x] 1.2 定义 `PreparedCharacterLocomotionPresentationBinding` 与 `LocomotionPresentationFailureCode`，覆盖 MissingPlan、InvalidPlan、MissingBodyProfile、InvalidBodyProfile、BodySourceUnavailable、MissingMovementFact、PlanIdentityMismatch、BodySourceMismatch、LineageGenerationMismatch 和 MovementSegmentMismatch。
- [x] 1.3 将 plan identity、Body source、movement clock identity、movement segment identity 和 lineage generation 接入 Character Presentation preparation 与 Fact 提交；完成后同一角色的 Body、clock policy 与 Pose Clip Player 使用同一 prepared binding。

## 2. 时钟策略装配

- [x] 2.1 将 `CharacterPresentationDomainRuntimeFactory` 改为只接收 prepared binding，并只按 `ClockMode` 装配 `FreeRunPresentationClockPolicy` 或 `CommittedMovementPresentationClockPolicy`；locomotion 不得使用 `CommittedFollowPresentationClockPolicy`。
- [x] 2.2 从 Pose authoring、compiled descriptor 和 runtime 删除 `CharacterClipPlayerClockSource`；用 node 的 locomotion 参与关系表达资源归属，不再让 Clip 选择时钟、Body source 或纠正策略。
- [x] 2.3 保证 FreeRun 路径只消费 PresentationFrame delta，不读取 committed movement history 或额外 projector；完成后普通 Local/Prediction/Authority 角色不产生额外历史复制和第二播放器。
- [x] 2.4 保证严格跟随只读取已提交逻辑移动进度事实，并把 `CommittedMovementPlaybackClock` 保持为事实读取对象而非第三个时钟；完成后运行时只存在 Simulation Tick 与 Presentation Frame 两个时间域。

## 3. Body source、纠正与 lineage

- [x] 3.1 将 `CharacterVisualTrajectoryMode` 和全部 `TrajectoryMode` 字段重命名为 `CharacterBodyCorrectionMode` / `CorrectionMode`；`CharacterBodyPresentationProfile` 成为 Direct、BoundedCorrection 和数值的唯一 owner，删除 plan 内重复 mode 和 Factory 的双份比较。
- [x] 3.2 保持 `CharacterBodyPresentationResetReason` 为已提交 stream 事件；Body correction 只收敛 VisualRoot，不重置 locomotion 播放时间，只有 Fact 的 pose discontinuity identity 改变才重置 Pose Player。
- [x] 3.3 验证严格模式的 PlanIdentity、BodySource、movement clock identity、lineage generation 和 movement segment identity；严格模式只能消费与 binding 同 lineage 的移动事实。
- [x] 3.4 清理由 Body correction 反向写入 Simulation State、Timeline state 或 locomotion logic fact 的路径；完成后表现纠正只能改变表现域结果。

## 4. Session Composition builders

- [x] 4.1 将 plan builder 从 Presentation 程序集迁入 Session Composition；Network Model Source 只声明 capability 和 Fact，Local 不得被实现或命名为 Network Model。
- [x] 4.2 为 Local Composition 提供 `FreeRun + CommittedStream` binding，并显式选择 Local Body Profile。
- [x] 4.3 为 Prediction owner 与 Server Authority remote 提供 `FreeRun + SelectedStream` binding；纠正只来自各自显式 Body Profile，不得伪造 correction state。
- [x] 4.4 为 Rollback local 提供 `FreeRun + CommittedStream`，为 Rollback remote 提供 `FreeRun + SelectedStream`；普通回滚战斗不得自动进入严格跟随。
- [x] 4.5 为 Replay/严格重演提供 `CommittedMovement` binding，要求 Composition 显式给出 BodySource 和同 lineage 的 movement fact；缺少任一输入时返回 failure code。

## 5. Clip、资源与旧装配清理

- [x] 5.1 迁移 WalkStart、WalkLoop、RunStart、RunLoop、RunEnd、MovingTurn 等资产，使它们只声明 locomotion 参与关系，不保存 ModelId、Endpoint、Transport、Prediction、Rollback、clock 或 correction mode。
- [x] 5.2 将现有 Pose Graph、Locomotion Group、AnimationClip 和 Blend Space 资源接入 prepared binding；替换单个 Clip 不得改变角色级表现策略。
- [x] 5.3 删除旧的 Presentation 静态 plan builder、`CharacterClipPlayerClockSource`、隐式 FreeRun fallback、重复 Body correction 配置和确认不会再使用的兼容字段；仓库只保留一条 locomotion 表现装配路径。

## 6. 诊断与规范收口

- [x] 6.1 发布 plan identity、ClockMode、Body source、CorrectionMode、Profile identity、logic fact lineage、movement segment identity、reset sequence/reason 和 failure code；诊断只读且不参与策略选择。
- [x] 6.2 让 Runtime、Composition 与 Editor 共用 `LocomotionPresentationFailureCode`；问题必须区分配置错误、输入事实缺失、lineage 不匹配和表现采样异常。
- [x] 6.3 更新 `character-presentation-interpolation` current spec，说明 plan、Body source、clock policy、correction、reset 和 Session Composition 的唯一所有权；当前仓库不存在 `docs/Path`，不创建没有实际依赖的 Path 文档。
