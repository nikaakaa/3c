## 1. 固定本地范围与冲突

- [x] 1.1 记录 ReplaceOldest 当前按 EffectId 跨来源搜索的调用点。
- [x] 1.2 记录 Scheduler Additional Effect 失败原因丢失的调用点。
- [x] 1.3 记录 Runtime Begin、Apply、Remove、Drain 的当前状态约束。
- [x] 1.4 记录 authoring、request、Magnitude 和 Attribute 写入的 float 边界。
- [x] 1.5 记录 Graph Apply 节点当前 source/target actor 字段。
- [x] 1.6 记录 MotionStage 访问完整 CharacterGraphContext 的实际成员。
- [x] 1.7 记录 CharacterPipelineHost 与 ServerAuthoritative binding 的重复 actor identity。
- [x] 1.8 确认项目不存在正式命中 solver、目标 registry 和统一 Cue consumer，不新增绕过路径。

## 2. 修正聚合 Stack 溢出

- [x] 2.1 将 ReplaceOldest 的移除目标改为当前达到上限的 ActiveEffect。
- [x] 2.2 保持 Overflow lifecycle 在替换前产生。
- [x] 2.3 保持旧 ActiveEffect 的 Modifier、Tag 和 Removed lifecycle 在同一事务撤销。
- [x] 2.4 使用 incoming Spec 创建唯一新 ActiveEffect。
- [x] 2.5 删除不再使用的按 EffectId 全局 oldest 查询。

## 3. 暴露生命周期执行失败

- [x] 3.1 新增 GameplayEffectExecutionFailure 类型。
- [x] 3.2 在 GameplayEffectChangeSet 增加 execution failure 列表。
- [x] 3.3 让 Scheduler 保留 Additional Effect failure code 和 reason。
- [x] 3.4 让 Scheduler 失败时回滚本次 lifecycle transaction。
- [x] 3.5 在 transaction 结束后把 failure 写入当前 ChangeSet。
- [x] 3.6 更新 ChangeRecorder clone、restore、drain 和 reset。
- [x] 3.7 将 execution failure 投影到 GE diagnostics。
- [x] 3.8 确认 execution failure 不进入 Character network fact。

## 4. 强制单 Tick ChangeSet 事务

- [x] 4.1 在 GameplayEffectRuntime 增加 Tick open 状态。
- [x] 4.2 上一 Tick 未 Drain 时拒绝 BeginLogicTick。
- [x] 4.3 Tick 未 Open 时拒绝 Apply。
- [x] 4.4 Tick 未 Open 时拒绝 Remove。
- [x] 4.5 Tick 未 Open 时拒绝 DrainChangeSet。
- [x] 4.6 DrainChangeSet 后关闭当前 Tick。
- [x] 4.7 Dispose 时清理 Tick 状态且不要求额外 Drain。
- [x] 4.8 检查 CharacterPipeline Begin/Commit 顺序满足新约束。

## 5. 收口 finite 数值

- [x] 5.1 增加通用 finite float 判断。
- [x] 5.2 拒绝非有限 Attribute 初始值。
- [x] 5.3 拒绝非有限 constant Attribute bound。
- [x] 5.4 拒绝非有限 Magnitude constant。
- [x] 5.5 拒绝非有限 Magnitude coefficient。
- [x] 5.6 拒绝非有限 Magnitude post-add。
- [x] 5.7 拒绝非有限 SetByCaller value。
- [x] 5.8 拒绝非有限 source attribute snapshot。
- [x] 5.9 拒绝非有限 Magnitude 最终解析值。
- [x] 5.10 拒绝 Attribute base mutation 的非有限结果。
- [x] 5.11 拒绝 authoritative base/current 的非有限值，不改变已有状态。
- [x] 5.12 拒绝 Attribute current 重算的非有限结果。

## 6. 建立唯一 Character ActorId

- [x] 6.1 在 CharacterPipelineHost 增加必填 ActorId。
- [x] 6.2 在 Host 创建 CharacterPipeline 时传入 ActorId。
- [x] 6.3 在 CharacterPipeline 保存只读 ActorId。
- [x] 6.4 在 CharacterGraphContext 保存只读 ActorId。
- [x] 6.5 在 CharacterGameplayEffectAdapter 保存只读 ActorId。
- [x] 6.6 删除 CharacterServerAuthoritativeBinding 的 SubjectActorId 序列化字段。
- [x] 6.7 让 binding 从 CharacterPipelineHost.ActorId 读取 SubjectActorId。
- [x] 6.8 将 Sandbox 的 LocalActor 值迁移到 CharacterPipelineHost。
- [x] 6.9 删除场景中的 binding 旧 identity 数据。

