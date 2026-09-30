package com.example.blastpromobile.ui.notes

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Image
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.tooling.preview.Preview
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.blastpromobile.ui.components.BlastProScreen
import com.example.blastpromobile.ui.components.ScreenHeading
import com.example.blastpromobile.ui.components.StatusChip
import com.example.blastpromobile.ui.components.WhiteDropdown
import com.example.blastpromobile.ui.components.WhiteTextField
import com.example.blastpromobile.ui.theme.BlastProMobileTheme
import com.example.blastpromobile.ui.theme.InkDark
import com.example.blastpromobile.ui.theme.InkMuted
import com.example.blastpromobile.ui.theme.StatusOrange

@Composable
fun NotesHomeScreen(
    onMenuClick: () -> Unit,
    onOpenNote: (String) -> Unit
) {
    NotesHomeContent(
        notes = sampleNotes,
        showSyncError = false,
        onMenuClick = onMenuClick,
        onOpenNote = onOpenNote
    )
}

@Composable
fun NotesHomeContent(
    notes: List<SampleNote>,
    showSyncError: Boolean,
    onMenuClick: () -> Unit,
    onOpenNote: (String) -> Unit
) {
    var search by remember { mutableStateOf("") }
    var project by remember { mutableStateOf("All projects") }

    BlastProScreen(onMenuClick = onMenuClick) {
        ScreenHeading("Notes", "Field notes and photos saved on this device.")

        WhiteTextField(value = search, onValueChange = { search = it }, placeholder = "Search notes")
        Box(Modifier.padding(top = 8.dp)) {
            WhiteDropdown(project, listOf("All projects") + sampleProjects.drop(1)) { project = it }
        }

        if (showSyncError) SyncErrorBanner()

        Box(Modifier.weight(1f).padding(top = 12.dp)) {
            if (notes.isEmpty()) {
                EmptyNotesState()
            } else {
                LazyColumn(verticalArrangement = Arrangement.spacedBy(10.dp)) {
                    items(notes, key = { it.id }) { note ->
                        NoteCard(note, onClick = { onOpenNote(note.id) })
                    }
                }
            }
        }
    }
}

@Composable
private fun NoteCard(note: SampleNote, onClick: () -> Unit) {
    Column(
        Modifier
            .fillMaxWidth()
            .clip(RoundedCornerShape(8.dp))
            .background(Color.White)
            .clickable(onClick = onClick)
            .padding(16.dp)
    ) {
        Row(
            Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            Text(
                note.title,
                color = InkDark,
                fontSize = 17.sp,
                fontWeight = FontWeight.Bold,
                maxLines = 1,
                overflow = TextOverflow.Ellipsis,
                modifier = Modifier.weight(1f).padding(end = 8.dp)
            )
            StatusChip(note.syncState.label, note.syncState.color)
        }
        if (note.preview.isNotEmpty()) {
            Text(
                note.preview,
                color = InkMuted,
                fontSize = 14.sp,
                maxLines = 2,
                overflow = TextOverflow.Ellipsis,
                modifier = Modifier.padding(top = 4.dp)
            )
        }
        Row(
            Modifier.fillMaxWidth().padding(top = 8.dp),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            Text(note.lastEdited, color = InkMuted, fontSize = 12.sp)
            Text(note.project ?: NO_PROJECT, color = InkMuted, fontSize = 12.sp)
            Row(verticalAlignment = Alignment.CenterVertically) {
                Icon(Icons.Default.Image, contentDescription = "Photos", tint = InkMuted, modifier = Modifier.padding(end = 3.dp))
                Text("${note.photoCount}", color = InkMuted, fontSize = 12.sp)
            }
        }
    }
}

@Composable
private fun EmptyNotesState() {
    Column(
        Modifier.fillMaxWidth().padding(top = 48.dp),
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        Text("No notes yet", color = Color.White, fontSize = 20.sp, fontWeight = FontWeight.Bold)
        Text(
            "Tap New Note to capture your first field note.",
            color = Color.White,
            fontSize = 14.sp,
            modifier = Modifier.padding(top = 4.dp)
        )
    }
}

@Composable
private fun SyncErrorBanner() {
    Row(
        Modifier
            .fillMaxWidth()
            .padding(top = 12.dp)
            .clip(RoundedCornerShape(6.dp))
            .background(StatusOrange)
            .padding(10.dp),
        horizontalArrangement = Arrangement.SpaceBetween,
        verticalAlignment = Alignment.CenterVertically
    ) {
        Text("Sync failed. Your notes are safe on this device.", color = Color.White, fontSize = 14.sp)
        Text("Retry", color = Color.White, fontSize = 14.sp, fontWeight = FontWeight.Bold)
    }
}

@Preview(showBackground = true, widthDp = 360, heightDp = 720)
@Composable
private fun NotesHomePreview() {
    BlastProMobileTheme { NotesHomeContent(sampleNotes, false, {}, {}) }
}

@Preview(showBackground = true, widthDp = 360, heightDp = 720)
@Composable
private fun NotesEmptyPreview() {
    BlastProMobileTheme { NotesHomeContent(emptyList(), false, {}, {}) }
}

@Preview(showBackground = true, widthDp = 360, heightDp = 720)
@Composable
private fun NotesSyncErrorPreview() {
    BlastProMobileTheme { NotesHomeContent(sampleNotes, true, {}, {}) }
}
