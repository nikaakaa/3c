# 2026-08-31 SmoothKnee单变量候选

## 固定对照与当前状态

保留版为fa656b2对应的20260831-160901-709-3e0df68f9d3640aaa82f4fbd2ec7c42f，后续8ae0bcb／e2bd016只补充记录。原samples、diagnoses与该目录replay-proof.json均原地保留。输入仍为43357ff3cd384e5cba75d2c31175b116。

本候选代码与诊断已接入，但尚未编译、加载、重建产品或生成候选Replay；不能作为新的保留基线或修复通过。准备加载时Unity仍为非Replay的Play；随后MCP的127.0.0.1:8080连接失败，资源查询与Editor日志共同确认WebSocket无法连接。没有停止用户Play、重启Unity、启动另一入口或覆盖旧采样。生成的Runtime.csproj尚未包含新增源文件，需正式Editor同步后再执行规定参数编译。

## 实际移植与适配

- 独立读取磁盘PE的0x165C51A0–0x165C5BE8，确认角度用K−H与A−K两段同向向量，直腿为0；有限步长之后保存角差，补偿−0.5／+1，右乘局部旋转并仅恢复脚旋转。
- 0x171DB44F常量0.25、0x171DB4AC常量3已从磁盘读取；829离线页确认Forward=(0,0,1)、Down=(0,−1,0)。按同一PoseRoot位置差和朝向生成下楼权重，首次位置基准及精确0／0采用design中明确的项目边界。
- Corin Rig引用姿态重算的大腿／小腿局部弯曲轴均近似＋Z，最大非Z分量约1.48e−6；代码使用该正式引用几何作静态坐标适配，不沿当前膝向或历史翻号。
- 本轮明确选择Forced路径，正式Profile v2配置7／4rad/s，revision为434a81d2e6d6adfe8cb11ca63ceb2e1cfd2ea04891af7574dd4e0ac46112da14。它不是已观测Force=false的可琳完整启停复刻；普通kneeState输入尚未迁移，不用Contact或Lock代替。
- 保留Foot目标、Pelvis、Bend方向／权重、Vendor及Reach撤除策略。补偿写同一Pending Component Pose，角差与移动历史归属根Bank；不增加Solver或Writer。无诊断interest时不构造逐腿输出测量。

## 静态核对

facts70／Analyzer70／diagnosis39，CSV1257列；42个新标量在Header、Writer与Parser一一对应，无重复列。原评分实现逐字符一致；FootPlacement目录无diff。FBBIK原Solved字段保留其阶段，补偿后的Knee／Ankle另列；最终Heel／Toe仍来自原Physical Writer。

OpenSpec change strict及列对账已通过。尚无C#编译或动态覆盖结论。后续必须核对额外膝角、真实响应后膝侧／位移及全套Foot／Pelvis质量；尤其保留原15个强反侧窗口与R825–827深折叠窗口，不能因限速公式正确就宣布反弯消失，也不能用脚位置重新偏离掩盖它。
