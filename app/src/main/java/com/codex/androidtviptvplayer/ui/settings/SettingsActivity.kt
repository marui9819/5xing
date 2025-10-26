package com.codex.androidtviptvplayer.ui.settings

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
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.input.KeyboardOptions
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import com.codex.androidtviptvplayer.PlaylistApplication
import androidx.compose.ui.res.stringResource
import com.codex.androidtviptvplayer.R
import com.codex.androidtviptvplayer.ui.theme.AndroidTVIptvPlayerTheme
import com.codex.androidtviptvplayer.util.PreferencesHelper
import com.codex.androidtviptvplayer.viewmodel.SettingsViewModel

class SettingsActivity : ComponentActivity() {

    private val settingsViewModel: SettingsViewModel by viewModels {
        val app = application as PlaylistApplication
        SettingsViewModel.Factory(app.repository, PreferencesHelper(this))
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContent {
            AndroidTVIptvPlayerTheme {
                SettingsScreen(settingsViewModel)
            }
        }
    }
}

@Composable
private fun SettingsScreen(viewModel: SettingsViewModel) {
    val refreshInterval by viewModel.refreshInterval.collectAsState()
    var intervalInput by remember(refreshInterval) { mutableStateOf(refreshInterval.toString()) }

    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(24.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp)
    ) {
        Text(text = stringResource(id = R.string.title_settings), style = MaterialTheme.typography.titleLarge)
        OutlinedTextField(
            value = intervalInput,
            onValueChange = { intervalInput = it.filter { ch -> ch.isDigit() } },
            label = { Text(stringResource(id = R.string.label_refresh_interval)) },
            singleLine = true,
            keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number),
            colors = TextFieldDefaults.outlinedTextFieldColors()
        )
        Button(onClick = {
            intervalInput.toLongOrNull()?.let { hours ->
                viewModel.setRefreshInterval(hours)
            }
        }) {
            Text(stringResource(id = R.string.action_save))
        }
    }
}
