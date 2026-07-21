# Change: 增加 Local Fixed Gameplay Lab 与手感验收闭环

## Why

当前项目已经有可运行的 Local Float32 + Unity CharacterController、完整 Fixed Program、确定性碰撞产物、Deterministic KCC、Rollback Fixed Pipeline、MotionWarp、Foot Placement IK、Timeline Live Debug 与 Walk/Run Animation Marker Sync。缺口不是再实现一套 KCC，而是这些能力没有一个不依赖 Relay、远端 Peer 或商业启动流程的统一单机观察入口。

现有 Fixed Unity 装配仍物理位于 `DeterministicRollback` 程序集，并把通用 Fixed Actor registration、Presentation output、diagnostics aggregate、Fixed Program Runtime、Backend 与 KCC Definition绑在 Rollback source、history、committer 和 endpoint 上。直接以“单 Peer Rollback”作为本地测试会继续要求网络模型身份，也无法证明 Fixed Program 与 KCC可独立复用。复制一份 Local Fixed Composer则会形成第二套 Fixed Session构造与输出提交路径。

当前正式玩法场景是 `Assets/Scenes/Standalone/StandaloneGameplay.unity`，商业启动 change 和实际 Editor launcher也使用该路径；`openspec/project.md`及商业 change局部又写成不存在的`Assets/Scenes/Sandbox/SandBox.unity`。Gameplay手感调试也不应要求经过版本更新、登录、主页和分包下载。本change增加一个Editor/Development用途的正式`GameplayLab`，但不把它变成产品唯一入口，也不删除或替换任何网络模型场景。

本change把现有 Fixed基座从Rollback所有权中抽出来，增加真正的Local Fixed Source与标准Fixed Local Pipeline，并让同一个Gameplay Lab场景通过显式、运行前锁定的Session Variant分别装配Float32/Unity CC和Fixed/Deterministic KCC。KCC改动以参数、诊断和实际手感收口为主；只有实机证据证明现有碰撞语义有缺陷时才修改Motor或query kernel。

## What Changes

- 新增独立`Assets/Scenes/GameplayLab/GameplayLab.unity`，复用现有CharacterMovementTestEnvironment、OpenKCCMovementCourse、Corin角色、训练目标和唯一Presentation链。
- 增加显式`GameplayLabSessionVariantDefinition`。场景只引用一个Variant资产；Variant完整引用一个Session runtime root与Composition，不使用enum、默认值、场景搜索或运行中切换。
- 提供`Local Float32 + Unity CharacterController`和`Local Fixed + Deterministic KCC`两个正式Variant。前者复用现有Local Float链，后者不创建Rollback Model、Relay、Endpoint、Peer、history或远端Actor。
- 将Fixed Program Runtime、Fixed Backend、Fixed Actor registration合同、Fixed output/diagnostics aggregate、Fixed Presentation output和Fixed Composer降低到model-neutral Unity Fixed程序集。
- 让DeterministicRollback保留自己的Source、Pipeline、restore/history、endpoint、network diagnostics与commit policy，并通过同一model-neutral Fixed Composer进入现有Fixed Program和KCC。
- 增加Local Fixed Session Source与Standard Fixed Local Pipeline，沿正式Input Ingress、单步Schedule、Program Evaluate、World Solve、Finalize、Immediate Commit顺序运行。
- 增加model-neutral Fixed Character Host与显式Fixed Control Source/Presentation Role装配。Local Fixed玩家使用Unity设备输入；训练目标使用Neutral输入；Rollback只在model adapter层绑定本地Peer输入与网络诊断。
- Local Fixed的ActionTarget输入消费`add-btsmtl-ai-controller-authoring`安装的唯一Committed Actor Observation port，不读取Visual Transform，也不新建第二份Actor Body registry。
- MotionWarp最终请求继续进入Deterministic KCC；墙体、墙角、薄墙和不可达目标只能裁剪Warp请求，不能由Presentation或Transform补偿完成。
- 把KCC接触、坡面、台阶、Ground Snap、容量与迭代参数保留在正式DeterministicKccWorldSolverDefinition，并确保全部影响结果的值进入ConfigurationHash、Solver Identity与World Identity。
- 增加只读Gameplay Lab诊断视图，明确显示Variant、Numeric Target、Source、Pipeline、Solver、collision world hash、KCC configuration hash、Actor committed body、MotionWarp trace、Foot Placement与Animation Marker Sync状态。
- 修复Program SourceMap的作者内容版本合同：Graph与Timeline fingerprint随每条source entry进入Semantic IR和Float/Fixed Program artifact，Runtime Debug按Timeline/Track/Clip优先级建立容器，确保Live Debug既能定位source又能拒绝过期作者内容。
- 修正项目文档中不存在的SandBox路径：产品入口继续进入StandaloneGameplay；GameplayLab只作为Editor/Development技术展示与手感调试入口。
- 将单机Gameplay Lab、双端网络验证、正式Published Player和Editor Bootstrap Play收口到唯一`Tools/3C/Launcher`四个业务分组；删除旧Standalone直进、独立产品构建窗口和分散网络测试菜单，但保留各自正式运行链与产品边界。
- 记录全部未归档change的代码一致性、实机验证缺口与并行文件边界，不归档任何change。

