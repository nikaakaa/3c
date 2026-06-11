## 1. 现状确认
> 已被 `refactor-unified-character-state-machine` 部分接管：依赖 Dodge runtime、FullBody Action Set、独立 Action Animation Profile 的运行时和配置任务需要被统一状态机替换。

- [x] 1.1 确认 Shift 当前通过 `UnityInputSystemLocomotionInputSource.RunActionName` 作为 held Run 输入。
- [x] 1.2 确认 `InputRequestKind.Dodge`、`ActionRequestType.Dodge` 和 `Action.Dodge` ID 当前存在。
- [x] 1.3 确认现有 `ActionInterruptArbiter` 可接受从 `Action.None` 或基础 action state 进入 `Action.Dodge` 的策略。
- [x] 1.4 确认 `ActionRuntimeStateTracker` 仍只负责事实记录，不实现自动退出或动画播放。
- [x] 1.5 确认 `PlayerLocomotionController` 和 `BasicLocomotionPipeline` 不新增第二移动入口。

## 2. Shift 输入迁移
- [x] 2.1 将 Shift pressed 映射为现有 FullBody/Dodge 输入请求。
- [x] 2.2 停止让 Shift held 直接决定基础移动 Run 档位。
- [x] 2.3 保持 Move 和 Look 仍由基础 locomotion input source 读取。
- [x] 2.4 确认 held Shift 不会每帧重复触发动作请求。
- [x] 2.5 测试 Shift pressed 能生成可消费动作请求。
- [x] 2.6 测试 Shift held 不会重复生成请求。

## 3. FullBody 纯数据模型
- [x] 3.1 定义 FullBody 变体枚举：`Directional`、`Backstep`。
- [x] 3.2 定义动作请求数据，包含变体、世界方向、来源 step/tick、优先级和目标 state id。
- [x] 3.3 定义动作配置数据，包含 Directional/Backstep 各自的时长、距离、优先级、抗性、是否允许旋转到方向。
- [x] 3.4 保持默认目标 state id 为 `Action.Dodge`，不新增 `Action.Sprint`。
- [x] 3.5 为非法方向、负时长、负距离、负优先级、负抗性增加防御规则。
- [x] 3.6 为缺配置时提供保守 fallback，不把手感参数只能写死在代码中。

## 4. 输入与方向解析
- [x] 4.1 从 `InputRequestBuffer` 查询 Shift 对应的动作请求，不让输入层直接触发动作结果。
- [x] 4.2 读取当前移动意图事实，有移动意图时生成 `Directional`。
- [x] 4.3 无移动意图时生成 `Backstep`。
- [x] 4.4 `Directional` 使用相机相对世界移动方向。
- [x] 4.5 `Backstep` 使用角色 facing 反方向或等价 facing provider 输出。
- [x] 4.6 `Directional` 开始时立即把角色朝向转到冲刺方向。
- [x] 4.7 `Backstep` 开始时保持当前角色 facing。
- [x] 4.8 输入方向解析不得直接读取 `Camera.main` 或具体 Cinemachine 实例。

## 5. 仲裁接入
- [x] 5.1 将动作请求转换为 `ActionInterruptRequest`。
- [x] 5.2 使用现有 `ActionInterruptArbiter` 判断能否进入 `Action.Dodge`。
- [x] 5.3 accepted 时消费对应输入请求。
- [x] 5.4 rejected 时保留未过期输入请求。
- [x] 5.5 accepted 时调用 `ActionRuntimeStateTracker.ApplyDecision`。
- [x] 5.6 配置 `Action.None -> Action.Dodge` 或等价空状态进入动作的默认策略。
- [x] 5.7 保持 `MoveStop -> MoveStart` 仍由 Locomotion 状态图处理。

## 6. 动作生命周期
- [x] 6.1 定义动作 active 判断。
- [x] 6.2 动作 active 时推进 elapsed time。
- [x] 6.3 动作 active 期间压制基础移动位移执行。
- [x] 6.4 动作 active 期间压制基础移动动画表现。
- [x] 6.5 达到动作 duration 后退出到 `Action.None` 或等价空 action state。
- [x] 6.6 动作 active 期间不得让 tracker 自己自动退出，退出由 action runner 或等价 action driver 负责。
- [x] 6.7 动作结束后基础移动仍能继续读取 WASD 输入。
- [x] 6.8 动作 active 期间相机 Look 继续响应。
- [x] 6.9 action runtime 不直接读取或控制 Cinemachine 具体实例。

