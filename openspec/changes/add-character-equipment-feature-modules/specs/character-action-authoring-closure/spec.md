## ADDED Requirements

### Requirement: ActionProfile authoring必须支持Required Tag Query

ActionProfile Inspector、Validator、Compiler、Program diagnostics与source revision MUST支持类型化Required Tag Query，并与现有Owned/Block/Cancel Tag Query使用同一Tag catalog和query authoring。空Required Query MUST显式表示Always；无效Tag、循环query或未登记Tag MUST失败。系统 MUST不增加EquipmentId枚举、WeaponType字段或装备专用If节点替代该通用条件。

#### Scenario: 配置Sawblade Attack要求

- **WHEN** 作者为Sawblade Attack配置`Equipment.Feature.CorinSawblade`
- **THEN** Inspector与Compiler MUST保存类型化Required Query
- **AND** ActionProfile MUST不保存MainWeapon asset引用

#### Scenario: Core Dodge无需装备

- **WHEN** Core Dodge Required Query为空
- **THEN** authoring MUST将其编译为Always
- **AND** MUST不自动继承当前Feature Tag

### Requirement: Equipment Route authoring必须引用正式ActionProfile和Input Request

每个Feature Action Route MUST通过稳定RouteId引用一个正式ActionProfile，并由Profile Route catalog绑定现有Input RequestId和消费策略。Editor与Validator MUST显示和检查三者的精确identity；MUST不按Action显示名、InputAction名称、Graph节点名称或数组index匹配。

#### Scenario: PrimaryAction绑定Attack请求

- **WHEN** Corin MainWeapon PrimaryAction Route绑定现有Attack RequestId
- **THEN** Sawblade Feature实现 MUST引用自己的正式Attack ActionProfile
- **AND** Host节点 MUST消费该Route定义而不是新增Raw Shift/Mouse绑定

#### Scenario: Route引用删除的Action

- **WHEN** Feature Route的ActionProfile不在合并Action catalog中
- **THEN** Validator MUST阻止发布
- **AND** MUST不创建匿名ActionInstance

### Requirement: Equipment Host节点必须保持通用authoring语义

BTSMTL MUST提供通用Persistent Feature Host与Equipment Action Route Host authoring入口，节点只配置稳定SlotId/RouteId和正式输入输出端口。节点 MUST不包含Corin、Sawblade、Gun、武器枚举、动画clip或Prefab字段，也 MUST不展开Feature body为RootTree分支。

#### Scenario: RootTree配置PrimaryAction Host

- **WHEN** 作者在Corin Action主流程放置PrimaryAction Host
- **THEN** 节点 MUST只引用MainWeapon/PrimaryAction identity
- **AND** 新增Gun Feature MUST不修改该节点

#### Scenario: Host节点配置未知Route

- **WHEN** 节点RouteId不属于Character Equipment Profile
- **THEN** Inspector与Compiler MUST失败
- **AND** MUST不创建自由字符串Route
