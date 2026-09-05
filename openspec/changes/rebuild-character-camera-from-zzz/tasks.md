以下按依赖顺序组织，不代表业务优先级。各项完成依据是明确交付物、现有正式编译/校验结果或实现发布的诊断事实，不包含新增测试或用户手动验收任务。代码和资产只在另行授权 apply 后修改；每个完整小步使用中文提交，真实冲突先交给用户决策。

## 1. 闭合 ZZZ 来源与原行为

- [x] 1.1 锁定原游戏/组件版本、原资源身份和内容 hash，交付同版本 Camera 来源清单及重复/冲突资源报告。
- [ ] 1.2 解析角色 Camera Profile、球面/轨道组、普通目标/Boss 锁定、输入和碰撞配置，交付逐字段值、单位、空间及正式消费者对应表。
- [ ] 1.3 追通单点/双点/多点/实体取景与原 Sequence 组合函数，交付输入输出、分支和依赖函数清单，所有纳入类型都有来源证据。
- [ ] 1.4 追通位置/旋转阻尼、BlendFromCurrent、MoveByBlending、抢占和退出函数，交付阶段历史、起终点、重入/取消与时间推进规则。
- [ ] 1.5 追通 Override、Zoom、Stretch/回弹、Shake 的组合、tag/优先级/静音规则，交付特殊时间值、相对/绝对数值、枚举与原曲线消费者映射。
- [ ] 1.6 补齐公共 Camera Curve、Shot、CinePrefab、Timeline/Track 绑定等可达依赖，交付无未解析引用的资源闭包及原曲线插值数据。
- [ ] 1.7 追通 WorldBasicCameraData 到活动 Cinemachine 实例及最终回读，交付逐阶段 owner、调用顺序、写入点、组件和结果表，每项计算只有一个 owner。
- [ ] 1.8 汇总来源对应表与精确未完成项，将 1.1—1.7 的证据写入本设计的具体阶段/字段附件；只有已纳入行为和依赖全部闭合后才能推进正式算法实现，不用近似公式完成此项。

## 2. 建立正式相机作者资源

- [ ] 2.1 建立 CharacterCameraProfile 与 Definition 的显式引用、默认序列和目标槽位合同，交付真实资源模型及 owner/dependency 定义，Definition 不内联相机参数。
- [ ] 2.2 建立 CameraSequenceAsset 的有限算法与组合描述，交付覆盖来源清单的 typed 数据模型和合法输入输出/组合校验，未支持类型不能发布。
- [ ] 2.3 建立 Override、Zoom、Stretch、Shake 资源模型，交付与原字段表逐项对应的数值、时间、空间、优先级及释放合同，无万能 Custom 字段袋。
- [ ] 2.4 建立 Shot 与 Camera Curve 资源模型，交付精确绑定需求、镜头参数和完整曲线数学描述，共享资源保持单一 owner。
- [ ] 2.5 建立来源基线与正式资源的身份映射、依赖和作者差异记录，交付可定位原身份与当前编辑差异的资源清单，重新导入不会自动覆盖作者改动。

## 3. 统一作者能力与 Mutation

- [ ] 3.1 为全部 Camera 资源和 SkillProgram/技能局部 Graph/TreeClip/Timeline 能力注册字段、类型、单位、空间、时间域和端口，交付人工 UI、Compiler、Document 共用的唯一 capability 目录。
- [ ] 3.2 实现 Profile/资源的创建、修改、删除和强类型引用 Mutation，交付与现有事务服务接线的 handlers，创建对象和引用变更属于同一 Undo owner 集合。
- [ ] 3.3 实现原数据到正式作者目标的明确 Import 命令，交付依赖预检、逐项映射和同一事务计划；缺失依赖或来源冲突时报告失败且不发布半套资产。
- [ ] 3.4 注册 Timeline-local Weight/Ease 与资源曲线各自的 Channel/Mutation owner，交付无重复可写曲线的 Catalog 和完整曲线替换接口。

## 4. 接入 Semantic IR 与唯一 Projection Build

