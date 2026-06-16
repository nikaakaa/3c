# 动画运动源采样管线任务

## 1. 现状确认
- [x] 1.1 读取本变更 `proposal.md`、`design.md` 和全部 spec delta。
- [x] 1.2 读取旧 Animator pending delta 方案，确认 pending 拉取消费假设被本变更取代。
- [x] 1.3 读取 `BasicMovementMotionFacts`，确认当前 TurnBack source enum 和 policy 字段。
- [x] 1.4 读取 `PlayerLocomotionController.ResolveTurnBackRootMotionFacts`，确认当前 source 选择和消费点。
- [x] 1.5 读取 `BasicLocomotionAnimancerPresenter.OnAnimatorMove`，确认 Animator runtime delta 采集点。
- [x] 1.6 读取默认状态机和动画配置资产，确认 TurnBack 配置入口。

## 2. TurnBack 默认权威路线
- [x] 2.1 将 `TurnBackMotionPolicy.Default` 的 yaw source 设为 `BakedMotionProfile`。
- [x] 2.2 将 `TurnBackMotionPolicy.Default` 的 translation source 设为 `BakedMotionProfile`。
- [x] 2.3 将默认 profile id 设为 `Locomotion.Turn.Back`。
- [x] 2.4 更新默认状态机资产，使 TurnBack yaw/translation source 均为 profile source。
- [x] 2.5 更新 Corin run locomotion animation config，使 TurnBack 绑定 sampled motion profile。
- [x] 2.6 保留 TurnBack alias `Locomotion.Turn.Back`。
- [x] 2.7 保留 TurnBack input lock、motion window 和 exit window 语义。

## 3. Animator Pending Delta 删除
- [x] 3.1 `ResolveTurnBackRootMotionFacts` 不再从 `OnAnimatorMove` pending buffer 拉取消费 delta。
- [x] 3.2 缺失 baked profile 时不 fallback 到 runtime root delta。
- [x] 3.3 `BasicLocomotionAnimancerPresenter` 不再保存 `pendingRootMotionDelta`。
- [x] 3.4 `BasicLocomotionAnimancerPresenter` 不再实现 root motion source 接口。
- [x] 3.5 `BasicLocomotionAnimancerPresenter` 不再实现 root motion rollback provider。
- [x] 3.6 删除 `ILocomotionRootMotionSource` 接口文件和 meta。
- [x] 3.7 rollback snapshot 不再从 Presenter 捕获 pending runtime root delta。
- [x] 3.8 `OnAnimatorMove` 诊断日志保留，但只报告 `presenter-delta-ignored`。
- [x] 3.9 确认没有新增直接 Transform 写入路径。
- [x] 3.10 确认没有新增第二套 TurnBack controller。
- [x] 3.11 删除 `RuntimeRootDelta` yaw/translation 配置枚举值。
- [x] 3.12 删除 `LocomotionRootMotionDelta` pending delta 值类型和 csproj include。
- [x] 3.13 删除旧 OpenSpec change `update-turnback-runtime-root-delta`。
- [x] 3.14 删除 `ILocomotionAuthoredRootMotionSource` 表现层当前 clip source。
- [x] 3.15 删除 `LocomotionAuthoredRootMotionDelta` authored delta 值类型和 csproj include。
- [x] 3.16 删除 `AnimationClipRootMotionSampler` 运行时/测试采样路径和 csproj include。
- [x] 3.17 将 Humanoid TurnBack Transition 视觉 clip 改为 NoRootTurn Inplace。

## 4. TickSampledMotion 采样主线
- [x] 4.1 复用 `AnimationMotionPlaybackWindow` 构建上一 tick 到当前 tick 的采样窗口。
- [x] 4.2 在 phase、gait、alias 或播放进度不连续时重置采样窗口。
- [x] 4.3 在 motion window inactive 时输出无动画运动贡献。
- [x] 4.4 通过 `LocomotionMotionProfileSO` 采样 TurnBack planar delta。
- [x] 4.5 通过 `LocomotionMotionProfileSO` 采样 TurnBack yaw delta。
- [x] 4.6 将 sampled planar delta 标记为 local space。
- [x] 4.7 将 sampled 结果转换为 `BasicMovementMotionFacts`。
- [x] 4.8 由现有 motion executor 应用 sampled animation yaw。
- [x] 4.9 由现有 motion executor 应用 sampled animation planar delta。

