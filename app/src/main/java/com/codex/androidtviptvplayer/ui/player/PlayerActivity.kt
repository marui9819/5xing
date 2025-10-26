package com.codex.androidtviptvplayer.ui.player

import android.content.Context
import android.content.Intent
import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.viewModels
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Button
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Scaffold
import androidx.compose.material3.SnackbarHost
import androidx.compose.material3.SnackbarHostState
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.viewinterop.AndroidView
import androidx.lifecycle.lifecycleScope
import com.codex.androidtviptvplayer.PlaylistApplication
import com.codex.androidtviptvplayer.data.model.Channel
import com.codex.androidtviptvplayer.R
import com.codex.androidtviptvplayer.ui.theme.AndroidTVIptvPlayerTheme
import com.codex.androidtviptvplayer.viewmodel.PlayerViewModel
import kotlinx.coroutines.launch

class PlayerActivity : ComponentActivity() {

    private val playerViewModel: PlayerViewModel by viewModels {
        val app = application as PlaylistApplication
        PlayerViewModel.Factory(application, app.repository)
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        val channelId = intent.getLongExtra(EXTRA_CHANNEL_ID, -1L)
        if (channelId <= 0) {
            finish()
            return
        }

        val repository = (application as PlaylistApplication).repository
        lifecycleScope.launch {
            val channel = repository.getChannelById(channelId)
            if (channel == null) {
                finish()
            } else {
                playerViewModel.playChannel(channel)
            }
        }

        setContent {
            AndroidTVIptvPlayerTheme {
                PlayerScreen(playerViewModel)
            }
        }
    }

    companion object {
        private const val EXTRA_CHANNEL_ID = "channel_id"

        fun buildIntent(context: Context, channelId: Long): Intent =
            Intent(context, PlayerActivity::class.java).apply {
                putExtra(EXTRA_CHANNEL_ID, channelId)
            }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun PlayerScreen(viewModel: PlayerViewModel) {
    val currentChannel by viewModel.currentChannel.collectAsState()
    val playbackError by viewModel.playbackError.collectAsState()
    val snackbarHostState = remember { SnackbarHostState() }

    LaunchedEffect(playbackError) {
        playbackError?.let {
            snackbarHostState.showSnackbar(it)
            viewModel.clearError()
        }
    }

    Scaffold(
        topBar = { TopAppBar(title = { Text(text = currentChannel?.name ?: stringResource(id = R.string.title_player)) }) },
        snackbarHost = { SnackbarHost(hostState = snackbarHostState) }
    ) { paddingValues ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .background(MaterialTheme.colorScheme.background)
                .padding(paddingValues),
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.Top
        ) {
            PlayerViewContent(
                modifier = Modifier
                    .fillMaxWidth()
                    .weight(1f),
                viewModel = viewModel,
                channel = currentChannel
            )
            ChannelInfoFooter(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(24.dp),
                channel = currentChannel,
                onToggleFavorite = viewModel::toggleFavorite
            )
        }
    }
}

@Composable
private fun PlayerViewContent(
    modifier: Modifier = Modifier,
    viewModel: PlayerViewModel,
    channel: Channel?
) {
    if (channel == null) {
        Column(
            modifier = modifier
                .fillMaxSize()
                .background(MaterialTheme.colorScheme.surface),
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.Center
        ) {
            Text(text = stringResource(id = R.string.label_loading_channel), style = MaterialTheme.typography.titleMedium)
        }
    } else {
        AndroidView(
            modifier = modifier
                .fillMaxSize()
                .background(MaterialTheme.colorScheme.surface),
            factory = { context ->
                com.google.android.exoplayer2.ui.PlayerView(context).apply {
                    useController = true
                    player = viewModel.getPlayer()
                }
            }
        )
    }
}

@Composable
private fun ChannelInfoFooter(
    modifier: Modifier = Modifier,
    channel: Channel?,
    onToggleFavorite: () -> Unit
) {
    if (channel == null) {
        return
    }
    Column(
        modifier = modifier,
        verticalArrangement = Arrangement.spacedBy(8.dp)
    ) {
        Text(text = channel.name, style = MaterialTheme.typography.titleLarge)
        channel.groupTitle?.let {
            Text(text = it, style = MaterialTheme.typography.bodyMedium)
        }
        Button(onClick = onToggleFavorite) {
            Text(
                text = if (channel.isFavorite) {
                    stringResource(id = R.string.action_remove_favorite)
                } else {
                    stringResource(id = R.string.action_add_favorite)
                }
            )
        }
    }
}
