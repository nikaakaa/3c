---
name: btsmtl-csharp-authoring
description: 通过正式 C# authoring API 和两个显式 MCP 管理 BTSMTL Skill、FSM、Timeline、Pose Graph 与 EventGraph 资产。Use when 用户要求导出或生成 BTSMTL C# authoring、修改正式 Graph/Timeline/Pose/FSM 资产，或提到 btsmtl.export_code、btsmtl.generate_assets。
---

# BTSMTL C# Authoring

## 正式边界

当前作者合同以 `openspec/specs/character-csharp-authoring/spec.md` 和正式领域 API 为准。C# authoring 是明确执行的创建/重建代码，不是第二份业务模型。

最小重建输出的后续规划见 `openspec/changes/minimize-csharp-authoring-reconstruction/design.md` 及其 spec delta。该变更尚未实施：下面的裁剪规则是目标口径，不能宣称现有导出器已经完成，也不要为了满足目标手删当前接口要求的成员或绕过正式 API。

只使用两个作者 MCP：

- `btsmtl.export_code`：正式资产完整导出为可编译 C#。
- `btsmtl.generate_assets`：执行已编译的 C# authoring 入口并保存指定资产范围。

不要调用或恢复 checkout、rebase、dry-run、apply、validate 等旧 Document 工具。不要创建 JSON 包、YAML 写入、SerializedProperty 旁路、fallback、自动同步、源码 Undo 或自动 Build。

## 工作流

1. 先确认精确根资产、精确 `CharacterPipelineDefinition`、输出路径和 recipe。读取 Unity Editor 状态；必须是非 Play、非编译、非导入，且每次 Unity 工具调用显式传入 `unity_instance`。

2. 明确导出时调用：

```text
btsmtl.export_code(
  asset_path,
  definition_asset_path,
  output_code_path,
  recipe_type,
  entry_type_name,
  namespace_name
)
```

导出器直接读取正式对象及其重建所需闭包，目标源码采用偏函数式的薄链式 builder 表达对象创建、作者配置、引用绑定、连线和根挂接。保留节点值、动态端口配置、Blackboard、FSM 状态/转移、Timeline 作者配置、每节点一份位置和精确外部引用；默认值、固定端口/结构由正式 API 恢复。“作者编辑过的信息”指当前最终配置及有意义的显式覆盖，不是编辑历史，也不区分 AI 与人工来源。graph/node/edge/variable identity 仅在正式引用、稳定拓扑或 owner 关系需要时保留旧值；不因字段可序列化或看起来像 GUID 就全量抄写。未知字段或无法表达的内容必须失败，不能用默认值补齐。

Timeline 只输出 Clip 数据段引用、源区间、时间轴位置及速度、混合等作者配置，不复制源 Clip 曲线，不输出逐 tick 采样、烘焙/分析缓存或编译结果。独立作者曲线保留关键帧、插值/切线/权重和正式时间域，不重采样或有损抽点；源引用缺失时报告正式内容缺口。Builder 直接调用正式领域能力，不新增持久化领域树或第二执行器；环和共享对象允许先声明再连接。

只输出影响正式重建的内容：省略正式 API 可确定恢复的默认赋值与可推导数据；显式覆盖若影响继承语义，即使值等于默认值也保留。不输出无消费 GUID、变量名 GUID 后缀、历史日志、诊断 hash 和生成过程信息。同一外部资源声明一次并复用；精确子资源定位需要的 GUID/localFileId 必须保留。节点位置与必要作者布局保留，选中态、视口与重绘缓存不输出。重建等价内容即可，不要求复制 YAML 或旧源码文本。

3. 入口使用正式创建合同，只调用正式领域 API，不加入旧 Document 类型或局部 JSON。目标合同仅保留执行入口与类型声明；recipe、入口类型和源码路径由工具请求及现有编译关联承担，不再在创建类中输出绝对 `SourceCodePath`、重复 `EntryTypeName` 和调度 `RecipeType` 属性。当前旧接口尚要求这些属性，须在正式迁移时统一修改接口、服务和生成源码，不能只删生成文件属性导致断链，也不能新增旁路注册文件。

4. 编译源码。项目静态编译使用：

```text
dotnet build ThirdPersonClient.Editor.csproj --disable-build-servers /nr:false /p:UseSharedCompilation=false
dotnet build-server shutdown
```

编译失败时停止生成，不使用上一次同名入口。生成 C# 后若 Unity 正在编译或导入，等待 Editor 回到可用状态。

5. 明确生成时调用：

```text
btsmtl.generate_assets(
  source_code_path,
  recipe_type,
  entry_type_name,
  definition_asset_path,
  output_asset_path
)
```

工具只接受精确源码路径和已编译入口，不接受任意源码正文、方法名、反射字段或节点操作参数。检查返回的 `saved`、创建/替换/删除路径和 `diagnostics`；保存失败不能报告成功。

6. 生成完成后，按用户明确要求单独执行 Character Float32、Fixed、Projection 或其它产品 Build。作者 MCP 不自动触发运行产物 Build。

## 领域所有权

- Skill/FSM：使用正式 Skill Graph、Macro、Blackboard、Native FSM、State Body、Timeline 和连接 API。
- Timeline：使用 `TimelineData`、正式 Track/Clip/Section 和 typed binding；不恢复 JSON clip binding。
- Pose：使用正式 Pose Graph Definition、Capability、Mutation、Source/Resource Slot、StateMachine、Rule 和 layout API。
- EventGraph：使用正式 EventGraph adapter、Host 和 mutation；不恢复 `EventGraphAuthoringDocument`。
- 角色产品：由 Character Semantic Frontend、Target Compiler 和正式 Build 入口负责。

## 结果判断

成功必须同时满足：输入资产未被导出操作修改；生成入口已通过 recipe、类型和路径核对；正式 API 完成创建/替换/绑定/保存；响应没有失败 diagnostic。只看到源码文件、静态编译成功或文件移动，不等于资产生成或运行产品完成。

人工 UI 编辑不会自动导出 C#。人工修改后若需要新源码，重新明确调用 `export_code`；旧源码不会自动合并人工修改。生成范围必须可重复替换，范围外的 AnimationClip、Rig、分析产物、共享 Graph 和其它外部资源不得被隐式复制或删除。
