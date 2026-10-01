package com.example.blastpromobile.data.local

import androidx.room.Dao
import androidx.room.Insert
import androidx.room.Query
import androidx.room.Transaction
import androidx.room.Update
import kotlinx.coroutines.flow.Flow

@Dao
interface NoteDao {

    @Query(
        """
        SELECT * FROM notes
        WHERE (:project IS NULL OR projectName = :project)
          AND (:query IS NULL OR
               title LIKE '%' || :query || '%' OR
               body  LIKE '%' || :query || '%')
        ORDER BY updatedAtMillis DESC
        """
    )
    fun observeFiltered(project: String?, query: String?): Flow<List<Note>>

    @Query("SELECT * FROM notes WHERE id = :id LIMIT 1")
    suspend fun getById(id: Long): Note?

    @Insert
    suspend fun insert(note: Note): Long

    @Update
    suspend fun update(note: Note)

    @Query("DELETE FROM notes WHERE id = :id")
    suspend fun deleteById(id: Long)

    @Query("SELECT * FROM note_photos WHERE noteId = :noteId ORDER BY addedAtMillis ASC")
    suspend fun getPhotosFor(noteId: Long): List<NotePhoto>

    @Insert
    suspend fun insertPhoto(photo: NotePhoto): Long

    @Query("DELETE FROM note_photos WHERE id = :id")
    suspend fun deletePhotoById(id: Long)

    @Query("SELECT COUNT(*) FROM note_photos WHERE noteId = :noteId")
    suspend fun countPhotosFor(noteId: Long): Int

    @Transaction
    suspend fun insertNoteWithPhotos(note: Note, photos: List<NotePhoto>): Long {
        val noteId = insert(note)
        photos.forEach { insertPhoto(it.copy(noteId = noteId)) }
        return noteId
    }
}