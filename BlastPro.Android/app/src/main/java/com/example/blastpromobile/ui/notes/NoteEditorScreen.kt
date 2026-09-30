package com.example.blastpromobile.ui.notes

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Check
import androidx.compose.material.icons.filled.Close
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material.icons.filled.Image
import androidx.compose.material.icons.filled.PhotoCamera
import androidx.compose.material.icons.filled.PhotoLibrary
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.tooling.preview.Preview
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.blastpromobile.ui.components.BlastProScreen
import com.example.blastpromobile.ui.components.FieldLabel
import com.example.blastpromobile.ui.components.RedButton
import com.example.blastpromobile.ui.components.ScreenHeading
import com.example.blastpromobile.ui.components.WhiteDropdown
import com.example.blastpromobile.ui.components.WhiteTextField
import com.example.blastpromobile.ui.theme.BlastProMobileTheme
import com.example.blastpromobile.ui.theme.BlastRed
import com.example.blastpromobile.ui.theme.InkMuted

/** Pass noteId = null for a new note. UI only: nothing is saved yet. */
@Composable
fun NoteEditorScreen(
    noteId: String?,
    onBack: () -> Unit,
    onOpenPhoto: () -> Unit
) {
    val existing = sampleNotes.firstOrNull { it.id == noteId }
    var title by remember { mutableStateOf(existing?.title ?: "") }
    var body by remember { mutableStateOf(existing?.preview ?: "") }
    var project by remember { mutableStateOf(existing?.project ?: NO_PROJECT) }
    var photoCount by remember { mutableStateOf(existing?.photoCount ?: 0) }
    var confirmDelete by remember { mutableStateOf(false) }

    BlastProScreen(onBackClick = onBack) {
        Column(Modifier.weight(1f).verticalScroll(rememberScrollState())) {
            ScreenHeading(
                if (existing == null) "New Note" else "Edit Note",
                "Add text and photos. Notes save on this device first."
            )

            FieldLabel("Title")
            WhiteTextField(title, { title = it }, placeholder = "Note title")

            FieldLabel("Note")
            WhiteTextField(
                body, { body = it },
                placeholder = "Write your note (optional for photo-only notes)",
                singleLine = false,
                minHeight = 180.dp
            )

            FieldLabel("Project")
            WhiteDropdown(project, sampleProjects) { project = it }

            FieldLabel("Photos")
            Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                RedButton("Take Photo", { photoCount++ }, icon = Icons.Default.PhotoCamera)
                RedButton("Choose from Gallery", { photoCount++ }, icon = Icons.Default.PhotoLibrary)
            }
            Row(
                Modifier.padding(top = 10.dp),
                horizontalArrangement = Arrangement.spacedBy(8.dp)
            ) {
                repeat(photoCount) { PhotoThumbnail(onClick = onOpenPhoto, onRemove = { photoCount-- }) }
            }
            if (photoCount == 0) {
                Text("No photos attached.", color = Color.White, fontSize = 13.sp)
            }
        }

        Row(
            Modifier.fillMaxWidth().padding(top = 8.dp),
            horizontalArrangement = Arrangement.spacedBy(8.dp)
        ) {
            RedButton("Save", onBack, icon = Icons.Default.Check, modifier = Modifier.weight(1f))
            if (existing != null) {
                RedButton("Delete", { confirmDelete = true }, icon = Icons.Default.Delete, modifier = Modifier.weight(1f))
            }
        }
    }

    if (confirmDelete) {
        AlertDialog(
            onDismissRequest = { confirmDelete = false },
            title = { Text("Delete note?") },
            text = { Text("This note and its photos will be removed.") },
            confirmButton = {
                TextButton(onClick = { confirmDelete = false; onBack() }) { Text("Delete", color = BlastRed) }
            },
            dismissButton = {
                TextButton(onClick = { confirmDelete = false }) { Text("Cancel") }
            }
        )
    }
}

@Composable
private fun PhotoThumbnail(onClick: () -> Unit, onRemove: () -> Unit) {
    Box(
        Modifier
            .size(96.dp)
            .clip(RoundedCornerShape(6.dp))
            .background(Color(0xFF4B5563))
            .clickable(onClick = onClick),
        contentAlignment = Alignment.Center
    ) {
        Icon(Icons.Default.Image, contentDescription = "Photo", tint = InkMuted, modifier = Modifier.size(36.dp))
        Box(
            Modifier
                .align(Alignment.TopEnd)
                .padding(3.dp)
                .size(26.dp)
                .clip(RoundedCornerShape(10.dp))
                .background(Color.Black.copy(alpha = 0.6f))
                .clickable(onClick = onRemove),
            contentAlignment = Alignment.Center
        ) {
            Icon(Icons.Default.Close, contentDescription = "Remove photo", tint = Color.White, modifier = Modifier.size(16.dp))
        }
    }
}

@Preview(showBackground = true, widthDp = 360, heightDp = 720)
@Composable
private fun NoteEditorPreview() {
    BlastProMobileTheme { NoteEditorScreen(noteId = "1", onBack = {}, onOpenPhoto = {}) }
}
