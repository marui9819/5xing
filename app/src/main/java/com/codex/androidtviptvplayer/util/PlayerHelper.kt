package com.codex.androidtviptvplayer.util

import android.content.Context
import com.google.android.exoplayer2.ExoPlayer
import com.google.android.exoplayer2.MediaItem
import kotlinx.coroutines.delay

class PlayerHelper(context: Context) {
    private val exoPlayer: ExoPlayer = ExoPlayer.Builder(context).build()

    fun player(): ExoPlayer = exoPlayer

    suspend fun playChannel(url: String, retryCount: Int = 3) {
        repeat(retryCount) { attempt ->
            try {
                val item = MediaItem.fromUri(url)
                exoPlayer.setMediaItem(item)
                exoPlayer.prepare()
                exoPlayer.playWhenReady = true
                return
            } catch (ex: Exception) {
                if (attempt == retryCount - 1) throw ex
                delay(1500)
            }
        }
    }

    fun release() {
        exoPlayer.release()
    }
}
