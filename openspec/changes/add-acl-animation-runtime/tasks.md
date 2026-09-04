本清单覆盖 Corin 当前正式 source 闭包的骨骼和片段自带 BlendShape，不新增独立表情行为系统。现有 8 片段离线解码是前置证据；以下任务均为尚待实施的项目接入，不以离线结果或格式校验代替完成状态。

## 0. 正式目标与输入证据

- [ ] 0.1 从当前 Corin Definition/Profile 编译闭包导出 Action、Clip、Blend Space 和已配置 provider 的正式资源身份清单；交付 source 到 Clip/原始 CAB/PathId 的对应记录，不替换现有动作以适配现成样本
- [ ] 0.2 为目标清单补全缺失的离线 Scalar、数据库头/bulk 与模型映射，复用现有解码工具；交付逐条解码产物及有效覆盖报告，未覆盖位宽、segment 或轨道类型保持明确失败，不标为已还原

## 1. ACL 资源合同与 Editor 构建

- [ ] 1.1 定义 manifest 与 Transform、可选 Scalar、Database Header、分 tier Bulk 的独立合同，包含存在标记、长度、对齐、版本/hash、完整 binding 和 reference/default 身份；交付 schema、合法空块规则与构建 validator 错误目录
- [ ] 1.2 在 Editor/Build 中从正式 `AnimationClip`、Rig 和 dense binding 生成不可变 ACL payload，禁止以展开 YAML、作者字符串或 AssetDatabase 查询作为 Runtime 输入；用同一输入重复构建并比较 manifest/payload 哈希
- [ ] 1.3 实现质量门禁，分别记录原始还原证明与正式 Clip 到项目 ACL 的重采样/压缩误差，覆盖所有必须轨道、离散性、绑定、关键时刻、采样间隔和循环端点；交付按资源/轨道定位的质量报告及阻止发布的错误结果
- [ ] 1.4 扩展 Projection 编译产物，写入显式 ACL backend、资源句柄、格式/内容身份和 Rig/binding 摘要；用编译输出确认 Runtime 不需要作者字符串、AssetDatabase 或曲线查找
- [ ] 1.5 生成资源清单、源依赖、压缩设置、质量结果和 native 目标平台记录；用清单校验命令确认每个 payload 均可追溯且无未声明文件
- [ ] 1.6 在现有 Presentation Profile 资源绑定中加入正式 backend 和压缩配置，覆盖完整 Clip 使用闭包；交付现有编辑入口与对应 Agent Document schema/exporter/reconciler/mutation/validator 的一致字段清单
- [ ] 1.7 分开保存正式 start/stop/loop、采样网格与压缩覆盖范围，保留端点并关闭隐式 loop 优化；交付构建产物中的时间映射记录，Program effective time 只应用一次
- [ ] 1.8 扩展 Graph 参数的 Control/AnimatedProperty 用途，以及 Profile 的参数到 Renderer/Mesh/BlendShape 映射，默认值仍由 Graph 唯一声明；交付完整 authoring schema 与 Agent Document 同步字段，拒绝重复身份和目标
- [ ] 1.9 实现显式属性曲线导入命令，把目标来源数据写入正式 AnimationClip；交付源 hash、单位转换、时间范围和仅属性差异记录，已有骨骼/Foot/Phase 或人工属性冲突明确拒绝覆盖
- [ ] 1.10 生成 NativeClip 编译标量曲线页与 ACL Scalar payload 的同一 typed binding，编译合法无动画通道的显式默认常量；交付完整参数/属性索引和存在性记录，14 条 Motion/Root 分类为来源证据
- [ ] 1.11 将参数、资源、属性绑定、所有工作区/历史容量和最终属性输出纳入统一 Projection/Program schema/ABI/hash；交付正式 Build 的新产物清单与旧版本拒绝结果，不保留运行时兼容分支

## 2. Native ACL 解码桥

