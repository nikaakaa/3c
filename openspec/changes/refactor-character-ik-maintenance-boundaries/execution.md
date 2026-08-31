# 串行执行记录

## 固定对照与接入

- 总源码及行为基线：`ad3527e103cc3235a63e8a1c1dbd26df5155e0ba`，不得随HEAD更新。
- 主证据：`Diagnostics/FootPlacementRuns/20260831-233436-894-d1564c7fa0b442f6aef02bb470ca0b1b`；独立交叉证据：`20260831-205014-114-dc157fde9c004846a72e9cd1fa1b5b01`。
- 唯一正式Record：`43357ff3cd384e5cba75d2c31175b116`，1044 Tick、60Hz，使用原`logic-locked`与`one-fixed-tick-per-presentation-frame`驱动。
- 已确认原始samples、geometry、analysis与持久replay-proof均存在。两包各1043表现帧、2086脚行、1215列、67186几何行；旧逐列结果为1191业务列一致、24身份列双向映射。候选仍需实际回放，不复用旧结论冒充新验证。
- 第二阶段`refactor-character-pose-graph-architecture`的串行接入文档已提交`8cb2eef`；仅第一阶段全部通过后启动。Reset修正单独提交、单独验证并作为第二阶段保留成果。
- `ad5f6f9`为共享工作区独立GM任务的正确改动，不回退。IK核心在本次开始时与固定基线相同。

## 第一个闭环：请求生产与最终发布

状态：Runtime与Editor均已编译通过，正式回放待完成，不宣称行为通过。Editor第一次使用no-restore时因GM新增工程缺少project.assets.json失败；按正式构建完成依赖还原后成功。最终构建只有既有InputValueNodes未使用字段警告，0错误；每次均按规则关闭build server。

| 原读取或决定 | 当前唯一位置 | 保持的业务语义 |
|---|---|---|
| Module混读Step、Motion.State和Resolved决定Reach准入 | `CharacterFootLifecycle.BuildRequest/AdmitLandingReach` | 原Grounded、事件身份、作者权重阈值、Contact或预测Landing条件；不改变逐腿可达观察 |
| 初步和最终均叫Resolved | 请求`CharacterFootPlacementRequest/Pair`；完成后`Publish`发布`CharacterResolvedFootResult/Pair` | Pelvis读请求；Goal只读完成结果，不增加插值或Pelvis响应次数 |
| 平铺重复的脚几何、支撑权重 | 只读`CharacterFootPlacementPose`与`CharacterFootSupportFacts` | 两个阶段复用同一组值；SupportWeight/SupportIntentWeight内部只有一个Weight，外部旧列继续投影 |
| 临时Foot Goal编码后反解Pelvis脚掌位置 | Foot生产`CharacterFootGoalTarget`；Pelvis直接读其EffectiveSole | 原world→component→world、归一化、Lerp/Slerp及Heel/Toe合成顺序不变 |
| Goal Encoder重新决定有效性与权重 | Foot生产GoalTarget，Encoder仅组装正式Goal | 原Ready、权重阈值及失效时动画姿态保留 |

`Pose.EffectiveAnkle/EffectiveSole`表达原脚目标求解所用的加权规划几何；`GoalTarget.EffectiveSole`表达按正式component目标和权重还原出的脚掌位置。基线中Pelvis姿态偏好使用前者的Ankle，共同高度使用后者的Sole，不能因同名Vector3而互换。本次明确这两个阶段的含义并保留原消费链；没有创建可任选的兼容读取。

本闭环不改变Profile、GroundPath、Contact、Interpolation、Pelvis公式、FBBIK、Bend历史、动画时序或CSV列格式。后续Stride最小输入、完成凭据封装、方向历史、Reset及Editor列绑定仍未完成。

## 验证约束

每个闭环由“采样数据自动测试”任务读取候选提交、上一通过提交和固定总基线，使用同一正式Record。先核对输入、Body、时序与版本身份，再对比实际Foot、Pelvis、Knee、Goal、动画状态、Solved和可用Physical输出；规则及总分只作辅助。不提高容差、改评分、删差异列或重造数据。原包未覆盖的输入、最终Physical Knee和Reset边界不能称为通过。

共享Unity、构建和回放一次只交给一个任务。测试期间停止相关代码写入；另一个产品任务使用Unity时等待明确释放，不抢占或并发Refresh。只提交本任务文件，原始证据不随代码提交。
