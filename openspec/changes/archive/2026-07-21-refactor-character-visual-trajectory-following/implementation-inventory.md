# 实现清单

## 正式运行链

```text
Float32 / Fixed / Observed World Body
  -> CharacterPresentationBodyState
       ActorId
       Position
       Rotation
       LinearVelocity
       Grounded
  -> CharacterBodyPresentationRuntime
       Source Cursor: CommittedStream | SelectedStream
       Target Trajectory Sampler
  -> CharacterVisualTrajectoryFollower
       Direct | BoundedCorrection
  -> CharacterBodyPresentationFrame
       target / visible / correction diagnostics
  -> VisualRoot
       AnimationSampleTick / Alpha
       Camera
```

`CharacterBodyPresentationRuntime`仍是Body history、表现时钟、target采样、visual correction和VisualRoot运行时写入的唯一owner。动画继续消费Source Cursor的tick与alpha，Follower不修改动画时间。

## 最终类型

- `CharacterPresentationBodyState`：正式表现运动学状态，直接投影Position、Rotation、LinearVelocity和Grounded。
- `CharacterBodyPresentationSourceMode`：只描述Body数据源，正式值为`CommittedStream`和`SelectedStream`。
- `CharacterVisualTrajectoryMode`：只描述视觉响应，正式值为`Direct`和`BoundedCorrection`。
- `CharacterBodyPresentationProfile`：唯一Presentation-owned authoring资产类型。
- `CharacterBodyPresentationSettings`：Profile在runtime创建时生成的不可变参数。
- `CharacterVisualTrajectoryFollower`：唯一visual correction状态owner。
- `CharacterBodyPresentationFrame`：同时暴露target、visible、correction和source/trajectory诊断。

## Target采样语义

- Committed source按单调presentation cursor重采样相邻tick；正常append不创建correction。
- Committed branch replacement只删除replacement起点及之后的旧样本，一批Rollback transaction只retarget一次。
- Selected source只接受连续append或显式Reset；无Reset的tick回退、重复或运动学断裂直接报错。
- Position、Rotation和LinearVelocity在区间内插值；Grounded在区间两端都落地时保持落地，否则到右端点再采用右端状态。
- 正常连续target直接驱动visible pose，不再持续运行第二次SmoothDamp。

## Follower语义

- `Direct`始终令visible等于target。
- `BoundedCorrection`只在branch replacement或显式Reset retarget时保存相对位置、相对速度、yaw和相对yaw速度。
- 误差按presentation delta和Profile half-life执行临界阻尼衰减。
- 超过maximum的误差在retarget帧限制到边界；低于settle阈值后清零。
- Grounded target直接采用target Y，只纠正水平误差；Airborne target允许三维误差收敛。
- 连续revision从当前visible pose和velocity重新计算误差，不累计旧offset或固定恢复时长。

## Profile资产与装配

- `CorinDirectBodyPresentationProfile.asset`，GUID `4791f09a9f8040b987d486eb6576cb0e`：Standard Local、Timeline Preview和ServerAuthoritative本地预测Actor。
- `CorinRollbackBodyPresentationProfile.asset`，GUID `00a5d19d0f46472db86d13cf1b930c0a`：DeterministicRollback本地与远端完整模拟Actor；position half-life `0.04s`、maximum `0.18m`、settle `0.005m`，yaw half-life `0.035s`、maximum `12deg`、settle `0.25deg`。
- `CorinObservedBodyPresentationProfile.asset`，GUID `af66407d1a1449718e36c61f7c6bb234`：ServerAuthoritative observed Actor；保留旧资产identity并迁移为通用Profile类型。
- `CharacterPipelineHost`、`DeterministicRollbackCharacterHost`和`ServerAuthoritativeRemotePresentationSite`都必须显式引用Profile；缺失或参数非法时创建失败。

## 调用点

- `CharacterPipelineHost -> CharacterPresentationRuntimeFactory.CreateLocalOwner`
- `PreviewSimulationActorRegistration -> CharacterPresentationRuntimeFactory.CreateSimulatedActor`
- `DeterministicRollbackCharacterHost -> CreateLocalOwner | CreateSimulatedActor`
- `ServerAuthoritativeRemotePresentationSite -> CreateObservedActor`

所有Factory入口都要求Profile，不按Network Model、Actor名称、Camera或运行角色推断。

## 删除的旧路径

- 删除`CharacterRemotePresentationProfile`和`CharacterRemotePresentationSettings`命名。
- 删除`CharacterBodyPresentationStreamMode`旧命名。
- 删除committed recovery position/rotation offset、active状态和固定`6 / tickRate`恢复时长。
- 删除selected visual position/yaw velocity与逐帧`SmoothDamp`/`SmoothDampAngle`。
- 不保留`MovedFrom`、兼容wrapper、默认Profile、硬编码runtime参数或Network Model内的visual filter。

## 未改变边界

- 未修改Network packet、Program ABI、Snapshot、StateHash或WorldSolver合同。
- 未新增Gameplay fact、同步状态或第二份动画时钟。
- Preview结束后恢复编辑器原始Transform只是会话清理，不是第二条运行时VisualRoot写入链。