## Scope

### In Scope

- Local Fixed Session Source、Pipeline、Composer与Unity actor装配。
- Fixed通用代码从Rollback程序集所有权中抽离。
- 一个Gameplay Lab场景与两个显式Session Variant。
- 复用现有Fixed Program、collision artifact、KCC、Corin Timeline和Presentation链。
- KCC手感参数、身份、诊断与实际观察入口。
- Local Fixed MotionWarp目标快照和KCC裁决链。
- Timeline、步态相位匹配、Foot Placement IK和MotionWarp的同场景只读诊断。
- Editor启动入口统一，以及本地无资源链与产品正式资源链的显式隔离。
- 当前未归档OpenSpec changes审计与冲突记录。

### Out of Scope

- 删除、合并或替换DeterministicRollback、ServerAuthoritative Unity Solver、DotRecast Solver或其它网络模型。
- 把GameplayLab设为Release Player、商业启动或Home后的唯一玩法场景。
- 重新实现Deterministic KCC已有连续胶囊查询、坡面、台阶、墙滑和Actor collision基础能力。
- Moving Platform、Rigidbody动力学、NavMesh绕路或MotionWarp障碍绕行。
- 把Animation Marker Sync描述为Motion Matching、Pose Search或Stride Warping。
- 完整AI、命中、伤害、锁定选择、账号、Relay或CDN。
- 为并行Equipment、AI Controller或Agent authoring change实现其尚未批准的业务内容。

## Impact

- Affected specs:
  - 新增`local-fixed-gameplay-lab`
  - `gameplay-simulation-session-composition`
  - `deterministic-kcc-world-solver`
  - `btsmtl-runtime-diagnostics`
  - `btsmtl-compiled-simulation-program`
  - `dotrecast-authoritative-server-backend`
  - 进行中的`client-startup-resource-delivery`
- Affected runtime:
  - Unity model-neutral Fixed composition与actor registration。
  - DeterministicRollback Unity adapter、runtime launcher与diagnostics binding。
  - Local Fixed Source、Pipeline与input/output ports。
  - GameplayLab bootstrap、Variant、runtime root与诊断。
- Affected assets:
  - 新增GameplayLab scene与两个Variant/runtime root资产。
  - 复用Corin Fixed Program、CorinDeterministicKcc、collision world artifact、CharacterMovementTestEnvironment与OpenKCCMovementCourse。
- Breaking changes:
  - Fixed Unity类型离开`DeterministicRollback`命名空间和程序集后，现有Rollback资产与源码必须原子迁移到新正式命名，不保留旧类型、wrapper或兼容reader。
  - Rollback Character Host中按Endpoint判断Local/Remote的装配职责被拆到Rollback adapter；model-neutral Fixed Host不再引用Endpoint。
  - Program SourceMap新增作者内容hash后，Semantic IR、Float Program与Fixed Program artifact版本必须提升；旧artifact直接拒绝并由正式编译链重建，不保留旧reader。
  - project文档中的SandBox产品路径被删除，StandaloneGameplay与GameplayLab职责重新写清。

## Current Spec Comparison

