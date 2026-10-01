package com.example.blastpromobile.ui

import android.content.Context
import android.os.Bundle
import android.view.View
import android.widget.EditText
import android.widget.Toast
import androidx.navigation.fragment.findNavController
import com.example.blastpromobile.R

/** Temporary hardcoded login — fields are pre-filled for quick testing. */
class LoginFragment : BaseFragment(R.layout.fragment_login) {

    // TODO: remove before final submission — hardcoded for dev only
    private val defaultEmail = "admin@xploma.co.za"
    private val defaultPassword = "Admin@12345!"

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)

        val email = view.findViewById<EditText>(R.id.edit_email)
        val password = view.findViewById<EditText>(R.id.edit_password)

        // Pre-fill the fields
        if (email.text.isNullOrBlank()) {
            email.setText(defaultEmail)
        }
        if (password.text.isNullOrBlank()) {
            password.setText(defaultPassword)
        }

        view.findViewById<View>(R.id.btn_login).setOnClickListener {
            val e = email.text?.toString()?.trim().orEmpty()
            val p = password.text?.toString().orEmpty()

            if (e.isEmpty() || p.isEmpty()) {
                Toast.makeText(requireContext(), "Enter email and password", Toast.LENGTH_SHORT).show()
                return@setOnClickListener
            }

            // Temporary: only stores the email locally. No API check yet.
            requireContext()
                .getSharedPreferences("blastpro_prefs", Context.MODE_PRIVATE)
                .edit()
                .putString("signed_in_email", e)
                .apply()

            findNavController().safeNavigate(R.id.action_login_to_notes)
        }
    }
}