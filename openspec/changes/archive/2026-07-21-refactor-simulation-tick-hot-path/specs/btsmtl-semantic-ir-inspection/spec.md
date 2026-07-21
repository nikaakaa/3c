## ADDED Requirements

### Requirement: Semantic IR与Target Program检查工具必须展示结构化Value输入

Unity Semantic IR Inspector与普通.NET portable Reader MUST直接读取artifact中的Value edge和constant input binding，并提供按target operation查看的Value Inputs section。输出 MUST显示target operation、target port、resolved value kind以及source operation/output port或constant index。工具 MUST不解析constant identity、反射authoring node或调用Runtime layout来重建缺失关系。

#### Scenario: 在Inspector检查Compare输入

- **WHEN** 作者在Semantic IR Inspector选择Value Inputs并定位一个Compare operation
- **THEN** Inspector MUST分别显示Left与Right的结构化source和resolved kind
- **AND** SourceMap导航 MUST仍定位原Graph port或constant source

#### Scenario: 普通DotNet读取Semantic artifact

- **WHEN** portable Reader使用`semantic-ir --section value-inputs`读取正式`.csir`
- **THEN** text与JSON输出 MUST包含Semantic constant input binding count和内容
- **AND** 该命令 MUST不需要UnityEngine、UnityEditor或Character authoring asset

#### Scenario: 普通DotNet读取Target Program

- **WHEN** portable Reader使用`program --section value-inputs`读取正式Float32 Program
- **THEN** 输出 MUST包含Target constant input binding及Program identity
- **AND** 旧artifact版本 MUST明确失败，MUST不显示通过字符串推导的伪binding
