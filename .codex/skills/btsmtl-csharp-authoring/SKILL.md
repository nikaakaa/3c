---
name: btsmtl-csharp-authoring
description: 通过正式 C# authoring API 和两个显式 MCP 管理 BTSMTL Skill、FSM、Timeline、Pose Graph 与 EventGraph 资产。Use when 用户要求导出或生成 BTSMTL C# authoring、修改正式 Graph/Timeline/Pose/FSM 资产，或提到 btsmtl.export_code、btsmtl.generate_assets。
---

# BTSMTL C# Authoring

## 规则

当前合同以 openspec/specs/character-csharp-authoring/spec.md 和正式领域 API 为准。C# 是一次明确执行的重建入口，不是第二份业务模型。

只使用两个作者 MCP：

- btsmtl.export_code：正式资产导出为可编译 C#。
- btsmtl.generate_assets：执行已编译入口并保存指定资产范围。

不恢复旧 Document/Agent 工具，不创建 JSON/YAML 中转、SerializedProperty 旁路、fallback、监听同步、源码 Undo、反射字段写入或自动 Build。代码文件用系统文件工具修改。

遇到其它窗口或其它模块的编译阻塞时，本窗口不发送跨窗口消息、不轮询其它任务；只记录阻塞并停止需要该程序集的 authoring 操作。

## 导出

1. 确认精确根资产、Definition、源码输出路径、recipe、入口类型和命名空间。
2. Unity 工具调用必须显式指定目标 unity_instance；目标必须非 Play、非编译、非导入。
3. export_code 直接读取正式对象闭包。失败时保留已有源码，不写半份结果。

输出只描述最小重建闭包：

- 保留类型、拓扑、业务顺序、owner、根绑定、有效作者配置、节点值、动态端口、Blackboard、FSM、Timeline 数据段和精确外部引用。
- 默认值、固定结构、派生值、tick/采样/分析缓存、历史过程、诊断 hash、变量名 GUID 后缀不输出。
- graph/node/edge/variable identity 只在正式引用、稳定拓扑或 owner 需要时保留原值；外部资源需要精确定位时保留路径和 localFileId。
- 同一外部资源在源码中声明一次后复用；类型用必要的 using/别名。
- Timeline 保存源 Clip/数据段、源区间、时间轴位置和正式作者覆盖。独立作者曲线保留完整关键帧、切线、权重和时间域，不重采样。
- Builder 直接调用正式领域 API；共享对象先声明再连接，不能复制第二棵领域树。

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

编译失败时停止 generate_assets。生成只接受精确源码路径、已编译入口、Definition 和输出路径；不接受任意源码正文或旧同名入口。响应必须区分保存、创建、替换、删除和 diagnostic。

## 领域归属

- Skill/FSM：正式 Graph、Macro、Blackboard、Native FSM、State Body、Timeline 和连接 API。
- Timeline：TimelineData、正式 Track/Clip/Section、typed binding 和曲线 API。
- Pose：正式 Pose Graph、Slot、StateMachine、Rule、layout 和 Mutation API。
- EventGraph：正式 Host、typed adapter 和 Mutation API。

当前不覆盖任意未登记节点、EventGraph CanvasGroup/外部序列化、TreeClip 内联树或 Character 产品 Build；遇到无法表达的正式内容必须诊断失败。

人工编辑不会自动导出源码。需要更新源码时重新明确调用 export_code；需要更新资产时重新明确调用 generate_assets。没有运行、Build 或端到端证据时，不作相应完成声明。
