# Design

## 当前事实

- `BaseTreeInspectorView` 当前有 `Graph` 和 `Inspector` 两个页签。`Graph` 页内包含 Graph 属性和 `ExposedProperty` 面板。
- `ExposedPropertyView` 是一个可拖拽列表项，拖到同一个 `BaseTreeView` 后通过 `ExposedPropertyNode.Create(...)` 在 Graph 中创建节点。
- `CharacterInputProfile` 当前表达输入定义，但在运行侧仍由 `CharacterPipelineHost` 单独引用；`CharacterPipelineDefinition` 当前只持有 RootTree、AnimationLayers 和 ActionProfiles。
- 当前输入信息节点已改为 `InputValueInfo` / `ActionRequestInfo`，但正式 authoring 表面还没有像 ExposedProperty 一样收进 Tree Inspector。

## 核心决策

### 1. Input 素材区属于 Graph 页，不是独立页签

`Input` 素材区放在 `BaseTreeInspectorView` 的 `Graph` 页内，与 `ExposedProperty` 同级：

```text
Graph
  Graph Properties
  ExposedProperty
  Input
    Input Values
      MoveAxis   Vector2
      LookAxis   Vector2

    Action Requests
      Attack
      Dodge

Inspector
```

该列表不允许编辑 Profile，不提供新增、删除或改名。编辑输入定义仍发生在 `CharacterInputProfile` 资产本身。

业务取舍：作者在 Graph 中看到的是“可消费输入合同”，不是“输入绑定配置”。把它放在 Graph 页内，能和 ExposedProperty 形成同类拖拽素材心智；做成独立页签边界更醒目，但会让 Input 看起来像第二套配置页，增加 authoring 表面的分裂感。

### 2. CharacterPipelineDefinition 持有输入合同

新增正式归属：

```text
CharacterPipelineDefinition
  RootTreeAsset
  InputProfile
  AnimationLayers
  ActionProfiles
```

`CharacterPipelineHost` 只引用 `CharacterPipelineDefinition`，运行时从 definition 取 `InputProfile` 创建 `CharacterInputStage`。

业务取舍：

- 把 input profile 放在 definition：一个角色定义完整表达 RootTree、输入、动作和动画，Graph authoring 能拿到同一个上下文。
- 放在 Host：不同场景对象可能给同一个 definition 配不同输入，Graph authoring 无法知道哪个才是正式输入合同。
- 放在 RootTreeAsset：会把角色输入合同写进通用 Graph 资产，破坏 BTSMTL 的通用 authoring 边界。

结论：使用 `CharacterPipelineDefinition.InputProfile`，并迁移 Corin 现有 host 配置。

### 3. TreeWindow 使用 editor-only authoring context

`BaseTreeWindow` 增加 editor-only authoring context 概念，供 Tree Inspector 内依赖业务上下文的区块读取当前打开入口。该 context 不序列化进 `BaseGraph` 或 `BaseTreeAsset`。

打开入口：

- 从 `CharacterPipelineDefinition` 打开 RootTree：传入 `CharacterPipelineDefinition` 和 `InputProfile`。
- 从普通 `BaseTreeAsset` 直接打开：没有角色输入上下文，`Input` 素材区显示缺失上下文状态，不提供 profile picker。
- 下钻 inline/shared graph：沿用当前窗口的 authoring context。

业务取舍：

- 显式 context：不会猜测资产引用关系，支持多个角色定义复用同一个 RootTree。
- AssetDatabase 反查：看似自动，但多个 definition 引用同一 RootTree 时不可靠。
- 面板内手动选择 Profile：会产生 Graph authoring 的临时配置入口，不符合干净链路。

### 4. 节点创建使用单一 factory

新增或收敛一个 editor-only `CharacterInputInfoNodeFactory`：

- 输入：当前 Tree、输入定义条目、目标位置。
- 行为：每次拖拽创建一个新的输入信息节点，并绑定到对应 id。
- 输出：正式 `InputValueInfo` 或 `ActionRequestInfo` 节点。

同一个 input value 或 action request 可以在 Graph 中被多个节点读取；这些节点只是多个使用点，共用同一份 Profile 定义，不复制 InputAction 配置。`Input` 素材区拖拽和未来其它正式入口必须调用同一个 factory。旧 Profile Inspector 的直接拖拽创建入口应删除，避免形成第二套 UI 路径。

### 5. 节点保存稳定 gameplay id，不保存 InputAction 配置

节点继续只保存：

- `InputValueId` + `CharacterInputValueType`
- 或 `RequestId`

节点不保存 `InputActionReference`、InputAction 名称、profile 引用副本或显示名。Profile 中 InputAction 重命名不影响节点；input value id 改名是正式破坏性 authoring 变更，节点应报缺失而不是自动猜新名字。

## 用户流程

1. 用户选中 `CharacterPipelineDefinition`。
2. 用户通过 definition editor 打开 RootTree。
3. Tree editor 左侧显示 `Graph | Inspector`。
4. 用户在 `Graph` 页内的 `Input` 素材区看到 `MoveAxis : Vector2` 和 `Attack`。
5. 用户拖 `MoveAxis` 到图上，生成 `CharacterInputVector2InfoNode`。
6. 用户将该节点 `Value` 输出接到移动模块的 `Move Input`。
7. 用户再次拖 `MoveAxis`，系统再生成一个绑定同一 `MoveAxis` 的读取节点。

## 迁移策略

- `CharacterPipelineDefinition` 增加 `InputProfile`。
- `CharacterPipelineHost` 删除独立 input profile 字段，构造 pipeline 时使用 definition 的 input profile。
- Corin definition 迁入当前 `CorinCharacterInputProfile`。
- Corin host 上旧 input profile 引用删除。
- 旧 Profile Inspector 拖拽条目删除；Profile Inspector 只负责编辑输入配置和报告配置错误。

## 与现有 active changes 的关系

- `refactor-character-input-boundary` 已完成输入术语边界，本变更只补正式 authoring 面板和上下文归属。
- `refactor-character-motion-arbitration` 后续会继续处理 motion contribution 仲裁；本变更只保证 locomotion 能从 `MoveAxis` 输入信息节点获取 Vector2。

## 待确认但不阻塞 proposal 的点

- `Input` 素材区中的条目视觉样式可以复用 `ExposedProperty.uxml/uss`，或新增 `CharacterInputDefinition.uxml/uss`。实现时按最少改动选择，但交互语义必须一致。
- 节点创建后是否自动选中新节点属于 editor 体验细节，不影响数据模型。
