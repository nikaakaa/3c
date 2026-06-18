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
- **AND** MUST 包含状态图 runtime active state、state time、variant 和 pending transition
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
系统 MUST 定义从角色模拟快照恢复本地模拟状态的边界，使状态图 runtime、Locomotion 运行时事实和真实模拟根可以回到旧 tick。恢复 MUST 通过当前主线的 adapter 或接口完成，不得创建第二套状态机或第二套 movement controller。

#### Scenario: 恢复状态图 runtime 事实
- **WHEN** 系统从 tick N 快照恢复
- **THEN** 状态图 runtime MUST 恢复 active state、state time、variant 和 pending transition
- **AND** MUST 恢复会影响下一 tick 输出的内部事实或等价事实

#### Scenario: 恢复 Locomotion 事实
- **WHEN** 系统从 tick N 快照恢复
- **THEN** Locomotion runtime MUST 恢复 Run latch、last moving gait 和 current world direction
- **AND** 真实模拟根 MUST 恢复到快照中的 position 和 yaw

#### Scenario: 不创建第二路径
- **WHEN** 本地状态恢复执行
- **THEN** 系统 MUST NOT 新增绕过 `CharacterRuntimeCore`、`CharacterFramePipeline` 或 `LocomotionRuntimeModule` 的 movement controller
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
系统 MUST 保持现有 tick、状态图 runtime、Locomotion pipeline、motion executor 和表现层边界。synctest core MAY 编排保存、恢复和重放，但 MUST NOT 直接拥有输入读取、角色位移、动画播放或网络协议职责。

#### Scenario: 重放继续走现有主线
- **WHEN** synctest 重放 tick
- **THEN** 重放 MUST 继续通过现有 simulation tick runner、状态图 runtime、`CharacterRuntimeCore` 和 `CharacterFramePipeline` 主线执行
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

### Requirement: 严格逐 Tick 一致性
系统 MUST 提供用于预测回滚验收的严格 synctest 语义。严格语义下，最终快照比较必须一致，且首个 restore/replay mismatch MUST 不存在；如果 `FirstMismatch.HasMismatch` 为 true，则本次 synctest MUST 失败，即使 end tick 快照最终重新收敛。

#### Scenario: 中间分叉但最终收敛
- **GIVEN** replay 从 tick A 恢复并重放到 tick B
- **AND** tick K 的重放快照与历史快照不一致
- **AND** tick B 的最终快照又与历史快照一致
- **WHEN** 严格 synctest 计算结果
- **THEN** 结果 MUST 失败
- **AND** first mismatch MUST 指向 tick K
- **AND** final comparison MUST 仍保留为匹配，供诊断说明“最终收敛但中间分叉”

#### Scenario: Restore 阶段分叉
- **GIVEN** synctest 从 tick A 快照恢复
- **WHEN** 恢复后立即 capture 的快照与 tick A 历史快照不一致
- **THEN** 结果 MUST 失败
- **AND** first mismatch stage MUST 为 `Restore`
- **AND** replay 阶段 MAY 继续执行以收集最终 comparison，但不得覆盖首个 mismatch

#### Scenario: 无中间分叉且最终一致
- **GIVEN** replay 每个可比较 tick 都与历史快照一致
- **AND** end tick 最终快照一致
- **WHEN** 严格 synctest 计算结果
- **THEN** 结果 MUST 通过
- **AND** first mismatch MUST 为空

### Requirement: First mismatch 字段级诊断
系统 MUST 为 synctest 的首个分叉输出结构化诊断，至少包含 stage、tick、restore tick、end tick、输入帧摘要、expected 快照摘要、actual 快照摘要和字段级 differences。诊断 MUST 能区分 restore mismatch、replay mismatch、缺失输入和缺失快照。

#### Scenario: Replay 分叉输出输入帧
- **GIVEN** first mismatch stage 为 `Replay`
- **WHEN** debug runner 输出失败日志
- **THEN** 日志 MUST 包含 mismatch tick
- **AND** MUST 包含该 tick 的 `PredictionInputFrame` 摘要
- **AND** MUST 包含 differences 字段列表

#### Scenario: Restore 分叉不伪造输入帧
- **GIVEN** first mismatch stage 为 `Restore`
- **WHEN** debug runner 输出失败日志
- **THEN** 日志 MUST 标记该 mismatch 没有关联输入帧
- **AND** MUST 包含 restore tick 的 expected/actual 摘要

