# BTSMTL 重构实施基线

记录时间：2026-09-05

本文保留实施起点的历史身份和当时的观测，正文中的“当前”均指采集时点，不代表最新代码、Editor、产物或验证状态。当前逐项进度、后续提交和剩余条件统一维护在[任务进度](tasks.md)，不覆盖本文件中的旧基线证据。

## 工作区身份

- 仓库：`D:/Unity_Project_1/3C`
- 分支：`main`
- HEAD：`3bf66c4ead5272367e35a6ec0484c7c5d172dcf8`
- Unity 项目：`D:/Unity_Project_1/3C/3cDemo/Client/3C_Client`
- 目标 Unity 实例：`3C_Client@e852139597e42532`，项目路径已由 MCP 资源核对为上述路径。
- 另一个同名实例 `3C_Client@1e41b3a3e2ded45f` 未使用。

工作区已有大量并行改动，保护范围包括 Pose、Foot Placement、IK、Camera、Rendering、Performance、输入回放、项目设置、Package 锁文件、Magica Cloth、其它 OpenSpec change 以及未跟踪的诊断与渲染证据。本 change 不使用 `git reset`、不恢复这些改动、不执行全量暂存。

目标实例首次检查时处于非 Play、非编译、`GameplayLab` 场景；基线回放请求进入 `PreparingReplay` 后，Unity 在切换运行状态期间暂时从 MCP 实例列表消失。没有重复提交请求、停止进程或重启 Editor。当前回放是否完成待实例恢复后用同一 `trace_id` 查询。

## 当前正式资产与产物

### Corin Character Definition

- Definition：`3cDemo/Client/3C_Client/Assets/Configs/Character/Corin/Pipeline/Definition/CorinCharacterPipelineDefinition.asset`
- Definition GUID：`c7a7c1e3f7e64d81b5a04a90cbeb8d4e`
- 当前 RootTree：`3cDemo/Client/3C_Client/Assets/Configs/Character/Corin/Pipeline/Graphs/CorinPlayableRootTree.asset`
- RootTree GUID：`ea4ccf43cafa4fe42aff8dfc08208d52`
- RootTree authoring identity：`79647291-0c69-4e9e-9276-96a93c3647e7`
- 当前 Definition 仍引用 RootTree、InputProfile、GameplayEffectProfile、BodyMotionProfile、AnimationPresentationProfile、两份 ActionProfile 和两份 BehaviorProfile；Equipment capability 当前关闭，没有有效 Equipment root。

### 当前 Float32 组合

- Program wrapper：`3cDemo/Client/3C_Client/Assets/Configs/Character/Corin/Pipeline/Definition/Generated/CorinCharacterPipelineDefinition.SimulationProgram.asset`
- Program GUID：`5740a6cfbfb0fe542ad6a6cb66fe1a80`
- Numeric Profile：`float32-ieee754`
- Target ABI：`8`
- SourceRevision：`68c5cded5b49c345693f3c20317373fee6883a6fd22a3969b2d2c0abdf53a759`
- SemanticHash：`75f14b0f4647e1b81ee1957955e42e3cd0c1961ba89862048c66e2c05c3fd745`
- ProgramHash：`2004947279ef49aff42ef81655e8ab85b6093f34c9ec90987e5e34672ba11abd`
- LayoutHash：`e393a861bb22ac4d3132d7f368ca687835f36b8c413e7fa739275ea296e93f71`
- Projection：`3cDemo/Client/3C_Client/Assets/Configs/Character/Corin/Pipeline/Definition/Generated/CorinCharacterPipelineDefinition.PresentationProjection.asset`
- Projection GUID：`f365735adcfd49c4e96070df6bcd3bc4`
- ProjectionRevision：`3327a85333515b3aca74f33565552365def1d1544c1ff6fe4bb6d0f3fe6f8844`
- Pose Plan：`character-presentation-pose-plan/v24`，Runtime ABI `character-presentation-pose-runtime/v27`

### 当前 Fixed Gameplay Lab 组合

- Variant：`3cDemo/Client/3C_Client/Assets/Configs/Simulation/GameplayLab/Variants/GameplayLabLocalFixedVariant.asset`
- VariantId：`gameplay-lab.local-fixed-q32.32`
- Composition：`3cDemo/Client/3C_Client/Assets/Configs/Simulation/GameplayLab/Compositions/CorinGameplayLabFixedComposition.asset`
- Fixed Program wrapper：`3cDemo/Client/3C_Client/Assets/Configs/Simulation/DeterministicRollback/Programs/CorinFixedProgram.asset`
- Fixed Program GUID：`91063668fd0eaa84d9b7d688aadbc90a`
- Numeric Profile：`fixed-q32.32`
- Target ABI：`7`
- SourceRevision：`c851c73cbe47678620e58a8300fdcb20a475da5f1fbf57d29c8f9a167a1d7d63`
- SemanticHash：`0e13d3bceced825d364bfda43a0df204e4e349f15db12c6e2357510b93e083a1`
- ProgramHash：`dfe32097138bc643ec1e7f4bb562346eee9478f18c389710f42fcdb6f8196345`
- LayoutHash：`40139ba56271af586179ca32574c8c8c938bd3010a69c9c1465fa258b0c032dc`
- World：`corin-gameplay-lab-world-v1`，Solver：`thirdperson.simulation.solver.deterministic-kcc`
- 场景：`3cDemo/Client/3C_Client/Assets/Scenes/GameplayLab/GameplayLab.unity`

## 既有输入与 Proof

- 当前匹配 Fixed ProgramHash 的 Record：`3cDemo/Client/3C_Client/Diagnostics/CharacterInputTraces/20260904-153738-150-f169da25c67742aaafa0e9860ae4a230.json`
  - trace_id：`f169da25c67742aaafa0e9860ae4a230`
  - schema：`character-fixed-input-trace/3`
  - actor：`gameplay-lab-player`
  - TickRate：`60`
  - first tick：`1`
  - frame count：`1492`
  - content hash：`794996596bf4c0fbe31abf7bbd12396a473bc81490dc6adc6468dcf41fb4509c`
  - payload 明确包含 `fixed-q32.32/dfe32097138bc643ec1e7f4bb562346eee9478f18c389710f42fcdb6f8196345`。
- 旧 Record：`3cDemo/Client/3C_Client/Diagnostics/CharacterInputTraces/20260827-183705-081-43357ff3cd384e5cba75d2c31175b116.json`
  - trace_id：`43357ff3cd384e5cba75d2c31175b116`
  - 1044 帧、60Hz、content hash `ebca979bc81ea309495ebfabff6a7ab5d3bdf368d86c42092aebb2add4286647`
  - payload 绑定旧 Fixed ProgramHash `3d7377bcf71aeffedf4e75f3aa986a2648cc84902526a904c3c1e2ec937682a5`，不作为当前基线。
- 已有 `FootPlacementReplayArchives` 中的 Proof 使用不同 SourceRevision、ProgramHash 或 ProjectionRevision；例如 `20260830-contact-height-advance/baseline-proof.json` 使用 SourceRevision `780ed7ec...`，不能冒充当前候选的基线。
- 已用正式 `character.fixed_input_trace` 对 `f169da25c67742aaafa0e9860ae4a230` 提交一次 `replay_start`。响应接受为 `PreparingReplay`，但结果尚未封口；因此当前没有宣称基线通过。

## 角色图到新职责的映射

当前 `CorinPlayableRootTree` 的业务结构是一个混合根：

- `Corin Locomotion StateMachine`（authoring id `8968c5a0-f19d-487f-94cc-01cd191fee7d`）拥有 `Idle`、`WalkStart`、`WalkLoop`、`WalkEnd`、`RunStart`、`RunLoop`、`RunEnd`、`MovingTurn` 及其 State Behavior Graph。
- `Corin Action StateMachine`（authoring id `fdfba4db-d919-460a-a2f3-eb8149c7610c`）拥有 `None`、`Attack1`、`Attack2`；内部 `Attack Combo StateMachine`（authoring id `ba00b356-4bf5-4b38-9bec-7ac934b7a25c`）继续承载 `Attack3`、`Attack4`、`Attack5`。
- 同一 RootTree 还承载 `DodgeBack`、`DodgeForward`、7 个 `ActivateActionInstanceNode`、8 个 TimelineNode、Action Window 查询、生命周期节点和 LocomotionInputMotionNode。
- 当前两份策略资产是 `CorinAttackActionProfile`（ActionId `Attack`，TargetRequirement `2`）和 `CorinDodgeActionProfile`（ActionId `Dodge`，TargetRequirement `0`）。它们不能单独区分 Attack1 至 Attack5，迁移后必须由精确 SkillDefinition 与请求规则区分。
- 新所有权：C# 控制模块拥有 Locomotion 的 State／Transition、输入消费、技能候选和装备 Route；Attack1 至 Attack5、DodgeBack、DodgeForward 各自成为 SkillDefinition 的内容闭包，继续拥有 Tree、Timeline、局部 StateMachine、窗口与有限 Action producer；旧 RootTree、外层 `None/Attack/Dodge` 状态机、角色级激活节点和旧装备根在对应迁移单元中删除。
- Presentation PoseStateMachine 只继续消费 committed movement fact；不从 Gameplay StateMachine 复制 Idle/Walk/Run 等状态。

