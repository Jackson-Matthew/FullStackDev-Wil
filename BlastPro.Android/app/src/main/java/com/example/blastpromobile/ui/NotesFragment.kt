package com.example.blastpromobile.ui

import android.os.Bundle
import android.view.View
import android.widget.AdapterView
import android.widget.ArrayAdapter
import android.widget.EditText
import android.widget.LinearLayout
import android.widget.Spinner
import android.widget.Toast
import androidx.core.os.bundleOf
import androidx.core.widget.doAfterTextChanged
import androidx.lifecycle.lifecycleScope
import androidx.navigation.fragment.findNavController
import androidx.recyclerview.widget.LinearLayoutManager
import androidx.recyclerview.widget.RecyclerView
import com.example.blastpromobile.R
import com.example.blastpromobile.data.ApiFailure
import com.example.blastpromobile.data.RemoteNote
import com.example.blastpromobile.data.RemoteProject
import com.example.blastpromobile.data.RemoteRepository
import kotlinx.coroutines.launch

class NotesFragment : BaseFragment(R.layout.fragment_notes) {
    private lateinit var repo: RemoteRepository
    private lateinit var adapter: NotesAdapter
    private lateinit var recycler: RecyclerView
    private lateinit var empty: LinearLayout
    private lateinit var search: EditText
    private lateinit var filterSpinner: Spinner
    private var projects = listOf<RemoteProject>()
    private var notes = listOf<RemoteNote>()

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        repo = RemoteRepository(requireContext().applicationContext)
        recycler = view.findViewById(R.id.recycler_notes)
        empty = view.findViewById(R.id.empty_notes)
        search = view.findViewById(R.id.edit_search)
        filterSpinner = view.findViewById(R.id.spinner_project_filter)
        adapter = NotesAdapter { note ->
            findNavController().safeNavigate(R.id.action_notes_to_editor,
                bundleOf("noteId" to note.id.toString(), "projectId" to note.projectId))
        }
        recycler.layoutManager = LinearLayoutManager(requireContext())
        recycler.adapter = adapter
        filterSpinner.onItemSelectedListener = object : AdapterView.OnItemSelectedListener {
            override fun onItemSelected(parent: AdapterView<*>?, view: View?, position: Int, id: Long) = showFiltered()
            override fun onNothingSelected(parent: AdapterView<*>?) = Unit
        }
        search.doAfterTextChanged { showFiltered() }
        view.findViewById<View>(R.id.btn_create_project).setOnClickListener {
            findNavController().safeNavigate(R.id.action_notes_to_new_project)
        }
    }

    override fun onResume() {
        super.onResume()
        refresh()
    }

    fun refresh() {
        viewLifecycleOwner.lifecycleScope.launch {
            try {
                projects = repo.projects()
                notes = projects.flatMap { repo.notes(it.id) }
                    .sortedByDescending { it.updatedAtUtc }
                val names = listOf("All projects") + projects.map { it.name }
                val selected = filterSpinner.selectedItem?.toString()
                filterSpinner.adapter = ArrayAdapter(requireContext(), R.layout.item_project_spinner, names).also {
                    it.setDropDownViewResource(R.layout.item_project_spinner)
                }
                filterSpinner.setSelection(names.indexOf(selected).coerceAtLeast(0))
                showFiltered()
            } catch (error: Exception) {
                if (error is ApiFailure && error.status == 401) {
                    findNavController().safeNavigate(R.id.action_global_login)
                } else Toast.makeText(requireContext(), error.message ?: "Could not load notes", Toast.LENGTH_LONG).show()
            }
        }
    }

    private fun showFiltered() {
        if (!::adapter.isInitialized) return
        val selectedProject = projects.getOrNull(filterSpinner.selectedItemPosition - 1)?.id
        val query = search.text?.toString()?.trim().orEmpty()
        val visible = notes.filter { note ->
            (selectedProject == null || note.projectId == selectedProject) &&
                (query.isBlank() || note.title.contains(query, true) || note.body.contains(query, true))
        }
        adapter.submit(visible, projects.associate { it.id to it.name })
        empty.visibility = if (visible.isEmpty()) View.VISIBLE else View.GONE
        recycler.visibility = if (visible.isEmpty()) View.GONE else View.VISIBLE
    }
}
