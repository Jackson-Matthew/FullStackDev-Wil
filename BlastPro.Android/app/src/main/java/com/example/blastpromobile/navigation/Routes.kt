package com.example.blastpromobile.navigation

object Routes {
    const val LOGIN = "login"
    const val NOTES = "notes"
    const val NOTE_EDITOR = "noteEditor/{noteId}"
    const val PHOTO_VIEWER = "photoViewer"
    const val CHAT = "chat"
    const val SETTINGS = "settings"

    const val NEW_NOTE_ID = "new"

    fun noteEditor(noteId: String) = "noteEditor/$noteId"
}
