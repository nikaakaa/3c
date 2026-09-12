## 1. Dump来源与Provenance

- [x] 1.1 读取 `Tools/Rendering/CorinRenderData/整理数据.json` 及其引用的Corin Dump输出，建立Source Manifest候选，覆盖模型、Rig、AnimationClip、Binding、Foot Analysis、Curve和快照来源；用机器校验确认每个来源有唯一identity、revision和content hash。证据：`Tools/Rendering/CorinRenderData/corin-source-manifest.json` 固定5个来源集合，覆盖AnimationClip 169条索引、ACL 169条索引、Camera Typetree、Timeline/Config Typetree、Model/Rig/Texture和渲染索引；每个来源保存revision、contentHash、fileCount和byteLength。
- [x] 1.2 将Dump来源与正式Unity资产建立NormalizedAssetIdentity映射，拒绝缺失、重复或hash不一致来源；用正式来源校验输出确认Runtime不依赖外部绝对路径。证据：Source Manifest列出14个正式Unity GUID/path/owner映射，sourceId引用全部落在5个source set或正式asset identity；Manifest只保存`sourceRootKey`与相对来源路径，不把外部绝对路径写入Runtime配置，并声明禁止目录扫描与Dump反向Runtime输入。
- [x] 1.3 生成Corin SourceManifestHash和缺口报告，并把它作为后续Authoring Closure的唯一上游输入；用canonical hash重算确认同一输入得到同一结果。证据：`corin-source-manifest.json` SHA-256=`694bfced917f6aea3c16f8825639d7e72e63ba024cc8c7fd8362687cdb3e4712`；`corin-source-gap-report.json` 固定Camera Trace、Foot Analysis Dump artifact、Session initial world和Animation normalized coverage四项缺口，未把缺口伪装成完成。

## 2. Corin正式配置闭包

- [ ] 2.1 从精确 `CorinCharacterPipelineDefinition.asset` 解析Control/Input、Skill/Action、GameplayEffect/Tag/Attribute、Motion、Presentation/Pose、Foot/IK/Blend、Timeline、Session、Prefab/Scene和生成产物依赖；输出稳定排序的AuthoringClosureHash。
- [ ] 2.2 核对三个Skill、SharedGraph、ActionContext、InputProfile、GameplayEffectProfile、Pose/Source/Binding、Timeline和Foot/IK/Blend owner；机器诊断不得出现跨角色、跨owner或未声明引用。
- [ ] 2.3 明确Authoring、Build Product和Runtime Evidence边界，确认Program、Projection、Runtime Dump和Replay结果不能反向作为作者配置；用闭包schema/validator拒绝反向引用。
- [ ] 2.4 将Corin Camera Profile、Default Sequence、Camera Curve、Action/Scene Camera Sequence及其数值/引用纳入Presentation Closure；用资产索引核对Camera owner和Timeline Camera/Scene引用。
- [ ] 2.5 生成Corin正式配置字段清单，覆盖每个作者资产的schema、identity、owner、引用、标量/vector/时间/权重/阈值/速度/角度/距离/迭代/容量和曲线元数据，以及Animation/Pose Transition参数；用清单hash确认没有漏项。

## 3. Document与正式产物

