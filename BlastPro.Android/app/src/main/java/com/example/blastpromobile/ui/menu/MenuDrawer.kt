package com.example.blastpromobile.ui.menu

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.statusBarsPadding
import androidx.compose.foundation.layout.navigationBarsPadding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.Logout
import androidx.compose.material.icons.filled.AddCircleOutline
import androidx.compose.material.icons.filled.Close
import androidx.compose.material.icons.filled.Description
import androidx.compose.material.icons.filled.Forum
import androidx.compose.material.icons.filled.Settings
import androidx.compose.material.icons.filled.Sync
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.tooling.preview.Preview
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.blastpromobile.Config
import com.example.blastpromobile.navigation.Routes
import com.example.blastpromobile.ui.components.BlastProLogo
import com.example.blastpromobile.ui.components.ContourBackground
import com.example.blastpromobile.ui.theme.BlastProMobileTheme
import com.example.blastpromobile.ui.theme.BlastRed
import com.example.blastpromobile.ui.theme.InkDark

/** Content of the slide-out menu opened from the hamburger icon. Highlights the screen the user is on. */
@Composable
fun MenuDrawerContent(
    currentRoute: String?,
    onClose: () -> Unit,
    onNewNote: () -> Unit,
    onNotes: () -> Unit,
    onChat: () -> Unit,
    onSettings: () -> Unit,
    onSync: () -> Unit,
    onLogout: () -> Unit
) {
    ContourBackground {
        Column(
            Modifier
                .fillMaxSize()
                .statusBarsPadding()
                .navigationBarsPadding()
                .padding(16.dp)
        ) {
            Row(
                Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                BlastProLogo()
                IconButton(onClick = onClose) {
                    Icon(Icons.Default.Close, contentDescription = "Close menu", tint = Color.White)
                }
            }

            Spacer(Modifier.padding(top = 16.dp))

            MenuItem("New Note", Icons.Default.AddCircleOutline, selected = false, onClick = onNewNote)
            MenuItem("Notes", Icons.Default.Description, selected = currentRoute == Routes.NOTES, onClick = onNotes)
            if (Config.CHAT_ENABLED) {
                MenuItem("Chat", Icons.Default.Forum, selected = currentRoute == Routes.CHAT, onClick = onChat)
            }
            MenuItem("Settings", Icons.Default.Settings, selected = currentRoute == Routes.SETTINGS, onClick = onSettings)
            MenuItem("Sync Now", Icons.Default.Sync, selected = false, onClick = onSync)

            Spacer(Modifier.weight(1f))

            MenuItem("Log Out", Icons.AutoMirrored.Filled.Logout, selected = false, onClick = onLogout)
        }
    }
}

@Composable
private fun MenuItem(label: String, icon: ImageVector, selected: Boolean, onClick: () -> Unit) {
    val content = if (selected) InkDark else Color.White
    Row(
        Modifier
            .fillMaxWidth()
            .padding(vertical = 3.dp)
            .clip(RoundedCornerShape(8.dp))
            .background(if (selected) Color.White else Color.Transparent)
            .clickable(onClick = onClick)
            .padding(horizontal = 12.dp, vertical = 16.dp),
        verticalAlignment = Alignment.CenterVertically
    ) {
        Icon(icon, contentDescription = null, tint = if (selected) BlastRed else content)
        Spacer(Modifier.width(12.dp))
        Text(label, color = content, fontSize = 17.sp)
    }
}

@Preview(showBackground = true, widthDp = 300, heightDp = 720)
@Composable
private fun MenuDrawerPreview() {
    BlastProMobileTheme { MenuDrawerContent(Routes.NOTES, {}, {}, {}, {}, {}, {}, {}) }
}
