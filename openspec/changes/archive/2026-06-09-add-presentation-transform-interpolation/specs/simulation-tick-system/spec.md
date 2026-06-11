## ADDED Requirements
### Requirement: 表现层插值读数
系统 MUST 为渲染帧表现层提供只读 simulation tick 插值读数，使角色可见表现、相机、动画、VFX 或 UI 等表现系统可以基于当前 tick 余量计算 0..1 的插值 alpha，同时不得改变 tick 权威推进语义。

#### Scenario: 不足一个 tick 时输出 alpha
- **WHEN** 客户端 tick accumulator 累积了小于一个 fixed delta 的剩余时间
- **THEN** 表现层 MUST 能读取到 0..1 范围内的 interpolation alpha
- **AND** alpha MUST 表达剩余时间相对 fixed delta 的比例

#### Scenario: tick 推进后保留余量语义
- **WHEN** 单个渲染帧产生一个或多个 simulation tick
- **THEN** tick accumulator MUST 保留追帧后的剩余时间
- **AND** 表现层读取到的 alpha MUST 基于该剩余时间计算

#### Scenario: 只读边界
- **WHEN** 角色可见表现、相机、动画、VFX 或 UI 表现层读取 interpolation alpha
- **THEN** 它们 MUST NOT 修改 accumulator 内部状态
- **AND** 它们 MUST NOT 改变 `SimulationTick` 单调推进结果

#### Scenario: core 不依赖表现系统
- **WHEN** 检查 simulation core 代码
- **THEN** simulation core MUST NOT 引用 Cinemachine、相机 runtime、Animancer、VFX、UI 或场景 Transform 类型
- **AND** 表现层适配 MUST 位于 runtime adapter 边界
