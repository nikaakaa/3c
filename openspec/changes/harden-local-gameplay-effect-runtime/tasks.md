## 1. 固定本地范围与冲突

- [ ] 1.1 记录 ReplaceOldest 当前按 EffectId 跨来源搜索的调用点。
- [ ] 1.2 记录 Scheduler Additional Effect 失败原因丢失的调用点。
- [ ] 1.3 记录 Runtime Begin、Apply、Remove、Drain 的当前状态约束。
- [ ] 1.4 记录 authoring、request、Magnitude 和 Attribute 写入的 float 边界。
- [ ] 1.5 记录 Graph Apply 节点当前 source/target actor 字段。
- [ ] 1.6 记录 MotionStage 访问完整 CharacterGraphContext 的实际成员。
- [ ] 1.7 记录 CharacterPipelineHost 与 ServerAuthoritative binding 的重复 actor identity。
- [ ] 1.8 确认项目不存在正式命中 solver、目标 registry 和统一 Cue consumer，不新增绕过路径。

## 2. 修正聚合 Stack 溢出

- [ ] 2.1 将 ReplaceOldest 的移除目标改为当前达到上限的 ActiveEffect。
- [ ] 2.2 保持 Overflow lifecycle 在替换前产生。
- [ ] 2.3 保持旧 ActiveEffect 的 Modifier、Tag 和 Removed lifecycle 在同一事务撤销。
- [ ] 2.4 使用 incoming Spec 创建唯一新 ActiveEffect。
- [ ] 2.5 删除不再使用的按 EffectId 全局 oldest 查询。

## 3. 暴露生命周期执行失败

- [ ] 3.1 新增 GameplayEffectExecutionFailure 类型。
- [ ] 3.2 在 GameplayEffectChangeSet 增加 execution failure 列表。
- [ ] 3.3 让 Scheduler 保留 Additional Effect failure code 和 reason。
- [ ] 3.4 让 Scheduler 失败时回滚本次 lifecycle transaction。
- [ ] 3.5 在 transaction 结束后把 failure 写入当前 ChangeSet。
- [ ] 3.6 更新 ChangeRecorder clone、restore、drain 和 reset。
- [ ] 3.7 将 execution failure 投影到 GE diagnostics。
- [ ] 3.8 确认 execution failure 不进入 Character network fact。

## 4. 强制单 Tick ChangeSet 事务

- [ ] 4.1 在 GameplayEffectRuntime 增加 Tick open 状态。
- [ ] 4.2 上一 Tick 未 Drain 时拒绝 BeginLogicTick。
- [ ] 4.3 Tick 未 Open 时拒绝 Apply。
- [ ] 4.4 Tick 未 Open 时拒绝 Remove。
- [ ] 4.5 Tick 未 Open 时拒绝 DrainChangeSet。
- [ ] 4.6 DrainChangeSet 后关闭当前 Tick。
- [ ] 4.7 Dispose 时清理 Tick 状态且不要求额外 Drain。
- [ ] 4.8 检查 CharacterPipeline Begin/Commit 顺序满足新约束。

## 5. 收口 finite 数值

- [ ] 5.1 增加通用 finite float 判断。
- [ ] 5.2 拒绝非有限 Attribute 初始值。
- [ ] 5.3 拒绝非有限 constant Attribute bound。
- [ ] 5.4 拒绝非有限 Magnitude constant。
- [ ] 5.5 拒绝非有限 Magnitude coefficient。
- [ ] 5.6 拒绝非有限 Magnitude post-add。
- [ ] 5.7 拒绝非有限 SetByCaller value。
- [ ] 5.8 拒绝非有限 source attribute snapshot。
- [ ] 5.9 拒绝非有限 Magnitude 最终解析值。
- [ ] 5.10 拒绝 Attribute base mutation 的非有限结果。
- [ ] 5.11 拒绝 authoritative base/current 的非有限值，不改变已有状态。
- [ ] 5.12 拒绝 Attribute current 重算的非有限结果。

## 6. 建立唯一 Character ActorId

