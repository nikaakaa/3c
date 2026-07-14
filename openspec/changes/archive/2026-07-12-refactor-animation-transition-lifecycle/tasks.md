## 1. 基线与实施门槛

- [x] 1.1 确认 `refactor-pipeline-blackboard-owned-scopes` 已完成或本 change 已按其最终结果重新基线化。
- [x] 1.2 确认 `restore-timeline-treeclip-pipeline-runtime` 已完成或本 change 已按其最终结果重新基线化。
- [x] 1.3 重新读取 animation pipeline、layer runtime、presentation interpolation、state interruption、SM authoring 和 root motion current specs。
- [x] 1.4 记录 StateMachine internal transition、Exit、parent graceful stop、ForceStop、deactivate 和 dispose 的当前调用链。
- [x] 1.5 记录 Registry pending/active owner transition、Outgoing 和 retirement 的当前数据流。
- [x] 1.6 记录 PresentationStage TransitionBlendSession 的创建、推进、完成和重入覆盖路径。
- [x] 1.7 记录 Animancer presenter 创建 graph、插入 output playable、Evaluate 和 dispose 的当前生命周期。
- [x] 1.8 确认当前 Animancer package 的 `InsertOutputJob` 能在现有 presenter graph 中安全使用。
- [x] 1.9 确认角色 Animator visual hierarchy 能创建稳定 animation stream handles。
- [x] 1.10 若 output job、stream handle 或 presentation delta 无法通过现有正式 adapter 接入，停止 apply 并记录缺口，不建立旁路。

## 2. Transition Definition 数据模型

- [x] 2.1 定义 `AnimationTransitionStrategy` 枚举。
- [x] 2.2 添加 `Immediate` 策略值。
- [x] 2.3 添加 `ContributionCrossFade` 策略值。
- [x] 2.4 添加 `Inertialization` 策略值。
- [x] 2.5 定义可内联序列化的 `AnimationTransitionDefinition`。
- [x] 2.6 在 definition 中保存显式 strategy。
- [x] 2.7 在 definition 中保存 duration。
- [x] 2.8 在 definition 中保存 curve。
- [x] 2.9 在 definition 中保留 edge authoring identity 的追踪入口。
- [x] 2.10 将 definition 接入 StateMachine Transition edge 正式序列化。
- [x] 2.11 删除 edge 上旧 animation blend duration 独立字段。
- [x] 2.12 删除 edge 上旧 animation blend curve 独立字段。
- [x] 2.13 删除旧字段的反序列化兼容读取。
- [x] 2.14 保持 ConditionRuleGraph 数据模型不包含任何 animation transition 字段。

## 3. Transition Request 合同

- [x] 3.1 定义稳定 `AnimationTransitionInstanceId`。
- [x] 3.2 定义 `AnimationTransitionRequest`。
- [x] 3.3 在 request 中保存 StateMachine runtime activation scope。
- [x] 3.4 在 request 中保存 source owner identity。
- [x] 3.5 在 request 中保存 target owner identity 或显式 Empty target。
- [x] 3.6 在 request 中保存 transition definition snapshot。
- [x] 3.7 在 request 中保存逻辑 stop/release cause。
- [x] 3.8 让每次命中 edge 生成新的 transition instance identity。
- [x] 3.9 禁止 duration、clip 类型或 contribution 缺失隐式推断 strategy。
- [x] 3.10 禁止无 target、无 strategy、无 cause 的 owner release 进入表现管线。

## 4. Transition Runtime 生命周期

- [x] 4.1 创建来源无关的 `CharacterAnimationTransitionRuntime`。
- [x] 4.2 定义 `Requested` 生命周期状态。
- [x] 4.3 定义 `WaitingTarget` 生命周期状态。
- [x] 4.4 定义 `Capturing` 生命周期状态。
- [x] 4.5 定义 `Running` 生命周期状态。
- [x] 4.6 定义 `Completed` 生命周期状态。
- [x] 4.7 定义 `Retired` 生命周期状态。
- [x] 4.8 定义 `Superseded` 终止结果和替代者 identity。
- [x] 4.9 按 StateMachine runtime activation scope 保存 active transition。
- [x] 4.10 限制同一 runtime activation scope 最多一个 active transition。
- [x] 4.11 让不同 runtime activation scopes 可并行推进 transition。
- [x] 4.12 实现 request 的幂等接收和重复 identity 检查。
- [x] 4.13 实现 target ready 门控。
- [x] 4.14 实现 target Empty 的正式 ready 语义。
- [x] 4.15 实现 transition complete 到 retire 的资源释放边界。
- [x] 4.16 实现 host deactivate/dispose 时 active transition 的确定性释放。

## 5. StateMachine 与 Tree 生命周期发布