#### Scenario: 缺失数据诊断
- **GIVEN** synctest 缺少恢复快照或输入帧
- **WHEN** runner 返回失败
- **THEN** failure reason MUST 包含缺失的 tick
- **AND** MUST NOT 把缺失数据伪装成 snapshot mismatch

### Requirement: Soak 严格窗口验收
系统 MUST 让本地 rollback soak 使用严格 synctest 语义。任一窗口出现 first mismatch 时，soak 结果 MUST 失败，并 MUST 保留首个失败窗口的 seed、restore tick、end tick、stage、mismatch tick 和 differences。

#### Scenario: 首个窗口分叉时停止
- **GIVEN** soak 配置 `stopOnFailure=true`
- **AND** 第一个失败窗口存在 first mismatch
- **WHEN** soak runner 执行
- **THEN** runner MUST 停止后续窗口
- **AND** result success MUST 为 false
- **AND** first failure MUST 指向该窗口

#### Scenario: 继续模式保留首个分叉
- **GIVEN** soak 配置 `stopOnFailure=false`
- **AND** 多个窗口存在 mismatch
- **WHEN** soak runner 执行完全部窗口
- **THEN** result success MUST 为 false
- **AND** first failure MUST 保留最早发现的严格失败窗口

#### Scenario: 所有窗口逐 Tick 一致
- **GIVEN** 所有 soak 窗口没有 first mismatch
- **AND** 所有 end tick 最终快照一致
- **WHEN** soak runner 执行完成
- **THEN** result success MUST 为 true

### Requirement: 严格工具不新增推进路径
系统 MUST 通过现有 `ILocalRollbackSynctestSimulation`、Character frame 主线、Locomotion 主线和 motion executor 边界执行严格验证。严格模式不得直接调用 `BasicLocomotionPipeline`、`CharacterController.Move`、Animancer runtime 或 Input System adapter。

#### Scenario: 严格模式复用现有接口
- **WHEN** synctest、soak 或 debug runner 执行 restore、advance、capture
- **THEN** 它们 MUST 通过 `ILocalRollbackSynctestSimulation` 或既有 adapter 边界执行
- **AND** MUST NOT 新增第二套角色推进路径

#### Scenario: 静态边界验证
- **WHEN** 运行 rollback core 静态边界测试
- **THEN** 测试 MUST 证明 core 不引用表现层和 Unity 运行时控制对象
- **AND** 失败信息 MUST 指出违规文件和违规类型

### Requirement: 本地回滚分层 Contract
系统 MUST 将本地回滚相关代码按 Rollback Core、Simulation Adapter、Gameplay Runtime、Simulation State、Presentation Local-Only 和 Debug Tooling 分层维护。Rollback Core MUST 只依赖纯数据和算法；Simulation Adapter MUST 负责把 core 接到现有角色主线；Presentation Local-Only MUST 不进入 gameplay rollback snapshot。

#### Scenario: Core 不拥有 Unity 表现对象
- **WHEN** 检查 Rollback Core 模块
- **THEN** 它 MUST NOT 引用 Cinemachine、Animancer runtime、Input System adapter、`CharacterController`、`Transform` 写入逻辑或 presentation interpolator
- **AND** 它 MUST 通过纯数据输入、快照和比较结果表达行为

#### Scenario: Adapter 接入现有主线
- **WHEN** 本地 replay 需要推进角色
- **THEN** Simulation Adapter MUST 调用现有 Character frame 或 Locomotion 主线入口
- **AND** 它 MUST NOT 新增第二套 movement controller、第二套状态机或直接移动真实根的旁路

#### Scenario: Debug Tooling 不成为 gameplay 状态
- **WHEN** F6/F8 工具为了保护现场捕获 presentation、visual 或 camera probe 数据
- **THEN** 这些数据 MUST 只属于 Debug Tooling
- **AND** 它们 MUST NOT 写入 `CharacterSimulationSnapshot`
- **AND** 它们 MUST NOT 作为后续网络同步或 gameplay rollback 状态传播

### Requirement: Debug Runner 职责拆分
系统 MUST 将本地 synctest debug runner 的触发编排、presentation restore、timing probe 和日志格式化拆成可独立测试的 Module。F6/F8 默认 hidden 模式 MUST 在结束时恢复触发前现场，并以固定日志标记输出结果。

#### Scenario: Synctest runner 只编排测试
- **WHEN** 用户触发 F6 synctest
- **THEN** debug runner MUST 负责选择 restore/end tick、调用 synctest core 并恢复现场
- **AND** presentation restore、timing probe 和日志格式化 SHOULD 由独立 Module 承担

