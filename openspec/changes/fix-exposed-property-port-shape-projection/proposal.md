# Change: 修复 ExposedProperty 模式端口形状投影

## Why

Corin 逻辑层 `Attack` 状态下钻到 `Attack State Body` 时，窗口会在绑定 `ExposedPropertyNode` 的 `property:m_Value` 端口时抛出形状不一致异常。资产中的 Set 节点正确保存为输入端口，但 BTSMTL Capability Catalog 通过默认构造的 Get 节点把同一端口登记为固定输出端口，导致 Canvas、Document node catalog 与实际节点形状分叉。

当前 `ExposedPropertyNodeType`、`PropertyPort.Direction` 和端口容量也没有由一个领域入口共同维护。UI、Timeline authoring 和 Agent Mutation 调用者需要各自记住修改方向，使合法资产仍可能被类型级默认投影判错。只绕过 Canvas 校验会让 Document exporter、Package Mapper、Reconciler 和 Validator 继续使用错误端口合同。

## What Changes

- 在 Graph Authoring Capability 中增加由 typed property discriminator 驱动的条件端口形状合同，区分类型级固定端口和实例级条件投影端口。
- 保留唯一 `exposed-property` node kind，以 `exposedProperty.mode` 作为正式 discriminator：Get 投影 `m_Value` 输出多连接端口，Set 投影 `Input` flow 输入和 `m_Value` 属性输入单连接端口。
- 让 `ExposedPropertyNode` 自己维护 `NodeType` 与 `m_Value.Direction` 的领域不变量，删除 UI、Timeline 和 Mutation 调用者中的重复方向维护。
- 让 Canvas、`context/node-catalog.json`、strict Package Mapper、Reconciler、Mutation preflight 和 Validator 读取同一个端口形状投影结果。
- 在 mode 变更时先删除与目标形状不兼容的旧边，再配置节点并建立目标边；目标 Document 没有完成该闭包时明确失败。
- 删除通过默认构造节点推断条件端口、通过当前 Unity snapshot 端口放行非法 Document endpoint，以及跳过端口形状校验的路径。
- 保留现有 Unity authoring 资产和 sparse Graph JSON identity；合法 Get/Set 节点不需要资产迁移，现有不一致节点由正式 Validator 报错，不自动修复。

## Impact

- Affected specs: `graph-authoring-domain-framework`、`btsmtl-agent-authoring-document-sync`、`character-pipeline-blackboard`
- Affected code:
  - `TreeDesigner/Scripts/Node/Custom/ExposedPropertyNode.cs`
  - `TreeDesigner/Editor/Scripts/View/Node/ExposedPropertyNodeView.cs`
  - `Authoring/SharedGraph/BtsmtlGraphAuthoringCapabilities.cs`
  - `Authoring/SharedGraph/BtsmtlSharedGraphAuthoringAdapters.cs`
  - `AgentAuthoringPackageMapper.cs`、Graph exporter、Reconciler、Mutation handler 与 Validator
  - Timeline TreeClip 与 Action Eligibility 中创建 Set 节点的调用点
- `context/node-catalog.json` 将正式表达条件端口变体；该文件由 service 重新生成，不保留旧格式兼容读取。
- Runtime Blackboard、compiled Program ABI、攻击状态机业务结构和现有资产 identity 不变。

## Spec Reconciliation

- 当前 `graph-authoring-domain-framework` 已要求固定端口来自 Capability、动态端口来自 node-local 投影。本变更补充“稳定端口 identity 的方向和容量由 typed discriminator 决定”这一缺失情况，不改变统一 Framework 方向。
- 当前 `btsmtl-agent-authoring-document-sync` 已要求 node catalog、Exporter、Reconciler 和 Validator 使用同一 Capability。本变更删除现有 snapshot endpoint 放行路径，使实现回到该要求。
- 当前 `character-pipeline-blackboard` 已规定 `BaseExposedProperty` 是唯一 Blackboard authoring 表面。本变更保留一个 `exposed-property` kind，不新增 Get/Set 两套变量或第二 Blackboard 入口。
- 未发现需要删除或重命名的 current requirement；active changes 未覆盖 BTSMTL Gameplay Graph 的模式相关属性端口合同。
