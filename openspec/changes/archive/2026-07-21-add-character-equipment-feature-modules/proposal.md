# Change: 增加角色装备功能核心

## Why

当前 Character 管线已经把 RootTree、StateMachine、Timeline、Action、Gameplay Effect、Motion 与 Animation Presentation 编译进唯一 Gameplay Program，但“装备会替换哪些动作、参数、持续行为、动画需求和外观”仍没有正式所有权。Corin 现有武器能力只能隐含在 RootTree、平铺 ActionProfile、角色 Prefab 和 Animation Profile 中；再增加枪械或其它武器时，作者必须继续修改角色主树，并很容易在 MonoBehaviour、独立 Ability runtime 或 Animator 调用中形成第二条业务路径。

本 change 先建立一个可独立实施的 Equipment Core。`CharacterEquipmentProfile`负责槽位、动作路由、装备、Feature 与初始装配；`CharacterEquipmentFeatureDefinition`负责可静态链接的持续行为图、动作路由图、类型化参数、局部状态、Action、Tag/Effect 与表现需求；`CharacterEquipmentPresentationProfile`负责装备物体的 Unity 表现绑定。Compiler 将这些输入与 Character RootTree 一起编译成同一 Semantic IR、Float32/Fixed Program、Character State 和 Presentation Projection。

这一步只安装通用能力，不迁移或改写 Corin authoring 资产，不更新 Agent schema、MCP bridge 或 `btsmtl-agent-authoring` 技能，也不修改任何具体网络模型。正式编译器可以按新 Operation Set 重发 Corin generated Program/Projection，但其中 Equipment capability 保持关闭且 catalog 为空。后续 Corin Sawblade 迁移只消费这里安装的正式 authoring/runtime API；网络模型后续只消费正式 EquipmentState 与表现投影，不得反向拥有装备规则。

## What Changes

- `CharacterPipelineDefinition`增加可选的`CharacterEquipmentProfile`与`CharacterEquipmentPresentationProfile`纯引用；Definition 不内嵌装备表或生成内容。
- 新增`CharacterEquipmentProfile`，唯一拥有稳定 Slot、Route、Equipment、Feature 引用与 Initial Loadout。
- 新增`CharacterEquipmentFeatureDefinition`，拥有可选 Persistent inline graph、Route inline graph、类型化参数 schema、局部状态声明、ActionProfile、Tag/Effect 与表现需求。
- 新增`EquipmentDefinition`，组合稳定 EquipmentId、SlotId、FeatureId、类型化参数值与 VisualBindingId。
- Slot 与 Route 分离：Slot 表达“装了什么”，Route 表达 Character 主流程向当前装备请求的业务动作；Route 只绑定现有 Input RequestId，不增加物理按键。
- 新增通用 Persistent Equipment Host 与 Equipment Action Route Host。Host 只选择已编译 entry，并继续使用唯一 compiled Runnable control runtime。
- Authoring discovery 从单一 RootTree 扩展为显式 composition roots：Character Root、Feature Persistent root 与 Feature Route root。
- Semantic IR、Float32/Fixed Program 与 ProgramExecutionLayout增加不可变 Equipment catalog、entry、参数、初始 Loadout 与 typed state layout。
- CharacterSimulationState增加 typed Equipment aggregate，覆盖当前装备、revision、pending change、host generation、贡献 handle 与 Feature local state。
- 增加 Begin/Commit/Cancel 装备切换事务；Timeline TreeClip只可在明确帧提交 Commit operation，不依据动画进度猜测换装时刻。
- ActionProfile增加通用 Required Tag Query；Feature ActionInstance捕获不可变 Equipment Context。系统不按 EquipmentId、WeaponType 或节点名称硬编码动作准入。
- 装备 Tag 与 Passive Effect复用唯一 Gameplay Tag/Effect aggregate，并按 Slot revision 稳定追踪来源。
- `CharacterAnimationPresentationProfile`继续唯一拥有动画 Layer、Transition 与 producer binding；Feature只声明需求。
- `CharacterEquipmentPresentationProfile`唯一拥有 ExistingRigObject 与 SpawnedVisualAsset 绑定；Projection 与 Visual Runtime从 committed EquipmentState维护持久外观。
- Float32与Fixed Target必须从同一 Semantic IR实现相同 Equipment 语义；缺失 operation、codec 或 capability 时拒绝整个目标 Program。
- Runtime 不读取 Equipment/Feature Unity authoring asset，不创建 Graph clone、Ability runtime、第二 Tick、第二 Action/Tag/GE/Animation runtime。

## Scope

### In Scope

- Equipment Gameplay/Presentation Profile与对应 Inspector。
- Slot、Route、Equipment、Feature、参数 schema、局部状态、Initial Loadout。
- Feature Persistent/Route inline graph的正式 owner 与下钻编辑入口。
- 显式 composition root discovery、Semantic IR、双 Target Program catalog与Execution Layout。
- typed EquipmentState、Host、Route resolution、装备切换事务。
- Action Required Tag Query、Action Equipment Context、Tag/Effect source。
- Animation requirement validation、Equipment visual projection与本地Visual Runtime。
- Program、State、Projection、source revision、diagnostics与普通 artifact reader更新。
- 旧命名、回调注册、运行时资产读取与名称搜索路径清理。

### Out of Scope

