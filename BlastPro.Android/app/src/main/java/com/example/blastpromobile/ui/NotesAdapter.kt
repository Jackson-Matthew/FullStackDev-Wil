package com.example.blastpromobile.ui

import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.ImageView
import android.widget.TextView
import androidx.core.content.ContextCompat
import androidx.recyclerview.widget.RecyclerView
import com.example.blastpromobile.R
import com.example.blastpromobile.data.local.Note
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale

class NotesAdapter(
    private val onOpen: (Note) -> Unit
) : RecyclerView.Adapter<NotesAdapter.NoteViewHolder>() {

    private val items = mutableListOf<Note>()
    private val photoCounts = mutableMapOf<Long, Int>()
    private val dateFmt = SimpleDateFormat("MMM d, yyyy", Locale.getDefault())

    fun submit(newItems: List<Note>, counts: Map<Long, Int>) {
        items.clear()
        items.addAll(newItems)
        photoCounts.clear()
        photoCounts.putAll(counts)
        notifyDataSetChanged()
    }

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): NoteViewHolder {
        val v = LayoutInflater.from(parent.context)
            .inflate(R.layout.item_note, parent, false)
        return NoteViewHolder(v)
    }

    override fun onBindViewHolder(holder: NoteViewHolder, position: Int) {
        val note = items[position]
        holder.bind(note, photoCounts[note.id] ?: 0, onOpen)
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

        fun bind(note: Note, count: Int, onOpen: (Note) -> Unit) {
            title.text = note.title.ifBlank { "(Untitled note)" }
            preview.text = note.body.ifBlank { "No text" }
            project.text = note.projectName.ifBlank { "No project" }
            date.text = dateFmt.format(Date(note.updatedAtMillis))

            chip.text = when (note.status) {
                Note.STATUS_SYNCED -> "Synced"
                Note.STATUS_RETRY -> "Needs retry"
                Note.STATUS_CONFLICT -> "Conflict"
                else -> "Saved on device"
            }

            val colorRes = when (note.status) {
                Note.STATUS_SYNCED -> R.color.status_green
                Note.STATUS_RETRY -> R.color.status_orange
                Note.STATUS_CONFLICT -> R.color.blast_red
                else -> R.color.status_grey
            }
            chip.backgroundTintList =
                ContextCompat.getColorStateList(itemView.context, colorRes)

            photoCount.text = count.toString()
            card.setOnClickListener { onOpen(note) }
        }
    }
}