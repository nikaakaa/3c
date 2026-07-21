# 未归档Change审计

## 审计口径

- `openspec validate --all --strict --no-interactive`在加入本change后为84 passed、0 failed。
- 任务勾选只说明文档声明完成，不自动等于Unity实机验证完成。
- 本表不归档change，也不替其它并行change补写业务实现。
- 共享工作树中的Character Semantic Emitter、Frontend Compiler、CharacterPipelineDefinition与Corin资产由并行Equipment/AI工作占用，本change不覆盖。
- 审计期间共享工作树继续出现AI、Equipment、Control Source、Float Local Pipeline、Fixed KCC与CharacterMovementTestEnvironment改动；这些不是本change产生。文件状态是动态并行快照，不能仅凭tasks计数判断代码是否开始实施。

## 2026-07-21即时编译快照

- `ThirdPersonSimulation.Core.csproj`、`ThirdPersonSimulation.Fixed.csproj`、`ThirdPersonSimulation.DeterministicRollback.Endpoint.csproj`与`ThirdPersonSimulation.Fixed.Unity.csproj --no-dependencies`均使用禁用build server、禁用共享编译的正式参数构建通过，0 Warning、0 Error；每轮构建后均执行`dotnet build-server shutdown`。
- Gameplay Lab runtime从预定义`Assembly-CSharp`迁入`ThirdPersonClient.Runtime`后，`ThirdPersonClient.Runtime.csproj --no-dependencies`构建通过，只有既有`CharacterInputValueNodes`未使用字段Warning。Editor launcher由`ThirdPersonClient.Editor`拥有。
- Unity通过computer-use真实刷新后，外部AI、Equipment与Presentation编译错误已经清除；当前5个错误全部来自DeterministicRollback旧Source/registration尚未适配新的model-neutral Fixed合同。
- Character Semantic Emitter、Frontend Compiler以及两个Rollback旧类仍被并行文件占用保护拒绝写入。本change只等待并追加正式合同，不复制文件、不建立旁路实现。

## 状态矩阵

| Change | 当前任务 | 与Gameplay Lab关系 | 当前判断 |
|---|---:|---|---|
| add-btsmtl-ai-controller-authoring | 0/205 | 定义唯一Committed Actor Observation并迁移玩家目标provider | 强依赖；tasks未勾选但共享代码已开始，不得复制Observation port |
| extend-agent-authoring-for-ai-controller | 0/85 | 扩展Agent AI authoring | 后续依赖；不修改Agent schema |
| add-corin-training-ai-demo | 0/71 | 将Neutral训练敌人替换为AI Control Source | 后续依赖；Gameplay Lab先保留Neutral目标 |
| add-character-equipment-feature-modules | 14/313 | 同时覆盖Float/Fixed Character compiler与definition | 并行冲突；共享代码明显超过勾选状态，避开其占用文件与Corin资产 |
| add-timeline-animation-marker-sync | Complete | Walk/Run步态相位匹配 | 代码/资产完成；缺Gameplay Lab两Variant实机证据 |
| add-character-vertical-body-motion | Complete | 空中、落地与Solver capability | 代码/资产完成；缺Local Fixed实机手感证据 |
| refactor-client-build-artifact-layout | Complete | 商业构建目录 | 与Gameplay Lab runtime无直接依赖 |
| add-predictive-foot-placement-presentation-pass | Complete | Foot Placement IK | 代码/资产完成；缺Gameplay Lab两Variant最终表现证据 |
| refactor-deterministic-rollback-input-propagation | Complete | Fixed input、history、request timing基线 | 必须保留；Local Fixed不得恢复旧input delay或codec |
| refactor-timeline-authoring-preview-to-presentation-only | Complete | Preview只做表现，Gameplay Warp只在正式Session | 必须保留；不创建Preview Gameplay Session |
| add-commercial-client-startup-showcase | 462/466 | 产品启动、资源、认证、Home、Gameplay preload | 剩HTTPS/WSS和字体/图集正式配置；Gameplay Lab不绕过产品作为fallback |
| add-corin-targeted-motion-warp-demo | Complete | 目标输入、训练敌人、五段Warp | 静态任务完成；当前Live Debug未绑定runtime实例，实机证据不完整 |
| add-program-motion-modifier-warping | Complete | Float/Fixed MotionWarp runtime | 底层完成；需Local Fixed与障碍场景实机证据 |
| refactor-character-visual-trajectory-following | Complete | committed body到视觉跟随 | 必须保持Presentation不反写Gameplay |
| refactor-character-presentation-runtime-modules | Complete | 唯一Presentation模块链 | Gameplay Lab两个Variant必须复用，不复制Presenter |
| refactor-simulation-tick-hot-path | Complete | Tick热路径 | Local Fixed Pipeline必须沿正式Tick，不建Update loop |
| refactor-gameplay-runtime-and-tooling-modules | Complete | runtime/tooling程序集边界 | 新Fixed程序集与Gameplay Lab Editor入口必须遵守 |

## 已发现矛盾

