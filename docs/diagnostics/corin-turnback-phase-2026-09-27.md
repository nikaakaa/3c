# Corin TurnBack 同步排查

## 已确认的要求

当前正在播放的动画保留原来的时间与速度。切入动画在自己允许的入口内寻找对应相位。混合期间只调整切入动画，结束后从调整后的时间继续。转身中间的双脚支撑是合法内容，不能按走跑循环的左右交替规则拒绝整段素材。

## 当前资源与运行链

`CorinAnimationPresentationProfile.asset` 的 `Locomotion.Gait` 当前只包含 Run 与 Walk。TurnBack 没有进入该组，也没有注册 `m_GaitPhase`。已有 Foot Motion Target 和 Ready 的脚部分析缓存不等于同步配置完成。

原生 Clip Player 之前没有消费同步组；原生 StateMachine 直接混合两个各自推进的播放器。本次接入的链路为：Profile 同步组 → 动画领域资源中的 Clip Phase plan → StateMachine 的当前源和切入目标 → Player 有效时间 → 同一时间的 Pose、脚特征和属性采样。原始时钟不被重写。Reset、退出同步和帧丢弃不保留失效关联。

## 从实际数据读取的结果

2026-09-27 通过 Unity 实例 `e852139597e42532` 读取当前 Ready 分析缓存，并在隔离 Preview Scene 采样 dump 与烘焙动画。预览场景已关闭，没有保存到正式场景。

| 素材 | 精确 GUID | 观察 |
| --- | --- | --- |
| 战斗 Run FootMotionTarget | `4e613663635adb8428d84960c297d5d7` | 首段右脚支撑；约 0.267 秒左脚落地时，右脚校准足底高度约 0.441 米 |
| 战斗 TurnBack dump | `835befbd18dffcb9734b445f90fc0af9` | 首帧与战斗 Run 首帧腿姿相同；先右脚支撑，再进入双脚着地，之后继续转身并恢复跑步 |
| 战斗 TurnBack FootMotionTarget | `ac3b96331bba6754aa9bc13302762790` | 约 0.167 秒第一次左脚落地，两脚校准足底高度约 -0.002／0.002 米；之后长时间双脚接近地面；约 0.933 秒再次出现左脚 Landing |
| MainCity_Run_TurnBack dump | `74e2307a050733d4c904cce5abf2ef14` | 从左脚支撑起步；首帧左右脚骨高度约 0.091／0.423 米；素材长度约 1.35 秒，与战斗版本约 1.183 秒不同 |

表中校准足底高度与脚骨高度不是同一种量，不应直接混算。Run 与 TurnBack 的校准足底数据来自同一个分析源；MainCity 行只用于确认起步侧，尚未作为正式 Foot Motion Target 分析、发布。

dump 还存在 MainCity_Run_End_L／R 与 MainCity_Walk_End_L／R。能证明原素材具有按左右脚分开的部分动作，不能从这些动画文件推断原游戏状态机、同步组或切换算法。

## 尚未解决的边界

只把 Run 的左脚 Landing 与 TurnBack 的第一次左脚 Landing 都写成 0.5，不能证明两只脚的姿态相容：前者另一只脚抬起，后者两脚着地。有限素材整段相位可计算，也不等于允许从整段任意位置开始转身。不能放宽入口到转身结束之后来获得“匹配成功”。

MainCity 版本提供另一侧起步的实际素材，但它不是战斗版本的自动等价替换。需要核对持械姿势、转身时序、正式 Motion Reference 与控制层运动时间；不能仅因为起步脚相反就切入现有战斗状态。当前控制层 MovingTurn 的运动时间为 28/60 秒，出口混合约 0.3 秒，二者也不能被当作素材全长。

本次没有扩大 TurnBack 的允许入口、替换为 MainCity 动画、增加响应等待或发布未经确认的 TurnBack 同步资产。通用代码编译通过不能作为腿部拉扯消失的证据；尚未完成真实运行效果验收。

当前范围按作者要求只处理战斗素材，城市素材不作为修复方案。

## 本次落地与发布阻塞

通用同步代码通过 `ThirdPersonClient.Editor.csproj` 单工作进程编译，0 错误、34 警告，构建服务器已关闭。

战斗 Run 与 Walk 原注册相位在首帧分别为 -0.03333334、-0.02631579，而当前 Ready 分析缓存的右脚 Landing 在首帧。已通过正式相位 AuthoringService 修正这两条注册曲线；Run 的左右脚锚点为 0／0.266666681 秒，Walk 为 0／0.3 秒。曲线保存后两份脚部分析缓存仍为 Ready，没有重新执行脚部分析。

动画领域资源编译发现当前 28 Clip ACL 组缺失或过期。本次随后执行正式 `Publish ACL Resources` 菜单，完成了 28 段读取／采样，但整个组的质量门槛失败：

- Clip：`Assets/AssetArt/Animation/MyDemoNeed/Corin/PipelineInplace/Corin_Pipeline_Attack5_End_Inplace.anim`。
- Clip-to-sampling 最大旋转误差：140.516068°；Clip-to-ACL：140.51326°。
- 诊断包括 Bip001、左右 Elbow、裙摆、武器骨骼及两个表情标量。

该发布失败使动画领域资源尚未采用新的相位计划。没有重试同一次失败、放宽精度门槛或修改攻击素材。当前文件状态不是“已烘焙完即可运行”；还需要解决这个正式资源组阻塞，再完成战斗 TurnBack 入口、出口配置与效果确认。
