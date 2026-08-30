## ADDED Requirements

### Requirement: Foot质量评分必须按唯一业务维度去重

Foot Diagnostics MUST只将下陷穿透20%、接触未贴合20%、普通Swing平顺度15%、Path变化连续性15%、接触状态交接15%、腿部姿态可达性10%、锁脚水平稳定性5%作为唯一加权质量维度。阶段原因、合同、原动画及反事实 MUST只提供Evidence，不再生成参与总分的平行Health。

#### Scenario: 同一跳变出现在多个阶段

- **WHEN** 同Side同相邻帧对同时存在Plant、Floor、Contact Acquisition和最终可见跳变事实
- **THEN** 质量 MUST只按最终输出及互斥Swing/Path/Contact域计一次
- **AND** 原阶段事实 MUST保留为不重复扣分的Evidence

### Requirement: Foot加权总分必须保持浅层参考与缺失边界

唯一Publisher MUST按固定权重发布版本化`quality-score.json`并保留所有维度Health、Evidence、次数、分母、规则、贡献和代表事实引用。总分 MUST明确为暂定粗略参考，不得名为Pass/Fail或代替视觉验收。旧文件级无权聚合 MUST删除。

#### Scenario: 全部维度可计算

- **WHEN** 7个唯一质量Target均有合法观测Health
- **THEN** 总分 MUST等于各分项Health乘固定权重之和
- **AND** 低样本维度 MUST仍显式出现在弱证据列表，Evidence不得改变Health权重

#### Scenario: 缺事实或零样本

- **WHEN** 任一维度缺少必需可见事实或eligible为零
- **THEN** 该项及完整总分 MUST发布Unavailable而非0或100
- **AND** 摘要 MUST保留原权重，发布可计算权重、已知贡献、分数可能区间及缺失原因

#### Scenario: 只有Path归因阶段缺失

- **WHEN** 最终物理输出事实完整而Path中间阶段缺失
- **THEN** 质量 MUST仍按已观测最终跳变计分
- **AND** 首个放大阶段 MUST保持Unavailable，不得猜造原因

### Requirement: Foot评分必须只消费正式事实且保留版本证据

采样、Analyzer、Publisher MUST保持一条链。接触未贴合 MUST使用同Event已验证Anchor与最终物理Heel/Toe；Releasing不适用，缺接触面不得补默认值。FullAnchor水平与Sliding政策 MUST分型。质量规则升级 MUST保留历史原包，不兼容伪造新列，不把换规则后的分差解释成行为改善。

#### Scenario: 回算同一旧原始样本

- **WHEN** 当前CSV包含新规则所需的全部正式事实
- **THEN** Analyzer MAY在独立副本生成新版本结果
- **AND** 原包及原评分 MUST保持不变，比较 MUST明确规则版本差异
