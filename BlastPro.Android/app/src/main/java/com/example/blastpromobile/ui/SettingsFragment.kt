package com.example.blastpromobile.ui

import android.os.Bundle
import android.view.View
import android.widget.TextView
import androidx.core.content.ContextCompat
import androidx.lifecycle.lifecycleScope
import androidx.navigation.fragment.findNavController
import com.example.blastpromobile.R
import com.example.blastpromobile.data.MobileSession
import com.example.blastpromobile.data.RemoteRepository
import com.google.android.material.button.MaterialButton
import kotlinx.coroutines.launch

class SettingsFragment : BaseFragment(R.layout.fragment_settings) {
    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        val dot = view.findViewById<View>(R.id.api_status_dot)
        val text = view.findViewById<TextView>(R.id.api_status_text)
        val refresh = view.findViewById<MaterialButton>(R.id.btn_refresh)

        fun checkStatus() {
            refresh.setBusy(true, "Checking…")
            text.text = "Checking API…"
            dot.backgroundTintList = ContextCompat.getColorStateList(requireContext(), R.color.status_grey)
            viewLifecycleOwner.lifecycleScope.launch {
                val online = RemoteRepository(requireContext().applicationContext).isOnline()
                dot.backgroundTintList = ContextCompat.getColorStateList(
                    requireContext(), if (online) R.color.status_green else R.color.blast_red)
                text.text = if (online) "API online" else "API offline"
                refresh.setBusy(false, "")
            }
        }
        refresh.setOnClickListener { checkStatus() }
        checkStatus()

        view.findViewById<View>(R.id.btn_logout).setOnClickListener {
            MobileSession.signOut()
            findNavController().safeNavigate(R.id.action_global_login)
        }
    }
}
