## MODIFIED Requirements

### Requirement: Graph Authoring必须拥有唯一领域框架

系统 MUST复用领域无关的document、Capability、Port Shape、selection、clipboard、Details、Navigator、Mutation和diagnostics合同。Pose MUST采用原生作者图基础及交互；共享序列化图基类不等于共享业务语义，领域payload、端口约束、编译操作和运行状态 MUST分别归属本领域。其它领域 MUST保持其正式入口与行为，不因Pose接入被隐式迁移。每个领域 MUST只保留一份正式可编辑拓扑。

#### Scenario: 打开不同领域Graph
- **WHEN** 作者分别打开BTSMTL技能图与Pose图
- **THEN** 两者 MUST复用已建立的共享合同与可复用交互，按各自迁移状态使用正式编辑入口
- **AND** 每个document MUST只加载本领域的asset adapter、Capability与Mutation

#### Scenario: 跨领域粘贴节点
- **WHEN** clipboard的domain identity与当前document不一致
- **THEN** 系统 MUST在Mutation前拒绝粘贴，不猜测或转换另一领域payload

### Requirement: 唯一领域框架必须从现有BTSMTL作者UI原地抽象

尚未迁移的BTSMTL领域 MUST保持既有布局、节点信息、黑板拖拽、Flow／Property Port、搜索、selection、clipboard、Undo、Inspector、下钻和Live行为，不用功能更少的替代画布切换入口。Pose的原生编辑接入 MUST复用共享领域合同并保留其作者能力，不再受“必须提取旧GraphView作为具体画布”的实现限制；共享框架不得为Pose复制第二份节点或端口语义目录。

#### Scenario: 拖出黑板变量
- **WHEN** 作者在未迁移的BTSMTL画布中拖出黑板变量
- **THEN** 既有手势、变量节点、Property Port及Mutation语义 MUST保持
- **AND** Pose的原生编辑接入 MUST不把该操作降级

### Requirement: Authoring节点与Runtime执行描述必须分离

作者合同 MUST只暴露稳定identity、typed字段、端口及Mutation。作者节点采用可执行框架的基类时，MUST不因此成为角色实际执行器。Compiler MUST直接读取正式作者数据并生成领域运行产物；正式Runtime MUST不依赖Editor图对象、作者getter或窗口生命周期。运行offset、buffer index与性能枚举 MUST不自动进入菜单、Details或Document。

#### Scenario: Runtime增加优化字段
- **WHEN** Pose Runtime为执行计划增加内部offset或buffer index
- **THEN** Authoring、Details与Document MUST不自动暴露该字段
- **AND** Compiler MUST继续负责生成该内部值
