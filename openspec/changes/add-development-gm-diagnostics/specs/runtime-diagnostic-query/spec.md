## ADDED Requirements

### Requirement: 诊断入口必须复用唯一分析与查询链

诊断工具 MUST 以精确封存包为输入，提供 `summary/events/detail/frame` 查询合同。Foot MUST 复用现有 Analyzer、Publisher、紧凑 Store/Reader 与评分规则；GM、Editor、MCP MUST 不各自重新分析、重新采样或复制全部明细。

#### Scenario: GM 停采后在 Editor 查看结果

- **WHEN** Editor 接收完整封存包
- **THEN** MUST 通过唯一 Finalizer 完成该输入身份和分析版本的发布
- **AND** 后续所有查询 MUST 读取同一结果，不重复运行 Analyzer

### Requirement: 诊断结论必须区分证据缺失与业务失败

诊断 MUST 展示内容身份、覆盖、原始边界、问题事件和原始帧引用。Action 时间线 MUST 根据记录说明 Select、Sample、撤销和生命周期登记经过的阶段，不重建第二套可执行 Action 状态机。缺失证据 MUST 明确标示，不得解释为无问题、满分或已知根因。

#### Scenario: 采样开始晚于动作 Select

- **WHEN** 记录中有 Sample 但采样覆盖不包含动作开始
- **THEN** 查询 MUST 说明当前覆盖内缺少 Select 证据
- **AND** MUST 不直接认定运行时从未产生 Select

### Requirement: 双端诊断必须尊重本地表现边界

Gameplay 对照 MUST 使用一致的 Actor、canonical tick、模型和内容身份。Presentation 对照 MUST 展示 Body sample、Clip/phase、分支与采样间隔，优先定位单帧 Source、Goal、IK、Final 的变化；不得要求两端 RenderFrame、Completion 或骨骼姿态逐位相同。

#### Scenario: 两端骨骼不同但逻辑状态一致

- **WHEN** 同一逻辑边界的 Gameplay 身份匹配而表现采样时钟不同
- **THEN** MUST 分开显示同步一致性与表现证据
- **AND** MUST 不把本地 IK 差异直接判为确定性状态失步

### Requirement: 诊断扩展不得改变已有 Foot 评分数学

接入运行时采样与多目标记录 MUST 保持现有七维权重、资格、去重、分布和证据规则；不同输入、覆盖或规则版本 MUST 明确区分。历史格式不匹配 MUST 明确拒绝；显式重新分析只能写入新结果目录。

#### Scenario: 相同原始 Foot 数据从不同入口提交分析

- **WHEN** 数据身份、完整性与 Analyzer 配置完全相同
- **THEN** MUST 取得相同的 Foot 诊断结论与来源引用
- **AND** MUST 不因 GM 或 Editor 入口不同采用另一套评分
