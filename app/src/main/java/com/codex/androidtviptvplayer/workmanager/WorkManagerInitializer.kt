package com.codex.androidtviptvplayer.workmanager

import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import android.content.IntentFilter
import android.os.Build
import androidx.startup.Initializer

class WorkManagerInitializer : Initializer<Unit> {
    override fun create(context: Context) {
        PlaylistRefreshWorker.schedule(context)
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            val receiver = object : BroadcastReceiver() {
                override fun onReceive(context: Context?, intent: Intent?) {
                    if (intent?.action == Intent.ACTION_BOOT_COMPLETED) {
                        context?.let { PlaylistRefreshWorker.schedule(it) }
                    }
                }
            }
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
                context.registerReceiver(
                    receiver,
                    IntentFilter(Intent.ACTION_BOOT_COMPLETED),
                    Context.RECEIVER_NOT_EXPORTED
                )
            } else {
                @Suppress("DEPRECATION")
                context.registerReceiver(receiver, IntentFilter(Intent.ACTION_BOOT_COMPLETED))
            }
        }
    }

    override fun dependencies(): List<Class<out Initializer<*>>> = emptyList()
}
