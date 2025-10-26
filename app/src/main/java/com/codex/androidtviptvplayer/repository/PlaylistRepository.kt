package com.codex.androidtviptvplayer.repository

import android.content.Context
import com.codex.androidtviptvplayer.data.db.ChannelDao
import com.codex.androidtviptvplayer.data.db.PlaylistDao
import com.codex.androidtviptvplayer.data.model.Channel
import com.codex.androidtviptvplayer.data.model.Playlist
import com.codex.androidtviptvplayer.network.RetrofitClient
import com.codex.androidtviptvplayer.util.M3UParser
import com.codex.androidtviptvplayer.util.PreferencesHelper
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.withContext
import okhttp3.ResponseBody
import java.io.BufferedReader
import java.io.InputStreamReader

class PlaylistRepository(
    private val playlistDao: PlaylistDao,
    private val channelDao: ChannelDao,
    context: Context
) {
    private val preferences = PreferencesHelper(context)
    private val apiService = RetrofitClient.create(context)

    fun observePlaylists(): Flow<List<Playlist>> = playlistDao.observePlaylists()

    fun observeChannels(playlistId: Long): Flow<List<Channel>> = channelDao.observeChannels(playlistId)

    fun observeFavorites(playlistId: Long): Flow<List<Channel>> = channelDao.observeFavorites(playlistId)

    fun searchChannels(playlistId: Long, query: String): Flow<List<Channel>> =
        channelDao.searchChannels(playlistId, query)

    suspend fun getChannelById(channelId: Long): Channel? = withContext(Dispatchers.IO) {
        channelDao.getChannelById(channelId)
    }

    suspend fun upsertPlaylist(playlist: Playlist): Long = withContext(Dispatchers.IO) {
        playlistDao.upsertPlaylist(playlist)
    }

    suspend fun deletePlaylist(id: Long) = withContext(Dispatchers.IO) {
        channelDao.deleteChannelsForPlaylist(id)
        playlistDao.deletePlaylist(id)
    }

    suspend fun setDefaultPlaylist(playlistId: Long) = withContext(Dispatchers.IO) {
        playlistDao.setDefaultPlaylist(playlistId)
        preferences.setDefaultPlaylistId(playlistId)
    }

    suspend fun getDefaultPlaylist(): Playlist? = withContext(Dispatchers.IO) {
        val defaultId = preferences.getDefaultPlaylistId()
        if (defaultId != null) {
            playlistDao.getPlaylistById(defaultId)
        } else {
            playlistDao.observePlaylists().first().firstOrNull { it.isDefault }
        }
    }

    suspend fun refreshPlaylist(playlist: Playlist): Result<Int> = withContext(Dispatchers.IO) {
        runCatching {
            val response = apiService.downloadPlaylist(playlist.sourceUrl)
            val parsedChannels = parseResponse(playlist.id, response)
            channelDao.deleteChannelsForPlaylist(playlist.id)
            channelDao.upsertChannels(parsedChannels)
            playlistDao.updatePlaylist(
                playlist.copy(lastRefreshed = System.currentTimeMillis())
            )
            parsedChannels.size
        }
    }

    suspend fun toggleFavorite(channel: Channel) = withContext(Dispatchers.IO) {
        channelDao.setFavorite(channel.id, !channel.isFavorite)
    }

    suspend fun updateLastPlayed(channel: Channel) = withContext(Dispatchers.IO) {
        channelDao.updateChannel(channel.copy(lastPlayed = System.currentTimeMillis()))
    }

    private fun parseResponse(playlistId: Long, responseBody: ResponseBody): List<Channel> {
        val reader = BufferedReader(InputStreamReader(responseBody.byteStream()))
        return M3UParser.parse(reader).map {
            Channel(
                playlistId = playlistId,
                name = it.name,
                groupTitle = it.group,
                logoUrl = it.logo,
                streamUrl = it.url,
                isFavorite = false
            )
        }
    }
}