### tasks状态滞后于并行代码

- `add-btsmtl-ai-controller-authoring`仍显示0/205，但共享工作树已新增AI Editor/Runtime目录，并修改Graph capability、Control Source、Actor target provider、Local input pipeline和Actor registration等文件。
- `add-character-equipment-feature-modules`仍显示14/313，但共享工作树已新增Equipment execution/layout代码，并修改Float/Fixed State、Action、GameplayEffect、Program semantics和KCC相关文件。
- `DeterministicKccConfiguration`、`DeterministicKccMotor`、`DeterministicKccWorldSolver`与`CharacterMovementTestEnvironment.prefab`也处于并行修改中，当前无法从Git状态单独归属到本目标。

处理：本审计只记录“代码进行中、tasks未同步”，不替并行Agent勾选；Local Fixed实施必须等待这些交叉文件稳定并重新读取最新合同。

### 实机证据文件

- 在本轮审计前，全部未归档change都没有`runtime-validation.md`。
- Warp、Foot Placement、Marker Sync、Vertical Body Motion等change即使显示Complete，也只有任务勾选或implementation inventory，没有统一记录真实Play Mode的输入条件、截图、Console和诊断结果。
- 本change新增的`runtime-validation.md`是当前第一份显式实机矩阵；它只记录看到的事实，不倒推其它change已经验收。

### 产品场景路径

- 实际存在并被代码使用：`Assets/Scenes/Standalone/StandaloneGameplay.unity`。
- 实际不存在：`Assets/Scenes/Sandbox/SandBox.unity`。
- `openspec/project.md`把SandBox写成产品与本地测试唯一场景。
- 商业change的proposal/design同时写了StandaloneGameplay产品链与SandBox本地入口。
- 商业change tasks、spec和Editor launcher实际使用StandaloneGameplay。

处理：本change明确保留StandaloneGameplay产品职责，新增GameplayLab开发职责，并删除SandBox名称，不制造第三条Sandbox路径。

### Fixed所有权

- Core已有model-neutral`FixedSimulationSessionComposer`。
- Unity Fixed Program Runtime、Backend、registration、output、diagnostics、KCC Definition与Composer仍在DeterministicRollback目录/命名空间。
- `UnityFixedSimulationSessionComposer`硬要求Rollback Prepared Source、Rollback Pipeline与Rollback state。

处理：抽离唯一model-neutral Unity Fixed composition；Local与Rollback只在Source/Pipeline/ports不同。

### MotionWarp目标真相

- 已完成Warp Demo保存最近提交Body供目标provider读取。
- 进行中的AI Controller change计划安装统一Committed Actor Observation，并删除旧provider路径。
- 当前共享代码已经出现`CommittedActorObservation`、snapshot与read port，但它们仍位于`Simulation/Core/Float32/AI`，Local Float Input Ingress已开始消费；这不是Fixed可引用的model-neutral合同。

处理：本change依赖AI change把最终Observation snapshot/port降到model-neutral Core。Fixed不得反向引用Float32，也不新增Fixed专用缓存；在该边界稳定前，Fixed Player target input任务保持未完成。

### 实机任务状态

- Warp、Foot IK、Marker Sync changes均显示Complete。
- 当前computer-use只能确认StandaloneGameplay进入Play、左键Attack发生、角色接近训练目标。
- Timeline Live Debug仍显示`Source: Shared Asset`与`None (Timeline)`，没有证明runtime实例中的Applied/Clamped/Blocked。
- Game View鼠标锁会把computer-use点击位移同时当作Look delta，造成镜头突变。

处理：Gameplay Lab增加走正式Input port的键盘Attack binding和只读诊断；实机结果写入runtime-validation.md，不把手动验证伪装成tasks勾选。

### Timeline Live Debug来源合同

- 真实Play Mode中目标菜单能列出玩家和训练敌人两个runtime实例，但二者均显示`source missing`。
- Timeline产生的SourceMap条目同时携带owner Graph/Node和Timeline/Track/Clip身份；旧Runtime Debug优先把条目解释成Node，导致运行时SourceMap没有Timeline容器。
- 旧SourceMap还把整个ProgramHash写入每条source；Timeline Editor使用Timeline资产指纹比较，因此即使修正身份优先级也会变成`revision mismatch`。

处理：SourceMap正式增加每个作者容器的content hash，Timeline身份优先于owner Node身份，runtime映射要求同一容器hash一致且不得缺失；Semantic IR、Float Program、Fixed Program和string table格式同时递增，Frontend Compiler版本递增使旧产物明确失效。当前代码链已经写入大部分字段与codec，等待并行占用释放后补齐fail-closed检查、compiler版本和正式重生成。

## 商业change剩余配置

- ResourceEndpoint必须使用正式HTTPS地址。
- AuthEndpoint必须使用正式WSS地址。
- Core字体必须配置。
- 字体、材质与图集pack规则必须正式配置。

这些配置不影响Gameplay Lab离线运行，本change不填写假CDN/Auth值，也不把Gameplay Lab当商业启动fallback。
