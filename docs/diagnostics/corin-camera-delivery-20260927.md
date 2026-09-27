# 可琳相机接入（2026-09-27）

## 当前实现

- 持续效果使用现有 CameraEffectTrack / CameraEffectClip：窗口、资源引用、权重与渐变曲线由正式 Timeline 合同消费。
- 震动在表现域 TreeClip 的 OnEnable 节点触发一次；宿主占一个源帧，震动时长来自 CameraShakeAsset。
- TimelinePresentationEventBridge 在当前循环中保留震动请求身份；普通片段退出不自动取消震动。OnDisable（作者所说的 OnExit）通过条件分支执行同一个相机请求节点时，才提交对应撤销。没有取消分支时，资源播完后由相机域移除完成请求。失效与回滚撤销仍走原来的域撤销入口。
- 相机模块负责求值和输出，不直接从技能修改 Unity Camera。
- 修正方向角误加为相机俯仰角的问题：方向角用于屏幕平面位移，旋转只消费旋转幅度。

## 数据来源及生成

来源：`D:/ZZZ_Dump/output/corin_replication/replication-guide/镜头参数.md`、`镜头补缺.md`、`data/variants/battle-0.json`、`shake-0.json` 和 `analysis/resolved-camera-dependencies.json`。

通过正式资源配置 API 保存 13 个震动资源、3 条共享曲线。通过 btsmtl.generate_assets 保存普攻内嵌时间轴及 7 条 E/Rush 外部时间轴；通过 PublishSelected 发布 Attack、RushAttack、BranchAttack 的 Fixed/Float32 执行产物。

## 已确认与限制

- 相机接入与调用路径修复经 Editor csproj 编译：0 错误；构建服务器已关闭。后续 OnExit 选择性取消改动由 Unity 编译。
- E/Rush 七条时间轴生成均返回 saved=true；普攻生成连接中断后，通过磁盘变更及三项技能正式编译发布成功确认产物可编译。
- 首次回放在 783/2716 帧失败，原因是源码父调用路径与带入口标识的执行调用路径被直接比较。已通过既有 SourceMap 映射修复，没有跳过身份或版本校验。
- 对正式执行产物核对相机绑定：Attack 24、RushAttack 48、BranchAttack 12 个生命周期入口全部匹配，失败 0。详见 `corin-camera-binding-20260927.json`。
- 第二次诊断回放在 174 帧主动停止以应用调用路径修复；修复后完整回放尚未通过，不能将绑定核对等同于完整运行或视觉验收。
- 整理数据中的 StandardConfigKey 标准模板覆盖尚未解析；本次使用资源原值，不能视为原游戏最终镜头参数的完整复现。
- E 持续按住时跨 Start/Loop/Walk 的推镜连续性尚未闭合；当前排布 Start、Explode 的镜头窗口。
- 普攻第一段 A01 是命中震动配置；已移除逻辑命中分支里原来的无效相机节点，尚未接入命中确认后的震动触发。
- 强化 Rush 起手恰好位于阶段末尾的震动事件未向下一阶段搬移，避免改变动作长度。