## 6A. FullBody 主行为域收束
- [x] 6A.1 复查当前 `PlayerDodgeActionController`、`DodgeActionRuntime`、`PlayerLocomotionController` 和动画 Presenter 的所有权边界。
- [x] 6A.2 明确 `BasicLocomotionStateMachine` 是 FullBody 行为域下的 Locomotion 局部子图，而不是和 Dodge 平级争夺 base layer 的第二状态权威。
- [x] 6A.3 明确 Dodge 是 FullBody 主树 `Action` 分支下的叶子行为模块，不能作为独立于 FullBody 主层的第二套 WASD/Action 状态机运行。
- [x] 6A.4 若当前薄层 runner/controller 已形成独立路径，重命名、合并或注册到统一 FullBody 行为域后再标记完成。
- [x] 6A.5 FullBody 主层 active Dodge 时，基础 Locomotion 子图不得同时输出平面位移或 base layer 动画命令。
- [x] 6A.6 Dodge 结束后的 Run latch、Idle 回退和再次 Shift 触发必须从同一个 FullBody 行为域出口写入事实。
- [x] 6A.7 确认本变更不接入 UpperBody、Facial、IK 或 Additive 并行表现层；后续若接入必须另开 OpenSpec。
- [x] 6A.8 静态复查没有复制 BBB 控制器，也没有新增绕过现有输入、Action 仲裁、motion executor 的角色运动入口。

## 7. Directional 后 Run 档位
- [x] 7.1 定义 Directional 完成后的 Run latch 或等价移动事实。
- [x] 7.2 `Directional` 动作完成后设置 Run latch。
- [x] 7.3 `Backstep` 动作完成后不设置 Run latch。
- [x] 7.4 Run latch active 时基础移动使用 `BasicMovementGait.Run`。
- [x] 7.5 Run latch active 不依赖 Shift held。
- [x] 7.6 角色完全停下并回到 Idle 后重置 Run latch 为 Walk。
- [x] 7.7 测试 Directional 完成后不用按住 Shift 仍进入 Run 档位。
- [x] 7.8 测试 Backstep 完成后不强制进入 Run 档位。
- [x] 7.9 测试回到 Idle 后再次移动默认 Walk。

## 7A. 动作结束后再次触发
- [x] 7A.1 修正动作退出到 `Action.None` 时不得把 current step 写入 tracker resistance。
- [x] 7A.2 确认动作结束后新按 Shift 会生成新的 Dodge 请求。
- [x] 7A.3 确认动作结束后新按 Shift 能重新参与 Action 仲裁。
- [x] 7A.4 确认动作结束后新按 Shift 在仲裁接受时能再次进入 `Action.Dodge`。
- [x] 7A.5 测试连续两次 Directional：第一次结束后重新按 Shift 能再次触发。
- [x] 7A.6 测试连续两次 Backstep：第一次结束后重新按 Shift 能再次触发。
- [x] 7A.7 测试第二次触发不是依赖 held Shift，而是新的 pressed 请求。

## 8. 动作动画 Profile
- [x] 8.1 定义稳定 action animation key 数据。
- [x] 8.2 定义 `Action.Dodge.Directional` key。
- [x] 8.3 定义 `Action.Dodge.Backstep` key。
- [x] 8.4 定义动作动画 Profile 配置资产。
- [x] 8.5 Profile entry 支持 key、clip/transition 引用、fade 参数和可选调试名。
- [x] 8.6 Profile 校验空 key、重复 key、缺失动画引用。
- [x] 8.7 动作根据变体解析动画 key，不直接引用具体可琳 clip。
- [x] 8.8 动作动画 Profile 不替代基础移动 Walk/Run alias 配置。

## 8A. FullBody Action 装配闭环
- [ ] 8A.1 定义 `Action.Dodge` 的动作逻辑入口或等价配置入口。
- [ ] 8A.2 动作逻辑入口能定位 Directional/Backstep 的运动参数配置。
- [ ] 8A.3 动作逻辑入口能定位 `Action.Dodge` 的打断策略配置。
- [ ] 8A.4 动作动画绑定入口能定位 Directional/Backstep 的动画表现 Profile 或等价 per-character override。
- [ ] 8A.5 动作逻辑入口校验缺失运动参数或打断策略，动作动画绑定入口校验缺失动画 Profile 或必要动画 key。
- [ ] 8A.6 保留 `DodgeActionConfigSO`、`ActionInterruptPolicySetSO`、`ActionAnimationProfileSO` 等子配置的复用能力，但它们必须通过动作逻辑入口、动作动画绑定入口和 FullBody 装配点形成闭环。
- [ ] 8A.7 明确基础 Locomotion 状态图、Walk/Run alias 和 TransitionLibrary 仍属于 Locomotion 配置入口，不并入 Dodge 动作逻辑入口。
- [ ] 8A.8 测试 FullBody 装配闭环能发现 Dodge 逻辑配置和动画绑定配置是否完整。

