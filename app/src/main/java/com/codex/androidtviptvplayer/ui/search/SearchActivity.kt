package com.codex.androidtviptvplayer.ui.search

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.viewModels
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
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
import androidx.compose.ui.text.input.KeyboardCapitalization
import androidx.compose.ui.text.input.KeyboardOptions
import androidx.compose.ui.unit.dp
import com.codex.androidtviptvplayer.PlaylistApplication
import com.codex.androidtviptvplayer.data.model.Channel
import androidx.compose.ui.res.stringResource
import com.codex.androidtviptvplayer.R
import com.codex.androidtviptvplayer.ui.theme.AndroidTVIptvPlayerTheme
import com.codex.androidtviptvplayer.viewmodel.SearchViewModel

class SearchActivity : ComponentActivity() {

    private val searchViewModel: SearchViewModel by viewModels {
        val app = application as PlaylistApplication
        SearchViewModel.Factory(app.repository)
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContent {
            AndroidTVIptvPlayerTheme {
                SearchScreen(searchViewModel)
            }
        }
    }
}

@Composable
private fun SearchScreen(viewModel: SearchViewModel) {
    val channels by viewModel.channels.collectAsState()
    var query by remember { mutableStateOf("") }

    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(24.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp)
    ) {
        Text(text = stringResource(id = R.string.title_search_screen), style = MaterialTheme.typography.titleLarge)
        OutlinedTextField(
            modifier = Modifier.fillMaxWidth(),
            value = query,
            onValueChange = {
                query = it
                viewModel.updateQuery(it)
            },
            label = { Text(stringResource(id = R.string.hint_search_channel)) },
            singleLine = true,
            keyboardOptions = KeyboardOptions(capitalization = KeyboardCapitalization.Words),
            colors = TextFieldDefaults.outlinedTextFieldColors()
        )
        LazyColumn(
            modifier = Modifier.fillMaxSize(),
            verticalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            items(channels, key = { it.id }) { channel ->
                SearchResultItem(channel)
            }
        }
    }
}

@Composable
private fun SearchResultItem(channel: Channel) {
    Column(modifier = Modifier.fillMaxWidth()) {
        Text(text = channel.name, style = MaterialTheme.typography.titleMedium)
        channel.groupTitle?.let {
            Text(text = it, style = MaterialTheme.typography.bodyMedium)
        }
        Text(
            text = channel.streamUrl,
            style = MaterialTheme.typography.bodySmall,
            color = MaterialTheme.colorScheme.onSurface.copy(alpha = 0.6f)
        )
    }
}
