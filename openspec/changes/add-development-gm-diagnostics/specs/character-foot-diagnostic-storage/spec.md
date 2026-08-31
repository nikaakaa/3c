## MODIFIED Requirements

### Requirement: Foot 诊断必须共享一次解析的正式事实

唯一离线 Finalizer MUST 在收到精确 Sealed Foot 原始包后完整读取校验 CSV 和 geometry，将同次内存事实直接交给唯一 Publisher。Editor 采样结束且 Writer 已封口时 MUST 自动向该 Finalizer 提交完整包；Development Player 停采 MUST 只封存原始记录和清单，由离线接收端提交同一个 Finalizer。完整包未进入离线分析前 MUST 明确显示待分析，不得假装已有诊断结果。

Publisher MUST NOT 先写出完整 facts.json 再全文读取，也 MUST NOT 重新计算 Foot Runtime 或查询世界。同一输入身份、Analyzer 版本和配置已经完成发布时，查询入口 MUST 读取既有结果而非重复计算。

#### Scenario: Editor 录制完成后自动分析

- **WHEN** Editor 的共用 CSV writer 已封口且包完整、至少存在一帧
- **THEN** Editor 适配器 MUST 自动提交唯一离线 Finalizer
- **AND** 所有 Target MUST 消费相同输入身份和既有完整统计规则

#### Scenario: Player 停采后接收原始包

- **WHEN** Development Player 封存完成，随后离线工具接收该精确包
- **THEN** Player MUST 不运行完整分析，离线工具 MUST 调用相同 Finalizer
- **AND** 与同身份、同配置的 Editor 输入 MUST 使用同一个 Analyzer 和 Publisher

#### Scenario: 不完整包进入分析

- **WHEN** 原始包缺失、损坏或清单声明采样中断
- **THEN** Finalizer MUST 返回明确完整性诊断，不将其作为完整样本发布质量结论
- **AND** MUST 不补齐缺失帧、重新采样或改写历史包