- [ ] 3.1 在Agent/Pose相关重构稳定后，对精确Corin Definition按当次唯一正式schema执行checkout，确认SourceManifestHash、AuthoringClosureHash和Document context一致；FSM发布v8后只消费v8，保留机器job证据，不独立保留旧版reader。
- [ ] 3.2 对同一Document执行dry-run、apply、re-checkout，确认documentHash/planHash、stable identity、owner和`syncState=Clean`一致；失败时保留ApplyFailed或结构化诊断。
- [x] 3.3 通过唯一Character Build入口发布Float32 Program、Fixed Program和Presentation Projection，核对ProgramHash、LayoutHash、ProjectionHash、Numeric Target、Source Resource和wrapper路径；不得引用其他worktree产物。证据：2026-09-11 主线 `3C_Client@e852139597e42532` 精确Definition发布 Float32/Fixed；共同ProgramId=`character:c7a7c1e3f7e64d81b5a04a90cbeb8d4e`、SourceRevision=`139c7eda5f4aa04e29ea2f713855f95fbeb0d9d47ad0ddcccb013648fcc80c30`、StateSlots=1127；Float32 ProgramHash=`94cc87fae8d4aa930d4fb69a4cff104bd00ea1a4c13537184a12e5c1a79cb60e`、LayoutHash=`e175d6635a0dbe9ca0f1896b96d7d54dae51417831d0803ec1e1c537b4776d0e`，ProjectionRevision=`e16b2c88868aabda90ecf93340aef455d834b1a8b78a13a9dc9ef699f733d476`；Fixed ProgramHash=`ac1d724ed66d44061eff6be11b9e10f965deb6af3f8ecdebacbe18990ab71a2c`、LayoutHash=`ffc4a1761e0abf315ce795d2cb1ad1bc1474bc57d59ccb4f62cdbaf72b8c0529`，wrapper=`Assets/Configs/Simulation/DeterministicRollback/Programs/CorinFixedProgram.asset`。Generated Program/Projection、Fixed wrapper和ACL source artifacts均来自主线Build时间窗，未引用parallel-test产物。
- [ ] 3.4 对Local、Fixed/Rollback及Server Authority所需精确Composition执行正式兼容校验，确认ProgramRuntime、Backend、Pipeline、SessionSource、WorldSolver、Snapshot Codec和Network Model身份一致。

## 4. Runtime Dump与Replay

- [ ] 4.1 创建或关联精确Corin ReplayRequest，固定Prefab/Scene、Actor/World初始状态、Numeric Target、Session Composition、Program/Projection hash、时钟和Fixed Input Trace；用RequestHash校验不可变输入。
- [ ] 4.2 使用正式RunHost/Unity运行链记录Corin Skill激活、ActionInstance、generation、Timeline开始/完成/中断、父子调用、State/Snapshot hash和Runtime Dump；运行版本变化或目标选择歧义必须失败。
- [ ] 4.3 按3C Development Center流程关联before/after change run，使用同一Fixed Input Trace执行Replay并生成Compare；比较至少覆盖输入/Tick、Action/Timeline、Program/Layout/Projection、State/Snapshot和Body结果。
- [ ] 4.4 对缺少Foot、Camera、Pose或性能专门分析的结果标记未验证，不把编译成功、Replay结束或Body轨迹一致推断为完整表现通过。
- [ ] 4.5 在Replay Request中固定Camera Profile/Sequence/Curve和Camera Input Trace，并比较Camera事件/参数与Gameplay/Body结果；缺少Camera证据时保留未验证状态。

## 5. 交付与规范同步

本节5.1同时接收旧refactor-btsmtl-flowcanvas-authoring的8.4.2中网络和正式运行证据汇总部分；作者与观察各自由对应新change交付，不要求一个总任务重复汇总实现状态。

- [ ] 5.1 汇总SourceManifest、AuthoringClosure、Document、Build、Session、Runtime Dump、Replay和Compare的hash/RunId，生成Corin闭环交付记录；所有失败Gate必须带原始机器诊断。
- [ ] 5.2 更新受影响current specs、project/change implementation记录和tasks状态，核对不存在Dump路径fallback、旧产物替换、目录扫描或第二Replay执行器；通过OpenSpec strict validate。
- [ ] 5.3 形成独立小步提交并检查最终工作区、产物路径和交付记录没有混入其他worktree改动；提交前输出完整文件范围和未验证项。

## 6. 接收Skill网络与正式运行收尾

- [ ] 6.1 接收旧4.5.5：盘点并补齐Corin Rollback与Server Authority正式Pass/Adapter集成，复用既有Session链；通过现有正式运行入口核对实际载荷只包含Input、Canonical Request、Hash/Snapshot，交付网络身份和载荷证据，不复制作者Graph、Blackboard名称、Timeline对象或最终Pose。
- [ ] 6.2 接收旧8.1：与本表4.2/4.3共享同一精确运行记录，覆盖Skill启动、Timeline等待、正常结束、取消和中断，核对父子调用与状态/Body结果并记录业务差异；交付当前产物身份下的正式CLI/Runtime证据，不新增测试、临时执行器或重复Replay任务。