## 7. 拆分 Character GE 端口

- [x] 7.1 新增只读 CharacterGameplayEffectQueryPorts。
- [x] 7.2 在 Query ports 只暴露 TagReader 和 AttributeReader。
- [x] 7.3 新增 CharacterGameplayEffectCommandPorts。
- [x] 7.4 在 Command ports 暴露 ApplySelf 和 RemoveSelf。
- [x] 7.5 让 Adapter 构造 source=target=ActorId 的 Self Context。
- [x] 7.6 保留 ActionInstanceId、PredictionKey、GameplayResultId 和 source tick。
- [x] 7.7 删除旧 combined CharacterGameplayEffectGraphPorts。
- [x] 7.8 更新 CharacterGraphContext 构造和属性。
- [x] 7.9 更新 HasTag、TagQuery 和 ReadAttribute 节点使用 Query ports。
- [x] 7.10 更新 ApplyEffect 节点使用 ApplySelf。
- [x] 7.11 删除 ApplyEffect 节点的 SourceActorId 字段。
- [x] 7.12 删除 ApplyEffect 节点的 TargetActorId 字段。
- [x] 7.13 更新 RemoveEffect 节点使用 Command ports。

## 8. 收窄 Motion 能力

- [x] 8.1 新增 ICharacterMotionContext。
- [x] 8.2 只暴露 ActionInstance 查询和 diagnostics context。
- [x] 8.3 让 CharacterGraphContext 实现 ICharacterMotionContext。
- [x] 8.4 将 CharacterMotionStage 字段改为 ICharacterMotionContext。
- [x] 8.5 将 MotionModifierContext 字段改为 ICharacterMotionContext。
- [x] 8.6 更新 MotionWarp target 查询使用专用接口。
- [x] 8.7 更新 Motion diagnostics 使用专用接口。
- [x] 8.8 确认 Motion 代码不再引用 CharacterGameplayEffectCommandPorts。

## 9. 更新 current specs 与项目口径

- [x] 9.1 更新 gameplay-effect-runtime Purpose。
- [x] 9.2 更新单 Tick ChangeSet transaction Requirement。
- [x] 9.3 更新生命周期 Additional Effect failure Requirement。
- [x] 9.4 更新 Stack ReplaceOldest 语义。
- [x] 9.5 更新 finite 数值 Requirement。
- [x] 9.6 更新 character-gameplay-effect-integration Purpose。
- [x] 9.7 更新 Self Graph command 与跨角色 Result 路由 Requirement。
- [x] 9.8 更新 Motion 最小能力 Requirement。
- [x] 9.9 更新 character-gameplay-effect-authoring Purpose 与 finite 校验。
- [x] 9.10 更新 character-pipeline-runtime 的 ActorId 唯一来源。
- [x] 9.11 更新 openspec/project.md 的本地 GE 口径。

## 10. 清理与自动校验

- [x] 10.1 搜索并确认 Runtime 不再调用全局 FindOldest。
- [x] 10.2 搜索并确认 ApplyEffect 节点不再保存 source/target actor 字段。
- [x] 10.3 搜索并确认 CharacterServerAuthoritativeBinding 不再序列化 SubjectActorId。
- [x] 10.4 搜索并确认 MotionStage 不再持有 CharacterGraphContext。
- [x] 10.5 搜索并确认没有新增命中、目标 registry 或 GE 专用 Cue fallback。
- [x] 10.6 使用项目要求的禁用 build server 参数编译 ThirdPersonGameplay。
- [x] 10.7 使用项目要求的禁用 build server 参数编译 Assembly-CSharp。
- [x] 10.8 编译结束后立即关闭 .NET build server。
- [x] 10.9 执行 `openspec validate harden-local-gameplay-effect-runtime --strict --no-interactive`。
- [x] 10.10 执行 `openspec validate --all --strict --no-interactive`。

## 11. 收口显式 Remove 结果

