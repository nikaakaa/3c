## 1. 锁定迁移边界与第三方来源

- [x] 1.1 逐项核对 `add-deterministic-rollback-kcc-model` 的 KCC、Collision Artifact、Fixed WorldState、Snapshot、Hash 与 Actor contact 合同。
- [x] 1.2 记录当前 endpoint-overlap cast、grounding、step、wall slide、penetration 和 static reconstraint 的唯一调用链。
- [x] 1.3 确认 `ICharacterWorldSolver.ResolveBatch`、Fixed Program ABI、Rollback Source/Pipeline 和 Presentation 不需要修改合同。
- [x] 1.4 记录 Philippe KCC 只作为行为参考的边界，并确认 Fixed asmdef 不引用其 package/assembly。
- [x] 1.5 固定 Rapier/Parry 与 PhysX 参考版本、来源文件、许可证和允许移植的最小算法集合。
- [x] 1.6 建立正式第三方声明位置，记录移植来源与本项目 Fixed Q32.32 修改范围。
- [x] 1.7 核对 active changes 对 DeterministicKcc 目录、Fixed Snapshot 与 collision baker 的文件重叠并锁定串行修改顺序。

## 2. 定义 canonical query 与 contact 数据合同

- [x] 2.1 定义 stable `CollisionPrimitiveId` 与 `CollisionFeatureId` 的编码和比较规则。
- [x] 2.2 定义 Fixed closest-feature 合同，使 Plane、Triangle、Box 进入同一 portable query pipeline。
- [x] 2.3 定义 shape cast 输入，包含 capsule pose、displacement、skin/contact offset、过滤范围与 iteration budget。
- [x] 2.4 定义 shape cast 输出，包含 hit、TOI、normal、character/world witness、primitive/feature identity 与 separation。
- [x] 2.5 定义 overlap/penetration 输出，区分初始重叠、接触、分离和不可恢复状态。
- [x] 2.6 定义 canonical contact set 的排序、去重、normal 合并与 simultaneous TOI 规则。
- [x] 2.7 定义 query failure code，覆盖 invalid shape、capacity、degenerate input、non-convergence 与 invalid artifact。
- [x] 2.8 定义 Session 创建期预分配的 candidate、contact、manifold 和 scratch layout。

## 3. 实现 Fixed 几何基础与 support-map 查询

- [x] 3.1 盘点现有 Fixed vector、dot、cross、normalize、sqrt 与除法的范围和溢出语义。
- [x] 3.2 复用 Core Fixed 几何操作完成 query kernel，不在 KCC 内复制数值实现。
- [x] 3.3 实现 upright capsule 轴线端点和稳定最近点选择。
- [x] 3.4 实现 quantized triangle face/edge/vertex 最近特征与退化 triangle 拒绝规则。
- [x] 3.5 实现 quantized box 的精确胶囊轴最近特征与稳定 face tie-break。
- [x] 3.6 实现 Plane 的解析 signed distance、overlap 与 translational TOI 路径。
- [x] 3.7 实现 Fixed 胶囊轴到 Primitive 的精确距离合同并输出 stable feature。
- [x] 3.8 为 segment、triangle edge/vertex 和 box face 退化情况定义稳定 tie-break。
- [x] 3.9 从最终 closest feature 计算稳定见证点、分离距离和法线。
- [x] 3.10 实现初始相交后的 Fixed penetration direction/depth 求解。
- [x] 3.11 对 penetration non-convergence 返回明确 failure，不返回零位移成功。
- [x] 3.12 为 query kernel 写入算法版本与第三方来源声明。

## 4. 实现连续胶囊 shape cast

- [x] 4.1 实现基于 Fixed distance query 的保守推进循环。
- [x] 4.2 在每次推进中使用位移长度上界计算保守 TOI 增量并防止越过接触面。
- [x] 4.3 处理位移平行于接触面、背离接触面和零位移三种分支。
- [x] 4.4 处理 TOI 0 的 touching 与 initial penetration 区别。
- [x] 4.5 在固定容差内收敛到最早保守 TOI，不依赖终点 overlap。
- [x] 4.6 对同一最早 TOI 收集全部 canonical contact feature。
- [x] 4.7 实现 capsule overlap 查询并复用同一 feature/contact 输出。
- [x] 4.8 删除 query 正确性对外部 movement substep 的依赖。
- [x] 4.9 保留显式最大位移/预算约束，但不把预算耗尽解释为无碰撞。
- [x] 4.10 将 candidate/contact overflow 转换为带 primitive 和 stage 的正式 failure。

## 5. 升级 Collision World Artifact 与 baker

