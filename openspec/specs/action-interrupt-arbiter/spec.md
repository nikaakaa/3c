# action-interrupt-arbiter Specification

## Purpose
TBD - created by archiving change add-action-interrupt-arbiter. Update Purpose after archive.
## Requirements
### Requirement: 纯数据动作打断输入
系统 MUST 提供纯数据动作打断请求、当前状态上下文和裁决结果模型，用于在逻辑层表达“当前状态能否被某个动作请求打断”。这些模型 MUST NOT 依赖 Unity 场景对象、Animancer 运行时对象、AnimationClip、Animator、CharacterController、Input System 或 BBB 运行时类型。

#### Scenario: 请求不携带 Unity 对象
- **WHEN** 系统构建一个动作打断请求
- **THEN** 请求 MUST 使用稳定状态 ID、请求类型、优先级、来源顺序或 tick、过期信息表达意图
- **AND** 请求 MUST NOT 保存 `AnimationClip`
- **AND** 请求 MUST NOT 保存 `UnityEngine.Object`
- **AND** 请求 MUST NOT 保存 Animancer 类型

#### Scenario: 上下文只保存逻辑事实
- **WHEN** 仲裁器读取当前状态上下文
- **THEN** 上下文 MUST 包含当前状态 ID、当前状态已持续时间和当前状态抗性
- **AND** 上下文 MAY 包含当前 simulation tick
- **AND** 上下文 MUST NOT 持有 MonoBehaviour、Transform、Animator 或 Animancer 引用

### Requirement: 打断策略规则
系统 MUST 使用显式策略描述从当前状态到目标状态的打断许可、最小优先级、时间规则和强制打断语义。没有匹配策略时，仲裁器 MUST 拒绝请求。

#### Scenario: 无策略时拒绝
- **GIVEN** 当前状态存在一个动作打断请求
- **AND** 策略集合中没有匹配当前状态和目标状态的策略
- **WHEN** 仲裁器执行裁决
- **THEN** 裁决 MUST 为 rejected
- **AND** 拒绝原因 MUST 表示没有匹配策略

#### Scenario: 优先级不足时拒绝
- **GIVEN** 请求匹配到一个策略
- **AND** 请求优先级低于策略要求的最小优先级
- **WHEN** 仲裁器执行裁决
- **THEN** 裁决 MUST 为 rejected
- **AND** 拒绝原因 MUST 表示优先级不足

#### Scenario: 当前状态抗性阻挡请求
- **GIVEN** 请求匹配到一个非强制策略
- **AND** 请求优先级小于或等于当前状态抗性
- **WHEN** 仲裁器执行裁决
- **THEN** 裁决 MUST 为 rejected
- **AND** 拒绝原因 MUST 表示被当前状态抗性阻挡

#### Scenario: 强制策略绕过抗性
- **GIVEN** 请求匹配到一个显式强制策略
- **AND** 请求满足策略最小优先级和时间规则
- **WHEN** 请求优先级小于或等于当前状态抗性
- **THEN** 仲裁器 MAY 接受该请求

### Requirement: 时间规则
系统 MUST 支持基础时间规则 `Always`、`AfterElapsedTime` 和 `DuringElapsedTimeWindow`。第一版时间判断 MUST 基于当前逻辑状态 elapsed time，不得直接读取 Animancer 当前播放进度或 clip length。

#### Scenario: Always 立即允许
- **GIVEN** 请求匹配到 `Always` 策略
- **AND** 请求满足优先级和抗性规则
- **WHEN** 仲裁器执行裁决
- **THEN** 仲裁器 MUST 接受该请求

#### Scenario: AfterElapsedTime 到时允许
- **GIVEN** 请求匹配到 `AfterElapsedTime` 策略
- **AND** 当前状态 elapsed time 小于策略要求时间
- **WHEN** 仲裁器执行裁决
- **THEN** 仲裁器 MUST 拒绝该请求
- **WHEN** 当前状态 elapsed time 大于或等于策略要求时间
- **THEN** 仲裁器 MUST 接受该请求

#### Scenario: DuringElapsedTimeWindow 只在窗口内允许
- **GIVEN** 请求匹配到 `DuringElapsedTimeWindow` 策略
- **WHEN** 当前状态 elapsed time 早于窗口开始
- **THEN** 仲裁器 MUST 拒绝该请求
- **WHEN** 当前状态 elapsed time 位于窗口开始和窗口结束之间
- **THEN** 仲裁器 MUST 接受该请求
- **WHEN** 当前状态 elapsed time 晚于窗口结束
- **THEN** 仲裁器 MUST 拒绝该请求

