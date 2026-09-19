## ADDED Requirements

### Requirement: 作者合同程序集与编辑集成保持单向依赖

此次迁出的项目自有声明与业务定义 MUST 不依赖旧 UI 或编辑程序集；必要框架适配保留在原领域。MUST 优先沿用现有模块，仅为真实依赖边界建立必要程序集，不为消除目录名字而重写有效类型。编辑集成 MAY 依赖共享合同和领域 runtime，但共享 runtime/Simulation/Core MUST 不依赖 Editor、窗口、GraphView 或 UI 描述。原生框架适配只能在对应领域或编辑程序集拥有。

#### Scenario: 编译运行时核心

- **WHEN** Simulation/Core 或 Timeline runtime 被单独编译
- **THEN** 编译 MUST 不需要旧 TreeDesigner Editor、窗口或 UI 类型
- **AND** 运行时 MUST 不反向引用 Editor authoring 集成

#### Scenario: 编辑器装配领域

- **WHEN** Unity Editor 打开 Skill 或 Pose 图
- **THEN** Editor MUST 沿用当前正式入口装配领域原生编辑器和面板，只更新迁出类型的直接引用
- **AND** 领域合同程序集 MUST 不引用窗口实现

### Requirement: 作者代码归位涉及的运行热路径必须保持零托管分配

此次触及的运行热路径 MUST 严格遵循 0 GC，不在每帧取值、递归求值或调用中分配容器、扩容、装箱、创建闭包或构造字符串。存储 MUST 沿用正式准备阶段与既有容量及生命周期。MUST 不把值缓冲复用当成结果缓存或零分配证明，不用关闭正式功能、静默吞掉容量问题或 fallback 达标。

#### Scenario: 文件归位影响运行调用

- **WHEN** 迁移后的运行消费者读取输入或调用领域服务
- **THEN** MUST 使用既有直接调用和已准备存储，不新增每帧包装对象或临时集合
- **AND** 该路径的 0 GC 结论 MUST 有对应证据，不能只凭格式校验或类型名认定

#### Scenario: 既有分配需要超出整理范围的修改

- **WHEN** 本次触及的真实运行路径存在分配且消除它需要修改求值算法或容量合同
- **THEN** MUST 报告具体调用、分配来源和业务取舍，交用户决定范围
- **AND** MUST 不绕过约束或宣称完成，也不擅自扩展整个编译器重构

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
- **AND** 迁移结束后 MUST 删除对应旧类型副本及确实已空的程序集，不保留兼容链；仍包含有效实现的程序集 MUST 保留

#### Scenario: 发现未知 managed reference

- **WHEN** 资产存在无法安全映射的 managed-reference assembly typename
- **THEN** 实施 MUST 停止并报告具体资产、字段和缺口
- **AND** MUST 不用默认类型或字符串替换继续迁移

#### Scenario: 迁移现有Composition资产

- **WHEN** Composition、Pipeline、Model、Endpoint、Solver或Scene组件脚本进入新程序集
- **THEN** 现有资产 MUST继续通过原MonoScript GUID引用唯一类型
- **AND** GraphHash、PipelineHash、Composition identity与Model identity MUST不因程序集迁移改变
