package com.example.blastpromobile.ui.photos

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material.icons.filled.Image
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.tooling.preview.Preview
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.blastpromobile.ui.components.BlastProScreen
import com.example.blastpromobile.ui.components.RedButton
import com.example.blastpromobile.ui.theme.BlastProMobileTheme
import com.example.blastpromobile.ui.theme.InkMuted

/** Full photo placeholder. UI only: no image is loaded and Remove just goes back. */
@Composable
fun PhotoViewerScreen(onBack: () -> Unit) {
    BlastProScreen(onBackClick = onBack) {
        Box(
            Modifier
                .weight(1f)
                .fillMaxWidth()
                .clip(RoundedCornerShape(8.dp))
                .background(Color(0xFF111827)),
            contentAlignment = Alignment.Center
        ) {
            Icon(Icons.Default.Image, contentDescription = null, tint = InkMuted, modifier = Modifier.size(72.dp))
            Text("Photo preview", color = InkMuted, fontSize = 14.sp, modifier = Modifier.align(Alignment.BottomCenter).padding(12.dp))
        }
        RedButton(
            text = "Remove Photo",
            onClick = onBack,
            icon = Icons.Default.Delete,
            modifier = Modifier.fillMaxWidth().padding(top = 12.dp)
        )
    }
}

@Preview(showBackground = true, widthDp = 360, heightDp = 720)
@Composable
private fun PhotoViewerPreview() {
    BlastProMobileTheme { PhotoViewerScreen(onBack = {}) }
}
