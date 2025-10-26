package com.codex.androidtviptvplayer.viewmodel

import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import androidx.lifecycle.viewModelScope
import com.codex.androidtviptvplayer.data.model.Channel
import com.codex.androidtviptvplayer.repository.PlaylistRepository
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.SharingStarted
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.flatMapLatest
import kotlinx.coroutines.flow.stateIn
import kotlinx.coroutines.launch

class SearchViewModel(private val repository: PlaylistRepository) : ViewModel() {

    private val playlistId = MutableStateFlow<Long?>(null)
    private val query = MutableStateFlow("")

    val channels: StateFlow<List<Channel>> = playlistId
        .flatMapLatest { id ->
            if (id == null) {
                MutableStateFlow(emptyList<Channel>())
            } else {
                query.flatMapLatest { search ->
                    if (search.isBlank()) {
                        repository.observeChannels(id)
                    } else {
                        repository.searchChannels(id, search)
                    }
                }
            }
        }
        .stateIn(viewModelScope, SharingStarted.WhileSubscribed(5_000), emptyList())

    init {
        viewModelScope.launch {
            repository.getDefaultPlaylist()?.let {
                playlistId.value = it.id
            }
        }
    }

    fun updateQuery(value: String) {
        query.value = value
    }

    class Factory(private val repository: PlaylistRepository) : ViewModelProvider.Factory {
        @Suppress("UNCHECKED_CAST")
        override fun <T : ViewModel> create(modelClass: Class<T>): T {
            if (modelClass.isAssignableFrom(SearchViewModel::class.java)) {
                return SearchViewModel(repository) as T
            }
            throw IllegalArgumentException("Unknown ViewModel class: ${modelClass.name}")
        }
    }
}