- [ ] 2.1 锁定官方 ACL v2.1.0 commit `414689d5cff4286a7898487a46dc5e48005d38da`、数值依赖、编译器/浮点选项和 C ABI，交付 Windows x64 Editor/Mono 与 IL2CPP 构建 manifest、导出符号和 plugin Importer 声明
- [ ] 2.2 实现 payload 校验、Context 创建/销毁、scratch 和 Pose writer 的预分配接口；用生命周期检查确认 Context 未 Ready 前不发布句柄，表现帧内不发生托管分配或外部 I/O
- [ ] 2.3 实现消费已确定 effective time、采样策略、dense binding 和固定质量租约的 C ABI，Transform 使用正式 database，Scalar 使用官方支持的独立流；交付输入输出布局和错误码，禁止私有版本重标及重复应用 play rate
- [ ] 2.4 实现 track writer 到现有 source 页的写入、合法 default/constant 子轨道恢复与完整性检查；交付必需轨道覆盖记录，缺 reference、未知轨道或容量不足返回稳定 Invalid
- [ ] 2.5 将平台能力矩阵和 native artifact 接入构建门禁；用未声明平台或 ACL 版本确认资源在构建/准备阶段为 Invalid，不生成运行时 fallback
- [ ] 2.6 分离共享 payload/database/streamer 与每活跃 source 的解码 Context，统一 aligned allocation、销毁和在途任务完成边界；交付 native handle 所有权与释放记录

## 3. 唯一 Pose Source 与 Graph 接入

- [ ] 3.1 在 `CharacterPoseSourceModule` 中增加显式 ACL backend 注册、source identity 和资源句柄解析，保持现有 Animancer backend 对非 ACL source 的行为；用 Projection 对照确认同一 source 不会同时选中两个 backend
- [ ] 3.2 在资源准备到 Program/Source handoff 中接通 typed Pending/Ready/Invalid，处理现有仅接受 Ready 的入口；交付 Entry Pending、候选 target Pending 和 Invalid 的 outcome 路径，正常等待不依赖异常
- [ ] 3.3 在 `CharacterPoseProgramSourcePreparationRuntime` 中复用现有 source binding、预分配 pose page 和 `AnimationScriptPlayable` 工作页；用 Graph 拓扑与 output-job 数量检查确认 ACL 不创建第二张 PlayableGraph、第二个 Player 或第二个 Final writer
- [ ] 3.4 让 ACL sample 通过现有 source fan-in、Pose Plan 和 `CharacterPoseFrameCoordinator` 的唯一 Evaluate Barrier 发布 `PresentationPoseSourceSample`；用同一 Frame completion、SourceGeneration 和 Pose identity 对账 source sample 与 Barrier 输出
- [ ] 3.5 保持 Program 对状态、Transition、slot、clock 和权重的唯一所有权，支持 ACL 与合法其它 source 的同帧过渡；用双 source demand 对账各自 capture、权重、退休和最终输出均无 ACL 私有状态机
- [ ] 3.6 对显式 ACL-backed source 禁止展开 `.anim` 同时驱动，保留非 ACL source 的现有路径；用 Projection/运行 binding 对账确认一个 source identity 只有一个可见播放器
- [ ] 3.7 保持 ACL backend 不写 Physical Transform、IK Goal、Foot Placement、Gameplay 或 Final Publication；用代码依赖扫描确认 ACL 模块只依赖 Source/Projection/资源接口
- [ ] 3.8 统一资源解析在 Action、Direct Clip、Blend Space、MM 与 Preview 的使用点，移除 ACL 条目的运行时 Clip 强引用；交付编译资源闭包和依赖扫描结果
- [ ] 3.9 让 ACL 采样复用现有 Root/Scale policy、Virtual Bone、Velocity、continuity 和 completion 处理，保持 Phase/Foot 注册曲线原消费者；交付单一调用链与 source 页布局记录
- [ ] 3.10 在唯一 Source Module 中按同一 effective time 采样 NativeClip 编译标量页或 ACL Scalar，复用现有 ClipSamplePlan 的权重与归一化；交付同 lineage 的 typed 属性结果，骨骼和属性共同 Pending/Ready/Invalid
- [ ] 3.11 让 Program 从 Source typed 结果写入现有 Player/Pose Value 参数页，并复用 State、BlendStack、Slot、Parameter Resolve 和 Inertialization 的既有参数规则；交付参数写入 owner 与容量记录，不新增表情混合算法或时钟
- [ ] 3.12 保持 Layered Bone Blend/Additive 的 Base 参数传播和既有 scalar/bone 权重分离，属性不再次乘骨骼 Mask；交付对应编译节点与调用链记录，不自动补末端曲线覆盖
- [ ] 3.13 在既有 Actor Factory 中显式装配 Renderer/Mesh/BlendShape binding，并扩展唯一 Final Publication 的属性页、整体预验证与同帧写入；交付统一 completion 和绑定错误结果，现有骨骼 Writer 数学保持
- [ ] 3.14 保持 Source Graph 最终输出权重为 0，统一 Runtime/Preview/reset/teardown 的属性所有权；交付唯一 Renderer 写入入口扫描及同一 Frame 的骨骼/属性发布记录，不增加图外脚本 writer

