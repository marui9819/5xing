package com.codex.androidtviptvplayer.workmanager

import android.app.NotificationChannel
import android.app.NotificationManager
import android.content.Context
import android.os.Build
import androidx.core.app.NotificationCompat
import androidx.core.app.NotificationManagerCompat
import androidx.work.CoroutineWorker
import androidx.work.ExistingPeriodicWorkPolicy
import androidx.work.PeriodicWorkRequestBuilder
import androidx.work.WorkManager
import androidx.work.WorkerParameters
import com.codex.androidtviptvplayer.R
import com.codex.androidtviptvplayer.data.model.Playlist
import com.codex.androidtviptvplayer.repository.PlaylistRepository
import com.codex.androidtviptvplayer.util.PreferencesHelper
import java.util.concurrent.TimeUnit

class PlaylistRefreshWorker(
    context: Context,
    workerParams: WorkerParameters
) : CoroutineWorker(context, workerParams) {

    private val repository: PlaylistRepository by lazy {
        val app = applicationContext as com.codex.androidtviptvplayer.PlaylistApplication
        app.repository
    }

    override suspend fun doWork(): Result {
        createNotificationChannel()
        val playlist = repository.getDefaultPlaylist()
        return if (playlist != null) {
            refresh(playlist)
        } else {
            Result.success()
        }
    }

    private suspend fun refresh(playlist: Playlist): Result {
        val result = repository.refreshPlaylist(playlist)
        return result.fold(
            onSuccess = { count ->
                notifyUser(
                    applicationContext.getString(R.string.message_refresh_success),
                    applicationContext.getString(R.string.message_refresh_success) + " ($count)"
                )
                Result.success()
            },
            onFailure = { error ->
                notifyUser(
                    applicationContext.getString(R.string.message_refresh_failed),
                    error.localizedMessage ?: "Unknown error"
                )
                Result.retry()
            }
        )
    }

    private fun notifyUser(title: String, message: String) {
        val notification = NotificationCompat.Builder(applicationContext, CHANNEL_ID)
            .setSmallIcon(R.drawable.ic_stat_name)
            .setContentTitle(title)
            .setContentText(message)
            .setPriority(NotificationCompat.PRIORITY_LOW)
            .build()
        NotificationManagerCompat.from(applicationContext).notify(NOTIFICATION_ID, notification)
    }

    private fun createNotificationChannel() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            val channel = NotificationChannel(
                CHANNEL_ID,
                "Playlist Updates",
                NotificationManager.IMPORTANCE_LOW
            )
            NotificationManagerCompat.from(applicationContext).createNotificationChannel(channel)
        }
    }

    companion object {
        private const val CHANNEL_ID = "playlist_refresh"
        private const val NOTIFICATION_ID = 1001
        private const val UNIQUE_WORK_NAME = "playlist_refresh_worker"

        fun schedule(context: Context) {
            val interval = PreferencesHelper(context).getRefreshInterval()
            val workRequest = PeriodicWorkRequestBuilder<PlaylistRefreshWorker>(
                interval, TimeUnit.HOURS
            ).build()
            WorkManager.getInstance(context).enqueueUniquePeriodicWork(
                UNIQUE_WORK_NAME,
                ExistingPeriodicWorkPolicy.UPDATE,
                workRequest
            )
        }
    }
}
