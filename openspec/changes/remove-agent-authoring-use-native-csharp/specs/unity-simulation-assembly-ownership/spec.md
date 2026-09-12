## MODIFIED Requirements

### Requirement: 客户端Runtime与Editor程序集必须保持单向依赖

Character、Camera、Animation、Presentation和Scene Host MUST进入明确客户端Runtime程序集；Editor authoring、compiler、Inspector与C#作者入口 MUST进入明确Editor程序集且不得进入Player。Editor程序集 MAY引用Runtime程序集，Runtime程序集 MUST不引用Editor程序集、Editor API或模型Editor工具。

#### Scenario: 编译Player运行程序集

- **WHEN** Unity编译Player运行程序集
- **THEN** Action、Behavior、Pipeline和Simulation Editor代码 MUST不进入运行程序集
- **AND** Runtime程序集 MUST不依靠特殊Editor目录或`Assembly-CSharp-Editor`提供业务类型