## 4. 异步流入、预取与释放

- [ ] 4.1 通过正式 IResourceModule/YooAsset 接入异步加载、校验与 Context 准备，composition 显式注入同一服务；交付独立于 Frame Seal 推进的准备路径，Evaluate 不同步等待 I/O
- [ ] 4.2 实现完整可达闭包预取、正式驻留预算、固定质量集合、在用租约和资源级 LRU；交付所需 tier/chunk manifest 与预算计算，部分数据只能 Pending，不能隐式降质
- [ ] 4.3 把 ACL usage、retention、retirement permission、Frame Seal 和 release completion 接入现有 Source Module generation；用连续过渡和退休序列确认 completion 前不复用逻辑/物理槽位
- [ ] 4.4 实现取消、Discard 和准备失败的资源收口，不保留孤立 Context、payload 或 source binding；用中断准备和失败释放清单确认每个 native handle 均有唯一终结记录
- [ ] 4.5 接通跨 Actor 共享租约、generation 校验和 stream-out 安全边界；交付共享引用及在途请求归零后唯一释放的记录

## 5. 只读事实与性能记录

- [ ] 5.1 发布 ACL resource identity、格式版本、压缩/驻留/流入字节、stream 状态、Context identity、effective time、采样轨道数、解码耗时、completion 和稳定错误字段；用已 Seal 的 Frame 对账所有事实带同一 Projection/Rig/SourceGeneration
- [ ] 5.2 让诊断只复制已提交 Source 结果，排除 Pending Context、下一帧数据和未 Seal 数据；用故意未完成的准备阶段确认事实不提前出现
- [ ] 5.3 关闭 ACL diagnostics interest 时跳过复制和事件而不跳过资源准备、解码、Transition、IK 或 Final Publication；用开关前后比较 Pose、source identity、release 和驻留行为
- [ ] 5.4 将解码耗时、Context 数量、驻留字节和流入完成记录接入现有性能汇总，不把任何 ACL 事实接入 Foot、IK、Goal、State 或 Gameplay 决策；用依赖扫描确认无反向调用
- [ ] 5.5 将属性源值、混合后值、目标索引与最终写入 completion 接入已有 interest-gated 事实，使用独立属性字段而不扩张 Foot 采样 schema；交付同 lineage 的已提交诊断及无 interest 时不复制的记录

## 6. 首批资源迁移与发布收口

- [ ] 6.1 以 Corin 当前闭包中的一条正式 source 发布骨骼/属性齐全的 ACL payload、manifest、Projection binding 和平台清单；交付当前模型绑定、正式参考误差与资源 hash 报告，不将 MainCity 样本强换成现有动作
- [ ] 6.2 将批准的 Corin source 清单逐项切换为显式 ACL backend，清理同一 source 的展开运行包引用但保留 authoring Clip 作为构建输入；用编译产物扫描确认没有隐式 `.anim` fallback
- [ ] 6.3 完成 native artifact 平台门禁与项目编译；dotnet build 必须加 `--disable-build-servers /nr:false /p:UseSharedCompilation=false` 并在结束后立即执行 `dotnet build-server shutdown`，交付编译日志，不把编译通过当作运行行为证明
- [ ] 6.4 固化发布包的 Projection、ACL payload、manifest、native artifact、能力矩阵和哈希清单；用清单校验确认部署缺任一身份或平台产物即阻止发布
- [ ] 6.5 记录从旧 Projection/资源包回滚的部署步骤，明确回滚依赖上一版完整 artifact 而非运行时 fallback；用两版清单对账确认回滚不会在同一 source identity 下并行两个播放器
- [ ] 6.6 固化完整接入报告，记录原生/ACL source、共同过渡、属性默认值、骨骼 Mask 分离、Pending/Invalid、共享释放与最终骨骼/属性同帧结果的现有诊断证据；未通过的目标阻止发布，不新增测试工程或把手动操作步骤写入本清单
