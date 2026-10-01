package com.example.blastpromobile.ui

import android.os.Bundle
import android.view.View
import androidx.navigation.fragment.findNavController
import com.example.blastpromobile.R

/** Dummy login: nothing is checked or sent. Login just opens the app. */
class LoginFragment : BaseFragment(R.layout.fragment_login) {

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        view.findViewById<View>(R.id.btn_login).setOnClickListener {
            findNavController().safeNavigate(R.id.action_login_to_notes)
        }
    }
}
