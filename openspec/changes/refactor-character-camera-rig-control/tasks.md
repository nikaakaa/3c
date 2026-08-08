## 1. 收敛Camera合同

- [ ] 1.1 定义稳定`CameraRigProfileId`、`CameraRigSlotId`和Profile revision合同
- [ ] 1.2 定义`CameraLookInputSourceKind`
- [ ] 1.3 定义`CameraLookInputSample`
- [ ] 1.4 定义完整`CameraModeProfile`
- [ ] 1.5 定义`CameraResolvedModeState`
- [ ] 1.6 定义`CameraTransitionPlan`
- [ ] 1.7 将`CameraPosePlan`迁移为`CameraRigPlan`
- [ ] 1.8 定义Recoil、FovKick、Shake和Collision Response typed modifier channel
- [ ] 1.9 让全部identity、weight、duration、curve和Rig参数具有严格合法性校验
- [ ] 1.10 禁止Camera合同引用Cinemachine、MonoBehaviour、Transform或Scene对象

## 2. 建立正式Camera Profile

- [ ] 2.1 新增`ThirdPersonCameraPresentationProfile`资产
- [ ] 2.2 新增Pointer look policy
- [ ] 2.3 新增Stick look policy
- [ ] 2.4 新增FreeLook mode profile
- [ ] 2.5 新增Aim mode profile
- [ ] 2.6 新增LockOn mode profile
- [ ] 2.7 新增ActionFocus mode profile
- [ ] 2.8 新增SkillCloseup mode profile与Shot Rig Slot引用
- [ ] 2.9 校验五种mode完整且唯一
- [ ] 2.10 校验Rig Slot、FOV、pitch、shoulder、distance、damping、collision和transition参数
- [ ] 2.11 拒绝Profile保存Scene Transform、Cinemachine组件、backend selector或fallback
- [ ] 2.12 将Corin正式Camera参数迁入唯一Profile

## 3. 重做Look输入与Basis锁存

- [ ] 3.1 扩展`ICharacterPresentationLookInput`返回typed look sample
- [ ] 3.2 在Unity Input Adapter按active control区分Pointer Delta与Rate输入
- [ ] 3.3 在Fixed Input Adapter使用同一look sample语义
- [ ] 3.4 在一次RenderFrame同时锁存look sample与Camera basis
- [ ] 3.5 让Float32 camera-relative move只读锁存basis
- [ ] 3.6 保持Fixed camera-relative move只读锁存basis
- [ ] 3.7 让Pointer delta不乘presentation delta
- [ ] 3.8 让Stick rate乘presentation delta并应用dead zone与响应曲线
- [ ] 3.9 让Camera response policy只修改消费权重
- [ ] 3.10 删除具体`ThirdPersonCameraController`从输入适配器构造参数的传播

## 4. 重构Mode仲裁与混合

- [ ] 4.1 让State Resolver使用priority、weight、source identity和producer generation稳定仲裁
- [ ] 4.2 实现BlendIn进入状态
- [ ] 4.3 实现BlendOut退出状态
- [ ] 4.4 实现Cut interrupt policy
- [ ] 4.5 实现HoldUntilSourceEnds interrupt policy
- [ ] 4.6 建立来源mode与目标mode的完整transition state
- [ ] 4.7 实现Linear、EaseIn、EaseOut与EaseInOut有限blend function
- [ ] 4.8 混合FOV、pitch、shoulder、distance、vertical arm、composition与damping
- [ ] 4.9 在显式handoff点切换target、Rig Slot与离散collision policy
- [ ] 4.10 让tracking reset清除旧transition且下一帧恢复正常推进
- [ ] 4.11 删除只计算debug progress但不影响Rig输出的旧逻辑
- [ ] 4.12 删除依赖Dictionary枚举顺序的同优先级结果

## 5. 完成Target与Modifier链

- [ ] 5.1 保持默认follow只消费最终`CharacterBodyPresentationFrame`
- [ ] 5.2 保持显式Camera Target只来自正式binding或Target Context
- [ ] 5.3 让LockOn target丢失时按正式request lifecycle退出
- [ ] 5.4 让Recoil输出有界yaw/pitch impulse与衰减
- [ ] 5.5 让FovKick输出lens offset与衰减
- [ ] 5.6 让Shake输出Impulse/Noise typed plan
- [ ] 5.7 让CollisionCorrection输出collision response policy
- [ ] 5.8 让Custom cue必须映射到已注册有限modifier kind
- [ ] 5.9 拒绝未知Custom cue、非法target和缺失Rig Slot
- [ ] 5.10 删除Shake、Recoil、CollisionCorrection进入Runtime后无效果通过的路径

## 6. 实现唯一Cinemachine Rig Adapter

- [ ] 6.1 将`ICameraRigAdapter`扩展为消费完整`CameraRigPlan`
- [ ] 6.2 让Camera Runtime只依赖`ICameraRigAdapter`
- [ ] 6.3 让输入侧只依赖`ICameraMovementBasisProvider`
- [ ] 6.4 新增唯一`CinemachineThirdPersonCameraRigAdapter`
- [ ] 6.5 使用显式Rotation Pivot保存yaw/pitch
- [ ] 6.6 使用唯一Gameplay Virtual Camera与`Cinemachine3rdPersonFollow`
- [ ] 6.7 映射shoulder、vertical arm、distance、damping、collision和FOV
- [ ] 6.8 映射screen composition与Aim稳定策略
- [ ] 6.9 映射Skill Shot Rig Slot和transition handoff
- [ ] 6.10 映射Recoil、FovKick与Shake输出
- [ ] 6.11 保持Cinemachine Brain唯一Manual Update
- [ ] 6.12 在Apply后发布稳定Camera basis
- [ ] 6.13 缺少Brain、Gameplay Rig、Profile或Rig Slot时明确失败
- [ ] 6.14 禁止adapter拥有第二request stack、target search或mode priority