- Agent Snapshot/Patch/lowerer/handler/emitter/validator、MCP bridge与Agent技能更新。
- Corin RootTree、ActionProfile、Timeline、Prefab、Animation Profile或锯刃资产迁移。
- ServerAuthoritative Observation、Equipment request协议、远端装备表现与Deterministic Rollback canonical input接入。
- 背包、拾取、掉落、商店、存档、耐久、弹药、随机词条、附件或EquipmentInstance持久化。
- 命中检测、伤害、受击、弹道、投射物、射线、Combat Rewind或完整枪械Demo。
- Session Active后下载Feature、热加载Graph、改变Program布局或动态安装operation程序集。
- Motion Matching、武器握把IK、姿态重定向或Procedural Recoil。

## Impact

- Affected specs:
  - 新增`character-equipment-feature-authoring`
  - 新增`character-equipment-runtime`
  - 新增`character-equipment-presentation`
  - `character-pipeline-definition-authoring`
  - `btsmtl-gameplay-semantic-ir`
  - `btsmtl-compiled-simulation-program`
  - `character-action-authoring-closure`
  - `character-action-activation-flow`
  - `character-action-instance-runtime`
  - `gameplay-tag-runtime`
  - `character-animation-presentation-authoring`
- Affected authoring/compiler:
  - Definition纯引用、Equipment Profile/Feature Inspector与inline graph page context。
  - 多composition root discovery、source revision、Semantic IR与双Target lowering。
- Affected runtime/presentation:
  - Equipment typed state、Host、change transaction、Action/Tag/GE集成。
  - Equipment visual projection与Visual Runtime。
- Breaking behavior:
  - 声明Equipment capability后，缺失Profile、Route、参数、Layer、Producer、Visual binding或Target capability会阻止产物发布。
  - Runtime拒绝未编译EquipmentId、Unity authoring asset读取、Hierarchy名称搜索与默认装备fallback。
  - 未启用Equipment capability的现有角色保持现有Program形状；本change不自动迁移或改写Corin。

## Current Spec Comparison

- `character-pipeline-definition-authoring`要求Definition只装配正式Config引用。本change遵守该边界，只增加两个可选Profile引用，不把Slot、Feature或generated catalog塞回Definition Inspector。
- `btsmtl-gameplay-semantic-ir`与`btsmtl-compiled-simulation-program`要求唯一numeric-neutral Frontend、不可变Target Program和完整State Layout。本change扩展同一IR/Program，不增加Equipment解释器。
- `character-action-instance-runtime`明确删除`AbilityAsset -> BodyGraph`和Action membership执行语义。本change把Feature定义为compiler composition root；Action身份仍只来自ActionProfile/ActionInstance。
- `character-action-activation-flow`要求Action runtime只负责catalog、admission和lifecycle。本change将Slot/Route entry选择放在compiled Equipment Host，不把Feature对象交给Action runtime。
- `gameplay-tag-runtime`已经支持层级Query和按source计数。本change复用该聚合实现Required Tag，不增加武器专用bool、Tag容器或If节点。
- `character-animation-presentation-authoring`当前已安装Marker/Curve schema v14并要求唯一Animation Profile。本change只增加Feature需求校验和独立Equipment物体绑定，不修改Agent schema，也不创建第二Animation Profile。
- Current network specs要求网络模型消费完整Program/State，但本change不声称完成Equipment网络输入和远端表现；这些合同将在后续独立network integration change中补齐。
- 当前Agent基线已经是v14，`add-btsmtl-ai-controller-authoring`仍未实施。原proposal中的“AI v12后升级Equipment v13”已经过期，本change删除全部Agent版本和工具任务。

## Dependencies And Sequencing

- 依赖当前已安装的Semantic IR、Float32/Fixed Target、compiled Runnable control、Action、Timeline、Tag/GE和Presentation Projection主链。
- `add-timeline-animation-marker-sync`、`add-corin-targeted-motion-warp-demo`与`add-predictive-foot-placement-presentation-pass`已完成；本change只消费现行合同，不修改其Agent schema。
- 不依赖`add-btsmtl-ai-controller-authoring`，也不修改该change涉及的Agent Snapshot/Patch/MCP文件。
- 本change完成后，Corin迁移、Agent装备authoring和网络模型Equipment同步必须分别建立后续change，串行消费这里的正式API，不得回填临时路径。

## Success Criteria

- Definition只通过两个可选Profile引用安装Equipment Gameplay与Visual配置，Inspector不复制catalog。
- 作者可手动创建Slot、Route、Feature、Equipment和Initial Loadout，并在Feature owner内下钻编辑普通inline BTSMTL graph。
- Compiler从显式composition roots生成一个Semantic IR及Float32/Fixed Program；Runtime不读取Graph或Feature Unity asset。
- ProgramExecutionLayout一次构建Equipment索引；Actor/Tick热路径不按字符串扫描、排序或分配catalog集合。
- EquipmentState、pending change、host generation、Feature local state与Action Equipment Context进入唯一Target state、codec、snapshot和hash。
- Equipment Host只调度compiled entry；Action、Timeline、Motion、Tag与GE继续使用现有唯一runtime。
- Required Tag Query和Equipment Context是通用Action合同，不存在EquipmentId/WeaponType硬编码。
- 装备切换在同一Character transaction中原子撤销旧贡献、安装新贡献并输出持久visual selection。
- Animation Profile仍唯一拥有Layer/Transition/producer binding；Equipment Visual Runtime只管理物体/Renderer/socket，不调用Animator或反写Gameplay。
- Float32与Fixed都支持全部Equipment operation和codec；任何缺口都阻止对应Program发布。
- 本change不修改Agent工具、MCP bridge、Corin authoring资产或具体Network Model；只允许唯一正式编译器重发generated Program/Projection。
- `openspec validate add-character-equipment-feature-modules --strict --no-interactive`通过。
