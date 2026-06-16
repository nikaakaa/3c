## 1. 现状确认
- [ ] 1.1 读取本变更 `proposal.md`、`design.md` 和全部 spec delta。
- [ ] 1.2 读取 `refactor-character-hierarchical-state-runtime` 当前 proposal/design/tasks。
- [ ] 1.3 读取 `formalize-animation-playback-rollback-authority` 当前 proposal/design/tasks。
- [ ] 1.4 搜索 `BasicLocomotionAnimancerPresenter` 的所有运行时代码引用。
- [ ] 1.5 搜索 `ActionAnimationAnimancerPresenter` 的所有运行时代码引用。
- [ ] 1.6 搜索 prefab/scene 中两个 Presenter 的组件挂载位置。
- [ ] 1.7 搜索测试中直接创建两个 Presenter 的 fixture。
- [ ] 1.8 列出统一 Presenter 必须承接的 Locomotion 行为。
- [ ] 1.9 列出统一 Presenter 必须承接的 Action 行为。
- [ ] 1.10 确认不引入第二个 Animator、第二个 AnimancerComponent 或第二套播放 facade。

## 2. 模型和接口
- [ ] 2.1 设计统一动画播放请求模型。
- [ ] 2.2 请求模型包含播放域或 owner 标识。
- [ ] 2.3 请求模型包含稳定 key。
- [ ] 2.4 请求模型包含 timeline binding key。
- [ ] 2.5 请求模型包含 source step。
- [ ] 2.6 请求模型支持 Locomotion phase。
- [ ] 2.7 请求模型支持 Locomotion gait。
- [ ] 2.8 请求模型支持 entry start normalized time override。
- [ ] 2.9 请求模型支持 Action clear / release 语义。
- [ ] 2.10 请求模型不包含 Animancer runtime 对象。
- [ ] 2.11 设计统一播放快照模型。
- [ ] 2.12 播放快照可映射到 Locomotion progress fact。
- [ ] 2.13 播放快照可映射到 Action progress fact。
- [ ] 2.14 播放快照不暴露 Animancer state。

## 3. 测试先行
- [ ] 3.1 增加统一 Presenter 播放 Locomotion alias 的测试。
- [ ] 3.2 增加统一 Presenter 播放 Action key 的测试。
- [ ] 3.3 增加同 key 连续提交不重复重播测试。
- [ ] 3.4 增加 Locomotion restore 后 same alias 不重启测试。
- [ ] 3.5 增加 Action restore 后恢复 normalized time 测试。
- [ ] 3.6 增加 Action clear 后清理 action playback fact 测试。
- [ ] 3.7 增加 TurnBack start normalized time 保留测试。
- [ ] 3.8 增加 foot phase start override 保留测试。
- [ ] 3.9 增加 root motion policy 不被 Action/Locomotion 双组件覆盖测试。
- [ ] 3.10 增加静态测试：正式 runtime 不同时引用两个 Presenter 作为播放入口。
- [ ] 3.11 增加静态测试：正式 prefab/scene 不同时挂两个旧 Presenter。
- [ ] 3.12 增加静态测试：统一 Presenter 不调用 `CharacterController.Move`。
- [ ] 3.13 增加静态测试：统一 Presenter 不调用状态机切换 API。
- [ ] 3.14 增加静态测试：状态机 runtime 不引用统一 Presenter。

## 4. 统一 Presenter 实现
- [ ] 4.1 新增统一 FullBody base layer Animancer Presenter。
- [ ] 4.2 接入 `AnimancerComponent`。
- [ ] 4.3 接入 TransitionLibrary key 播放。
- [ ] 4.4 接入当前动画名读取。
- [ ] 4.5 接入 normalized time 读取。
- [ ] 4.6 接入 Locomotion playback progress 构建。
- [ ] 4.7 接入 Action playback progress 构建。
- [ ] 4.8 接入 Locomotion restore。
- [ ] 4.9 接入 Action restore。
- [ ] 4.10 接入 Action clear。
- [ ] 4.11 接入 Locomotion same alias early return。
- [ ] 4.12 接入 TurnBack start normalized time。
- [ ] 4.13 接入 foot phase start override。
- [ ] 4.14 接入 AnimatorRootMotionController policy。
- [ ] 4.15 保留必要诊断日志关键字。

## 5. Character Output 接入
- [ ] 5.1 将 `PlayerFullBodyActionController` 的 action presenter 引用迁为统一 Presenter 接口。
- [ ] 5.2 将 action animation `Present` 调用改为统一播放请求提交。
- [ ] 5.3 将 action animation `Clear` 调用改为统一 Presenter clear action domain。
- [ ] 5.4 将 action playback progress 写黑板改为读取统一播放快照。
- [ ] 5.5 保持 Character output applier 只在 PresentationBridge 或等价输出阶段提交动画。
- [ ] 5.6 保持 Action owner 和 Locomotion owner 互斥提交。
- [ ] 5.7 保持状态机不直接调用 Presenter。

