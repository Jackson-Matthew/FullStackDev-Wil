package com.example.blastpromobile.ui

import android.os.Bundle
import android.view.View
import android.widget.EditText
import android.widget.Spinner
import android.widget.TextView
import androidx.navigation.fragment.findNavController
import com.example.blastpromobile.R
import com.google.android.material.dialog.MaterialAlertDialogBuilder

/** Note editor. UI only: nothing is saved. The sample notes below stand in for the local database. */
class NoteEditorFragment : BaseFragment(R.layout.fragment_note_editor) {

    private data class SampleNote(val title: String, val body: String, val projectIndex: Int, val photos: Int)

    private val sampleNotes = mapOf(
        "1" to SampleNote("Bench 2 loose rock", "Loose material along the east wall, check before charging.", 1, 2),
        "2" to SampleNote("Wet holes row C", "Water in holes 4 to 7, consider emulsion for this row.", 2, 1),
        "3" to SampleNote("Photo of face", "", 0, 3),
        "4" to SampleNote("Site meeting", "Confirmed exclusion zone and blast time with the site manager.", 3, 0)
    )

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        val nav = findNavController()
        val note = sampleNotes[requireArguments().getString("noteId")]

        view.findViewById<TextView>(R.id.text_heading).text = if (note == null) "New Note" else "Edit Note"
        view.findViewById<EditText>(R.id.edit_title).setText(note?.title.orEmpty())
        view.findViewById<EditText>(R.id.edit_body).setText(note?.body.orEmpty())
        view.findViewById<Spinner>(R.id.spinner_project).setSelection(note?.projectIndex ?: 0)
        view.findViewById<View>(R.id.btn_delete).visibility = if (note == null) View.GONE else View.VISIBLE

        // Photo thumbnails: the first N are shown, tapping opens the viewer, the X removes one.
        val thumbs = listOf(R.id.thumb_1, R.id.thumb_2, R.id.thumb_3).map { view.findViewById<View>(it) }
        val removes = listOf(R.id.thumb_1_remove, R.id.thumb_2_remove, R.id.thumb_3_remove)
        val noPhotosText = view.findViewById<View>(R.id.text_no_photos)

        fun refreshEmptyText() {
            noPhotosText.visibility = if (thumbs.none { it.visibility == View.VISIBLE }) View.VISIBLE else View.GONE
        }

        thumbs.forEachIndexed { index, thumb ->
            thumb.visibility = if (index < (note?.photos ?: 0)) View.VISIBLE else View.GONE
            thumb.setOnClickListener { nav.safeNavigate(R.id.action_editor_to_photo) }
            view.findViewById<View>(removes[index]).setOnClickListener {
                thumb.visibility = View.GONE
                refreshEmptyText()
            }
        }
        refreshEmptyText()

        // Take Photo and Gallery just reveal the next empty thumbnail for now.
        val addPhoto = View.OnClickListener {
            thumbs.firstOrNull { it.visibility == View.GONE }?.visibility = View.VISIBLE
            refreshEmptyText()
        }
        view.findViewById<View>(R.id.btn_take_photo).setOnClickListener(addPhoto)
        view.findViewById<View>(R.id.btn_gallery).setOnClickListener(addPhoto)

        view.findViewById<View>(R.id.btn_save).setOnClickListener { nav.safePop() }
        view.findViewById<View>(R.id.btn_delete).setOnClickListener {
            MaterialAlertDialogBuilder(requireContext())
                .setTitle("Delete note?")
                .setMessage("This note and its photos will be removed.")
                .setPositiveButton("Delete") { _, _ -> nav.safePop() }
                .setNegativeButton("Cancel", null)
                .show()
        }
    }
}
