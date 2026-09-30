# 特效诊断

原生资源解析、ParticleSystem/Renderer Transfer 与依赖调查从[Transfer 报告](effect-particle-native-transfer-20260930.md)读取。该报告记录静态解析证据与剩余边界，不能据此宣称 VFX 已具备完整接入条件。

| 原始证据 | 内容 |
| --- | --- |
| [Transfer 证据包](effect-particle-native-transfer-20260930/) | 65 个文件；函数、字节、扫描、模块探测与失败记录 |
| [正式 Renderer 扫描](effect-particle-native-transfer-20260930/corin-renderer-formal-scan.json) | 正式 CAB 范围与 Renderer 解析结果 |
| [外部依赖追踪](effect-particle-native-transfer-20260930/corin-renderer-external-dependency-trace.json) | Mesh 等外部依赖；`scan` 保留生成当时的原路径，对应上面的正式扫描 |
| [AssetRipper 引用扫描](effect-particle-native-transfer-20260930/corin-ripper-prefab-reference-scan.json) | 导出副本、缺失资产与占位 GUID 范围 |
| [批量解析失败](effect-particle-native-transfer-20260930/system-batch-failure.json) | 失败现场；保留失败数据用于复核 |

源数据归属见[抄录来源](../../replication/corin-copy-sources.md)，正式渲染工具与合同从 [Rendering](../../../Tools/Rendering/README.md)读取。证据包内部内容和文件名未改，没有复制出第二套数据。
