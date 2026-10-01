package com.example.blastpromobile.data.local

import androidx.room.Entity
import androidx.room.PrimaryKey

@Entity(tableName = "notes")
data class Note(
    @PrimaryKey(autoGenerate = true)
    val id: Long = 0,

    val title: String,
    val body: String,
    val projectName: String,

    val createdAtMillis: Long,
    val updatedAtMillis: Long,

    val status: String = STATUS_SAVED
) {
    companion object {
        const val STATUS_SYNCED = "synced"
        const val STATUS_SAVED = "saved"
        const val STATUS_RETRY = "retry"
        const val STATUS_CONFLICT = "conflict"
    }
}