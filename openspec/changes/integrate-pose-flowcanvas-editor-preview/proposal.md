## Why

当前Pose虽然已经接入FlowCanvas，但作者仍被要求把Action输入、参数汇总、Goal Contribution、Goal Assembler与求解阶段逐个连接，根图表达的是内部执行流程。更新本提案，按UE的AnimGraph、Animation Layer、State Machine、Montage／Slot、按骨骼混合与Control Rig组织作者数据，让作者用动画功能搭图，同时继续编译到已有Program Image、Native／Job和唯一最终输出。

## What Changes

- **BREAKING**：本提案从“接通原生图UI与观察”扩大为“UE式动画作者组织与编译适配”。原来的20/21完成计数只代表旧范围；受新职责影响的任务重新打开，不另建change。
- AnimGraph作为角色动画总装图；Animation Layer、状态机、状态Pose图、转换条件图、Control Rig分别拥有明确输入、输出和下钻入口，只有一份正式作者拓扑。
- Player按UE习惯直接选择AnimationClip／Blend Space／Motion Matching资源或显式资源参数；废除面向作者的Source Slot与Profile Binding两次选择。资源准备、dense source binding和usage identity由编译器生成。
- 现有有限Action Timeline承担本项目的Montage职责，在原资产内补齐Slot轨道、Clip引用、Sections、Blend In／Out和Blend Profile。动画片段、玩法窗口、Motion与MotionWarp继续共用原Timeline时钟与生命周期；原生AnimationClip唯一拥有素材骨骼与注册曲线，不新增并行Montage资产或播放器。
- Slot是Montage进入Pose流的明确位置，能位于AnimGraph、Animation Layer或State Pose图。Slot选择稳定Slot定义，不保存Bone Mask；动画层可以封装状态机、Slot与Layered Blend Per Bone，但不自动获得私有Slot或独立Montage实例。
- Layered Blend Per Bone通过Branch Filter或Rig拥有的Blend Mask决定骨骼范围，Alpha控制层贡献；状态转换与各Montage分别配置过渡时间、曲线和Blend Profile。Slot名称、Slot Group、Bone Mask与Blend Profile保持不同职责。
- Control Rig作者图组织Forwards Solve、控制目标、Foot Placement和Full Body IK。目标构造与实际求解对作者可见；Goal Contribution打包、Goal Assembler、必要边界转换与工作区分配由编译器展开，不再是必接作者节点。
- 保留显式Inertialization、局部分支连续性、Pose可用性、实际调用身份和UE式状态／转换编辑。不能为了减少节点隐藏惯性化，也不能用重新排版或把旧流水线塞进子图代替职责重构。
- 一颗作者节点可生成多条内部operation，Source Map同时标识作者节点、内部步骤、图版本、调用路径和输出端口。Unity普通Play中的真实角色继续提供只读运行观察；不运行FlowCanvas作者图、不创建预览角色或窗口时钟。
- 在当前唯一Document v6基础上规划一次v7升级，完整承接技能侧v6内容并加入新的动画作者合同。统一Capability、Exporter、Reconciler、typed Mutation、owner事务及五生命周期，不增加局部动画MCP写入入口。
- 先完成代码与编辑能力，最后通过正式资产事务迁移Corin。此次迁移有真实的数据组织变化；上一轮“仅换UI无需迁移”的结论只适用于旧范围。迁移保持可保留的图／节点／状态identity及原动作语义，删除被替代的作者配置后统一发布Float32、Fixed和Projection。

## Capabilities

### New Capabilities

- `pose-flowcanvas-editor-preview`：扩充原有未归档能力，覆盖UE式作者分层、Montage与Slot、骨骼混合、Control Rig、源映射及普通Play观察。

### Modified Capabilities

- `character-presentation-pose-graph`：作者层次与内部运行拓扑分离；直接资源Player、层内Slot、曲线传播、控制图和编译展开。
- `graph-authoring-domain-framework`：统一角色、接口、作用域、字段、交互及多operation来源合同。
- `graph-authoring-editor-shell`：同一窗口按图职责提供UE式导航、状态连线、作者详情及运行观察。
- `btsmtl-agent-authoring-document-sync`：v7动画分片、完整owner事务、旧作者数据迁移与唯一反向导出。

本次只修改上述已有delta文件。其它现行规范的精确冲突、适用边界和实施同步要求集中记录在design.md“现行规范对账”；未将未改动的旧条款宣称为已经一致。

## Impact

- Pose作者Capability、资源选择、图角色／接口、Slot与Group定义、Montage、Mask／Profile、Rig控制目标和Document。
- Pose Compiler的Closure、语义展开、Topology、Stage／Value／Workspace规划及Source Map；允许扩展正式动画指令和所需运行描述，不建立第二Compiler或Runtime。
- 复用既有Linked Pose接口、Implementation／Group绑定和调用生命周期承载Animation Layer；复用原Native帧事务、Foot／Goal／FBBIK算法、Animancer source backend和Final Publication。
- Montage式设置直接扩展现有Action Timeline动画内容合同；技能图重构、动作准入、战斗规则和网络执行仍归原技能提案，不借此调整它们的业务行为。
- 当前v28观察元数据是可复用基础，不承诺新作者结构仍使用v28产物。最终schema由同一编译链一次升级，旧产物明确Stale，禁止双reader、自动Build或运行时修复。
- 本轮仅更新计划文档，不实施代码、不迁移资产、不Build、不归档。tasks仅列实现、清理、文档同步与迁移／发布工作。
