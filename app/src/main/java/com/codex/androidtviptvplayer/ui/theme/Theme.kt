package com.codex.androidtviptvplayer.ui.theme

import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color

private val DarkColors = darkColorScheme(
    primary = PrimaryDark,
    onPrimary = Color.White,
    secondary = SecondaryDark,
    background = BackgroundDark,
    surface = SurfaceDark,
    onSurface = OnSurfaceDark
)

private val LightColors = lightColorScheme(
    primary = PrimaryDark,
    onPrimary = Color.White,
    secondary = SecondaryDark,
    background = Color.White,
    surface = Color(0xFFF0F4FA),
    onSurface = Color(0xFF111418)
)

@Composable
fun AndroidTVIptvPlayerTheme(
    useDarkTheme: Boolean = isSystemInDarkTheme(),
    content: @Composable () -> Unit
) {
    val colors = if (useDarkTheme) DarkColors else LightColors

    MaterialTheme(
        colorScheme = colors,
        typography = Typography,
        content = content
    )
}
