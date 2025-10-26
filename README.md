# AndroidTV IPTV Player

AndroidTV IPTV Player 是一款基于 Kotlin 开发的 Android TV 应用，支持通过 ExoPlayer 播放 IPTV 播放列表（M3U/M3U8）。项目覆盖频道管理、遥控器友好的界面、Room 数据持久化、WorkManager 周期性刷新以及针对电视端的设置体验。

## 功能特性

- **播放列表管理**：导入本地或远程播放列表 URL，使用 Room 持久化存储并支持收藏。
- **ExoPlayer 播放**：稳定的播放内核，支持自动重试与遥控器控制。
- **遥控器友好界面**：Jetpack Compose 页面针对 Android TV 导航进行优化。
- **定时刷新**：通过 WorkManager 定时任务保持播放列表最新，并在成功或失败时通知用户。
- **搜索与设置**：快速搜索频道，可配置刷新频率或默认播放列表。

## 项目结构

```
app/
 ├── data/            # Room 实体与 DAO
 ├── network/         # Retrofit 服务定义
 ├── repository/      # PlaylistRepository 负责数据编排
 ├── ui/              # 面向电视的 Compose Activity
 ├── util/            # 工具类（含 M3U 解析与播放器封装）
 ├── viewmodel/       # 主界面、播放、设置、搜索等 ViewModel
 └── workmanager/     # 后台刷新任务与初始化器
```

## 快速开始

1. **环境要求**
   - Android Studio Giraffe（或更新版本），已安装 Android SDK 34。
   - JDK 17。

2. **在 Android Studio 打开项目**
   - 克隆仓库后，通过 **File → Open** 选择项目根目录。
   - 按提示下载缺失的依赖和 Gradle Wrapper 组件。

3. **构建与运行**
   - 在 Android Studio 中点击 **Run**，即可部署到 Android TV 模拟器或实体设备（API 23+）。
   - 命令行方式：
     ```bash
     ./gradlew assembleDebug
     ```
     > 若运行时因环境限制无法下载 Gradle，需手动安装 Gradle 8.2.2 后再次执行命令。

4. **导入播放列表**
   - 启动应用后选择 **Import Playlist** 添加新的 M3U/M3U8 源。
   - 设置默认播放列表并开始观看频道。

## 后台刷新

`PlaylistRefreshWorker` 会根据用户设定的间隔（默认 12 小时）定期更新播放列表。刷新结果通过通知告知用户，同时借助 `WorkManagerInitializer` 在设备重启后重新注册任务。

## M3U 解析

`M3UParser` 会读取 `#EXTINF` 元数据（标题、分组、台标），并将其与后续流媒体地址配对，映射为本地持久化的 `Channel` 实体。

## 测试与质量

- Kotlin 代码遵循官方编码规范。
- Compose 界面基于 Material 3 组件，针对电视端焦点行为进行优化。

## 许可证

本项目作为模板/演示提供，不包含任何版权受限的流媒体资源。请确保导入的 IPTV 播放列表拥有合法使用权限。
