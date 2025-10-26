package com.codex.androidtviptvplayer

import android.app.Application
import androidx.work.Configuration
import androidx.work.OneTimeWorkRequestBuilder
import androidx.work.WorkManager
import com.codex.androidtviptvplayer.data.db.AppDatabase
import com.codex.androidtviptvplayer.repository.PlaylistRepository
import com.codex.androidtviptvplayer.workmanager.PlaylistRefreshWorker
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.launch

class PlaylistApplication : Application(), Configuration.Provider {

    val applicationScope = CoroutineScope(SupervisorJob() + Dispatchers.IO)

    val database: AppDatabase by lazy {
        AppDatabase.getDatabase(this)
    }

    val repository: PlaylistRepository by lazy {
        PlaylistRepository(database.playlistDao(), database.channelDao(), this)
    }

    override fun onCreate() {
        super.onCreate()
        scheduleInitialRefresh()
    }

    private fun scheduleInitialRefresh() {
        applicationScope.launch {
            WorkManager.getInstance(this@PlaylistApplication)
                .enqueue(OneTimeWorkRequestBuilder<PlaylistRefreshWorker>().build())
        }
    }

    override fun getWorkManagerConfiguration(): Configuration =
        Configuration.Builder()
            .setMinimumLoggingLevel(android.util.Log.INFO)
            .build()
}