- [ ] 4.1 迁移 SkillProgram Root、技能局部 Graph、TreeClip/Timeline 的 Camera 请求 payload，分离 portable 命令语义与 Presentation 资源引用，交付带完整 source mapping 的新版本相机 operation 合同。
- [ ] 4.2 同步 Float32/Fixed 相机 emitter、evaluator 和命令 disposition，交付两个 Target 同语义的生成结果，未知/旧版本 payload 明确拒绝。
- [ ] 4.3 实现 Camera Projection 编译模块，交付同一 CharacterPresentationProjection 内的 dense 资源/曲线/序列/目标/Shot 计划和唯一阶段顺序，不新增独立 loader。
- [ ] 4.4 补齐资源类型、来源闭包、数值/时间域、阶段和容量预检，交付能定位 Profile、资源、节点或 Clip 的正式 Compiler 诊断。
- [ ] 4.5 将 Camera dependency/revision 与生成字段纳入原子 Build，交付完整发布组及身份报告，纯效果参数修改只改变 Projection，时点/命令语义变化更新对应 Program 合同。

## 5. 接入本地相机帧与请求生命周期

- [ ] 5.1 建立 CameraFrameInput、CameraFramePlan、CameraRigResult 和窄接口，交付不依赖场景对象/作者资产的计算合同及明确平台 binding 合同。
- [ ] 5.2 扩展现有 Presentation Frame context 的相机时间和动作锚点输入，交付 scaled/unscaled/暂停/重置来源清单，核心不读取 Unity Time 或二次缩放。
- [ ] 5.3 实现 producer/generation/EventId 的请求容器和 Publish/Replace/Retire，交付有界状态与诊断事实，旧 generation 不能清理新实例，同一事件不重复触发。
- [ ] 5.4 实现请求退休、效果退出尾段和 Runtime/Body reset 的原规则，交付覆盖 Cut/BlendOut/等待来源结束、快速重入与目标切换的正式状态转换及原因快照。
- [ ] 5.5 将目标采样接入同帧 visible Body、最终骨骼、明确实体/世界点/候选输入和本地物理场景，交付槽位解析与缺失原因结果，不建立第二份 Body/台阶历史。

## 6. 实现完整基础构图与序列

- [ ] 6.1 实现原单点、双点、多点及实体取景算法，交付来源对应表中每种已纳入算法的正式 evaluator 和输入输出诊断。
- [ ] 6.2 实现原球面/轨道、构图偏移、屏幕位置和角色默认序列，交付消费 Profile 的完整基础镜头计划，删除基础参数硬编码依赖。
- [ ] 6.3 实现原手动输入、俯仰/水平响应、输入接管、普通目标/Boss 锁定和目标转换，交付明确输入权重、目标与构图结果，不修改 Gameplay 目标事实。
- [ ] 6.4 实现原位置/旋转阻尼与上下运动镜头响应，交付真实连续历史与阶段结果，对应 Adapter 不再执行同类阻尼。
- [ ] 6.5 实现 BlendFromCurrent、MoveByBlending、状态仲裁和进入/退出/抢占恢复，交付实际位置/旋转/构图/FOV 混合结果和起终点诊断，不只发布 blendProgress。

## 7. 实现全部相机效果

- [ ] 7.1 实现 OverrideTrack 的原轨道/构图覆盖和进出曲线，交付同 tag/优先级/被压制状态的正式结果与来源映射。
- [ ] 7.2 实现 Zoom 的原 FOV 类型、延迟、进入、保持、退出和时间尺度规则，交付与来源字段对应的完整效果阶段和最终 FOV 贡献。
- [ ] 7.3 实现 Stretch 的半径、位置、倾斜/俯仰及回弹行为，交付完整空间贡献和恢复到当前有效构图的结果，不折算为 FOV Kick。
- [ ] 7.4 实现 Shake 的原方向、频率、噪声/随机、各轴幅度、衰减和叠加规则，交付正式效果结果与种子/事件来源，不扰动角色或跟随目标。
- [ ] 7.5 将所有效果接入已确认阶段顺序，交付同帧覆盖/累计及首次采样结果，短事件不因统一先减寿命而无条件丢失。