## 5. 自动测试
- [x] 5.1 增加/更新测试：TurnBack 默认 policy 使用 baked profile source。
- [x] 5.2 增加/更新测试：TurnBack 状态配置使用 baked profile source。
- [x] 5.3 增加/更新测试：缺失 baked profile 不 fallback 到 runtime root delta。
- [x] 5.4 增加/更新测试：sampled profile 使用时忽略 runtime root delta。
- [x] 5.5 增加/更新静态测试：controller 不再消费 pending root motion source。
- [x] 5.6 增加/更新静态测试：Presenter 不暴露 pending root motion source 或 rollback provider。
- [x] 5.7 增加/更新静态测试：Presenter 不直接调用 `CharacterController.Move` 或写 Transform。
- [x] 5.8 增加/更新测试：motion executor 应用 sampled animation yaw。
- [x] 5.9 增加/更新测试：motion executor 应用 sampled animation planar delta。

## 6. 编译和定向验证
- [x] 6.1 运行 `dotnet build 3cDemo/Client/3C_Client/Assembly-CSharp.csproj --no-restore -v:minimal`。
- [x] 6.2 运行 `dotnet build 3cDemo/Client/3C_Client/Assembly-CSharp-Editor.csproj --no-restore -v:minimal`。
- [x] 6.3 使用 Unity Test Runner 定向运行 `UnifiedCharacterStateMachineTests`，结果 110/110 通过。
- [x] 6.4 使用 Unity Test Runner 定向运行 motion profile / visual clip 相关 EditMode 测试，随 6.3 同批通过。
- [x] 6.5 读取 Unity Console：编译前 0 error；测试后仅出现 Unity Test Runner 保存 `TestResults.xml` 的记录，无编译或测试失败 error。
- [x] 6.6 运行 `openspec validate add-animation-motion-source-pipeline --strict --no-interactive`。
- [x] 6.7 不运行 Unity batchmode。

## 7. 手动验证
- [ ] 7.1 打开 Sandbox 场景并使用当前可琳角色。
- [ ] 7.2 启用 Locomotion、Animation 相关诊断 channel。
- [ ] 7.3 按 W 进入 RunLoop 后切 S，确认进入 `FullBody/Locomotion/TurnBack`。
- [ ] 7.4 确认 TurnBack 动画播放 `Locomotion.Turn.Back`。
- [ ] 7.5 搜索 `turnback-root-motion-consumed`，确认 `appliedYawSource=BakedMotionProfile`。
- [ ] 7.6 搜索 `turnback-root-motion-consumed`，确认 `appliedTranslationSource=BakedMotionProfile`。
- [ ] 7.7 搜索 `presenter-delta-ignored`，确认 Animator delta 只作为诊断存在。
- [ ] 7.8 确认运行日志不再出现 Animator pending delta 被 controller 消费的链路。
- [ ] 7.9 观察角色朝向随 TurnBack sampled profile 稳定转身。
- [ ] 7.10 观察 TurnBack 期间普通输入位移和旋转不叠加。
- [ ] 7.11 观察 motion window 结束后不继续应用 TurnBack root motion。
- [ ] 7.12 观察 exit window 后回到 MoveLoop 或 Idle。
- [ ] 7.13 验证 Walk、MoveStart、MoveStop、Idle 不误触发 TurnBack。
- [ ] 7.14 验证 A/D 横向切换不误触发前后 TurnBack。

## 8. OpenSpec 收尾
- [x] 8.1 对照 proposal 确认没有实现未审批旁路。
- [x] 8.2 检查是否需要更新 Path 文档，结果为 `no-op`：现有 Path 文档目录属于 DG_Entity，未发现 3C TurnBack/animation motion 对应文档；链接检查器只发现 DG_Entity 既有反向依赖缺失，和本变更无关。
- [ ] 8.3 完成验证后再统一归档本 change。
