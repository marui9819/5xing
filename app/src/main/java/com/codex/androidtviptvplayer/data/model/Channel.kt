package com.codex.androidtviptvplayer.data.model

import androidx.room.ColumnInfo
import androidx.room.Entity
import androidx.room.ForeignKey
import androidx.room.Index
import androidx.room.PrimaryKey

@Entity(
    tableName = "channels",
    foreignKeys = [
        ForeignKey(
            entity = Playlist::class,
            parentColumns = ["id"],
            childColumns = ["playlist_id"],
            onDelete = ForeignKey.CASCADE
        )
    ],
    indices = [Index("playlist_id"), Index("is_favorite")]
)
data class Channel(
    @PrimaryKey(autoGenerate = true) val id: Long = 0,
    @ColumnInfo(name = "playlist_id") val playlistId: Long,
    val name: String,
    val groupTitle: String?,
    val logoUrl: String?,
    val streamUrl: String,
    @ColumnInfo(name = "is_favorite") val isFavorite: Boolean = false,
    val lastPlayed: Long? = null
)
