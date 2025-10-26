package com.codex.androidtviptvplayer.ui.playlist

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.viewModels
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Button
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.material3.TextFieldDefaults
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.input.KeyboardCapitalization
import androidx.compose.ui.text.input.KeyboardOptions
import androidx.compose.ui.unit.dp
import androidx.compose.ui.res.stringResource
import com.codex.androidtviptvplayer.PlaylistApplication
import com.codex.androidtviptvplayer.R
import com.codex.androidtviptvplayer.ui.theme.AndroidTVIptvPlayerTheme
import com.codex.androidtviptvplayer.viewmodel.PlaylistViewModel

class PlaylistImportActivity : ComponentActivity() {

    private val playlistViewModel: PlaylistViewModel by viewModels {
        val app = application as PlaylistApplication
        PlaylistViewModel.Factory(app.repository)
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContent {
            AndroidTVIptvPlayerTheme {
                ImportPlaylistScreen(
                    onImport = { name, url ->
                        playlistViewModel.importPlaylist(name, url)
                        finish()
                    }
                )
            }
        }
    }
}

@Composable
private fun ImportPlaylistScreen(onImport: (String, String) -> Unit) {
    var name by remember { mutableStateOf("") }
    var url by remember { mutableStateOf("") }

    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(24.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp)
    ) {
        Text(text = stringResource(id = R.string.title_import_playlist), style = MaterialTheme.typography.titleLarge)
        OutlinedTextField(
            value = name,
            onValueChange = { name = it },
            label = { Text(stringResource(id = R.string.hint_playlist_name)) },
            singleLine = true,
            colors = TextFieldDefaults.outlinedTextFieldColors(),
            keyboardOptions = KeyboardOptions(capitalization = KeyboardCapitalization.Words)
        )
        OutlinedTextField(
            value = url,
            onValueChange = { url = it },
            label = { Text(stringResource(id = R.string.hint_playlist_url)) },
            singleLine = true,
            colors = TextFieldDefaults.outlinedTextFieldColors()
        )
        Button(onClick = { if (name.isNotBlank() && url.isNotBlank()) onImport(name.trim(), url.trim()) }) {
            Text(text = stringResource(id = R.string.action_import))
        }
    }
}