- [ ] 6.1 在 CharacterPipelineHost 增加必填 ActorId。
- [ ] 6.2 在 Host 创建 CharacterPipeline 时传入 ActorId。
- [ ] 6.3 在 CharacterPipeline 保存只读 ActorId。
- [ ] 6.4 在 CharacterGraphContext 保存只读 ActorId。
- [ ] 6.5 在 CharacterGameplayEffectAdapter 保存只读 ActorId。
- [ ] 6.6 删除 CharacterServerAuthoritativeBinding 的 SubjectActorId 序列化字段。
- [ ] 6.7 让 binding 从 CharacterPipelineHost.ActorId 读取 SubjectActorId。
- [ ] 6.8 将 Sandbox 的 LocalActor 值迁移到 CharacterPipelineHost。
- [ ] 6.9 删除场景中的 binding 旧 identity 数据。

## 7. 拆分 Character GE 端口

- [ ] 7.1 新增只读 CharacterGameplayEffectQueryPorts。
- [ ] 7.2 在 Query ports 只暴露 TagReader 和 AttributeReader。
- [ ] 7.3 新增 CharacterGameplayEffectCommandPorts。
- [ ] 7.4 在 Command ports 暴露 ApplySelf 和 RemoveSelf。
- [ ] 7.5 让 Adapter 构造 source=target=ActorId 的 Self Context。
- [ ] 7.6 保留 ActionInstanceId、PredictionKey、GameplayResultId 和 source tick。
- [ ] 7.7 删除旧 combined CharacterGameplayEffectGraphPorts。
- [ ] 7.8 更新 CharacterGraphContext 构造和属性。
- [ ] 7.9 更新 HasTag、TagQuery 和 ReadAttribute 节点使用 Query ports。
- [ ] 7.10 更新 ApplyEffect 节点使用 ApplySelf。
- [ ] 7.11 删除 ApplyEffect 节点的 SourceActorId 字段。
- [ ] 7.12 删除 ApplyEffect 节点的 TargetActorId 字段。
- [ ] 7.13 更新 RemoveEffect 节点使用 Command ports。

## 8. 收窄 Motion 能力

- [ ] 8.1 新增 ICharacterMotionContext。
- [ ] 8.2 只暴露 ActionInstance 查询和 diagnostics context。
- [ ] 8.3 让 CharacterGraphContext 实现 ICharacterMotionContext。
- [ ] 8.4 将 CharacterMotionStage 字段改为 ICharacterMotionContext。
- [ ] 8.5 将 MotionModifierContext 字段改为 ICharacterMotionContext。
- [ ] 8.6 更新 MotionWarp target 查询使用专用接口。
- [ ] 8.7 更新 Motion diagnostics 使用专用接口。
- [ ] 8.8 确认 Motion 代码不再引用 CharacterGameplayEffectCommandPorts。

## 9. 更新 current specs 与项目口径

- [ ] 9.1 更新 gameplay-effect-runtime Purpose。
- [ ] 9.2 更新单 Tick ChangeSet transaction Requirement。
- [ ] 9.3 更新生命周期 Additional Effect failure Requirement。
- [ ] 9.4 更新 Stack ReplaceOldest 语义。
- [ ] 9.5 更新 finite 数值 Requirement。
- [ ] 9.6 更新 character-gameplay-effect-integration Purpose。
- [ ] 9.7 更新 Self Graph command 与跨角色 Result 路由 Requirement。
- [ ] 9.8 更新 Motion 最小能力 Requirement。
- [ ] 9.9 更新 character-gameplay-effect-authoring Purpose 与 finite 校验。
- [ ] 9.10 更新 character-pipeline-runtime 的 ActorId 唯一来源。
- [ ] 9.11 更新 openspec/project.md 的本地 GE 口径。

## 10. 清理与自动校验

- [ ] 10.1 搜索并确认 Runtime 不再调用全局 FindOldest。
- [ ] 10.2 搜索并确认 ApplyEffect 节点不再保存 source/target actor 字段。
- [ ] 10.3 搜索并确认 CharacterServerAuthoritativeBinding 不再序列化 SubjectActorId。
- [ ] 10.4 搜索并确认 MotionStage 不再持有 CharacterGraphContext。
- [ ] 10.5 搜索并确认没有新增命中、目标 registry 或 GE 专用 Cue fallback。
- [ ] 10.6 使用项目要求的禁用 build server 参数编译 ThirdPersonGameplay。
- [ ] 10.7 使用项目要求的禁用 build server 参数编译 Assembly-CSharp。
- [ ] 10.8 编译结束后立即关闭 .NET build server。
- [ ] 10.9 执行 `openspec validate harden-local-gameplay-effect-runtime --strict --no-interactive`。
- [ ] 10.10 执行 `openspec validate --all --strict --no-interactive`。
