# local-rollback-synctest-foundation Specification

## Purpose
定义本地回滚 synctest 的输入历史、快照历史、恢复接口、比较结果和 debug runner 地基。
## Requirements
### Requirement: 本地 Tick 输入历史
系统 MUST 提供本地 tick 输入历史，用 `SimulationTick` 保存每个 tick 的 Move、Look、Run 和离散按钮事实。输入历史 MUST 保存纯数据，MUST NOT 保存 InputAction、MonoBehaviour、场景对象或动作结果。

#### Scenario: 写入 tick 输入
- **WHEN** tick N 的输入被采集
- **THEN** 系统 MUST 将 Move、Look、Run 和离散按钮事实写入 tick N 的输入历史
- **AND** 输入帧 MUST 可按 tick N 查询

#### Scenario: 读取重放区间
- **WHEN** 本地 synctest 从 tick A 重放到 tick B
- **THEN** 输入历史 MUST 能按 tick 顺序返回 A..B 的输入帧
- **AND** 任一 tick 输入缺失时 MUST 返回失败并输出诊断

#### Scenario: 输入历史不保存动作结果
- **WHEN** Dodge 输入帧被保存
- **THEN** 输入历史 MUST 只保存按钮事实
- **AND** MUST NOT 保存该输入已经进入 Dodge 状态的结果

### Requirement: 本地角色模拟快照 v0
系统 MUST 提供本地角色模拟快照 v0，用纯数据表达恢复和比较本地动作模拟所需的最小事实。快照 MUST 以 `SimulationTick` 标记，并 MUST NOT 保存 Transform、GameObject、CharacterController、Animator、AnimationClip、Animancer state 或 InputAction。

#### Scenario: 快照包含最小模拟事实
- **WHEN** tick N 的快照被创建
- **THEN** 快照 MUST 包含 tick N
- **AND** MUST 包含真实模拟根 position 和 yaw
- **AND** MUST 包含统一状态机 active state、state time、variant 和 pending transition
- **AND** MUST 包含 Run latch、last moving gait、current world direction 和 locomotion phase/gait

#### Scenario: 快照保持纯数据
- **WHEN** 检查快照模型
- **THEN** 快照 MUST NOT 保存 Unity Object 字段
- **AND** MUST NOT 保存 Animancer runtime 对象

#### Scenario: 非法数值安全处理
- **WHEN** 快照输入包含 NaN、Infinity 或负 tick
- **THEN** 系统 MUST 拒绝该快照或修正为明确安全值
- **AND** MUST 以测试覆盖该行为

### Requirement: 快照历史
系统 MUST 提供本地快照历史，用 ring buffer 或等价有界结构按 tick 保存角色模拟快照，并支持查询、覆盖、裁剪和缺失诊断。

#### Scenario: 保存并查询快照
- **WHEN** tick N 的角色模拟快照写入历史
- **THEN** 系统 MUST 能按 tick N 查询该快照

#### Scenario: 裁剪旧快照
- **WHEN** 快照历史超过容量或收到确认裁剪请求
- **THEN** 系统 MUST 裁剪旧 tick 快照
- **AND** MUST 保留容量范围内的新 tick 快照

#### Scenario: 恢复点缺失
- **WHEN** 本地 synctest 请求一个不存在或已裁剪的恢复 tick
- **THEN** 系统 MUST 返回失败
- **AND** MUST 输出包含请求 tick 的诊断

### Requirement: 快照采集接入
系统 MUST 能在 `WriteSnapshotAndEvents` phase 采集本地角色模拟快照，并写入快照历史。采集 adapter MUST 只读取现有主线结果，不得改变真实模拟根、表现根、状态机或动画播放。

#### Scenario: 在快照 phase 记录
- **WHEN** tick N 执行 `WriteSnapshotAndEvents`
- **THEN** 快照采集 adapter MUST 读取当前角色模拟事实
- **AND** MUST 写入 tick N 的快照历史

#### Scenario: 采集不改变运行时
- **WHEN** 快照采集 adapter 运行
- **THEN** 它 MUST NOT 调用 `CharacterController.Move`
- **AND** MUST NOT 写真实模拟根 Transform
- **AND** MUST NOT 写表现根 Transform
- **AND** MUST NOT 播放 Animancer 动画

### Requirement: 本地状态恢复边界
系统 MUST 定义从角色模拟快照恢复本地模拟状态的边界，使统一状态机、Locomotion 运行时事实和真实模拟根可以回到旧 tick。恢复 MUST 通过当前主线的 adapter 或接口完成，不得创建第二套状态机或第二套 movement controller。

