package com.example.blastpromobile.data

import android.content.Context
import android.net.Uri
import com.example.blastpromobile.data.local.BlastProDatabase
import com.example.blastpromobile.data.local.Note
import com.example.blastpromobile.data.local.NotePhoto
import com.example.blastpromobile.util.PhotoStorage
import kotlinx.coroutines.flow.Flow

class NoteRepository(private val appContext: Context) {

    private val dao = BlastProDatabase.get(appContext).noteDao()

    fun observeNotes(projectFilter: String?, query: String?): Flow<List<Note>> {
        val project = projectFilter
            ?.takeIf { it.isNotBlank() && it != ALL_PROJECTS }
        val q = query?.takeIf { it.isNotBlank() }
        return dao.observeFiltered(project, q)
    }

    suspend fun getNote(id: Long): Note? = dao.getById(id)

    suspend fun getPhotos(noteId: Long): List<NotePhoto> = dao.getPhotosFor(noteId)

    suspend fun countPhotos(noteId: Long): Int = dao.countPhotosFor(noteId)

    suspend fun createNote(
        title: String,
        body: String,
        projectName: String,
        photoUris: List<Uri>
    ): Long {
        val now = System.currentTimeMillis()

        val note = Note(
            title = title,
            body = body,
            projectName = projectName,
            createdAtMillis = now,
            updatedAtMillis = now,
            status = Note.STATUS_SAVED
        )

        val photos = photoUris.mapNotNull { uri ->
            val path = PhotoStorage.copyIntoAppStorage(appContext, uri)
                ?: return@mapNotNull null
            NotePhoto(noteId = 0, filePath = path, addedAtMillis = now)
        }

        return dao.insertNoteWithPhotos(note, photos)
    }

    suspend fun updateNote(
        noteId: Long,
        title: String,
        body: String,
        projectName: String
    ) {
        val existing = dao.getById(noteId) ?: return
        dao.update(
            existing.copy(
                title = title,
                body = body,
                projectName = projectName,
                updatedAtMillis = System.currentTimeMillis()
            )
        )
    }

    suspend fun addPhoto(noteId: Long, uri: Uri): Boolean {
        val path = PhotoStorage.copyIntoAppStorage(appContext, uri) ?: return false
        dao.insertPhoto(
            NotePhoto(
                noteId = noteId,
                filePath = path,
                addedAtMillis = System.currentTimeMillis()
            )
        )
        return true
    }

    suspend fun removePhoto(noteId: Long, photoId: Long) {
        val photos = dao.getPhotosFor(noteId)
        val target = photos.firstOrNull { it.id == photoId } ?: return
        dao.deletePhotoById(photoId)
        PhotoStorage.deleteIfExists(target.filePath)
    }

    suspend fun deleteNote(noteId: Long) {
        val photos = dao.getPhotosFor(noteId)
        dao.deleteById(noteId)
        photos.forEach { PhotoStorage.deleteIfExists(it.filePath) }
    }

    companion object {
        const val ALL_PROJECTS = "All projects"
        const val NO_PROJECT = "No project"
    }
}