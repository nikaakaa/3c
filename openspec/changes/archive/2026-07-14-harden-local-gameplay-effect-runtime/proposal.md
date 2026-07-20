# Change: 收口本地 Gameplay Effect 运行时正确性

## Why

当前 Gameplay Effect 已经形成独立 Runtime、Character Adapter、Graph 端口和 ChangeSet 投影，但本地运行仍存在会直接影响业务正确性的缺口：AggregateBySource 的 ReplaceOldest 可能删除其他来源并创建重复 StackKey；生命周期阶段触发的 Additional Effect 失败会静默回滚；显式 Remove 无法区分无匹配与 Removal Additional Effect 失败；可选 SetByCaller 会让 Effect 产生 Applied 结果却跳过部分数值修改；Additional Effect 会把父 Effect 的全部参数隐式复制给子 Effect；Runtime 没有强制一个 Tick 对应一次 Begin/Drain，也没有在未预期异常后恢复 Tick 起点；非有限数值可以进入 SetByCaller、属性初值和 Magnitude；Graph Apply 节点允许填写并不会被路由的 TargetActorId；Motion 通过完整 CharacterGraphContext 间接获得 Effect command 能力。

这些问题与具体网络模型无关。继续在当前边界上接伤害、资源消耗或 Buff 会把错误固化进项目主链。本变更只收口本地 GE 的事务、数值、Tick 和 Character 能力边界，不实现预测对账、权威 Tick 映射、packet、endpoint、命中求解或专用表现播放器。

## What Changes

- 将 ReplaceOldest 明确为替换达到上限的当前聚合 StackKey，不再跨来源搜索同 EffectId 的其他实例。
- 让生命周期阶段的 Additional Effect 提交失败产生结构化 Runtime execution failure，并保留失败原因，不再静默回滚。
- 让显式 Remove 返回 Removed、NoMatch、ExecutionFailed、InvalidRequest 或 Disposed，Removal Additional Effect 失败时回滚移除并把同一结构化 failure 写入当前 ChangeSet。
- 删除 SetByCaller 的可选参数字段，Effect 声明的参数必须全部提供；任何 Component magnitude 无法解析时整笔事务失败，不得产生半成功 Effect。
- 为 Additional Effect 引用增加显式子参数绑定，只允许从父 Effect 已声明参数或正式常量构造子请求，不再复制父参数全集。
- 让 Apply 在一次调用中只构建一次 Spec、只执行一次 Application Requirement。
- 强制 `BeginLogicTick -> Apply/Remove -> DrainChangeSet` 单 Tick 事务；上一 Tick 未 Drain 时不得开始下一 Tick，Tick 外不得执行 Effect mutation。
- 在 Tick 打开时保存状态与 ChangeSet 起点快照；Begin、Apply 或 Remove 发生未预期异常时恢复起点、关闭 Tick 并继续抛出异常。
- 在 authoring build、SetByCaller、source snapshot、Magnitude 解析和 Attribute mutation 边界拒绝 NaN、Infinity 与运算溢出结果。
- 将 Character GE Graph ports 拆为 Query ports 与 Self Command ports；Apply 节点删除手填 source/target actor 字段，只能对当前 Character 提交本地 Effect。
- 为 `CharacterPipelineHost` 增加唯一 model-neutral ActorId，并让 CharacterPipeline、GraphContext 与现有网络 binding 复用该身份；删除 binding 内重复 SubjectActorId 配置。
- 将 MotionStage 对完整 CharacterGraphContext 的依赖缩为 motion action context 与 diagnostics context，保证 Motion 无法获得 Effect command sink。
- 更新 Gameplay Effect current specs 与项目口径，明确本地 Self mutation、跨角色 Result 路由、Tick transaction 和 execution failure。

## Impact

- Affected specs:
  - `gameplay-effect-runtime`
  - `character-gameplay-effect-integration`
  - `character-gameplay-effect-authoring`
  - `character-pipeline-runtime`
- Affected code:
  - `Assets/GameScripts/Main/Runtime/Gameplay/Effects`
  - `Assets/GameScripts/Main/Runtime/Gameplay/Attributes`
  - `Assets/GameScripts/Main/Runtime/Character/Pipeline/GameplayEffect`
  - `Assets/GameScripts/Main/Runtime/Character/Pipeline/Graph`
  - `Assets/GameScripts/Main/Runtime/Character/Pipeline/Motion`
  - `Assets/GameScripts/Main/Runtime/Character/Pipeline/Runtime`
  - `Assets/GameScripts/Main/Runtime/Character/Pipeline/Unity`
  - `Assets/GameScripts/Main/Runtime/Networking/ServerAuthoritativeHybrid/CharacterServerAuthoritativeBinding.cs`
  - 现有 Sandbox binding 资产
- Breaking changes:
  - Graph ApplyEffect 节点不再保存 SourceActorId/TargetActorId。
  - CharacterPipeline 构造需要显式 ActorId。
  - ServerAuthoritative binding 不再独立保存 SubjectActorId，而是使用 CharacterPipelineHost.ActorId。
  - Tick 外 Apply/Remove 和未 Drain 就进入下一 Tick 会明确失败。
  - `GameplayEffectRemoveResult` 增加明确状态与结构化 execution failure。
  - SetByCaller 声明不再包含可选标记，全部声明参数都必须提供。
  - Additional Effect 不再继承父 Effect 参数全集，必须逐项配置子参数绑定。
- Out of scope:
  - 不修改 prediction journal、Confirm/Reject/Correct 或 authority revision 语义。
  - 不修改 GameplayEffect network fact、packet、policy、history、endpoint 或 LocalLoopback 行为。
  - 不伪造命中检测、目标注册或 GameplayResult；Corin Damage 仍等待正式命中求解/结果路由产生合法输入。
  - 不新增只服务 GE 的 VFX、Audio、受击动画或 Cue 播放器；统一 Cue 消费必须覆盖 Timeline、Graph 和 GE 的同一事实链。
