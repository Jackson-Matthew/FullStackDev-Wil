package com.example.blastpromobile.ui

import android.os.Bundle
import android.view.View
import android.widget.EditText
import com.example.blastpromobile.BuildConfig
import com.example.blastpromobile.R

class SettingsFragment : BaseFragment(R.layout.fragment_settings) {
    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        view.findViewById<EditText>(R.id.edit_api_address).setText(BuildConfig.API_BASE_URL)
    }
}
