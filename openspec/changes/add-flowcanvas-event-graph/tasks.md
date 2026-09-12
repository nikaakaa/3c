## 1. 原生图与公共合同

- [ ] 1.1 保留已有效的 HostEventGraph、领域无关宿主合同与原生 FlowScript 运行；交付现有实现保留清单，不重建事件 Compiler 或执行器。
- [ ] 1.2 保持同一原生声明投影出的唯一 Contract/Layout/Frame；交付图/Variable.ID、类型、实例与采样身份对照，不增加第二份声明或布局。
- [ ] 1.3 保持 Float/Int32/Bool 精确输出与图内 Vector2/Vector3，沿原局部规则处理类型和只读权限；交付明确 API 合同，不新建中央 Validator。
- [ ] 1.4 按 design D7 登记事件图三份领域文件、Pose调用文件、Agent Mapper及公共输出器的唯一Owner；交付两项删除各自的消费者依赖，不再登记Document升版。

## 2. 原生作者直接API

- [ ] 2.1 保留原生 GraphEditor、Blackboard、拖拽、Inspector及现有Undo；提供人工编辑与C#共用的变量/节点/配置/连接直接API，交付无整图DTO必经中转的调用链。
- [ ] 2.2 从 HostEventGraphEditorMutation 原实现复用有效 typed 配置、身份、端口顺序和局部约束，补齐可读/可恢复字段；交付节点、Get/Set目标及赋值模式的直接调用对照。
- [ ] 2.3 保持已有原生克隆、Manual驱动、调用身份和错误处理，不因作者协议退役修改运行语义；交付保留入口和有效行为说明。
- [ ] 2.4 保留Macro闭包、接口、共享引用与生命周期，提供生成所需的正式读取和配置；交付逻辑ID、端口、owner与内部对象绑定合同。

## 3. 动画宿主与输出

- [ ] 3.1 保持正式 FactFrame/delta → 初始化或一次更新 → 完整输出的唯一动画接入；交付不依赖Agent协议的现有宿主链。
- [ ] 3.2 保持typed只读帧的实例、表现采样、Simulation tick、Reset代际、合同/layout版本和租约；交付消费者完成前不覆盖的数据寿命。
- [ ] 3.3 保留Node/Graph按实例失败通知、失败不发布及Editor/Player一致结果；交付不扫描Console、不发布部分Set结果的正式边界。
- [ ] 3.4 保留暂停、Reset、Body discontinuity、Replacement、Dispose与Actor隔离；交付变量和节点历史共同清理的生命周期。
- [ ] 3.5 保留同步更新及时间准入，保持Pending不回退成功事件状态；交付Wait/Timed Split/全局时间模式等原约束，不改通用插件能力。

## 4. 公共C#输出与生成薄适配

- [ ] 4.1 为共同输出器完整读取当前原生变量、节点、typed配置、动态端口、Macro、连接、布局及引用；交付字段覆盖与未知正式内容的精确拒绝，不经Document/JSON。
- [ ] 4.2 按公共扩展合同输出正式创建/配置/连接API调用，复用通用值/语句输出；交付事件图薄适配，不另建导出器、源码模型或EventGraph MCP。
- [ ] 4.3 输出稳定图ID、原生Variable.ID和内部对象引用，区分生成闭包与外部资源；交付不依赖旧生成GUID、可删除重建的创建代码。
- [ ] 4.4 经正式API返回根输出并恢复明确Profile/owner挂接及跨Pose引用；交付相同逻辑身份与共享关系，不全局扫描消费者。
- [ ] 4.5 只接公共export_code/generate_assets，保存交由既有明确范围能力；交付人工编辑不导出、生成不合并未导出修改、两工具不自动Build的接入边界。

## 5. 正式动画链与消费侧交接

- [ ] 5.1 保留RuntimeFactory和Fact后的唯一动画事件宿主，维护动画根事件图引用及其直接配置API；交付不改变Pose根调度职责的入口。
- [ ] 5.2 向Pose消费侧提供唯一Contract/Layout/Frame与事件图直接API，合同通过代码和execution记录交付；不改Pose Get/条件/BlendSpace及调用适配所属文件。
- [ ] 5.3 在消费侧完成同一输入接入后收口生产切换、Pending/Fault行为；交付无重复更新或旧值补偿的正式顺序。
- [ ] 5.4 观察继续区分原生执行、变量发布和Pose completion；交付同实例来源标识，不新增预览时钟或节点重执行。

## 6. 两项独立删除与内容生成

- [ ] 6.1 在直接API可用后，依赖Pose调用适配迁出和C# authoring公共Mapper退役，删除EventGraphAuthoringDocument及其附属DTO、ApplyAuthoringDocument/ApplyDocument和无消费者协议解析；交付调用引用清单，不将模型改名保留。
- [ ] 6.2 按明确角色/Fixture生成范围与根绑定参数完成事件图薄适配内容接入；交付完整变量/节点/引用/布局重建与Get逻辑身份，不删除范围外资源或未授权人工内容。
- [ ] 6.3 独立等待Pose、条件、BlendSpace、运行与Preview全部迁入同一变量合同，再删除CharacterPresentationProgramParameterFrame、Supports、FromBody/FromFact/FromDirect及固定注册；交付全消费者迁移清单，不因Agent删除提前拆桥。
- [ ] 6.4 清理确认无消费者且受固定桥移除牵连的旧协调类和重复配置，保留真实坐标语义；交付明确旧路径去向，不默认补值或临时绕行。

## 7. 独立发布与文档收口

- [ ] 7.1 保持既有Build中的运行合同/layout、依赖hash、Stale、AOT/泛型和Replacement要求；交付作者生成与运行发布分开的入口，不引入Document包版本。
- [ ] 7.2 在消费接口与明确内容就绪后沿原精确Definition Build发布正式产物；交付独立Build结果，不以导出或生成保存成功代替运行完成。
- [ ] 7.3 归并本任务四份delta与公共C# r2，保留共享Requirement中的原生EventGraph例外，清除本任务的Document扩展口径；交付准确规范对账，不覆盖其它规划文件。
- [ ] 7.4 由实现窗口在唯一execution.md记录直接API、薄适配、完整往返、两项删除、中文小步提交及实际证据/限制；不新增测试代码或手动验证tasks。本次PLAN仅修订清单，不触发这些实施工作。
