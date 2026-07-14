# Change: 重构 Pipeline Blackboard 声明归属与运行时作用域

## Why

当前 Pipeline Blackboard 已经在 `ExposedProperty` 上声明 `Scope`、`Lifetime`、`Authority`、`SyncPolicy` 和分类，但这些元数据还没有形成真实的 authoring 与 runtime 语义：

- 角色管线编辑器在任意下钻页面都把变量面板强制指向 RootTree，Graph、State 和 Action 局部变量无法就近创建，少量局部参数也会堆进角色全局列表。
- runtime 只以裸 `string key` 保存 declaration 和 value；同名局部声明无法隔离，Graph runtime instance 也没有独立存储身份。
- State 生命周期只传 `stateId`，退出任一状态会清除全部 State 变量。Corin 的 Locomotion 与 Action StateMachine 并行运行，因此一个状态退出可能误清另一个状态机的数据。
- `ClearActionInstance(actionInstanceId)` 没有按 `ActionInstanceId` 分桶，会清除全部 ActionInstance 变量。
- 跨 Graph 使用变量依赖同 key 重复声明或字符串读取。Corin 当前 RootTree、DodgeForward body 和 DodgeBack body 分别声明了 `IsDodging`，形成重复真相。
- ConditionRuleGraph 的字符串读取节点在缺失或类型错误后仍可向端口写入零值，破坏了 current spec 要求的“无 fallback”语义。

这不是单纯增加文件夹或筛选 UI 的问题。若不先补齐 declaration ownership、显式引用和 owner-qualified runtime address，分类界面只会掩盖错误生命周期。

## What Changes

- 保持一套 `BaseExposedProperty` / Pipeline Blackboard declaration 模型和一套 `PipelineBlackboardRuntime`，不创建分类 Blackboard asset 或第二套局部黑板。
- 使用现有 declaration GUID 作为稳定 declaration identity；`BlackboardKey` 只作为所属可见命名空间内的作者键，不再作为全角色 runtime 主键。
- Character scope declaration 归属 RootTree；Graph、State、ActionInstance、Frame 等局部声明归属当前 inline/shared Graph，并由所属 Graph 序列化。
- 节点和 ConditionRuleGraph 使用显式 variable reference 绑定 declaration identity 与声明 owner；下钻 Graph 可以读取可见的 Character/上层声明，但不得复制同 key declaration。
- runtime address 由 declaration identity 与实际 owner identity 共同组成：Character runtime、Graph runtime instance、`StateMachineExecutionScope`、`ActionInstanceId` 或 local logic tick。
- State enter/exit 改为传递完整 `StateMachineExecutionScope(RuntimeId, StateId, ActivationGeneration)`；ActionInstance 和 Frame 清理也必须只清理目标 owner bucket。
- 增加明确的 scope/lifetime 合法组合与校验；`Config` 变量运行时只读，Graph runtime 生命周期使用正式 `GraphInstance` lifetime。
- 将 `DebugCategory` 收敛为 authoring/runtime 共用的层级 `CategoryPath`，编辑器按 scope、当前上下文、分类和搜索展示变量。
- Pipeline Blackboard 面板在 Graph 与 Transition selection 视图中保持可访问，并区分 `Local`、`Inherited` 与声明 owner；创建变量时写入当前合法 owner。
- 单节点常量继续使用节点字段或 `PropertyPort` 默认值，单次图内计算继续使用 ValueNode/PropertyEdge；只有跨节点、跨 tick、跨状态或需要共享调试的值才进入 Blackboard。
- 迁移 Corin 现有变量：阈值与 `IsDodging` 保持 RootTree Character declaration，Dodge body 的重复 `IsDodging` declaration 删除，所有读写节点改为显式引用 RootTree declaration。
- 删除 pipeline 内按裸 key 查找、重复 declaration 共享和错误零值回退路径，不保留兼容读取或临时桥接。

## Impact

- 受影响规格：
  - `character-pipeline-blackboard`
  - `btsmtl-graph-core`
  - `btsmtl-sm-node-authoring`
- 受影响实现：
  - `BaseExposedProperty` 元数据和变量引用模型
  - `BaseGraph` declaration ownership、克隆、注册与校验
  - `ExposedPropertyNode`、ConditionRuleGraph blackboard ValueNode
  - `PipelineBlackboardRuntime` declaration/value address、生命周期与 debug
  - `StateMachineGraphRuntime`、ActionInstance、logic tick 生命周期通知
  - BTSMTL Graph/Transition Inspector 的 Pipeline Blackboard 面板
  - Corin RootTree inline graph 变量引用与重复 declaration 清理
- 网络边界不变：Blackboard 不直接生成通用 key/value packet，只有正式 resolver 产出的 SyncFacts 可进入网络层。
- 当前 active change `fix-corin-action-lifecycle-and-dodge-interruption` 也会修改 Corin RootTree。实施时 MUST 先完成该 change，再迁移 Corin blackboard，避免两项资产重写并行发生。
- 不新增测试，不运行 Unity batchmode。

