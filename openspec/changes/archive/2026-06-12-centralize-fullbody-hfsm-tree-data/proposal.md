# Change: 中心化 FullBody HFSM 树数据

## Why
当前 `add-fullbody-hfsm-state-tree` 已让运行时路径表现为 `/FullBody/Locomotion/MoveLoop` 和 `/FullBody/Action/Dodge`，但树结构仍由 `FullBodyHfsmStateTreeBuilder.Create()` 在代码里硬编码。Locomotion 分支、Action.Dodge 分支、路径拼接和 owner 推导仍分散在 builder、driver 和状态 ID helper 中。

这会让后续编辑器只能观察运行时结果，不能编辑或校验“整棵 FullBody HFSM 树”。后续 Roll、Jump、Attack 等动作继续接入时，也容易回到每个动作在代码里补一段分支的分裂路径。

## What Changes
- 新增 `FullBodyHfsmTreeDefinitionSO`，用一个中心资产表达当前 FullBody HFSM 层级树。
- 新增内嵌可序列化节点 `FullBodyHfsmNodeDefinition`，节点承载稳定 ID、路径段、节点类型和可选绑定。
- 当前默认资产表达 `FullBody -> Locomotion(Idle/MoveStart/MoveLoop/MoveStop) -> Action(Dodge)` 结构。
- 路径、owner 和 Action/Locomotion 绑定从编译后的节点树推导，不再在 builder/driver 中手写拼接。
- `FullBodyHfsmStateTreeBuilder` 或等价 builder 改为消费编译后的树定义，而不是硬编码 Locomotion、Action、Dodge 节点。
- 新增树定义校验、编译器、路径解析测试和只读编辑器预览。

## Impact
- Affected specs:
  - `fullbody-hfsm-tree-data`
  - 依赖活跃变更 `add-fullbody-hfsm-state-tree`
  - 关联活跃变更 `add-fullbody-action-framework`
  - 关联现有 `locomotion-state-graph-config`
  - 关联现有 `action-runtime-state-tracker`
- Affected code after approval:
  - `Assets/Scripts/Character/Action/FullBody/Config`
  - `Assets/Scripts/Character/Action/FullBody/Model`
  - `Assets/Scripts/Character/Action/FullBody/Solver`
  - `Assets/Scripts/Character/Action/FullBody/Runtime`
  - `Assets/Configs/3C/Statemachine/FullBody`
  - `Assets/Tests/Editor`
  - `docs/agents/character-animation-state-roadmap.md`
- Not in scope:
  - 不新增 Roll、Jump、Attack、Hit、Death 等动作。
  - 不把 Dodge motion config、Action animation profile、interrupt policy 并入树资产。
  - 不改 Locomotion 四阶段 transition 规则。
  - 不做可写图形编辑器、拖拽排序、节点模板或 timeline。
  - 不新增第二套 FullBody coordinator、motion executor 或动画 Presenter。
  - 不调整网络协议、预测、回滚或 Fantasy DTO。