## 7. 建立共享Camera Rig Prefab

- [ ] 7.1 新增唯一`ThirdPersonGameplayCameraRig.prefab`
- [ ] 7.2 显式绑定Main Camera、Brain、Rotation Pivot、Follow Point与Aim Point
- [ ] 7.3 显式绑定Gameplay 3rd Person Follow Rig
- [ ] 7.4 显式绑定SkillCloseup Shot Rig Slot
- [ ] 7.5 让Prefab只保存组件和Slot绑定，不复制mode参数表
- [ ] 7.6 让Standalone Scene实例化正式Camera Rig Prefab
- [ ] 7.7 让ServerAuthoritative Client Scene实例化正式Camera Rig Prefab
- [ ] 7.8 让DotRecast Authority Client Scene实例化正式Camera Rig Prefab
- [ ] 7.9 让Local Fixed Gameplay Lab引用正式Camera Rig Prefab
- [ ] 7.10 让DeterministicRollback产品引用正式Camera Rig Prefab
- [ ] 7.11 保持AI与remote actor不创建Camera runtime或Camera容器

## 8. 收口Host与Factory装配

- [ ] 8.1 让Local Character Host显式绑定Camera adapter、basis provider和Profile
- [ ] 8.2 让Fixed Character Host显式绑定同一Camera合同
- [ ] 8.3 让Rollback Character Host显式绑定同一Camera合同
- [ ] 8.4 让ServerAuthoritative本地玩家Host显式绑定同一Camera合同
- [ ] 8.5 让Presentation Runtime Factory按Local Owner capability创建Camera Runtime
- [ ] 8.6 让无Camera Actor收到Camera命令时继续明确失败
- [ ] 8.7 删除Rollback Host的`FindObjectOfType` Camera搜索
- [ ] 8.8 删除所有`Camera.main`、缺引用自动创建和默认Camera补齐
- [ ] 8.9 删除具体Camera MonoBehaviour类型跨模块传播
- [ ] 8.10 保持Camera不决定Body clock或Presentation role

## 9. 接入Camera Diagnostics

- [ ] 9.1 将`CameraDebugSnapshot`接入structured presentation diagnostics
- [ ] 9.2 发布全部active request与稳定winner
- [ ] 9.3 发布来源mode、目标mode、blend function、duration和progress
- [ ] 9.4 发布look source kind、raw sample、processed delta与response weight
- [ ] 9.5 发布target source、follow point与aim point
- [ ] 9.6 发布全部active modifier及剩余生命周期
- [ ] 9.7 发布最终`CameraRigPlan`
- [ ] 9.8 发布Apply后的`CameraBasisSnapshot`
- [ ] 9.9 删除未接入正式消费者的dead debug路径

## 10. 迁移Camera authoring内容

- [ ] 10.1 对账现有Camera Graph node与Timeline clip字段
- [ ] 10.2 将现有BlendIn、BlendOut和InterruptPolicy映射到正式transition plan
- [ ] 10.3 将现有Weight与Ease curve映射到有限blend function或正式custom curve identity
- [ ] 10.4 为Corin需要的Aim、ActionFocus和SkillCloseup内容建立明确mode request
- [ ] 10.5 保持Camera节点只编译PresentationCommand
- [ ] 10.6 保持Timeline不直接控制Cinemachine
- [ ] 10.7 保持Camera basis影响Action时固化为Gameplay fact
- [ ] 10.8 拒绝缺少正式target、Rig Slot或Profile identity的Camera producer

## 11. 删除旧Camera路径

- [ ] 11.1 删除旧`ThirdPersonCameraController`
- [ ] 11.2 删除全部Scene内`CinemachineFreeLook`正式运行对象
- [ ] 11.3 删除Gameplay Lab Builder程序化FreeLook与Collider创建
- [ ] 11.4 删除Runtime硬编码mode FOV
- [ ] 11.5 删除Controller硬编码统一灵敏度
- [ ] 11.6 删除未绑定到正式Brain的`ThirdPersonCameraBlends.asset`
- [ ] 11.7 删除FreeLook target rebinding与axis provider清理代码
- [ ] 11.8 删除旧Scene Camera重复参数和Prefab override
- [ ] 11.9 搜索并删除Camera自主Update/LateUpdate、直接Transform写入和Scene priority业务判断
- [ ] 11.10 确认仓库只剩唯一Camera request、mode mixer、Rig plan和Cinemachine adapter链

## 12. 文档与严格校验

- [ ] 12.1 更新`openspec/project.md`的Camera current state与唯一链路
- [ ] 12.2 将最终delta并入`character-camera-pipeline` current spec
- [ ] 12.3 对账`add-discrete-stair-presentation`的最终visible Body Y合同
- [ ] 12.4 记录Philippe KCC、Lyra和Cinemachine参考中实际采用与明确未采用的边界
- [ ] 12.5 运行`openspec validate refactor-character-camera-rig-control --strict --no-interactive`
- [ ] 12.6 运行Camera依赖与旧路径`rg`检查