## 回放比较口径

同版本重复运行必须比较精确 Record、Fixed Program/Projection/World identity、起始 Body、TickRate、输入序列、Body 轨迹、动作激活与停止事实以及已覆盖的表现调度。跨 ABI 或新旧布局不比较 StateHash、ProgramHash 或 canonical bytes 是否相等；只比较语义上的输入、Body/Intent、动作阶段、窗口、输出和诊断来源。当前身份已有一份 `character-fixed-input-replay-proof/5`，但其 `baseline_available=false`、比较帧数为 `0`，还没有同身份第二次运行可对账，1.4 保持未完成。

## 共享接口边界

本 change 只迁移控制合同、技能内容和现有 Session／Program／Action／Document 的消费接口。现有 Graph Authoring Framework、唯一 Capability／Port Shape、Timeline、Pose、Foot、KCC、MotionWarp、GE、网络 Pass、Replay 工作流继续由各自 owner 持有；独立场景预览只接收精确 Skill 请求与只读实例观察接口，本 change 不实现其场景启动、运行权限或旧预览播放器清理。Equipment 只接入已经安装的核心合同，不补做 Corin 装备样例和网络装备业务。

## 1.1 可重建的 Source-before 身份

### 观测条件

- 观测时间：`2026-09-05T02:17:36.8645464+08:00`。
- Unity 项目根：`D:/Unity_Project_1/3C/3cDemo/Client/3C_Client`。
- 目标产品组合：`GameplayLabLocalFixedVariant`，Fixed Q32.32，60Hz，场景 `Assets/Scenes/GameplayLab/GameplayLab.unity`。
- 这次 change 在此阶段没有修改任何 Unity C#、asmdef、Package、ProjectSettings 或 Unity 资产；只修改本 change 的 `baseline.md` 和 `tasks.md`。
- 目标 Editor 在一次正式 `character.fixed_input_trace(replay_start)` 接受后从 MCP 实例表消失，原请求没有重复提交、没有停止进程、没有重启 Editor。实例恢复后用同一 `trace_id` 查询到回放已完成并生成 v5 Proof；Proof 没有可比较的旧 baseline（`baseline_available=false`、`compared_frame_count=0`），所以仍不能把它写成重复性行为通过。

### 运行代码与外部依赖

实际 Editor 已生成并可被运行时加载的程序集以文件 SHA-256 锁定。程序集时间均为 `2026-09-04T22:04:29+08:00` 左右；同名文件内容改变即表示 Source-before 失效。

| 程序集 | 大小 | SHA-256 |
|---|---:|---|
| `Library/ScriptAssemblies/ThirdPersonSimulation.Core.dll` | 542720 | `dc3ac3849605c9b6fde723556a964e70537e22597514ab29de23f736f14e5681` |
| `Library/ScriptAssemblies/ThirdPersonSimulation.Float32.dll` | 747520 | `f776252b71b26903a21d0548b18d59c52ffb88baa13dddffb0e929e554f6fae1` |
| `Library/ScriptAssemblies/ThirdPersonSimulation.Fixed.dll` | 722944 | `40acd035010121ffdcd619272caf261191ca42ddb6a142234283ac86261302c9` |
| `Library/ScriptAssemblies/ThirdPersonClient.Runtime.dll` | 2940416 | `a2551a44351834f888c6499d123965812b41cbaa3d6d358bb669841136c8713e` |
| `Library/ScriptAssemblies/ThirdPersonClient.Editor.dll` | 3395584 | `1c532493f9809132da1ec52f5ea48820f796ec02981d16438818b858d0325f7f` |
| `Library/ScriptAssemblies/ThirdPersonGameplay.dll` | 50688 | `b4f3e39a4ab1c80c76811cd9f56045823841f86cb28550056a6227cbce38f34f` |
| `Library/ScriptAssemblies/ThirdPersonSimulation.DeterministicRollback.dll` | 138752 | `c4dcd4d9df01980e727d7259b6fbb5f144129435ff045f54f81b7e9cef4e0efc` |
| `Library/ScriptAssemblies/ThirdPersonSimulation.ServerAuthoritative.dll` | 218624 | `1bc17c15a3323e8f0293077bf6a203ec3d650f6129b2c47b146213cab89b1111` |
| `Library/ScriptAssemblies/BTSMTL.TreeDesigner.dll` | 253440 | `e082880731a6e07d99d539e02d3339e1b35af2e750ec787870218bc24ca7462b` |
| `Library/ScriptAssemblies/BTSMTL.Timeline.Tree.dll` | 30208 | `1b4dad77b11d974f8522cfe9d0a0fc04075dcb821aee283ccb275e6dce200851` |

外部和本地依赖由以下正式输入决定，不建立替代包路径：

- `Packages/manifest.json` SHA-256：`47922f287ad0cfec3e4ea00b55c87cbe9c9bd106cf75a5fc00c23558370fe311`。
- `Packages/packages-lock.json` SHA-256：`0c534a3c362a76c08d8cc841c98b32e2affd2902ffd1929c27c273948fd57415`。
- 当前 local package source：`Packages/com.alex.tengine`、`Packages/UniTask`、`Packages/com.thirdperson.dotrecast`、`Packages/com.thirdperson.performance-instrumentation`、`Packages/YooAsset`、`C:/Users/Lenovo/.unity-mcp/MCPForUnity`、`../../../../../generated-diagnostic-sampling/Packages/com.kk.generated-diagnostic-sampling`、`../../../Shared/UnityPackages/com.thirdperson.tooling-contracts`。
- Git package 的 commit/hash 与 registry/builtin 版本只取上述 lock 文件；不从网络重新解析另一版本来补齐运行结果。
- `ProjectSettings/ProjectSettings.asset` SHA-256：`f7f75d8c469445c383432ba4ff8d9887275067e006119de1a59832ede3495a0c`；`ProjectSettings/EditorBuildSettings.asset` SHA-256：`2dc695acc1555db00b9dfecb5c712e316fba6efc673606e981e846f1d82b468f`；`ProjectSettings/ProjectVersion.txt` SHA-256：`a0b539d00856d5efb273114cd789a37783233784464dbea6d23f2c4b0fd59d36`。

### 作者配置、场景和生成产物

| 输入 | 大小 | 内容身份 |
|---|---:|---|
| `Assets/Configs/Character/Corin/Pipeline/Definition/CorinCharacterPipelineDefinition.asset` | 1560 | 文件 SHA-256 `5119d400c744666e56862d9064994f1b50bc254450573c9eca17be0db0eefc93`，GUID `c7a7c1e3f7e64d81b5a04a90cbeb8d4e` |
| `Assets/Configs/Character/Corin/Pipeline/Graphs/CorinPlayableRootTree.asset` | 2510274 | 文件 SHA-256 `af6f982375dea23f8431987cac08f52544b075c6f1b184d2b0bd68cbb7e8d282`，GUID `ea4ccf43cafa4fe42aff8dfc08208d52`，authoring id `79647291-0c69-4e9e-9276-96a93c3647e7` |
| `Assets/Configs/Character/Corin/Pipeline/Graphs/SharedTimelines/CorinAttack1Timeline.asset` | 216338 | 文件 SHA-256 `16758a66d4f5a1304efd0265597f77067b32179184bf5a6e909561541514253c` |
| `Assets/Configs/Character/Corin/Pipeline/Definition/Generated/CorinCharacterPipelineDefinition.SimulationProgram.asset` | 6159633 | 文件 SHA-256 `f0c744279f5bc2ced720e7b3754ee19291d24bd0629c3f698f31078bd38c463d`，ProgramHash `2004947279ef49aff42ef81655e8ab85b6093f34c9ec90987e5e34672ba11abd`，LayoutHash `e393a861bb22ac4d3132d7f368ca687835f36b8c413e7fa739275ea296e93f71` |
| `Assets/Configs/Character/Corin/Pipeline/Definition/Generated/CorinCharacterPipelineDefinition.PresentationProjection.asset` | 121048342 | GUID `f365735adcfd49c4e96070df6bcd3bc4`，ProjectionRevision `3327a85333515b3aca74f33565552365def1d1544c1ff6fe4bb6d0f3fe6f8844`；大文件使用 wrapper metadata 精确身份，不现场重哈希或重建 |
| `Assets/Configs/Simulation/GameplayLab/Variants/GameplayLabLocalFixedVariant.asset` | 1351 | 文件 SHA-256 `49ae37e5698d99d4074e85c84945bc3bf0f93a5ebbe04e2b0019137c930ed85b`，VariantId `gameplay-lab.local-fixed-q32.32`，Target ABI `7` |
| `Assets/Configs/Simulation/GameplayLab/Compositions/CorinGameplayLabFixedComposition.asset` | 1126 | 文件 SHA-256 `371f5cef522dfe0fb7f4400077944c6b669749ff9f7e506a34e8c65d8f146ecf`，Composition GUID `79505af9210f85e43b5654aec3484cd8` |
| `Assets/Configs/Simulation/DeterministicRollback/Programs/CorinFixedProgram.asset` | 6470971 | 文件 SHA-256 `c60e1ab9dadb97581b126e681ec081c4e425c8d5cd53b1e00a2c1ea7370716e4`，ProgramHash `dfe32097138bc643ec1e7f4bb562346eee9478f18c389710f42fcdb6f8196345`，LayoutHash `40139ba56271af586179ca32574c8c8c938bd3010a69c9c1465fa258b0c032dc` |
| `Assets/Scenes/GameplayLab/GameplayLab.unity` | 15930 | 文件 SHA-256 `7190299383b5f4bb29e9262137cc3f06a320d797af614780b695ba28ab65cb55`，GUID `0f690c4f45a8b88488f43f2f7a234699` |
| `Assets/Prefabs/GameplayLab/GameplayLabLocalFixed.prefab` | 35977 | 文件 SHA-256 `36028fb4768f633c8b33b5b6d5182a7f41bcdac51d4c9bdcb7224a9144f71804`，GUID `dc28003f1eeaefc46aeeca6201996fa6` |
| `Assets/Configs/Simulation/DeterministicRollback/World/CorinDeterministicKcc.asset` | 2024 | 文件 SHA-256 `66d289ced2e980827e6905da64e1fbcdf4747f6f10e4ba94fd75397857a4e45b`，GUID `329a97239e12fac4dbe21a2325533ccc` |
| `Assets/Configs/Simulation/DeterministicRollback/World/CorinDeterministicCollisionWorld.asset` | 349823 | 文件 SHA-256 `44dbebfa9b10f7306bee33bb6b2f1254ba87ff0e4a78a244fa46dfc42ecb98ae`，GUID `d0b6f91c641a9a94aaeabe7bb97263e6` |
| `Diagnostics/CharacterInputTraces/20260904-153738-150-f169da25c67742aaafa0e9860ae4a230.json` | 924800 | 文件 SHA-256 `447551862cfe348e4cc9504248aa258e8d5684536d39ec8f2918a248cd8ca076`，trace content hash `794996596bf4c0fbe31abf7bbd12396a473bc81490dc6adc6468dcf41fb4509c` |
| `Library/CharacterSimulation/Programs/c7a7c1e3f7e64d81b5a04a90cbeb8d4e/float32-ieee754-abi8.csim` | 3079259 | canonical bytes SHA-256 `a6ce0f23b695fe2fbf335cfb86f662ce0f4be127b819329f48eaad7f4a90a810` |
| `Library/CharacterSimulation/Fixed/c7a7c1e3f7e64d81b5a04a90cbeb8d4e.fixed-program` | 3234949 | canonical bytes SHA-256 `66ab52044506f1ea782a75a421001f1c2dc65432f85d0c4dec066e6ee351fe9c` |
| `Library/CharacterSimulation/SemanticIr/c7a7c1e3f7e64d81b5a04a90cbeb8d4e.csir` | 31624875 | 文件 SHA-256 `302f2250eb474c976092f34a41b88b7b6645b07d48f97e55e1d54d321d1cbe9a` |

