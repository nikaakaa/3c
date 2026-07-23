## 1. 建立并行实施门禁

- [x] 1.1 在独立分支或工作树中实施本 change。
- [x] 1.2 记录开始实施时的基线 commit，供后续接入 change 重放或变基。
- [x] 1.3 确认当前闭环分支不接收本 change 的中间提交。
- [x] 1.4 确认本 change 不修改现用 `DeterministicKccMotor` 调用点。
- [x] 1.5 确认本 change 不修改 `DeterministicKccWorldSolver`、Solver descriptor、Composition 或 Session。
- [x] 1.6 确认本 change 不修改 Unity Solver Definition 和正式 KCC asset。
- [x] 1.7 确认本 change 不升级 Motor/Solver version、KCC identity 或 snapshot 口径。
- [x] 1.8 确认本 change 不单独合并、不单独 archive。

## 2. 锁定只读依赖合同

- [x] 2.1 记录 continuous cast 向 Step Solver 提供 canonical contact set 的字段。
- [x] 2.2 记录 `DeterministicKccContact` 的 primitive、feature、TOI、normal 和 witness 字段。
- [x] 2.3 记录 `DeterministicKccGroundReport` 的 stable landing、support identity、normal、distance 和 ledge 字段。
- [x] 2.4 记录 capsule cast、ground query、overlap validation 和 triangle adjacency 的只读接口。
- [x] 2.5 记录现有 query buffer 的容量、排序和生命周期。
- [x] 2.6 记录 `MaximumStepHeight`、`GroundSnapDistance`、`MinimumMovementDistance` 的现有数值来源。
- [x] 2.7 将 `MinimumStepDepth` 作为模块输入语义定义，不修改现有序列化字段。
- [x] 2.8 确认模块不读取 Graph、Action、Input、Network、Presentation 或 Unity 对象。

## 3. 定义 Step Solver 数据模型

- [x] 3.1 定义 Step Up 请求，包含 safe position、remaining、previous support 和 canonical contacts。
- [x] 3.2 定义 Step Down 请求，包含 movement position、requested displacement、previous support 和 Snap 结果。
- [x] 3.3 定义只读 Step policy，包含最大台阶高度、最小踏面深度、Snap 距离和最小移动距离。
- [x] 3.4 定义 blocker identity，包含 primitive、feature、TOI、normal 和 witness。
- [x] 3.5 定义 outer、inner 和 final landing identity。
- [x] 3.6 定义原子 Step candidate，包含 accepted pose、consumed planar displacement、remaining 和 stable report。
- [x] 3.7 定义拒绝阶段，区分资格、顶部、深度、高度、净空、前移、关联、稳定性和 overlap。
- [x] 3.8 定义只读诊断，包含 blocker、landing、actual height、consumed progress 和 query summary。
- [x] 3.9 确认所有临时模型不进入 snapshot、不跨 Tick 缓存。

## 4. 建立唯一 Step Solver 模块

- [x] 4.1 新增唯一 `DeterministicKccStepSolver`。
- [x] 4.2 通过构造或调用参数接收现有 capsule query contract。
- [x] 4.3 保持模块不拥有 collision world 和 Actor state。
- [x] 4.4 保持模块不修改输入 pose、remaining、support、constraint plane 或 query buffer 所有权。
- [x] 4.5 集中 blocker 选择、顶部发现、路径验证、landing 关联和结果构造。
- [x] 4.6 热路径复用调用方提供的预分配 buffer。
- [x] 4.7 禁止 LINQ、集合分配和动态扩容。
- [x] 4.8 不新增 asmdef、Solver descriptor、注册器或运行时选择开关。

## 5. 实现 blocker-linked 顶部发现

- [x] 5.1 从最早 TOI contact set 过滤闭合侧面 contact。
- [x] 5.2 排除稳定 ground、背离 movement、上部阻挡和没有最小平面进展的 contact。
- [x] 5.3 按 canonical contact identity 选择唯一 blocker。
- [x] 5.4 从 blocker normal 推导确定性的障碍内侧平面方向。
- [x] 5.5 从 `MaximumStepHeight` 上方执行只读 outer 向下 probe。
- [x] 5.6 要求 outer landing 高于当前稳定高度且不超过最大台阶高度。
- [x] 5.7 沿内侧方向偏移 `MinimumStepDepth` 执行 inner 向下 probe。
- [x] 5.8 要求 inner landing 与 outer landing 高度一致并且稳定。
- [x] 5.9 验证 Box landing 与 blocker primitive 相同。
- [x] 5.10 验证 Triangle landing 与 blocker 为自身或共享边相邻 primitive。
- [x] 5.11 拒绝尖角、细栏、回落到原高度和不相邻几何形成的候选。
- [x] 5.12 从 canonical landing 计算唯一 `actualStepHeight`。

