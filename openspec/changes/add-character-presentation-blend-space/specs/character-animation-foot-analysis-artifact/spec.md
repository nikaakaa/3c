## ADDED Requirements

### Requirement: Foot Analysis必须支持Blend Space Sample source identity

Foot Analysis Artifact Builder与Store MUST把`BlendSpaceAssetId + SampleId + Clip identity + Rig identity + Calibration identity`作为BlendSpace sample的正式source binding。Projection Compiler MUST逐个解析需要feature的sample并拒绝Missing、Stale、Corrupt或identity不匹配artifact。系统 MUST不从同名Timeline Clip、目录或运行时AnimationClip重新分析结果。

#### Scenario: Blend Space样本引用过期artifact

- **WHEN** Sample clip content revision变化但artifact仍对应旧revision
- **THEN** Projection Build MUST报告Stale并定位AssetId与SampleId
- **AND** MUST不读取Timeline上的其它artifact代替

### Requirement: Blend Space必须只聚合generated feature而不复制分析算法

BlendSpacePlayer MUST按compiled artifact binding和每个sample的effective time读取既有generated Foot Analysis feature，并用最终姿势sample weight聚合。它 MUST不复制Artifact Builder算法、不生成runtime curve、不把generated Sole Speed、Height、Plant或Landing变成editable Blend Space channel。

#### Scenario: Agent或Editor查看Blend Space sample

- **WHEN** 工具展示一个已绑定Foot Analysis的Sample
- **THEN** 工具 MAY显示artifact identity、revision和availability摘要
- **AND** MUST不把generated feature payload作为可编辑Sample数据