- [x] 5.1 让 internal state transition 发布 source -> target request。
- [x] 5.2 让 Transition to Exit 发布 source -> Empty request。
- [x] 5.3 让 target State 首次 OnEnter 或 Root tick 发布 TargetReady。
- [x] 5.4 保持 TargetReady 与 target animation contribution 是否存在分离。
- [x] 5.5 让 parent graceful replacement 通过 stop context 传递明确 transition definition。
- [x] 5.6 让 parent graceful stop 发布 source -> Empty request。
- [x] 5.7 让 ForceStop 发布显式 Immediate source -> Empty request。
- [x] 5.8 让 pipeline deactivate 发布显式 Immediate release request。
- [x] 5.9 让 pipeline dispose 发布显式 Immediate release request。
- [x] 5.10 保持 source State 在逻辑 barrier 内完成 OnExit。
- [x] 5.11 保持 source Timeline、Action、motion 和 gameplay output 在逻辑 barrier 内关闭。
- [x] 5.12 禁止 StateMachine runtime 等待动画 transition 完成。
- [x] 5.13 禁止 source State 为表现收尾继续 tick。
- [x] 5.14 对缺少明确 definition 的 graceful stop 报告配置错误。

## 6. Registry 职责收缩

- [x] 6.1 保留 playback instance identity 管理。
- [x] 6.2 保留 contribution instance identity 管理。
- [x] 6.3 保留 runtime owner membership 管理。
- [x] 6.4 保留 Active contribution 生命周期。
- [x] 6.5 保留 CompletedHeld contribution 生命周期。
- [x] 6.6 保留 Retired contribution 生命周期。
- [x] 6.7 删除 Registry 的 PendingOwnerTransition 数据。
- [x] 6.8 删除 Registry 的 ActiveOwnerTransition 数据。
- [x] 6.9 删除 Registry 的 transition elapsed 和 curve progress。
- [x] 6.10 删除 Outgoing 作为 Registry transition session 状态。
- [x] 6.11 删除 Registry 的 TargetReady 门控职责。
- [x] 6.12 删除 Registry 的 transition supersede 职责。
- [x] 6.13 将 owner membership release 与 visual transition retirement 分离。
- [x] 6.14 删除含义不清的 `ReleaseOwner` command。
- [x] 6.15 删除 active transition 重入时直接 retire source 的路径。
- [x] 6.16 保持 sample、complete、membership release 的幂等处理。

## 7. PresentationStage 批次重构

- [x] 7.1 在 presentation batch 中先合并本帧 target samples。
- [x] 7.2 在同一 batch 中消费 TargetReady。
- [x] 7.3 在同一 batch 中启动 waiting transition capture。
- [x] 7.4 使用真实 presentation delta 推进 transition runtime。
- [x] 7.5 将 transition strategy 输出并入统一 LayerRuntime 输入。
- [x] 7.6 在 LayerRuntime 后只生成一份最终 Animancer playback plan。
- [x] 7.7 删除 PresentationStage 旧 `TransitionBlendSession`。
- [x] 7.8 删除 PresentationStage 旧 owner transition completion 回调。
- [x] 7.9 删除 source release 与 target first sample 之间的空计划窗口。
- [x] 7.10 保持 Timeline visual resampling 不改写 logic time。
- [x] 7.11 保持 PresentationFrame 不提交 window、cue、motion 或 sync facts。

## 8. Immediate 策略

- [x] 8.1 实现 Immediate strategy runtime。
- [x] 8.2 校验 Immediate duration 必须为 0。
- [x] 8.3 在同一 presentation batch 接受 target snapshot。
- [x] 8.4 在同一 presentation batch 释放 source visual snapshot。
- [x] 8.5 保证 Immediate 中间不产生空 playback plan。
- [x] 8.6 让 ForceStop、deactivate 和 dispose 只使用 Immediate。
- [x] 8.7 让 target Empty 的 Immediate 明确输出空计划。

## 9. ContributionCrossFade 策略

- [x] 9.1 实现 ContributionCrossFade strategy runtime。
- [x] 9.2 校验 CrossFade duration 必须大于 0。
- [x] 9.3 在 Capturing 冻结 source owner 最后合法 contribution snapshot。
- [x] 9.4 保证冻结 snapshot 不依赖 source producer 后续提交。
- [x] 9.5 使用 presentation delta 推进 elapsed。
- [x] 9.6 使用 definition curve 计算 source 权重。
- [x] 9.7 使用 definition curve 计算 target 权重。
- [x] 9.8 将 source 与 target 放入同一 LayerRuntime batch。
- [x] 9.9 保持 target contribution 按 visual Timeline time 正常重采样。
- [x] 9.10 让 target Empty 作为真实空目标参与淡出。
- [x] 9.11 在 duration 完成后释放冻结 source snapshot。
- [x] 9.12 禁止 CrossFade 继续 tick source State、Timeline 或 Action。