## 9. 动作动画表现层
- [x] 9.1 新增或扩展动作动画 Presenter，只消费动作动画命令。
- [x] 9.2 Presenter 根据 action animation key 查询 Profile。
- [x] 9.3 Presenter 播放 Directional 动画。
- [x] 9.4 Presenter 播放 Backstep 动画。
- [x] 9.5 Presenter 暴露只读播放进度和当前 key。
- [x] 9.6 Presenter 不调用状态机切换 API。
- [x] 9.7 Presenter 不调用 `CharacterController.Move`。
- [x] 9.8 Presenter 不写入角色 Transform。

## 10. 动作运动输出
- [x] 10.1 定义动作运动命令或动作运动 facts。
- [x] 10.2 根据动作配置时长和距离采样本帧位移。
- [x] 10.3 位移方向使用动作请求保存的世界方向。
- [x] 10.4 动作位移通过统一运动出口或等价 motion executor 执行。
- [x] 10.5 动作 active 期间不得由动画 Root Motion 直接移动角色。
- [x] 10.6 若必须使用完整 Root Motion，停止实现并新增 OpenSpec。

## 11. EditMode 测试
- [x] 11.1 测试 Shift pressed 生成动作请求。
- [x] 11.2 测试 held Shift 不重复生成动作请求。
- [x] 11.3 测试有移动输入时变体为 `Directional`。
- [x] 11.4 测试无移动输入时变体为 `Backstep`。
- [x] 11.5 测试 `Directional` 使用输入世界方向。
- [x] 11.6 测试 `Backstep` 使用 facing 反方向。
- [x] 11.7 测试 `Directional` 开始时转向冲刺方向。
- [x] 11.8 测试 `Backstep` 开始时保持当前 facing。
- [x] 11.9 测试配置负值安全处理。
- [x] 11.10 测试仲裁 accepted 后消费输入请求。
- [x] 11.11 测试仲裁 rejected 后保留未过期输入请求。
- [x] 11.12 测试 accepted 后 tracker 进入 `Action.Dodge`。
- [x] 11.13 测试动作 duration 到期后退出。
- [x] 11.14 测试 Directional 完成后进入 Run 档位。
- [x] 11.15 测试 Backstep 完成后不进入 Run 档位。
- [x] 11.16 测试回 Idle 后 Run latch 重置。
- [x] 11.17 测试动作 active 期间相机 Look 输入仍被转交给项目侧相机入口。
- [x] 11.18 测试 Profile 能解析两个动作动画 key。
- [x] 11.19 测试 Profile 校验空 key、重复 key、缺失动画引用。
- [x] 11.20 静态测试动作动画 Profile 不引用 BBB 运行时类型。
- [x] 11.21 静态测试动作表现层不调用 `CharacterController.Move` 或写 Transform。
- [x] 11.22 静态测试 action runtime 不直接读取或控制 Cinemachine 具体实例。
- [x] 11.23 回归测试基础移动状态图 `MoveStop -> MoveStart` 不依赖该动作。
- [x] 11.24 测试 Directional 结束后新按 Shift 能再次进入 `Action.Dodge`。
- [x] 11.25 测试 Backstep 结束后新按 Shift 能再次进入 `Action.Dodge`。
- [x] 11.26 测试第二次触发依赖新的 pressed 请求，不依赖 held Shift。
- [x] 11.27 测试动作退出到 `Action.None` 后 tracker resistance 不被 current step 污染。
- [x] 11.28 静态测试 Dodge 实现没有新增独立于 FullBody 主层的第二套 base layer 状态权威。
- [x] 11.29 静态测试 Locomotion 子图和 Dodge action/module 通过统一运动出口提交位移。
- [x] 11.30 测试 FullBody Dodge active 期间基础 Locomotion 不同时输出平面位移。
- [x] 11.31 测试动作动画 Presenter 只消费命令和输出播放进度，不切换 FullBody 状态。
- [ ] 11.32 测试 FullBody Action 逻辑入口能定位运动参数和打断策略，动作动画绑定入口能定位动画 Profile。
- [ ] 11.33 测试动作逻辑入口或动作动画绑定入口缺失必要子配置时校验失败。
- [ ] 11.34 测试动作动画 Profile 可作为动画绑定入口的子配置或引用，不要求游离配置。