## 6. Locomotion Adapter 接入
- [ ] 6.1 将 `PlayerLocomotionController` 的 `BasicLocomotionAnimancerPresenter` 字段迁为统一 Presenter 接口或统一 Presenter 引用。
- [ ] 6.2 将 `PresentLocomotionAnimation` 改为提交统一播放请求。
- [ ] 6.3 将 `CurrentAnimationPlaybackProgress` 改为读取统一播放快照。
- [ ] 6.4 将 `CurrentAnimationName` 改为读取统一播放快照。
- [ ] 6.5 将 Locomotion restore 入口迁到统一 Presenter。
- [ ] 6.6 更新 `LocomotionRuntimeReferenceResolver` 的自动发现逻辑。
- [ ] 6.7 自动发现限制在当前角色层级。
- [ ] 6.8 自动发现不创建配置、不使用 Resources、不跨角色查找。

## 7. 旧 Presenter 退役
- [ ] 7.1 迁移 `BasicLocomotionAnimancerPresenter` 的测试覆盖到统一 Presenter。
- [ ] 7.2 迁移 `ActionAnimationAnimancerPresenter` 的测试覆盖到统一 Presenter。
- [ ] 7.3 删除旧 Presenter，或将旧 Presenter 降级为非正式兼容桥。
- [ ] 7.4 如果保留兼容桥，标记 Obsolete 或迁移用途。
- [ ] 7.5 如果保留兼容桥，静态测试确认正式 prefab/scene 不挂载它们。
- [ ] 7.6 删除 FullBody controller 对 `IActionAnimationPresenter` 的正式依赖。
- [ ] 7.7 删除 Locomotion controller 对 `BasicLocomotionAnimancerPresenter` 具体类型的正式依赖。

## 8. Prefab / Scene 装配
- [ ] 8.1 更新 `可琳.prefab`，只保留一个统一 Animancer Presenter。
- [ ] 8.2 更新 `可琳_Humanoid.prefab`，只保留一个统一 Animancer Presenter。
- [ ] 8.3 更新 Sandbox scene override。
- [ ] 8.4 确认 `AnimancerComponent` 仍在同一视觉根。
- [ ] 8.5 确认 FullBody controller 引用统一 Presenter。
- [ ] 8.6 确认 Locomotion controller 不再引用旧 Presenter 具体类型。
- [ ] 8.7 确认动作动画 key `Action.Dodge.Directional` 和 `Action.Dodge.Backstep` 仍可播放。
- [ ] 8.8 确认基础移动 alias `Idle / WalkStart / WalkLoop / WalkEnd / RunStart / RunLoop / RunEnd` 仍可播放。

## 9. 自动验证
- [ ] 9.1 运行统一 Presenter 相关 EditMode 测试。
- [ ] 9.2 运行 `Tests.Editor.UnifiedCharacterStateMachineTests`。
- [ ] 9.3 运行 `Tests.Editor.Simulation.FullBodyRollbackReplayTests`。
- [ ] 9.4 运行 `Tests.Editor.Simulation.LocalRollbackSynctestFoundationTests`。
- [ ] 9.5 运行 `Tests.Editor.LocomotionFootPhaseMatchingTests`。
- [ ] 9.6 运行 Action animation profile 相关测试。
- [ ] 9.7 运行基础移动动画相关测试。
- [ ] 9.8 运行静态边界测试。
- [ ] 9.9 运行 `dotnet build .\Assembly-CSharp.csproj --no-restore /m:1 /nr:false /p:UseSharedCompilation=false /clp:ErrorsOnly`。
- [ ] 9.10 运行 `dotnet build .\Assembly-CSharp-Editor.csproj --no-restore /m:1 /nr:false /p:UseSharedCompilation=false /clp:ErrorsOnly`。
- [ ] 9.11 运行 `openspec validate refactor-unified-animancer-presenter --strict --no-interactive`。
- [ ] 9.12 不运行 Unity batchmode。

## 10. 收尾
- [ ] 10.1 更新相关 agent 文档中两个 Presenter 并存的描述。
- [ ] 10.2 确认没有新增 fallback 配置。
- [ ] 10.3 确认没有新增第二套动画播放路径。
- [ ] 10.4 确认没有绕过 Character frame pipeline / output applier。
- [ ] 10.5 确认没有删除用户未要求删除的诊断日志。
- [ ] 10.6 全部任务真实完成后再将 checklist 标为 `- [x]`。
