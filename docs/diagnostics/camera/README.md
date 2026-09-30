# 相机诊断

业务现状先读[相机进展](../../status/camera.md)。本目录保存各时间点的实施与来源证据，不能把资源存在、静态字段一致或编译成功当作完整镜头验收。

| 实施与审计 | 内容 |
| --- | --- |
| [实施记录](corin-camera-implementation-20260929.md) | 已接入批次、后续修正和剩余范围 |
| [完整链路审计](corin-camera-completion-audit-20260929.md) | 常驻资源、引用与实际求值消费者 |
| [时钟与升降资格](corin-camera-clock-and-vertical-20260929.md) | 原生震动时钟、资格和取消来源 |

| 原始证据 | 内容 |
| --- | --- |
| [输入、轨道、Delay 与 Stretch 证据包](camera-basis-runtime-20260929/) | 305 个文件；运行对象、函数、字段与作者核对 |
| [基础证据 manifest](camera-basis-runtime-20260929/manifest.json) | 包内相对路径与记录时的哈希 |
| [相机输入快照](camera-basis-runtime-20260929/pointer-input-snapshot.json) | `CorinCameraResourcesAuthoring.PublishInput` 正式读取的玩家输入配置来源 |
| [震动证据包](camera-shake-runtime-20260928/) | 183 个文件；信号、空间衰减、优先级、宿主与时钟 |
| [绑定初查](corin-camera-binding-20260927.json) | 9 月 27 日资源绑定现场 |
| [源字段初查](corin-camera-source-audit-20260927.json) | 9 月 27 日配置数值对照 |

震动链的可复用解释见[原生消费者研究](../../reference/camera/corin-camera-shake-source-parity-20260928.md)，早期交付与调查见[相机阶段记录](../../archive/records/camera/)。原始 JSON 中的旧绝对路径仅记录当时来源；当前证据位置以本目录为准。
