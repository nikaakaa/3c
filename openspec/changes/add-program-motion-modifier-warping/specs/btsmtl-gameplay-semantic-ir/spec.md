## ADDED Requirements

### Requirement: MotionWarp authoring 必须编译为唯一 numeric-neutral operation

Frontend MUST为每个合法MotionWarpClip生成唯一`TimelineMotionWarp` Semantic operation，并保存position/rotation mode、target offset、weight、clamp、两条canonical progress curve、Timeline/Action Context provenance及到源MotionCurve operation的typed reference。IR MUST不保存Unity Transform、GameObject、AnimationCurve对象或Solver类型。

#### Scenario: 编译带 MotionWarp 的动作 Timeline

- **WHEN** Timeline包含合法MotionCurveClip和引用它的MotionWarpClip
- **THEN** Semantic IR MUST包含两个独立operation
- **AND** MotionWarp operation MUST通过typed reference唯一指向MotionCurve operation
- **AND** SourceMap MUST能返回两个authoring clip

### Requirement: MotionWarp 必须成为两个 Numeric Target 的显式 capability

Operation Set MUST声明MotionWarp operation schema、reference、state requirement与canonical modifier顺序。Float32和Fixed Target MUST显式声明支持或在Target编译时拒绝整个Program；系统 MUST不允许某个Network Model在runtime忽略未知Warp operation。

#### Scenario: Target backend 缺少 MotionWarp

- **WHEN** validated Semantic IR包含TimelineMotionWarp
- **AND** 某Numeric Target没有完整实现该operation和state schema
- **THEN** Target编译 MUST失败
- **AND** MUST不生成会在运行时跳过Warp的Program

### Requirement: MotionWarp source 与 Action Context 必须在 Semantic 阶段闭合

Frontend MUST验证MotionWarp source、Timeline owner、窗口、Action channel、Override语义、Action Context call site与ActionProfile target requirement。shared Timeline被多个TimelineNode引用时，每个可执行call site MUST满足同一要求；任一call site缺少Action Context MUST使编译失败。

#### Scenario: Shared Timeline 被普通状态复用

- **WHEN** 一个包含MotionWarp的shared Timeline同时被动作状态和无Action Context状态引用
- **THEN** Frontend MUST拒绝该Program
- **AND** MUST不假定运行时只会走合法call site