## 10. LayerRuntime 仲裁统一

- [x] 10.1 按 priority 从高到低处理同一 Override layer。
- [x] 10.2 让高优先级 contribution 只占用其当前实际权重。
- [x] 10.3 让后续低优先级 contribution 填充层剩余权重。
- [x] 10.4 让同优先级超出剩余权重时组内归一化。
- [x] 10.5 保持合法 additive contribution 独立进入最终计划。
- [x] 10.6 让普通 contribution 和 transition strategy snapshot 使用同一仲裁入口。
- [x] 10.7 删除与“低优先级一律剔除”旧规则对应的实现残留。
- [x] 10.8 保持缺失 layer 为配置错误且不创建默认 layer。

## 11. Inertialization Pose History

- [x] 11.1 定义角色 visual skeleton binding 数据。
- [x] 11.2 从正式 Animator visual hierarchy 创建 stream handles。
- [x] 11.3 排除 Animator/visual root 的 root motion 通道。
- [x] 11.4 定义 current final local pose buffer。
- [x] 11.5 定义 previous final local pose buffer。
- [x] 11.6 定义每骨骼 local position velocity buffer。
- [x] 11.7 定义每骨骼 local rotation velocity buffer。
- [x] 11.8 使用最短弧计算 quaternion 旋转差。
- [x] 11.9 处理首个 presentation frame 无 previous pose 的初始化。
- [x] 11.10 处理 presentation delta 非法或为 0 的配置错误。
- [x] 11.11 保证 pose history 只属于表现 runtime。
- [x] 11.12 禁止黑板、ConditionRuleGraph、SyncFact 或网络层读取 pose history。

## 12. Inertialization Output Job

- [x] 12.1 定义 Unity Animation Job 的 native 输入数据。
- [x] 12.2 通过 Animancer `InsertOutputJob` 插入正式 output job。
- [x] 12.3 将 output job 放在 Animancer layer 合成之后。
- [x] 12.4 保留后续 IK/程序化姿态插入位置。
- [x] 12.5 在 Capturing 读取当前最终输出 local pose。
- [x] 12.6 在 Capturing 计算 source pose velocity。
- [x] 12.7 读取 target Animancer pose 作为新基准。
- [x] 12.8 按 curve 和 elapsed 衰减 local position offset。
- [x] 12.9 按 curve 和 elapsed 衰减 local rotation offset。
- [x] 12.10 对 position/rotation 数值应用有限性检查。
- [x] 12.11 在 `ProcessRootMotion` 中只透传上游 root motion。
- [x] 12.12 禁止 output job 生成 motion contribution。
- [x] 12.13 禁止 output job 修改逻辑 Transform。
- [x] 12.14 向 output job 显式提交真实 presentation delta。
- [x] 12.15 保持 Animancer 手动 `Evaluate(0)` 不成为 job 时间来源。
- [x] 12.16 在 transition complete 后清空该 instance 的 inertial offsets。
- [x] 12.17 在 presenter dispose 时释放全部 NativeArray 和 handles。
- [x] 12.18 缺少有效 rig/output job 时报告配置错误，不降级为 CrossFade。

## 13. Transition 重入

- [x] 13.1 新 request 到来时定位同 scope active transition。
- [x] 13.2 为旧 transition 记录 Superseded 原因。
- [x] 13.3 记录替代者 transition instance identity。
- [x] 13.4 在 CrossFade 重入时冻结当前加权视觉 contribution snapshot。
- [x] 13.5 在 Inertialization 重入时捕获当前已修正最终 pose。
- [x] 13.6 在 Inertialization 重入时继承当前最终 pose velocity。
- [x] 13.7 在 Immediate 重入时原子替换 target。
- [x] 13.8 在新 capture 完成后释放旧 transition snapshot/native data。
- [x] 13.9 禁止同一 scope 保留 active transition 栈。
- [x] 13.10 保持 Locomotion 与 Action 不同 scope 的 transition 可并行存在。

## 14. Edge Inspector 与默认数据

- [x] 14.1 在 Transition edge Inspector 添加 strategy 枚举控件。
- [x] 14.2 为 Immediate 隐藏 curve 编辑并固定 duration 为 0。
- [x] 14.3 为 ContributionCrossFade 显示 duration 和 curve。
- [x] 14.4 为 Inertialization 显示 duration、curve 和 rig binding 摘要。
- [x] 14.5 在 edge 画布摘要显示 strategy 和 duration。
- [x] 14.6 让新建 edge 必须写入显式 definition。
- [x] 14.7 让默认 StateMachineGraph 初始化写入显式 strategy。
- [x] 14.8 保持 Transition Rule 视图只显示条件图数据。
- [x] 14.9 禁止 ExposedProperty 保存 animation strategy、duration 或 curve。
- [x] 14.10 保持 inline StateMachineGraph 下钻编辑链路不变。

