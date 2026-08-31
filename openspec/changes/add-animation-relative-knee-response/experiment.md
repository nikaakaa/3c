# 2026-08-31 SmoothKnee单变量候选

## 固定对照与当前状态

保留版为fa656b2对应的20260831-160901-709-3e0df68f9d3640aaa82f4fbd2ec7c42f，后续8ae0bcb／e2bd016只补充记录。原samples、diagnoses与该目录replay-proof.json均原地保留。输入仍为43357ff3cd384e5cba75d2c31175b116。

候选代码为f2c3ec0。首次加载因用户Play和MCP连接中断停止；用户恢复连接后，已完成Editor Refresh、规定参数的.NET编译（57既有警告、0错误并立即shutdown）及Corin Float32／Fixed正式产品重建。两产品SourceRevision、SemanticHash、ContractHash和ProjectionRevision一致。

候选Replay为20260831-183002-949-5b2fb1f3f4c647d18c7a88fc180e34f7，1043采样帧／2086脚行，facts70／diagnosis39。原始CSV、diagnoses与必要的replay-proof.json原地保留。官方Proof因7个产品身份字段变化为matched=false，但1044条逐帧记录完全一致，输入内容、起始Body、输入序列与Body轨迹hash均相同。没有修改比较器或伪造matched。

用户已明确拒绝本次踏空回归。候选已退出Play，本轮只完成数据归因与记录，没有追加算法修改；源码尚未撤回160901，不能把当前候选或新产品称为保留版。

## 实际移植与适配

- 独立读取磁盘PE的0x165C51A0–0x165C5BE8，确认角度用K−H与A−K两段同向向量，直腿为0；有限步长之后保存角差，补偿−0.5／+1，右乘局部旋转并仅恢复脚旋转。
- 0x171DB44F常量0.25、0x171DB4AC常量3已从磁盘读取；829离线页确认Forward=(0,0,1)、Down=(0,−1,0)。按同一PoseRoot位置差和朝向生成下楼权重，首次位置基准及精确0／0采用design中明确的项目边界。
- Corin Rig引用姿态重算的大腿／小腿局部弯曲轴均近似＋Z，最大非Z分量约1.48e−6；代码使用该正式引用几何作静态坐标适配，不沿当前膝向或历史翻号。
- 本轮明确选择Forced路径，正式Profile v2配置7／4rad/s，revision为434a81d2e6d6adfe8cb11ca63ceb2e1cfd2ea04891af7574dd4e0ac46112da14。它不是已观测Force=false的可琳完整启停复刻；普通kneeState输入尚未迁移，不用Contact或Lock代替。
- 保留Foot目标、Pelvis、Bend方向／权重、Vendor及Reach撤除策略。补偿写同一Pending Component Pose，角差与移动历史归属根Bank；不增加Solver或Writer。无诊断interest时不构造逐腿输出测量。

## 静态核对

facts70／Analyzer70／diagnosis39，CSV1257列；42个新标量在Header、Writer与Parser一一对应，无重复列。原评分实现逐字符一致；FootPlacement目录无diff。FBBIK原Solved字段保留其阶段，补偿后的Knee／Ankle另列；最终Heel／Toe仍来自原Physical Writer。

OpenSpec全量98／98、列对账与编译通过。2086行均实际执行角差响应，1026行限速；初始化未有采样覆盖。37个Target的规则和scorePolicy与160901完全一致。

## 回归归因与裁决

- 两包2086行正式Foot输入、全部FinalIkLeg字段、Goal及实际Pelvis字段逐值一致。Current Support／Resolved／Anchor几何及原Foot响应没有行为差异；身份差来自本次实例和产品换代。
- 新旧实际Ankle世界差与本轮KneeAngleResponseFootDisplacement的重建误差最大4.90微米，Sole中点对应误差最大4.37微米。因此新增脚位变化来自FBBIK之后的膝角补偿，不是Foot目标、骨盆或Solver先改变。
- R229：Contact=1、Goal位置权重=1、AnchorY=1.08000016。原脚底中心约1.080000，候选1.260306；整脚对接触平面的间隙0→179.465毫米。上一额外角50.994度，本帧目标−4.721度，7rad/s只退6.685度，仍保留44.309度，形成49.031度补偿；腿被重新弯回，脚底上移180.306毫米。
- L247是明确LockedFullAnchor反例：原脚底中心在1.620000平面，补偿37.605度后到1.743222，整脚间隙123.202毫米。Anchor及原Solver解均未变化。
- 反方向同样成立：L1043原中心已到1.620000，本次−47.594度补偿使中心降到1.491980，接触平面穿透从0.364增到128.384毫米。以上是已验证Contact Plane距离，不冒充最终Heel／Toe实体Collider查询。
- 接触间隙命中13／60→18／60；接触平面穿透19／84→34／84；锁脚水平漂移0／15→11／15；接触输出大步418／1052→515／1052。Stable命中143／344→133／344，但Path199／667→222／667，不能用局部减少抵消接触回归。
- 原R934反侧未消失：Solver膝步414.751毫米未变，响应后为419.020毫米。同一可靠膝侧判据下强反侧15→27，但可靠弯曲覆盖也变化，不能简单把差值当作12个同严重度新增事件。另有16行animationAngle+currentExtra<0，局部旋转跨过伸直姿态；其实际无符号角与负目标绝对值吻合，说明角差历史也没有自动保证膝侧合法。

裁决：否决当前Forced后处理组合。它限制的是额外角差，不是脚位置；在髋和已求解Foot端点之外旋转腿，会重新移动脚。仅保存脚旋转不能保持Anchor。此次没有迁移ZZZ普通kneeState时序，也没有证明关闭Force就能修复；不能把本次回归归咎为ZZZ原算法无效，更不能调大速度、追加贴地或重解IK掩盖。后续应先精确撤销本候选并恢复160901，再决定独立的膝侧修复或完整启停／位置协调方案。