Float32 SourceRevision 为 `68c5cded5b49c345693f3c20317373fee6883a6fd22a3969b2d2c0abdf53a759`，Fixed SourceRevision 为 `c851c73cbe47678620e58a8300fdcb20a475da5f1fbf57d29c8f9a167a1d7d63`。这些身份和 wrapper metadata 一起锁定了 Definition、Semantic IR、Target Program、Projection、World 和 trace 的实际消费版本。当前工作区共有 `1798` 项状态，Unity 项目目录内有 `1577` 项状态；其余脏改动属于并行任务，不能以“目录干净”作为本 change 的前提，也不能被本 change 暂存。

### 正式 Fixed Replay Proof

- Proof：`Temp/CharacterInputReplayProofs/v5/f169da25c67742aaafa0e9860ae4a230/20260905-022420-970-52d757e59fb841f9968feba2eab66461.json`。
- Proof 文件大小 `992427`，文件 SHA-256：`63ece2ab409c692d89a0a1d5f2358e536559e784e4f29f230b177c3f1b789f53`；schema `character-fixed-input-replay-proof/5`，run id `52d757e59fb841f9968feba2eab66461`，proof hash `1e76b35368ae75099266cb664258964bec2e58646936b8000006dbe2bfeab43c`。
- Proof 锁定：trace `f169da25c67742aaafa0e9860ae4a230`、Fixed ProgramHash `dfe32097138bc643ec1e7f4bb562346eee9478f18c389710f42fcdb6f8196345`、SourceRevision `c851c73cbe47678620e58a8300fdcb20a475da5f1fbf57d29c8f9a167a1d7d63`、SemanticHash `0e13d3bceced825d364bfda43a0df204e4e349f15db12c6e2357510b93e083a1`、ProjectionRevision `3327a85333515b3aca74f33565552365def1d1544c1ff6fe4bb6d0f3fe6f8844`、WorldRevision `corin-gameplay-lab-world-v1`、StartBodyHash `f8f969f5f578a13088473352ec28c1c7e94ecb7fbdb9e97378a2adc880631474`、1492 帧和 60Hz。
- Proof 的业务聚合字段为 InputSequenceHash `9687ee6e5ab27203524360b87acd319e61e6dcf317c768d5bc1254f9cec237bf`、BodyTrajectoryHash `a591a39b9452b01bd8621845f6f3817f6e3dead715459fd98a6132c937203813`。`comparison.baseline_available=false`、`matched=true` 但比较帧数为 `0`，这里的 `matched` 只表示没有执行比较时没有发现差异，不能替代第二次同身份运行。

### Development Center 正式短链路状态

- 按 `3c-fast-development-validation` 从 Git common dir 的 `development-project.json` 和 catalog 解析的 Run Host 为 artifact `7f67def67b989e409819e0946bf64d42760e5a94168436a424310d8aaf4dd8be`，版本 `thirdperson.development.run-host/1.4.5`，entry point 为 `ThirdPerson.Development.RunHost.exe`。
- 已按正式命令对当前仓库、同一绝对 trace 路径提交一次 `editor --workflow character.replay`。Host 返回 `WorkspaceEditorInUse: D:\Unity_Project_1\3C\3cDemo\Client\3C_Client`，没有 run_id、没有 source-before、source-changes、editor-receipt、editor-result 或 Center Evidence。
- 没有停止现有 Unity Editor、没有修改 workspace lease、没有换到不含该 trace 的 `D:/Unity_Project_1/3C-center`，也没有重试本次提交。该结果是 1.1 的环境阻塞记录，不是完成证据。

### 保护和失效规则

- 本次 baseline 只把实际运行入口锁定到上述 Fixed 组合；TrainingEnemy 的 `e135...` Semantic IR、无效 Program 或旧 Foot proof 不是本次基线，不通过修它们来取得成功。
- `Assets/GameScripts/Main/Runtime/Simulation/Core/**`、`.../Simulation/Core/Fixed/**`、`.../Simulation/Core/Float32/**`、`.../Character/Pipeline/**`、`.../BTSMTL/**` 与其对应 ScriptAssemblies 是运行代码输入；`CharacterFixedInputTraceWorkflow.cs` 等未提交改动已由当前 Editor 程序集身份覆盖，但不能在重建后继续默认为相同版本。
- `openspec/AGENTS.md` 已按项目指令删除，不恢复为第二套 OpenSpec 规则。`Library/**` 只作为本次运行的产物身份，不进入业务提交。
- 如果以上任何文件、外部 local package、asmdef、ProjectSettings、程序集、wrapper、ProjectionRevision、World 身份或 trace 内容改变，必须重新记录 Source-before，不能继续使用本记录的 replay 结论。

## 1.2 RootTree 逐入口迁移表

### Locomotion 状态

判断标准是“是否改变 Gameplay motion、Gameplay fact 或下一条转换门槛”，不是状态名字，也不是是否含有 Timeline。当前图中确实有 8 个状态；其中 `RunStart` 没有正式入边，`WalkEnd` 和 `RunEnd` 的 State Body 只有 Root／StateOnEnter／StateOnExit，没有运动节点，但仍参与 stopping、StateRootCompleted 和重新输入，因此迁移为规范化的 C# `WalkStopping/RunStopping` 状态，不能原样照搬也不能删除。

