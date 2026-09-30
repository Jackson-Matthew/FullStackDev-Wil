package com.example.blastpromobile.ui.settings

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.material3.Switch
import androidx.compose.material3.SwitchDefaults
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.tooling.preview.Preview
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.blastpromobile.Config
import com.example.blastpromobile.ui.components.BlastProScreen
import com.example.blastpromobile.ui.components.FieldLabel
import com.example.blastpromobile.ui.components.ScreenHeading
import com.example.blastpromobile.ui.components.WhiteTextField
import com.example.blastpromobile.ui.theme.BlastProMobileTheme
import com.example.blastpromobile.ui.theme.BlastRed

/** UI only: the API address is shown for reference and the switch does nothing yet. */
@Composable
fun SettingsScreen(onMenuClick: () -> Unit, onBack: () -> Unit) {
    var aiEnabled by remember { mutableStateOf(true) }

    BlastProScreen(onMenuClick = onMenuClick, onBackClick = onBack) {
        ScreenHeading("Settings", "App information.")

        FieldLabel("API address")
        WhiteTextField(Config.DEV_API_BASE_URL, {}, readOnly = true)

        FieldLabel("Chat")
        Row(
            Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            Text("AI chat enabled", color = Color.White, fontSize = 16.sp)
            Switch(
                checked = aiEnabled,
                onCheckedChange = { aiEnabled = it },
                colors = SwitchDefaults.colors(checkedTrackColor = BlastRed)
            )
        }

        FieldLabel("About")
        Text("BlastPro Mobile 1.0", color = Color.White, fontSize = 14.sp)
    }
}

@Preview(showBackground = true, widthDp = 360, heightDp = 720)
@Composable
private fun SettingsPreview() {
    BlastProMobileTheme { SettingsScreen(onMenuClick = {}, onBack = {}) }
}
