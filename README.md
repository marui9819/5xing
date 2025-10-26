# AndroidTV IPTV Player

AndroidTV IPTV Player is a Kotlin-based Android TV application that streams IPTV playlists (M3U/M3U8) using ExoPlayer. The project showcases channel management, remote-friendly UI, Room persistence, periodic playlist refresh with WorkManager, and settings tailored for leanback devices.

## Features

- **Playlist management** – Import remote or local playlist URLs, persist them with Room, and mark favorites.
- **ExoPlayer playback** – Resilient playback engine with automatic retry and remote control support.
- **Remote-friendly UI** – Jetpack Compose screens optimized for Android TV navigation.
- **Scheduled refresh** – WorkManager task keeps playlists up to date with notifications on success or failure.
- **Search and settings** – Quickly find channels and adjust refresh cadence or default playlist.

## Project structure

```
app/
 ├── data/            # Room entities and DAOs
 ├── network/         # Retrofit service definitions
 ├── repository/      # PlaylistRepository orchestrating data operations
 ├── ui/              # Compose-based activities for TV
 ├── util/            # Helpers including M3U parser and player wrapper
 ├── viewmodel/       # ViewModels for main, player, settings, search flows
 └── workmanager/     # Background refresh worker and initializer
```

## Getting started

1. **Prerequisites**
   - Android Studio Giraffe (or newer) with Android SDK 34.
   - JDK 17.

2. **Open in Android Studio**
   - Clone the repository and open the project via **File → Open**.
   - Let Android Studio download missing dependencies and Gradle wrapper components.

3. **Build & run**
   - From Android Studio: click **Run** to deploy on an Android TV emulator or device (API 23+).
   - From command line:
     ```bash
     ./gradlew assembleDebug
     ```
     > The provided wrapper script attempts to download the Gradle launcher if missing. If your environment blocks the download, install Gradle 8.2.2 manually and rerun the command.

4. **Import a playlist**
   - Launch the app and select **Import Playlist** to add a new M3U/M3U8 source.
   - Choose a default playlist and start watching channels.

## Background refresh

The `PlaylistRefreshWorker` schedules periodic updates based on the user-selected interval (default 12 hours). Notifications surface refresh success or failure, and the worker re-registers on boot via the `WorkManagerInitializer`.

## M3U parsing

`M3UParser` reads `#EXTINF` metadata (title, group, logo) and pairs it with the subsequent stream URL, mapping it into `Channel` entities persisted locally.

## Testing & quality

- Kotlin style follows the official Kotlin coding conventions.
- Compose UI uses Material 3 components optimized for TV focus behavior.

## License

This project is provided as a template/demo and does not include proprietary streaming sources. Ensure you have the rights to any IPTV playlists you import.