| 当前状态与身份 | 当前实际内容 | 新 owner | Pose 侧 | 旧删除点 |
|---|---|---|---|---|
| `Idle`，State GUID `fc35fcf6-209f-5ce9-82c8-2cfe5081510c`，Body Graph `9e85a0fd-ec72-45a9-bee4-fc214d0fd8d7` | State Body 只有生命周期和 Enter 的 `Clear Directional Dodge Run Intent`；该 Intent 是 Gameplay 事实，不能当作动画清理 | C# `Idle` State；Enter 的 Intent 消费写入同一控制状态 owner，不能留在 Blackboard 镜像 | committed Idle fact 进入现有 PoseStateMachine | 删除 RootTree 中的 `StateNode` 和 StateBehaviorSubTree；保留状态语义，不保留图调度 |
| `WalkStart`，State GUID `01fa46e2-ef80-5ac4-9acb-d874fd5b5f6b`，Body Graph `038c0194-37cc-4357-a042-4d7a21e7d9e8` | `LocomotionInputMotionNode` GUID `d374b14e-fe5b-50b1-bc5d-7bfae2675b11`，速度 `4.592`，TurnSpeed `720`，CameraRelative，ExecutionMode `1`，Duration `1.1s`；进入 Loop 的条件引用 `StateRootCompleted` 和输入门槛 | C# `WalkStart` State；速度、转速、相对相机和必须等待的完成事实进入 typed control parameter/state，不复制曲线算法 | 保留 WalkStart 作为有限表现 source，消费 committed WalkStart/Walk mode | 删除该 State Body 的 InputMotion 节点和外层 State 图入口 |
| `WalkLoop`，State GUID `129fb8de-2914-50f1-8bb6-e682ad76a67b`，Body Graph `e24e290b-b14d-4d41-adb5-56cfcc25ecd3` | `LocomotionInputMotionNode` GUID `9fa9b118-97a8-52ac-bb06-030e8e533efa`，速度 `6`，TurnSpeed `720`，CameraRelative，ExecutionMode `2`，Duration `0`；`MoveAxis` 直接形成持续 Gameplay motion | C# `WalkLoop` State；保持输入消费、速度和同 Tick motion request 顺序 | 保留 WalkLoop source 和原有 Gait/相位行为 | 删除旧 InputMotion、StateBehaviorSubTree 和角色 RootTree 调度 |
| `WalkEnd`，State GUID `ff19b76a-3e71-50bb-8677-d8b5a187a50a`，Body Graph `be79c263-a278-41e1-893b-df7f9bdec52e` | 只有生命周期节点；进入门槛是 `MoveAxis < StopThreshold`，离开到 Idle 还检查 StateRootCompleted；虽然 Body 没有运动节点，仍然拥有停止状态身份、StateRootCompleted 门槛和重新输入的正式边 | C# `WalkStopping` State；保留 Enter/Tick/Exit、StateRootCompleted 和同 Tick 重新接入语义；旧 State/edge GUID 只作为迁移 Source Map 来源关系，新 State 必须声明新的稳定代码 identity，不能复用旧 GUID、兼容 alias 或第二查找键 | Pose 仍可消费 WalkStopping/WalkEnd 的表现 transition，但不能替代 Gameplay 停止决策 | 删除旧 StateNode、StateBehaviorSubTree 和图调度；完整旧来源映射到 `WalkStopping`，新代码 identity 在实施任务中登记 |
| `RunStart`，State GUID `54223f0b-f2e9-5178-ab79-bc4269701214`，Body Graph `b61c1e1f-8f91-4e5b-bc0c-8cd21cbc3e2f` | Body 有 `RunStart Input Motion` GUID `ea8f1d27-c17f-5d09-aa40-f864e0cff04f`，速度 `5.1`，TurnSpeed `720`，ExecutionMode `0`，但当前 State `m_InputEdgeGUIDs: []`，没有正式入边 | 不作为当前 C# 正式 Gameplay State；先保留“无正式入口”的诊断事实，不能为修作者图凭空补一条入口 | Pose 可保留 RunStart source，按正式 Run mode 的表现 transition 使用 | 删除不可达 Gameplay State 和其旧 InputMotion 调度；Pose source 不删 |
| `RunLoop`，State GUID `95fea828-8395-5d62-9be5-5727b48acd0d`，Body Graph `d1df5966-fc69-4a8e-ac65-a3a0261bbd91` | `LocomotionInputMotionNode` GUID `31f03892-a8f0-5d1f-8911-c18986b688a7`，速度 `7.36`，TurnSpeed `720`，CameraRelative，ExecutionMode `2`；既承接前闪避后的 `HasDirectionalDodgeRunIntent`，也作为 MovingTurn 唯一入口 | C# `RunLoop` State；Run 只能由正式方向闪避意图和明确 Transition 进入，不能由速度猜测 | 保留 RunLoop source；Pose 只消费 committed Run mode | 删除旧 InputMotion、StateBehaviorSubTree 和角色图调度 |
| `RunEnd`，State GUID `84c12f85-34ab-5f09-8b1f-c73166c3deb9`，Body Graph `0b5ba3fb-ebe1-4dc6-b7da-400747b1a1e7` | 只有生命周期节点；当前只承担 `MoveAxis < StopThreshold` 后的空状态过渡，但仍拥有 RunEnd 的状态身份、StateRootCompleted 门槛和重新输入边 | C# `RunStopping` State；保留 Enter/Tick/Exit、StateRootCompleted 和同 Tick 重新接入语义；旧 State/edge GUID 只作为迁移 Source Map 来源关系，新 State 必须声明新的稳定代码 identity，不能复用旧 GUID、兼容 alias 或第二查找键 | Pose 可保留 RunEnd source，用于 Run 离开时的表现过渡，但不拥有 Gameplay 决策 | 删除旧 StateNode、StateBehaviorSubTree 和图调度；完整旧来源映射到 `RunStopping`，新代码 identity 在实施任务中登记 |
| `MovingTurn`，State GUID `27d93ca2-0aa4-5da9-9e2b-9ab1a096bb15`，Body Graph `6609d713-9bea-4a1a-aa00-9d9b0ea80554` | 唯一从 RunLoop 进入；State Body 里的 TimelineData `CorinMovingTurnRootMotionTimeline` authoring id `8a6491b4-93fe-4002-a814-2ac6eb75e567`，MotionCurve `MovingTurn180`，范围 `0..28` 帧，产生 Gameplay Body motion | C# `MovingTurn` State；曲线作为编译后的 typed Body Motion descriptor，曲线求值复用纯 Motion 能力；不创建 SkillDefinition、不创建 ActionInstance | committed MovingTurn fact 选择 Turn Pose；Pose 不能重新读取 Facing Error 或修改 Body yaw | 删除 TimelineNode 和旧 Timeline 调度器；保留静态曲线描述和控制状态的恢复字段 |

`WalkStart/WalkLoop/RunStart/RunLoop` 的 Start／Loop 完成语义仍必须通过 `StateRootCompleted` 的同 Tick 事实迁移。当前资产已经确认 WalkStart 使用 `ExecutionMode=1, Duration=1.1`，其余 Start/Loop 的原始值也写在上表；迁移时不能把这些值当作纯动画时长删除。`WalkEnd/RunEnd` 虽然当前 State Body 没有运动节点，仍是正式 Gameplay stopping state；初次迁移保留为 `WalkStopping/RunStopping`，不能把停止状态和 Pose 表现合并。

### Locomotion 的 16 条 Transition

下面的顺序是 `Corin Locomotion StateMachine` 的序列化 `m_Edges` 顺序。`CompareType=2/4/5` 分别是 Less／GreaterEqual／Greater；同一来源的多个候选仍以 priority 后的稳定顺序评估。左列旧 edge identity 只作为迁移 Source Map 来源，右列每个 `C#` 名称都是待实施登记的新稳定代码 identity；新运行时不得复用旧 GUID、兼容 alias 或第二查找键。集合核对结果：资产中 `m_Edges` 共 16 条，以下列出的 16 个 edge identity 一一对应，没有孤立 edge，也没有遗漏 edge。

