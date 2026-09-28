# 实施切片清单（排查后）

R1—R10 的本轮静态排查已完成，证据和取舍见 design 文末。本清单只记录实施切片，不构成自动实施授权。全部保持未勾选，工作区已有代码不等于本 change 已交付；现有表现恢复能力缺失不在本次 History 提取中补建。

R1／R2 的已有提交、R8 的保留结论、R9 的证据与专项归属记录在 design，不复制成新的已完成任务。测试、手动验证、用户验收与采集运行不列为 tasks；实际修改仍按项目规则执行基本检查并如实报告。

## 1. 收口会话历史职责（R3）

- [ ] 1.1 完成 SimulationSessionHistory 对检查点集合、裁剪缓冲与历史分支的唯一持有，Host 只接入 runtime、roster 和已完成 Tick。
- [ ] 1.2 将捕获、精确恢复、目标 Tick 恢复和输入区间恢复统一接入 History，恢复既有 capability／检查点／兼容性失败优先级和错误文案；保留真实回滚，内部助手不重复公开入口检查。
- [ ] 1.3 在实际 runtime 正常与失败释放边界解除 History 借用引用，保持新会话重置和原销毁顺序，删除 Host 中被替代的历史字段及旧实现，形成完整独立提交。

## 2. 收口动画采样职责（R4）

- [ ] 2.1 完成 AnimationSampler 对预览场景、采样实例、PlayableGraph、NativeArray 和临时 Clip 的统一生命周期管理。
- [ ] 2.2 让 Analyzer 从同一规范样本数组执行现有几何、特征与 Motion Data 构建，保持 Target／Motion Reference 的采样时刻、坐标空间、loop 及末端语义。
- [ ] 2.3 移除 Analyzer 内被替代的采样环境与重复辅助入口，保留唯一正式分析入口、既有数值算法及 artifact／曲线输出。

## 3. 整理Pose作者工作区（R5）

- [ ] 3.1 将新建 Graph 的创建 Undo 注册收拢到实际 GraphCatalog mutation owner，删除 C# 同一路径的重复注册，保留根资产及 SourceSlot／ResourceSlot 各自创建事务。
- [ ] 3.2 将 State／Alias 的领域数据创建交给现有 StateMachine mutation，窗口只传递 document、位置与选择并刷新页面；状态、子图、布局继续在同一 Undo 事务内。
- [ ] 3.3 由正式校验报告携带错误定位身份，统一作者检查与带 Rig 内容检查的输入模式，删除窗口和内部重复执行的同一能力／状态机校验。
- [ ] 3.4 收拢图、Profile 与其正式引用参数资源的保存操作，支持正式无 Profile 作者入口，移除 Tuning 模块内独立的保存协议。
- [ ] 3.5 将 C# 生成实际修改的独立 Profile 纳入正式生成事务的写 owner 集合，统一写入前快照、保存和失败回退；只读依赖不进入写集合，不调用 Workspace.Save 绕过生成事务。
- [ ] 3.6 将导航目录、图显示名、角色与规则 owner 查询移到只读作者目录职责，窗口保留选择、打开页面和导航历史。
- [ ] 3.7 删除已确认无消费者的 tuning 指纹字段、方法、分支及对应文件／meta，同时清理直接调用者。
- [ ] 3.8 删除只执行 Validate 的 Compile 入口、虚假编译成功提示和过时 Projection／Build 状态命名，更新 presenter 等直接消费者，保持实际采用状态来自正式 Session。
- [ ] 3.9 删除仅有定义的 m_ObservedPorts、DefinitionContextValue、RefreshRuntimeDetails 及同链冗余表达式，保留有真实消费者的 Live／只读合同。

## 4. 清理旧作者依赖（R6）

本轮证据支持 design R6 所列完整旧簇退役；实际实施前核对工作区新增消费者，不扩大到同名第三方类型和正式 Flow 合同。

- [ ] 4.1 清理已不需要的 TreeDesigner import 和正式 C# 导出模板；同步已生成代码，修正 EnumMenu.uxml 旧路径并保留其共享样式 GUID。
- [ ] 4.2 删除 design 列出的七文件三十九个旧类型、ActionTargetSnapshotExposedProperty 和孤立的 ISubmitActionLifecycleAuthoring；保留 ActionTargetSnapshot、其余正式接口与 Camera Flow 节点。
- [ ] 4.3 删除 GameplayAbilitySemanticFrontendCompiler 中不可达的三个旧 Equipment 类型判断、requiresEquipment 状态及对应能力声明，不引入新的 Equipment Flow 功能。
- [ ] 4.4 在同一个完整旧簇提交中删除 TreeDesigner 包、反射发现机制、三个 BaseTreeAsset 旧菜单和目录 meta，并移除 Character Runtime／Editor 两个 asmdef 依赖；不留无法编译的中间提交。

## 5. 按已有职责整理文件（R7）

- [ ] 5.1 将 TimelineRuntimePreparation 中既有 Preparation、Playback、Evaluator、缓冲与合同按责任归文件，保持类型与运行语义。
- [ ] 5.2 将 CharacterFootSwingMotionBuilder 中合同、诊断与算法按现有职责归文件，保持数值计算、字段布局和访问方式。
- [ ] 5.3 将 CharacterTimelineHost 同文件中的独立服务／合同归到对应现有职责文件，保持新 GraphBindings 与唯一播放状态，不增加转发模块。
- [ ] 5.4 清除 BtsmtlPreview.unity 与 GameplayLab.unity 中四个仍引用普通 C# TimelineHost 的失效 MonoBehaviour 对象及对应 m_Component fileID，保留 GameObject 和其他组件。

## 6. 收拢表现外围失败与释放（R1）

- [ ] 6.1 在现有 Presentation owner 收拢 Pose 成功后 Timeline／桥／时钟／Camera 业务收尾的失败归属，使用同一 Actor 表现故障结果阻止后续帧，不重复建立 Pose 阶段驱动。
- [ ] 6.2 将会拒绝的业务检查放回正式准备／验证阶段，明确 Timeline 业务事件与纯观察输出的处理，已提交后不伪装物理回滚或吞掉业务失败。
- [ ] 6.3 完成 DomainSession／Services 和 Presentation 各资源 owner 的异常收尾，保证一个释放失败不跳过剩余已取得资源，并保留首故障与清理错误。

## 7. 文档与迁移收尾（R10）

- [ ] 7.1 在正式规格同步阶段应用 Pose 整帧／外围故障与 Workspace 行为增量，同步 Native Pose 外壳入口以及 Dense Pending 的旧 Program 措辞。
- [ ] 7.2 按 design R10 的精确清单修订 Foot artifact／曲线／领域资源准备的过时发布表述、BlendStack 原生装配表述，并在对应规划阶段形成缺少的 companion delta；保留可达关系质量要求与有效资源计划，不把缺实现证据改成删除要求。
- [ ] 7.3 更新 R1—R10 的最终状态、对应提交、保留理由、专项交接和证据限制；HoldLastFrame、性能归因和表现恢复新能力继续由明确的原专项或新业务方案负责。
