package com.codex.androidtviptvplayer.viewmodel

import android.app.Application
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.ViewModelProvider
import androidx.lifecycle.viewmodel.CreationExtras
import androidx.lifecycle.viewModelScope
import com.codex.androidtviptvplayer.data.model.Channel
import com.codex.androidtviptvplayer.repository.PlaylistRepository
import com.codex.androidtviptvplayer.util.PlayerHelper
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch

class PlayerViewModel(
    application: Application,
    private val repository: PlaylistRepository
) : AndroidViewModel(application) {

    private val playerHelper = PlayerHelper(application)
    private val _currentChannel = MutableStateFlow<Channel?>(null)
    val currentChannel: StateFlow<Channel?> = _currentChannel

    private val _playbackError = MutableStateFlow<String?>(null)
    val playbackError: StateFlow<String?> = _playbackError

    fun playChannel(channel: Channel) {
        _currentChannel.value = channel
        viewModelScope.launch {
            runCatching {
                playerHelper.playChannel(channel.streamUrl)
                repository.updateLastPlayed(channel)
            }.onFailure { error ->
                _playbackError.value = error.localizedMessage
            }
        }
    }

    fun clearError() {
        _playbackError.value = null
    }

    fun getPlayer() = playerHelper.player()

    fun toggleFavorite() {
        val channel = _currentChannel.value ?: return
        viewModelScope.launch {
            repository.toggleFavorite(channel)
            _currentChannel.value = channel.copy(isFavorite = !channel.isFavorite)
        }
    }

    override fun onCleared() {
        super.onCleared()
        playerHelper.release()
    }

    class Factory(
        private val application: Application,
        private val repository: PlaylistRepository
    ) : ViewModelProvider.AndroidViewModelFactory(application) {
        @Suppress("UNCHECKED_CAST")
        override fun <T : androidx.lifecycle.ViewModel> create(
            modelClass: Class<T>,
            extras: CreationExtras
        ): T {
            if (modelClass.isAssignableFrom(PlayerViewModel::class.java)) {
                return PlayerViewModel(application, repository) as T
            }
            return super.create(modelClass, extras)
        }
    }
}
