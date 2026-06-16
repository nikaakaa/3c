## 0. 准备和冲突检查
- [x] 0.1 阅读本变更 `proposal.md`。
- [x] 0.2 阅读本变更 `design.md`。
- [x] 0.3 阅读本变更 `tasks.md`。
- [x] 0.4 运行 `openspec list`，确认相关 active changes 没有新增冲突。
- [x] 0.5 运行 `git status --short -- 3cDemo/Client/3C_Client/Assets/Configs/3C openspec`，记录实施前已有用户改动。
- [x] 0.6 搜索 `Statemachine`、`StateMachine`、`Animacer`、`Pramater`、`DefaultCharacterStateMachine`、`DefaultDodgeActionConfig`、`DefaultDodgeInterruptPolicySet`、`DefaultRunLocomotionAnimationConfig`、`CorinFullBodyStateRequestPolicySet`、`TestTurnback`、`turnback613`、`turnInPlace` 的所有引用。

## 1. 资产目录和引用校验
- [x] 1.1 新增默认 3C 配置目录扫描测试。
- [x] 1.2 校验 `CorinCharacterConfig.asset` 位于 `Assets/Configs/3C/Character/Corin/CorinCharacterConfig.asset`。
- [x] 1.3 校验根目录 `Assets/Configs/3C/CharacterConfig.asset` 不作为第二正式入口。
- [x] 1.4 校验旧 `Assets/Configs/3C/Statemachine/` 不包含正式状态机资产。
- [x] 1.5 校验旧 `Assets/Configs/3C/Animacer/` 不作为正式 Animancer 配置入口。
- [x] 1.6 校验旧 `Pramater` 拼写不作为正式参数目录。
- [x] 1.7 校验默认配置不引用 `TestTurnback*`、`turnback*` 或 `testTurn` 这类测试命名资产。
- [x] 1.8 校验 `CharacterConfig` 内不存在 dangling 子配置引用。
- [x] 1.9 校验移动后的 `.meta` GUID 没有被重建。

## 2. 角色配置根作者入口
- [x] 2.1 对 `CharacterConfigSO` 相关符号运行 GitNexus impact analysis。
- [x] 2.2 将现有 `CharacterConfig.asset` 迁移并重命名到 `Assets/Configs/3C/Character/Corin/CorinCharacterConfig.asset`。
- [x] 2.3 扩展或确认 `CharacterConfigSO` 的正式作者入口字段分组。
- [x] 2.4 让根入口能追踪 StateMachine 配置。
- [x] 2.5 让根入口能追踪 Movement 配置。
- [x] 2.6 让根入口能追踪 Locomotion Animation 配置。
- [x] 2.7 让根入口能追踪 FullBody Action 逻辑配置。
- [x] 2.8 让根入口能追踪 FullBody Action Animation 配置。
- [x] 2.9 让根入口能追踪 Animancer rig variant 配置。
- [x] 2.10 让根入口能追踪 Input/InputReferences 配置。
- [x] 2.11 让根入口能追踪 Camera 配置。
- [x] 2.12 缺失正式配置时输出明确诊断并停止相关正式输出。
- [x] 2.13 增加根入口解析 EditMode 测试。
- [x] 2.14 增加缺失配置不 fallback 的 EditMode 测试。

## 3. Action 配置权威收口
- [x] 3.1 对 `DodgeActionConfigSO`、`ActionMotionResolver`、`CharacterStateOutputResolver` 相关符号运行 GitNexus impact analysis。
- [x] 3.2 确认 Dodge Directional motion 参数只从正式 Action 逻辑配置读取。
- [x] 3.3 确认 Dodge Backstep motion 参数只从正式 Action 逻辑配置读取。
- [x] 3.4 移除或停用默认状态机资产中重复的 Dodge duration/distance 权威。
- [x] 3.5 增加测试：默认 Dodge motion 参数只有一个正式来源。
- [x] 3.6 增加测试：状态机节点缺失重复 motion 数值时 Dodge 仍由 Action 配置解析。
- [x] 3.7 增加测试：Action 配置缺失时不使用代码 fallback。

