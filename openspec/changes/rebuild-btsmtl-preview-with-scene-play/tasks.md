## 1. 固定迁移范围与规范对账

- [ ] 1.1 盘点 Timeline、Graph、Pose Graph、Blend Space、MM 和 Action Workspace 的完整角色预览调用链、序列化字段及资源引用，在设计迁移表中列明每项的新归属或删除位置。
- [ ] 1.2 按本提案已确定的场景运行方向对齐相机和 Pose active change 的重叠 Preview 说明与任务，交付不再要求独立表现 fixture 或窗口 seek 的一致计划；保留其算法、资源和已完成运行实现。
- [ ] 1.3 明确记录脚部分析生成的既有代码/spec 分歧和本次边界，在迁移表中区分完整角色播放器、离线分析/校准、原生素材编辑与独立模块诊断工具，不把后面三类误删或改成预览补充路径。

## 2. 共享合同与场景上下文

- [ ] 2.1 定义场景预览操作、只读状态、请求 identity 和场景 generation 合同，接口能够表达开始、暂停、继续、重建、结束及每个失败阶段，不持有 Simulation/Presentation 工作区。
- [ ] 2.2 在已有客户端 Unity 边界实现场景上下文声明，序列化准确 Session/Actor 引用并显式登记和撤销；唯一性、场景归属与 roster 不匹配具有稳定诊断。
- [ ] 2.3 为领域适配器提供运行调参资格与字段编辑影响合同，输出来自共享 Capability 和现有参数布局，保留所有原有值域、生效时机与只读规则。
- [ ] 2.4 配置 Editor、客户端与公共 Composition 的单向依赖，程序集引用中不出现公共 Simulation 到 Editor、窗口、Animancer 或具体 Network Model 的新增反向依赖。

## 3. 通用场景启动器

- [ ] 3.1 将现有 `EditorPlayModeSceneLauncher` 收敛为明确启动请求，原有调用者迁入同一入口，删除旧单 active scene 路径假设和并行恢复实现。
- [ ] 3.2 接入指定 Play 启动场景及原设置恢复，保存完整 Scene setup；启动请求结果能够区分成功、用户取消保存与配置失败。
- [ ] 3.3 将必要请求身份和编辑环境信息持久化到 editor-only 会话状态，Domain Reload 后只能重新解析合法请求，数据中不保存旧 Runtime 或 GameObject 实例。
- [ ] 3.4 按 Unity Play 生命周期完成进入、退出、取消和失败清理，终态保留操作诊断并清空待执行命令，不出现自动重启。
- [ ] 3.5 迁移现有 prepare 调用者对场景起始条件的传递，正式场景作者值不再被运行准备临时改写，既有 Launcher 仍按明确配置启动。
- [ ] 3.6 实现受控场景在 Play 内的正式重载操作，返回新的场景 generation，并由同一启动器管理加载失败和最终编辑环境恢复。

## 4. 唯一场景预览协调器

- [ ] 4.1 实现检查、等待构建、进入 Play、准备、运行、暂停、重建、停止和失败状态，公开状态能够定位当前等待或失败阶段。
- [ ] 4.2 加入实例级运行所有权和请求校验，第二次启动及外部 Play 占用返回明确结果，不自动抢占或选择其它场景。
- [ ] 4.3 从选定场景上下文取得准确 Definition/构建目标，正式 Session Active 后才允许控制；加载后发现缺失产物时返回 Edit Mode 的精确构建状态，不猜测 roster。
- [ ] 4.4 将暂停和继续连接到真实 Unity Play 状态，清理旧 Editor 定时推进依赖，公开状态与原生 Pause/Stop 操作一致。
- [ ] 4.5 实现重建前产物检查、旧目标失效、正式 Quiesce/Dispose 和新场景重新连接；保留重建前的暂停意图并通过正式调度完成准备，产物不匹配时要求明确构建。
- [ ] 4.6 处理窗口全部关闭、Unity Stop、请求丢失和运行 Fault，窗口只撤销本地 interest，预览控制资源只由协调器与正式 owner 释放。

## 5. 独立场景与正式输入

- [ ] 5.1 创建 `Assets/Scenes/Authoring/BtsmtlPreview.unity`，明确装配稳定 Corin 的正式 Prefab、启动与 Tick 依赖、Session/Composition、必要物理环境和已有相机，资源引用通过现有资产完整性校验。
- [ ] 5.2 接入明确场景和角色选择表面，场景保存环境与起始条件，窗口只保存编辑视图选择；无目标、重复上下文和缺失依赖均有可定位结果。
- [ ] 5.3 将动作试验输入连接到当前角色已有 Control Source/Ingress 合同，交付准确输入映射与唯一输入 owner，不直接指定 State、Action winner、动画或相机效果。
- [ ] 5.4 投影正式输入映射、Action admission 和目标缺失诊断，动作未被接受时窗口显示真实结果并提供正式调用点导航，不创建替代 producer。

## 6. 作者窗口与真实运行观察

