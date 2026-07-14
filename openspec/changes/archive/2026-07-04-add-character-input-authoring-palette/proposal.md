# Proposal

## 背景

`refactor-character-input-boundary` 已经把角色输入边界收口为 `InputValue` 和 `ActionRequest`，并删除了 Graph 作者可见的 `signal/command` 口径。但当前 authoring 入口仍偏临时：输入信息节点可以从 `CharacterInputProfile` Inspector 或 profile asset 拖出，体验不像 BTSMTL 自己的图级 authoring 面板。

ExposedProperty 的心智更清楚：Graph 页内有独立列表，列表项可以拖到当前 Graph，落地为正式节点。`InputValue` 也应该采用同类交互，但数据归属不同：`ExposedProperty` 是 Graph 内部变量，`InputValue` 是角色输入配置的只读投影，Graph 只能引用它，不能复制一份 InputSystem 配置。

## 目标

- 在 BTSMTL Tree Inspector 的 `Graph` 页内新增与 ExposedProperty 同级的 `Input` 素材区，展示当前角色可用的 `InputValue` 和 `ActionRequest`。
- 让 `Input` 素材区像 ExposedProperty 一样支持拖拽条目到当前 Graph，每次生成一个新的对应输入信息节点。
- 将 `CharacterInputProfile` 归入 `CharacterPipelineDefinition`，让角色定义成为 RootTree、输入合同、动作和动画配置的统一 authoring 上下文。
- 让 `CharacterPipelineHost` 不再单独持有 input profile，避免 scene host 和 definition 之间出现分裂配置。
- 删除或停用 Profile Inspector 里直接创建 Graph 节点的旧入口，避免并行 authoring 路径。
- 保持 Graph 不保存第二份 InputAction 配置；节点只保存稳定 input value id/request id 和期望值类型。

## 非目标

- 不新增输入专用 Graph、Workbench 路径或 Graph 内 input 配置表。
- 不让 `BaseTreeAsset` 自己保存 `CharacterInputProfile` 引用。
- 不通过场景搜索、AssetDatabase 猜测或手动 ObjectField fallback 来决定当前输入 Profile。
- 不改变 raw `InputActionValueNode` 的调试用途。
- 不实现 Unity 端到端手动验证任务。

## 影响范围

- `CharacterPipelineDefinition` 和 `CharacterPipelineHost` 的输入配置归属。
- BTSMTL `BaseTreeWindow/BaseTreeInspectorView` 的 editor-only authoring context 和 Graph 页内素材区 UI。
- 输入信息节点创建 editor utility，复用现有 `InputValueInfo/ActionRequestInfo` 节点。
- Corin pipeline definition / host / root tree 资产迁移。
- `character-input-node-authoring`、`character-pipeline-runtime` 和 `btsmtl-graph-core` specs。

## 风险

- 如果直接打开孤立 `BaseTreeAsset`，窗口没有角色输入上下文，`Input` 素材区不能展示定义。该情况应明确显示“缺少 CharacterPipelineDefinition 上下文”，而不是提供临时 profile picker。
- 如果多个角色定义复用同一个 RootTree，`Input` 素材区必须以打开入口传入的 definition 为准，不能反向猜测。
- 旧 Profile Inspector 拖拽入口需要移除或改为非创建节点用途，否则会形成两套 authoring 表面。
