## MODIFIED Requirements

### Requirement: 程序集迁移必须保持唯一序列化身份

程序集迁移 MUST保持正式资产的唯一作者 identity 和 MonoScript 引用，纯归属迁移应保留稳定序列化合同。需要改变 Timeline 领域归属、程序集限定类型或已失真的类型名称时，系统 MUST通过正式 Editor 迁移完整更新所有受影响引用并验证，不能使用旧空壳程序集、重复类型、兼容转发、MovedFrom 链或 runtime 修复。无关 Composition 等正确合同不得顺带修改；无法安全转换的真实资产冲突必须报告。

#### Scenario: 迁移现有Composition资产

- **WHEN** Composition、Pipeline、Model、Endpoint、Solver 或 Scene 组件只改变程序集归属
- **THEN** 资产 MUST仍通过原 MonoScript GUID 引用唯一类型
- **AND** Program/Pipeline/Composition/Model identity 不得因纯归属迁移改变

#### Scenario: Timeline managed-reference 类型迁移

- **WHEN** Timeline 领域拆分改变已序列化片段的程序集限定类型
- **THEN** 正式迁移 MUST完整更新受影响资产并保持内容身份与引用关系
- **AND** 新产物不得在仍有旧类型引用时发布，不能运行时补救

## ADDED Requirements

### Requirement: Timeline 共用执行必须与 Character 和 Tree 扩展保持单向依赖

共用 Timeline 数据/时间执行合同 MUST不依赖 Character Definition、Action 业务、角色状态容器、Unity 场景对象或 Editor。TreeClip 扩展 MUST依赖共用 Timeline 和树执行合同，Timeline 核心不得反向依赖 Tree。Character 接入与非 Skill 场景接入 MUST各自依赖共用执行和自己的领域模块，不能互相引用具体实现。领域片段不得使核心被动加载全部 Character 能力。

#### Scenario: 构建不含角色的独立调用闭包

- **WHEN** 正式构建选取 Timeline 核心、必要 Tree 扩展和场景表现参数模块
- **THEN** 其直接/传递必需业务依赖 MUST不包含 Character Definition、Action Runtime 或角色表现实现
- **AND** MUST能通过项目正式程序集构建，不能用额外自造工程替代依赖检查

#### Scenario: 内容没有 TreeClip

- **WHEN** 内容只使用不依赖 Tree 的片段
- **THEN** Timeline 核心 MUST不要求 Tree 扩展装配