| # | Edge identity | From → To | Priority | ConditionRule 来源与实际门槛 | 新 owner 或删除理由 |
|---:|---|---|---:|---|---|
| 1 | `362a5b1d-dc00-5d71-aac7-554891a4f4d1` | Any `6f450ae1-3cfa-5b99-a062-eb1bad7a60fe` → Idle | 0 | `locomotion/Enter_to_Idle_Rule`，结果常量为 true | C# 新 identity `LocomotionEntryToIdle`；旧 edge 仅进 Source Map，运行时删除 Any→Idle 图 edge |
| 2 | `cefb032a-559d-5b49-a828-636ab8675b57` | Idle → WalkStart | 1 | `Idle To WalkStart Condition`：`MoveAxis magnitude > StopThreshold` | C# `IdleToWalkStart`；输入只读，不消费两次 |
| 3 | `cfd47800-17b5-5bdf-b52f-05186263aae0` | WalkStart → WalkLoop | 100 | `WalkStart To WalkLoop Condition`：`StateRootCompleted AND MoveAxis magnitude > StopThreshold` | C# `WalkStartToWalkLoop`；保留 Start 完成门槛与 priority |
| 4 | `ca8d861b-ab9a-50a2-8860-2f41fa6fa7e9` | WalkLoop → WalkEnd | 1 | `locomotion/WalkLoop_to_WalkEnd_Rule`：`MoveAxis magnitude < StopThreshold` | C# 新 identity `WalkLoopToWalkStopping`（待实施登记）；旧 edge GUID 只进入 Source Map，进入正式 stopping state |
| 5 | `286730dd-a1eb-520e-b7da-20fe5deeed66` | WalkEnd → Idle | 100 | `locomotion/WalkEnd_to_Idle_Rule`：`StateRootCompleted AND MoveAxis magnitude < StopThreshold` | C# `WalkStoppingToIdle`；保留空 Body 首次 Tick 后的完成门槛和 source barrier |
| 6 | `5a7962f8-6408-591f-b85a-6f5bae17a85d` | RunLoop → RunEnd | 0 | `locomotion/RunLoop_to_RunEnd_Rule`：`MoveAxis magnitude < StopThreshold` | C# 新 identity `RunLoopToRunStopping`（待实施登记）；旧 edge GUID 只进入 Source Map，进入正式 stopping state |
| 7 | `7494ee92-1226-533f-9ce9-ee03dca3c69b` | RunEnd → Idle | 100 | `locomotion/RunEnd_to_Idle_Rule`：`StateRootCompleted AND MoveAxis magnitude < StopThreshold` | C# `RunStoppingToIdle`；保留空 Body 首次 Tick 后的完成门槛和 source barrier |
| 8 | `8a410586-1246-5d8f-a3fb-2d7030477dfc` | MovingTurn → RunLoop | 100 | `MovingTurn To RunLoop Condition`：曲线完成、`HasDirectionalDodgeRunIntent`、输入仍高于 StopThreshold | C# `MovingTurnToRunLoop`；Intent 由同一控制 owner 消费 |
| 9 | `cc018baf-cd18-416b-82d0-ba083584fe36` | WalkStart → WalkEnd | 1 | `State_To_State_Rule 3`：`MoveAxis magnitude < StopThreshold` | C# `WalkStartToWalkStopping`；保留 Start 中止到 stopping state 的状态可见性 |
| 10 | `2cf4ef14-7cf8-4476-8e30-c8e34633ffa9` | WalkEnd → WalkStart | 1 | `WalkEnd To WalkStart Condition`：`MoveAxis magnitude > StopThreshold` | C# `WalkStoppingToWalkStart`；保留重新输入的同 Tick/source order |
| 11 | `f9842f44-15dc-49a9-a7eb-68e4206ca5fb` | RunEnd → RunLoop | 1 | `RunEnd To WalkStart Condition`：`MoveAxis magnitude > StopThreshold` | C# `RunStoppingToRunLoop`；保留重新输入的同 Tick/source order |
| 12 | `8b001fc1-ec21-48ae-bc95-a11be3cc47e5` | MovingTurn → WalkEnd | 0 | `MovingTurn To WalkEnd Condition`：曲线完成、`MoveAxis magnitude < StopThreshold` | C# `MovingTurnToWalkStopping`；MovingTurn 完成后仍经过正式 stopping state |
| 13 | `cf51c921-5c32-4295-851d-a1a8e35219c5` | WalkLoop → RunLoop | 0 | `WalkLoop To RunLoop Condition`：`HasDirectionalDodgeRunIntent` | C# 显式转换；不以速度阈值猜 Run |
| 14 | `1795497d-9f9f-432a-877a-c05b12b1a56a` | WalkStart → RunLoop | 100 | `WalkStart To RunLoop Condition`：`HasDirectionalDodgeRunIntent` | C# 显式转换；保留前闪避恢复在 Start 阶段的优先级 |
| 15 | `ef58278c-4f0f-478e-a58b-2d0eed4c1b4d` | RunLoop → MovingTurn | 2 | `RunLoop To MovingTurn Condition`：有效 MoveAxis、`MoveFacingAngle >= 135°`、Attack/Dodge Context 均 inactive | C# `RunLoopToMovingTurn`；MovingTurn 曲线进入同一 control state，不创建 Action |
| 16 | `36a98d5b-89cf-404d-ab20-6679194dd018` | MovingTurn → WalkLoop | 100 | `MovingTurn To WalkLoop Condition`：曲线完成、无 Run Intent、输入高于 StopThreshold | C# `MovingTurnToWalkLoop`；与 #8 使用同一完成事实 |

`StopThreshold` 当前作者值为 `0.05`，`MovingTurnAngleThreshold` 当前作者值为 `135`。RunLoop→MovingTurn 是唯一 MovingTurn 入口；普通 Walk 不因为朝向角度进入 MovingTurn。MovingTurn 完成后再按 Intent／输入分流，不能用第二个 Facing Error 路径提前释放。

### 空 State Body 的实际执行顺序

当前实现不能因为 State Body 没有运动节点就把 stopping state 合并掉。代码链的事实如下：

1. `StateBehaviorSubTree.UpdateStateRoot` 只有在 `UpdateStateEnter` 成功后才调用 `UpdateTree`；Enter／Exit 是独立生命周期阶段。
2. 编译后的 `OperationControlRuntime.Execute` 对 `Root` 使用 `TickSingleChild`。Root 没有 child edge 时返回 `Success`，但这只表示该次 Root operation 完成，不会跳过所属 State 的身份。
3. `TickState` 先推进 StateOnEnter，再 `TickPersistent(StateRoot)`；随后 `TickStateMachine` 在同一次控制求值中调用 `SelectTransition(active)`，`CurrentStateRootCompleted` 读取 Root operation 的 `RunnableLifecycle=Success`。所以 `WalkEnd/RunEnd` 的 StateRootCompleted 能在首次 tick 空 Body 时成立。
4. 选中 edge 后，`ContinueStateTransition` 先对当前 State 执行 `RequestStop`／StateOnExit，清理 exiting/pending/transition，再 `ActivateState` 目标 State；目标 State 的 Root 不在该次调用中再次 tick，而是在下一次 StateMachine tick 开始。
5. 因此从 WalkLoop/RunLoop 进入 WalkEnd/RunEnd 的 edge、stopping state 的 active identity、下一次 tick 的 StateRootCompleted、以及重新输入 edge 都是可观察的时序。新合同保留 `WalkStopping/RunStopping`；旧 Graph State/Edge identity 只进入迁移 Source Map，新 C# State/Transition 必须拥有新的稳定代码 identity，不把 empty Root 当作不存在。

该顺序由 `OperationControlRuntime.cs` 的 `Execute`、`TickStateMachine`、`ContinueStateTransition`、`TickState`，以及 `OperationControlContracts.cs` 的 `CurrentStateRootCompleted` 路径确定。本表完成源到目标映射；正式 C# 控制实现仍需由后续任务以同一输入和已有 Replay 核对状态 identity、generation、停止 barrier、Pose fact 与 Body 输出。

### 动作入口、技能内容和引用

当前动作图由外层 `Corin Action StateMachine`（authoring id `fdfba4db-d919-460a-a2f3-eb8149c7610c`，状态 `None/Attack1/Attack2`）、嵌套 `Attack Combo StateMachine`（authoring id `ba00b356-4bf5-4b38-9bec-7ac934b7a25c`，状态 `Attack3/Attack4/Attack5`）和 `Dodge Direction StateMachine`（authoring id `ac7e681e-356d-4092-87b6-d6836bcac884`，状态 `DodgeBack/DodgeForward`）组成。七个正式激活入口逐项如下：

| Skill 目标 | 当前 State Body Graph | ActivateActionInstanceNode | ActionProfile / Context | TimelineNode → TimelineData | 新 owner / 删除点 |
|---|---|---|---|---|---|
| Attack1 | `cf79f885-4e1f-4a3b-8bf7-8a21620959b1` | `935605a3-a912-5969-9d09-9140c6a67a83` | Attack profile GUID `450722665af79c2468ff212811859458`，Context `2639a9c0228ef404389f9fd9a36561f5`，含 `ActionTarget` snapshot | `35f64fa2-0ff9-5fcc-a8e6-393fb9c61782` → shared asset `CorinAttack1Timeline.asset`，Timeline authoring id `10f4cb90-8b9a-4944-b77c-14efc9a3124d` | `SkillDefinition Attack1` 引用同一 ActionProfile；激活节点从角色图删除 |
| Attack2 | `cbe6f984-5d7a-48e7-89b9-f30bc6c91325` | `5ca1d7d5-06c7-5311-86a0-6caa5f729947` | 同 Attack profile / Context，含 target snapshot | `f7e0a35f-8383-5c84-b7d3-0409f82e6f59` → `CorinAttack2Timeline`，authoring id `40908a3b-5568-459b-b4f0-b871155dc226` | `SkillDefinition Attack2`；删除外层 Attack2 State 与节点 |
| Attack3 | `451f8513-596d-416c-a685-fff57e9fe51f` | `edf8c28e-db08-4916-8780-e293867ecd7f` | 同 Attack profile / Context，含 target snapshot | `7cb8df79-b65a-4e3a-b812-5c60b8110c2f` → `CorinAttack3Timeline`，authoring id `001b250e-9f1c-44ab-a170-c43896e756ac` | `SkillDefinition Attack3`；删除嵌套 Action State 与节点 |
| Attack4 | `87cc7381-11cd-4631-a1b1-99114073ede9` | `a5ec1c18-1d81-4da8-ac27-267fd162b50e` | 同 Attack profile / Context，含 target snapshot | `b9425a66-9c3a-4722-b1de-dcf1e36cab32` → `CorinAttack4Timeline`，authoring id `9fa96566-14a4-47e4-b6ea-c6c710ffbbcf` | `SkillDefinition Attack4`；删除嵌套 Action State 与节点 |
| Attack5 | `3dfd9a99-a63f-4df6-9689-b9b9789e2171` | `6a021735-d216-4710-bf73-cb596dcbdad4` | 同 Attack profile / Context，含 target snapshot | `b4190219-bdf9-437c-ba83-d607ab1f22a3` → `CorinAttack5Timeline`，authoring id `7b4f6ad5-7513-4a00-92f3-d34c3761d2f9` | `SkillDefinition Attack5`；删除嵌套 Action State 与节点 |
| DodgeBack | `ee909991-5be0-4961-838a-a2854baca30d` | `ff10ace1-68cb-433b-a188-67636d72cfca` | Dodge profile GUID `add8a25636588a543a6770231bea14b6`，Context `7294b270a227b32429a8159cdbfd96b3`，无 target snapshot | `2379310b-cbad-44e2-9f3b-398442659c85` → `CorinDodgeBackTimeline`，authoring id `1ec9175b-a959-4960-af6b-4177f601425f` | `SkillDefinition DodgeBack`；删除 Dodge Direction 图中的旧激活节点 |
| DodgeForward | `b2328afd-40a8-467d-91f8-784c461f6137` | `cb819416-9c25-4f1c-bc2f-921bbd8d9970` | 同 Dodge profile / Context，无 target snapshot；完成时写入 `HasDirectionalDodgeRunIntent` | `3f4f4ef4-8a13-430c-afc4-c772e6612e72` → `CorinDodgeForwardTimeline`，authoring id `86e3cd9c-eab8-41b0-b2f5-e9a73c3cfa27` | `SkillDefinition DodgeForward`；Intent 通过控制事实回流，删除旧 Dodge Direction 激活节点 |