### Requirement: 确定性仲裁结果
系统 MUST 在同一帧多个候选请求中输出确定性的单一裁决。仲裁结果 MUST 说明是否接受、选择的请求、目标状态和拒绝原因。

#### Scenario: 选择最高优先级请求
- **GIVEN** 同一帧存在多个可接受请求
- **WHEN** 仲裁器执行裁决
- **THEN** 仲裁器 MUST 选择优先级最高的请求
- **AND** 裁决 MUST 包含该请求的目标状态

#### Scenario: 同优先级稳定选择
- **GIVEN** 同一帧存在多个可接受请求
- **AND** 它们拥有相同优先级
- **WHEN** 仲裁器执行裁决
- **THEN** 仲裁器 MUST 按来源顺序、提交顺序或等价稳定规则选择一个请求
- **AND** 多次使用相同输入执行裁决 MUST 得到相同结果

#### Scenario: 过期请求不参与裁决
- **GIVEN** 请求已超过自身过期 tick 或过期时间
- **WHEN** 仲裁器执行裁决
- **THEN** 该请求 MUST 不得成为 accepted decision 的 selected request

### Requirement: 与现有 Locomotion 边界
系统 MUST 保持当前统一状态机对 `FullBody/Locomotion/Idle|MoveStart|MoveLoop|MoveStop` 的流转职责。动作打断仲裁模块 MAY 作为纯数据策略 helper 保留，但 MUST NOT 接管当前 `MoveStop -> MoveStart` 或 `MoveStop -> Idle` 路径。

#### Scenario: MoveStop 重新输入仍由状态图处理
- **GIVEN** 当前基础移动阶段为 `MoveStop`
- **WHEN** 本帧重新出现移动输入
- **THEN** `MoveStop -> MoveStart` MUST 继续由统一角色逻辑状态机 transition 处理
- **AND** 本仲裁模块 MUST NOT 成为该流转的必需依赖

#### Scenario: Presenter 不依赖仲裁器
- **WHEN** 基础移动动画 Presenter 根据 `MovementAnimationContext` 播放 alias
- **THEN** Presenter MUST NOT 调用动作打断仲裁器
- **AND** Presenter MUST NOT 决定业务打断是否允许

### Requirement: 模块边界和 BBB 参考边界
系统 MAY 参考 BBB 的 priority、resistance、interceptor 和 override 思路，但 MUST NOT 复制 BBB 运行时代码或依赖 BBB 运行时路径。动作打断仲裁模块 MUST 保持纯逻辑边界，供未来状态机、输入缓冲、tick 和编辑器消费。

#### Scenario: 不依赖 BBB 运行时
- **WHEN** 动作打断仲裁模块实现完成
- **THEN** 新增运行时代码 MUST NOT 引用 `BBBNexus` 命名空间
- **AND** MUST NOT 依赖 `Ref/BBB-Nexus` 下的运行时类型、Prefab 或 ScriptableObject

#### Scenario: 不直接切状态
- **WHEN** 仲裁器接受一个请求
- **THEN** 仲裁器 MUST 只返回裁决结果
- **AND** MUST NOT 持有或调用状态机实例
- **AND** MUST NOT 直接调用 `ChangeState`

#### Scenario: 不直接播放动画
- **WHEN** 仲裁器接受一个请求
- **THEN** 仲裁器 MUST NOT 调用 Animancer 或 Animator 播放 API
- **AND** MUST NOT 写入动画层权重、root motion 或 Transform

### Requirement: 校验和测试
系统 MUST 提供策略校验、自动测试和静态边界验证，证明仲裁规则可诊断、确定且不会污染现有动画与移动边界。

#### Scenario: 策略校验报告非法窗口
- **GIVEN** 一个 `DuringElapsedTimeWindow` 策略的结束时间早于开始时间
- **WHEN** 运行策略校验
- **THEN** 校验结果 MUST 报告错误

#### Scenario: 自动测试覆盖核心规则
- **WHEN** 运行动作打断仲裁 EditMode 测试
- **THEN** 测试 MUST 覆盖无请求、无策略、过期、优先级不足、抗性阻挡、强制打断、三种时间规则、多请求最高优先级和同优先级稳定选择

#### Scenario: 静态验证纯逻辑边界
- **WHEN** 检查动作打断仲裁模块源码
- **THEN** 静态搜索 MUST 能确认该模块不引用 Animancer、AnimationClip、Animator、CharacterController、Cinemachine、Input System 或 `BBBNexus`
