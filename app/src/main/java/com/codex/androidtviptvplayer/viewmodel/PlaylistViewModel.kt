package com.codex.androidtviptvplayer.viewmodel

import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import androidx.lifecycle.viewModelScope
import com.codex.androidtviptvplayer.data.model.Channel
import com.codex.androidtviptvplayer.data.model.Playlist
import com.codex.androidtviptvplayer.repository.PlaylistRepository
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.SharingStarted
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.flatMapLatest
import kotlinx.coroutines.flow.stateIn
import kotlinx.coroutines.launch

class PlaylistViewModel(private val repository: PlaylistRepository) : ViewModel() {

    val playlists: StateFlow<List<Playlist>> = repository.observePlaylists()
        .stateIn(viewModelScope, SharingStarted.WhileSubscribed(5_000), emptyList())

    private val _selectedPlaylistId = MutableStateFlow<Long?>(null)
    val selectedPlaylistId: StateFlow<Long?> = _selectedPlaylistId.asStateFlow()

    val channels: StateFlow<List<Channel>> = _selectedPlaylistId.flatMapLatest { playlistId ->
        playlistId?.let { repository.observeChannels(it) } ?: MutableStateFlow(emptyList<Channel>())
    }.stateIn(viewModelScope, SharingStarted.WhileSubscribed(5_000), emptyList())

    val favorites: StateFlow<List<Channel>> = _selectedPlaylistId.flatMapLatest { playlistId ->
        playlistId?.let { repository.observeFavorites(it) } ?: MutableStateFlow(emptyList<Channel>())
    }.stateIn(viewModelScope, SharingStarted.WhileSubscribed(5_000), emptyList())

    private val _isRefreshing = MutableStateFlow(false)
    val isRefreshing: StateFlow<Boolean> = _isRefreshing.asStateFlow()

    private val _errorMessage = MutableStateFlow<String?>(null)
    val errorMessage: StateFlow<String?> = _errorMessage.asStateFlow()

    fun selectPlaylist(playlistId: Long) {
        _selectedPlaylistId.value = playlistId
    }

    fun refreshSelectedPlaylist() {
        val playlistId = selectedPlaylistId.value ?: return
        viewModelScope.launch {
            _isRefreshing.value = true
            val playlist = playlists.value.firstOrNull { it.id == playlistId }
            if (playlist != null) {
                val result = repository.refreshPlaylist(playlist)
                result.exceptionOrNull()?.let { ex ->
                    _errorMessage.value = ex.localizedMessage
                }
            }
            _isRefreshing.value = false
        }
    }

    fun importPlaylist(name: String, url: String) {
        viewModelScope.launch {
            val playlist = Playlist(name = name, sourceUrl = url)
            val id = repository.upsertPlaylist(playlist)
            if (playlists.value.isEmpty()) {
                repository.setDefaultPlaylist(id)
                selectPlaylist(id)
            }
        }
    }

    fun setDefaultPlaylist(playlistId: Long) {
        viewModelScope.launch {
            repository.setDefaultPlaylist(playlistId)
        }
    }

    fun dismissError() {
        _errorMessage.value = null
    }

    class Factory(private val repository: PlaylistRepository) : ViewModelProvider.Factory {
        @Suppress("UNCHECKED_CAST")
        override fun <T : ViewModel> create(modelClass: Class<T>): T {
            if (modelClass.isAssignableFrom(PlaylistViewModel::class.java)) {
                return PlaylistViewModel(repository) as T
            }
            throw IllegalArgumentException("Unknown ViewModel class: ${modelClass.name}")
        }
    }
}
