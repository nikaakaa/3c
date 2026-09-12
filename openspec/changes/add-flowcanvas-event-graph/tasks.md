## 1. 原生图与公共合同

- [ ] 1.1 建立 HostEventGraph 与领域无关宿主合同，复用 FlowScript 和现有程序集；交付代码依赖中不出现动画、角色或 Skill 业务类型。
- [ ] 1.2 从原生 Blackboard 投影稳定变量引用与动画 Contract/Layout，保留原生 ID、类型和初值；交付唯一公共合同，不增加第二份可写声明。
- [ ] 1.3 定义 Float/Int/Bool 输出及图内 Vector2/Vector3 能力，提供类型与只读权限校验；交付精确 typed 字段及错误路径，不以 float/object 中转所有值。
- [ ] 1.4 登记本次图种与届时唯一 Document 发布版本、消费侧字段归属；交付 execution.md 中的精确基线与交接记录，不产生同版本分叉。

## 2. 原生作者与执行接入

- [ ] 2.1 接入原生 GraphEditor、Blackboard、创建/拖拽、Inspector 和 Mutation 路由；交付单一作者入口，保留当前只读列表拾取能力。
- [ ] 2.2 建立同一节点/成员能力目录与宿主准入，覆盖初始化、更新、Get/Set、计算、比较、分支、Flip Flop 和同步 Macro；交付 UI、Document、Validator 共用的描述。
- [ ] 2.3 实现每实例原生克隆、Manual 驱动和一次调用身份；交付无自动组件 Update、无新事件 IR/Compiler 的正式运行器。
- [ ] 2.4 接入 Macro 闭包、参数和实例生命周期；交付稳定引用、禁止递归和跨帧节点的正式校验，保留原生调用语义。

## 3. 动画宿主与输出

- [ ] 3.1 提供正式 FactFrame 与本次表现 delta 的只读输入，接初始化和更新入口；交付从已有事实采样到原生调用的唯一适配。
- [ ] 3.2 实现成功调用后的 typed 变量帧及只读租约；交付带实例、采样、图/布局版本和 Reset 代际的输出，不向 Worker 暴露原生可变对象。
- [ ] 3.3 在原生 Node/Graph 错误边界接入按实例失败通知；交付 Editor/Player 一致的失败结果，部分 Set 后不发布、不扫描 Console 补判成功。
- [ ] 3.4 接通暂停、Reset、Body discontinuity、Replacement 和 Dispose；交付变量与节点历史共同清理、另一 Actor 不受影响的生命周期。
- [ ] 3.5 落实同步更新和时间准入规则；交付对 Wait、Timed Split、perSecond/全局时间模式和跨帧断点的明确诊断。

## 4. Document与资产事务

- [ ] 4.1 在现有 PresentationDocument 增加事件图、变量、Macro 和宿主 context 的 Codec/Exporter；交付 canonical 图/布局闭包，不输出私有序列化字段。
- [ ] 4.2 在唯一 Reconciler/typed Mutation/Validator 接入事件图修改和跨 Pose 变量引用；交付删除悬空引用、非法类型和缺失依赖的精确诊断。
- [ ] 4.3 将新 owner 纳入既有 Undo、保存、失败恢复与反向导出；交付同批事件图和消费引用的原子资产事务，不增加局部工具入口。

## 5. 正式动画链与消费侧交接

- [ ] 5.1 由现有 RuntimeFactory 装配唯一动画事件宿主，在正式 Fact 输入后、Pose 推进前调用；交付一条实际生产链，根 Pose 调度职责不搬入作者图。
- [ ] 5.2 向 readonly-blackboard change 提供已实现的唯一 Contract/Layout/Frame 和原生声明引用；交付明确 API 与文件/字段所有权，Pose Get/Compiler 消费不在本 change 重做。
- [ ] 5.3 在消费侧接通同一输入后完成生产调用处切换，落实 Source 未就绪保留原生状态、Actor Faulted 停止更新；交付无旧值补偿和重复事件推进的正式调用顺序。
- [ ] 5.4 对接既有只读观察，区分节点执行、变量发布与 Pose completion；交付同实例来源标识，不新增预览时钟或节点重执行。

## 6. 迁移与旧链删除

- [ ] 6.1 按消费侧现有正式角色/Fixture 清单创建或绑定动画事件图、变量与输入；交付精确资产引用，保护未提交 PoseGraph 修改，不重写角色状态机。
- [ ] 6.2 在所有运行和 Preview 消费者完成同一合同迁移后，删除 CharacterPresentationProgramParameterFrame、Supports 和 FromBody/FromFact/FromDirect；交付无旧生产类型引用的代码及资产清单。
- [ ] 6.3 清除旧固定 motor 提供者注册与重复配置，对照真实坐标语义保留既有行为；交付旧 ID 到正式变量的精确迁移结果，不按名称猜测。
- [ ] 6.4 确认 CharacterPresentationFrameCoordinator 的全部引用与编译条件，仅删除受旧桥移除牵连且无消费者的旧类；交付唯一主入口，保留其它任务仍在使用的实现。

## 7. 正式发布与文档收口

- [ ] 7.1 将图/宿主/变量依赖 hash、语义 Stale、合规反射成员与泛型保留纳入既有 Build；交付正式构建绑定，不增加事件图指令编译或运行时补建。
- [ ] 7.2 在消费侧接口和选定内容都完成后，通过原资产事务与精确 Definition Build 发布对应产物；交付唯一可消费的新版本，旧 ABI/包按正式迁移退役。
- [ ] 7.3 按本提案范围同步现行规范与 project 入口；交付 EventGraph 原生执行边界及关联消费/文档版本的对账，不改其它规划窗口的文件。
- [ ] 7.4 在唯一 execution.md 记录实现、中文小步提交、删除清单、正式检查和实际限制；交付可直接审查的证据，不把历史通过或文档齐全当作运行完成。
