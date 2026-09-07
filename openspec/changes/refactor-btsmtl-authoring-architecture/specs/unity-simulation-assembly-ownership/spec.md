## MODIFIED Requirements

### Requirement: 客户端Runtime与Editor程序集必须保持单向依赖

Character、Camera、Animation、Presentation和Scene Host MUST进入明确客户端Runtime程序集；Editor authoring、compiler、Inspector与Agent工具 MUST进入明确Editor程序集且不得进入Player。Editor程序集 MAY引用Runtime程序集，Runtime程序集 MUST不引用Editor程序集、Editor API或模型Editor工具。

#### Scenario: 编译Player运行程序集

- **WHEN** Unity编译Player运行程序集
- **THEN** Action、Behavior、Pipeline和Simulation Editor代码 MUST不进入运行程序集
- **AND** Runtime程序集 MUST不依靠特殊Editor目录或`Assembly-CSharp-Editor`提供业务类型
#### Scenario: 装配可更新规则程序集

- **WHEN** Player或portable服务端加载角色控制及技能叶子实现
- **THEN** 共享核心 MUST只依赖portable合同；规则实现不得引用Editor、Unity对象或Fantasy transport
- **AND** 模块 MUST通过精确发布清单装配，AOT不得直接引用热更实现


## ADDED Requirements

### Requirement: 技能作者与规则实现必须具有明确程序集所有权

技能作者、Document和compiler MUST属于Editor层；portable技能Program／解释器、控制与状态合同、Target数值实现、可更新规则实现以及Unity装配 MUST具有明确单向依赖。规则实现由正式发布版本登记，不能藏在窗口、Scene MonoBehaviour或网络Handler中。

#### Scenario: 检查依赖方向

- **WHEN** 新增控制规则或技能节点
- **THEN** 作者层 MUST只通过共享合同与正式构建扩展，Runtime不能反向依赖作者层

#### Scenario: 模块未发布

- **WHEN** 场景要求的规则模块不在Player／服务端发布闭包
- **THEN** 准备 MUST失败，不能按类型扫描或加载默认模块
