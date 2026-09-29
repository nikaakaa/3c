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

## 当前项目已有能力与缺口

- `DeterministicCapsuleQueries` 已有 Overlap、Cast、CastAll、Raycast 和复用缓冲区。
- `TryCastPrimitive` 已基于距离/闭合速度执行保守推进；输入是配置固定的竖直胶囊与平移量，不包含任意凸体旋转和双方动态运动。
- 静态场景包含平面、三角形和轴对齐 Box，已有包围盒树。动态受击目标还需要自己的正式数据与更新职责。
- `DeterministicCollisionWorldBaker.AddMesh` 读取 Mesh 顶点与三角形并量化；这种表面数据不能自动提供封闭攻击体内部的命中语义。
- 因此可以复用定点数和已有几何能力，尚不能声称任意旋转 Box、扇形、凸包及其连续查询都已支持。

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
