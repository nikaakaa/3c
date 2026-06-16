## 1. 现状确认
- [x] 1.1 盘点 `CharacterStateNodeDefinition` 当前字段。
- [x] 1.2 盘点默认状态机资产中每个节点实际使用的字段。
- [x] 1.3 盘点 `Owner.IsAction`、`IsLocomotion`、`HasTag(Action)`、`HasTag(Locomotion)` 的运行时使用点。
- [x] 1.4 盘点 action animation、locomotion animation、TurnBack alias 的配置来源。
- [x] 1.5 盘点 Dodge duration / distance 的配置来源。
- [x] 1.6 记录哪些字段是有效能力，哪些是万能节点噪音。

## 2. Characterization 测试
- [x] 2.1 覆盖默认状态机节点 ID 和父子路径。
- [x] 2.2 覆盖 Idle/MoveStart/MoveLoop/MoveStop 当前输出。
- [x] 2.3 覆盖 TurnBack 当前 alias、timeline 和 motion policy 行为。
- [x] 2.4 覆盖 Dodge Directional 当前位移、动画 key 和输入消费。
- [x] 2.5 覆盖 Dodge Backstep 当前位移、动画 key 和 run latch 行为。
- [x] 2.6 覆盖 Dodge->Dodge 连发重播动作动画。
- [x] 2.7 覆盖 rollback replay 当前状态快照和动画 facts。
- [x] 2.8 覆盖默认资产不依赖 UnityHFSM runtime。

## 3. 模块模型设计
- [x] 3.1 设计节点核心字段。
- [x] 3.2 设计模块标识方式。
- [x] 3.3 设计模块 payload 序列化方式。
- [x] 3.4 设计 LocomotionPhase 模块。
- [x] 3.5 设计 InputDrivenMotion 模块。
- [x] 3.6 设计 ConfiguredActionMotion 模块。
- [x] 3.7 设计 ActionAnimation 模块。
- [x] 3.8 设计 LocomotionAnimationAlias 模块。
- [x] 3.9 设计 TurnBackMotionPolicy 模块。
- [x] 3.10 设计 InputConsume 模块。
- [x] 3.11 设计 RunLatch 模块。
- [x] 3.12 设计 TimelineWindow 模块。
- [x] 3.13 确认 gait 不进入模块配置。
- [x] 3.14 确认模块模型不引用 Unity runtime 对象。

## 4. Validator 设计
- [x] 4.1 校验分组节点不得携带输出模块。
- [x] 4.2 校验普通 Locomotion 节点不得携带 ActionAnimation 模块。
- [x] 4.3 校验 Dodge 节点具备动作请求模块。
- [x] 4.4 校验 Dodge 节点具备动作位移模块。
- [x] 4.5 校验 Dodge 节点具备动作动画模块。
- [x] 4.6 校验 Dodge 变体 animation key 非空。
- [x] 4.7 校验 TurnBack alias 只有一个正式来源。
- [x] 4.8 校验同一节点不存在重复 motion authority。
- [x] 4.9 校验同一节点不存在重复 animation authority。
- [x] 4.10 校验旧万能字段不再被运行时读取。

## 5. 资产迁移规划
- [x] 5.1 设计旧节点字段到模块集合的映射表。
- [x] 5.2 设计默认资产迁移步骤。
- [x] 5.3 设计迁移后资产校验步骤。
- [x] 5.4 设计旧 `output` 字段退役步骤。
- [x] 5.5 设计旧 `animation` 字段退役步骤。
- [x] 5.6 设计旧 `variants` 字段迁移到模块 payload 的步骤。
- [x] 5.7 明确不提供 fallback 配置。

## 6. Solver 接入规划
- [x] 6.1 让 runner 继续只维护 active state、state time、variant、pending transition。
- [x] 6.2 让 output resolver 读取模块集合。
- [x] 6.3 让 output resolver 产出 motion output channel。
- [x] 6.4 让 output resolver 产出 animation output channel。
- [x] 6.5 让 output resolver 产出 input output channel。
- [x] 6.6 让 output resolver 产出 latch output channel。
- [x] 6.7 让 timeline sampler 读取模块声明的 playback fact source。
- [x] 6.8 让 lifecycle payload 判断从 tag/owner 改为模块查询。
- [x] 6.9 保留现有 frame 外壳直到调用方迁移完成。

