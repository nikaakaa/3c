# 可琳相机接入（2026-09-27）

> 历史记录：保存当时实现、问题与证据；当前合同见[现行文档索引](../../../../openspec/maintenance-audit.md)，当前进展见[文档入口](../../../README.md)。文中的“当前”“待完成”和旧阻塞均属于该记录时点。

## 当前实现

- 持续效果使用现有 CameraEffectTrack / CameraEffectClip：窗口、资源引用、权重与渐变曲线由正式 Timeline 合同消费。
- 震动在表现域 TreeClip 的 OnEnable 节点触发一次；宿主占一个源帧，震动时长来自 CameraShakeAsset。
- TimelinePresentationEventBridge 在当前循环中保留震动请求身份；普通片段退出不自动取消震动。OnDisable（作者所说的 OnExit）通过条件分支执行同一个相机请求节点时，才提交对应撤销。没有取消分支时，资源播完后由相机域移除完成请求。失效与回滚撤销仍走原来的域撤销入口。
- 相机模块负责求值和输出，不直接从技能修改 Unity Camera。
- 修正方向角误加为相机俯仰角的问题：方向角用于屏幕平面位移，旋转只消费旋转幅度。

## 数据来源及生成

来源：`D:/ZZZ_Dump/output/corin_replication/replication-guide/镜头参数.md`、`镜头补缺.md`、`data/variants/battle-0.json`、`shake-0.json` 和 `analysis/resolved-camera-dependencies.json`。

通过正式资源配置 API 保存 13 个震动资源、3 条共享曲线。通过 btsmtl.generate_assets 保存普攻内嵌时间轴及 7 条 E/Rush 外部时间轴；通过 PublishSelected 发布 Attack、RushAttack、BranchAttack 的 Fixed/Float32 执行产物。

逐项核对 13 个已保存震动资源的 18 个原始数值字段，共 234 项，在浮点存储精度内与 `data/variants/shake-0.json` 一致，见 `corin-camera-source-audit-20260927.json`。该核对不包含未经确认的枚举转换、坐标空间选择或震动求值公式。

## 已确认与限制

- 相机接入与调用路径修复经 Editor csproj 编译：0 错误；构建服务器已关闭。后续 OnExit 选择性取消改动由 Unity 编译。
- E/Rush 七条时间轴生成均返回 saved=true；普攻生成连接中断后，通过磁盘变更及三项技能正式编译发布成功确认产物可编译。
- 首次回放在 783/2716 帧失败，原因是源码父调用路径与带入口标识的执行调用路径被直接比较。已通过既有 SourceMap 映射修复，没有跳过身份或版本校验。
- 对正式执行产物核对相机绑定：Attack 24、RushAttack 48、BranchAttack 12 个生命周期入口全部匹配，失败 0。详见 `corin-camera-binding-20260927.json`。
- 后续补齐 Timeline 相机资源依赖，并修正共享 Tick 时钟的倍率输入后，2716 帧诊断回放完成，运行错误 0，相机有效输出 2716 帧、请求 984 次。这个结果只证明当前输入的运行链路可执行，不证明视觉或手感复现。
- StandardConfigKey 标准模板正文尚未定位，而且整理文档明确要求先确认它是否只供作者阶段使用；不能假定原游戏存在运行时模板覆盖。
- 当前 CameraShakeEffectEvaluator 使用项目自定义正弦波、请求身份散列生成的起始相位，以及 1.07/1.13 旋转频率系数。这些没有原游戏消费者证据；复制资源数值不等于复现原震动。
- 当前求值没有完整消费 ShakeType、CameraShakePropConfigEnum、DissipationMode 和 CustomCurveKey 等原始语义。空间曲线正文虽已取得，不能把未使用曲线称为已经还原空间衰减。
- E 持续按住时跨 Start/Loop/Walk 的推镜连续性尚未闭合；当前排布 Start、Explode 的镜头窗口。
- 普攻第一段 A01 是命中震动配置；已移除逻辑命中分支里原来的无效相机节点，尚未接入命中确认后的震动触发。
- 强化 Rush 起手恰好位于阶段末尾的震动事件未向下一阶段搬移，避免改变动作长度。
