package com.example.blastpromobile.ui

import android.Manifest
import android.content.pm.PackageManager
import android.graphics.Bitmap
import android.net.Uri
import android.os.Bundle
import android.view.View
import android.widget.ArrayAdapter
import android.widget.Button
import android.widget.EditText
import android.widget.ImageView
import android.widget.LinearLayout
import android.widget.Spinner
import android.widget.TextView
import android.widget.Toast
import androidx.activity.result.contract.ActivityResultContracts
import androidx.core.content.ContextCompat
import androidx.lifecycle.lifecycleScope
import androidx.navigation.fragment.findNavController
import com.example.blastpromobile.R
import com.example.blastpromobile.data.RemotePhoto
import com.example.blastpromobile.data.RemoteProject
import com.example.blastpromobile.data.RemoteRepository
import com.google.android.material.dialog.MaterialAlertDialogBuilder
import kotlinx.coroutines.launch
import java.io.File

class NoteEditorFragment : BaseFragment(R.layout.fragment_note_editor) {
    private lateinit var repo: RemoteRepository
    private lateinit var editTitle: EditText
    private lateinit var editBody: EditText
    private lateinit var projectSpinner: Spinner
    private lateinit var textHeading: TextView
    private lateinit var textNoPhotos: TextView
    private lateinit var previewPanel: View
    private lateinit var previewImage: ImageView
    private lateinit var commentInput: EditText
    private lateinit var commentsContainer: LinearLayout
    private val photoFrames = mutableListOf<View>()
    private val photoIcons = mutableListOf<ImageView>()
    private val pendingUris = mutableListOf<Uri>()
    private val existingPhotos = mutableListOf<RemotePhoto>()
    private val currentPaths = mutableListOf<String>()
    private val shownPaths = mutableListOf<String>()
    private var projects = listOf<RemoteProject>()
    private var noteId = 0
    private var projectId = 0

    private val takePhoto = registerForActivityResult(ActivityResultContracts.TakePicturePreview()) { bitmap: Bitmap? ->
        if (bitmap != null) {
            val file = File(requireContext().cacheDir, "cap_${System.currentTimeMillis()}.jpg")
            file.outputStream().use { bitmap.compress(Bitmap.CompressFormat.JPEG, 90, it) }
            addPending(Uri.fromFile(file))
        }
    }
    private val pickImage = registerForActivityResult(ActivityResultContracts.GetContent()) { uri: Uri? ->
        if (uri != null) addPending(uri)
    }
    private val requestCameraPermission = registerForActivityResult(ActivityResultContracts.RequestPermission()) { granted ->
        if (granted) takePhoto.launch(null)
        else toast("Camera permission is needed to take photos.")
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        repo = RemoteRepository(requireContext().applicationContext)
        noteId = arguments?.getString("noteId")?.toIntOrNull() ?: 0
        projectId = arguments?.getInt("projectId") ?: 0
        editTitle = view.findViewById(R.id.edit_title)
        editBody = view.findViewById(R.id.edit_body)
        projectSpinner = view.findViewById(R.id.spinner_project)
        textHeading = view.findViewById(R.id.text_heading)
        textNoPhotos = view.findViewById(R.id.text_no_photos)
        previewPanel = view.findViewById(R.id.photo_preview_panel)
        previewImage = view.findViewById(R.id.image_preview)
        commentInput = view.findViewById(R.id.edit_comment)
        commentsContainer = view.findViewById(R.id.comments_container)
        textHeading.text = if (noteId == 0) "New Note" else "Edit Note"
        view.findViewById<View>(R.id.btn_delete).visibility = if (noteId == 0) View.GONE else View.VISIBLE
        view.findViewById<View>(R.id.comments_section).visibility = if (noteId == 0) View.GONE else View.VISIBLE

        listOf(R.id.thumb_1, R.id.thumb_2, R.id.thumb_3).forEach { id ->
            val frame = view.findViewById<View>(id)
            photoFrames += frame
            photoIcons += (frame as android.view.ViewGroup).getChildAt(0) as ImageView
        }
        photoFrames.forEachIndexed { index, frame ->
            frame.setOnClickListener { shownPaths.getOrNull(index)?.let(::showPreview) }
            photoIcons[index].setOnClickListener { shownPaths.getOrNull(index)?.let(::showPreview) }
        }
        listOf(R.id.thumb_1_remove, R.id.thumb_2_remove, R.id.thumb_3_remove)
            .forEachIndexed { index, id -> view.findViewById<View>(id).setOnClickListener { removePhotoAt(index) } }
        view.findViewById<View>(R.id.preview_close).setOnClickListener { previewPanel.visibility = View.GONE }
        view.findViewById<View>(R.id.btn_take_photo).setOnClickListener {
            if (ContextCompat.checkSelfPermission(requireContext(), Manifest.permission.CAMERA) == PackageManager.PERMISSION_GRANTED)
                takePhoto.launch(null)
            else requestCameraPermission.launch(Manifest.permission.CAMERA)
        }
        view.findViewById<View>(R.id.btn_gallery).setOnClickListener { pickImage.launch("image/*") }
        view.findViewById<View>(R.id.btn_save).setOnClickListener { saveNote() }
        view.findViewById<View>(R.id.btn_delete).setOnClickListener { confirmDelete() }
        view.findViewById<View>(R.id.btn_add_comment).setOnClickListener { addComment() }
        load()
    }

