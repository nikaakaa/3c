## MODIFIED Requirements

### Requirement: Agent Document必须从正式作者metadata投影完整闭包

Agent Document MUST支持从零创建或完整修改Character Skill与Presentation闭包，包括SkillDefinition、Entry Graph、嵌套Graph/State/Condition、Macro、Skill Timeline、TreeClip、局部Blackboard、Pose Graph、PoseStateMachine、Animation Layer、Control Rig、Slot/Group、Mask、Blend Policy、Animation Producer、Clip Curve与typed引用。Document JSON MUST只表达稳定业务identity、kind、typed properties、logical ports、references、owner和闭包关系。

正式作者类型、字段、引用关系和正式Mutation写入方法上的metadata MUST是上述字段可见性、可写性、类型、端口、owner和闭包规则的唯一来源。Agent Document MUST不维护第二份节点模型、字段表、端口表、Pose模型或owner推断。

Skill字段访问、逻辑端口与引用 MUST消费原业务节点和FlowCanvas适配共用的正式业务定义，Document只处理现有包格式与事务接入，不得成为两套重复业务定义之间的唯一桥。本次参数类型和存储位置调整 MUST保持实施基线的公开kind、typed properties、values、端点与owner形状；独立变更正式升级协议后 MUST消费唯一新合同，不得恢复旧版。无法无损映射的差异 MUST明确报告，不能自行升级版本、放宽parser或增加兼容reader。

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

#### Scenario: 内部参数抽取保持包形状

- **WHEN** 节点参数从宿主字段迁入共同业务参数对象但公开合同不变
- **THEN** 现行Document导出、dry-run和apply MUST保持相同可见字段与业务结果
- **AND** 该内部改动 MUST不增加新的owner、端点格式或生命周期工具
