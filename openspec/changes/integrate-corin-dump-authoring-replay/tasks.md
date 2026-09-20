## 1. Dump来源与Provenance

- [x] 1.1 读取 `Tools/Rendering/CorinRenderData/整理数据.json` 及其引用的Corin Dump输出，建立Source Manifest候选，覆盖模型、Rig、AnimationClip、Binding、Foot Analysis、Curve和快照来源；用机器校验确认每个来源有唯一identity、revision和content hash。证据：`Tools/Rendering/CorinRenderData/corin-source-manifest.json` 固定5个来源集合，覆盖AnimationClip 169条索引、ACL 169条索引、Camera Typetree、Timeline/Config Typetree、Model/Rig/Texture和渲染索引；每个来源保存revision、contentHash、fileCount和byteLength。
- [x] 1.2 将Dump来源与正式Unity资产建立NormalizedAssetIdentity映射，拒绝缺失、重复或hash不一致来源；用正式来源校验输出确认Runtime不依赖外部绝对路径。证据：Source Manifest列出14个正式Unity GUID/path/owner映射，sourceId引用全部落在5个source set或正式asset identity；Manifest只保存`sourceRootKey`与相对来源路径，不把外部绝对路径写入Runtime配置，并声明禁止目录扫描与Dump反向Runtime输入。
- [x] 1.3 生成Corin SourceManifestHash和缺口报告，并把它作为后续Authoring Closure的唯一上游输入；用canonical hash重算确认同一输入得到同一结果。证据：`corin-source-manifest.json` SHA-256=`694bfced917f6aea3c16f8825639d7e72e63ba024cc8c7fd8362687cdb3e4712`；`corin-source-gap-report.json` 固定Camera Trace、Foot Analysis Dump artifact、Session initial world和Animation normalized coverage四项缺口，未把缺口伪装成完成。

## 2. Corin正式配置闭包

- [ ] 2.1 从精确 `CorinCharacterPipelineDefinition.asset` 解析Control/Input、Skill/Action、GameplayEffect/Tag/Attribute、Motion、Presentation/Pose、Foot/IK/Blend、Timeline、Session、Prefab/Scene和生成产物依赖；输出稳定排序的AuthoringClosureHash。
- [ ] 2.2 核对三个Skill、SharedGraph、ActionContext、InputProfile、GameplayEffectProfile、Pose/Source/Binding、Timeline和Foot/IK/Blend owner；机器诊断不得出现跨角色、跨owner或未声明引用。
- [ ] 2.3 明确Authoring、Graph／Domain／Presentation产物和Runtime Evidence边界，确认Graph artifact、领域／表现binding、Runtime Dump和Replay结果不能反向作为作者配置；用闭包schema／validator拒绝反向引用。
- [ ] 2.4 将Corin Camera Profile、Default Sequence、Camera Curve、Action/Scene Camera Sequence及其数值/引用纳入Presentation Closure；用资产索引核对Camera owner和Timeline Camera/Scene引用。
- [ ] 2.5 生成Corin正式配置字段清单，覆盖每个作者资产的schema、identity、owner、引用、标量/vector/时间/权重/阈值/速度/角度/距离/迭代/容量和曲线元数据，以及Animation/Pose Transition参数；用清单hash确认没有漏项。

## 3. 作者闭包与正式产物

- [ ] 3.1 在Pose相关重构稳定后，从精确Corin Definition和其可达Graph、Timeline、Pose、EventGraph作者资产重算AuthoringClosureHash，确认SourceManifestHash、stable identity、owner和正式引用一致，不引入Document同步包或旧reader。
- [ ] 3.2 对同一作者闭包执行正式Graph校验及领域／表现准备，确认GraphArtifactHash、DomainBindingSetHash、PresentationBindingHash和来源identity形成单向依赖；失败时保留结构化诊断。
- [ ] 3.3 通过各正式owner发布Float32与Fixed所需Graph artifact、Domain Binding Set和Presentation Binding，核对Numeric Target、Source Resource和wrapper身份；旧2026-09-11整角色Program／Projection记录只作历史证据，不满足本任务且不得引用其他worktree产物。
- [ ] 3.4 对Local、Fixed／Rollback及Server Authority所需精确Composition执行正式兼容校验，确认Domain Runtime、Backend、Pipeline、SessionSource、WorldSolver、Snapshot Codec和Network Model身份一致。

## 4. Runtime Dump与Replay

- [ ] 4.1 创建或关联精确Corin ReplayRequest，固定Prefab／Scene、Actor／World初始状态、Numeric Target、Session Composition、GraphArtifact／DomainBindingSet／PresentationBinding hash、时钟和Fixed Input Trace；用RequestHash校验不可变输入。
- [ ] 4.2 使用正式RunHost/Unity运行链记录Corin Skill激活、ActionInstance、generation、Timeline开始/完成/中断、父子调用、State/Snapshot hash和Runtime Dump；运行版本变化或目标选择歧义必须失败。
- [ ] 4.3 按3C Development Center流程关联before／after change run，使用同一Fixed Input Trace执行Replay并生成Compare；比较至少覆盖输入／Tick、Action／Timeline、GraphArtifact／DomainBindingSet／PresentationBinding、State／Snapshot和Body结果。
- [ ] 4.4 对缺少Foot、Camera、Pose或性能专门分析的结果标记未验证，不把编译成功、Replay结束或Body轨迹一致推断为完整表现通过。
- [ ] 4.5 在Replay Request中固定Camera Profile/Sequence/Curve和Camera Input Trace，并比较Camera事件/参数与Gameplay/Body结果；缺少Camera证据时保留未验证状态。

## 5. 交付与规范同步

本节5.1同时接收旧refactor-btsmtl-flowcanvas-authoring的8.4.2中网络和正式运行证据汇总部分；作者与观察各自由对应新change交付，不要求一个总任务重复汇总实现状态。

- [ ] 5.1 汇总SourceManifest、AuthoringClosure、Graph／Domain／Presentation产物、Session、Runtime Dump、Replay和Compare的hash／RunId，生成Corin闭环交付记录；所有失败Gate必须带原始机器诊断。
- [ ] 5.2 更新受影响current specs、project/change implementation记录和tasks状态，核对不存在Dump路径fallback、旧产物替换、目录扫描或第二Replay执行器；通过OpenSpec strict validate。
- [ ] 5.3 形成独立小步提交并检查最终工作区、产物路径和交付记录没有混入其他worktree改动；提交前输出完整文件范围和未验证项。

## 6. 接收Skill网络与正式运行收尾

- [ ] 6.1 接收旧4.5.5：盘点并补齐Corin Rollback与Server Authority正式Pass/Adapter集成，复用既有Session链；通过现有正式运行入口核对实际载荷只包含Input、Canonical Request、Hash/Snapshot，交付网络身份和载荷证据，不复制作者Graph、Blackboard名称、Timeline对象或最终Pose。
- [ ] 6.2 接收旧8.1：与本表4.2/4.3共享同一精确运行记录，覆盖Skill启动、Timeline等待、正常结束、取消和中断，核对父子调用与状态/Body结果并记录业务差异；交付当前产物身份下的正式CLI/Runtime证据，不新增测试、临时执行器或重复Replay任务。
