# btsmtl-agent-authoring-document-sync Specification

## Purpose
定义 Agent Authoring Document 与正式作者 metadata、Graph/Timeline/Presentation Mutation、事务 apply 和反向发布之间的唯一同步合同，保证 Agent 不维护第二套节点模型、字段规则或 owner 推断路径。

## Requirements

### Requirement: Agent Document必须从正式作者metadata投影完整闭包

Agent Document MUST支持从零创建或完整修改Character Skill与Presentation闭包，包括SkillDefinition、Entry Graph、嵌套Graph/State/Condition、Macro、Skill Timeline、TreeClip、局部Blackboard、Pose Graph、PoseStateMachine、Animation Layer、Control Rig、Slot/Group、Mask、Blend Policy、Animation Producer、Clip Curve与typed引用。Document JSON MUST只表达稳定业务identity、kind、typed properties、logical ports、references、owner和闭包关系。

正式作者类型、字段、引用关系和正式Mutation写入方法上的metadata MUST是上述字段可见性、可写性、类型、端口、owner和闭包规则的唯一来源。Agent Document MUST不维护第二份节点模型、字段表、端口表、Pose模型或owner推断。

#### Scenario: Agent从空目标创建完整Skill

- **WHEN** Agent目标包含合法Skill Entry Graph及其完整Graph/State/Condition/Macro/Timeline/TreeClip/Blackboard闭包
- **THEN** checkout、dry-run和apply MUST按同一Document hash校验完整闭包并进入同一事务
- **AND** apply后的正式Skill作者入口 MUST能读取相同的stable identity、owner和引用关系

#### Scenario: Agent修改Skill和Pose的跨域引用

- **WHEN** Agent同时修改Skill Timeline的AnimationSlot引用和Presentation Pose Graph的Slot/Mask组合
- **THEN** dry-run MUST在同一Document hash中核对两个正式owner的引用闭包
- **AND** apply MUST使用与人工作者入口相同的typed Mutation和唯一事务

#### Scenario: Agent提交metadata未声明内容

- **WHEN** JSON目标写入metadata未声明的节点kind、字段、port、引用、owner、Pose空间或Clip Curve channel
- **THEN** strict parser、Reconciler、Validator或Mutation preflight MUST返回稳定路径诊断并拒绝目标
- **AND** MUST不按C#类型名、显示名、Unity序列化字段、SerializedProperty路径或Compiler operation猜测能力

#### Scenario: 正式作者内部实现变化

- **WHEN** 正式作者类型、文件组织或Compiler实现变化但Agent可见kind、typed field、logical port、owner和闭包语义不变
- **THEN** Agent Document schema MUST保持不变
- **AND** Exporter、Parser、Reconciler、Validator、Compiler和原生UI MUST继续消费同一正式metadata投影

### Requirement: Agent Document不得拥有第二套作者写入入口

Agent Document的Skill、Timeline、Blackboard和Presentation修改 MUST通过现有正式authoring Mutation Adapter进入同一Document Transaction。MCP MUST继续只暴露五个生命周期工具；MUST不增加节点、边、属性、Pose或Clip专用工具、第二Reconciler、第二Mutation Service或第二Undo owner。

#### Scenario: Document和人工作者修改同一对象

- **WHEN** 人工作者入口和Agent Document修改同一个Skill节点、Timeline字段、Pose policy或Clip Curve
- **THEN** 两条入口 MUST使用同一正式Mutation、Validator、owner、Undo和保存语义
- **AND** 不同入口的canonical结果 MUST一致

#### Scenario: Agent目标apply失败

- **WHEN** 任一Skill、Timeline、Blackboard、Pose或Clip owner的Mutation、保存或反向导出失败
- **THEN** 唯一事务 MUST恢复所有已修改owner和正式package
- **AND** response MUST不报告`applied=true`、`saved=true`或`Clean`
