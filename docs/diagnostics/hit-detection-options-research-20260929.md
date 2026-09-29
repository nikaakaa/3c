# 命中检测方式：实际项目源码调研

日期：2026-09-29。范围：检测方式和实际实现，不决定最终攻击运行时方案，不修改运行时代码。

## 调研结论与前述说明修正

- 射线/线段、形状重叠、形状扫掠是场景查询的三个主要类别。Trigger 是接收物理检测结果的方式；扇形数学判定是相交算法的实现，不能把它们都说成互斥的底层查询类别。
- 在业务选型中，应比较具体实现：武器多线段、离散攻击体、多点形状扫掠、帧间姿态细分、包含旋转和双方运动的连续碰撞，以及物理事件驱动。这些可以组合。
- Box、胶囊、凸包、三角网格是形状；离线烘焙是数据准备方式。制作 Mesh 不等于完成体积判定，也不等于生成了扫掠体。
- 以下证据来自实际源码，不能推导为 ZZZ 的实现。示例项目不是性能基准，也不应整段搬入当前项目。

## 1. Unity FPS Sample：射线与角色几何查询结合

仓库版本：`c8375e7cf29cd0bcc3764b0650028a99aecb7475`。

- [RaySphereQueryReciever.cs](https://github.com/Unity-Technologies/FPSSample/blob/c8375e7cf29cd0bcc3764b0650028a99aecb7475/Assets/Scripts/Game/Modules/HitCollision/RaySphereQueryReciever.cs#L187)：环境用 `RaycastCommand` 批量查询。遇到场景遮挡后缩短查询距离，再执行角色检测。
- [HitCollisionJobs.cs](https://github.com/Unity-Technologies/FPSSample/blob/c8375e7cf29cd0bcc3764b0650028a99aecb7475/Assets/Scripts/Game/Modules/HitCollision/HitCollisionJobs.cs#L155)：角色检测读取指定 tick 的形状变换，对球、胶囊和 Box 使用自己的几何函数。部分分支把带半径的路径转成胶囊做相交。
- 具体边界：该样例返回角色命中时使用 `primCenter`，法线使用查询反方向并留有 TODO；不能把接口名字 SphereCast 当成所有形状都有精确最早碰撞时刻和交点的证明。
- 实际收益：场景遮挡与角色受击范围可以使用不同几何表示，并在一个查询业务入口汇总。
- 当前项目适用性：自定义定点受击体有实际项目先例，无需依赖 Unity Collider 才能成立。但不要照搬该样例的临时 NativeArray 分配、旧 ECS 接口及近似命中位置。

## 2. Unity 3D Game Kit：武器采样点和球形查询

来源为 Unity HLODSystemDemo 仓库内的 3DGamekit，版本 `07e2be4530d2705123d69960b519f6e9990c3fb0`。

- [MeleeWeapon.cs](https://github.com/Unity-Technologies/HLODSystemDemo/blob/07e2be4530d2705123d69960b519f6e9990c3fb0/Assets/3DGamekit/Scripts/Game/Weapon/MeleeWeapon.cs#L116)：每个 AttackPoint 配置半径、偏移和跟随 Transform，记录上一位置，在 FixedUpdate 调用 `SphereCastNonAlloc`。
- 编辑方式：手工布置少量攻击点并绘制球及轨迹，不需要逐招制作复杂 Mesh。
- 必须注意该版本源码：位移为 `worldPos - m_PreviousPos[i]`，但射线原点是 `worldPos`，实际向当前点前方扫，而非从上一点扫到当前点。此处只记录代码事实，不把它推荐为正确轨迹实现。
- 另有零位移时人为给一个微小位移的处理，不能据此保证初始重叠被正确处理。每次 BeginAttack 也新建上一位置数组。
- 结论：可参考编辑交互和采样点设计；不能因其是官方示例就直接视为满足当前项目的精度和 0 GC 要求。

## 3. MeleeTrace：沿武器布点，逐点做前后帧扫掠

仓库版本：`d7e0098f255e0f34dd5001b4233d80987a7c5955`。

- [MeleeTraceComponent.cpp](https://github.com/rlewicki/MeleeTrace/blob/d7e0098f255e0f34dd5001b4233d80987a7c5955/Source/MeleeTrace/Private/MeleeTraceComponent.cpp#L47)：取新位置，使用上一帧对应位置至新位置的 `SweepMultiByChannel`。
- [GetTraceSamples](https://github.com/rlewicki/MeleeTrace/blob/d7e0098f255e0f34dd5001b4233d80987a7c5955/Source/MeleeTrace/Private/MeleeTraceComponent.cpp#L218)：在 StartSocket 和 EndSocket 间按 TraceDensity 做线性插值，生成刀身采样点。
- 这里的插值是沿刀身增加空间采样点，不是重新求值帧间动画姿态。
- 优点：比单条刀尖射线覆盖更多刀身范围；形状厚度可以提高擦边覆盖。
- 限制：采样点前后位置连的是弦，不是完整旋转弧线。增加刀身点数不会自动消除时间方向的大角度误差。单次查询取目标当时姿态，也不等于处理了目标整个时间段内的运动。
- 当前项目适用性：如果选贴合武器的方案，运行时可用定点采样点实现相同思路；仍需明确旋转采样和姿态来源，不能直接消费任意渲染帧骨骼就宣称逻辑确定性。

## 4. Unity Chop Chop：启停攻击体并接收 Trigger

仓库版本：`608eac98df29cd97821a6115cd52dfb9027345b1`。

- [Attacker.cs](https://github.com/UnityTechnologies/open-project-1/blob/608eac98df29cd97821a6115cd52dfb9027345b1/UOP1_Project/Assets/Scripts/Characters/Attacker.cs)：启用/禁用攻击 GameObject。
- [Attack.cs](https://github.com/UnityTechnologies/open-project-1/blob/608eac98df29cd97821a6115cd52dfb9027345b1/UOP1_Project/Assets/Scripts/Characters/Attack.cs#L14)：`OnTriggerEnter` 过滤同阵营标签后向 Damageable 提交攻击。
- 优点：作者能直接查看和调整碰撞体，技能启停简单。
- 限制：这里是进入事件，并不天然提供持续攻击的命中节奏；也没有因此解决高速漏判。其时序和物理世界相关。
- 当前项目适用性：证明事件驱动是实际使用的业务方案，但接入现有定点逻辑需处理不同的世界状态与时序。当前不据此更换后端。

## 5. Bullet：包含平移和旋转的连续凸体碰撞

仓库版本：`63c4d67e337017f9d8b298c900e9aabdb69296e7`。

- [btContinuousConvexCollision.cpp](https://github.com/bulletphysics/bullet3/blob/63c4d67e337017f9d8b298c900e9aabdb69296e7/src/BulletCollision/NarrowPhaseCollision/btContinuousConvexCollision.cpp#L90)：输入 A/B 双方的起止变换，计算线速度与角速度，通过 GJK 距离和保守推进迭代求碰撞时刻。
- 代码使用角速度与角运动半径估计运动界，并在迭代中推进双方的旋转和平移。这与一次固定朝向的线性 BoxCast 不同。
- 收益：处理模型内的双方平移和旋转，不只检查当前姿态或端点连线。
- 限制：要求合适的凸形状表达，增加距离求解与迭代成本；有收敛阈值和迭代边界。起止姿态之间采用的运动模型仍不能恢复任意动画的弯曲轨迹。
- 当前项目适用性：是后续扩展连续凸体查询的技术参考，不是现有定点 KCC 已完成的能力，也不是本轮已决定引入 Bullet。

## 6. 扫掠线段与扫掠 Mesh：必须单列的实现方式

用户指出之前未充分覆盖这两种方式。它们不能简单用“射线”或“固定朝向的 BoxCast”替代说明。

### 扫掠线段

- 用线段两端 A/B 表示刀刃，输入前后姿态 A0/B0、A1/B1。检查整条线段在时间段内的运动，而非只追踪刀尖。
- 实用近似包括连接前后线段形成三角带、对时间细分并反复检测线段、或给线段厚度后进行胶囊运动检测。
- 四个端点连接成两个三角形只是近似：一般三维运动可能不共面，线性端点插值形成的扫掠面也不一定等同这两个平面三角形。刚体旋转弧线需要额外的运动模型或中间姿态。
- 另一条实现路线是直接解移动点-边、边-边等连续相交，不必先创建 Unity Mesh。

### 扫掠 Mesh

- 含义一：把武器截面/简化网格沿动作轨迹扫过，生成覆盖运动的表面或实体，再与目标相交。可在线生成，也可离线烘焙。
- 含义二：已有 Mesh 的顶点/三角形随时间运动，直接做连续碰撞检测。无需生成一个可见的扫掠体，也不等同 Unity 任意 MeshCast API。
- 生成扫掠体时要处理封闭、厚度、自交和凸分解等具体需求；取全部采样点的一个凸包会填掉凹处，可能放大命中范围。
- 仅对双方各自整段运动的空间并集求交，会丢失时间一致性：它们可能在不同时刻经过同一地点。需要双方同一时刻的运动判定，或者明确接受这种业务近似。
- 扫掠刀刃所形成的三角带本身就是一种 Mesh 表达，因此这两个名字并非互斥方案。

### 可查的连续网格实现

- [IPC Toolkit NarrowPhaseCCD](https://github.com/ipc-sim/ipc-toolkit/blob/main/src/ipc/ccd/narrow_phase_ccd.hpp) 明确提供带起止位置及碰撞时间输出的 point-edge、edge-edge、point-triangle 查询。
- [NonlinearCCD](https://github.com/ipc-sim/ipc-toolkit/blob/main/src/ipc/ccd/nonlinear_ccd.hpp) 接收随时间变化的位置函数和偏离线性轨迹的误差界。
- [实现](https://github.com/ipc-sim/ipc-toolkit/blob/main/src/ipc/ccd/nonlinear_ccd.cpp) 使用分段线性 CCD 处理非线性轨迹。它是计算几何/仿真库的实例，不是已验证可直接接入本项目的 Unity 战斗插件，也不是 ZZZ 使用该方法的证据。

## 当前项目已有能力与缺口

- `DeterministicCapsuleQueries` 已有 Overlap、Cast、CastAll、Raycast 和复用缓冲区。
- `TryCastPrimitive` 已基于距离/闭合速度执行保守推进；输入是配置固定的竖直胶囊与平移量，不包含任意凸体旋转和双方动态运动。
- 静态场景包含平面、三角形和轴对齐 Box，已有包围盒树。动态受击目标还需要自己的正式数据与更新职责。
- `DeterministicCollisionWorldBaker.AddMesh` 读取 Mesh 顶点与三角形并量化；这种表面数据不能自动提供封闭攻击体内部的命中语义。
- 因此可以复用定点数和已有几何能力，尚不能声称任意旋转 Box、扇形、凸包及其连续查询都已支持。

## 7. 通用查询先做扫掠：可复用库调查

用户当前范围：通用模块，先实现扫掠，开放正式调用接口；射线/线段以后再扩展，不以可琳专用脚本实现。以下仅为源码适配评估，未接入或编译外部库。

本地 `ProjectVersion.txt` 是 Unity `2022.3.62f2c1`；此前工具讨论中按 Unity 6 估计兼容性不准确。当前 `FixedScalar` 是 Q32.32。

| 候选 | 实际核对到的扫掠能力 | 数值/接入限制 | 本项目适用性 |
| --- | --- | --- | --- |
| BEPUphysics1int | GJKToolbox.ConvexCast、MPRToolbox.Sweep；接受双方凸形状、初始姿态和双方平移，返回 RayHit | C#、FixedMath.NET Q32.32；接口没有结束朝向/角速度；仓库最后推送时间为 2019-02-03 | 定点凸体平移扫掠源码的优先移植候选，不能当成完整旋转 Mesh 扫掠 |
| BEPUphysics2 | SweepDemo 实际覆盖多种基础形状、凸包、复合形状、Mesh 组合；ConvexSweepTaskCommon 处理双方线速度/角速度 | C# 浮点/SIMD；依赖其形状系统、SweepTaskRegistry 和 BufferPool | 功能较完整的参考，移植为现有定点数需要较大改造 |
| Jitter2 | DynamicTree.SweepCast 泛型 support mapping，提供球/Box/胶囊/圆柱入口 | float/double；所查接口为固定朝向+平移；当前 csproj 面向 net8.0/net9.0/net10.0 | 可参考窄相与查询接口，不可直接当成 Unity 2022.3 定点数包安装 |
| Bullet | btContinuousConvexCollision 接受双方起止变换，考虑旋转与平移 | C++，默认 float/可选 double；需要绑定或算法移植 | 旋转连续凸体求交参考，直接采用会改变现有数值后端 |
| IPC Toolkit | 连续 point-edge、edge-edge、point-triangle；非线性轨迹分段检测 | C++/Eigen，double 和相关依赖；不是 Unity 战斗插件 | 更接近移动网格表面的连续检测，适配量较大 |

### BEPUphysics1int 源码与边界

版本 `9237daa68c3014fd7c2e93c6a99326ba5248d60b`。

- [GJKToolbox.ConvexCast](https://github.com/sam-vdp/bepuphysics1int/blob/9237daa68c3014fd7c2e93c6a99326ba5248d60b/BEPUphysics/CollisionTests/CollisionAlgorithms/GJK/GJKToolbox.cs#L269)。
- [MPRToolbox.Sweep](https://github.com/sam-vdp/bepuphysics1int/blob/9237daa68c3014fd7c2e93c6a99326ba5248d60b/BEPUphysics/CollisionTests/CollisionAlgorithms/MPRToolbox.cs#L1314)。两者是同一需求的不同算法入口，正式实现应选定路径，不把另一套作为 fallback。
- [ConvexShape](https://github.com/sam-vdp/bepuphysics1int/blob/9237daa68c3014fd7c2e93c6a99326ba5248d60b/BEPUphysics/CollisionShapes/ConvexShapes/ConvexShape.cs) 提供方向极值点方法，但继承 EntityShape；ConvexHullShape 还依赖其几何构建、资源管理等代码。不能说复制单个方法就可独立运行。
- [Fix64](https://github.com/sam-vdp/bepuphysics1int/blob/9237daa68c3014fd7c2e93c6a99326ba5248d60b/FixedMath.Net/src/Fix64.cs) 和项目同为 Q32.32，但同格式不证明乘除舍入、溢出及容差行为相同。拟移植时使用项目现有数学类型，不长期保留第二套定点数路径。
- [LICENSE.md](https://github.com/sam-vdp/bepuphysics1int/blob/9237daa68c3014fd7c2e93c6a99326ba5248d60b/LICENSE.md) 分别列出 fork、FixedMath.Net 与原 BEPU 的许可；移植保留对应来源和许可。
- README 报告其历史整库性能约为浮点版的四倍耗时，并列出数值范围和多线程确定性限制。这是作者对旧版本的报告，不是本项目扫掠性能实测，不能直接套用。

### 其他候选源码

- [BEPU2 SweepDemo](https://github.com/bepu/bepuphysics2/blob/c230dd1178d6f481d8b3f03c0f595f8ad910b725/Demos/Demos/SweepDemo.cs)、[ConvexSweepTaskCommon](https://github.com/bepu/bepuphysics2/blob/c230dd1178d6f481d8b3f03c0f595f8ad910b725/BepuPhysics/CollisionDetection/SweepTasks/ConvexSweepTaskCommon.cs)。原库 Apache-2.0。
- [Jitter2 SweepCast](https://github.com/notgiven688/jitterphysics2/blob/9e62240e264d444bfe9a61d63361c13c5edcb6ea/src/Jitter2/Collision/DynamicTree/DynamicTree.SweepCast.cs)、[csproj](https://github.com/notgiven688/jitterphysics2/blob/9e62240e264d444bfe9a61d63361c13c5edcb6ea/src/Jitter2/Jitter2.csproj)。MIT。该树查询返回最近命中，不能直接冒充攻击所需的全部目标查询。

### 接口和形状表达建议，尚未实施

- 模块公开扫掠查询，接收形状、运动描述、目标过滤和调用方结果存储。目标身份、命中时间、位置/法线与初始重叠状态需要明确合同；初始重叠不能伪造唯一接触点。
- 核心几何求交不含可琳、技能名、伤害和震动；攻击业务负责命中节奏、次数及正式事实输出。
- 射线/线段以后复用目标数据、过滤及结果类型，届时增加入口；现在不声明未实现的方法或额外实现选择器。
- 泛用凸体可通过方向极值点表达，Box、胶囊、凸包共用凸体算法。凹 Mesh 不能直接按其全部顶点取极值：那得到的是整体凸包，会填掉凹处。凹形状须明确采用凸分解或三角表面语义。
- 旋转运动不能默默降级为起始朝向的平移扫掠。BEPU1int 的现有接口只覆盖平移部分；如果交付范围包含旋转，需要实现相应运动算法或有误差界的细分方案后再声称支持。
- 结论：有可复用核心实现，没有在本轮核查中找到同时满足“本项目定点数、Unity 2022.3、旋转、任意 Mesh、直接安装”的完整包。优先评估 BEPU1int 的定点凸体算法依赖，旋转能力另按明确算法补齐；此结论不是已批准引入整套物理世界。

## ZZZ 证据边界

元数据目录：`D:/ZZZ_Dump/PIK分析包/元数据/控制器与战斗`。

- 已确认 `MonoBoxCollider` 持有 BoxCollider，`MonoFanCylinderCollider` 持有 MeshCollider；父类有 SetupTimeDrivenCollider 和 Trigger 回调。
- 已确认 BoxCollisionDetect、BoxCollisionContinuousDetect、FanCollisionWithHeightDetect 的配置类型及字段。
- Unity Physics 查询方法存在不等于指定攻击调用了该方法。
- 尚未追通瞬时 Box/Fan 的全部运行时消费者；尚未证明角色的所有攻击都使用 Trigger。
- 尚未找到可琳攻击的离线轨迹烘焙/扫掠体生产消费证据；不得把 Continuous 命名解释为 CCD。
- 外部案例只能用来比较方案，不能用于补写 ZZZ 未确认的行为。

## 仅用于核对边界的引擎文档

- [PhysX Scene Queries](https://nvidia-omniverse.github.io/PhysX/physx/5.4.0/docs/SceneQueries.html)：Raycast、Overlap、Sweep 及返回信息。
- [PhysX Geometry Queries](https://nvidia-omniverse.github.io/PhysX/physx/5.4.0/docs/GeometryQueries.html)：形状组合、初始重叠、穿透深度、点距离和三角面语义。底层 PhysX 的行为不能不加核对地等同 Unity 包装 API。
- [Unity ComputePenetration](https://docs.unity3d.com/ScriptReference/Physics.ComputePenetration.html)：给定双方姿态计算穿透方向/深度；它不是任意运动的连续碰撞查询。

本轮没有执行这些外部项目，没有性能实测，没有安装第三方库，没有运行 Unity 或 replay。比较来自源码和文档；性能只列影响因素，不给出无测量依据的耗时排名。
