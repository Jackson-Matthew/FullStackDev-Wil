package com.example.blastpromobile.ui

import android.os.Bundle
import android.view.View
import androidx.core.os.bundleOf
import androidx.navigation.fragment.findNavController
import com.example.blastpromobile.R

/** Notes home. UI only: the cards are sample notes defined in fragment_notes.xml. */
class NotesFragment : BaseFragment(R.layout.fragment_notes) {

    private val cardToNoteId = mapOf(
        R.id.card_note_1 to "1",
        R.id.card_note_2 to "2",
        R.id.card_note_3 to "3",
        R.id.card_note_4 to "4"
    )

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        cardToNoteId.forEach { (cardId, noteId) ->
            view.findViewById<View>(cardId).setOnClickListener {
                findNavController().safeNavigate(R.id.action_notes_to_editor, bundleOf("noteId" to noteId))
            }
        }
    }
}
