# btsmtl-agent-authoring-document-sync Delta

## ADDED Requirements

### Requirement: Document v3必须通过共享Capability表达Secondary Motion节点

Document v3的Pose Graph `graph.json` MUST通过唯一Authoring Capability Catalog表达`SecondaryMotion`节点、`pose.local`输入输出、root-only上下文和强类型`profile`对象引用。`context/asset-catalog.json` MUST只读输出可引用`CharacterSecondaryMotionProfile`的稳定identity、结构化Unity对象引用、Rig lineage与revision；Profile正文、Physical Bone正文、Collider Transform、Magica组件、team和generated setup MUST不进入editable。Exporter、strict codec、Reconciler、typed Presentation Mutation、Validator和reverse export MUST使用同一Capability，不得增加Secondary Motion私有schema、Patch operation、SerializedProperty写入或专用MCP工具。

#### Scenario: Document新增Secondary Motion节点

- **WHEN** AI在root Pose Graph加入合法`SecondaryMotion`节点并引用Asset Catalog中的Profile
- **THEN** dry-run MUST生成与人工Canvas相同的typed Node create、Profile configure和Pose edge Mutation
- **AND** apply MUST只修改正式authoring并保持Projection stale

#### Scenario: Document在Linked Entry加入节点

- **WHEN** AI在Linked Pose Entry graph写入`SecondaryMotion`
- **THEN** strict capability validation MUST在Mutation前拒绝该上下文
- **AND** MUST不把节点移到root graph或创建Magica组件

#### Scenario: Document尝试内联Profile正文

- **WHEN** SecondaryMotion payload包含root bone数组、Collider几何、Magica参数或组件路径
- **THEN** strict parser MUST拒绝未知字段
- **AND** MUST要求只保存结构化Profile对象引用

### Requirement: Secondary Motion变化必须进入唯一Presentation事务

Document Reconciler MUST把SecondaryMotion节点的创建、Profile引用修改、Pose edge改接和删除降低为现有typed Presentation Mutation，并与Gameplay、Timeline及其它Presentation变化进入同一Document v3 hash、dry-run、apply、Undo、Validator、save和reverse export事务。Character apply MUST不生成Magica setup、Presentation Projection或Native Pose Program；这些产物 MUST继续由显式Character Build发布。系统 MUST不新增profile文件级apply、Magica component mutation或第二authoring transaction。

#### Scenario: 同一Document同时修改Slot与Secondary Motion

- **WHEN** editable目标同时改接AnimationSlot并新增SecondaryMotion节点
- **THEN** Reconciler MUST生成一个完整immutable Mutation Plan并锁定同一document hash
- **AND** 任一Presentation校验失败 MUST回滚两项变化及同事务内其它领域变化

#### Scenario: apply后generated setup仍旧

- **WHEN** SecondaryMotion节点apply成功但尚未执行Character Build
- **THEN** reverse export MUST发布新的正式authoring与stale generated context
- **AND** MCP bridge MUST不自动Build或把旧setup标记为当前
