package com.example.blastpromobile.ui

import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.ImageView
import android.widget.TextView
import androidx.core.content.ContextCompat
import androidx.recyclerview.widget.RecyclerView
import com.example.blastpromobile.R
import com.example.blastpromobile.data.RemoteNote
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale
import java.time.Instant

class NotesAdapter(
    private val onOpen: (RemoteNote) -> Unit
) : RecyclerView.Adapter<NotesAdapter.NoteViewHolder>() {

    private val items = mutableListOf<RemoteNote>()
    private val projectNames = mutableMapOf<Int, String>()
    private val dateFmt = SimpleDateFormat("MMM d, yyyy", Locale.getDefault())

    fun submit(newItems: List<RemoteNote>, names: Map<Int, String>) {
        items.clear()
        items.addAll(newItems)
        projectNames.clear()
        projectNames.putAll(names)
        notifyDataSetChanged()
    }

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): NoteViewHolder {
        val v = LayoutInflater.from(parent.context)
            .inflate(R.layout.item_note, parent, false)
        return NoteViewHolder(v)
    }

    override fun onBindViewHolder(holder: NoteViewHolder, position: Int) {
        val note = items[position]
        holder.bind(note, projectNames[note.projectId].orEmpty(), onOpen)
    }

    override fun getItemCount(): Int = items.size

    inner class NoteViewHolder(itemView: View) : RecyclerView.ViewHolder(itemView) {
        private val card: View = itemView.findViewById(R.id.card_note_root)
        private val title: TextView = itemView.findViewById(R.id.note_title)
        private val preview: TextView = itemView.findViewById(R.id.note_preview)
        private val chip: TextView = itemView.findViewById(R.id.note_chip)
        private val date: TextView = itemView.findViewById(R.id.note_date)
        private val project: TextView = itemView.findViewById(R.id.note_project)
        private val photoCount: TextView = itemView.findViewById(R.id.note_photo_count)

        fun bind(note: RemoteNote, projectName: String, onOpen: (RemoteNote) -> Unit) {
            title.text = note.title.ifBlank { "(Untitled note)" }
            preview.text = note.body.ifBlank { "No text" }
            project.text = projectName
            date.text = runCatching { dateFmt.format(Date.from(Instant.parse(note.updatedAtUtc))) }.getOrDefault("")
            chip.text = "Synced"
            val colorRes = R.color.status_green
            chip.backgroundTintList =
                ContextCompat.getColorStateList(itemView.context, colorRes)

            photoCount.text = note.photos.size.toString()
            card.setOnClickListener { onOpen(note) }
        }
    }
}
