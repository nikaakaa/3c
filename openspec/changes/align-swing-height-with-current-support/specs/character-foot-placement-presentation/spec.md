## MODIFIED Requirements

### Requirement: Ground Envelope必须来自可达Edge与上侧凸包

Ground Envelope Builder MUST把Raw Contacts投影到脚步纵向与Component Up组成的二维平面，稳定生成Edge候选并在同一路径距离保留最高候选。Path Start与Target Landing MUST作为首尾端点保留；CastAbove和CastBelow只属于查询范围，不得成为Reachability限值。

正式Profile MUST提供米制MaximumReachableVerticalEdge。任一Edge超过限值时 MUST发布UnreachableEdge与首个Invalid Segment，不得删除障碍后继续构造Hull、沿用旧Envelope或借用KCC Step高度和腿长替代。全部Edge合法时，Builder MUST输出位于所有保留候选上侧或与其重合的连续上侧Convex Hull。Envelope MUST表达规划路径的地面下界，不改变Foot XZ、直接驱动Pelvis或充当另一当前位置Swing脚的高度目标/安全下限。

#### Scenario: 路径经过不可达垂直面

- **WHEN** 任一Edge沿Component Up的高度超过MaximumReachableVerticalEdge
- **THEN** Ground Path MUST发布UnreachableEdge且Accepted Envelope为空
- **AND** Raw Contacts与Edge事实 MUST保留在同一成功Seal的只读诊断页

## ADDED Requirements

### Requirement: Swing可见高度与安全下限必须属于同帧当前支撑

唯一Foot Owner MUST使用同帧CurrentSupport的零净空Sole目标作为Swing Height Reference；正式FootHeight沿ComponentUp叠加，动画XZ不变。普通Swing安全下限 MUST使用同一个高度参考。CurrentSupport的位置、查询身份和脚侧不得由另一个预测路径位置补全。

目标高度历史和Residual换代 MUST消费该支撑的正式几何变化。纯未来Path身份更新或正常动画XZ位移 MUST不单独重捕可见Residual；GroundPath仍保存规划身份，MUST不伪装为CurrentSupport的位置来源。Diagnostics MUST分开规划Envelope与可见Height Reference，按实际运行来源重算目标、历史和安全下限，不能重写旧包或放宽质量规则。

#### Scenario: 实际脚不在预测路径走廊中

- **WHEN** 预测GroundPath与当前动画脚不具备相同空间位置，但CurrentSupport合法
- **THEN** Swing目标与安全下限 MUST继续使用同一当前支撑与正式FootHeight
- **AND** 规划Envelope MUST仅保留自身事实，不因为更高而再次抬高该当前位置的脚，也不触发临时fallback选择

#### Scenario: 当前支撑不可用

- **WHEN** CurrentSupport的必需查询或Frame/Completion/Side/World身份不合法
- **THEN** 当前Swing支撑高度 MUST发布typed unavailable，不得用默认值、旧结果或预测包络发布成功参考
- **AND** 已持有的Contact Anchor MUST仍由原生命周期处理，不被当前支撑拒绝反向释放

#### Scenario: 正式Contact及Release交接

- **WHEN** 脚进入Contact、Locked或从接触进入Release
- **THEN** 既有Anchor、完整世界Residual捕获与同帧推进、旋转权重和唯一Goal归属 MUST保持
- **AND** Release的Swing目标 MUST与普通Swing使用同一高度来源，不建立第二末端目标或新增膝角后处理