#### Scenario: Hidden replay 恢复现场
- **GIVEN** debug runner 未启用 apply replay result
- **WHEN** hidden replay 完成或失败
- **THEN** 系统 MUST 恢复触发前最新 live simulation snapshot
- **AND** MUST 恢复 Debug Tooling 捕获的 presentation 现场
- **AND** MUST NOT 将 replay 过程中间态永久留在 source、visual 或 camera target 上

#### Scenario: 固定日志标记
- **WHEN** F6/F8 输出诊断
- **THEN** 日志 MUST 保留可搜索标记 `[rollback-synctest]`、`ROLLBACK_TIMING_PROBE`、`ROLLBACK_SOAK_RESULT` 或 `ROLLBACK_SOAK_FIRST_MISMATCH`
- **AND** timing 或长跑相关日志 MUST 带固定标记，便于过滤刷屏日志

### Requirement: Scoped 快照比较结果
本地 synctest snapshot comparison MUST 支持 scoped comparison result。结果 MUST 至少包含 strict gameplay differences 和 presentation differences 两组字段；`Matches` 或等价 success 判定 MUST 只由 strict gameplay differences 决定。

#### Scenario: 只有表现漂移时通过
- **GIVEN** replay 后没有 strict gameplay differences
- **AND** 存在 animation normalized time presentation drift
- **WHEN** synctest runner 生成结果
- **THEN** result MUST 为成功
- **AND** result MUST 保留 presentation differences

#### Scenario: Strict 差异时失败
- **GIVEN** replay 后存在 position、yaw、状态机或 motion executor strict 差异
- **WHEN** synctest runner 生成结果
- **THEN** result MUST 为失败
- **AND** failure reason MUST 不被 presentation drift 覆盖

#### Scenario: First drift 不覆盖 first mismatch
- **GIVEN** replay 区间内先出现 presentation drift，后出现 strict mismatch
- **WHEN** runner 记录 first difference
- **THEN** strict mismatch MUST 作为失败依据
- **AND** presentation drift MAY 作为辅助诊断保留

### Requirement: Scoped F6/F8 诊断日志
本地 F6/F8 诊断日志 MUST 明确输出 strict differences 与 presentation differences。只有 presentation drift 时，日志 MAY 输出 PASS 但 MUST 附带 drift 字段；strict mismatch 时日志 MUST 输出 FAIL 和 strict differences。

#### Scenario: F6 输出 presentation drift
- **GIVEN** F6 replay 只有视觉动画 drift
- **WHEN** debug runner 输出结果
- **THEN** Console MUST 包含 `presentationDifferences` 或等价字段
- **AND** MUST NOT 输出 strict failure

#### Scenario: F6 输出 strict failure
- **GIVEN** F6 replay 存在 gameplay mismatch
- **WHEN** debug runner 输出结果
- **THEN** Console MUST 包含 strict differences
- **AND** MUST 标记 `[rollback-synctest] FAIL`

#### Scenario: F8 汇总 drift
- **GIVEN** F8 soak 的某些窗口只有 presentation drift
- **WHEN** soak 输出结果
- **THEN** 输出 MUST 能诊断 drift 窗口
- **AND** MUST NOT 将其计入 strict failure

### Requirement: Scope Resolver 可测试
本地 synctest 的字段分类 MUST 通过可测试的 resolver、policy 或等价纯数据表完成。Resolver MUST 能解释当前字段属于 strict gameplay、presentation drift、predictive gameplay 或 ignored，并且 MUST 支持后续状态/动画配置扩展。

#### Scenario: TurnBack 字段归类 strict
- **WHEN** resolver 收到 TurnBack profile-driven playback progress 字段
- **THEN** resolver MUST 返回 strict gameplay scope

#### Scenario: MoveLoop 字段归类 presentation
- **WHEN** resolver 收到 MoveLoop 视觉 playback normalized time 字段
- **THEN** resolver MUST 返回 presentation drift scope

#### Scenario: Resolver 不依赖表现层对象
- **WHEN** 检查 resolver 模型
- **THEN** resolver MUST NOT 保存 Animancer state、Animator、AnimationClip、TransitionAsset 或 Unity 场景对象引用

### Requirement: Rollback Debug Rig 装配边界
系统 MUST 将本地 rollback / synctest / soak 的 Debug Tooling 装配在独立 `RollbackDebugRig` prefab 上。正式角色 prefab 和正式场景角色实例 MUST NOT 常驻挂载本地 rollback debug runner、history recorder、prediction input source 或 replay adapter 作为角色运行时能力。Debug Rig prefab 的场景实例 MAY 通过显式引用连接目标角色 runtime，但 MUST NOT 创建第二角色控制器、第二状态机、第二 motion executor、第二 animation presenter 或隐藏 fallback 配置。

