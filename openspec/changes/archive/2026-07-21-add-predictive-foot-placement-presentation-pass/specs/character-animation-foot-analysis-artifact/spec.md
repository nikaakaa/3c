# character-animation-foot-analysis-artifact Specification

## MODIFIED Requirements

### Requirement: Analyzer必须使用统一校准地面与heel/toe接触语义

Analyzer MUST从Sampling Rig与Runtime共享的Rig Calibration绑定姿势得到唯一脚底地面参考高度，并分别采样左右脚heel与toe。每脚高度 MUST取heel/toe最低接触点相对该统一地面的高度；sole轨迹 MAY使用heel/toe中点。Plant进入/退出速度 MUST只使用sole垂直速度，不得把InPlace动画的局部水平轨迹计入Plant速度，也不得把每个AnimationClip自身最低点重新定义为地面。Algorithm identity与artifact format MUST覆盖这些语义；旧算法artifact MUST判为Stale或未知版本并拒绝，不得兼容读取。

#### Scenario: 抬脚动画自身最低点仍高于地面

- **WHEN** 一个AnimationClip全程让该脚高于Calibration地面参考
- **THEN** Analyzer MUST保留真实离地高度并保持plant confidence为非接触
- **AND** MUST不把该clip最低采样点归零为地面

#### Scenario: InPlace Run包含局部水平脚步

- **WHEN** sole在VisualRoot局部空间高速前后摆动但垂直速度与高度满足Plant条件
- **THEN** Plant classifier输入 MUST只使用垂直速度与校准高度
- **AND** 水平速度 MUST继续保存在生成轨迹中供Runtime世界接触速度合成