## 8. 完成 Shot、碰撞和 Cinemachine 输出

- [ ] 8.1 实现原 Shot 的资源解析、明确绑定、进入/退出和打断，交付由唯一 Runtime/Adapter 管理的承载生命周期，Shot 无自主更新。
- [ ] 8.2 按原职责实现相机碰撞及其显式世界端口/平台扩展，交付唯一碰撞阶段与结果，缺少场景能力时提供正式错误。
- [ ] 8.3 实现 CinemachineCameraRigAdapter 的计划应用、原组件阶段和唯一 Brain 推进，交付阶段 owner 与活动输出结果，移除重复阻尼/混合/效果计算。
- [ ] 8.4 从实际活动输出发布同帧 CameraBasisSnapshot 和正式重置结果，交付 Shot/blend 后一致的方向/yaw/pitch，输入不再读取未活动 FreeLook。
- [ ] 8.5 将完整相机装配接回 CharacterCameraPresentationRuntime 与 Factory，交付保持 Body/最终动画/Camera 顺序的唯一调用链，无相机 Actor 不分配相机能力。

## 9. 接入正式作者入口与 Timeline

- [ ] 9.1 在 SkillProgram Root、技能局部 Graph 与 TreeClip/Timeline 入口接入 Camera Navigator、资源 Details、引用导航与 source mapping，交付复用原 Shell/Canvas/selection 的领域 adapter，无独立 Workbench；C# Locomotion 控制拓扑不提供 Camera 图节点。
- [ ] 9.2 迁移正式 SkillProgram Camera producer 的 Sequence、效果、响应、目标和 basis 能力，交付菜单/字段/端口/编译/Document 一致的正式能力；producer 通过 SkillProgram 与新 SourceMap 衔接，消费已提交 PresentationCommand、ActionInstance 来源和 generation。
- [ ] 9.3 接入 Timeline 的 Sequence/Override/Zoom/Stretch/Shot 区间与 Shake 时点，交付明确资源、时序、同帧顺序和生命周期编辑，不创建原 Animator 事件播放器。
- [ ] 9.4 接入 Clip Curve Lane 与共享资源 Curve owner 导航，交付带单位/时间/值域的原交互与唯一 Mutation，不生成隐式曲线副本。
- [ ] 9.5 接入明确 Import/Build/统一 ScenePlay Preview 准备命令与 Stale/错误显示，交付可定位资源的轻量状态；Camera 不拥有 Preview session、fixture executor 或 seek controller，OnInspectorGUI、selection 和恢复调用链不含重操作。

## 10. 接入统一 ScenePlay Preview 与 Live Debug

- [ ] 10.1 对账 `rebuild-btsmtl-preview-with-scene-play` 的统一 Preview owner 接口，交付 Camera Runtime、Projection、Rig/目标/物理绑定、Reset 和只读诊断的接入边界；BTSMTL 实际签名未提交前不写桥接或占位接口。
- [ ] 10.2 由统一 ScenePlay owner 提供初始镜头、目标/输入轨迹、时间和随机种子等可复用 fixture，交付明确输入来源；Camera 不创建第二份 fixture executor，也不从当前场景偷取隐式历史。
- [ ] 10.3 由统一 ScenePlay owner 调度播放、暂停、循环与可取消的分步 seek 重建，Camera 只执行正式 Runtime 的 Reset/逐帧求值；Timeline 游标只定位作者内容或观察历史，seek 不直接修改 Simulation。
- [ ] 10.4 由统一 ScenePlay owner 管理 Camera/Shot 输出独占绑定、Stop/Dispose/重绑/domain reload 清理，交付占用/释放与缺少上下文的明确结果；Camera 不拥有第二个会话或角色执行链。
- [ ] 10.5 接入正式相机诊断 provider 与窗口本地 Follow/Pin，交付资源/producer/generation/时间/退出/碰撞/最终 basis 快照和作者导航，Live 不调用 Preview，Preview owner 不把 Live 伪装成 Authoring Preview。

