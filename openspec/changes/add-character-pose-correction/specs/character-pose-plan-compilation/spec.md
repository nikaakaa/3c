## ADDED Requirements

### Requirement: 静态修正必须随正式图编译完整发布

Character Build MUST从精确Correction Slot、Profile Binding、样本revision和Rig编译固定的骨骼引用、参考姿态、样本常量与驱动合同，并将其纳入Program Image身份及依赖。运行时 MUST只读取已发布数据，不搜索作者对象、不创建样本Player、不追加Operation。缺少绑定、错误单位或过期Rig MUST阻止整体发布。

#### Scenario: 样本发生修改

- **WHEN** 正式样本姿态或中性参考revision改变
- **THEN** 相关Projection MUST变为Stale并在显式Build后发布新身份
- **AND** Runtime MUST不静默消费旧样本或自动编译

#### Scenario: 修正节点不可达

- **WHEN** 修正节点不在正式可达图闭包中
- **THEN** 它 MUST不产生运行Operation、工作页或采样任务

### Requirement: 修正拓扑必须证明共同姿态基线和完整骨骼影响

Compiler MUST在统一拓扑校验中证明目标源与FBBIK的共同输入基线、修正节点的Pose依赖顺序、末端政策及完整骨骼影响集合。影响 MUST包含父子传播和Virtual派生关系。局部节点声明只提供自身读写语义，不得自行扫描全图或补建求解路径。

#### Scenario: 根骨影响已求解手脚

- **WHEN** PreserveSolvedEffectors后置节点修改Root，且受约束手脚位于其受影响依赖中
- **THEN** Compiler MUST报告全部相关末端及来源节点并拒绝发布

#### Scenario: 同节点测量和修改同一骨骼

- **WHEN** 骨骼方向节点的源骨骼也属于写入集合
- **THEN** 计划 MUST明确测量读取节点输入，修改产生独立输出，不形成自反馈

### Requirement: 修正求值必须沿现有纯姿态调度与事务执行

静态修正 MUST作为PurePose计算进入既有编译Stage、线程安全工作计划和节点完成记录，每个可达节点每帧只执行一次。计算 MUST只读不可变编译数据及同帧输入，并写本节点Pending输出；Actor之间的页、完成和故障 MUST隔离。生成ABI变化 MUST要求重新发布匹配产物，不保留旧格式reader或运行时适配。

#### Scenario: 两个Actor共用图与样本

- **WHEN** 两个Actor使用相同编译修正数据但输入方向不同
- **THEN** 两者 MUST独立产生输出，不能通过共享可变权重或骨骼页互相影响

#### Scenario: 修正之后最终发布

- **WHEN** 所有必需姿态、Constraint与后置修正均完成
- **THEN** 最终骨骼 MUST仍由既有唯一Final Publication发布一次
- **AND** 修正节点不得增加独立Transform写入
