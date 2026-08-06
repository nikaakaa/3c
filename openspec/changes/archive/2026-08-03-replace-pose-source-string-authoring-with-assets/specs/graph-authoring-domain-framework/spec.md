## MODIFIED Requirements

### Requirement: Details必须只显示当前作者需要的业务字段

Details MUST只投影当前selection、当前capability与当前authoring mode允许查看或修改的字段和命令。Unity资源关系 MUST使用Capability声明的精确对象类型和对象选择器；领域identity关系 MUST使用精确上下文提供的可读选项目录。IdentityReference缺少选项目录时 MUST显示Unavailable并禁止编辑，不得退化为TextField；选项标签 MUST不拼接内部value。稳定identity、revision、GUID、local file id、compiled index、runtime handle、generated path、内部枚举载荷、缓存、Projection中间值与不适用nullable字段 MUST默认隐藏；只读References与Diagnostics MUST放入明确折叠区，且 MUST不伪装成可编辑属性。

#### Scenario: 选择Sequence Player

- **WHEN** 作者在Authoring模式选择Sequence Player
- **THEN** Details MUST显示类型受限的Source Slot对象选择、loop、play rate、sync与该节点真实可写策略
- **AND** References MUST显示解析后的动画资源与唯一owner
- **AND** MUST不显示Source Id、TwoBoneIK、Slot、compiled offset或联合体空字段

#### Scenario: identity选项目录不可用

- **WHEN** 当前页面缺少解析某个IdentityReference所需的精确Definition或owner上下文
- **THEN** Details MUST显示该引用不可用及缺失上下文原因
- **AND** MUST不允许作者输入任意字符串绕过目录

### Requirement: Navigator与Data Catalog必须复用统一信息架构

框架 MUST提供统一Navigator、breadcrumb、Data Catalog、搜索与Open命令宿主。领域adapter MUST只投影真实owner、引用、页面和业务分组，并使用业务显示名与Unity资源名作为作者标签；不得保存第二份authoring数据，也不得在缺失显示名时回退显示GUID、hash或stable identity。跨资产字段修改 MUST通过Open Owner导航到唯一正式编辑入口。

#### Scenario: Pose Navigator显示Producer

- **WHEN** 作者从精确Character Definition上下文打开Pose Graph
- **THEN** Navigator MUST投影Profile、Pose Source Slot、实际资源、Action producer、Pose graph页面与引用
- **AND** MUST不复制resource binding或Timeline字段供当前页面直接修改
- **AND** MUST不把内部identity作为Navigator项目名称
