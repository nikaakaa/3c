---
name: btsmtl-csharp-authoring
description: 通过正式 C# authoring API 和两个显式 MCP 管理 BTSMTL Skill、FSM、Timeline、Pose Graph 与 EventGraph 资产。Use when 用户要求导出或生成 BTSMTL C# authoring、修改正式 Graph/Timeline/Pose/FSM 资产，或提到 btsmtl.export_code、btsmtl.generate_assets。
---

# BTSMTL C# Authoring

## 规则

当前合同以 openspec/specs/character-csharp-authoring/spec.md 和正式领域 API 为准。C# 是一次明确执行的重建入口，不是第二份业务模型。

这是 Agent 作者工具：只负责调用表达、文件组织、输出管理和诊断。正式字段、默认值、身份/owner、业务校验、创建/事务/保存、编译和运行均归正式领域；不把它们复制进工具，也不在工具内新增攻击阶段、连段或其它业务模板。

唯一来源目标：实例值从正式资产读取，成员、默认/覆盖语义、身份/owner、端口和创建操作从核心共享作者合同读取。Agent 不维护具体节点/Clip 字段清单、Priority=100 等默认常量或逐业务类型 switch；普通类型/字符串/数值的 C# 编码属于工具职责。核心在既有合同内增删字段、改默认值、扩展同类节点时不应修改 Agent。导出前必须确认当前核心合同已经公开所需字段和操作；缺口要失败并定位到对象、字段或操作。

缺少正式描述时列明核心合同缺口，不把旧适配器搬目录当作去重，不新增 Agent 字段表、运行模型或反射猜测。真正破坏性的公开协议变化或新通用值类型可能需要公共适配；不恢复兼容业务分支。领域变化不自动导出源码，仍只响应显式 export/generate。

已有多文件能力保留：一个根一个专属 Generated 目录、一个执行入口，明确范围内清理退役输出，相同内容不写，手写扩展在范围外。局部输出采用短入口、根局部实现和按正式维护边界划分的阶段文件；不把 emission 阶段机械转录成包装函数，不解析旧 C# 或自动同步，不为分文件新增领域对象。只有真实独立维护内容才拆文件；只在收口阶段赋值、没有独立局部语句的对象合并到同一逻辑分组，不生成空 Parts 文件。

只使用两个作者 MCP：

- btsmtl.export_code：正式资产导出为可编译 C#。
- btsmtl.generate_assets：执行已编译入口并保存指定资产范围。

不恢复旧 Document/Agent 工具，不创建 JSON/YAML 中转、SerializedProperty 旁路、fallback、监听同步、源码 Undo、反射字段写入或自动 Build。代码文件用系统文件工具修改。

遇到其它窗口或其它模块的编译阻塞时，本窗口不发送跨窗口消息、不轮询其它任务；只记录阻塞并停止需要该程序集的 authoring 操作。

## 导出

1. 确认精确根资产、Definition、入口源码路径、recipe、入口类型和命名空间。入口路径使用 `Assets/GameScripts/Main/Editor/CharacterPipeline/Authoring/CodeGeneration/Generated/<Root>/<Root>.cs`。
2. Unity 工具调用必须显式指定目标 unity_instance；目标必须非 Play、非编译、非导入。
3. export_code 直接读取正式对象闭包。失败时保留已有源码，不写半份结果。

输出只描述最小重建闭包：

