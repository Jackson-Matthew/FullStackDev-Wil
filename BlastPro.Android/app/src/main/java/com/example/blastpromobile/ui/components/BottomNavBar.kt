package com.example.blastpromobile.ui.components

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.navigationBarsPadding
import androidx.compose.foundation.layout.offset
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.Description
import androidx.compose.material.icons.filled.Forum
import androidx.compose.material3.Icon
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
import com.example.blastpromobile.ui.theme.BlastProMobileTheme
import com.example.blastpromobile.ui.theme.BlastRed
import com.example.blastpromobile.ui.theme.TopBarBackground

/** Bottom navigation: Notes, New Note (centre) and Chat. Chat is hidden when Config.CHAT_ENABLED is false. */
@Composable
fun BottomNavBar(
    notesSelected: Boolean,
    chatSelected: Boolean,
    onNotes: () -> Unit,
    onNewNote: () -> Unit,
    onChat: () -> Unit
) {
    Row(
        Modifier
            .fillMaxWidth()
            .background(TopBarBackground)
            .navigationBarsPadding()
            .height(72.dp),
        horizontalArrangement = Arrangement.SpaceEvenly,
        verticalAlignment = Alignment.CenterVertically
    ) {
        BottomNavItem("Notes", Icons.Default.Description, notesSelected, onNotes, Modifier.weight(1f))
        NewNoteItem(onNewNote, Modifier.weight(1f))
        if (Config.CHAT_ENABLED) {
            BottomNavItem("Chat", Icons.Default.Forum, chatSelected, onChat, Modifier.weight(1f))
        } else {
            Box(Modifier.weight(1f))
        }
    }
}

@Composable
private fun BottomNavItem(
    label: String,
    icon: ImageVector,
    selected: Boolean,
    onClick: () -> Unit,
    modifier: Modifier = Modifier
) {
    val tint = if (selected) BlastRed else Color.White.copy(alpha = 0.7f)
    Column(
        modifier.clickable(onClick = onClick).padding(vertical = 8.dp),
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        Icon(icon, contentDescription = label, tint = tint, modifier = Modifier.size(28.dp))
        Text(label, color = tint, fontSize = 13.sp)
    }
}

@Composable
private fun NewNoteItem(onClick: () -> Unit, modifier: Modifier = Modifier) {
    Box(modifier, contentAlignment = Alignment.Center) {
        Box(
            Modifier
                .offset(y = (-10).dp)
                .size(60.dp)
                .clip(CircleShape)
                .background(BlastRed)
                .clickable(onClick = onClick),
            contentAlignment = Alignment.Center
        ) {
            Icon(Icons.Default.Add, contentDescription = "New note", tint = Color.White, modifier = Modifier.size(34.dp))
        }
    }
}

@Preview(widthDp = 360)
@Composable
private fun BottomNavBarPreview() {
    BlastProMobileTheme { BottomNavBar(true, false, {}, {}, {}) }
}