- [ ] 6.1 在共享 Graph Shell 装配领域提供的场景预览操作与状态，现有 Navigator、Canvas、Details、Bottom Dock、selection、clipboard 和 Undo 仍由原组件拥有。
- [ ] 6.2 将 inline/shared Timeline 的播放控件改接场景操作，编辑游标、实时运行标记和 Capture 历史位置独立表达，Timeline Field 不再拥有预览 evaluator。
- [ ] 6.3 迁移 Tree/StateMachine 和 TreeClip 下钻的本地运行绑定，窗口 navigation/close 不改变正式 Session，多个 playback 保持显式 Follow/Pin 和准确 source map。
- [ ] 6.4 迁移 Pose Graph Bottom Dock、Pose Watch 和目标选择，观察来自真实 Actor 的 committed snapshot，删除私有 Fact Preview 装配。
- [ ] 6.5 迁移 Blend Space 与 MM 的完整角色预览入口，实际 Fact/Query 来自正式角色；保留采样点、曲线和几何等作者数据绘制。
- [ ] 6.6 迁移 Action Workspace 到同一场景与 Actor，显示真实 Action、logic/visual time、Slot、最终 Pose 及已安装相机结果，移除 Base Pose/Action playback fixture。
- [ ] 6.7 迁移原生 Animation Window 的 typed navigation 签名和显式素材编辑目标，Production Prefab 不安装素材接收器，运行中的物理输出不被素材采样接管。

## 7. 直接作者调参与运行采用

- [ ] 7.1 将各页面运行参数入口统一降低到现有正式 Mutation、Validator 和 Undo，修改写入真实作者 owner，非法参数在写入与提交前被拒绝。
- [ ] 7.2 复用精确 Actor 的现有参数候选编译和原子提交协议，保留 Program/Projection/布局身份、NextFrame/NextActivation 与 `resetOwnerState` 语义，不修改共享不可变产物。
- [ ] 7.3 实现作者已修改、运行待生效、已采用、需要 Build 和应用失败的分别显示，以正式运行确认更新状态，不用提交成功代替生效。
- [ ] 7.4 接通 Undo/Redo 的同一路径候选更新，运行应用失败时保留作者修改、Undo 和上一份运行参数，诊断能够指出两者差异。
- [ ] 7.5 处理暂停、共享 Profile 与多 Actor 的采用状态，只向明确选中的 Actor 提交，暂停期间不主动执行帧，其它 Actor 不被暗中修改。
- [ ] 7.6 处理结构变化、外部修改和参数布局失效，受控预览停止使用不匹配产物，结构编辑与明确构建保留 Edit Mode 边界。

## 8. 明确构建与阶段耗时

- [ ] 8.1 接通构建并开始／构建并重启，流程只在 Edit Mode 调用现有精确 Definition/Target Build，失败保持编辑状态并返回正式诊断。
- [ ] 8.2 在现有构建报告中记录前端、确定性检查、表现计划、Numeric Target 和发布的实际耗时，不改变编译算法、产物或确定性检查。
- [ ] 8.3 为已有明确分析操作记录生成或复用状态，使报告能够区分分析工作与图数据编译；不因预览新增分析生成触发或改变现有产物所有权。
- [ ] 8.4 在预览请求中记录检查、进入 Play、Session 准备、目标连接和重建耗时，未测量和失败阶段可见，轮询总等待不作为编译耗时。
- [ ] 8.5 将状态与报告接入共享 UI 和现有构建任务返回，不在 Inspector 绘制、selection 或资产刷新中执行重操作，不增加热更新插件或重载设置修改。

## 9. 删除被替代的完整角色预览路径

- [ ] 9.1 删除窗口级 `TimelinePreviewSession` 和 `TimelinePreviewTarget` 依赖，迁移 `CharacterPipelineHost` 继承与调用者，保留已有组件资产 identity 及正式运行端口。
- [ ] 9.2 删除仅服务旧完整角色预览的 Controller、Runtime、worker adapter、`PreviewSession`、preview program 和 Action/Fact/Query adapters，所有保留调用者都有明确业务归属。
- [ ] 9.3 删除旧 Pose/动画私有场景 fixture、视觉根接管与恢复、预览动画时钟、独立 MotionCurve 求值以及仅为它们存在的配置和资源引用。
- [ ] 9.4 清理旧窗口字段、UXML 控件、菜单、目标选择、playback/seek 分支和失效 editor state key，正式入口不保留旧路径开关。
- [ ] 9.5 删除完整角色预览专用 seek/reset 输入，同时保留正式 Actor reset、Fault、Dispose 和已验证惯性/Foot 初始化，不改算法或建立新恢复分支。

## 10. Document与规范同步

- [ ] 10.1 将运行编辑资格与作者字段变化同步到共享 Capability 和需要的只读 context，Document v4 模型、Exporter/Codec/Reconciler/Mutation/Validator 对同一字段保持一致，不增加 runtime editable 字段。
- [ ] 10.2 保持人工调参后的 TreeDirty/Conflict、五个 Document 生命周期和 Play Mode 门禁，退出后的正式导出使用真实作者值而非运行采用值。
- [ ] 10.3 同步 `btsmtl-agent-authoring` 技能和本提案涉及的 current spec 安装内容，更新旧 Preview Purpose、`project.md` 的入口说明及 v3/v4 过时表述，保留明确列出的素材和模块诊断合同。
- [ ] 10.4 完成相机、Pose、ACL 等 active change 的交叉引用核对，文档中不再同时要求独立表现播放器和场景真实运行，不改其它变更的算法及已完成工作。

## 11. 集成门禁与交付

- [ ] 11.1 通过现有编辑器编译和程序集依赖门禁，保留项目已有错误与本次错误的区分；不运行 Unity batchmode，不新增测试代码。
- [ ] 11.2 通过现有场景、作者资产、产物与 Document 校验器的适用检查，交付引用闭合的独立 Corin 场景和准确失败诊断，不自动修补 TrainingEnemy 或替换 Numeric Target。
- [ ] 11.3 对迁移清单执行定向源码与资源引用检查，完整角色播放入口只剩统一场景运行，原生素材编辑和离线分析入口保持完整。
- [ ] 11.4 完成本次变更的 OpenSpec 严格校验和文档对账，按模块生成独立中文提交记录，交付文件路径、实际完成范围和遗留的外部冲突。
