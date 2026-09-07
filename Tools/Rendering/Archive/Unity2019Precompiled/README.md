# 旧预编译导入失败证据

本目录位于 Unity Assets 外，不参与导入或运行。保留的是原始 Shader、两份实验材质与旧注册插件，不是可用的还原版。

原 Unity 2019 Shader 在 Unity 2022 绘制时报 GPU 程序类型无法识别，随后 `ShaderLab::Program::GetMatchingSubProgram` 崩溃。旧插件会在域加载及 Shader 导入后注册此 Shader、重绑两份实验材质，不能修复程序格式。2026-09-04 已检查插件 IL；两份实验材质没有场景、Prefab 或其它资产引用，遂按逐文件 SHA-256 不变迁出导入目录。

社区 ZZZMiyabi Shader、当前四份角色材质及现有 Renderer 引用未改。用户要求保留的社区版与未来还原版仍分开保存；未来正式还原版使用从原程序恢复并由当前 Unity 重新编译的源码，不会恢复本目录的自动注册路径。

文件保留原名及原 `.meta`，仅用于来源核对。不要把整个目录复制回 Assets。

原崩溃日志：`C:/Users/Lenovo/AppData/Local/Temp/Unity/Editor/Crashes/Crash_2026-09-04_071334389/Editor.log`，同型记录另见当日 `064647303`、`070816660`。