- 保留类型、拓扑、业务顺序、owner、根绑定、有效作者配置、节点值、动态端口、Blackboard、FSM、Timeline 数据段和精确外部引用。
- 默认值、固定结构、派生值、tick/采样/分析缓存、历史过程、诊断 hash、变量名 GUID 后缀不输出。
- graph/node/edge/variable identity 只在正式引用、稳定拓扑或 owner 需要时保留原值；外部资源需要精确定位时保留路径和 localFileId。
- 同一外部资源在源码中声明一次后复用；类型用必要的 using/别名。
- Ability 中的外部 Timeline 只保留正式资源引用，不重复输出它的轨道、Clip、属性或曲线；同一资产文件内的私有 Timeline 才输出数据段、源区间、时间轴位置和正式作者覆盖。正式默认曲线不展开关键帧；独立非默认作者曲线仍保留完整关键帧、切线、权重和时间域，不重采样，除非核心合同先提供对应的正式曲线源资产。
- Builder 直接调用正式领域 API；共享对象先声明再连接，不能复制第二棵领域树。
- 目标入口只组合局部结果、必要共享资源、跨局部连接和 Complete；不逐项转录 emission 阶段或生成几百个 BuildCreate/BuildConfigure 调用。正式依赖顺序仍保留。
- 阶段私有节点、参数、位置、小条件和 Timeline 放在同一文件；只为真实共享或独立维护内容拆出文件，不固定生成三套目录。私有对象使用局部变量，不建立全对象执行状态，只返回跨边界确实需要的类型化结果。
- 节点创建后保留具体类型，就近表达有效配置，删除无用 cast、别名和包装函数。只按正式默认语义完整省略默认参数/曲线，不能另建工具默认表或攻击模板。
- 每文件只输出实际需要的 using/别名，不复制完整列表，不增加 global using 或隐藏导入配置；入口合同和生成上下文所需的正式框架 using 必须保留，不能因它没有出现在领域 using 列表中被裁掉。分文件若增加大量包装和共享字段，应合并局部，而非继续拆分。
- 同一外部资源在本次调用中解析一次，私有资源就近、共享资源明确传递；同一根文件内容未变化时不重写，也不改已有 `.meta`。结果说明区分总源码量、入口与局部修改范围，不用文件数代替压缩收益。

MotionCurve 按正式 API 输出 RootMotionCurveAsset 引用与使用配置，不展开源关键帧。export_code 不隐式提取素材；generate_assets 不复制、重烘焙或删除范围外源。具体源所有权和时间规则查当前 `openspec/specs/character-root-motion-curves/spec.md`，工具不重新实现这些规则。

未知字段或无法由正式 API 表达的内容必须报告并失败，不能用零值、占位或猜测补齐。

## 生成

入口类只实现：

~~~csharp
BtsmtlAuthoringGenerationResult Execute(BtsmtlAuthoringGenerationContext context)
~~~

recipe、源码路径和入口类型属于工具请求及服务层。服务必须确认请求类型、精确源码与当前编译脚本关联；它们不写入生成类。

静态编译使用：

~~~text
dotnet build ThirdPersonClient.Editor.csproj --disable-build-servers /nr:false /p:UseSharedCompilation=false
dotnet build-server shutdown
~~~

编译失败时停止 generate_assets。生成只接受精确入口源码路径、已编译入口、Definition 和输出路径；入口目录内的 partial 文件随当前编译关联参与。不接受任意源码正文或旧同名入口。响应必须区分保存、创建、替换、删除和 diagnostic。

## 正式 API 边界

Skill/FSM、Timeline、Pose、Motion 和 EventGraph 均消费各自正式 typed API/metadata/mutation。只在任务涉及相应业务时读取其当前 spec/API；本 skill 不定义运行链或维护领域规则副本。已有 builder 和生成入口保留，缺少能力时报告具体正式 API 缺口，不通过反射、工具业务函数或新事务填补。

当前不覆盖任意未登记节点、EventGraph CanvasGroup/外部序列化、TreeClip 内联树或 Character 产品 Build；遇到无法表达的正式内容必须诊断失败。

错误应保留正式诊断并附上根、局部对象、字段及对应文件位置，不能另建领域 validator。工具只消除自身重复的保存/刷新调用，不改变正式事务或在 Inspector 重绘中做重操作。

人工编辑不会自动导出源码。需要更新源码时重新明确调用 export_code；需要更新资产时重新明确调用 generate_assets。没有运行、Build 或端到端证据时，不作相应完成声明。
