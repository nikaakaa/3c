## 当前范围

按2026-09-13 r2执行：共同业务定义与共享metadata归本任务；export_code/generate_assets、JSON binding退役和公共协议删除归C# authoring。FSM与事件图保留各自职责。已有TransferPayload、真实端口和组合步骤成果继续使用，不重新实施旧v8发布或包重建。

旧任务的勾选和未完成记录已原样转为[implementation.md](implementation.md)中的历史表。以下只列r2剩余实现，不含测试、验证、包往返或等待运行证据任务；本轮文档修改不勾选实现完成。

## 1. 共享字段与端口合同

- [x] 1.1 补齐GraphAuthoringCapabilityCatalog.cs及共享字段描述中的正式typed读取、默认值/约束和配置入口绑定，由所属领域提供真实业务方法，供人工UI、C#输出适配和compiler消费。
- [x] 1.2 将固定、条件和动态端口统一从正式Capability、typed参数及真实动态接口投影，原生节点只物化结果；保留已正确的端口、Macro接口和组合步骤，移除本任务范围内重复规则。
- [x] 1.3 让共享注册只组合各领域正式能力模块，明确类型、role、字段和引用入口；保持领域业务规则、运行、Undo和保存责任独立，不建立中央Validator或领域对象全集。

## 2. 原节点与FlowCanvas共同定义

- [ ] 2.1 将尚未统一的原业务节点与FlowCanvas同义参数、默认值、局部校验和引用规则收进原业务模块，两个宿主分别持有实例值；真实差异和未决字段保留在本设计，不自动覆盖。
- [ ] 2.2 将对应原节点、FlowCanvas目录和人工字段入口改为消费共同定义，保留明确本地端口/identity映射，不再维护第二份业务声明。
- [ ] 2.3 将受影响的业务编译与引用读取接入同一正式参数接口，复用既有lowering和正确数据规则；不构造旧作者节点执行，不改FSM或事件变量运行。

## 3. 向C# authoring提供正式消费入口

- [ ] 3.1 为本任务拥有的Skill共同定义提供当前typed字段值、正式引用目标和创建/配置入口描述，接入C# authoring的公共扩展合同，不输出JObject、AgentPackage或第二份节点/owner模型。
- [ ] 3.2 在所属Skill业务模块补齐确实缺失的读取或Configure/Set类API，供人工编辑与代码创建共用；不修改BtsmtlSkillNodeAuthoringBinding.cs和TimelineAuthoringClipBinding.cs的JSON退役实现。
- [ ] 3.3 将本任务拥有的UI、目录和compiler消费方改接正式metadata/API，对其它领域保留清楚的输入输出依赖；不实现两个MCP、通用代码输出/生成、源码解析或自动同步。

## 4. 删除重复与退役协议字段

- [ ] 4.1 对合法调用者已经改接的共享字段，删除DocumentCodecId等仅供旧Agent协议使用的描述及重复规则；保留实际用于领域role、真实端口、owner和业务方法的内容，不按Document名字整体删除。
- [ ] 4.2 删除本任务拥有的重复参数字段、目录声明、校验和已无消费者旧适配；保留正确TransferPayload与普通步骤，不恢复旧Step补读、不重做FSM迁移。
- [ ] 4.3 将本目录节点模型与共享metadata独有规范增量同步到正式文档，清除本任务旧Agent协议消费要求；协议整体删除和版本收口仅由C# authoring负责，不改其它任务文档。
