## 1. 对账故障与唯一边界

- [x] 1.1 记录故障Tick的start position、requested displacement与previous grounding raw值。
- [x] 1.2 记录八轮movement的position、TOI、remaining before/after raw值。
- [x] 1.3 记录每轮canonical contact的PrimitiveId、SurfaceId、FeatureId和normal raw值。
- [x] 1.4 确认Primitive 812映射`RoughTile_05_07`且Surface 95身份稳定。
- [x] 1.5 确认Primitive 705映射`RoughTile_04_07`且Surface 86身份稳定。
- [x] 1.6 确认异常来自movement iteration耗尽而非candidate/contact/pair容量。
- [x] 1.7 确认Foot Placement、LegIK、Animation和Presentation没有进入故障调用链。
- [x] 1.8 对账current `deterministic-kcc-world-solver`的失败、Baker与multi-plane合同。
- [x] 1.9 对账`add-discrete-stair-presentation`对同一Collision Artifact的后续修改点。
- [x] 1.10 对账`close-deterministic-rollback-character-pipeline`只消费KccId和CollisionWorldHash。
- [x] 1.11 锁定本change不修改Step、Ground Probe、Ramp或Body Motion语义。
- [x] 1.12 锁定本change不新增第二Motor、第二Baker或运行时fallback。

## 2. 收敛 active constraint 数据合同

- [x] 2.1 为constraint plane补齐稳定候选排序所需的identity读取。
- [x] 2.2 保持constraint plane容量来自现有锁定配置。
- [x] 2.3 保持相同PrimitiveId与FeatureId更新原plane而不新增副本。
- [x] 2.4 保持NormalMergeDot只合并等价法线而不吞掉独立约束。
- [x] 2.5 定义candidate kind的固定排序。
- [x] 2.6 定义single-plane candidate的固定plane index排序。
- [x] 2.7 定义two-plane crease candidate的固定index pair排序。
- [x] 2.8 定义fixed raw vector字典序tie-break。
- [x] 2.9 定义全部active plane共用的可行性判定。
- [x] 2.10 定义candidate与原始remaining的平方距离比较。
- [x] 2.11 定义退化或近似平行crease的拒绝边界。
- [x] 2.12 定义零向量作为始终存在的封闭约束候选。

## 3. 实现完整 active-constraint 求解

- [x] 3.1 将`ProjectRemaining`输入固定为原始remaining与完整planeCount。
- [x] 3.2 实现原始remaining可行候选检查。
- [x] 3.3 实现每个active plane的单平面投影候选。
- [x] 3.4 实现每对非平行active plane的交线投影候选。
- [x] 3.5 对每个候选校验全部active planes。
- [x] 3.6 按最小平方距离选择唯一可行候选。
- [x] 3.7 按固定候选顺序解决等距结果。
- [x] 3.8 保持求解过程只使用FixedScalar与FixedVector3。
- [x] 3.9 保持求解过程不分配集合、数组、委托或字符串。
- [x] 3.10 删除只处理前两个plane和第三plane特判的旧实现。
- [x] 3.11 保持一面撞墙的合法切向位移。
- [x] 3.12 保持两独立面只留下合法crease位移。
- [x] 3.13 保持约束封闭时输出Fixed零向量。
- [x] 3.14 保持结果不依赖contact容器遍历顺序。

## 4. 实现零进展接触状态

- [x] 4.1 定义transient zero-progress contact signature结构。
- [x] 4.2 signature保存canonical contact count。
- [x] 4.3 signature保存每项PrimitiveId。
- [x] 4.4 signature保存每项FeatureId。
- [x] 4.5 signature保存每项normal raw值。
- [x] 4.6 在Motor构造时按MaximumContacts预分配signature scratch。
- [x] 4.7 在每次Move开始时清空上一调用的signature有效位。
- [x] 4.8 在position发生有效前进时清空signature。
- [x] 4.9 在TOI非零时清空signature。
- [x] 4.10 在projected remaining改变时清空signature。
- [x] 4.11 在contact count或任一contact identity变化时替换signature。
- [x] 4.12 第一次完整零进展时保存signature并允许一次确认迭代。
- [x] 4.13 第二次完整相同零进展时清零remaining并退出movement loop。
- [x] 4.14 保持退出position为最后canonical safe position。
- [x] 4.15 保持已经形成的collision summary与output contacts。
- [x] 4.16 保持零进展退出前已经完成的initial penetration recovery结果。
- [x] 4.17 保持零进展退出后继续执行Ground Probe。
- [x] 4.18 保持零进展退出后继续执行最终static validation。
- [x] 4.19 为Motor result增加只读transient termination reason。
- [x] 4.20 保持termination reason不进入Snapshot或StateHash。
- [x] 4.21 让无法形成完整证明的iteration耗尽继续抛失败。
- [x] 4.22 删除任何调大MaximumContactIterations的临时修改。