- [x] 5.1 定义新的 artifact schema version、quantization profile 与 canonical content hash。
- [x] 5.2 将 MeshCollider 顶点和索引降低为稳定 quantized indexed triangles。
- [x] 5.3 将 TerrainCollider/TerrainData 高度网格降低为同一 indexed-triangle 表面。
- [x] 5.4 为 triangle 生成 stable primitive id、vertex id、edge id 与 face id。
- [x] 5.5 生成 triangle adjacency 和 shared-edge identity。
- [x] 5.6 固定 one-sided winding、背面处理与法线生成规则。
- [x] 5.7 在 baker 中拒绝退化 triangle、量化后坍缩和重复冲突 identity。
- [x] 5.8 更新 broadphase bounds/index，使 candidate 输出保持 canonical order。
- [x] 5.9 将 artifact schema、quantization 与 adjacency version 纳入 WorldConfigurationHash。
- [x] 5.10 删除旧 artifact schema reader 与旧无 adjacency 的兼容读取路径。
- [x] 5.11 使用正式 codec 重建 Rollback Demo collision artifact 及其引用。

## 6. 建立唯一 DeterministicKccMotor

- [x] 6.1 定义 Motor 输入：body、capsule、desired displacement、previous support、configuration 与 query service。
- [x] 6.2 定义 Motor 输出：applied displacement、body state、ground report、contacts、diagnostics 与 failure。
- [x] 6.3 将初始 overlap recovery 放到 movement 前的固定阶段。
- [x] 6.4 实现最早 TOI 移动和 contact offset 停靠。
- [x] 6.5 实现单平面 remaining displacement 投影。
- [x] 6.6 实现双平面交线约束。
- [x] 6.7 实现三平面或相反约束下的自由度封闭。
- [x] 6.8 实现 normal 合并和近共面 contact 去重。
- [x] 6.9 实现每轮 progress 判定，区分正常阻挡与 non-convergence。
- [x] 6.10 实现最终 overlap 校验，禁止提交仍穿透的 static candidate。
- [x] 6.11 删除旧简化 wall-slide 和逐法线覆盖路径。

## 7. 实现稳定 Grounding、坡面与边缘判定

- [x] 7.1 定义 `FoundAnyGround` 与 `IsStableOnGround` 的独立语义。
- [x] 7.2 定义 support primitive/feature、ground normal、distance 与 ledge state。
- [x] 7.3 实现 capsule bottom support region 判定。
- [x] 7.4 使用 slope threshold 判断稳定地面，同时保留陡坡作为普通 collision contact。
- [x] 7.5 使用 triangle adjacency 区分共享内边、外边和断崖边缘。
- [x] 7.6 实现坡顶与 triangle seam 的稳定 ground normal 选择。
- [x] 7.7 实现 previous support 连续性规则，禁止跨不相邻 feature 保持假 grounded。
- [x] 7.8 实现下坡 ground probe 并保持 canonical contact 顺序。
- [x] 7.9 将 ground/support 输出接入现有 Fixed BodyResult 与 GameplayFact 所需字段。

## 8. 实现 Step Up/Down 与 Ground Snap

- [x] 8.1 定义 step eligibility，排除稳定坡面、背离障碍和无水平进展请求。
- [x] 8.2 实现向上 clearance cast 并使用 capsule 顶部真实空间。
- [x] 8.3 实现前向 step cast 并要求最小水平进展。
- [x] 8.4 实现向下 landing probe 并限制 MaxStepHeight。
- [x] 8.5 要求 landing feature 为稳定 ground，不能只命中墙面或陡坡。
- [x] 8.6 对 step 最终 pose 执行 overlap 与 support validation。
- [x] 8.7 将 raise/forward/down 作为一个候选事务，只在全部成功时接受。
- [x] 8.8 实现 previous stable support、非向上运动与 SnapDistance 共同控制的 ground snap。
- [x] 8.9 禁止跨断崖、陡坡、头顶阻挡或显式向上运动执行 snap。
- [x] 8.10 删除旧部分提交式 step 与无 previous support 约束的 snap 路径。

## 9. 保持 Actor batch 与静态重约束闭环

- [x] 9.1 让全部 Actor 的初始静态 candidate 通过新的 Motor/query pipeline 生成。
- [x] 9.2 保持 candidate 生成期间不写入 committed world state。
- [x] 9.3 保持 `DeterministicActorContactSolver` 的 stable ActorId pair order 与 `SolidBodyBlock` 语义。
- [x] 9.4 将 Actor contact 修正后的 pose 重新送入同一 static reconstraint contract。
- [x] 9.5 确认 static reconstraint 不调用旧 query helper或第二个 Motor。
- [x] 9.6 在任一 Actor query/contact/reconstraint 失败时 abort 完整 batch。
- [x] 9.7 只在所有 Actor 成功后按 request set 一一对应提交 BodyResult。
- [x] 9.8 确认 WorldSolver 不读取 Network Model、Input、Timeline、Presentation 或 Unity Transform。

