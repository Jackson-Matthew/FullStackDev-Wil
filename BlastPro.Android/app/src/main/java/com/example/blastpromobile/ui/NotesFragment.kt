package com.example.blastpromobile.ui

import android.os.Bundle
import android.view.View
import android.widget.AdapterView
import android.widget.ArrayAdapter
import android.widget.EditText
import android.widget.LinearLayout
import android.widget.Spinner
import androidx.core.os.bundleOf
import androidx.core.widget.doAfterTextChanged
import androidx.lifecycle.lifecycleScope
import androidx.navigation.fragment.findNavController
import androidx.recyclerview.widget.LinearLayoutManager
import androidx.recyclerview.widget.RecyclerView
import com.example.blastpromobile.R
import com.example.blastpromobile.data.NoteRepository
import kotlinx.coroutines.flow.collectLatest
import kotlinx.coroutines.launch

class NotesFragment : BaseFragment(R.layout.fragment_notes) {

    private lateinit var repo: NoteRepository
    private lateinit var adapter: NotesAdapter
    private lateinit var recycler: RecyclerView
    private lateinit var empty: LinearLayout
    private lateinit var search: EditText
    private lateinit var filterSpinner: Spinner

    private var currentProject: String? = null
    private var currentQuery: String? = null

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)

        repo = NoteRepository(requireContext().applicationContext)

        recycler = view.findViewById(R.id.recycler_notes)
        empty = view.findViewById(R.id.empty_notes)
        search = view.findViewById(R.id.edit_search)
        filterSpinner = view.findViewById(R.id.spinner_project_filter)

        adapter = NotesAdapter { note ->
            findNavController().safeNavigate(
                R.id.action_notes_to_editor,
                bundleOf("noteId" to note.id.toString())
            )
        }
        recycler.layoutManager = LinearLayoutManager(requireContext())
        recycler.adapter = adapter

        ArrayAdapter.createFromResource(
            requireContext(),
            R.array.project_filter_options,
            android.R.layout.simple_spinner_item
        ).also { a ->
            a.setDropDownViewResource(android.R.layout.simple_spinner_dropdown_item)
            filterSpinner.adapter = a
        }

        filterSpinner.onItemSelectedListener = object : AdapterView.OnItemSelectedListener {
            override fun onItemSelected(parent: AdapterView<*>?, v: View?, pos: Int, id: Long) {
                currentProject = filterSpinner.selectedItem?.toString()
                refresh()
            }
            override fun onNothingSelected(parent: AdapterView<*>?) {}
        }

        search.doAfterTextChanged { text ->
            currentQuery = text?.toString()
            refresh()
        }

        refresh()
    }

    private fun refresh() {
        viewLifecycleOwner.lifecycleScope.launch {
            repo.observeNotes(currentProject, currentQuery).collectLatest { notes ->
                val counts = notes.associate { it.id to repo.countPhotos(it.id) }
                adapter.submit(notes, counts)
                empty.visibility = if (notes.isEmpty()) View.VISIBLE else View.GONE
                recycler.visibility = if (notes.isEmpty()) View.GONE else View.VISIBLE
            }
        }
    }
}