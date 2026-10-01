package com.example.blastpromobile.ui

import android.Manifest
import android.content.pm.PackageManager
import android.graphics.Bitmap
import android.net.Uri
import android.os.Bundle
import android.util.Log
import android.view.View
import android.widget.EditText
import android.widget.ImageView
import android.widget.Spinner
import android.widget.TextView
import android.widget.Toast
import androidx.activity.result.contract.ActivityResultContracts
import androidx.core.content.ContextCompat
import androidx.lifecycle.lifecycleScope
import androidx.navigation.fragment.findNavController
import com.example.blastpromobile.R
import com.example.blastpromobile.data.NoteRepository
import com.example.blastpromobile.data.local.NotePhoto
import com.google.android.material.dialog.MaterialAlertDialogBuilder
import kotlinx.coroutines.launch
import java.io.File

class NoteEditorFragment : BaseFragment(R.layout.fragment_note_editor) {

    private val tag = "NoteEditor"

    private lateinit var repo: NoteRepository

    private lateinit var editTitle: EditText
    private lateinit var editBody: EditText
    private lateinit var projectSpinner: Spinner
    private lateinit var textHeading: TextView
    private lateinit var textNoPhotos: TextView

    // Large preview panel + image
    private lateinit var previewPanel: View
    private lateinit var previewImage: ImageView

    private val photoFrames = mutableListOf<View>()
    private val photoIcons = mutableListOf<ImageView>()

    private val currentPaths = mutableListOf<String>()

    private var noteId: Long = 0L
    private var isNew = true
    private val pendingUris = mutableListOf<Uri>()
    private val existingPhotos = mutableListOf<NotePhoto>()

    // ----------------------------------------------------------------
    // Activity result launchers
    // ----------------------------------------------------------------

    private val takePhoto = registerForActivityResult(
        ActivityResultContracts.TakePicturePreview()
    ) { bitmap: Bitmap? ->
        if (bitmap != null) {
            val tmp = File(requireContext().cacheDir, "cap_${System.currentTimeMillis()}.jpg")
            tmp.outputStream().use { out ->
                bitmap.compress(Bitmap.CompressFormat.JPEG, 90, out)
            }
            onPhotoPicked(Uri.fromFile(tmp))
        }
    }

    private val pickImage = registerForActivityResult(
        ActivityResultContracts.GetContent()
    ) { uri: Uri? -> if (uri != null) onPhotoPicked(uri) }

    private val requestCameraPermission = registerForActivityResult(
        ActivityResultContracts.RequestPermission()
    ) { granted ->
        if (granted) {
            takePhoto.launch(null)
        } else {
            Toast.makeText(
                requireContext(),
                "Camera permission is needed to take photos.",
                Toast.LENGTH_SHORT
            ).show()
        }
    }