## 15. Validator 与 Debug

- [x] 15.1 Validator 报告缺失 strategy。
- [x] 15.2 Validator 报告 Immediate 非零 duration。
- [x] 15.3 Validator 报告 CrossFade 非正 duration。
- [x] 15.4 Validator 报告 Inertialization 非正 duration。
- [x] 15.5 Validator 报告 graceful Empty release 缺少 definition。
- [x] 15.6 Validator 报告 Inertialization host 缺少正式 Animator/rig binding。
- [x] 15.7 Runtime debug 显示 transition instance identity。
- [x] 15.8 Runtime debug 显示 lifecycle state 和 strategy。
- [x] 15.9 Runtime debug 显示 source、target 和 target ready。
- [x] 15.10 Runtime debug 显示 elapsed、duration 和 curve progress。
- [x] 15.11 Runtime debug 显示 complete、release 或 supersede 原因。
- [x] 15.12 Runtime debug 显示 CrossFade snapshot 摘要。
- [x] 15.13 Runtime debug 显示 Inertialization bone count、offset 和 velocity 摘要。
- [x] 15.14 保持 debug snapshot 只读且不成为 runtime 输入。

## 16. 资产破坏性迁移

- [x] 16.1 枚举全部 StateMachine Transition edge 的旧 blend 字段。
- [x] 16.2 将旧 duration 为 0 的边写入显式 Immediate definition。
- [x] 16.3 将旧 duration 大于 0 的边写入显式 ContributionCrossFade definition。
- [x] 16.4 保留旧 curve 的数值语义到新 definition。
- [x] 16.5 删除迁移后 asset 中旧字段数据。
- [x] 16.6 将 Corin Attack1 进入、连击和退出指定边显式配置为 Inertialization。
- [x] 16.7 将 Corin Attack2 进入和退出指定边显式配置为 Inertialization。
- [x] 16.8 将 Corin Dodge 进入和返回 Run/Idle 指定边显式配置为 Inertialization。
- [x] 16.9 将 Corin 高频急停/转身指定边显式配置为 Inertialization。
- [x] 16.10 保持普通 locomotion 边显式使用 Immediate 或 ContributionCrossFade。
- [x] 16.11 不修改 Corin 动画资源选择和 MotionCurve 资产。
- [x] 16.12 不创建新的动画 SO、SubTree asset 或 fallback config。
- [x] 16.13 通过正式 authoring service 更新 Corin 图摘要。
- [x] 16.14 通过正式 agent snapshot/export 更新动画 transition 摘要。

## 17. 旧路径删除与静态校验

- [x] 17.1 搜索并删除 `PendingOwnerTransition` 定义与引用。
- [x] 17.2 搜索并删除 `ActiveOwnerTransition` 定义与引用。
- [x] 17.3 搜索并删除旧 `TransitionBlendSession` 定义与引用。
- [x] 17.4 搜索并删除旧 `CompleteOwnerTransition` 命令与引用。
- [x] 17.5 搜索并删除旧 `ReleaseOwner` 模糊命令与引用。
- [x] 17.6 搜索并删除 active transition 重入直接 retire source 的逻辑。
- [x] 17.7 搜索并确认 ConditionRuleGraph 不含动画 transition 字段。
- [x] 17.8 搜索并确认 producer 不直接调用 Animator、Animancer 或 PlayableGraph。
- [x] 17.9 搜索并确认 inertialization 不调用 motion resolver 或写逻辑 Transform。
- [x] 17.10 搜索并确认没有 duration 推断 strategy 的 fallback。
- [x] 17.11 搜索并确认没有缺 rig 自动降级 CrossFade 的 fallback。
- [x] 17.12 搜索并确认没有旧 edge 字段兼容读取。

## 18. 构建、资产校验与 OpenSpec 收口

- [x] 18.1 运行 StateMachine/animation 资产 validator 并清理全部新增 error。
- [x] 18.2 触发 Unity 正常脚本和资产导入并清理全部新增 console error。
- [x] 18.3 使用 `--disable-build-servers /nr:false /p:UseSharedCompilation=false` 编译相关项目。
- [x] 18.4 立即运行 `dotnet build-server shutdown`。
- [x] 18.5 对照 current specs 确认 override priority-fill 冲突已消除。
- [x] 18.6 对照两个依赖 change 的最终 delta 确认没有重复或矛盾 requirement。
- [x] 18.7 对照 proposal、design 和实现逐项确认任务状态真实。
- [x] 18.8 将全部已完成任务更新为 `[x]`。
- [x] 18.9 运行 `openspec validate refactor-animation-transition-lifecycle --strict --no-interactive`。
