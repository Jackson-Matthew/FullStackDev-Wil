package com.example.blastpromobile.ui

import android.os.Bundle
import android.view.View
import android.widget.ArrayAdapter
import android.widget.EditText
import android.widget.Spinner
import android.widget.Toast
import androidx.lifecycle.lifecycleScope
import androidx.navigation.fragment.findNavController
import com.example.blastpromobile.R
import com.example.blastpromobile.data.RemoteRepository
import kotlinx.coroutines.launch

class NewProjectFragment : BaseFragment(R.layout.fragment_new_project) {
    private lateinit var repo: RemoteRepository

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        repo = RemoteRepository(requireContext().applicationContext)
        val name = view.findViewById<EditText>(R.id.edit_project_name)
        val site = view.findViewById<EditText>(R.id.edit_project_site)
        val type = view.findViewById<Spinner>(R.id.spinner_blast_type)
        type.adapter = ArrayAdapter(requireContext(), R.layout.item_project_spinner, BLAST_TYPES)
            .also { it.setDropDownViewResource(R.layout.item_project_spinner) }

        val create = view.findViewById<View>(R.id.btn_create)
        create.setOnClickListener {
            val n = name.text.toString().trim()
            val s = site.text.toString().trim()
            if (n.isBlank() || s.isBlank()) {
                toast("Enter a name and site location")
                return@setOnClickListener
            }
            create.isEnabled = false
            viewLifecycleOwner.lifecycleScope.launch {
                try {
                    repo.createProject(n, s, type.selectedItem as String)
                    findNavController().safePop()
                } catch (error: Exception) {
                    create.isEnabled = true
                    toast(error.message ?: "Could not create project")
                }
            }
        }
    }

    private fun toast(message: String) = Toast.makeText(requireContext(), message, Toast.LENGTH_LONG).show()

    private companion object {
        val BLAST_TYPES = listOf("Production", "Pre-split", "Trim", "Development", "Trench")
    }
}