    private fun load() {
        viewLifecycleOwner.lifecycleScope.launch {
            try {
                projects = repo.projects()
                projectSpinner.adapter = ArrayAdapter(requireContext(), R.layout.item_project_spinner,
                    projects.map { it.name }).also { it.setDropDownViewResource(R.layout.item_project_spinner) }
                if (noteId != 0) {
                    val found = if (projectId != 0) repo.note(projectId, noteId)
                    else projects.firstNotNullOfOrNull { p -> runCatching { repo.note(p.id, noteId) }.getOrNull() }
                        ?: throw IllegalStateException("Note not found")
                    projectId = found.projectId
                    editTitle.setText(found.title)
                    editBody.setText(found.body)
                    projectSpinner.isEnabled = false
                    existingPhotos.clear(); existingPhotos.addAll(found.photos)
                    renderComments(found.comments.map { "${it.authorName}: ${it.body}" })
                    loadPhotoPaths()
                }
                projectSpinner.setSelection(projects.indexOfFirst { it.id == projectId }.coerceAtLeast(0))
                refreshPhotoStrip()
            } catch (error: Exception) { toast(error.message ?: "Could not load the note") }
        }
    }

    private fun renderComments(comments: List<String>) {
        commentsContainer.removeAllViews()
        comments.forEach { line -> commentsContainer.addView(TextView(requireContext()).apply {
            text = line; setTextColor(ContextCompat.getColor(requireContext(), R.color.white)); textSize = 15f
            setPadding(0, 4, 0, 4)
        }) }
    }

    private fun addComment() {
        val body = commentInput.text.toString().trim()
        if (body.isBlank()) { toast("Enter a comment"); return }
        viewLifecycleOwner.lifecycleScope.launch {
            try {
                repo.addComment(projectId, noteId, body)
                commentInput.text.clear()
                val note = repo.note(projectId, noteId)
                renderComments(note.comments.map { "${it.authorName}: ${it.body}" })
            } catch (error: Exception) { toast(error.message ?: "Could not add comment") }
        }
    }

    private fun saveNote() {
        val selected = projects.getOrNull(projectSpinner.selectedItemPosition)
        if (selected == null) { toast("Create or select a project first"); return }
        val title = editTitle.text.toString().trim().ifBlank { if (pendingUris.isNotEmpty()) "Field photo" else "" }
        val body = editBody.text.toString().trim()
        if (title.isBlank() && body.isBlank()) { toast("Add a title, note text, or photo"); return }
        val button = requireView().findViewById<View>(R.id.btn_save)
        button.isEnabled = false
        viewLifecycleOwner.lifecycleScope.launch {
            try {
                if (noteId == 0) {
                    val created = repo.createNote(selected.id, title, body)
                    noteId = created.id
                    projectId = selected.id
                } else repo.updateNote(projectId, noteId, title, body)
                pendingUris.toList().forEach { repo.uploadPhoto(projectId, noteId, it); pendingUris.remove(it) }
                findNavController().popBackStack()
            } catch (error: Exception) { toast(error.message ?: "Could not save note or photo") }
            finally { button.isEnabled = true }
        }
    }

    private fun confirmDelete() {
        MaterialAlertDialogBuilder(requireContext()).setTitle("Delete note?")
            .setMessage("This note, its comments, and its photos will be removed from the web and mobile app.")
            .setPositiveButton("Delete") { _, _ -> viewLifecycleOwner.lifecycleScope.launch {
                try { repo.deleteNote(projectId, noteId); findNavController().popBackStack() }
                catch (error: Exception) { toast(error.message ?: "Could not delete note") }
            } }.setNegativeButton("Cancel", null).show()
    }

    private fun addPending(uri: Uri) {
        if (existingPhotos.size + pendingUris.size >= 3) { toast("Up to three photos per note"); return }
        pendingUris += uri
        refreshPhotoStrip()
        showPreview(uri.toString())
    }

    private fun removePhotoAt(index: Int) {
        if (index < existingPhotos.size) {
            val photo = existingPhotos[index]
            viewLifecycleOwner.lifecycleScope.launch {
                try {
                    repo.deletePhoto(projectId, noteId, photo.id)
                    existingPhotos.removeAt(index)
                    loadPhotoPaths()
                    refreshPhotoStrip()
                } catch (error: Exception) { toast(error.message ?: "Could not remove photo") }
            }
        } else {
            val pendingIndex = index - existingPhotos.size
            if (pendingIndex in pendingUris.indices) pendingUris.removeAt(pendingIndex)
            refreshPhotoStrip()
        }
        previewPanel.visibility = View.GONE
    }

    private suspend fun loadPhotoPaths() {
        currentPaths.clear()
        existingPhotos.forEach { photo ->
            currentPaths += runCatching { repo.photoFile(projectId, noteId, photo.id).absolutePath }.getOrDefault("")
        }
    }

    private fun refreshPhotoStrip() {
        val paths = currentPaths + pendingUris.map { it.toString() }
        shownPaths.clear(); shownPaths.addAll(paths)
        photoFrames.forEachIndexed { index, frame ->
            val path = paths.getOrNull(index)
            frame.visibility = if (path == null) View.GONE else View.VISIBLE
            if (path != null) {
                val uri = if (path.startsWith("content:") || path.startsWith("file:")) Uri.parse(path)
                    else Uri.fromFile(File(path))
                photoIcons[index].setImageURI(uri)
            } else photoIcons[index].setImageResource(R.drawable.ic_image)
        }
        textNoPhotos.visibility = if (paths.isEmpty()) View.VISIBLE else View.GONE
    }

    private fun showPreview(path: String) {
        previewPanel.visibility = View.VISIBLE
        val uri = if (path.startsWith("content:") || path.startsWith("file:")) Uri.parse(path)
            else Uri.fromFile(File(path))
        previewImage.setImageURI(uri)
    }

    private fun toast(message: String) = Toast.makeText(requireContext(), message, Toast.LENGTH_LONG).show()
}
