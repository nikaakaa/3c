## 1. 现状清单与边界确认

- [x] 1.1 列出Float32、Fixed与ServerAuthoritative observed Body interval的全部生产点
- [x] 1.2 列出 `CharacterPresentationBodyState` 的全部构造、转换与序列化边界
- [x] 1.3 列出 `CharacterPresentationRuntimeFactory` 的Local、Preview、Rollback和Observed调用点
- [x] 1.4 列出现有 `CharacterRemotePresentationProfile` 脚本、资产、`.meta` 和Scene引用
- [x] 1.5 确认Velocity与Grounded已经存在于Float32和Fixed `WorldBodyState`
- [x] 1.6 确认本change不需要修改Network packet、Program ABI、Snapshot或StateHash
- [x] 1.7 记录Committed recovery与Selected SmoothDamp的旧字段、方法和diagnostics引用

## 2. Body运动学表现合同

- [x] 2.1 为 `CharacterPresentationBodyState` 增加 `LinearVelocity`
- [x] 2.2 为 `CharacterPresentationBodyState` 增加 `Grounded`
- [x] 2.3 扩展Body state构造校验以拒绝非法Velocity
- [x] 2.4 更新Float32 `WorldBodyState`到Presentation Body的正式转换
- [x] 2.5 更新Fixed `WorldBodyState`到Presentation Body的正式转换
- [x] 2.6 更新ServerAuthoritative selected Body到Presentation interval的正式转换
- [x] 2.7 更新Local与Preview initial Body构造
- [x] 2.8 更新Body interval continuity比较以覆盖运动学字段需要的语义
- [x] 2.9 确认新增字段不进入网络载荷、Simulation snapshot或hash

## 3. Source与Trajectory配置模型

- [x] 3.1 将现有Body mode收窄并命名为明确的SourceMode
- [x] 3.2 定义 `Direct` 与 `BoundedCorrection` TrajectoryMode
- [x] 3.3 定义不可变 `CharacterBodyPresentationSettings`
- [x] 3.4 定义Presentation-owned `CharacterBodyPresentationProfile`
- [x] 3.5 为BoundedCorrection定义position half-life字段
- [x] 3.6 为BoundedCorrection定义maximum horizontal error字段
- [x] 3.7 为BoundedCorrection定义position settle threshold字段
- [x] 3.8 为BoundedCorrection定义yaw half-life字段
- [x] 3.9 为BoundedCorrection定义maximum yaw error字段
- [x] 3.10 为BoundedCorrection定义yaw settle threshold字段
- [x] 3.11 让Profile validation拒绝未知mode、非有限值和非法参数关系
- [x] 3.12 删除runtime默认profile和按source/model推断trajectory的可能入口

## 4. Profile资产单路迁移

- [x] 4.1 将 `CharacterRemotePresentationProfile` 脚本单路重命名为通用Body Presentation Profile
- [x] 4.2 保留迁移脚本的 `.meta` identity并更新类名、菜单名和namespace引用
- [x] 4.3 将现有ServerAuthoritative remote profile资产迁移为BoundedCorrection profile
- [x] 4.4 为Standard Local与Preview建立显式Direct profile资产
- [x] 4.5 为Corin DeterministicRollback建立显式BoundedCorrection profile资产
- [x] 4.6 将设计中的Corin首轮参数写入Rollback profile资产
- [x] 4.7 更新所有Scene、Prefab与Definition引用到唯一新profile类型
- [x] 4.8 删除旧profile类名、旧settings类型、旧字段名和旧CreateAssetMenu入口
- [x] 4.9 搜索并确认不存在旧profile兼容wrapper、MovedFrom或双资产入口

## 5. Target轨迹采样

- [x] 5.1 将Committed与Selected时钟推进收敛为只输出target sample的内部边界
- [x] 5.2 为Committed区间插值Position与Rotation
- [x] 5.3 为Committed区间插值LinearVelocity并解析Grounded
- [x] 5.4 为Selected区间插值Position与Rotation
- [x] 5.5 为Selected区间插值LinearVelocity并解析Grounded
- [x] 5.6 保持Committed presentation tick在branch replacement期间单调推进
- [x] 5.7 保持Selected interval queue与显式Reset的唯一source语义
- [x] 5.8 让连续append区间产生连续target而不创建correction事件
- [x] 5.9 让branch replacement、Reset与合法不连续产生明确retarget事件
- [x] 5.10 拒绝无Reset的非法Selected tick回退或区间断裂

## 6. Visual Trajectory Follower

- [x] 6.1 新建Presentation内部 `CharacterVisualTrajectoryFollower`
- [x] 6.2 实现Direct模式的target pose直通
- [x] 6.3 实现BoundedCorrection的position error与relative velocity状态
- [x] 6.4 实现BoundedCorrection的yaw error与relative yaw velocity状态
- [x] 6.5 使用presentation delta与position half-life推进临界阻尼误差
- [x] 6.6 使用presentation delta与yaw half-life推进临界阻尼误差
- [x] 6.7 将水平position error限制在profile maximum内
- [x] 6.8 将yaw error限制在profile maximum内
- [x] 6.9 在误差低于settle threshold时归零position correction状态
- [x] 6.10 在误差低于settle threshold时归零yaw correction状态
- [x] 6.11 Grounded target只平滑水平误差并直接跟随target Y
- [x] 6.12 Airborne target允许三维position correction
- [x] 6.13 新revision从当前visible状态重新计算误差而不累计旧offset
- [x] 6.14 Reset清空Follower状态并按正式profile重新锚定
- [x] 6.15 Dispose清空Follower状态且不保留跨Session数据