两份当前 ActionProfile 的实际策略是：Attack 的 `ActionId=Attack`、Tag `Attack`、CancelTags Any=`Attack/Dodge`、TargetRequirement=`2`；Dodge 的 `ActionId=Dodge`、Tag `Dodge`、同样可取消 Attack/Dodge、TargetRequirement=`0`。它们不能单独区分 Attack1–5，所以新目录必须用七个稳定 SkillId 精确选中技能，同时继续让 ActionProfile 只拥有准入策略。

技能内容中的 Timeline、TreeClip、Window 不删除为“动画数据”，而是从角色 Root 调度迁入对应 SkillProgram 闭包：

- TreeClip 总数为 23。Attack1 shared timeline 有 `d7c8216a-4bdf-4676-809f-f4828d6e986a(18..45)`、`ffc20627-9e1a-4ab0-be42-8c4af9e6f1b0(50..93)`、`de283c70-27e9-453b-b188-24736779ffa3(73..162)`、`3509c34c-84ff-4a89-8100-cc549a0f8c85(43..162)`；Attack2 有 `61051861-039f-4201-881c-fb54f2196f94(18..45)`、`a5b14e00-6ecf-4dbf-823c-0930c5eb78a4(49..92)`、`12c83914-03e3-45c5-bd53-19b60de4184f(72..167)`、`7d8c2bcc-da17-4fd4-8d78-a8773c41372a(42..167)`；DodgeBack 有 `cfc6c09a-4242-4d47-a1ed-ac7afc63f2f0(45..141)`、`13026fa8-024d-401e-a8dc-05d2029ade02(6..45)`；DodgeForward 有 `33043955-e07f-4d19-90e2-eedaf53be24a(46..142)`、`65e45e92-4d8a-4a39-8d77-ec4bbf9a0ddb(6..45)`；Attack3 有 `6edc10a0-ce7d-4df5-90b5-68c0f558c754(18..45)`、`a9b9776e-813b-4370-9830-ae60b790ce3a(82..125)`、`16532453-14c2-4e3d-887e-d1a9f1e1cb34(105..200)`、`c0da1981-c720-4b18-ac88-18636ea5a250(75..200)`；Attack4 有 `1291bc39-93e9-42d1-9ecd-74adba11903c(18..45)`、`b4b57cb6-f6a8-44fa-80de-c012b26143b8(90..133)`、`a519e919-74ac-4d61-9d53-9dd23585b789(113..276)`、`241be600-9d40-4701-b766-c6ea4535c9ff(83..276)`；Attack5 有 `d7416695-9eac-4822-87b7-256c0e8dcf69(18..45)`、`487f2658-8af7-446b-b63e-7e2992c41bd1(149..206)`、`33a8e667-5ab4-4850-b14e-ced978a45630(119..206)`。
- Window fact 总数为 23：Attack1/2/3/4 各有 `Hit`、`ComboAccept`、`RecoveryLate`、`RecoveryEarly` 四项，digest 分别为 `1001..1004`、`2001..2004`、`3001..3004`、`4001..4004`；Attack5 有 `Attack5Hit=5001`、`Attack5MoveCancel=5003`、`Attack5RecoveryEarly=5004`；Dodge 有 `DodgeBackIFrame=1`、`DodgeForwardIFrame=2`、`DodgeRecoveryCancel=7002`、`DodgeForwardRecoveryOpen=7003`。Window 查询节点可重复出现，但事实 identity 以上述 key/digest 为准。
- 7 个 `ActivateActionInstanceNode`、8 个 TimelineNode、23 个 Window fact、23 个 TreeClip 的引用集合已按 RootTree 和 Attack1 shared asset 逐项列出。迁移后的 SkillDefinition 继续拥有 Tree／Timeline／局部 StateMachine／Window，角色控制只生成 SkillRequest，不运行角色级激活节点。

### 输入、Blackboard 和表现 producer

- `CorinCharacterInputProfile` 当前只声明 `MoveAxis`、`LookAxis` 两个 Vector2；`Attack` 和 `Dodge` 两个 request 的 Buffer 都是 `0.2s`，优先级分别 `0` 和 `100`，Dodge 绑定 Left Shift 的正式输入引用。
- RootTree 序列化类型计数为 `CharacterInputVector2InfoNode=5`、`CharacterInputVector2MagnitudeInfoNode=35`、`CharacterActionRequestInfoNode=32`、`ActionContextActiveInfoNode=30`、`ActionWindowActiveInfoNode=42`、`CanActivateActionInfoNode=32`。这些是旧图的重复查询入口，不是新控制状态的独立 owner。
- Locomotion 作者 Blackboard 值为 `StopThreshold=0.05`、`MovingTurnAngleThreshold=135`；`ActionTarget` 是 `InputValueId=ActionTarget` 的 target snapshot；`HasDirectionalDodgeRunIntent` 是 Action 范围的持久事实，必须由 DodgeForward／控制模块唯一写入并在 Idle 等终止结果消费。
- Attack Timeline 的 AnimationTrack 使用有限 `FullBodyAction` channel；Corin Presentation action source 资产为 `CorinAttack1AnimationSource` 至 `CorinAttack5AnimationSource`、`CorinDodgeBackAnimationSource`、`CorinDodgeForwardAnimationSource`。这些 producer 迁为 SkillDefinition 的有限 action binding；Pose Graph 继续是唯一播放与混合 owner。
- `MovingTurn180` 是例外的 Gameplay motion producer：从 Timeline 迁为控制模块的静态 typed Body Motion descriptor，不迁成 Pose producer，也不保留第二个 Timeline scheduler。

### Equipment 当前入口

Corin Definition 当前 `m_EquipmentCapabilityEnabled=0`，没有有效 Equipment Profile、Presentation Profile 或 Equipment Persistent/Route root。Equipment Core 的代码入口存在于 `CharacterPipelineDefinition.Equipment.cs`、`CharacterEquipmentAuthoring.cs`、`EquipmentRuntimeNodes.cs`、`Runtime/Simulation/Core/Execution/EquipmentRuntimeControl.cs` 和 Fixed/Float Equipment runtime，但这份 Corin 基线没有可迁移的实际 route 数据。本 change 只为已安装 Equipment Core 增加控制 binding／Skill 引用接口，不能用补造 Corin 装备样例来填表，也不能把无效 root 当作成功。

## 1.3 33 项 Capability 与并行边界矩阵

矩阵中的“触碰”表示本 change 的实施范围，不表示当前阶段已经修改代码。保护标记：`Pose` 保护现有 Pose／IK／Foot／Projection 实现；`KCC` 保护 Deterministic KCC、Collision World 和 Motion 数学；`Preview` 保护独立场景预览 change 的运行权限与播放器；`Equip` 只接入已安装 Equipment Core；`AI` 保护 AI 只写 CharacterSimulationInput；`Pipeline` 保护既有 Session／Pass／Commit 四阶段。

