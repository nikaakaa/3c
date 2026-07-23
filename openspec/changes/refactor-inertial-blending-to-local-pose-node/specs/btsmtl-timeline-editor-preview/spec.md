## MODIFIED Requirements

### Requirement: 预览采样必须复用正式动画Selection与Pose Plan

Timeline Preview MUST执行图上正式Inertialization节点的history、capture、rebase、HardCut与Reset语义。连续播放 MAY产生Discontinuity并惯性化；非连续scrub/seek MUST重置节点。图中没有Inertialization时Preview MUST显示真实硬切或CrossFade，不得自动全局平滑。

#### Scenario: Preview非连续拖动时间

- **WHEN** 作者把预览时间从一个不连续位置跳到另一个位置
- **THEN** Preview MUST重置Inertialization history
- **AND** MUST不把seek解释为可惯性化的连续切换
