# 编译雪放源码

本仓库保留 Godot.NET.Sdk 4.5.0、net9.0 和 Alchyr.Sts2.ModAnalyzers 0.1.6。源码工程不包含美术场景或完整导出资源；编译 DLL 与导出可玩安装包是两件事。

## 环境

- 安装 .NET SDK，优先 .NET 9；可先运行 `dotnet --version` 确认可用。其他 SDK 版本以实际构建结果判断。
- 首次构建需要从 NuGet 还原 Godot SDK 和分析器依赖。
- 安装对应分支的正版游戏与匹配 BaseLib，找到自己的 DLL。
- `GameDataDir` 指向包含 sts2.dll、GodotSharp.dll、0Harmony.dll 的对应游戏数据文件夹。
- `BaseLibDll` 指向匹配该分支的 BaseLib.dll 文件。

仓库不分发游戏原版 DLL、第三方 DLL 或原版反编译源码。不要为了凑齐依赖混用其他分支的 DLL。

## Beta 构建

在仓库根目录打开 PowerShell。将占位路径替换为自己电脑上的路径后，执行：

```powershell
dotnet build ./src/beta/Snowfallen.csproj -c Release '-p:GameDataDir=D:/你的游戏目录/data_sts2_windows_x86_64' '-p:BaseLibDll=D:/你的BaseLib目录/BaseLib.dll'
```

成功判据是进程退出码为 0、总结中 0 个错误，并出现本次生成的 Snowfallen.dll 输出路径。阅读警告，不使用构建失败后遗留的旧 DLL。

## 正式版构建

将命令中的 `src/beta` 换为 `src/stable`，两个依赖参数也替换为正式版实际依赖，再单独执行。游戏更新可能改变 API，同名 DLL 不代表版本一致。

## 工程整理范围

上传工程用显式编译清单限定 SnowfallenCode 和入口 MainFile.cs，用参数替换开发机路径；两个 project.godot 只服务源码编译，不配置启动场景。中文本地化作为 AdditionalFiles 随源码保留。

公开源码保留雪放使用的动画辅助逻辑。原有卡牌、遗物、药水数值和本地化文案未因工程整理而改写。对应发布 DLL 从该源码构建，不能混用旧 DLL 与新源码。

完整 PCK 仅在 Releases 中提供。将源码工程直接导出会缺图或缺场景，不要把它当成完整美术工程。