## 10. 升级 KCC State、Snapshot 与身份

- [x] 10.1 盘点哪些 ground/support 字段会影响下一 Tick 分支。
- [x] 10.2 将 stable support primitive/feature、normal、grounded 与必要 counters 加入 Fixed KCC state。
- [x] 10.3 确认 transient candidate、simplex、manifold、query scratch 和 diagnostics 不进入 state。
- [x] 10.4 更新 Fixed KCC state canonical codec。
- [x] 10.5 更新 SimulationWorldSnapshot 的 KCC state 编解码和恢复路径。
- [x] 10.6 更新 actor/world KCC subhash。
- [x] 10.7 将 query/Motor semantic version、容差、迭代和容量纳入 KccId。
- [x] 10.8 将 artifact schema/quantization/adjacency version 纳入 WorldConfigurationHash。
- [x] 10.9 在 Session 创建前严格校验 Program requirements、KccId、artifact hash/schema 与 Fixed ABI。
- [x] 10.10 删除旧 KCC state codec、旧 KccId version 和兼容读取分支。

## 11. 收敛热路径与诊断

- [x] 11.1 在 Solver runtime 创建期分配每 Actor query scratch 和 batch shared buffers。
- [x] 11.2 移除 ResolveBatch、Motor、query、ground、step 与 reconstraint 热路径中的 LINQ。
- [x] 11.3 移除热路径中的临时 List/Dictionary/字符串格式化和按 Primitive 分配。
- [x] 11.4 锁定 candidate、contact、manifold、pair 和 iteration 容量，禁止运行时自动扩容。
- [x] 11.5 输出 requested/applied/remaining displacement 与 movement iteration。
- [x] 11.6 输出 hit TOI、primitive/feature、normal、ground stability 与 support identity。
- [x] 11.7 输出 step 尝试阶段和拒绝原因。
- [x] 11.8 输出 penetration/reconstraint 的 iteration、required capacity 与 failure stage。
- [x] 11.9 确认 diagnostics 不改变排序、数值分支、state、snapshot 或 hash。

## 12. 删除旧路径与无效正式依赖

- [x] 12.1 删除 endpoint-overlap 后二分的旧 `DeterministicCapsuleQueries.Cast` 实现。
- [x] 12.2 删除旧简化 overlap/contact 数据结构和只按 PrimitiveId 决策的路径。
- [x] 12.3 删除旧 grounding、wall slide、step 与 snap helper。
- [x] 12.4 删除旧 static reconstraint 查询分支。
- [x] 12.5 搜索并删除旧 artifact schema/version 与旧 KccId 的引用。
- [x] 12.6 从 Unity package manifest 与 lock 删除未使用的 `com.janooba.kcc` 正式依赖。
- [x] 12.7 确认产品 asmdef、源码、资产与 build graph 不引用 `Gawidev.KCC` 或 `KinematicCharacterMotor`。
- [x] 12.8 确认 `Physics.*`、CharacterController、TerrainData 与 Mesh API 只存在于 Editor baker或其它 Float32 adapter，不进入 Fixed runtime。
- [x] 12.9 确认仓库只有一个 `DeterministicKccWorldSolver` Composition 入口和一个 Fixed KCC Motor 路径。

## 13. 更新正式资产与文档

- [x] 13.1 更新 Rollback Demo 的 KCC profile 为新完整配置并生成新 KccId。
- [x] 13.2 更新 Rollback Demo collision artifact 为新 schema/content hash。
- [x] 13.3 更新 Composition/Endpoint/Scene 的正式引用，不保留旧 artifact 引用。
- [x] 13.4 更新 `openspec/project.md` 的 Deterministic KCC 当前实现说明。
- [x] 13.5 更新 rollback implementation inventory，明确成熟 KCC 的支持范围和 non-goals。
- [x] 13.6 更新受影响的 current spec 描述，删除“substep 等同连续 sweep”等过时口径。
- [x] 13.7 记录基础 Rollback change 与本 change 的归档顺序。

## 14. 编译与 OpenSpec 校验

- [x] 14.1 使用禁止共享编译进程的参数编译 Fixed/Core/DeterministicKcc 相关程序集。
- [x] 14.2 使用禁止共享编译进程的参数编译 Runtime 与 Editor 主程序集。
- [x] 14.3 每次编译后立即执行 `dotnet build-server shutdown`。
- [x] 14.4 搜索确认不存在旧 KCC runtime、fallback、compatibility reader、第二 Solver 或无效 package 引用。
- [x] 14.5 运行 `openspec validate refactor-deterministic-kcc-movement-runtime --strict --no-interactive` 并修复全部问题。
