## ADDED Requirements

### Requirement: 作者合同程序集与编辑集成保持单向依赖

项目自有作者合同、分域声明和编译所需的只读类型 MUST 放在不依赖 TreeDesigner、FlowCanvas、NodeCanvas 或任何编辑程序集的共享模块。编辑集成 MAY 依赖共享合同和领域 runtime，但共享 runtime/Simulation/Core MUST 不依赖 Editor、窗口、GraphView 或 UI 描述。原生框架适配只能在对应领域或编辑程序集拥有。

#### Scenario: 编译运行时核心

- **WHEN** Simulation/Core 或 Timeline runtime 被单独编译
- **THEN** 编译 MUST 不需要旧 TreeDesigner Editor、窗口或 UI 类型
- **AND** 运行时 MUST 不反向引用 Editor authoring 集成

#### Scenario: 编辑器装配领域

- **WHEN** Unity Editor 打开 Skill 或 Pose 图
- **THEN** Editor 只能通过共享作者合同装配当前领域原生编辑器和面板
- **AND** 领域合同程序集 MUST 不引用窗口实现

## MODIFIED Requirements

### Requirement: 程序集迁移必须保持唯一序列化身份

普通 Unity Simulation、Network Model、WorldSolver、Runtime 与 Editor 类型迁移 MUST 保留原 .meta GUID、namespace、类型名和序列化字段。对于本变更明确抽出的项目自有 authoring contract，允许在同一次正式资产迁移中清理 TreeDesigner namespace 和 assembly 限定名；该例外 MUST 只适用于已列入迁移清单的作者类型，不能扩展到其它组件。系统 MUST 不保留旧 assembly 空壳、重复类型、MovedFrom 兼容链、一次性 runtime migrator 或双份 ScriptableObject。若受影响类型存在无法安全迁移的 managed-reference assembly typename，实施 MUST 停止并报告缺口。

#### Scenario: 迁移普通 Unity 类型

- **WHEN** 普通 Simulation、Model、Solver 或 Scene 组件进入新程序集
- **THEN** 资产 MUST 继续引用原 MonoScript GUID、namespace、类型名和字段
- **AND** GraphHash、PipelineHash、Composition identity 与 Model identity MUST 保持

#### Scenario: 迁移自有作者合同

- **WHEN** 列入清单的 TreeDesigner 自有 authoring type 迁入共享合同程序集
- **THEN** 正式迁移 MUST 更新全部 managed-reference typename、资产和声明引用
- **AND** 迁移结束后 MUST 删除旧类型、空程序集和兼容链

#### Scenario: 发现未知 managed reference

- **WHEN** 资产存在无法安全映射的 managed-reference assembly typename
- **THEN** 实施 MUST 停止并报告具体资产、字段和缺口
- **AND** MUST 不用默认类型或字符串替换继续迁移

#### Scenario: 迁移现有Composition资产

- **WHEN** Composition、Pipeline、Model、Endpoint、Solver或Scene组件脚本进入新程序集
- **THEN** 现有资产 MUST继续通过原MonoScript GUID引用唯一类型
- **AND** GraphHash、PipelineHash、Composition identity与Model identity MUST不因程序集迁移改变