- `gameplay-simulation-session-composition`已经规定五项显式Composition、numeric-neutral Host和target-specific Composer，但当前只给出Local Float32示例；实际Fixed Unity Composer又由Rollback程序集拥有。本change补充Local Fixed组合，并用程序集依赖保证Fixed Composer不认识Rollback。
- `gameplay-network-model-boundary`已经规定Local不是Network Model。本change落实这一边界：Local Fixed没有Model Definition、Endpoint、Transport、Peer、history或rollback pass，不用单Peer网络会话伪装Local。
- `deterministic-kcc-world-solver`的Requirement已要求portable World Solver、正式配置哈希和fail-closed，但Purpose仍把它描述成`DeterministicRollback Fixed Target`专用。本change把其所有权明确为Fixed Numeric Target可复用World Solver；Rollback只是一个消费者。
- `character-targeted-motion-warp-demo`已经要求目标来自最近提交的逻辑Body并由唯一WorldSolver裁决。当前运行实测只能看到攻击发生和角色靠近目标，Live Debug仍停在Shared Asset，未形成Applied/Clamped/Blocked证据。本change不另建Target registry，等待并消费AI Controller change定义的唯一Committed Actor Observation port。
- `add-program-motion-modifier-warping`、`add-predictive-foot-placement-presentation-pass`和`add-timeline-animation-marker-sync`任务均已完成，但“任务完成”不等于GameplayLab中Float与Fixed两种Variant都通过实机观察。本change只建立共同观察入口和诊断，不复制这些runtime。
- `openspec/project.md`和商业change中的SandBox描述与实际资产、launcher和商业spec冲突。本change必须把产品Standalone与开发GameplayLab分开，不能再创建第三个Sandbox名称。
- `dotrecast-authoritative-server-backend`把Unity Authority的Build/Run场景写死为分散菜单。本change只把按钮位置迁入统一Launcher，不改变Build不启动、Run不编译以及模型产物隔离合同。

## Dependencies And Parallel Work

- 依赖已完成的`add-program-motion-modifier-warping`、`add-corin-targeted-motion-warp-demo`、`add-predictive-foot-placement-presentation-pass`、`add-timeline-animation-marker-sync`、`add-character-vertical-body-motion`与`refactor-deterministic-rollback-input-propagation`作为唯一运行基线。
- 依赖进行中的`add-btsmtl-ai-controller-authoring`安装model-neutral Committed Actor Observation port，并完成玩家MotionWarp目标provider迁移。本change不在其完成前实现第二份观察缓存或Transform读取。
- `extend-agent-authoring-for-ai-controller`与`add-corin-training-ai-demo`依赖AI Controller change。本change不修改Agent schema、AI Graph或训练敌人AI Control Source。
- `add-character-equipment-feature-modules`正在修改Character Semantic compiler与definition资产。本change避免修改其正在占用的Semantic Emitter、Frontend Compiler、CharacterPipelineDefinition与Corin authoring资产，直到并行change释放边界。
- `add-commercial-client-startup-showcase`只剩Endpoint和字体/图集正式配置。本change不进入商业启动链，也不填充CDN/Auth假配置。

## Success Criteria

- `GameplayLab.unity`是一个独立Editor/Development场景，不改变Release产品进入StandaloneGameplay的链路。
- 同一GameplayLab可以在Play前通过显式Variant资产稳定选择Local Float32/Unity CC或Local Fixed/Deterministic KCC；Active Session不热切换。
- Local Fixed创建完整Session时不实例化DeterministicRollback Model、Endpoint、Relay、Peer、history或远端Actor。
- Local Fixed与Rollback装配同一Fixed Program identity、collision world hash、KCC configuration hash与WorldSolver identity，不存在复制的Fixed Composer或KCC。
- Fixed Actor registration、output、diagnostics、Program Runtime、Backend和Composer位于model-neutral Fixed Unity程序集；Rollback只拥有模型差异。
- MotionWarp目标来自唯一Committed Actor Observation，Warp displacement继续由KCC裁剪并提交，不读取视觉Transform或直接写角色Transform。
- GameplayLab能观察起步、加减速、急停、转向、空中、落地、坡面、台阶、墙角、薄墙、狭窄通道、墙滑与Actor碰撞；调校值来自正式配置并进入身份/哈希。
- Timeline Live Debug能绑定实际运行实例并显示MotionWarp source、target snapshot、progress、requested/applied displacement与solver disposition。
- 同一Presentation链能观察Timeline animation、Walk/Run步态相位匹配、MotionWarp与Foot Placement IK，且诊断不把它们错误命名为Motion Matching或Stride Warping。
- 全部未归档changes有状态、代码一致性、实机验证缺口和并行边界记录；本change不自动归档它们。
