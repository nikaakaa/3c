## ADDED Requirements

### Requirement: DotRecast Navigation Surface Solver不得声明空中垂直能力

当前DotRecastWorldSolver只通过nearest-poly、MoveAlongSurface、height projection与Surface reconstraint处理Navigation Surface运动，因此 MUST不声明`AirborneVerticalMotion`。需要该capability的Program与DotRecast Solver组合 MUST在Session Active前失败。DotRecast MUST不丢弃request Y、保持假Grounded、把Actor吸附到NavMesh、按Network Model关闭Body Motion、调用Unity Physics或隐藏Fixed KCC作为fallback来伪造支持。

#### Scenario: DotRecast组合需要重力的Corin Program

- **WHEN** Composition发现Corin Program要求AirborneVerticalMotion
- **AND** DotRecast descriptor未声明该capability
- **THEN** Composition MUST明确拒绝并报告缺失能力
- **AND** MUST不创建DotRecast runtime或发布部分Session资源

