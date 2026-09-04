# 旧预编译 Shader 已停止导入

Unity 2019 的 `NapAvatarStandard.asset` 在本项目 Unity 2022 绘制时曾触发原生崩溃，不能通过注册名字或重绑材质修复。

原 Shader、两份无人引用的实验材质及自动注册插件已逐文件校验 SHA-256 后移到仓库 `Tools/Rendering/Archive/Unity2019Precompiled/`，原始数据没有删除。

当前社区材质保持不变。原程序源码恢复资产位于 `Assets/Render/ZZZRestored/`；其中角色 LUT 已实际绘制，主体角色渲染尚未完成验收。不得将旧预编译资产重新挂回角色或启用强制绘制入口。