## 7. Pipeline 接入规划
- [x] 7.1 让 FullBody pipeline 消费 motion output channel。
- [x] 7.2 让 FullBody pipeline 消费 animation output channel。
- [x] 7.3 让 FullBody pipeline 消费 input output channel。
- [x] 7.4 让 FullBody pipeline 写入 runtime facts channel。
- [x] 7.5 移除 `Owner.IsAction` 对 action animation 播放的直接控制。
- [x] 7.6 移除 `Owner.IsAction` 对基础移动压制的直接控制。
- [x] 7.7 保持 motion executor 和 animation presenter 为外围 adapter。

## 8. 静态边界验证
- [x] 8.1 测试状态机模型不引用 Animancer。
- [x] 8.2 测试状态机模型不引用 AnimationClip。
- [x] 8.3 测试状态机模型不引用 TransitionAsset。
- [x] 8.4 测试状态机 solver 不直接调用 CharacterController。
- [x] 8.5 测试状态机 solver 不直接调用 Animator 或 Animancer 播放。
- [x] 8.6 测试状态机 solver 不读取 InputAction。
- [x] 8.7 测试 output resolver 不使用 `Owner.IsAction` 作为主分支。
- [x] 8.8 测试 timeline sampler 不使用 `HasTag(Action/Locomotion)` 选择播放事实。

## 9. 行为验证
- [x] 9.1 运行统一状态机 EditMode 测试。
- [x] 9.2 运行 FullBody pipeline EditMode 测试。
- [x] 9.3 运行 Locomotion pipeline EditMode 测试。
- [x] 9.4 运行动作动画模块 / 等价表现入口 EditMode 测试。
- [x] 9.5 运行 rollback replay EditMode 测试。
- [x] 9.6 运行模块 validator 测试。
- [x] 9.7 运行 `openspec validate refactor-character-state-node-modules --strict --no-interactive`。

## 10. 用户手动验证
- [x] 10.1 记录 Inspector 查看分组节点只显示关系字段的步骤。
- [x] 10.2 记录 Inspector 查看 MoveLoop 不显示无效 animation binding 的步骤。
- [x] 10.3 记录 Inspector 查看 TurnBack 单一 alias 来源的步骤。
- [x] 10.4 记录 Inspector 查看 Dodge 模块配置的步骤。
- [x] 10.5 记录 Sandbox WASD 验证步骤。
- [x] 10.6 记录 Sandbox TurnBack 验证步骤。
- [x] 10.7 记录 Sandbox Dodge Directional 验证步骤。
- [x] 10.8 记录 Sandbox Dodge Backstep 验证步骤。
- [x] 10.9 记录连续 Dodge 动画重播验证步骤。
- [x] 10.10 记录 Console 无 error 验证步骤。

### 手动验证记录
- Inspector 分组节点：打开 `Assets/Configs/3C/StateMachine/DefaultCharacterStateMachine.asset`，展开 `FullBody` / `Locomotion` / `Action` 分组节点，确认只显示 `stateId`、`parentStateId`、`pathSegment`、`tags`、`modules`。
- Inspector MoveLoop：展开 `FullBody/Locomotion/MoveLoop`，确认模块只表达 Locomotion phase 和 input-driven motion，不显示旧 `animation`、`variants` 或 Dodge action movement 字段。
- Inspector TurnBack：展开 `FullBody/Locomotion/TurnBack`，确认 alias 来源在 `LocomotionAnimationAlias` / `TurnBackMotionPolicy` 模块中一致为 `Locomotion.Turn.Back`。
- Inspector Dodge：展开 `FullBody/Action/Dodge`，确认具备 `InputConsume`、`ConfiguredActionMotion`、`ActionAnimation`、`RunLatch` 等模块，并能看到 Directional / Backstep 的稳定 animation key。
- Sandbox WASD：打开 Sandbox 场景进入 Play Mode，按 WASD，确认 Idle、MoveStart、MoveLoop、MoveStop 路径和基础移动动画恢复正常。
- Sandbox TurnBack：RunLoop 中按反向输入触发 TurnBack，确认进入 `FullBody/Locomotion/TurnBack`，使用 `Locomotion.Turn.Back` alias，完成后回到 MoveLoop 或 Idle。
- Sandbox Dodge Directional：移动输入非零时按 Dodge，确认进入 `FullBody/Action/Dodge` Directional，动作位移、输入消费、动作动画 key 为 `Action.Dodge.Directional`。
- Sandbox Dodge Backstep：无移动输入时按 Dodge，确认进入 Backstep，动作动画 key 为 `Action.Dodge.Backstep`，完成后回到 Idle。
- Sandbox 连续 Dodge：Dodge duration 满足后再次按 Dodge，确认同一动作 key 会重新发出播放请求，动画重播且没有第二播放路径。
- Console：完成上述验证后确认 Console 没有 error。