## 12. 手动验证
- [x] 12.1 在 Unity Editor 中把 Shift 绑定为该 FullBody 动作输入。
- [x] 12.2 在 Unity Editor 中创建或配置动作动画 Profile。
- [x] 12.3 绑定 Directional 动画。
- [x] 12.4 绑定 Backstep 动画。
- [x] 12.5 Play Mode 中按住方向键再按 Shift，确认角色向输入方向冲刺。
- [x] 12.6 确认 Directional 开始时角色朝向立即对齐冲刺方向。
- [x] 12.7 Shift 松开后继续按方向键，确认角色保持 Run 档位，不需要按住 Shift。
- [x] 12.8 Play Mode 中不按方向键只按 Shift，确认角色保持朝向并后闪。
- [x] 12.9 后闪后再按普通方向键，确认不强制进入 Run 档位。
- [x] 12.10 动作 active 期间移动鼠标或右摇杆，确认相机 Look 继续响应。
- [x] 12.11 松开移动直到回 Idle 后再次移动，确认默认回 Walk。
- [x] 12.12 替换其中一个动作动画 clip，确认无需修改动作逻辑代码。
- [x] 12.13 MoveStop 期间重新输入移动，确认仍立即回到起步/移动，不被该动作方案破坏。
- [ ] 12.14 Directional 或 Backstep 动作结束后松开并再次按 Shift，确认可以重新触发 Directional 或 Backstep。

## 13. 文档和边界复查
- [x] 13.1 更新相关路线文档中该 FullBody 动作所属阶段。
- [x] 13.2 复查没有新增未审批独立角色控制器或第二套 base layer 状态权威。
- [x] 13.3 复查没有新增独立 `Action.Sprint` 或第二套 Sprint 变更。
- [x] 13.4 复查没有复制 BBB 运行时代码。
- [x] 13.5 复查没有删除现有 log。
- [x] 13.6 记录用户手动验证步骤和结果。
- [x] 13.7 记录 BBB 参考口径：FullBody 主状态机包含 Locomotion 和 Dodge/Roll 等全身动作；UpperBody 等并行表现层不是当前变更范围。
- [x] 13.8 记录工业口径：输入缓冲、Action/Ability 仲裁、FullBody 主层、统一运动出口、动画表现层单向协作。
- [x] 13.9 更新 spec，要求模块化实现必须归属 FullBody 行为域，不能形成分裂路径。
- [x] 13.10 更新 spec，明确工业配置口径是“职责分层、入口聚合”，不是多个游离资产手工同步。

## 14. 验证记录
- [x] 14.1 `openspec validate add-dodge-action-profile --strict --no-interactive` 通过。
- [x] 14.2 修复动作结束后再次 Shift 触发后，Unity MCP 定向 EditMode 测试通过。
- [x] 14.3 静态检查确认没有 `Action.Sprint`、没有 `BBBNexus` 动作依赖、动作动画 Presenter 不调用 `CharacterController.Move` 或写 Transform。
- [x] 14.4 静态检查确认 `CharactorInput.inputactions` 中 Shift 只绑定到 `Dodge`，主角色 Prefab 和 `CameraTest` 场景的 `runActionName` 已清空。
- [ ] 14.5 需用户在 Unity Editor Play Mode 中执行手动验证：按住方向键再按 Shift 确认方向冲刺，松开 Shift 后继续按方向键确认保持 Run；松开移动回 Idle 后再次移动确认默认 Walk；Directional 或 Backstep 动作结束后松开并再次按 Shift 确认可以重新触发；无方向按 Shift 确认后闪且不强制 Run；移动鼠标/右摇杆确认动作期间相机 Look 继续响应；替换 `CorinDodgeActionAnimationProfile` 任一 clip 确认无需改动作逻辑。
- [x] 14.6 FullBody 主行为域收束完成后，重新运行 `openspec validate add-dodge-action-profile --strict --no-interactive`。
- [x] 14.7 FullBody 主行为域收束完成后，重新运行相关 EditMode 测试并记录结果。

