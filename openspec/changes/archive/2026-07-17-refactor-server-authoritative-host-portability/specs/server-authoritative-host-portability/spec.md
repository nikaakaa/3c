# server-authoritative-host-portability Specification

## ADDED Requirements

### Requirement: Authority Pipeline Catalog必须Host-Neutral

Authority Pipeline Pass顺序、config lowering、descriptor构造、Pass factory与product factory MUST位于portable ServerAuthoritative source set。Unity Definition MUST只降低authoring输入，MUST不拥有第二份descriptor或factory catalog。

#### Scenario: Unity Worker编译Authority Pipeline

- **WHEN** Unity Definition提交合法authoring字段
- **THEN** MUST由portable catalog产生descriptor与factory集合
- **AND** 迁移前后PipelineHash MUST相同

### Requirement: Authority Source Runtime必须Host-Neutral

每Actor command queue、authority clock、missing-input policy、每Client checkpoint baseline、snapshot sequence、reliable/full-checkpoint queue与typed Source ports MUST由portable Authority Source runtime唯一拥有。Unity与未来普通.NET Host MUST只提供transport adapter和显式launch输入。

#### Scenario: Source消费Command

- **WHEN** transport将已校验command写入Actor queue
- **THEN** portable Source MUST在outer tick边界消费并生成typed ingress
- **AND** transport MUST不执行Program或missing-input决策

### Requirement: Authority Control Transport必须只承载控制与可靠产品

Host-neutral control transport MUST只交换register、roster、ticket、heartbeat、reliable event、full checkpoint、leave和failure产品。Routine command/snapshot MUST继续使用唯一portable datagram endpoint，MUST不进入control transport或回退KCP gameplay stream。

#### Scenario: 发布Routine Snapshot

- **WHEN** Authority Egress生成routine snapshot
- **THEN** Source MUST通过portable datagram endpoint发送
- **AND** control transport MUST不接收该snapshot

### Requirement: Authority Host必须通过唯一Launch Request调用Portable Composer

Authority Host launch request MUST显式提供Program Runtime、Backend、Authority Pipeline、Source policy/ports、roster、WorldSolver、initial state、Committer、diagnostics和output routes，并调用唯一portable Float32 Composer。缺失或不兼容输入 MUST失败，不得选择默认组件或复制Composer。

#### Scenario: 普通.NET Host准备接入

- **WHEN** 后续Host提供完整portable launch输入
- **THEN** MUST可以在不引用Unity Definition的情况下调用同一launch request
- **AND** 当前change MUST不以空Worker或fallback证明该能力
