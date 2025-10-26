package com.codex.androidtviptvplayer.ui.main

import android.content.Intent
import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.viewModels
import androidx.compose.foundation.background
import androidx.compose.foundation.focusable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.grid.GridCells
import androidx.compose.foundation.lazy.grid.LazyVerticalGrid
import androidx.compose.foundation.lazy.grid.items
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Scaffold
import androidx.compose.material3.SnackbarHost
import androidx.compose.material3.SnackbarHostState
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.focus.FocusRequester
import androidx.compose.ui.focus.focusRequester
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.unit.dp
import com.codex.androidtviptvplayer.PlaylistApplication
import com.codex.androidtviptvplayer.data.model.Channel
import com.codex.androidtviptvplayer.data.model.Playlist
import com.codex.androidtviptvplayer.R
import com.codex.androidtviptvplayer.ui.player.PlayerActivity
import com.codex.androidtviptvplayer.ui.playlist.PlaylistImportActivity
import com.codex.androidtviptvplayer.ui.search.SearchActivity
import com.codex.androidtviptvplayer.ui.settings.SettingsActivity
import com.codex.androidtviptvplayer.ui.theme.AndroidTVIptvPlayerTheme
import com.codex.androidtviptvplayer.viewmodel.PlaylistViewModel
import kotlinx.coroutines.delay

class MainActivity : ComponentActivity() {

    private val playlistViewModel: PlaylistViewModel by viewModels {
        val app = application as PlaylistApplication
        PlaylistViewModel.Factory(app.repository)
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContent {
            AndroidTVIptvPlayerTheme {
                MainScreen(playlistViewModel)
            }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun MainScreen(viewModel: PlaylistViewModel) {
    val playlists by viewModel.playlists.collectAsState()
    val channels by viewModel.channels.collectAsState()
    val isRefreshing by viewModel.isRefreshing.collectAsState()
    val errorMessage by viewModel.errorMessage.collectAsState()
    val selectedPlaylistId by viewModel.selectedPlaylistId.collectAsState()

    val snackbarHostState = remember { SnackbarHostState() }
    val context = LocalContext.current

    LaunchedEffect(playlists, selectedPlaylistId) {
        if (playlists.isNotEmpty() && selectedPlaylistId == null) {
            val default = playlists.firstOrNull { it.isDefault } ?: playlists.first()
            viewModel.selectPlaylist(default.id)
        }
    }

    LaunchedEffect(errorMessage) {
        errorMessage?.let { message ->
            snackbarHostState.showSnackbar(message)
            viewModel.dismissError()
        }
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text(text = stringResource(id = R.string.app_name)) },
                actions = {
                    TextButton(onClick = {
                        context.startActivity(Intent(context, PlaylistImportActivity::class.java))
                    }) {
                        Text(text = stringResource(id = R.string.action_import_playlist))
                    }
                    TextButton(onClick = {
                        context.startActivity(Intent(context, SearchActivity::class.java))
                    }) {
                        Text(text = stringResource(id = R.string.action_open_search))
                    }
                    TextButton(onClick = {
                        context.startActivity(Intent(context, SettingsActivity::class.java))
                    }) {
                        Text(text = stringResource(id = R.string.action_open_settings))
                    }
                }
            )
        },
        snackbarHost = { SnackbarHost(snackbarHostState) }
    ) { paddingValues ->
        if (playlists.isEmpty()) {
            EmptyState(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(paddingValues)
            )
        } else {
            Column(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(paddingValues)
                    .background(
                        Brush.verticalGradient(
                            colors = listOf(
                                MaterialTheme.colorScheme.surface,
                                MaterialTheme.colorScheme.background
                            )
                        )
                    )
            ) {
                PlaylistSelector(
                    playlists = playlists,
                    onPlaylistSelected = viewModel::selectPlaylist,
                    onRefreshRequested = { viewModel.refreshSelectedPlaylist() },
                    onSetDefault = { playlistId ->
                        viewModel.setDefaultPlaylist(playlistId)
                        viewModel.selectPlaylist(playlistId)
                    },
                    isRefreshing = isRefreshing
                )
                ChannelGrid(
                    modifier = Modifier.weight(1f),
                    channels = channels,
                    onChannelSelected = { channel ->
                        val intent = PlayerActivity.buildIntent(context, channel.id)
                        context.startActivity(intent)
                    }
                )
            }
        }
    }
}

