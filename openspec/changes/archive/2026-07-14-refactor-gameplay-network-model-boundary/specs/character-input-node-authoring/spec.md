## MODIFIED Requirements

### Requirement: BTSMTL 输入 authoring 不得暴露 ClientCommand

BTSMTL 输入 authoring MUST 只创建和读取 CharacterInputFrame values、action requests 与 request buffer。它 MUST NOT 创建、读取、保存或显示 ClientCommandFrame、MotionCommand、Rollback input bundle、model packet 或 endpoint。Model command preview MUST 只存在于对应 model profile/Runtime Debug，不得进入 Graph Data Catalog 的输入节点列表。

#### Scenario: 创建 MoveAxis 节点

- **WHEN** 作者从输入配置创建 MoveAxis ValueNode
- **THEN** 节点 MUST 读取 CharacterInputFrame
- **AND** MUST 不提供 ServerAuthoritative MotionCommand 节点

#### Scenario: 查看模型 packet preview

- **WHEN** 作者需要查看 resolved motion 如何映射为 MotionCommand
- **THEN** MUST 在 ServerAuthoritative model Inspector/Debug 查看
- **AND** BTSMTL 输入 authoring MUST 不显示该 packet

