# character-foot-diagnostic-storage Specification

## Purpose

规定 Foot 诊断报告的事实复用、紧凑明细、索引、身份与历史证据保存规则。旧工具已经删除，当前分析实现由独立 Schema-driven Analysis 承接；本规范不恢复旧 Reader 或承诺采样停止后自动分析。

## Requirements

### Requirement: Foot 诊断必须共享一次解析的正式事实

对已完成采样的正式产物，离线分析 MUST 由唯一 Reader 校验身份并读取一次正式事实，同次内存结果直接用于规则计算和报告发布。报告生成 MUST NOT 先写出展开 facts.json 再全文读回，也 MUST NOT 重算 Foot Runtime 或查询世界。当前生成采样以 Completed Artifact 为输入，分析由独立 Host workflow 显式触发；历史 Analyzer、Publisher、Diagnosis Store、旧 CSV Reader 与自动停止后分析入口 MUST 不因本合同恢复。

#### Scenario: 对完成的录制显式分析

- **WHEN** 作者通过正式 Host workflow 分析合法 Completed Artifact
- **THEN** 唯一 Reader MUST 校验并解析一次输入，规则和报告共享同次事实
- **AND** 所有报告 MUST 绑定同一份采样、Schema 与分析规则身份

### Requirement: 小报告与完整明细必须分离且可追溯

每类诊断 MUST 保留问题、规则、eligible/matched、发生率、完整分布、Health/Evidence 和至多五条代表预览。全部事件和派生观察 MUST 只在唯一紧凑明细存储中保存，报告 MUST 引用正式记录身份与原始帧范围，不复制全量帧、候选或阶段对象。原始 CSV 和几何 MUST 保持完整。

#### Scenario: 同一事件用于质量与阶段归因

- **WHEN** 多个 Target 引用同一已分析事件
- **THEN** 明细 MUST 只保存一次，所有引用 MUST 指向同一记录
- **AND** 预览截断 MUST 不改变 eligible、matched、评分和全部事件的可枚举性

#### Scenario: 查看一个事件

- **WHEN** 请求合法记录 ID
- **THEN** Reader MUST 按正式字节索引读取并验证对应明细
- **AND** MUST NOT 重跑 Analyzer、Replay 或全文读取原始事实镜像

### Requirement: 发布必须保持身份和完整性

分析报告索引 MUST 引用唯一正式采样 manifest，记录输入及子表 hash、Schema、分析版本、coverage和明细索引身份，不生成第二采样 manifest。报告、明细与索引 MUST 在同次完整发布中生效。缺失或损坏的正式记录 MUST typed 拒绝，不提供旧 JSON fallback。

#### Scenario: 旧包缺少新存储

- **WHEN** 新 Reader 收到旧 facts.json 或缺索引的目录
- **THEN** MUST 明确拒绝，历史包 MUST 不被改写
- **AND** 原始输入满足当前 Schema Reader 合同时 MAY 在显式新目录离线生成新版本报告，不增加旧格式 Reader

### Requirement: 存储迁移不得改变质量结论

七维评分、规则阈值、事件资格、去重、分位数与最大值算法 MUST 保持不变。Replay MUST 只迁移诊断产物引用，输入、Body、Schedule 与 Proof 比较合同 MUST 不改变。

#### Scenario: 同一个封存原始包重新分析

- **WHEN** 新存储链处理同一份合法 raw
- **THEN** 全部 Target 的 eligible/matched、分布、Health/Evidence 和加权质量 MUST 与原规则一致
- **AND** 输出 MUST 独立保存，性能测量 MUST 不被解释为行为改善