    // ----------------------------------------------------------------
    // Lifecycle
    // ----------------------------------------------------------------

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)

        repo = NoteRepository(requireContext().applicationContext)

        editTitle = view.findViewById(R.id.edit_title)
        editBody = view.findViewById(R.id.edit_body)
        projectSpinner = view.findViewById(R.id.spinner_project)
        textHeading = view.findViewById(R.id.text_heading)
        textNoPhotos = view.findViewById(R.id.text_no_photos)

        previewPanel = view.findViewById(R.id.photo_preview_panel)
        previewImage = view.findViewById(R.id.image_preview)

        val frameIds = listOf(R.id.thumb_1, R.id.thumb_2, R.id.thumb_3)
        frameIds.forEach { id ->
            val frame = view.findViewById<View>(id)
            photoFrames += frame
            val icon = (frame as android.view.ViewGroup).getChildAt(0) as ImageView
            photoIcons += icon
        }

        val arg = requireArguments().getString("noteId")
        isNew = arg.isNullOrBlank() || arg == "new"

        if (isNew) {
            textHeading.text = "New Note"
            view.findViewById<View>(R.id.btn_delete).visibility = View.GONE
        } else {
            noteId = arg!!.toLongOrNull() ?: 0L
            textHeading.text = "Edit Note"
            view.findViewById<View>(R.id.btn_delete).visibility = View.VISIBLE
            loadNote()
        }

        // Camera
        view.findViewById<View>(R.id.btn_take_photo).setOnClickListener {
            val hasPermission = ContextCompat.checkSelfPermission(
                requireContext(),
                Manifest.permission.CAMERA
            ) == PackageManager.PERMISSION_GRANTED

            if (hasPermission) {
                takePhoto.launch(null)
            } else {
                requestCameraPermission.launch(Manifest.permission.CAMERA)
            }
        }

        // Gallery
        view.findViewById<View>(R.id.btn_gallery).setOnClickListener {
            pickImage.launch("image/*")
        }

        view.findViewById<View>(R.id.btn_save).setOnClickListener { saveNote() }
        view.findViewById<View>(R.id.btn_delete).setOnClickListener { confirmDelete() }

        // Close preview panel
        view.findViewById<View>(R.id.preview_close).setOnClickListener {
            previewPanel.visibility = View.GONE
        }

        // Remove photo buttons
        val removeButtons = listOf(
            R.id.thumb_1_remove,
            R.id.thumb_2_remove,
            R.id.thumb_3_remove
        )
        removeButtons.forEachIndexed { index, id ->
            view.findViewById<View>(id).setOnClickListener { removePhotoAt(index) }
        }

        // Wire tap listeners on frames and icons — show in the preview panel
        photoFrames.forEachIndexed { index, frame ->
            val showPreview: (View) -> Unit = {
                val path = currentPaths.getOrNull(index)
                Log.d(tag, "Tap thumb $index, path=$path")
                if (!path.isNullOrBlank()) {
                    showInPreviewPanel(path)
                }
            }
            frame.setOnClickListener(showPreview)
            photoIcons[index].setOnClickListener(showPreview)
        }

        refreshPhotoStrip()
    }

    override fun onResume() {
        super.onResume()
        if (::textNoPhotos.isInitialized) {
            refreshPhotoStrip()
        }
    }

    // ----------------------------------------------------------------
    // Inline preview
    // ----------------------------------------------------------------

    private fun showInPreviewPanel(path: String) {
        previewPanel.visibility = View.VISIBLE
        previewImage.setImageResource(R.drawable.ic_image)

        try {
            when {
                path.startsWith("content://") ->
                    previewImage.setImageURI(Uri.parse(path))
                path.startsWith("file://") ->
                    previewImage.setImageURI(Uri.parse(path))
                else -> {
                    val file = File(path)
                    if (file.exists()) {
                        previewImage.setImageURI(Uri.fromFile(file))
                    }
                }
            }
        } catch (t: Throwable) {
            Log.e(tag, "Failed to load preview", t)
        }
    }

    // ----------------------------------------------------------------
    // Load / Save / Delete
    // ----------------------------------------------------------------

    private fun loadNote() {
        viewLifecycleOwner.lifecycleScope.launch {
            val note = repo.getNote(noteId) ?: return@launch
            editTitle.setText(note.title)
            editBody.setText(note.body)

            val projects = resources.getStringArray(R.array.project_options)
            val idx = projects.indexOfFirst { it.equals(note.projectName, ignoreCase = true) }
            if (idx >= 0) projectSpinner.setSelection(idx)

            existingPhotos.clear()
            existingPhotos += repo.getPhotos(noteId)
            refreshPhotoStrip()
        }
    }

    private fun saveNote() {
        val title = editTitle.text?.toString()?.trim().orEmpty()
        val body = editBody.text?.toString().orEmpty()
        val project = projectSpinner.selectedItem?.toString().orEmpty()

        if (title.isBlank() && body.isBlank() && pendingUris.isEmpty()) {
            Toast.makeText(requireContext(), "Add a title or note text", Toast.LENGTH_SHORT).show()
            return
        }

        viewLifecycleOwner.lifecycleScope.launch {
            if (isNew) {
                repo.createNote(title, body, project, pendingUris.toList())
            } else {
                repo.updateNote(noteId, title, body, project)
            }
            findNavController().popBackStack()
        }
    }

    private fun confirmDelete() {
        MaterialAlertDialogBuilder(requireContext())
            .setTitle("Delete note?")
            .setMessage("This note and its photos will be removed.")
            .setPositiveButton("Delete") { _, _ ->
                viewLifecycleOwner.lifecycleScope.launch {
                    repo.deleteNote(noteId)
                    findNavController().popBackStack()
                }
            }
            .setNegativeButton("Cancel", null)
            .show()
    }

    // ----------------------------------------------------------------
    // Photo handling
    // ----------------------------------------------------------------

    private fun onPhotoPicked(uri: Uri) {
        val totalNow = existingPhotos.size + pendingUris.size
        if (totalNow >= 3) {
            Toast.makeText(requireContext(), "Up to 3 photos per note.", Toast.LENGTH_SHORT).show()
            return
        }

        if (isNew) {
            pendingUris += uri
            refreshPhotoStrip()
            // Auto-show the newly added photo
            showInPreviewPanel(uri.toString())
        } else {
            viewLifecycleOwner.lifecycleScope.launch {
                if (repo.addPhoto(noteId, uri)) {
                    existingPhotos.clear()
                    existingPhotos += repo.getPhotos(noteId)
                    refreshPhotoStrip()
                    showInPreviewPanel(uri.toString())
                }
            }
        }
    }

    private fun removePhotoAt(index: Int) {
        val totalShown = existingPhotos.size + pendingUris.size
        if (index >= totalShown) return

        if (index < existingPhotos.size) {
            val photo = existingPhotos[index]
            viewLifecycleOwner.lifecycleScope.launch {
                repo.removePhoto(noteId, photo.id)
                existingPhotos.clear()
                existingPhotos += repo.getPhotos(noteId)
                refreshPhotoStrip()
                previewPanel.visibility = View.GONE
            }
        } else {
            val pendingIndex = index - existingPhotos.size
            pendingUris.removeAt(pendingIndex)
            refreshPhotoStrip()
            previewPanel.visibility = View.GONE
        }
    }

    private fun refreshPhotoStrip() {
        currentPaths.clear()
        existingPhotos.forEach { currentPaths += it.filePath }
        pendingUris.forEach { currentPaths += it.toString() }

        for (i in 0..2) {
            if (i < currentPaths.size) {
                photoFrames[i].visibility = View.VISIBLE

                try {
                    val path = currentPaths[i]
                    when {
                        path.startsWith("content://") ->
                            photoIcons[i].setImageURI(Uri.parse(path))
                        path.startsWith("file://") ->
                            photoIcons[i].setImageURI(Uri.parse(path))
                        else ->
                            photoIcons[i].setImageURI(Uri.fromFile(File(path)))
                    }
                } catch (_: Throwable) {
                    photoIcons[i].setImageResource(R.drawable.ic_image)
                }
            } else {
                photoFrames[i].visibility = View.GONE
                photoIcons[i].setImageResource(R.drawable.ic_image)
            }
        }

        textNoPhotos.visibility =
            if (currentPaths.isEmpty()) View.VISIBLE else View.GONE
    }
}