## 5. 更新 Runtime 诊断和身份

- [x] 5.1 在成功movement诊断中输出termination reason。
- [x] 5.2 在BlockedNoProgress诊断中输出确认轮数。
- [x] 5.3 在BlockedNoProgress诊断中输出最后canonical contact set。
- [x] 5.4 保持普通成功帧不创建诊断字符串。
- [x] 5.5 保持capacity failure继续报告required与capacity。
- [x] 5.6 保持true non-convergence failure报告stage和remaining。
- [x] 5.7 提升唯一Motor/query策略版本。
- [x] 5.8 让策略版本变化进入KccId。
- [x] 5.9 保持旧KccId无法与新KccId完成Rollback握手。
- [x] 5.10 保持Runtime程序集不引用UnityEngine或Editor类型。

## 6. 建立 Baker walkable Box 交叠校验

- [x] 6.1 在Baker收集阶段建立稳定Collider source record。
- [x] 6.2 source record保存Collider hierarchy identity。
- [x] 6.3 source record保存Surface identity与Walkable。
- [x] 6.4 source record保存量化后的Box八顶点。
- [x] 6.5 按现有稳定Collider顺序生成pair。
- [x] 6.6 只检查双方均进入Artifact且Walkable的Box pair。
- [x] 6.7 用量化顶点派生双方局部Y支撑轴。
- [x] 6.8 让平行支撑轴pair退出竞争支撑面校验。
- [x] 6.9 从双方上表面四边形派生水平XZ分离轴。
- [x] 6.10 只保留上表面水平投影具有超容差正面积交叠的pair。
- [x] 6.11 用量化顶点派生第一个Box的三个OBB面轴。
- [x] 6.12 用量化顶点派生第二个Box的三个OBB面轴。
- [x] 6.13 生成九个跨Box叉积轴。
- [x] 6.14 跳过长度不超过固定退化阈值的SAT轴。
- [x] 6.15 在每个有效轴投影双方八顶点。
- [x] 6.16 使用一个quantization cell作为唯一接触容差。
- [x] 6.17 允许平行支撑拼装、坡面平台边界、面接触、边接触和容差内量化残差。
- [x] 6.18 拒绝三项条件同时成立且全部OBB有效轴均形成超容差正穿透的pair。
- [x] 6.19 选择最小穿透轴与深度作为稳定诊断。
- [x] 6.20 聚合全部非法pair而不只报告首项。
- [x] 6.21 诊断包含两个Collider identity。
- [x] 6.22 诊断包含两个Surface identity。
- [x] 6.23 诊断包含量化penetration depth。
- [x] 6.24 在生成或写入Artifact前执行交叠校验。
- [x] 6.25 校验失败时保持既有Artifact字节不变。
- [x] 6.26 保持Baker不修改Collider、Layer、Transform或Scene。
- [x] 6.27 保持Baker不自动删除triangle或执行CSG。
- [x] 6.28 保持非Walkable封闭障碍可按正式作者几何进入Artifact。
- [x] 6.29 保持MeshCollider与TerrainCollider沿现有canonical lowering处理。

## 7. 迁移 Gameplay Lab 粗糙地面

