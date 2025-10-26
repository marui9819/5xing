package com.codex.androidtviptvplayer.viewmodel

import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import androidx.lifecycle.viewModelScope
import com.codex.androidtviptvplayer.repository.PlaylistRepository
import com.codex.androidtviptvplayer.util.PreferencesHelper
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch

class SettingsViewModel(
    private val repository: PlaylistRepository,
    private val preferencesHelper: PreferencesHelper
) : ViewModel() {

    private val _refreshInterval = MutableStateFlow(preferencesHelper.getRefreshInterval())
    val refreshInterval: StateFlow<Long> = _refreshInterval

    fun setRefreshInterval(hours: Long) {
        val normalized = hours.coerceAtLeast(1)
        preferencesHelper.setRefreshInterval(normalized)
        _refreshInterval.value = normalized
    }

    fun setDefaultPlaylist(playlistId: Long) {
        viewModelScope.launch {
            repository.setDefaultPlaylist(playlistId)
        }
    }

    class Factory(
        private val repository: PlaylistRepository,
        private val preferencesHelper: PreferencesHelper
    ) : ViewModelProvider.Factory {
        @Suppress("UNCHECKED_CAST")
        override fun <T : ViewModel> create(modelClass: Class<T>): T {
            if (modelClass.isAssignableFrom(SettingsViewModel::class.java)) {
                return SettingsViewModel(repository, preferencesHelper) as T
            }
            throw IllegalArgumentException("Unknown ViewModel class: ${modelClass.name}")
        }
    }
}
