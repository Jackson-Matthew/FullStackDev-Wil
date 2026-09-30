package com.example.blastpromobile.ui.theme

import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color

private val BlastProColorScheme = darkColorScheme(
    primary = BlastRed,
    onPrimary = Color.White,
    background = ScreenBackground,
    onBackground = Color.White,
    surface = TopBarBackground,
    onSurface = Color.White
)

@Composable
fun BlastProMobileTheme(content: @Composable () -> Unit) {
    MaterialTheme(
        colorScheme = BlastProColorScheme,
        typography = Typography,
        content = content
    )
}