## 4. Request policy 命名和归属
- [x] 4.1 对 `ActionInterruptPolicySetSO` 和 request submission arbiter 相关符号运行 GitNexus impact analysis。
- [x] 4.2 将包含 Dodge 和 TurnBack 的默认策略集合重命名为 `CorinFullBodyStateRequestPolicySet.asset`。
- [x] 4.3 将默认策略集合移动到 `Action/FullBody/RequestPolicy/` 或批准的等价目录。
- [x] 4.4 更新根入口或正式装配点引用。
- [x] 4.5 增加测试：包含 TurnBack 的策略集合不再使用 Dodge-only 命名。
- [x] 4.6 增加测试：缺失策略集合时不从旧 Dodge policy 路径 fallback。

## 5. Animation 和 Animancer 目录编排
- [x] 5.1 对 Locomotion animation config、motion profile config、Animancer binding 相关符号运行 GitNexus impact analysis。
- [x] 5.2 将 FootPhase profile 放入正式 `Animation/Corin/Locomotion/FootPhase/` 或批准的等价目录。
- [x] 5.3 将 Locomotion motion profile 放入正式 `Animation/Corin/Locomotion/MotionProfiles/` 或批准的等价目录。
- [x] 5.4 选定一个正式 TurnBack motion profile。
- [x] 5.5 将 `DefaultRunLocomotionAnimationConfig.asset` 重命名为 `CorinLocomotionAnimationConfig.asset`。
- [x] 5.6 更新 `CorinLocomotionAnimationConfig.asset` 引用正式 TurnBack motion profile。
- [x] 5.7 将动作动画相关资产放入 `Animation/Corin/FullBody/Action/` 或批准的等价目录。
- [x] 5.8 将 `Animacer` 目录迁到 `Animation/Corin/Animancer/` 或批准的等价目录。
- [x] 5.9 将 Generic transition library 迁到 `Animation/Corin/Animancer/RigVariants/Generic/CorinGenericAnimancerTransitionLibrary.asset`。
- [x] 5.10 将 `Pramater` 目录迁到 `Parameters`。
- [x] 5.11 若保留 Humanoid transition library，将其放入参考、测试或未来迁移目录，不作为正式配置闭环解析。
- [x] 5.12 增加测试：正式配置不引用测试命名 motion profile。
- [x] 5.13 增加测试：正式 Animancer 目录拼写正确。
- [x] 5.14 增加测试：Generic 作为 Corin 唯一正式 rig variant 可解析。
- [x] 5.15 增加测试：Humanoid 不作为本次正式必需 rig variant 被根配置要求。

## 6. 状态机资产旧字段收口
- [x] 6.1 对 `CharacterStateMachineDefinitionSO` 和 validator 相关符号运行 GitNexus impact analysis。
- [x] 6.2 校验默认状态机节点能力只由 modules 或批准的等价模型表达。
- [x] 6.3 将 `DefaultCharacterStateMachine.asset` 重命名为 `CorinFullBodyStateMachine.asset`。
- [x] 6.4 清理旧 `output` 与 modules 并行决定同一输出的默认资产数据。
- [x] 6.5 清理 TurnBack alias 重复填写。
- [x] 6.6 增加测试：默认状态机资产没有旧万能字段和新 modules 双权威。
- [x] 6.7 增加测试：TurnBack alias、timeline binding 和 motion source id 不要求设计者重复填写。
- [x] 6.8 增加测试：Corin 正式状态机资产不使用 `Default` 命名。

## 7. 构建和自动验证
- [x] 7.1 运行相关 EditMode 测试。
- [x] 7.2 运行 `dotnet build .\Assembly-CSharp.csproj --no-restore /m:1 /nr:false /p:UseSharedCompilation=false /clp:ErrorsOnly`。
- [x] 7.3 运行 `dotnet build .\Assembly-CSharp-Editor.csproj --no-restore /m:1 /nr:false /p:UseSharedCompilation=false /clp:ErrorsOnly`。
- [x] 7.4 运行 `openspec validate refactor-fullbody-config-authoring-layout --strict --no-interactive`。
- [x] 7.5 运行 GitNexus detect changes，确认影响范围只包含预期配置、校验、解析和测试。
