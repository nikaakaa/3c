## Why

Corin当前的数据来源分散在外部Dump、Unity正式作者资产、BTSMTL Document、Character Build产物、Session配置和固定输入Replay之间，缺少一条能审计来源、身份、依赖和运行结果的完整闭环。现在Pose/Agent作者重构正在改变资源归属，正好需要把Dump source重新定义为上游输入，把Corin正式配置和Replay固定为同一组可追溯版本。

## What Changes

- 新增Corin Dump Source Manifest，记录Dump根目录、来源版本、文件哈希、模型/Rig、AnimationClip、Binding、Foot Analysis和渲染/运行快照的来源关系；运行时不直接读取Dump路径。
- 定义Corin最终配置闭包：Definition、Control/Input、Skill/Action、GameplayEffect/Tag/Attribute、Motion、Presentation/Pose、Foot/IK/Blend、Timeline、Session Composition、Prefab/Scene和生成产物的唯一依赖方向。
- 将Camera Profile、Default Sequence、Camera Curve、Action Camera Sequence、镜头过渡/碰撞/锁定/Zoom/Stretch/Shake参数纳入Presentation闭包，并让Skill Timeline的Camera/Scene请求与Camera配置使用稳定引用。
- 将Dump资源先归一化为正式Unity作者资产，再由Definition引用；禁止用生成Program、Projection或Dump文件反向充当作者配置。
- 将Corin的Float32、Fixed、Rollback和Server Authority目标与各自Program、Projection、Session、Prefab/Scene和网络Adapter身份绑定，禁止跨目标混用产物。
- 将固定输入Trace、运行版本、Program/Layout/Projection hash、初始Actor/World配置和Runtime Dump绑定为Replay Request；Replay只消费正式产物和固定输入，不创建临时执行器或第二套运行链。
- 规定串行闭环：Dump source核对 → 正式作者配置 → Document checkout/dry-run/apply → Character Build → Session/Play → Runtime Dump与Replay → A/B比较和交付汇总。
- **BREAKING**：禁止通过目录扫描、显示名、临时Dump路径、旧生成产物或其他worktree文件补齐Corin依赖；缺少来源、身份、哈希或正式owner时必须拒绝Build/Replay。

## Capabilities

### New Capabilities

- `corin-dump-authoring-replay-closure`：管理Corin外部Dump来源、正式作者配置闭包、生成产物、运行配置、固定输入Replay和证据链。

### Modified Capabilities

- `agent-character-controller-synthesis`：Corin Document需要保留正式配置闭包的来源身份、owner和可重建依赖，不能把Dump或生成产物当作编辑真相。
- `character-pipeline-definition-authoring`：Definition成为Corin正式依赖闭包根，并记录来源资产与生成产品的稳定身份边界。
- `character-pipeline-runtime`：Runtime Actor/Program/Projection注册必须绑定同一Corin配置与产物身份，禁止混用不同Dump或Build版本。
- `gameplay-simulation-session-composition`：每个Corin Replay必须固定精确Session Composition、Program Runtime、Backend、Pipeline、Solver、Session Source和Numeric Target。
- `btsmtl-runtime-diagnostics`：Runtime Dump、Action/Skill generation、Timeline、SourceMap、State/Snapshot hash必须能绑定到同一Replay Request和Build身份。

## Impact

影响Corin Dump索引与来源校验、Character Definition/Presentation/Skill配置、BTSMTL v7 Document闭包、Character Float32/Fixed Build入口、Rollback/Server Authority Session配置、Runtime Diagnostics、固定输入Replay和3C Development Center验证记录。需要新增正式manifest/identity合同、串行Gate和失败诊断；不把外部Dump、生成Program或Replay数据写进运行时作者配置，不新增第二套编译、网络或回放执行器。
