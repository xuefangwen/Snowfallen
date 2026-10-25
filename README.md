# Snowfallen · 雪放与东铭

《杀戮尖塔 2》（Slay the Spire 2）角色模组，提供雪放与东铭两名可玩角色，分别维护 Beta 和正式版。

> **AI 创作声明：本项目代码由 AI 编写，美术由 AI 生成。** 作者提出需求并组织开发、修改与验证。此声明说明创作方式，不保证 AI 输出无缺陷，也不代替第三方权利核查。
>
> **代码采用 MIT，美术另行说明。** 源码目录不含美术原稿或完整安装包，玩家请从 Releases 下载对应 ZIP。

## 最新变化：2026-10-25

1. 雪放的玄狐变添加了简单特效。
2. 加入新角色东铭：可以使用虚实印翻转诅咒进行战斗，身负恶念缠绕，同时掌握冰与月的魔法，在诅咒、凝霜、月华多项能力中灵活运用，击败敌人。
3. 修复部分情况下，加载其他 Mod 的第四层时出现角色素材丢失的问题。

## 下载与安装

进入 [Releases](https://github.com/xuefangwen/Snowfallen/releases)，在 Assets 中选择匹配分支的 `Snowfallen-0.2.5-beta-20261025.zip` 或 `Snowfallen-0.2.5-stable-20261025.zip`。不要下载自动生成的 Source code ZIP 用于安装。

需自行安装对应游戏与 BaseLib。两版只安装一份，避免重复启用同 ID 的手动版与 Workshop 版，建议新开局使用。详见 [安装说明](docs/INSTALL.md)。

## 源码与验证

`src/beta` 和 `src/stable` 分别包含对应游戏 API 的源码、本地化及清单。两名角色的代码分布在 SnowfallenCode 和 DongmingCode；两版不能直接互相覆盖或混用依赖。编译见 [构建说明](docs/BUILD.md)。缺少美术的源码仓库不能直接导出完整 PCK。

完整包来源记录包含离线模型、资源及修复检查；详细范围与未验证事项见 [发布说明](docs/RELEASE_NOTES.md)。构建和离线验证不代表实际游戏、真人联机或旧档已通过。

原创代码、源码工程配置及随附说明文档采用 [MIT License](LICENSE)，限项目有权授予的范围。美术、动画、音频和第三方内容不自动纳入代码 MIT，具体见 [AI 与权利说明](docs/AI_AND_RIGHTS.md)。