#### Scenario: 正式角色不承载 Debug Tooling
- **WHEN** 自动校验 Corin 正式角色 prefab 或正式场景角色实例
- **THEN** 角色对象 MUST NOT 挂载 `LocalRollbackSynctestDebugRunner`
- **AND** MUST NOT 挂载 `LocalLatencyReconciliationDebugRunner`
- **AND** MUST NOT 挂载 `LocalRollbackSoakDebugRunner`
- **AND** MUST NOT 挂载 `PredictionInputHistoryTickRecorder`
- **AND** MUST NOT 挂载 `LocomotionSnapshotHistoryRecorder`
- **AND** MUST NOT 挂载 `CharacterFrameRollbackSimulation`

#### Scenario: Debug Rig 显式连接目标角色
- **GIVEN** 场景中存在独立 `RollbackDebugRig` prefab 实例
- **WHEN** Debug Rig 需要执行 F6/F7/F8 或等价本地 rollback 工具
- **THEN** Debug Rig prefab 实例 MUST 通过显式序列化引用注入目标 `CharacterFrameRuntimeController`、tick driver、prediction input source、history recorder 和 replay adapter
- **AND** 缺失必需引用时 MUST 输出诊断失败
- **AND** MUST NOT 通过隐藏默认配置继续运行

#### Scenario: Debug Tooling 不成为 gameplay 状态
- **WHEN** F6/F7/F8 工具捕获 input history、snapshot history、presentation probe 或 timing probe
- **THEN** 这些数据 MUST 只属于 Debug Tooling
- **AND** MUST NOT 写入角色正式配置根
- **AND** MUST NOT 成为后续 gameplay tick、网络同步或正式 rollback authority 的必需组件

#### Scenario: 自动查找不作为正式绑定
- **WHEN** Debug Rig 或 recorder 的显式引用缺失
- **THEN** 系统 MAY 在编辑期提供自动填充辅助
- **BUT** Play Mode 工具执行时 MUST 以显式引用是否完整作为成功条件
- **AND** MUST NOT 在角色层级中扫描第一个匹配 MonoBehaviour 作为正式 fallback 绑定

### Requirement: 本地回滚 Soak 长跑验证
系统 MUST 提供有限时长的本地 rollback soak 验证能力，用固定 seed 生成输入流并重复执行现有 restore/replay/compare 管线。Soak MUST 复用 `PredictionInputHistory`、`PredictionSnapshotHistory`、`ILocalRollbackSynctestSimulation` 和 `LocalRollbackSynctestRunner`，不得直接调用 `BasicLocomotionPipeline`、`CharacterController.Move` 或新增第二套角色推进路径。

#### Scenario: 固定 seed 可复现
- **GIVEN** soak 配置包含 seed、tickCount 和 rollbackFrames
- **WHEN** 使用同一配置运行两次输入生成
- **THEN** 两次生成的 `PredictionInputFrame` 序列 MUST 完全一致

#### Scenario: 长跑成功输出单条总结
- **GIVEN** soak 在所有窗口中 restore/replay/compare 均通过
- **WHEN** soak 运行结束
- **THEN** Console MUST 输出包含 `ROLLBACK_SOAK_RESULT` 的总结日志
- **AND** 日志 MUST 包含 seed、tickCount、rollbackFrames、checkedWindows 和 result=PASS

#### Scenario: 首个失败低噪声诊断
- **GIVEN** soak 的某个窗口出现 snapshot mismatch
- **WHEN** stopOnFailure 为 true
- **THEN** soak MUST 停止在首个失败窗口
- **AND** Console MUST 输出一条包含 `ROLLBACK_SOAK_FIRST_MISMATCH` 的详情日志
- **AND** 详情 MUST 包含 seed、restore tick、end tick、first mismatch tick 和 differences

#### Scenario: 刷屏时过滤 rollback 关键日志
- **GIVEN** Unity Console 或 Editor.log 存在大量非 rollback 日志
- **WHEN** 开发者需要收集本地 rollback 验证证据
- **THEN** 系统 MUST 提供本地过滤方式，只输出 `ROLLBACK_SOAK_RESULT`、`ROLLBACK_SOAK_FIRST_MISMATCH`、`ROLLBACK_TIMING_PROBE` 或 `[rollback-synctest]` 关键行

