## Why

2026-09-12 拆分接收：旧refactor-btsmtl-flowcanvas-authoring的4.5.5网络Pass／Adapter收尾和8.1正式Skill运行证据归本change第6节，最终汇总归5.1。FSM与资产迁移由integrate-native-fsm-skill-authoring负责，通用图观察由finish-skill-runtime-observation负责；本change不再反向要求旧总任务保持active。

Corin当前的数据来源分散在外部Dump、Unity正式作者资产、Gameplay Graph产物、领域／表现binding、Session配置和固定输入Replay之间，缺少一条能审计来源、身份、依赖和运行结果的完整闭环。现在Pose与C#作者链正在改变资源归属，正好需要把Dump source重新定义为上游输入，把Corin正式配置和Replay固定为同一组可追溯版本。

## What Changes

- 接收Corin正式Rollback/Server Authority Pass与Adapter的剩余集成及载荷核对，复用既有Composition和运行入口；检查只传Input、Canonical Request、Hash/Snapshot，不复制作者Graph、Blackboard名称、Timeline对象或最终Pose。
- 将Skill启动、Timeline等待、正常结束、取消和中断链证据并入现有Runtime Dump/Replay闭环；来源绑定同一发布产物，不以结构兼容或旧Build结果代替本次运行结果。

- 新增Corin Dump Source Manifest，记录Dump根目录、来源版本、文件哈希、模型/Rig、AnimationClip、Binding、Foot Analysis和渲染/运行快照的来源关系；运行时不直接读取Dump路径。
- 定义Corin最终配置闭包：Definition、Control/Input、Skill/Action、GameplayEffect/Tag/Attribute、Motion、Presentation/Pose、Foot/IK/Blend、Timeline、Session Composition、Prefab/Scene和生成产物的唯一依赖方向。
- 将Camera Profile、Default Sequence、Camera Curve、Action Camera Sequence、镜头过渡/碰撞/锁定/Zoom/Stretch/Shake参数纳入Presentation闭包，并让Skill Timeline的Camera/Scene请求与Camera配置使用稳定引用。
- 将Dump资源先归一化为正式Unity作者资产，再由Definition引用；禁止用Graph产物、领域／表现binding或Dump文件反向充当作者配置。
- 将Corin的Float32、Fixed、Rollback和Server Authority目标与各自Graph artifact、Domain Binding Set、Presentation Binding、Session、Prefab／Scene和网络Adapter身份绑定，禁止跨目标混用产物。
- 将固定输入Trace、运行版本、GraphArtifactHash、DomainBindingSetHash、PresentationBindingHash、初始Actor／World配置和Runtime Dump绑定为Replay Request；Replay只消费正式产物和固定输入，不创建临时执行器或第二套运行链。
- 保留通用动画框架按资源选择`NativeClip`或ACL的能力，将Corin全部Locomotion与Action作者Clip统一显式构建为同一ACL资源组；Timeline动作样本携带精确Clip身份并解析到该组，ACL缺失或过期时拒绝运行而不回退NativeClip。
- 规定串行闭环：Dump source核对 → 正式作者配置闭包 → Graph／领域／表现产物准备 → Session／Play → Runtime Dump与Replay → A／B比较和交付汇总。
- **BREAKING**：禁止通过目录扫描、显示名、临时Dump路径、旧生成产物或其他worktree文件补齐Corin依赖；缺少来源、身份、哈希或正式owner时必须拒绝Build/Replay。

## Capabilities

### New Capabilities

- `corin-dump-authoring-replay-closure`：管理Corin外部Dump来源、正式作者配置闭包、生成产物、运行配置、固定输入Replay和证据链。

### Modified Capabilities

- `character-pipeline-definition-authoring`：Definition成为Corin正式依赖闭包根，并记录来源资产与生成产品的稳定身份边界。
- `character-pipeline-runtime`：Runtime Actor必须绑定同一Definition、Graph artifact、领域／表现binding与Session身份，禁止混用不同Dump或产物版本。
- `gameplay-simulation-session-composition`：每个Corin Replay必须固定精确Session Composition、Domain Runtime、Backend、Pipeline、Solver、Session Source和Numeric Target。
- `btsmtl-runtime-diagnostics`：Runtime Dump、Action/Skill generation、Timeline、SourceMap、State/Snapshot hash必须能绑定到同一Replay Request和Build身份。

## Impact

影响Corin Dump索引与来源校验、Character Definition／Presentation／Skill配置、正式C#作者资产、ACL显式构建发布、Pose Source／Animation Slot动作采样、Float32／Fixed Graph与领域binding准备、Rollback／Server Authority Session配置、Runtime Diagnostics、固定输入Replay和3C Development Center记录。需要新增正式manifest／identity合同、串行Gate和失败诊断；不把外部Dump、运行产物或Replay数据写回作者配置，不新增整角色编译、网络或回放执行器。
