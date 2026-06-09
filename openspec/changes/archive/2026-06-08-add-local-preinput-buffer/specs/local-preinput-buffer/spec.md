## ADDED Requirements
### Requirement: 离散按钮事实
系统 MUST 使用纯数据模型表达离散按钮在本地输入 step 上的 pressed、held、released 事实，使输入缓冲能与 Unity Input System adapter 和未来玩法消费层解耦。

#### Scenario: 记录按钮按下
- **WHEN** 某个离散按钮从未按住变为按住
- **THEN** 按钮事实 MUST 标记 pressed
- **AND** MUST 标记 held

#### Scenario: 记录按钮保持
- **WHEN** 某个离散按钮连续两个输入 step 都处于按住状态
- **THEN** 按钮事实 MUST 标记 held
- **AND** MUST NOT 标记 pressed

#### Scenario: 记录按钮释放
- **WHEN** 某个离散按钮从按住变为未按住
- **THEN** 按钮事实 MUST 标记 released
- **AND** MUST NOT 标记 held

#### Scenario: 保持纯数据边界
- **WHEN** 按钮事实被创建或读取
- **THEN** 按钮事实 MUST NOT 引用 Unity 场景对象
- **AND** MUST NOT 引用 Animancer、Cinemachine、CharacterController 或状态类

### Requirement: 本地输入请求缓冲
系统 MUST 从离散按钮 pressed 事实派生本地输入请求，并在请求有效窗口内允许未来玩法消费层查询和消费这些请求。

#### Scenario: 按下按钮生成请求
- **WHEN** 按钮事实包含 Attack、Dodge、Jump 或 Interact 的 pressed
- **THEN** 输入请求缓冲 MUST 生成对应种类的请求
- **AND** 请求 MUST 记录来源 step
- **AND** 请求 MUST 记录过期 step

#### Scenario: 窗口内可查询
- **WHEN** 当前 step 位于请求来源 step 和过期 step 的有效范围内
- **AND** 请求尚未在本次模拟中消费
- **THEN** 输入请求缓冲 MUST 能返回该请求

#### Scenario: 过期后不可查询
- **WHEN** 当前 step 超过请求过期 step
- **THEN** 输入请求缓冲 MUST 不再返回该请求作为可消费请求

#### Scenario: 消费后不重复返回
- **WHEN** consumer 在某次模拟中消费一个请求
- **THEN** 输入请求缓冲 MUST 避免同一请求在同次模拟中再次被消费

### Requirement: 预输入消费边界
系统 MUST 将预输入定义为输入请求在短窗口内等待玩法消费层消费，而不是输入层提前决定未来动作结果。

#### Scenario: 按下时不确定未来动作 step
- **WHEN** 玩家在 step N 提前按下 Attack
- **THEN** 输入缓冲 MUST 只记录 Attack 请求从 step N 起有效
- **AND** MUST NOT 记录未来某个 step 必定触发 Attack 动作

#### Scenario: 状态不允许时保留请求
- **WHEN** 请求仍在有效窗口内
- **AND** 当前状态或仲裁规则不允许消费该请求
- **THEN** 输入缓冲 MUST 保留该请求直到过期或被合法消费

#### Scenario: 只有玩法层消费请求
- **WHEN** 输入请求可被消费
- **THEN** 只有状态机、ActionArbiter 或等价玩法仲裁层 MUST 决定是否消费
- **AND** Input System adapter MUST NOT 直接消费请求
- **AND** Locomotion 输入读取 MUST NOT 直接消费 Attack、Dodge、Jump 或 Interact 请求

### Requirement: 缓冲窗口配置
系统 MUST 支持按请求种类配置本地预输入窗口长度，使不同输入请求能拥有不同的短窗口。

#### Scenario: 不同请求使用不同窗口
- **WHEN** Attack 和 Dodge 配置了不同窗口长度
- **THEN** 输入请求缓冲 MUST 分别计算 Attack 和 Dodge 的过期 step

#### Scenario: 零窗口请求
- **WHEN** 某个请求种类的窗口长度为 0
- **THEN** 该请求 MUST 只在来源 step 可被查询

#### Scenario: 窗口不依赖 Time.time
- **WHEN** 输入请求缓冲计算过期条件
- **THEN** 它 MUST 使用本地离散 step 或等价整数
- **AND** MUST NOT 依赖 `Time.time` 作为过期判断事实

### Requirement: 可重复构建语义
系统 MUST 能从同一段按钮事实序列重复构建输入请求缓冲，并在相同消费规则下得到确定结果。

#### Scenario: 同序列构建一致
- **WHEN** 系统用同一段按钮事实序列构建两次输入请求缓冲
- **AND** 使用相同的请求窗口和消费规则
- **THEN** 两次得到的可消费请求和消费结果 MUST 一致

#### Scenario: 消费结果不污染按钮事实
- **WHEN** 输入请求在某次模拟中被消费
- **THEN** 原始按钮事实 MUST 保持不变
- **AND** MUST NOT 保存该请求已触发动作的永久结果

### Requirement: 现有 Locomotion 边界保持
系统 MUST 保持当前 Locomotion 主线只消费 Move/Look 所需输入，不得因为输入缓冲层而新增第二套角色控制路径。

#### Scenario: Locomotion 行为保持
- **WHEN** `PlayerLocomotionController` 执行一帧移动 tick
- **THEN** 它 MUST 继续通过现有输入端口或等价 adapter 获取 Move/Look
- **AND** MUST 继续通过现有 pipeline 生成移动意图、世界方向、阶段和运动命令

#### Scenario: 输入缓冲不接管移动
- **WHEN** 输入缓冲处理离散按钮请求
- **THEN** 输入缓冲 MUST NOT 调用 `CharacterController.Move`
- **AND** MUST NOT 提交 `MovementCommand`
- **AND** MUST NOT 播放 Animancer 动画

#### Scenario: 不新增第二控制入口
- **WHEN** 实施输入缓冲层
- **THEN** 系统 MUST NOT 新增绕过 `PlayerLocomotionController`、Locomotion pipeline 或当前运动执行端口的角色控制器入口

### Requirement: 网络与预测回滚不进入本变更
系统 MUST 将本变更限制为本地预输入和输入缓冲层，不得在本变更中实现网络同步、完整 tick 系统或预测回滚。

#### Scenario: 不修改网络协议
- **WHEN** 实施本地输入缓冲层
- **THEN** 实施 MUST NOT 修改 Fantasy 协议文件
- **AND** MUST NOT 新增服务器输入队列实现

#### Scenario: 不实现预测回滚驱动
- **WHEN** 实施本地输入缓冲层
- **THEN** 实施 MUST NOT 新增预测回滚驱动
- **AND** MUST NOT 新增状态快照历史
- **AND** MUST NOT 新增服务器权威校正流程