| Capability | 当前实现入口／owner | 本 change 替换或保留的接口 | 触碰 | 冲突边界与保护文件 |
|---|---|---|---|---|
| `character-control-runtime` | 当前没有显式 C# 控制模块；实际控制在 `CorinPlayableRootTree.asset`、`CharacterAuthoringSourceCompilationModel.cs` 和两个 Target evaluator | 新增唯一 C# State／Transition／SkillRequest／typed state contract；删除角色级控制图入口 | 全量迁移 | 不生成 Pose 状态，不绕过 `PipelineTransactionCoordinator`；保护 `Pose`、`Pipeline` |
| `btsmtl-skill-program-runtime` | `CharacterSemanticEmitter.cs`、`OperationControlRuntime.cs`、`TimelineControlRuntime.cs` 执行 RootTree | 将解释器正式入口收为 SkillProgram closure；保留同一 operation／Timeline interpreter 和 ActionInstance owner | 全量迁移 | 不创建第二 interpreter、SkillInstance manager 或 Preview runtime；保护 `Preview`、`Pose` |
| `btsmtl-gameplay-semantic-ir` | `CharacterGameplaySemanticIr.cs`、`CharacterGameplaySemanticIrCodec.cs`、`CharacterAuthoringSourceCompilationModel.cs`、`CharacterSemanticFrontendCompiler.cs`、`CharacterSemanticEmitter.cs` | Character root 改为 control binding＋skill roots；保留 numeric-neutral IR、source map、dependency closure | 改 Discovery／IR schema | 不包含 C# 对象、Unity Object 或角色 RootTree execution root；保护当前 `.csir` 产物直到新版本发布 |
| `btsmtl-compiled-simulation-program` | `CharacterSimulationProgramBuilder.cs`、`CharacterSimulationProgramAsset.cs`、Fixed/Float `CharacterSimulationProgramCodec.cs`、`SimulationProgramSemantics.cs` | Program 包加入 control catalog、SkillProgram catalog、完整 state layout；保留 canonical bytes、精确重读、stale/ABI 拒绝和原子发布 | 改包和 ABI | 不复用旧 Program reader，不把 Projection 算法并入 Program；保护 Projection asset |
| `character-simulation-kernel` | Fixed/Float `SimulationKernel.cs`、`SimulationKernelContracts.cs`、`PipelineTransactionCoordinator.cs` | 在同一 Evaluate 中调用 control 与 Skill interpreter；保留数值、WorldResolve、Finalize、Commit 边界 | 改调用顺序 | 不增加 Controller Update、网络 Tick 或 WorldSolver 旁路；保护 `Pipeline`、`KCC` |
| `character-action-instance-runtime` | Fixed/Float `FixedActionRuntime.cs`、`Float32ActionRuntime.cs`、`FixedActionStateStore.cs`、`Float32ActionStateStore.cs` | ActionInstance 继续是一次释放唯一 owner；新增其 SkillProgram identity、SkillExecutionState、generation 和调用 frame | 改实例状态 | 不新增平行 SkillInstance 生命周期；保护 Action target snapshot、既有 transition 规则 |
| `character-action-activation-flow` | `Character/Pipeline/Graph/ActionRuntimeNodes.cs`、Fixed/Float ActionRuntime、`ActionProfile.cs` | 代码控制提交统一 SkillRequest，继续复用 RequiredTag、CancelTag、TargetRequirement、准入与 lifecycle | 改请求来源 | 不在 Pose、Timeline 回调或网络旁路激活；保护 `ActionProfile` 的策略含义 |
| `character-action-authoring-closure` | `ActionProfile.cs`、`ActionProfileEditor.cs`、Corin Action Profile/RootTree 资产 | 增加 SkillDefinition、签名、图闭包、退出语义；ActionProfile 仍只负责策略 | 改作者闭包 | Attack1–5 共用 Profile 但必须用 SkillId 精确区分；保留技能原owner编辑入口 |
| `btsmtl-runnable-timeline-node` | `BTSMTL/Timeline/Scripts/Tree/TimelineNode.cs`、`TimelineControlRuntime.cs`、TreeTrack/TreeClip | Timeline／TreeClip 只在 ActionInstance 的 SkillExecutionState 内推进；保留 Decision→Commit、停止顺序 | 改 owner/入口 | `MovingTurn` 曲线抽成控制 Body Motion 后删除 TimelineNode 调度；Attack/Dodge Timeline 保留；保护 `Pose` |
| `btsmtl-sm-node-authoring` | `StateMachineGraph.cs`、`StateMachineGraphRuntime.cs`、`StateBehaviorSubTree.cs`、RootTree 中 3 个角色级 StateMachine | 角色 Locomotion 改 C# State／Transition，WalkEnd/RunEnd 映射为 WalkStopping/RunStopping；技能内局部 StateMachine 继续使用共享 Graph | 改角色范围 | 不删除 AI/Pose 共享 StateMachine；不把空 Body 当成无状态，不折叠 stopping barrier；保护 `AI`、`Pose` |
| `character-pipeline-blackboard` | RootTree ExposedProperty、`CharacterInputValueNodes.cs`、runtime Blackboard state/access | 技能变量按声明和调用 frame 归实例；C# control state 拥有自己的 typed schema；输入只读投影 | 改所有权和布局 | 删除角色 Blackboard→C# 镜像；GE／Equipment 仍各自拥有事实；保护 `Pipeline` |
| `character-equipment-feature-authoring` | `CharacterEquipmentAuthoring.cs`、`CharacterEquipmentPresentationAuthoring.cs`、`CharacterEquipmentEditors.cs`、`CharacterPipelineDefinition.Equipment.cs` | Feature 提供 control binding、Skill 引用和稳定 Slot/Route/Feature/Parameter identity；删除 Persistent/Route flow root | 改入口 | 只接入已有 Equipment Core，不补 Corin 装备样例；保护 `Equip` |
| `character-equipment-runtime` | `EquipmentExecutionContracts.cs`、`EquipmentRuntimeControl.cs`、Fixed/Float EquipmentRuntime 和 LayoutCompiler | 代码控制选择 Route；保留 Begin/Commit/Cancel、Tag/Effect contribution、Feature generation、Action Equipment Context | 改 Route source | 不让 ActionInstance manager 接管装备选择；不复制 Equipment state；保护 `Equip`、`Pipeline` |
| `gameplay-simulation-pipeline` | `PipelineTransactionCoordinator.cs`、Fixed/Float evaluator 和 `SimulationKernel` | 保留 Ingress/Schedule/Step/Egress 与 Evaluate/WorldResolve/Finalize；控制和技能进入同一个 Step | 改 Step 内部调用 | 不创建第二执行循环；保护 `Pipeline`、`KCC` |
| `gameplay-simulation-session-composition` | `SimulationSessionCompositionDefinition.cs`、`SimulationSessionCompositionPreparation.cs`、`SimulationSessionCompositionCompatibility.cs`、GameplayLab Composition/Variant | Active 前锁定 control module、Skill closure、Target、容量和 layout；保留 World capability union 校验 | 改装配检查 | 缺模块、混版、ABI/Target 不匹配必须失败；不切 Solver；保护 `Pipeline`、World assets |
| `server-authoritative-prediction-correction-pipeline` | `Float32/Network/ServerAuthoritative/**`、`Networking/GameplayNetwork/ServerAuthoritative/**`、`ServerAuthoritativeCheckpointReconstructionModule.cs` | Owner baseline、Full/Delta、Correction 覆盖 control／ActionInstance／nested state；Remote 仍是观察体 | 改 checkpoint/layout | 不让 Remote 生成本机技能实例，不改变 Fantasy transport；保护 `AI`、`Pipeline` |
| `deterministic-rollback-network-model` | `DeterministicRollback/Pipeline/RollbackSnapshotRuntime.cs`、Rollback history/hash/codec、Fixed state codecs | Fixed snapshot、history、hash、restore 覆盖 control／SkillExecutionState；Relay 继续只路由 | 改 Fixed state schema | 不触碰 Deterministic KCC、Collision World、数值算法；保护 `KCC`、Fixed world assets |
| `btsmtl-graph-core` | `BaseGraphAuthoring.cs`、`BaseTreeAsset`、`StateMachineGraph.cs`、`GraphAuthoringFingerprint.cs` | Character 正式图范围改为 Skill closure；共享 Graph data、Port/Edge、Tree asset 基础保留 | 改 Character root policy | 不删除 AI/Pose 共用 Graph 基础，不提供 RootTree fallback；保护 `AI`、`Pose` |
| `graph-authoring-domain-framework` | `BtsmtlGraphAuthoringCapabilities.cs`、`GraphAuthoringCapabilityCatalog.cs`、`GraphAuthoringDomainContracts.cs`、`BtsmtlGraphAuthoringAdapters.cs` | Flow、Skill、Timeline、参数/变量、领域叶子向同一 Capability/Port Shape 投影；删除中央 Character 特例 | 改装配和领域 adapter | 不按 C# 类型名猜端口，不建立第二 catalog；保护 `Pose`、`AI` 现有 adapter |
| `graph-authoring-editor-shell` | `GraphAuthoringEditorShell.cs`、`BtsmtlSharedAuthoringWorkspaceRegistry.cs`、Navigator/Details/Canvas presenter | 保留共享窗口生命周期、selection、reload、订阅释放；新增 Skill/Control 导航 | 只改导航 projection | 不在 `OnInspectorGUI` 重计算；保护所有并行 Pose/Foot workspace |
| `btsmtl-agent-authoring-document-sync` | `AgentAuthoringDocumentCodec.cs`、`AgentAuthoringDocumentV4Exporter.cs`、V4 Models/Presentation Codec/Reconciler、TransactionServiceV4、PackageStore | 整包升级 v5，加入 `editable/skills/<canonical-id>/definition.json` 和 control binding；保持一份 hash、一份计划、五个生命周期工具 | 全量改读写闭环 | 删除 v4/v3 reader/writer，不增加局部 apply 或技能专用 MCP；保护 `AI`、`Pose` 文档分片 |
| `btsmtl-ai-controller-authoring` | `AIControllerAuthoring.cs`、`AIControllerDefinition.cs`、`AIControllerNodes.cs`、`AIControllerEditor.cs` | 只同步共享 Capability 和 v5 references；AI 继续产出 CharacterSimulationInput | 只改引用/校验 | AI 不读 control/skill private state，不扩 AI 行为；保护 `AI` |
| `btsmtl-runtime-diagnostics` | `RuntimeDiagnosticsContracts.cs`、`RuntimeDiagnosticsStore.cs`、`RuntimeDebugViewModel.cs`、`RuntimeDebugSession.cs` | 诊断区分 Control module/code source、SkillDefinition/SkillProgram、Actor、ActionInstance、subgraph call/generation | 改来源模型 | 不伪造 Graph node 表示 C# 状态，不参与 simulation 计算；保护独立 Foot diagnostics |
| `btsmtl-semantic-ir-inspection` | `CharacterSemanticIrInspectorWindow.cs`、Semantic IR artifact store/codec | Inspector 显示 control contract、Skill roots、closure、source map、build identity；保留只读 canonical artifact 观察 | 改只读 projection | 不现场编译、修改 IR 或读取 Unity runtime object；保护当前 `.csir` 直到新产物发布 |
| `unity-simulation-assembly-ownership` | `ThirdPersonSimulation.*.asmdef`、`ThirdPersonClient.Runtime.asmdef`、`ThirdPersonClient.Editor.asmdef`、`HotUpdateAssemblyLoader.cs`、HotUpdate manifest | 共享合同/解释器/Target/Unity adapter/可更新规则按单向依赖重排；保留现有 HybridCLR 发布基础 | 改程序集引用/发布清单 | portable 层不引用 Unity/Fantasy；不安装第二热更框架；保护当前启动和发布脚本 |
| `agent-ai-controller-synthesis` | `AgentAIControllerSnapshotExporter.cs`、Agent package models/validator、AI synthesis window | Agent 目标改为 v5 control input binding 与 AI 文档；Character control/Skill state 不开放给 AI | 改目标/schema | 不扩 AI 运行能力，仍只写正式 CharacterSimulationInput；保护 `AI` |
| `agent-character-controller-synthesis` | `AgentCharacterControllerSynthesisWindow.cs`、Agent authoring models、Graph snapshot/mutation handlers | Agent 生成目标改为控制配置、SkillDefinition、Skill graph closure；删除 RootTree 正文和旧输入镜像描述 | 改目标/schema/导航 | Pose Presentation 仍由原 owner；不让 Agent 生成任意 C# 调用节点；保护 `Pose` |
| `btsmtl-agent-authoring-mcp-bridge` | `BtsmtlAgentAuthoringMcpTools.cs`、`BtsmtlAgentAuthoringMcpJobScheduler.cs`、正式 checkout/dry-run/apply/validate bridge | 五个生命周期工具继续透传唯一 v5 整包；工具参数只表达整包 domain，不新增技能局部工具 | 改 payload/strict validation | 不通过 MCP 写 C# 或 YAML，不保留 v4 兼容桥；保护 `AI`、`Pose` |
| `character-state-timeline-authoring-loop` | Corin RootTree、StateBehaviorSubTree、Timeline、ActionProfile、InputProfile、`CharacterFixedInputTraceWorkflow.cs` | Corin Locomotion/Action 拆成 C# control＋七个 SkillDefinition；保留输入、连段、取消、Window、有限 Motion/Animation 结果 | 全量迁移 | 不重做正确 Pose/KCC/Foot；回放仅按正式 workflow；保护 `Replay`、`Pose`、`KCC` |
| `character-animation-presentation-authoring` | Corin Presentation Profile、Action source assets、`CharacterAnimationPresentationProfile.cs`、Projection producer binding | 来源导航改 Skill catalog/finite producer；保留有限 Action channel、Pose source、动画资源和表现 owner | 改 producer source identity | 不让 Gameplay control 决定动画播放，不改 Pose 算法；保护 `Pose` 资产和 Projection |
| `character-presentation-pose-graph` | `CharacterPoseStateMachineRuntime.cs`、`CharacterPoseProgramExecutor.cs`、`CharacterPoseFrameCoordinator.cs`、Pose Graph editor/capability | 只更新 v5/source navigation 与有限 Skill producer contract；保留现有 PoseStateMachine、Blend、IK、Foot、事务顺序 | 仅接口对账 | 不把 Locomotion/MovingTurn motion 搬进 Pose，不读 Timeline Gameplay Window；保护 `Pose` 全目录 |
| `character-pose-plan-compilation` | `CharacterPoseNodeDefinitionModule.cs`、Pose node definitions/catalog、Projection compiler/Program Image | 只同步 Document v5 引用和 Skill producer source；保留唯一 Definition Adapter、typed lowering、Program Image 和 runtime ABI | 仅接口对账 | 不改 Pose 数学、Image layout、Foot/IK/Camera；保护 `Pose`、Projection asset |