- [x] 11.1 新增 GameplayEffectRemoveResultCode。
- [x] 11.2 让 GameplayEffectRemoveResult 携带状态、RemovedHandles 和 execution failure。
- [x] 11.3 区分 Removed 与 NoMatch。
- [x] 11.4 区分 InvalidRequest 与 Disposed。
- [x] 11.5 在 Remove transaction 中保留失败的 Additional Effect application。
- [x] 11.6 在 Remove transaction 回滚后构造结构化 execution failure。
- [x] 11.7 将 Remove execution failure 写入当前 ChangeSet。
- [x] 11.8 确认失败 Remove 不留下 Attribute、Tag、ActiveEffect 或 Cue 修改。
- [x] 11.9 更新 Character Self Remove 的非法 selector 结果。

## 12. 收口参数合同与 Additional Effect

- [x] 12.1 删除 GameplaySetByCallerParameterDefinition.Required。
- [x] 12.2 将 Runtime Definition 的 SetByCaller 参数集合改为无可选标记的精确集合。
- [x] 12.3 让 SpecFactory 要求全部声明参数存在。
- [x] 12.4 保持未声明和重复 SetByCaller 参数明确拒绝。
- [x] 12.5 让 Modifier persistent magnitude 解析失败触发事务回滚。
- [x] 12.6 让 Modifier execute magnitude 解析失败触发事务回滚。
- [x] 12.7 让 Execution mutation magnitude 解析失败触发事务回滚。
- [x] 12.8 新增 Additional Effect 子参数绑定 authoring 类型。
- [x] 12.9 支持从父 SetByCaller 参数绑定子参数。
- [x] 12.10 支持从有限常量绑定子参数。
- [x] 12.11 校验绑定目标属于子 Effect 声明参数。
- [x] 12.12 校验父参数来源属于父 Effect 声明参数。
- [x] 12.13 校验每个子参数恰好绑定一次。
- [x] 12.14 删除 Additional Effect 的父参数全集复制。
- [x] 12.15 使用显式绑定结果构造唯一子 ApplyRequest。

## 13. 收口 Apply 与 Tick 异常

- [x] 13.1 删除 Apply 内部对 CanApply 的重复调用。
- [x] 13.2 让 Apply 在一个 transaction 中只构建一次 Spec。
- [x] 13.3 让 Apply 在一个 transaction 中只执行一次 Application Requirement。
- [x] 13.4 保持正常拒绝结果和 Rejected lifecycle 可观察。
- [x] 13.5 将 CurrentTick 纳入 Runtime state transaction snapshot。
- [x] 13.6 在 Begin 前保存 Tick 起点 state snapshot。
- [x] 13.7 在 Begin 前保存 Tick 起点 ChangeSet snapshot。
- [x] 13.8 Begin 发生未预期异常时恢复起点并关闭 Tick。
- [x] 13.9 Apply 发生未预期异常时恢复起点并关闭 Tick。
- [x] 13.10 Remove 发生未预期异常时恢复起点并关闭 Tick。
- [x] 13.11 Tick abort 时清理 Additional Effect 临时队列。
- [x] 13.12 成功 Drain 与 Dispose 时释放 Tick 起点快照。

## 14. 更新配置与规格

- [x] 14.1 删除正式 Effect 资产中的 m_Required 序列化字段。
- [x] 14.2 确认正式资产不存在需要迁移的 Additional Effect 隐式参数。
- [x] 14.3 更新 harden change proposal 与 design。
- [x] 14.4 更新 gameplay-effect-runtime delta 与 current spec。
- [x] 14.5 更新 character-gameplay-effect-authoring delta 与 current spec。
- [x] 14.6 更新 openspec/project.md 的本地 GE 参数和失败口径。

## 15. 再次清理与自动校验

- [x] 15.1 搜索并确认不存在 SetByCaller Required 字段和读取。
- [x] 15.2 搜索并确认 Additional Effect 不再复制父参数全集。
- [x] 15.3 搜索并确认 Component 不再静默跳过 Magnitude 解析失败。
- [x] 15.4 使用项目要求的禁用 build server 参数编译 ThirdPersonGameplay。
- [x] 15.5 使用项目要求的禁用 build server 参数编译 Assembly-CSharp。
- [x] 15.6 编译结束后立即关闭 .NET build server。
- [x] 15.7 执行 `openspec validate harden-local-gameplay-effect-runtime --strict --no-interactive`。
- [x] 15.8 执行 `openspec validate --all --strict --no-interactive`。
