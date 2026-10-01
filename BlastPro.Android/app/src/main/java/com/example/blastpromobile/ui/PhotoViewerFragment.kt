package com.example.blastpromobile.ui

import android.net.Uri
import android.os.Bundle
import android.util.Log
import android.view.View
import android.widget.ImageView
import androidx.navigation.fragment.findNavController
import coil.load
import com.example.blastpromobile.R
import java.io.File

class PhotoViewerFragment : BaseFragment(R.layout.fragment_photo_viewer) {

    private val tag = "PhotoViewer"

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)

        view.findViewById<View>(R.id.btn_remove).setOnClickListener {
            findNavController().safePop()
        }
    }

    override fun onResume() {
        super.onResume()
        loadCurrentPhoto()
    }

    private fun loadCurrentPhoto() {
        val view = view ?: return
        val image = view.findViewById<ImageView>(R.id.image_photo) ?: return

        // Reset to placeholder so stale images don't linger
        image.setImageResource(R.drawable.ic_image)

        val path = arguments?.getString("photoPath")
        Log.d(tag, "Loading path on resume: $path")

        if (path.isNullOrBlank()) return

        when {
            path.startsWith("content://") -> {
                image.load(Uri.parse(path)) {
                    placeholder(R.drawable.ic_image)
                    error(R.drawable.ic_image)
                }
            }
            path.startsWith("file://") -> {
                image.load(Uri.parse(path)) {
                    placeholder(R.drawable.ic_image)
                    error(R.drawable.ic_image)
                }
            }
            else -> {
                val file = File(path)
                Log.d(tag, "File exists: ${file.exists()}, size: ${file.length()}")
                if (file.exists()) {
                    image.load(file) {
                        placeholder(R.drawable.ic_image)
                        error(R.drawable.ic_image)
                    }
                }
            }
        }
    }
}