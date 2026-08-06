# character-animation-foot-analysis-artifact Specification Delta

## MODIFIED Requirements

### Requirement: Analyzer必须使用统一校准地面与heel/toe接触语义

Analyzer MUST在创建采样Playable前，以Analysis Source精确引用的Sampling Rig执行共享Rig Calibration几何validator。Calibration MUST使用v2 heel/toe contact、单一Sole Frame Rotation和Preferred Bend Direction合同；validator MUST证明左右heel/toe形成有限鞋底基线、接近统一参考地面、sole frame前/上轴和手性合法、参考平地ankle correction有界，并证明hip-knee-ankle与preferred bend direction不退化。验证失败 MUST阻止artifact生成并定位脚侧、指标、实测值和允许边界，MUST不使用最低点、默认axis或旧schema继续分析。

Analyzer MUST从通过验证的Sampling Rig绑定姿势heel/toe接触几何建立唯一脚底地面参考，并分别采样左右脚heel与toe。它 MUST不再通过所有接触点的全局最低值掩盖单脚或单接触点的参考高度错误。每脚高度 MUST取heel/toe最低接触点相对该统一地面的高度；sole轨迹 MAY使用heel/toe中点。Plant进入/退出速度 MUST只使用sole垂直速度，不得把InPlace动画的局部水平轨迹计入Plant速度，也不得把每个AnimationClip自身最低点重新定义为地面。Algorithm identity与artifact format MUST覆盖Calibration v2和这些语义；旧算法artifact MUST判为Stale或未知版本并拒绝，不得兼容读取。

#### Scenario: 抬脚动画自身最低点仍高于地面

- **WHEN** 一个AnimationClip全程让该脚高于Calibration地面参考
- **THEN** Analyzer MUST保留真实离地高度并保持plant confidence为非接触
- **AND** MUST不把该clip最低采样点归零为地面

#### Scenario: InPlace Run包含局部水平脚步

- **WHEN** sole在VisualRoot局部空间高速前后摆动但垂直速度与高度满足Plant条件
- **THEN** Plant classifier输入 MUST只使用垂直速度与校准高度
- **AND** 水平速度 MUST继续保存在生成轨迹中供Runtime世界接触速度合成

#### Scenario: 右脚heel与toe参考高度不一致

- **WHEN** Sampling Rig绑定姿势中的右脚heel/toe ground error超过正式边界
- **THEN** Analyzer MUST在采样前拒绝该Calibration
- **AND** MUST不取较低点作为统一地面后继续生成内部一致的错误artifact

#### Scenario: Calibration升级到v2

- **WHEN** 已存在artifact仍声明旧Calibration schema或旧algorithm version
- **THEN** Artifact Store MUST将其报告为Stale或Unknown
- **AND** Definition Build MUST要求显式重建而不使用旧reader
