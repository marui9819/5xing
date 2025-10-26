package com.codex.androidtviptvplayer.util

import android.content.Context
import androidx.core.content.edit

class PreferencesHelper(context: Context) {
    private val prefs = context.getSharedPreferences(Constants.PREFS_FILE, Context.MODE_PRIVATE)

    fun setDefaultPlaylistId(id: Long) {
        prefs.edit { putLong(Constants.KEY_DEFAULT_PLAYLIST_ID, id) }
    }

    fun getDefaultPlaylistId(): Long? {
        val stored = prefs.getLong(Constants.KEY_DEFAULT_PLAYLIST_ID, -1L)
        return if (stored >= 0) stored else null
    }

    fun setRefreshInterval(hours: Long) {
        prefs.edit { putLong(Constants.KEY_REFRESH_INTERVAL, hours) }
    }

    fun getRefreshInterval(): Long = prefs.getLong(Constants.KEY_REFRESH_INTERVAL, Constants.DEFAULT_REFRESH_HOURS)

    fun setLastChannelId(channelId: Long) {
        prefs.edit { putLong(Constants.KEY_LAST_CHANNEL_ID, channelId) }
    }

    fun getLastChannelId(): Long? {
        val stored = prefs.getLong(Constants.KEY_LAST_CHANNEL_ID, -1L)
        return if (stored >= 0) stored else null
    }
}
