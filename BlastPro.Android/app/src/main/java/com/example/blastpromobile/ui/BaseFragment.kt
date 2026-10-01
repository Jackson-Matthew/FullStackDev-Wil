package com.example.blastpromobile.ui

import android.os.Bundle
import android.view.View
import androidx.annotation.LayoutRes
import androidx.fragment.app.Fragment
import androidx.navigation.fragment.findNavController
import com.example.blastpromobile.R

/** Wires the shared top bar buttons (hamburger and back arrow) for every screen that has them. */
open class BaseFragment(@LayoutRes layoutId: Int) : Fragment(layoutId) {

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        view.findViewById<View>(R.id.btn_menu)?.setOnClickListener {
            (requireActivity() as MainActivity).openMenu()
        }
        view.findViewById<View>(R.id.btn_back)?.setOnClickListener {
            findNavController().safePop()
        }
    }
}