#### Scenario: 恢复统一状态机事实
- **WHEN** 系统从 tick N 快照恢复
- **THEN** 统一状态机 MUST 恢复 active state、state time、variant 和 pending transition
- **AND** MUST 恢复会影响下一 tick 输出的内部事实或等价事实

#### Scenario: 恢复 Locomotion 事实
- **WHEN** 系统从 tick N 快照恢复
- **THEN** Locomotion runtime MUST 恢复 Run latch、last moving gait 和 current world direction
- **AND** 真实模拟根 MUST 恢复到快照中的 position 和 yaw

#### Scenario: 不创建第二路径
- **WHEN** 本地状态恢复执行
- **THEN** 系统 MUST NOT 新增绕过 `PlayerLocomotionController` 的 movement controller
- **AND** MUST NOT 通过旧 FullBody/HFSM/Dodge 缝合路径恢复状态权威

### Requirement: 本地 Synctest 重放
系统 MUST 提供本地 synctest 重放能力，用同一段输入验证模拟可恢复和可重放。synctest MUST 支持正常运行保存历史、从旧 tick 恢复、重放输入区间，并比较最终快照。

#### Scenario: 同输入重放成功
- **GIVEN** 输入历史包含 tick A..B 的输入帧
- **AND** 快照历史包含 tick A 的恢复快照
- **WHEN** synctest 从 tick A 恢复并重放到 tick B
- **THEN** 重放后的 tick B 快照 MUST 与原 tick B 快照在定义容差内一致

#### Scenario: 输入缺失时停止
- **GIVEN** 输入历史缺少 tick K
- **WHEN** synctest 尝试重放包含 tick K 的区间
- **THEN** synctest MUST 停止
- **AND** MUST 输出缺失输入诊断

#### Scenario: 快照缺失时停止
- **GIVEN** 快照历史缺少恢复 tick A
- **WHEN** synctest 尝试从 tick A 恢复
- **THEN** synctest MUST 停止
- **AND** MUST 输出缺失快照诊断

### Requirement: 主线边界保持
系统 MUST 保持现有 tick、统一状态机、Locomotion pipeline、motion executor 和表现层边界。synctest core MAY 编排保存、恢复和重放，但 MUST NOT 直接拥有输入读取、角色位移、动画播放或网络协议职责。

#### Scenario: 重放继续走现有主线
- **WHEN** synctest 重放 tick
- **THEN** 重放 MUST 继续通过现有 simulation tick runner、统一状态机和 `PlayerLocomotionController` 主线执行
- **AND** MUST NOT 直接调用 `BasicLocomotionPipeline`
- **AND** MUST NOT 直接调用 `CharacterController.Move`

#### Scenario: core 不依赖表现层
- **WHEN** 检查 synctest core 代码
- **THEN** core MUST NOT 引用 Animancer、Cinemachine、Input System adapter 或 `CharacterController`

#### Scenario: 不进入网络同步
- **WHEN** 实施本地 synctest 地基
- **THEN** 系统 MUST NOT 修改 Fantasy proto
- **AND** MUST NOT 新增真实网络发送接收流程

### Requirement: 可测试和可手动验证
系统 MUST 提供自动测试、静态边界验证和手动验证，证明本地 synctest 地基可保存输入、保存快照、恢复、重放和比较，同时不改变当前本地动作 demo 行为。

#### Scenario: 自动测试覆盖核心
- **WHEN** 运行定向 EditMode 测试
- **THEN** 测试 MUST 覆盖输入帧、输入历史、角色快照、快照历史、快照比较、状态恢复和 synctest 重放

#### Scenario: 静态验证无分裂路径
- **WHEN** 运行静态边界测试
- **THEN** 测试 MUST 证明 synctest core 不引用表现和 Unity 运行时控制对象
- **AND** MUST 证明没有新增第二套 movement controller

#### Scenario: 手动验证本地行为
- **WHEN** 用户在演示场景未启用 synctest 时操作角色
- **THEN** WASD、Look、Run、Dodge、Idle、MoveStart、MoveLoop、MoveStop 行为 MUST 不回退
- **AND** 用户启用仅记录输入和快照模式后行为仍 MUST 不回退

#### Scenario: Play Mode 触发本地 synctest
- **GIVEN** Play Mode 中输入历史和快照历史已有足够 tick
- **WHEN** 用户触发本地 synctest debug runner
- **THEN** 系统 MUST 从历史 tick 快照恢复
- **AND** MUST 用已记录输入重放到当前可恢复 tick
- **AND** MUST 在 Console 输出 PASS 或包含 reason/differences 的失败诊断