## 7. Body Runtime统一接入

- [x] 7.1 让 `CharacterBodyPresentationRuntime` 显式接收SourceMode与Profile settings
- [x] 7.2 将Target Sampler输出唯一交给Visual Trajectory Follower
- [x] 7.3 让正常Committed append不再进入旧recovery路径
- [x] 7.4 将Committed branch replacement迁移为一次Follower retarget
- [x] 7.5 保持Rollback Body transaction整批替换后只retarget一次
- [x] 7.6 让正常Selected interval不再持续运行SmoothDamp
- [x] 7.7 将Selected显式Reset迁移为Follower reset/retarget
- [x] 7.8 保持Body Runtime是VisualRoot唯一写入者
- [x] 7.9 保持visual bind position与rotation应用顺序
- [x] 7.10 从Body Runtime删除旧committed recovery offset与active状态
- [x] 7.11 从Body Runtime删除旧selected visual position/yaw/velocity SmoothDamp状态
- [x] 7.12 删除固定 `6f / tickRate` recovery时长和全部旧辅助方法

## 8. Factory与调用点装配

- [x] 8.1 让Factory所有正式创建入口都要求显式Body Presentation Profile
- [x] 8.2 集中校验SourceMode与TrajectoryMode组合
- [x] 8.3 将Standard `CharacterPipelineHost`绑定Direct profile
- [x] 8.4 将Preview registration绑定Direct profile
- [x] 8.5 将DeterministicRollback本地Actor绑定BoundedCorrection profile
- [x] 8.6 将DeterministicRollback无相机simulated Actor绑定同一BoundedCorrection profile
- [x] 8.7 将ServerAuthoritative observed Actor绑定其BoundedCorrection profile
- [x] 8.8 保持Local、Simulated、Observed仍返回同一Presentation Runtime合同
- [x] 8.9 搜索并删除Factory内部按Network Model、Actor名称或Camera猜测profile的路径
- [x] 8.10 确认Network Model模块不保存Follower状态、不调用SmoothDamp且不写VisualRoot

## 9. 动画与相机时钟保持

- [x] 9.1 保持 `AnimationSampleTick` 来自Source Cursor的target tick
- [x] 9.2 保持 `AnimationSampleAlpha` 来自Source Cursor的target alpha
- [x] 9.3 确认Follower correction不修改animation sample time或Animancer delta
- [x] 9.4 保持同一PlaybackId/generation的replay sample替换只更新目标采样
- [x] 9.5 保持producer变化继续由现有AnimationPlaybackLifecycle和Animancer Fade接管
- [x] 9.6 保持Camera只消费最终Body Presentation Frame且不建立第二份Body history
- [x] 9.7 搜索并删除用visual correction进度驱动动画或Gameplay的引用

## 10. Diagnostics与错误语义

- [x] 10.1 在Body frame中暴露target velocity与visible velocity
- [x] 10.2 在Body frame中暴露target grounded状态
- [x] 10.3 在Body frame中暴露SourceMode与TrajectoryMode
- [x] 10.4 在Body frame中暴露position/yaw correction error与velocity
- [x] 10.5 在Body frame中暴露correction active、clamped与settled状态
- [x] 10.6 保留previous/current tick、sample alpha、reset sequence与reset reason
- [x] 10.7 更新Presentation trace payload以区分target轨迹与visible轨迹
- [x] 10.8 保证diagnostics不参与Follower、Simulation、Network或动画选择
- [x] 10.9 为缺Profile、非法参数和非法stream continuity提供明确fail-fast错误

## 11. 清理与架构文档

- [x] 11.1 搜索并删除旧 `CharacterRemotePresentationProfile` 名称
- [x] 11.2 搜索并删除旧 `CharacterRemotePresentationSettings` 名称
- [x] 11.3 搜索并删除Body Runtime内全部直接 `SmoothDamp` 与 `SmoothDampAngle`
- [x] 11.4 搜索并删除固定六 Tick recovery与offset累加路径
- [x] 11.5 搜索并确认只有通用Follower拥有visual correction状态
- [x] 11.6 搜索并确认只有Body Runtime写VisualRoot
- [x] 11.7 更新 `openspec/project.md` 的Body source、trajectory profile和Presentation链说明
- [x] 11.8 修正 `openspec/project.md` 中已完成Presentation modules change的过时进度
- [x] 11.9 更新受影响current spec并删除与新source/trajectory边界冲突的旧文字
- [x] 11.10 更新本change implementation inventory以记录最终类型、资产和调用点

## 12. 静态编译与OpenSpec校验

- [x] 12.1 使用规定参数编译 `ThirdPersonClient.Runtime.csproj`
- [x] 12.2 编译后立即执行 `dotnet build-server shutdown`
- [x] 12.3 使用规定参数编译 `ThirdPersonSimulation.DeterministicRollback.Unity.csproj`
- [x] 12.4 编译后立即执行 `dotnet build-server shutdown`
- [x] 12.5 使用规定参数编译 `ThirdPersonSimulation.ServerAuthoritative.Unity.csproj`
- [x] 12.6 编译后立即执行 `dotnet build-server shutdown`
- [x] 12.7 使用规定参数编译 `ThirdPersonClient.Editor.csproj`
- [x] 12.8 编译后立即执行 `dotnet build-server shutdown`
- [x] 12.9 运行 `openspec validate refactor-character-visual-trajectory-following --strict --no-interactive`
- [x] 12.10 确认全部任务真实完成后再将本文件所有任务标记为 `[x]`
