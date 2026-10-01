package com.example.blastpromobile.ui

import android.os.Bundle
import android.view.View
import androidx.navigation.fragment.findNavController
import com.example.blastpromobile.R

/** Full photo placeholder. UI only: Remove just goes back. */
class PhotoViewerFragment : BaseFragment(R.layout.fragment_photo_viewer) {

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        view.findViewById<View>(R.id.btn_remove).setOnClickListener { findNavController().safePop() }
    }
}
