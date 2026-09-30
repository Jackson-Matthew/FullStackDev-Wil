package com.example.blastpromobile.ui.notes

import androidx.compose.ui.graphics.Color
import com.example.blastpromobile.ui.theme.BlastRed
import com.example.blastpromobile.ui.theme.StatusBlue
import com.example.blastpromobile.ui.theme.StatusGreen
import com.example.blastpromobile.ui.theme.StatusGrey
import com.example.blastpromobile.ui.theme.StatusOrange

/** Placeholder sync states. Real values come from the local database once sync exists. */
enum class SyncState(val label: String, val color: Color) {
    SavedOnDevice("Saved on device", StatusGrey),
    Syncing("Syncing", StatusBlue),
    Synced("Synced", StatusGreen),
    Conflict("Conflict", BlastRed),
    NeedsRetry("Needs retry", StatusOrange)
}

/** Temporary UI-only model. Replaced by the Room entity later. */
data class SampleNote(
    val id: String,
    val title: String,
    val preview: String,
    val lastEdited: String,
    val photoCount: Int,
    val project: String?,
    val syncState: SyncState
)

const val NO_PROJECT = "No project"

val sampleProjects = listOf(NO_PROJECT, "Test 1", "Test 2", "Test 3")

val sampleNotes = listOf(
    SampleNote("1", "Bench 2 loose rock", "Loose material along the east wall, check before charging.", "Oct 24, 2026", 2, "Test 1", SyncState.Synced),
    SampleNote("2", "Wet holes row C", "Water in holes 4 to 7, consider emulsion for this row.", "Oct 22, 2026", 1, "Test 2", SyncState.SavedOnDevice),
    SampleNote("3", "Photo of face", "", "Oct 19, 2026", 3, null, SyncState.NeedsRetry),
    SampleNote("4", "Site meeting", "Confirmed exclusion zone and blast time with the site manager.", "Oct 15, 2026", 0, "Test 3", SyncState.Conflict)
)
