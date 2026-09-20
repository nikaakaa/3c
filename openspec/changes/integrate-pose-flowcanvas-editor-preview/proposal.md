修订：公共输入/authoring协议r2（2026-09-13）。依据用户广播及[公共C# authoring r2](../archive/2026-09-13-remove-agent-authoring-use-native-csharp/design.md)更新。范围与历史任务去向见[对接记录](coordination-r2.md)，不重规划旧Pose其它业务。

## Why

当前Pose虽然已经接入FlowCanvas，但作者仍被要求把Action输入、参数汇总、Goal Contribution、Goal Assembler与求解阶段逐个连接，根图表达的是内部执行流程。更新本提案，按UE的AnimGraph、Animation Layer、State Machine、Montage／Slot、按骨骼混合与Control Rig组织作者数据，让作者用动画功能搭图，同时继续编译到已有Program Image、Native／Job和唯一最终输出。

## What Changes

- **BREAKING**：本提案从“接通原生图UI与观察”扩大为“UE式动画作者组织与编译适配”。原来的20/21完成计数只代表旧范围；受新职责影响的任务重新打开，不另建change。
- AnimGraph作为角色动画总装图；Animation Layer、状态机、状态Pose图、转换条件图、Control Rig分别拥有明确输入、输出和下钻入口，只有一份正式作者拓扑。
- **BREAKING**：Pose成为独立编译模块。动画图、Rig、资源及动画侧输入声明即可进入同一个Pose Compiler，不先编译Character／SkillGraphs；角色总Build只复用其结果并完成Gameplay到动画的接口绑定及原子发布。技能缺失可以阻止角色发布，不能阻止合法Pose的编辑与编译。
- Player按UE习惯直接选择AnimationClip／Blend Space／Motion Matching资源或显式资源参数；废除面向作者的Source Slot与Profile Binding两次选择。资源准备、dense source binding和usage identity由编译器生成。
- 现有有限Action Timeline承担本项目的Montage职责，在原资产内补齐Slot轨道、Clip引用、Sections、Blend In／Out和Blend Profile。动画片段、玩法窗口、Motion与MotionWarp继续共用原Timeline时钟与生命周期；原生AnimationClip唯一拥有素材骨骼与注册曲线，不新增并行Montage资产或播放器。
- Slot是Montage进入Pose流的明确位置，能位于AnimGraph、Animation Layer或State Pose图。Slot选择稳定Slot定义，不保存Bone Mask；动画层可以封装状态机、Slot与Layered Blend Per Bone，但不自动获得私有Slot或独立Montage实例。
- Layered Blend Per Bone通过Branch Filter或Rig拥有的Blend Mask决定骨骼范围，Alpha控制层贡献；状态转换与各Montage分别配置过渡时间、曲线和Blend Profile。Slot名称、Slot Group、Bone Mask与Blend Profile保持不同职责。
- Control Rig作者图组织Forwards Solve、控制目标、Foot Placement和Full Body IK。目标构造与实际求解对作者可见；Goal Contribution打包、Goal Assembler、必要边界转换与工作区分配由编译器展开，不再是必接作者节点。
- 保留显式Inertialization、局部分支连续性、Pose可用性、实际调用身份和UE式状态／转换编辑。不能为了减少节点隐藏惯性化，也不能用重新排版或把旧流水线塞进子图代替职责重构。
- 一颗作者节点可生成多条内部operation，Source Map同时标识作者节点、内部步骤、图版本、调用路径和输出端口。Unity普通Play中的真实角色继续提供只读运行观察；PoseGraph仍不运行FlowCanvas作者图；原生EventGraph作为独立输入生产者由动画宿主推进，窗口不创建预览角色或私有时钟。
- 正式Pose读取/配置、类型化Mutation与领域校验继续保留；C# authoring提供两个显式export_code/generate_assets入口。当前资产完整导出C#，已编译代码重建并保存明确范围；旧Document、五工具及专属校验退役，不新增中央Validator或整包同步。
- 先完成代码与编辑能力，最后通过正式资产事务迁移Corin。此次迁移有真实的数据组织变化；上一轮“仅换UI无需迁移”的结论只适用于旧范围。迁移保持可保留的图／节点／状态identity及原动作语义，删除被替代的作者配置后统一发布Float32、Fixed和Projection。

## Capabilities

### New Capabilities

- `pose-flowcanvas-editor-preview`：扩充原有未归档能力，覆盖UE式作者分层、Montage与Slot、骨骼混合、Control Rig、源映射及普通Play观察。

### Modified Capabilities

- `character-presentation-pose-graph`：作者层次与内部运行拓扑分离；直接资源Player、层内Slot、曲线传播、控制图、独立Pose编译及角色接口装配。
- `graph-authoring-domain-framework`：统一角色、接口、作用域、字段、交互及多operation来源合同。
- `graph-authoring-editor-shell`：同一窗口按图职责提供UE式导航、状态连线、作者详情及运行观察。

本轮仅修改公共输入与authoring相关delta；删除已退役Document delta，公共参数传播由独立只读Blackboard delta唯一维护。其它现行规范的精确冲突、适用边界和实施同步要求集中记录在design.md“现行规范对账”；未将未改动的旧条款宣称为已经一致。

## Impact

- Pose作者Capability、资源选择、图角色／接口、Slot与Group定义、Montage、Mask／Profile、Rig控制目标和正式C#读取/配置能力。
- Pose Compiler的Closure、语义展开、Topology、Stage／Value／Workspace规划及Source Map；允许扩展正式动画指令和所需运行描述，不建立第二Compiler或Runtime。
- 将动画编译输入构造从Character Semantic Frontend中拆出。Pose窗口调用动画模块入口；角色构建器分别取得Gameplay和Pose结果，再绑定Fact、事件图唯一变量合同、Slot和Timeline动画接口。不能以跳过技能检查、读取旧角色产物或伪造输入实现所谓独立编译。
- 复用既有Linked Pose接口、Implementation／Group绑定和调用生命周期承载Animation Layer；复用原Native帧事务、Foot／Goal／FBBIK算法、Animancer source backend和Final Publication。
- Montage式设置直接扩展现有Action Timeline动画内容合同；技能图重构、动作准入、战斗规则和网络执行仍归原技能提案，不借此调整它们的业务行为。
- 当前v28观察元数据是可复用基础，不承诺新作者结构仍使用v28产物。最终schema由同一编译链一次升级，旧产物明确Stale，禁止双reader、自动Build或运行时修复。
- 本轮仅更新计划文档，不实施代码、不迁移资产、不Build、不归档。tasks仅列实现、清理、文档同步与迁移／发布工作。

## r2公共输入、身份与预览

CharacterAnimationInputContract的共享实例变量只引用事件图唯一声明/Contract/Layout/Frame；Fact、Slot、World、子图公开参数和source-local曲线保留原来源。Get、条件、BlendSpace和完整角色/Pose预览使用同次成功发布的精确类型变量帧，Source Pending不回退事件图状态，输入发布不等于最终Pose提交。单资源查看使用原资源调参合同；所有旧消费签名迁完后事件图任务才删除固定ProgramParameterFrame。

C#导出/生成保留完整曲线、布局、动态端口、资源及引用。生成范围内物理对象可替换，图/节点/变量业务ID保持，内部引用使用新对象，明确恢复Profile/Definition根挂接，不依赖旧生成子资产GUID。人工编辑不自动写代码，生成不自动合并未导出修改，不自动Build。具体公共调用迁移由只读Blackboard change唯一实施，旧方案其它任务不并入。