## 6. 实现真实高度 Step Up 验证

- [x] 6.1 从 safe position 只按 `actualStepHeight` 执行向上 clearance cast。
- [x] 6.2 从 remaining 派生不超过请求幅度的实际前移。
- [x] 6.3 允许只读 probe 检查请求终点外的踏面深度。
- [x] 6.4 禁止 accepted movement 超过请求幅度。
- [x] 6.5 在实际抬高位置执行前向 cast。
- [x] 6.6 前向受阻时拒绝完整候选。
- [x] 6.7 从前移位置向下贴合已验证 landing 高度。
- [x] 6.8 建立使用稳定顶部法线和 identity 的 support report。
- [x] 6.9 对最终 pose 执行 overlap validation。
- [x] 6.10 对最终 support 与 outer/inner 证据执行一致性复核。
- [x] 6.11 只在全部阶段成功后返回 Step Up candidate。

## 7. 实现台阶鼻部 Support Evaluator

- [x] 7.1 定义普通 seam、台阶鼻部和 unsupported ledge 的输入分类。
- [x] 7.2 对顶部/立面共享边执行 outer/inner probe。
- [x] 7.3 outer 非稳定而 inner 在最小踏面深度内稳定时选择 inner 顶部法线。
- [x] 7.4 保留明确 ledge state，不构造共享边混合法线。
- [x] 7.5 将 previous support 和 movement direction 纳入连续性判断。
- [x] 7.6 角色朝空侧离开时取消 stable support。
- [x] 7.7 inner 证据或 primitive 关联失效时取消 stable support。
- [x] 7.8 普通稳定 face 和双稳定 seam 不执行 secondary probe。
- [x] 7.9 evaluator 只返回报告，不修改当前 Grounding。

## 8. 实现位移消费结果

- [x] 8.1 计算 Step Up 实际消费的平面位移。
- [x] 8.2 从原 remaining 派生未消费的闭合平面分量。
- [x] 8.3 将 consumed 与 remaining 一起写入 candidate。
- [x] 8.4 不在模块内继续 Motor collide-and-slide iteration。
- [x] 8.5 不在模块内写入 constraint plane。
- [x] 8.6 不写入或推导 `VerticalVelocity`。

## 9. 实现独立 Step Down 候选

- [x] 9.1 只允许 previous stable Grounded。
- [x] 9.2 只允许超过最小阈值的平面进展。
- [x] 9.3 最终 request 明确向上时直接拒绝。
- [x] 9.4 Ground Snap 成功时不建立 Step Down 候选。
- [x] 9.5 从 movement position 向下查询不超过 `MaximumStepHeight` 的 landing。
- [x] 9.6 要求实际下降超过 Ground Snap 微小范围。
- [x] 9.7 要求 landing 为 stable ground。
- [x] 9.8 对下级踏面边缘复用 inner/outer support evaluator。
- [x] 9.9 拒绝陡坡、unsupported edge、无支撑和超过最大高度的断崖。
- [x] 9.10 对 landing pose 执行 overlap 和 stable support 复核。
- [x] 9.11 只在全部阶段成功后返回完整 Step Down candidate。
- [x] 9.12 失败时不返回部分下移结果。
- [x] 9.13 不写入或推导 `VerticalVelocity`。

## 10. 收口模块诊断与交付物

- [x] 10.1 为 Step Up 和 Step Down 返回唯一成功阶段。
- [x] 10.2 为每次拒绝返回唯一 rejection。
- [x] 10.3 输出 blocker primitive/feature。
- [x] 10.4 输出 outer/inner/final landing identity。
- [x] 10.5 输出 actual height 和 consumed progress。
- [x] 10.6 确认 diagnostics 不影响 Fixed 分支、排序、state 或 hash。
- [x] 10.7 搜索模块对 Motor、WorldSolver、Composition、Unity asset 和 identity 的写入并全部删除。
- [x] 10.8 记录隔离实现提交或补丁范围。
- [x] 10.9 记录后续接入所需的唯一类型和调用入口。
- [x] 10.10 运行 `openspec validate rebuild-deterministic-kcc-step-solving --strict --no-interactive` 并修复全部问题。
- [x] 10.11 保持本 change 未合并、未归档，等待接入门禁放行。
