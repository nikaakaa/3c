## ADDED Requirements

### Requirement: Composition必须校验Body Motion与Solver垂直能力

Session Composition MUST从compiled Program读取Body Motion descriptor与required world capabilities，并在Runtime Launcher创建Session前验证选定WorldSolver真实支持`AirborneVerticalMotion`。Capability校验 MUST不按Network Model、Scene、Actor或Host放宽；失败 MUST按现有owner释放已经准备的资源，MUST不切换Solver、关闭重力或使用Grounded-only fallback。

#### Scenario: Solver缺少AirborneVerticalMotion

- **WHEN** Program要求AirborneVerticalMotion但Solver descriptor不支持
- **THEN** Preparation MUST fail-closed
- **AND** 错误 MUST包含Program identity、Solver identity与缺失capability