@Composable
private fun EmptyState(modifier: Modifier = Modifier) {
    Column(
        modifier = modifier,
        verticalArrangement = Arrangement.Center,
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        Text(text = stringResource(id = R.string.message_no_playlists), style = MaterialTheme.typography.titleLarge)
        Text(text = stringResource(id = R.string.message_import_playlist), style = MaterialTheme.typography.bodyMedium)
    }
}

@Composable
private fun PlaylistSelector(
    playlists: List<Playlist>,
    onPlaylistSelected: (Long) -> Unit,
    onRefreshRequested: () -> Unit,
    onSetDefault: (Long) -> Unit,
    isRefreshing: Boolean
) {
    val focusRequester = remember { FocusRequester() }

    LaunchedEffect(Unit) {
        delay(500)
        focusRequester.requestFocus()
    }

    Row(
        modifier = Modifier
            .fillMaxWidth()
            .padding(horizontal = 24.dp, vertical = 12.dp),
        horizontalArrangement = Arrangement.spacedBy(12.dp),
        verticalAlignment = Alignment.CenterVertically
    ) {
        playlists.forEachIndexed { index, playlist ->
            Card(
                modifier = Modifier
                    .then(
                        if (index == 0) Modifier.focusRequester(focusRequester) else Modifier
                    )
                    .focusable(),
                colors = CardDefaults.cardColors(
                    containerColor = if (playlist.isDefault) MaterialTheme.colorScheme.primary else MaterialTheme.colorScheme.surface
                ),
                onClick = { onPlaylistSelected(playlist.id) }
            ) {
                Column(modifier = Modifier.padding(16.dp)) {
                    Text(text = playlist.name, style = MaterialTheme.typography.titleMedium)
                    Text(text = playlist.sourceUrl, style = MaterialTheme.typography.bodySmall)
                    if (playlist.isDefault) {
                        Text(text = stringResource(id = R.string.label_default), style = MaterialTheme.typography.bodySmall)
                    } else {
                        Button(onClick = { onSetDefault(playlist.id) }) {
                            Text(text = stringResource(id = R.string.action_set_default))
                        }
                    }
                }
            }
        }

        Button(onClick = onRefreshRequested, enabled = !isRefreshing) {
            Text(
                text = if (isRefreshing) {
                    stringResource(id = R.string.action_refreshing)
                } else {
                    stringResource(id = R.string.action_refresh)
                }
            )
        }
    }
}

@Composable
private fun ChannelGrid(
    modifier: Modifier = Modifier,
    channels: List<Channel>,
    onChannelSelected: (Channel) -> Unit
) {
    if (channels.isEmpty()) {
        Box(modifier = modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
            Text(text = stringResource(id = R.string.message_empty_channels), style = MaterialTheme.typography.bodyLarge)
        }
        return
    }

    LazyVerticalGrid(
        modifier = modifier
            .fillMaxSize()
            .padding(horizontal = 32.dp),
        columns = GridCells.Adaptive(280.dp),
        horizontalArrangement = Arrangement.spacedBy(16.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp)
    ) {
        items(channels, key = { it.id }) { channel ->
            ChannelCard(channel = channel, onClick = { onChannelSelected(channel) })
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun ChannelCard(channel: Channel, onClick: () -> Unit) {
    Card(
        modifier = Modifier.focusable(),
        colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surfaceVariant),
        onClick = onClick
    ) {
        Column(modifier = Modifier.padding(16.dp)) {
            Text(text = channel.name, style = MaterialTheme.typography.titleMedium)
            channel.groupTitle?.let {
                Text(text = it, style = MaterialTheme.typography.bodyMedium, color = Color.Gray)
            }
            Text(
                text = channel.streamUrl,
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.6f)
            )
        }
    }
}
