# Skill authoring 数据模型统一实施记录

## 当前基线

本记录从 2026-09-12 开始维护，目标是落实本 change 的数据层与状态机转移收口。正式 Unity authoring asset 仍是唯一真相，Agent Document 只作为 v8 工作副本，FlowCanvas 继续拥有图拓扑、Node/Edge identity 与布局。

当前工作区存在其它未提交改动，实施只修改本 change 明确涉及的 Skill authoring 文件，不回退、不覆盖无关改动。迁移前必须保留精确 Definition、v7 package、Step/Edge 对应、条件图闭包、Macro/Timeline/Blackboard 引用与当前 hash。

## 已确认的数据双写

状态机转移当前存在两份来源：

| 来源 | 保存内容 | 当前消费者 |
|---|---|---|
| `BtsmtlSkillStepPort` | condition、priority、abortPolicy | 旧步骤端口、部分 Closure/Copy/Exporter/Validator/Applier |
| `BtsmtlSkillFlowConnection` | condition、priority、abortPolicy | 状态机编译、部分 Closure、Document Edge 导出 |

当前目标是状态机的 Edge 唯一保存转移参数；`Sequence`、`Selector`、`Parallel` 的步骤语义继续保留。状态机转移还需要显式 `order`，不能按 UID、字典顺序或节点位置重新推断。

## 当前 v7 资产证据

精确 Corin v7 package 当前包含 90 个 Skill Graph。状态机 Graph 中存在 20 条带 `edge.conditionGraphId` 的转移，同时仍保留 20 条旧 `steps[].conditionGraphId`。`@any` 还存在一个未连接的空步骤。

这说明旧 steps 尚未具备删除条件。必须先完成 Edge 条件图的闭包、owner、复制、Document 校验和顺序收口，再删除状态机 steps。

## 实施顺序

1. 完成 Step/Edge 逐实体差异与并列顺序报告。
2. 建立状态机 Transfer payload、固定端口、多连接容量和显式 order。
3. 让条件引用、Closure、ClosureIndex、GraphCopy、Exporter、Validator、Applier、Occurrence 和 SourceMap 只读取 Edge 转移数据。
4. 按现有 Document 事务把 v7 精确迁移目标写成 v8，失败恢复全部 owner 与 package。
5. 通过 re-checkout、无修改 dry-run、正式 validate 与技能编译核对 identity、owner、条件、优先级、中止策略和 order。
6. 删除状态机 Composite/steps、旧补读、重复字段分支和一次性迁移入口，并同步父 change、current spec 与技能合同。

普通组合节点的 steps 不进入本次状态机迁移；其它领域的同名 BaseGraph 类型按实际消费者保留。
