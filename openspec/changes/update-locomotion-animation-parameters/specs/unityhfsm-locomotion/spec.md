## MODIFIED Requirements

### Requirement: UnityHFSM 基础 Locomotion 阶段机
系统 MUST 使用项目已安装的 UnityHFSM 管理基础 Locomotion 的 `Idle / MoveStart / MoveLoop / MoveStop` 阶段，并 MUST 保留当前基础移动阶段语义；其中 `MoveStop` 无输入回到 `Idle` 的等待时长 MUST 能由当前停止动画配置解析结果驱动。

#### Scenario: 初始化进入 Idle
- **WHEN** 基础 Locomotion 阶段机初始化
- **THEN** 当前阶段 MUST 为 `Idle`
- **AND** 阶段计时 MUST 为 0

#### Scenario: 有移动意图进入 MoveStart
- **GIVEN** 当前阶段为 `Idle`
- **WHEN** 本帧存在移动意图
- **THEN** 阶段机 MUST 切换到 `MoveStart`
- **AND** 阶段计时 MUST 从切换后重新开始

#### Scenario: 起步达到最小时长进入 MoveLoop
- **GIVEN** 当前阶段为 `MoveStart`
- **AND** 本帧持续存在移动意图
- **WHEN** `MoveStart` 阶段计时达到 `MoveStartMinTime`
- **THEN** 阶段机 MUST 切换到 `MoveLoop`

#### Scenario: 起步中断进入 MoveStop
- **GIVEN** 当前阶段为 `MoveStart`
- **WHEN** 本帧没有移动意图
- **THEN** 阶段机 MUST 切换到 `MoveStop`

#### Scenario: 循环移动停止进入 MoveStop
- **GIVEN** 当前阶段为 `MoveLoop`
- **WHEN** 本帧没有移动意图
- **THEN** 阶段机 MUST 切换到 `MoveStop`

#### Scenario: 停止完成回到 Idle
- **GIVEN** 当前阶段为 `MoveStop`
- **AND** 本帧没有移动意图
- **AND** 当前停止动画退出时长为 `StopExitDuration`
- **WHEN** `MoveStop` 阶段计时达到 `StopExitDuration`
- **THEN** 阶段机 MUST 切换到 `Idle`

#### Scenario: 停止未完成保持 MoveStop
- **GIVEN** 当前阶段为 `MoveStop`
- **AND** 本帧没有移动意图
- **AND** 当前停止动画退出时长为 `StopExitDuration`
- **WHEN** `MoveStop` 阶段计时小于 `StopExitDuration`
- **THEN** 阶段机 MUST 保持 `MoveStop`

#### Scenario: 停止期间重新移动
- **GIVEN** 当前阶段为 `MoveStop`
- **WHEN** 本帧重新存在移动意图
- **THEN** 阶段机 MUST 立即切换到 `MoveStart`
- **AND** 该切换 MUST NOT 等待当前停止动画退出时长结束

### Requirement: Locomotion Pipeline 接入 UnityHFSM
系统 MUST 将基础 Locomotion pipeline 的阶段来源切换为 UnityHFSM 适配器，同时 MUST 保持输入、相机相对方向、运动命令、运动执行、动画时长解析、动画表现和相机 Resolve 的既有顺序。

#### Scenario: Pipeline 顺序保持
- **WHEN** 基础 Locomotion pipeline 处理一帧输入
- **THEN** 系统 MUST 先生成输入快照
- **AND** MUST 再生成移动意图
- **AND** MUST 再解析相机相对世界方向
- **AND** MUST 再解析当前阶段机需要的纯数据时长事实
- **AND** MUST 再推进 UnityHFSM Locomotion 阶段
- **AND** MUST 再构建 `MovementCommand`
- **AND** MUST 再提交给运动执行端口
- **AND** MUST 再提交 `MovementAnimationContext`
- **AND** MUST 最后完成相机 Resolve

#### Scenario: 运动命令继续使用 BasicMovementPhase
- **WHEN** UnityHFSM Locomotion 阶段机输出当前阶段
- **THEN** `MovementCommand` MUST 继续携带 `BasicMovementPhase`
- **AND** `MovementAnimationContext` MUST 继续携带 `BasicMovementPhase`

#### Scenario: Pipeline 不依赖具体运动实现
- **WHEN** 基础 Locomotion pipeline 执行移动
- **THEN** 系统 MUST 通过运动执行端口提交 `MovementCommand`
- **AND** pipeline MUST NOT 持有 `CharacterMotionDriver` 具体类型
- **AND** pipeline MUST NOT 调用 `CharacterController.Move`
- **AND** pipeline MUST NOT 调用 KCC API

#### Scenario: 状态机不依赖动画 runtime
- **WHEN** 基础 Locomotion 阶段机使用当前停止动画退出时长
- **THEN** 阶段机 MUST 只读取调用方传入的纯数据时长
- **AND** MUST NOT 引用 Animancer
- **AND** MUST NOT 读取 Animancer 当前播放状态
- **AND** MUST NOT 判断具体动画 key 是否为 `RunEnd` 或 `WalkEnd`
