## MODIFIED Requirements

### Requirement: RuntimeDebugSession 必须统一目标、interest、Capture 与只读视图

RuntimeDebugSession 或等价 service MUST 通过唯一共用工具控制服务协调 registered target、显式 target、target-level Live interest、共享 provider 和 Capture 开始/停止。GM 与 Editor MUST 调用同一服务；现有 runtime target/store/provider MUST 继续拥有正式诊断事实，控制服务不得创建可写角色镜像。Editor 的 Capture history position 与只读视图 MUST 读取同一已冻结记录，Graph、Timeline 和 Host Inspector 不得各自扫描 runtime service、持有 runtime clone 或重建第二份 diagnostics 数据。

共用服务 MUST 不保存全局 Graph/Timeline runtime instance、Follow 或 Pin。Graph、Timeline 等观察页面 MUST 继续通过各自的 editor-only view binding 保存 source、Follow/Pin 和 runtime instance。Player 目标 MUST 使用明确的进程、业务 Session、Actor 与 runtime identity，不按显示名或场景顺序选择。

#### Scenario: GM 与 Editor 观察同一个进程目标

- **WHEN** 两个入口订阅同一已注册 Actor
- **THEN** MUST 使用同一 provider，interest 按 owner 汇总
- **AND** 关闭一个入口 MUST 只释放自己的订阅，不停止另一入口或独立 Capture

#### Scenario: Graph 与 Timeline 同时观察

- **WHEN** 同一 Character target 的 Graph 窗口 Follow 一个 Graph，Timeline 窗口 Follow 一个 playback
- **THEN** 两个窗口 MUST 使用同一共享 provider 和 Capture 数据
- **AND** 各窗口 MUST 保留自己的 runtime instance，不互相覆盖 Follow/Pin

#### Scenario: 目标结束

- **WHEN** 已附着 target 注销
- **THEN** 共用服务 MUST 终止该目标的记录订阅并发布明确结束状态
- **AND** 视图 MUST 只保留已冻结的 Ended 记录，不持有 runtime store 或附着同名新角色

### Requirement: Trace channel 必须控制调试采集成本

系统 MUST 至少提供 Graph、StateMachine、Timeline、Blackboard、Animation、Motion 和 GameplayEffect channel。未被 Live interest 或显式 Capture 请求的 channel MUST 阻止其非必要 payload 构造、source handle 解析和 diagnostics 写入，并且 MUST NOT 改变 runtime 执行结果。GM、Editor 与持久采样请求 MUST 通过同一 owner interest 汇总，不能启用无界默认全量记录。

#### Scenario: 关闭 Animation channel

- **WHEN** 当前目标没有 Animation 观察或采样订阅
- **THEN** runtime MUST NOT 构建非必要 Animation trace payload
- **AND** ActionPlaybackCommandInbox、CharacterActionPlaybackRuntime、PoseStateMachine、AnimationSlot、显式 Player 与 Pose Graph native job MUST 继续产生相同正式结果

#### Scenario: 记录 Blackboard 值

- **WHEN** Blackboard channel 启用且变量发生正式写入或清理
- **THEN** Trace MUST 使用受限结构化 debug value snapshot
- **AND** MUST NOT 持有任意 gameplay object reference 或调用未知对象逻辑作为序列化 fallback

#### Scenario: 关闭 GameplayEffect channel

- **WHEN** 当前目标没有 GameplayEffect 订阅
- **THEN** runtime MUST NOT 构建非必要 tag、attribute、effect lifecycle 或 prediction journal trace payload
- **AND** Gameplay Effect MUST 继续产生相同 tag、attribute、effect 和 sync fact 结果

#### Scenario: 记录 Effect 生命周期

- **WHEN** GameplayEffect channel 启用且 effect 被应用、叠层、抑制、到期或移除
- **THEN** Trace MUST 使用稳定 effect identity、instance identity、context、logic tick 和结构化结果
- **AND** MUST NOT 持有 Effect asset、component asset 或 active runtime object reference
