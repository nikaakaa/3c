## Why

现有导出把完整重建理解成广泛复制身份与字段，Attack 创建源码当前达到 1,166,932 字节，并带有 GUID 变量后缀和本机绝对源码路径。作者需要的是可读、可迁移、足以重建正式资产的 C#，不是原资产序列化信息的另一份转录。

## What Changes

- **BREAKING** 将“所有元素稳定 identity 均写入”改为按正式引用、稳定拓扑或 owner 关系保留必需 identity；未被这些关系使用的身份由正式创建 API 分配。
- 输出最小重建闭包：对象类型、必需身份、拓扑、有效配置、业务顺序、根绑定、外部依赖；去掉不参与重建的历史、诊断和过程字段。
- 局部变量使用可读短名与调用内序号，不附带 GUID；共享外部引用只声明一次。
- **BREAKING** 从生成类删除 `SourceCodePath` 与重复 `EntryTypeName` 属性；recipe、类型与源码定位在调度层处理，不作为资产重建数据输出。正式入口保留创建执行合同。
- 同步作者 skill，明确这是待实施的目标合同；本轮不改导出器、正式 API 或生成 C#。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `character-csharp-authoring`：明确最小重建闭包、按用途保留身份、调度元数据与重建数据分离。

## Impact

影响公共生成入口/调度、Skill/Pose/EventGraph 等领域导出适配、正式生成 C# 和作者 skill。保持两个显式 MCP、人工编辑不导出、正式 API 拥有业务规则、完整失败不覆盖旧源码等现有行为。

与现行 spec 的差异：`输出必须覆盖正式配置和业务顺序` 与 `生成范围必须可删除重建且保持逻辑身份` 当前广泛要求稳定身份，需由本次 delta 收窄。现行 skill 强制入口携带精确 `SourceCodePath`，需迁出。完整配置、曲线、layout 和顺序依旧保留，不把裁剪当作静默漏字段。原变更已经归档，本次新建后续变更，不改写 archive 历史。
