## ADDED Requirements

### Requirement: Foot 诊断必须共享一次解析的正式事实

停止采样后的唯一后台 Analyzer MUST 完整读取校验 Sealed CSV 和 geometry，并将同次内存事实直接交给唯一 Publisher。Publisher MUST NOT 先写出完整 facts.json 再全文读取，也 MUST NOT 重新计算 Foot Runtime 或查询世界。

#### Scenario: 录制完成后自动分析

- **WHEN** CSV writer 已封口且至少存在一帧
- **THEN** 现有后台 Finalizer MUST 自动执行一次 Analyzer 和 Publisher
- **AND** 所有 Target MUST 消费相同输入身份和既有完整统计规则

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

唯一 manifest MUST 保存输入与几何 hash、schema、Analyzer 版本、coverage和明细索引身份。报告、明细、索引、manifest MUST 在同次完整发布中生效。缺失或损坏的正式记录 MUST typed 拒绝，不提供旧 JSON fallback。

#### Scenario: 旧包缺少新存储

- **WHEN** 新 Reader 收到旧 facts.json 或缺索引的目录
- **THEN** MUST 明确拒绝，历史包 MUST 不被改写
- **AND** 所需原始 CSV 合同时可在显式新目录离线生成新版本报告

### Requirement: 存储迁移不得改变质量结论

七维评分、规则阈值、事件资格、去重、分位数与最大值算法 MUST 保持不变。Replay MUST 只迁移诊断产物引用，输入、Body、Schedule 与 Proof 比较合同 MUST 不改变。

#### Scenario: 同一个封存原始包重新分析

- **WHEN** 新存储链处理同一份合法 raw
- **THEN** 全部 Target 的 eligible/matched、分布、Health/Evidence 和加权质量 MUST 与原规则一致
- **AND** 输出 MUST 独立保存，性能测量 MUST 不被解释为行为改善
