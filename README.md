# AoAoEnergy-SP

FFXIV（国服）Dalamud 插件：使用爆发药时，将特效替换为"能量饮料GG"的 RGB 光效。

基于 [Loskh/AoAoEnergy](https://github.com/loskh/aoaoenergy) 修改维护。原作者已停止更新，原版插件在当前国服客户端无法生效，本项目修复并精简使其恢复可用。

## 与原版的区别

- 直接调用国服客户端自带的原生特效 `vfx/common/eff/ev_energydrink_01x.avfx`（即"能量饮料GG"物品的官方特效），不再依赖自制的 avfx 文件——原自定义文件在当前客户端已无法渲染
- 移除了已失效的资源重定向机制（当前客户端本地文件读取路径不生效）
- 移除了高度偏移、震屏开关等依赖修改特效文件的功能
- 保留了目标开关（自己/其他人）、职业过滤（战斗/生产/采集）、预览与清除效果功能

## 功能

- 喝爆发药时自动播放能量饮料 RGB 光效
- 可分别对"自己"和"其他人"生效进行开关
- 可按职业类型（战斗职业 / 生产职业 / 采集职业）过滤
- 设置窗口提供"预览当前效果"与"清除预览效果"按钮

## 构建

- .NET 10 SDK
- 依赖 Dalamud / FFXIVClientStructs / Dalamud.Bindings.ImGui / Newtonsoft.Json，引用路径见 `AoAoEnergy/AoAoEnergy.csproj`，需指向本机 Dalamud 运行时目录（`%APPDATA%\XIVLauncherCN\addon\Hooks\<version>\`）
- `dotnet build -c Release` 后将 `AoAoEnergy.dll` 与 `AoAoEnergy.deps.json` 放入插件目录即可

## 致谢

- 原作者：[Loskh](https://github.com/loskh) —— [AoAoEnergy](https://github.com/loskh/aoaoenergy)
