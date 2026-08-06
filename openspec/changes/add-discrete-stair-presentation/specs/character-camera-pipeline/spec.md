## MODIFIED Requirements

### Requirement: 默认相机跟随必须使用统一表现根姿态

系统 MUST让 `CharacterBodyPresentationRuntime` 基于正式 previous/current `CharacterBodySample` 和其显式 Body clock 策略生成唯一 `CharacterBodyPresentationFrame` 与 visible pose。默认 camera anchor MUST作为相对初始 logic body 的绑定偏移保存，并由内部 `CharacterCameraPresentationRuntime` 使用同一最终visible pose生成 follow point。接地竖直不连续修正 MUST先在Body Runtime内形成最终visible Y，再沿同一个Body Frame同时驱动VisualRoot与默认Camera。系统 MUST不让默认相机在表现帧直接读取 logic body、KCC Step diagnostics或camera anchor子节点的离散世界坐标，也 MUST不维护第二份pose插值历史或台阶竖直filter。

#### Scenario: 渲染帧高于 logic tick

- **WHEN** 两个 logic tick 之间执行多个 PresentationFrame
- **THEN** visual root 和默认 camera follow point MUST使用同一个插值 body pose
- **AND** CameraRigAdapter MUST 在每个表现帧收到连续的 follow point
- **AND** 相机 MUST NOT 因 logic anchor 未更新而交替冻结和跳变

#### Scenario: 强制位置校正

- **WHEN** 表现层因正式 motion correction 使用贴合策略
- **THEN** 同一插值 body pose MUST同时驱动 visual root 和默认 camera follow point
- **AND** Presentation runtime MUST NOT使用旧 logic anchor 世界坐标产生不同步的第二次贴合

#### Scenario: 离散台阶产生竖直Body修正

- **WHEN** Body Runtime正在有界收敛接地竖直不连续offset
- **THEN** VisualRoot和默认camera follow point MUST使用同一个最终visible Y
- **AND** Camera Runtime MUST不从logic Body Y建立第二条台阶修正

#### Scenario: 显式相机目标

- **WHEN** 有效 `CameraTargetRequest.AnchorKey` 解析出正式世界点
- **THEN** Presentation camera resolver MUST使用该显式 follow point
- **AND** 系统 MUST NOT 把默认 camera anchor 绑定规则隐式应用到该世界点

#### Scenario: 表现根姿态缺失

- **WHEN** 默认 camera anchor 需要生成 follow point但当前没有有效 body sample
- **THEN** 内部 CharacterCameraPresentationRuntime MUST报告明确错误并停止生成该帧相机计划
- **AND** 系统 MUST NOT 回退读取 logic anchor 世界坐标、visual root Transform 或场景搜索结果
