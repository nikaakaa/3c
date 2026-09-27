# 第五段无输入收尾修复

## 已保存的修改

- Attack5 主体完成转移改为进入 Attack5End，不再直接进入技能出口；作者入口尚未成功生成保存，不能认为运行图已采用。
- 最终 Pipeline 收尾动画从 1.45 秒恢复为 2.016667 秒，与已经修正的 WithWeaponInplace 源一致。通过正式 AnimationClip 作者服务替换、归零水平根位移，保留原资产身份。
- 收尾 FootPlacementWeight 按缺失的 34 帧向后平移，补入首段零权重，保留原收尾权重时序。
- 仅将收尾的 Motion Reference 绑定改为完整的 WithWeaponRootmotion 动画；其他绑定与 IK 求解代码未修改。
- 正式单片段足部烘焙已成功 Apply：122 个采样点，22 条配套曲线，回读状态 Same。左脚 LiftOff=47、Landing=74；双脚均无接触的样本数为 0。

## 证据

- Target：`Assets/AssetArt/Animation/MyDemoNeed/Corin/PipelineInplace/Corin_Pipeline_Attack5_End_Inplace.anim`
- Analysis Artifact：`db7bb3c0a4f31b08446eab9602f056feadff67656fc0782a2abb6ead2edbd51c`
- Artifact Content：`6fee02c6443dd3d4a549122218f8ab53451b5add15a5d0038c7d3fbe8f72ad71`
- Registered Curves：`807d3fd51bf296bab08297a6ac8ad9e72a07fbf125ab678d3e398850aa504cb1`
- 动画位于仓库忽略的本机美术目录；本次动画修改不包含在 Git 提交中。

## 待完成的正式发布

`btsmtl.generate_assets` 请求期间 Unity 会话断开，磁盘 Attack Definition 没有变化。随后编辑器连续程序集重载；HTTP 服务在线，但官方 CLI 和 MCP 均没有注册的目标实例。主编辑器仍在运行，未重启、未绕过作者系统写图资产。

连接恢复后：确认 Edit 且编译完成；重新生成 CorinAttackGameplayAbilityAuthoringCode，核对自然完成边进入 Attack5End、End 动画长度为 121 帧；PublishSelected 仅发布 Attack Fixed/Float32；通过正式 ACL 发布和 Animation Domain 编译更新运行资源。

没有运行 replay，没有运行表现验收。目前不是可测试交付状态。
