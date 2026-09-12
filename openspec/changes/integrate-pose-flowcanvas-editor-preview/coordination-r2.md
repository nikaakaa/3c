# Pose公共输入与C# authoring r2对接记录

日期：2026-09-13。用户广播：2026-09-13-authoring-r2-plan。对接规划窗口：01a09594-1751-7512-b8c0-08b04185055b。

本轮仅按用户指定对接本旧方案中的公共输入和authoring协议条款；未在proposal/design/tasks中发现另一明确planner身份。旧方案的层、Slot、Action Timeline、曲线算法、Control Rig、独立动画Compiler和观察业务保持原范围，不并入只读Blackboard任务。implementation.md与authoring-inventory.md是历史证据，本轮不改写。

## 唯一公共基线和实施归属

- 两个显式MCP、完整C#输出/生成、明确范围替换、源码与未导出修改边界：[C# authoring r2](../remove-agent-authoring-use-native-csharp/design.md)。
- 原生事件图、唯一变量声明/Contract/Layout/Frame和原生操作API：[事件图方案](../add-flowcanvas-event-graph/design.md)。
- CharacterPoseGraphAuthoringAdapter、Pose Mutation、输入合同、Get/条件/BlendSpace及运行/Preview消费：[只读Blackboard r2](../refine-pose-graph-readonly-blackboard/design.md)。公共实施只在该目录任务1.x/2.x登记，本旧方案不重复列同一实现任务。

## 旧协议退出

旧btsmtl-agent-authoring-document-sync delta要求升级包、整包同步和反向导出，与r2直接冲突，已从本change删除，不再参与归并。Agent能力删除由C# authoring change拥有；不在本change新增另一份REMOVED副本。

旧graph-authoring-domain-framework中对“Authoring Capability Catalog必须是UI与Document的唯一语义目录”的MODIFIED块也退出。本change将其中Pose领域字段、端口与局部规则保留为新的Pose专用API要求；共享旧要求的删除与新人工/C#要求由C# authoring change唯一拥有。

旧character-presentation-pose-graph中的“Pose参数必须通过typed页面和显式解析传播”不再与独立只读change重复修改。其Curve组合、内部Resolver与原场景内容已并入只读change的同名delta，旧组合节点实现和任务仍归本方案。

## 历史已完成记录

下列任务在r2前已标记完成，反映当时Document链路的工作；按本轮退役规则从当前tasks移出，仅保留历史状态，不改成未完成或把C#替代能力标成完成。

| 原任务 | 原状态 | 原文 |
|---|---|---|
| 8.1 | 原已完成，协议现已退役 | 在当前唯一v6基础上统一升级v7动画字段与图角色，保留技能Macro和其它领域正文。 |
| 8.2 | 原已完成，协议现已退役 | 同步Capability、严格codec、Exporter、Reconciler、typed Mutation、owner及五生命周期说明。 |
| 8.3 | 原已完成，协议现已退役 | 将新图、Rig关联设置和Timeline动画字段纳入同一保存、回滚与反向导出事务。 |
| 8.6 | 原已完成，协议现已退役 | 将动画目录与Document动画字段处理接到独立动画合同，保留现有整包事务，不以Gameplay编译成功作为动画编辑前提。 |
| 9.3 | 原已完成，协议现已退役 | 在成功反向导出后删除退役作者节点、无消费者Source Slot／Binding及迁移专用旧读写代码。 |

## 规划比较与保留条件

本轮保留同一次成功发布的Float/Int32/Bool、实例/采样/Simulation tick/Reset/版本、输出租约以及Source Pending不回退事件状态的边界。Get、条件、BlendSpace和完整Preview消费同一合同；单资源查看仍使用原资源合同。生成对象可替换物理身份，但业务ID、内部新对象引用和明确Profile/Definition根挂接必须恢复。人工编辑不自动导出，generate_assets不自动合并未导出修改，不自动Build。

所有验收条件留在设计与执行证据，不新增测试或手动验证tasks。本轮不改代码/资产，不发送执行消息或窗口回执。

## 从任务清单移出的旧阶段说明

以下原文仅作历史背景，不作为r2的Document/Build执行要求。

## 历史对账（2026-09-10，非r2执行协议）

此前将Capability注册、Compiler内部operation支持和一次旧拓扑Build误判为完整作者架构。对照当前Corin根图、Projection和面板代码后，以下任务重新打开：2.5、4.4、7.5；4.2、7.3、9.2、9.4已按当前代码、Document和正式产物完成。现有character-animation-blend-stack、character-animation-transition-routing-module和Presentation authoring spec已经要求显式BlendStack、独立Inertialization、Layered Blend Per Bone及正式详情选项源，本次不修改spec，只按spec修正实现。

本轮已完成的增量：根图通过Document v7正式事务接入通用Inertialization，Locomotion↔Turn两条过渡选择该逻辑；Layered Blend Per Bone已把Local／Component Pose Space贯通到Program与Worker；Pose详情已接入Animation Channel、Slot、Graph、Linked Pose、Presentation Fact、Gameplay State和Pose History候选。Corin没有Motion Matching或其它多源Selection source，也没有真实UpperBody Overlay，因此当前不伪造BlendStack或Layered节点；剩余任务继续围绕真实Overlay、观察和最终产品发布推进。

Action Slot外接通用Inertialization的尝试已被正式Build拒绝并回滚：当前Pose-only输入不携带完整Character的ACL Action producer endpoint，不能凭空生成Action route。保留Slot-owner的通用编译/runtime扩展，待独立Action route contract完成后再接入，不改变现有Pose-only Build隔离。

## 精确旧要求退役

Pose旧“共享Capability与类型化Presentation Mutation”要求包含必须保留的Document命名场景，不能用MODIFIED静默删场景。本delta显式REMOVED旧要求并ADDED“Pose authoring必须复用正式类型化API与领域校验”，保留业务规则，删除旧协议场景别名。Editor Shell场景名称对齐现行“打开BTSMTL Gameplay Graph”，不恢复RootTree。

## 本轮文档检查

refine-pose-graph-readonly-blackboard与integrate-pose-flowcanvas-editor-preview均通过严格OpenSpec校验；文档diff检查通过。只读任务保留原3项完成，新增8项r2任务未完成；旧方案5条已退役协议完成项移入本文件，其它任务原文与勾选状态保留。这些是规划检查，不是业务代码、Unity或运行验收结果。
