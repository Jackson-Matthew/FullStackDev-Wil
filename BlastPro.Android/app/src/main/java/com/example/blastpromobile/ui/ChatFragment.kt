package com.example.blastpromobile.ui

import android.os.Bundle
import android.view.View
import androidx.core.os.bundleOf
import androidx.navigation.fragment.findNavController
import com.example.blastpromobile.R

/**
 * Optional module: UI only, the conversation is sample text in fragment_chat.xml and Send does nothing.
 * Delete this class, fragment_chat.xml, its nav_graph entry and Config.CHAT_ENABLED to remove chat.
 */
class ChatFragment : BaseFragment(R.layout.fragment_chat) {

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        view.findViewById<View>(R.id.source_note_2).setOnClickListener {
            findNavController().safeNavigate(R.id.action_chat_to_editor, bundleOf("noteId" to "2"))
        }
    }
}