### 并行 change 的硬边界

- `rebuild-btsmtl-preview-with-scene-play` 只消费本 change 最终提供的精确 Skill 选择、正式动作输入、只读 ActionInstance/SkillExecution observation 和调参合同；它仍负责场景创建、播放控件、运行权限、fixture 和旧预览播放器。本 change 不实现这些内容，不修改 `Runtime/Character/Pipeline/Unity/PreviewSession.cs`、`CharacterPipelineAuthoringPreviewController.cs`、`CharacterEquipmentPreviewFixture.cs` 的场景流程。
- `Pose` owner 继续负责 `CharacterPoseStateMachineRuntime`、`CharacterPoseProgramExecutor`、`CharacterPoseFrameCoordinator`、Pose Graph/Projection compiler、AnimationSlot、Inertialization、FootPlacement、FullBodyIK、Camera 和渲染。Gameplay 只提交 committed Body/Intent/finite action producer，不能反向写 Pose state、Transform 或播放器。
- `KCC` owner 继续负责 `Runtime/Simulation/DeterministicKcc/**`、`CorinDeterministicKcc.asset`、`CorinDeterministicCollisionWorld.asset` 和 World Solver。MovingTurn 只改变 motion request 的来源合同，不复制 KCC、MotionWarp 或曲线求值。
- `Equip` owner 继续负责唯一 Equipment aggregate、事务和 Presentation visual selection。本 change 只把已有 Feature Route 的入口换成 control binding/Skill reference；当前 Corin 没有有效 Equipment 数据，禁止补样例。
- `AI` owner 继续负责 AI RootTree、Perception、AI Blackboard 和 `AIIntentProgram`；唯一写边界仍是 `CharacterSimulationInput`，不允许 AI 读取或写 control/Skill private state。

## 基线阶段结论

- 1.1：当前只具备运行程序集和关键资产的候选身份；DLL 不能从源码重建，tracked dirty patch、本地包内容身份以及 Development Center 的 source-before/editor receipt 仍缺失，保持未完成。
- 1.2：已按 8 个 Locomotion 状态、16 条 Transition、7 个激活入口、8 个 Timeline、23 个 TreeClip、23 个 Window fact、Blackboard 输入/变量和当前 Equipment 空入口完成源到目标映射；空 Body 的 stopping 状态已修正为 `WalkStopping/RunStopping`，旧 Graph identity 只作 Source Map，新代码 identity 待实施登记。正式实现仍须由后续任务以 Replay 核对 identity/generation/barrier/Pose fact/Body 输出。
- 1.3：已完成 proposal 中 2 个 New＋31 个 Modified capability 的实际入口、接口替换／保留、触碰范围和 Preview／Pose／Equipment／AI／KCC／Pipeline 保护边界矩阵。
- 1.4：2026-09-07 更新：trace `f169da25c67742aaafa0e9860ae4a230` 已完成多次结果封口——标准 replay 于同一 Program 身份下连续两次 1492 帧逐帧 `matched`（proof `20260907-100726-718` 对 `20260907-051700-925`）；Projection 字面量修复重建后（ProjectionRevision `06ae25d7…`）再次完整回放 1492 帧、`divergent_frame_count=0`，`projection_revision` 字段差异为身份护栏预期（baseline 自动重建，见 `20260907-125020-340`）。表现层配套证据：修复后采样（`Diagnostics/GeneratedPresentationSampling/20260907-044940-…`）motion 状态机出现全链路（idle 424/WalkStart 66/WalkLoop 60/RunLoop 98/RunEnd 128 帧）。跨 ABI 双 Target 同输入比较仍未执行，保持未完成。另：locomotion 迁移字面量旧 GUID 错位曾导致状态机恒 idle（c262837fb 修复），是控制状态身份手抄字符串风险的实证，2.1b 交付代码常量后消除。

## 2026-09-07 事实补充（控制层载体与诊断基础设施）

- 控制状态机运行时骨架决定：项目 fork UnityHFSM 为本地嵌入包 `Packages/UnityHFSM`（remote=nikaakaa/UnityHFSM，manifest `file:UnityHFSM`，原上游引用已移除）；时序由仿真 Tick 注入。任务见 2.1/2.1a/2.1b。
- 表现复制采样补齐三块观测列并已提交：事实帧列（`CharacterPresentationFactCaptureFrame`，movement-mode/播放时钟 owner，61ce9ee6f/4df9ad182）、迁移规则逐操作输入表（`transition-rule-evaluations`，ac20ee980）、状态机 pending target 与目标 provider 状态列（b58242da9）。诊断入口 `character.presentation_replication_diagnostics`（full 采样器）。
- 回放视角对齐：trace schema 升 v4 记录录制起始相机航向、回放开始时恢复（27036267c）；v3 旧 trace 兼容读取。live 输入的相机相对转换链经采样与 trace 逐帧解码核实为正确（540 移动帧世界向量连续分布，208 个不同角度）。
- 已知待删旧结构清单（2.1b/3.1/4.1 完成后执行）：`CorinPlayableRootTree.asset` 内控制状态机 authoring；Discovery 的控制树发现分支与 locomotion 节点发射；Definition 的 `m_RootTreeAsset`（根树瘦身为纯 Skill 入口）；姿态图 `movement-mode.state/…` 手抄字面量。