- [x] 7.1 枚举全部`RoughTile_*`可见对象与当前Collider。
- [x] 7.2 枚举粗糙地面当前Deterministic Surface owner。
- [x] 7.3 提取全部Tile上表面的持久化边界与高度。
- [x] 7.4 建立一个连续共享边界的粗糙地面Mesh资产。
- [x] 7.5 保持Mesh顶面表达现有小坡度变化。
- [x] 7.6 删除Mesh中的内部封闭侧面和底面。
- [x] 7.7 拒绝Mesh中的退化triangle。
- [x] 7.8 拒绝Mesh中的非流形共享边。
- [x] 7.9 为粗糙地面建立单一无Renderer Collider对象。
- [x] 7.10 让该对象使用Ground层。
- [x] 7.11 让该对象引用唯一连续MeshCollider。
- [x] 7.12 让该Collider由恰好一个Deterministic Surface owner拥有。
- [x] 7.13 让该Collider继续被Foot Placement Ground mask查询。
- [x] 7.14 从全部可见`RoughTile_*`删除BoxCollider组件。
- [x] 7.15 从Tile层级删除废弃的Surface owner数据。
- [x] 7.16 保持全部RoughTile Renderer和视觉Transform不变。
- [x] 7.17 删除`RoughTile_05_07`与`RoughTile_04_07`的重叠Gameplay Collider。
- [x] 7.18 确认粗糙地面与周边普通Ground只形成单一连续支持边界。
- [x] 7.19 保持场景不按Tile名称建立Runtime特判。
- [x] 7.20 保持场景不保留旧Tile Collider作为Foot Placement副本。
- [x] 7.21 删除旧`CourseBase`的全覆盖BoxCollider并保留其视觉组件。
- [x] 7.22 建立围绕粗糙区域精确开孔的持久化`CourseGroundCollision`顶面Mesh。
- [x] 7.23 让粗糙Mesh外围顶点与Course Ground只在`y=0`共享边界。
- [x] 7.24 将LowStairs Gameplay与Foot子树统一归一到课程根`x=12`。
- [x] 7.25 将Gentle Ramp及Top平台作为同一路段移入空闲车道。
- [x] 7.26 将Steep Ramp及Top平台作为同一路段移入空闲车道。
- [x] 7.27 将`Vault_H0.90_Yaw15`移出OverLimit上行Ramp。
- [x] 7.28 保持以上场景修正不分离Renderer与Collider Transform。

## 8. 发布唯一 Collision 与 KCC 身份

- [x] 8.1 保持Collision Artifact schema不因纯作者几何迁移无故升级。
- [x] 8.2 通过现有显式菜单执行唯一Collision Bake。
- [x] 8.3 确认Baker消费新的连续粗糙地面MeshCollider。
- [x] 8.4 确认Artifact不再包含旧RoughTile闭合Box primitives。
- [x] 8.5 确认Artifact保留Ramp、Step课程和其它正式Surface。
- [x] 8.6 更新唯一Collision Artifact canonical bytes。
- [x] 8.7 更新唯一CollisionWorldHash。
- [x] 8.8 更新唯一KccId。
- [x] 8.9 让Gameplay Lab Local Fixed Variant引用新CollisionWorldHash与KccId。
- [x] 8.10 让DeterministicRollback Variant引用同一CollisionWorldHash与KccId。
- [x] 8.11 保持两个Variant只在Source与Network Model装配上不同。
- [x] 8.12 保持Product Build和Run不隐式触发Collision Bake。

## 9. 收敛文档与静态校验

- [x] 9.1 更新change delta中的active-constraint合同。
- [x] 9.2 更新change delta中的零进展成功终止与true non-convergence失败边界。
- [x] 9.3 更新change delta中的竞争支撑面Box穿插拒绝合同。
- [x] 9.4 更新`openspec/project.md`中的Fixed KCC当前状态。
- [x] 9.5 更新KCC来源对账文档中的remaining movement收敛映射。
- [x] 9.6 更新KCC实现清单中的Motor策略版本与身份。
- [x] 9.7 对账`add-discrete-stair-presentation`只在新Artifact基线上继续。
- [x] 9.8 对账`close-deterministic-rollback-character-pipeline`不覆盖新身份。
- [x] 9.9 检查Runtime代码不存在UnityEngine、LINQ和热路径分配。
- [x] 9.10 检查竞争支撑面校验不存在OnInspectorGUI、OnValidate或自动Bake入口。
- [x] 9.11 检查仓库没有旧ProjectRemaining特判或Tile Collider兼容路径。
- [x] 9.12 编译受影响的portable普通.NET工程并禁用build servers。
- [x] 9.13 关闭本轮dotnet build servers。
- [x] 9.14 执行`openspec validate fix-deterministic-kcc-zero-progress-contact --strict --no-interactive`。
