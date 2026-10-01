package com.example.blastpromobile.util

import android.content.Context
import android.net.Uri
import java.io.File
import java.io.FileOutputStream
import java.util.UUID

object PhotoStorage {

    private const val DIR_NAME = "note_photos"

    fun photoDir(context: Context): File {
        val dir = File(context.filesDir, DIR_NAME)
        if (!dir.exists()) dir.mkdirs()
        return dir
    }

    fun copyIntoAppStorage(context: Context, source: Uri): String? {
        return try {
            val targetFile = File(photoDir(context), "photo_${UUID.randomUUID()}.jpg")
            context.contentResolver.openInputStream(source)?.use { input ->
                FileOutputStream(targetFile).use { output ->
                    input.copyTo(output)
                }
            } ?: return null
            targetFile.absolutePath
        } catch (_: Throwable) {
            null
        }
    }

    fun deleteIfExists(path: String?) {
        if (path.isNullOrBlank()) return
        try {
            val f = File(path)
            if (f.exists()) f.delete()
        } catch (_: Throwable) { /* ignore */ }
    }
}