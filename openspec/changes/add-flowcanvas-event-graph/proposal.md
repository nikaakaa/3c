## Why

当前问题是动画输入职责尚未收口：FactProjector 同时处理数据对齐和动画分类，Pose 输入合同仍混合不同来源的声明，Blackboard 与实际 Fact 消费不一致；Corin EventGraph 没有业务节点，却已成为动画装配的强制依赖。继续补一个新动画效果不能解决这些问题。

r3 按用户要求先整理现有数据、计算和消费者，再保持原行为完成职责迁移。不新增步频、播放倍率、速度平滑或其它未经提出的动画需求；没有实际需求时不强制挂空事件图。

## What Changes

- 先建立现有输入职责表，区分正式角色事实、动画派生判断、作者实例变量、子图公开输入、随姿势传播的曲线和节点/资源配置，记录真实生产者和消费者。
- 只迁移已有且明确归为作者逻辑的动画计算，保持原公式、阈值、时钟、状态判断和消费者。FactFrame 继续是正式只读输入，不把其中全部计算搬到 EventGraph，也不为制造非空图复制事实或恢复 Action/Foot 变量。
- **BREAKING**：EventGraph 按正式需求装配。没有绑定图且没有动画变量需求时，动画沿同一正式 Fact/Pose 链执行，不要求占位图或虚构变量帧；存在变量需求但缺少图/变量绑定时仍明确失败，绝不补默认值。显式绑定的图按其原生逻辑执行，不因输出变量为空被偷偷跳过。
- 与 Pose 任务统一来源合同：变量引用同一原生声明，Fact 直接读正式事实，子图参数遵循调用接口，曲线从输入姿势读取，配置留在原节点/资源。删除无用途的重复声明和按固定名称推断来源的规则，不禁止编译器为执行生成只读索引。
- 补齐实际所需的 Fact 输入节点目录和直接 API 接线，复用正式 Fact schema/读取能力；未支持类型明确报告，不再维护脱离正式事实合同的第二清单。
- 保留已确认的 FlowCanvas 原生事件执行、实例隔离、错误/Reset、唯一 Contract/Layout/Frame、C# authoring 两个显式工具和现有 Pose 编译运行。
- 公共 C# 输出器继续通过事件图薄适配读取完整对象并输出正式 API，保持稳定逻辑身份和明确根绑定；不恢复 Document、整图 DTO、中央 Validator、自动源码同步或另一个 MCP。
- 当前已删除的固定 motor 桥和 EventGraph Document 不重新引入。只清理新职责表证实多余的占位绑定、重复输入处理及无消费者内容，保护已正确的曲线、状态机与运行算法。

## Capabilities

### New Capabilities

- flowcanvas-event-graph：可复用原生事件图、宿主、变量与正式直接作者/C#输出能力，保留既有路线。
- character-animation-event-graph：按真实需求装配的动画事件能力，现有输入分类、只读Fact接入和同次变量交接。

### Modified Capabilities

- graph-authoring-domain-framework：保留原生EventGraph与编译Pose/Skill的职责边界。
- character-animation-pipeline：按正式输入需求决定是否需要事件宿主，保持唯一Fact/Pose链和可选的唯一变量生产者。

Pose Blackboard、Get、条件、BlendSpace、参数/曲线声明和编译绑定的完整修改仍归 [Pose 只读输入任务](../refine-pose-graph-readonly-blackboard/proposal.md)；本任务提供接口要求与生产侧接入，不重复维护它的同名规范增量。

## Impact

- 本任务：正式Fact到宿主的适配、原生事件图作者 API/薄适配、动画根图引用与生产端合同，以及 Corin 占位事件图/recipe 的明确处理。
- Pose/Presentation 所属任务：FactProjector 的既有事实/判断消费者边界，CharacterAnimationInputContract、Blackboard、Get/条件/BlendSpace、编译输入绑定与完整Preview消费。需要移动公共事实或改变动画行为时先由用户决定，不在本任务越权改动。
- C# authoring 任务：保留公共export_code/generate_assets、通用输出及生成保存；事件图仍只作薄扩展。
- r2 的运行与作者基础作为已有基线，不以空图、Profile引用或构建成功宣称业务整理完成。本轮只更新本任务文档，不改代码/资产，不发送实施指令。
