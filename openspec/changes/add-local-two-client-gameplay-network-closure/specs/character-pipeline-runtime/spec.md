## ADDED Requirements

### Requirement: 远端 Character 必须使用现有正交控制模式

系统 MUST 使用同一个 CharacterPipeline、RootTree、Timeline、ActionRuntime、AnimationLayerSelection、AnimationPlaybackLifecycle 和 PresentationStage 运行本地与远端 Corin。本地 Owner MUST 使用 LocalDevice + LocalSolver；远端 Character MUST 使用 ExternalFacts + ExternalPose。控制模式 MUST 在 pipeline 激活前确定，运行中修改 MUST 明确失败。系统 MUST NOT 恢复 LocalPredicted/RemoteProxy 总控枚举或第二套角色控制器。

#### Scenario: 创建本地 Owner

- **WHEN** roster 收到 JoinCompleted
- **THEN** Owner host MUST 在激活前配置 LocalDevice + LocalSolver
- **AND** InputStage 与 MotionStage MUST 继续使用现有本地路径

#### Scenario: 创建远端 Character

- **WHEN** roster 收到另一个 Actor descriptor
- **THEN** clone MUST 在激活前配置 ExternalFacts + ExternalPose
- **AND** Graph/Timeline MUST 继续产生状态与动画选择
- **AND** LocalSolver MUST 不移动远端 logic root

### Requirement: Character 必须支持显式 ExternalActionActivation

Character runtime MUST 提供 model-neutral `ExternalActionActivation` 输入，保存 ActionId、显式 ActionInstanceId、source sequence/tick 和必要 gameplay target identity。ActionRuntime MUST 使用该显式实例 ID 激活现有动作，并拒绝空 ID、重复 ID 或与当前实例冲突的 activation。该合同 MUST 不包含 network model、packet、endpoint 或动画字段。

#### Scenario: 远端攻击激活

- **WHEN** ExternalActionActivation 指向 Corin Attack 且实例 ID 合法
- **THEN** 现有 Action StateMachine MUST 进入对应 Attack authoring
- **AND** ActionRuntime MUST 使用外部实例 ID

#### Scenario: 重复激活

- **WHEN** 相同 actor/action instance 的 activation 再次到达
- **THEN** Character Runtime MUST 拒绝重复启动
- **AND** Timeline MUST 不重复播放

### Requirement: 远端 Character 不得要求本地相机依赖

ExternalFacts + ExternalPose CharacterPipelineHost MUST 不要求 CameraRig、camera follow/aim anchors 或 look input。其 movement basis MUST 来自 external movement summary；本地 CameraRig MUST 只绑定 LocalDevice + LocalSolver Owner。

#### Scenario: 激活远端 Corin

- **WHEN** roster 在没有独立 CameraRig 的情况下激活远端 clone
- **THEN** pipeline MUST 正常初始化
- **AND** clone MUST 不注册或重置本地相机目标

### Requirement: ActorLeft 必须精确释放 Character 资源

ActorLeft MUST 依次注销对应 binding、清空该 actor 的 model/snapshot/action 输入、Dispose CharacterPipeline 并销毁 clone。释放远端 Character MUST 不清理同 Session Owner、Fantasy endpoint 或其它 actor。

#### Scenario: 远端客户端离开

- **WHEN** roster 收到远端 ActorLeft
- **THEN** 对应远端 pipeline MUST 完整停止并释放
- **AND** 本地 Owner 与 Session MUST 继续运行