## 11. 完成 Agent Document v5 目标对账

- [ ] 11.1 盘点当前已安装 Document v4 中可达的 Camera 语义、资源身份、引用和 owner，交付迁入最终 v5 Camera domain 的完整对账表；不为过渡建立 v4 Camera 分片。
- [ ] 11.2 在 BTSMTL v5 实际接口提交后，注册 Camera Profile/Sequence/Effect/Curve、Definition 引用、结构化引用语义和 capability/context revision，交付 v5 owner 统一装配的精确 manifest 闭包，AI domain 拒绝 Camera 可写分片。
- [ ] 11.3 在 v5 实际接口提交后，同步 v5 owner 的 Exporter、严格 Codec、Catalog、local identity 发现、Reconciler、typed Mutation、Validator、owner 锁定与 Undo/失败恢复；Camera domain 不拥有第二套 schema/codec/Mutation。
- [ ] 11.4 在 v5 实际接口提交后完成整包反向导出、稳定对象身份、package hash 和 Clean 状态发布，交付经重读校验的完整包，Camera apply 不触发 Build 或运行相机。
- [ ] 11.5 在 v5 实际接口提交后同步五个生命周期工具的机器诊断、现有窗口和 skill/current-contract；在接口未提交前只维护边界说明和失败诊断，不写 v5 占位代码或把未安装能力写成 current truth。

## 12. 迁移 Corin 内容与全部相机调用者

- [ ] 12.1 通过正式 Import/作者事务迁入 Corin 的 81 项 Shake、18 项 Zoom、18 项 Stretch、4 项 Override，交付逐资源身份、数值和消费者对应报告。
- [ ] 12.2 补齐 Corin Profile、Sequence、公共 Curve、Shot 与绑定依赖，交付完整可达作者配置，无默认替代或仅保留名字的资源。
- [ ] 12.3 将已确认属于单角色动作演出的 Corin 相机事件迁入正式技能局部 Graph/TreeClip/Timeline 请求链，交付保留原帧率/时点/顺序的对账结果，先覆盖 AssaultAid 与 ParryAid；SwitchInAttack 只保留来源事件证据，待换人/跨角色归属设计确认后再决定是否迁入。
- [ ] 12.4 迁移 Float32/Fixed 输入、Local/Fixed/Rollback Host、Control Source 和 Factory 的 Camera/basis 接口，交付无具体旧 Controller 依赖的调用清单，输入与网络原逻辑保持原样。
- [ ] 12.5 迁移 GameplayLab Builder、性能采集/回放的初始相机接口及全部明确 Scene/Prefab bindings，交付无旧 FreeLook axis 直写的引用清单及正式绑定校验结果。
- [ ] 12.6 完整切换唯一正式入口并删除旧 Controller、State/Modifier 空实现、Mode/FOV 映射、旧 Cue/字符串/序列化配置和过期 generated 合同；交付旧符号与资产引用搜索结果，无法映射的既有正确行为先报告冲突。

## 13. 完整发布与规范收口

- [ ] 13.1 通过精确 Definition 的既有 Build 发布请求的 Float32/Fixed 与唯一 Projection，交付通过原子发布和相机依赖校验的完整产物组；不运行 Unity batchmode。
- [ ] 13.2 汇总来源、资源、编译、运行消费者、编辑入口和诊断的完整性报告，所有已纳入项无未解析/无消费者/旧路径状态，未达到则保持变更未完成。
- [ ] 13.3 对照本 delta、current v4 spec、未来 v5 owner 边界、ScenePlay active change（若已安装）和其它 active change，同步 Camera 相关主规范及 project context，交付冲突对账结果；不覆盖 Pose、Body、PIK、Performance 已有正确变更，也不把 v5/ScenePlay 未安装接口写成 current truth。
- [ ] 13.4 整理本变更各小步中文提交、作者资源和生成产物身份，交付精确变更范围及用户可跳转的最终作者/运行入口；如使用 .NET 构建，命令带 `--disable-build-servers /nr:false /p:UseSharedCompilation=false` 并立即执行 `dotnet build-server shutdown`。
