## 1. 事实模型
- [x] 1.1 增加动作动画播放进度纯数据事实，表达 action key、normalized time、是否有效和是否结束。
- [x] 1.2 保持该事实不包含 Animancer runtime、AnimationClip、TransitionAsset、Animator 或场景对象引用。
- [x] 1.3 更新 `CharacterRuntimeAnimationFacts` 默认值和构造入口。
- [x] 1.4 更新本地回滚快照比较或相关测试夹具，使新增事实可比较且默认安全。

## 2. 动画外观层接入
- [x] 2.1 在 `ActionAnimationAnimancerPresenter` 暴露只读动作播放进度。
- [x] 2.2 确认 Presenter 不通过 `OnEnd` 或回调调用状态机。
- [x] 2.3 在 `PlayerFullBodyActionController` 写入动作播放进度事实。
- [x] 2.4 保留现有动作动画播放和 clear 日志，不删除日志。

## 3. 状态机条件
- [x] 3.1 增加 `ActionCanExit` 或等价 transition condition kind。
- [x] 3.2 在 evaluator 中只读取 runtime blackboard 的动作播放事实。
- [x] 3.3 支持按当前动作 key 或当前变体绑定 key 判断匹配，避免错误消费其它 action 动画结束。
- [x] 3.4 保持 evaluator 不引用 Animancer、CharacterController、Input System 或 Camera。

## 4. Backstep 配置
- [x] 4.1 更新 `CharacterStateMachineDefinition.CreateDefault()` 中 Backstep 无输入回 Idle 的条件。
- [x] 4.2 更新 `DefaultCharacterStateMachine.asset` 中 Backstep 无输入回 Idle 的条件。
- [x] 4.3 保持 Directional Dodge 退出到 MoveLoop 的 0.35 秒行为不变。
- [x] 4.4 保持 Backstep 动作位移 duration 和 distance 不因动画长度改变。
- [x] 4.5 保持 Backstep 恢复段在重新输入移动时可提前回移动阶段。

## 5. 自动测试
- [x] 5.1 更新 `BackstepDodgeCompletionDoesNotWriteRunLatch`，拆分位移完成和状态退出断言。
- [x] 5.2 增加 Backstep 动画未结束时保持 `FullBody/Action/Dodge` 的测试。
- [x] 5.3 增加 Backstep 动画结束事实为 true 后回 `FullBody/Locomotion/Idle` 的测试。
- [x] 5.4 增加 Directional Dodge 仍按 0.35 秒进入 `MoveLoop` 的回归测试。
- [x] 5.5 增加 Backstep 恢复段收到移动输入时提前回移动阶段的测试。
- [x] 5.6 增加静态边界测试，确认状态机 runner/evaluator 不引用 Animancer runtime。
- [x] 5.7 运行定向 EditMode 测试。

## 6. 手动验证
- [ ] 6.1 打开当前演示场景，使用可琳角色。
- [ ] 6.2 不输入方向只按 Shift，确认进入 Backstep 后闪。
- [ ] 6.3 保持无输入，确认后闪蹲下到起身恢复过程不会在 0.35 秒被 Idle 打断。
- [ ] 6.4 确认恢复完成后回到 Idle。
- [ ] 6.5 后闪恢复段重新输入移动，确认可提前回到移动阶段。
- [ ] 6.6 有方向按 Shift，确认 Directional Dodge 仍快速冲刺并接回移动。
- [ ] 6.7 确认没有新增第二角色控制器、第二状态机或动画直接位移路径。

## 7. OpenSpec
- [x] 7.1 运行 `openspec validate update-backstep-dodge-exit-fact --strict --no-interactive`。
- [x] 7.2 实现完成后更新本任务清单状态。
