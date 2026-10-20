# 下载与安装

本项目代码由 AI 编写，美术由 AI 生成。代码采用 MIT；美术和第三方内容另行说明。完整安装包附 LICENSE.txt 与 ASSET_NOTICE.txt。

1. 在 GitHub 仓库右侧进入 Releases，选择对应版本，在 Assets 中下载 `Snowfallen-0.2.5-beta-20261020.zip` 或 `Snowfallen-0.2.5-stable-20261020.zip`。不要下载自动生成的源码 ZIP 用于安装。
2. 使用匹配分支的游戏和 BaseLib。清单要求 BaseLib 最低 3.3.0，Beta 游戏最低版本为 0.111.0；这些最低要求不代表兼容所有未来版本。正式版依赖须与其目标 API 匹配。
3. 在 Steam 库中右键游戏 → 管理 → 浏览本地文件，定位游戏根目录。完全退出游戏后，将现有 Snowfallen 文件夹备份到 mods 之外。
4. 解压 ZIP，把其中的 Snowfallen 文件夹放入游戏根目录的 mods 文件夹。正确层级是：

```text
mods/
└─ Snowfallen/
   ├─ Snowfallen.dll
   ├─ Snowfallen.pck
   └─ Snowfallen.json
```

5. 只启用匹配分支的一份 Snowfallen，避免重复加载手动安装和 Workshop 中相同 ID 的包，也不要混用 DLL/PCK/清单。把旧版仅改文件夹名留在 mods 里仍可能冲突。
6. 启动游戏，按界面提示启用模组；确认模组列表有 Snowfallen 和 BaseLib，并新开一局检查角色显示。

建议新开局使用，不承诺与其他内容版本的旧档兼容。需要回到其他版本时，退出游戏后恢复完整备份，不修改个人存档凑兼容。

游戏、BaseLib、Harmony 和 GodotSharp DLL 不随本包提供。出现缺依赖或 API 不匹配时，先核对分支和依赖版本。不要分享密码、API 密钥或带敏感信息的完整日志。

每包附 SHA256SUMS.txt，可用 PowerShell 的 `Get-FileHash 文件路径 -Algorithm SHA256` 检查文件内容指纹。哈希一致说明文件未变化，不保证没有游戏缺陷。
