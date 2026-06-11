## Context
当前 FullBody 主行为域已经有两个相邻能力：

- `add-fullbody-action-framework`：把 Locomotion 和 Dodge 收束到单一 FullBody owner，保证每帧只有一个平面位移和 base layer 动画来源。
- `add-fullbody-hfsm-state-tree`：在运行时暴露 `/FullBody/Locomotion/*` 和 `/FullBody/Action/Dodge` 路径，并让 owner 来自 HFSM snapshot。

现在剩下的架构问题是数据归属。`FullBodyHfsmStateTreeBuilder` 当前仍硬编码 `FullBody`、`Locomotion`、`Action` 和 `Dodge`。`FullBodyHfsmStateIds` 也负责拼接路径。运行时路径已经像 HFSM，但“整棵树是什么”还不是一个可编辑、可校验、可替换的数据结构。

## Goals / Non-Goals
- Goals:
  - 用一个 `FullBodyHfsmTreeDefinitionSO` 表达 FullBody HFSM 的中心树数据。
  - 用内嵌 `FullBodyHfsmNodeDefinition` 表达节点，不创建一堆独立 state SO。
  - 让路径、owner 和 phase/action 绑定从节点树编译结果推导。
  - 让 builder 只消费编译后的树，不再硬编码 Locomotion、Action、Dodge。
  - 保持 Locomotion transition、Dodge 业务、Action 仲裁和输出权威在原模块内。
  - 第一版只做只读 Inspector/EditorWindow 预览和校验，不做图编辑。
- Non-Goals:
  - 不把树资产做成动作配置主入口。
  - 不把动画 clip、motion profile、interrupt policy 塞进树节点。
  - 不改 `BasicLocomotionStateMachine` 的规则。
  - 不改 `DodgeFullBodyActionModule` 的进入、tick、退出规则。
  - 不引入新的状态机库或 BBB 运行时依赖。

## Decisions
- Decision: 树资产是拓扑和绑定数据，不是动作配置资产。
  - Reason: `FullBodyActionSetSO` 只负责动作逻辑配置，动作动画绑定集只负责 `ActionStateId -> ActionAnimationProfileSO`，Locomotion 配置也已有独立入口。树资产只回答“FullBody 主树有哪些节点、节点绑定到哪个 phase/action”，避免变成混合拓扑、动画、运动和打断策略的大资产。

- Decision: 节点使用内嵌 serializable class。
  - Reason: 第一版节点数量很少，内嵌节点可以让设计者在一个资产里看到整棵树，避免 state SO 资产爆炸和引用同步成本。

- Decision: 编译器输出运行时只读树描述。
  - Reason: builder 和 driver 不应该直接遍历可变 ScriptableObject 数据。编译结果可以集中做路径、重复 ID、绑定关系和查找表，测试也更稳定。

- Decision: path 从父子节点递归计算。
  - Reason: `/FullBody/Action/Dodge` 不应由多个类手写拼字符串。路径计算集中后，编辑器预览、snapshot、pending transition 和测试可共享同一个事实来源。

- Decision: owner 从 active compiled node 的节点类型和绑定推导。
  - Reason: 当前 driver 中 `ResolveOwner` 仍假设 Action 分支就是 Dodge。迁移后 owner 应来自 active leaf 的 `LocomotionPhase` 或 `ActionStateId` 绑定。

- Decision: 第一版 Action 分支只要求 `Action.Dodge`。
  - Reason: 这是对当前行为的数据化，不借机扩展新动作。后续动作要通过新的 OpenSpec 追加节点和模块。

- Decision: `Action` 是 FullBody 主树下的子域，不是与 Locomotion 并列的第二状态机权威。
  - Reason: Locomotion 局部图和 Action module 都必须受同一个 FullBody owner 选择约束；Action 分支只表达当前全身动作叶子和 action id 绑定，不能自行提交 base layer 或平面位移。

- Decision: 编辑器第一版只读。
  - Reason: 当前最重要的是立住中心数据结构和校验。可写树编辑、拖拽、模板、图视图会扩大范围，并可能绕过校验与运行时编译路径。

## Data Shape
建议第一版节点字段保持窄接口：

- `nodeId`：稳定节点 ID，例如 `FullBody.Root`、`FullBody.Locomotion.MoveLoop`。
- `pathSegment`：路径段，例如 `FullBody`、`Locomotion`、`MoveLoop`、`Dodge`。
- `kind`：`Root`、`Composite`、`LocomotionPhase`、`Action`。
- `locomotionPhase`：仅 `LocomotionPhase` 节点有效。
- `actionStateId`：仅 `Action` 节点有效。
- `children`：内嵌子节点列表。

编译结果建议提供：

- 根节点。
- 按 `nodeId` 查询节点。
- 按完整路径查询节点。
- 按 `BasicMovementPhase` 查询 Locomotion 节点。
- 按 `ActionStateId` 查询 Action 节点。
- 当前默认起始 leaf。

## Validation Rules
- 必须有且只有一个 Root 节点。
- Root path segment 必须能生成 `/FullBody`。
- 节点 `nodeId` 不得为空且不得重复。
- 同级 `pathSegment` 不得为空且不得重复。
- 完整路径不得重复。
- `LocomotionPhase` 节点必须绑定有效 `BasicMovementPhase`。
- 每个 `BasicMovementPhase` 绑定不得重复。
- 当前默认树必须包含 Idle、MoveStart、MoveLoop、MoveStop。
- `Action` 节点必须绑定有效 `ActionStateId`。
- 每个 `ActionStateId` 绑定不得重复。
- 第一版必须包含 `Action.Dodge`。
- `Action.Dodge` 必须位于 `/FullBody/Action` 分支下。
- Composite/Root 节点不得绑定 phase/action。

## Migration Plan
1. 增加树定义数据类型和校验器，不接入运行时。
2. 增加默认树资产，表达当前运行时结构。
3. 增加编译器和 path resolver，先用纯逻辑测试锁定结果。
4. 让 builder 新增从 compiled tree 创建 HFSM 的入口，保留最小默认 fallback 只用于缺失配置时报错或测试构造。
5. 将 `PlayerFullBodyActionController` 序列化引用到树资产。
6. 将 snapshot、pending path 和 owner 解析切到 compiled node 结果。
7. 增加只读编辑器预览，显示树、路径、节点类型和绑定。
8. 更新文档，说明后续 Action 节点只能通过中心树定义接入。

## Risks / Trade-offs
- Risk: 树资产和 `FullBodyActionSetSO` 同时配置 Action，出现两个权威。
  - Mitigation: 树资产只保存 action state id 绑定；动作逻辑完整性由 `FullBodyActionSetSO` 校验，动作动画完整性由动作动画绑定集校验，新增交叉校验只检查树里的 Action 是否能在动作逻辑集中找到定义。

- Risk: builder 迁移时短期保留硬编码 fallback，形成隐藏第二路径。
  - Mitigation: 任务要求最终运行时必须由资产编译结果构建；任何 fallback 只能用于测试或明确错误提示，不作为 prefab 的长期路径。

- Risk: 可写编辑器提前扩展，绕过编译校验。
  - Mitigation: 本 change 只做只读预览；所有写入能力另开 OpenSpec。

## Open Questions
- 默认树资产命名建议为 `DefaultFullBodyHfsmTreeDefinition.asset`，如果后续需要角色差异化，可由角色 prefab 引用不同树资产。
- 当前 change 不决定 Roll/Jump/Attack 的节点模板，只预留 Action 节点绑定规则。
