package com.codex.androidtviptvplayer.data.db

import androidx.room.Dao
import androidx.room.Insert
import androidx.room.OnConflictStrategy
import androidx.room.Query
import androidx.room.Update
import com.codex.androidtviptvplayer.data.model.Channel
import kotlinx.coroutines.flow.Flow

@Dao
interface ChannelDao {
    @Query("SELECT * FROM channels WHERE playlist_id = :playlistId ORDER BY name")
    fun observeChannels(playlistId: Long): Flow<List<Channel>>

    @Query("SELECT * FROM channels WHERE playlist_id = :playlistId AND is_favorite = 1 ORDER BY name")
    fun observeFavorites(playlistId: Long): Flow<List<Channel>>

    @Query(
        "SELECT * FROM channels WHERE playlist_id = :playlistId AND name LIKE '%' || :query || '%' ORDER BY name"
    )
    fun searchChannels(playlistId: Long, query: String): Flow<List<Channel>>

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun upsertChannels(channels: List<Channel>)

    @Query("DELETE FROM channels WHERE playlist_id = :playlistId")
    suspend fun deleteChannelsForPlaylist(playlistId: Long)

    @Update
    suspend fun updateChannel(channel: Channel)

    @Query("UPDATE channels SET is_favorite = :favorite WHERE id = :channelId")
    suspend fun setFavorite(channelId: Long, favorite: Boolean)

    @Query("SELECT * FROM channels WHERE id = :channelId LIMIT 1")
    suspend fun getChannelById(channelId: Long): Channel?
}
