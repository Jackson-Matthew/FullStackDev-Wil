package com.example.blastpromobile.ui

import android.os.Bundle
import android.view.View
import android.widget.EditText
import android.widget.Toast
import androidx.lifecycle.lifecycleScope
import androidx.navigation.fragment.findNavController
import com.example.blastpromobile.R
import com.example.blastpromobile.data.RemoteRepository
import com.google.android.material.button.MaterialButton
import kotlinx.coroutines.launch

/** Sign in with an existing web account. Account creation is handled by the web app. */
class LoginFragment : BaseFragment(R.layout.fragment_login) {
    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        val email = view.findViewById<EditText>(R.id.edit_email)
        val password = view.findViewById<EditText>(R.id.edit_password)
        val button = view.findViewById<MaterialButton>(R.id.btn_login)
        button.setOnClickListener {
            val address = email.text?.toString()?.trim().orEmpty()
            val secret = password.text?.toString().orEmpty()
            if (address.isBlank() || secret.isBlank()) {
                Toast.makeText(requireContext(), "Enter email and password", Toast.LENGTH_SHORT).show()
                return@setOnClickListener
            }
            button.setBusy(true, "Signing in…")
            viewLifecycleOwner.lifecycleScope.launch {
                try {
                    RemoteRepository(requireContext().applicationContext).login(address, secret)
                    password.text?.clear()
                    findNavController().safeNavigate(R.id.action_login_to_notes)
                } catch (error: Exception) {
                    Toast.makeText(requireContext(), error.message ?: "Sign in failed", Toast.LENGTH_LONG).show()
                } finally { button.setBusy(false, "") }
            }
        }
    }
}
