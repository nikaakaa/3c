# Foot IK关键经验

本文只保留会改变正式架构、否决方案或决定下一步的结论。历史编号永久保留，但不记录逐轮流水账。

## 不变量

- 唯一链：`FootGrounding -> optional PredictiveFootPlacementModifier -> FinalIK FBBIK`。
- Ground Envelope只提供feet-only安全下界；最终Swing保留动画XZ与动画净空。
- Plan、Revision、Landing、Anchor、Pelvis都必须有唯一owner；Rejected不得由响应式Swing伪装成功。
- FBBIK只执行最终Goal。Goal先跳而solver residual很小时，首因不在FBBIK。
- 固定验收顺序：Artifact重建 -> Projection事件/时钟 -> Future Body -> Ground Path -> Revision -> Landing/Anchor -> Pelvis -> FBBIK。

## 历史经验索引

1. 当前不是完整GDC数据层：Artifact、Constraint、Support Leg、Orientation、Pivot和平地重建门禁曾长期缺失，Runtime调参不能补齐这些事实。
2. GDC核心不是提前射线：`Final Sole Height = Ground Path Height + Animation Height Above Foot Path`，Convex Hull不是最终脚轨迹。
3. 四条路线必须分开：Animation Foot Route、Future Body Transform、Virtual Ground Query Route、feet-only Ground Envelope不能互相冒充。
4. In-place动画不提供世界位移；输入幅值、Action Motion、Visible速度、Body Yaw和Render差分都不能重建KCC世界运动。
5. 楼梯前先证明Artifact可按同一相位还原Heel/Toe/Sole/Ankle/Knee/Hip位置、旋转、弧长、侧向范围和事件边界。
6. 冻结Plan与输入变化不冲突：committed trajectory实质变化后创建新的不可变Revision，而不是逐帧自适应或永远锁死旧Plan。
7. 曲线内部连续不等于跨owner连续；Active/Revision、Event、Predictive/Grounding、Swing/Anchor、Reach和Pelvis换边都可跳。
8. GDC Ground Path顺序固定为采集位置/法线 -> 排序 -> Edge Plane -> Reachability -> 删除不可达 -> 上侧Hull；顺序颠倒会先承诺错误路线。
9. Foot Lock是数据意图加世界验证；Landing必须原子提交Pose、Surface、Anchor local、Committed Sole和Successor Start。
10. Pelvis必须消费独立Body Support Path与Support Leg事实，不能平均左右Foot Envelope或把spring current当目标。
11. 上坡、下坡、跑步Foot Orientation与Support Foot Pivot是正式求解事实，不是诊断装饰。
12. 已否决：逐帧重规划、旧世界Plan直接晋升、Route接管Foot XYZ、双Y owner、全局抛物线、平滑错误Path、响应式fallback、FBBIK后处理和第二套owner。
13. 编译、Character Build、Console 0 Error和CSV等宽都不是运动效果通过。
14. 自动输入必须真实穿过故障地形；普通Play与自动Variant分开，自动只接管MoveAxis并保留LookAxis。
15. 最小完整A/D事务足以定位owner；扩大运行时间只增加重复数据。
16. Body Yaw与轨迹曲率不是同一事实；Revision C1不能从Render差分、Final Goal历史或额外Hermite猜测。
17. Foot Route起点、Ground Probe起点和Clearance起点是三个事实；混用曾造成约80cm XZ错位与长斜线。
18. Reachability必须在完整地面采集后构造有向链；稀疏端点预拒绝和单点双端直连都会误删正常楼梯。
19. 冻结Plan必须同时冻结Step时长、Future Body范围和phase映射；运行时改时长曾触发轨迹越界。
20. 下一事件必须在Incoming PreSwing预建；等它成为Current再建会错过LiftOff，形成无Plan空窗和迟到托脚。
21. “全收命中”会踏面切换抖动，“全拒命中”会Plan消失踏空；Physics命中距离不等于路线支撑所有权。
22. 单次Cast贪心选最高点只会把错误从一只脚转到另一只脚；必须先保留候选组再求完整有向链。
23. 自动CSV只允许`LiveState -> completed frame -> 流式压缩`；叠加无界Continuous Capture曾造成约10MB/帧分配和严重低帧率。
24. Current与Incoming必须由Analyzer在同一Artifact采样点原子发布完整Step事实，Runtime不得搜索或补建。
25. Marker occurrence由`Source Landing Cycle + Event Ordinal + Foot Side`拥有；不能从连续时间距离反推身份。
26. Projection必须整对选择`Current + Incoming`，不能逐字段择优或从不同source拼装一个不存在的Step。
27. 权威Step不能再乘Pose contribution weight；否则事件身份和Foot Placement所有权会换代两次。
28. Revision起点必须是上一完成帧实际送入FBBIK的Final Sole及其支撑，不是旧Plan理论Target；创建帧Blend保持0。
29. Future Body曲率只来自相邻Simulation committed Intent和Tick时长；Presentation不拥有导数。
30. 原子Step不等于10KB巨型值类型复制；完整事实进入预分配Workspace页，Action Frame只携带lease快照。
31. Incoming事件边界需要离散Source Landing Cycle Offset；连续Clock插值曾让Incoming提前跨到N+2并造成1.6m断点。
32. 弹簧不能修复同一步多次换路；Revision资格属于不可变源Plan，每个源Plan至多尝试一次，新Active可再次离散修订。
33. 接触净空不能逐帧累加进Current spring；删除重复`offset += constraint`后Current Offset从米级降到约0.4m内。
34. Plan在ReleasePhase开始所有权淡入，Ground Path到PathStartPhase才开始推进；用几何起点门控状态会制造LiftOff硬切。
35. 运动偏差必须是提交边界：过期Plan不得输出Landing、Anchor或Successor；run `2755132...`曾在误差0.99m至2.61m时继续执行并产生`+65/-116/+100cm`跳变。
36. Idle Capture不能依赖权重先低于1：run `32d8bff...`静止段两脚`Contact=true`但`HasAnchor=false`；改为进入`GroundedStationary`显式武装后，run `d6ee145...`两脚frame76捕获、frame82权重到1并持续Anchored到frame97，Final XZ不再跟随Idle动画漂移。
37. Revision权重连续不等于旧侧目标连续：run `d6ee145...`中旧Active已偏离0.51m至4.82m仍在Blend期间继续求值。Revision旧侧必须冻结上一完成输出；本Plan从未输出时则没有预测历史可保留，必须先退出再从当前事实重建。

## 当前证据与下一owner

- `32d8bff...`已消除旧run的1m级NonFinite目标和13cm物理穿透，证明过期Plan禁止提交Landing/Successor有效。
- 仍有真实预测修正跳变：右脚frame159在Plan首个Executing帧已`MotionError=14.8cm > 8cm`，Path Y跳`89.85cm`、Required Lift为`40.57cm`；过期Plan仍输出了一帧。
- 右脚frame224在Revision Blend从`0.456`升到`0.760`时Path Y跳`21.96cm`、预测修正跳`19.36cm`；两个绝对目标虽用连续权重混合，但空间与切线未连续。
- frame172出现`AnchorCaptured`，frame173立即`AnchorDistanceExceeded`；Landing后的Locked所有权尚未与事件边界形成同一事务。
- Pelvis 244行内换支撑44次，是次级抖动owner；先闭合脚部Plan与Anchor事务，再处理Pelvis。
- Idle锁脚入口已由run `d6ee145...`闭环，不再把后续运动跳变归因于Idle权重曲线。