#### Scenario: Sandbox 接线可静态验证
- **GIVEN** Unity Editor 当前会话不可用或无法运行 Unity Test Runner
- **WHEN** 开发者需要确认本地 rollback debug 入口没有断线
- **THEN** 系统 MUST 提供本地静态检查方式，确认 Sandbox 中 F6 和 F8 runner 处于 hidden 模式，并引用 Character frame simulation、presentation interpolator 和 camera controller
- **AND** 检查结果 MUST 输出包含 `ROLLBACK_WIRING_CHECK` 的单行结果

#### Scenario: F8 soak 结果可本地断言
- **GIVEN** Play Mode 已触发 F8 soak 并写入 `ROLLBACK_SOAK_RESULT`
- **WHEN** 开发者需要确认最近一次 F8 soak 是否满足 hidden restore 要求
- **THEN** 系统 MUST 提供本地断言方式，检查最近一条结果包含 `result=PASS`、`applyReplay=False`、`sourceRestored=True`、`visualRestored=True`、`cameraRestored=True`、`visualChecked=True` 和 `cameraChecked=True`
- **AND** 断言结果 MUST 输出包含 `ROLLBACK_SOAK_ASSERT` 的单行结果

#### Scenario: F8 soak 可人机协作验收
- **GIVEN** Unity MCP 当前会话不可用且开发者可以手动操作 Unity Editor
- **WHEN** 开发者启动本地 HITL 验收并在 Play Mode 按 F8
- **THEN** 系统 MUST 等待 `ROLLBACK_SOAK_RESULT` 出现并复用本地断言方式检查最近一次结果
- **AND** HITL 验收 MUST 输出包含 `ROLLBACK_SOAK_HITL` 的低噪声结果

#### Scenario: F6 synctest 可人机协作验收
- **GIVEN** Unity MCP 当前会话不可用且开发者可以手动操作 Unity Editor
- **WHEN** 开发者启动本地 HITL 验收并在 Play Mode 按 F6
- **THEN** 系统 MUST 等待 `[rollback-synctest]` 结果出现并检查最近一次结果为 PASS
- **AND** HITL 验收 MUST 输出包含 `ROLLBACK_SYNCTEST_HITL` 的低噪声结果

#### Scenario: 本地回滚 demo 可组合验收
- **GIVEN** Unity MCP 当前会话不可用且开发者可以手动操作 Unity Editor
- **WHEN** 开发者启动组合 HITL 验收并依次在 Play Mode 触发 F6 与 F8
- **THEN** 系统 MUST 先验证 F6 synctest PASS，再验证 F8 soak PASS
- **AND** 组合验收 MUST 输出包含 `ROLLBACK_DEMO_HITL` 的低噪声结果
- **AND** 组合验收 MUST 显式输出人工画面稳定确认状态，不得把日志通过自动当作画面稳定已确认

#### Scenario: HITL 脚本可自检
- **GIVEN** 开发者需要确认本地 HITL 验收脚本自身没有误判旧日志或丢失快速按键日志
- **WHEN** 运行 HITL 脚本自检
- **THEN** 系统 MUST 使用临时日志样本验证 F6+F8 通过、人工视觉确认标记通过和缺失 F8 失败路径
- **AND** 自检 MUST 输出包含 `ROLLBACK_HITL_SCRIPT_CHECK` 的单行结果

#### Scenario: Editor.log 编译错误可低噪声扫描
- **GIVEN** Unity MCP 当前会话不可用或无法读取 Console
- **WHEN** 开发者需要辅助确认最近 Editor.log 是否包含 C# 编译错误
- **THEN** 系统 MUST 提供本地扫描方式，检查最近日志中的 `error CS` 或 Unity 编译失败标记
- **AND** 扫描结果 MUST 输出包含 `UNITY_COMPILE_LOG_CHECK` 的单行结果

#### Scenario: Unity MCP 连接状态可低噪声诊断
- **GIVEN** 本地 Unity Test Runner 无法通过 MCP 启动
- **WHEN** 开发者需要区分 server、Unity 进程和 instance 注册状态
- **THEN** 系统 MUST 提供本地诊断方式，检查 MCP server health、Unity 进程和 `/api/instances`
- **AND** 诊断结果 MUST 输出包含 `UNITY_MCP_CHECK` 的单行结果

#### Scenario: Hidden soak 不污染当前现场
- **GIVEN** soak 未启用应用 replay 结果到场景
- **WHEN** soak 触发后内部执行多次 restore/replay
- **THEN** 结束后真实模拟根 MUST 恢复到触发前现场
- **AND** 已配置的表现插值状态和相机 controller 表现状态 MUST 恢复到触发前状态
