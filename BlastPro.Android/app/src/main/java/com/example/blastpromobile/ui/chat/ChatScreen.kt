package com.example.blastpromobile.ui.chat

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.Send
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.tooling.preview.Preview
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.blastpromobile.ui.components.BlastProScreen
import com.example.blastpromobile.ui.components.RedButton
import com.example.blastpromobile.ui.components.ScreenHeading
import com.example.blastpromobile.ui.components.WhiteTextField
import com.example.blastpromobile.ui.theme.BlastProMobileTheme
import com.example.blastpromobile.ui.theme.BlastRed
import com.example.blastpromobile.ui.theme.InkDark
import com.example.blastpromobile.ui.theme.StatusOrange

/**
 * Optional module: everything for Chat lives in this package so it can be deleted
 * (with its route in AppNavHost and Config.CHAT_ENABLED) without touching notes or photos.
 * UI only: the messages below are sample text and Send does nothing.
 */
private data class SampleSource(val noteId: String, val title: String, val excerpt: String)

private sealed interface SampleMessage {
    data class Question(val text: String) : SampleMessage
    data class Answer(val text: String, val sources: List<SampleSource>) : SampleMessage
    data class NoAnswer(val text: String) : SampleMessage
}

private val sampleConversation = listOf(
    SampleMessage.Question("Which notes mention wet holes?"),
    SampleMessage.Answer(
        "One note mentions water in holes on row C.",
        listOf(SampleSource("2", "Wet holes row C", "Water in holes 4 to 7, consider emulsion for this row."))
    ),
    SampleMessage.Question("What was decided about the vibration limit?"),
    SampleMessage.NoAnswer("Your notes do not contain an answer to that question.")
)

@Composable
fun ChatScreen(
    onMenuClick: () -> Unit,
    onOpenNote: (String) -> Unit,
    aiAvailable: Boolean = true
) {
    var input by remember { mutableStateOf("") }

    BlastProScreen(onMenuClick = onMenuClick) {
        ScreenHeading("Chat", "Ask questions about your notes. Chat only finds and summarises notes.")

        if (!aiAvailable) AiUnavailableBanner()

        LazyColumn(
            Modifier.weight(1f),
            verticalArrangement = Arrangement.spacedBy(10.dp)
        ) {
            items(sampleConversation.size) { index ->
                when (val message = sampleConversation[index]) {
                    is SampleMessage.Question -> QuestionBubble(message.text)
                    is SampleMessage.Answer -> AnswerBubble(message.text, message.sources, onOpenNote)
                    is SampleMessage.NoAnswer -> AnswerBubble(message.text, emptyList(), onOpenNote)
                }
            }
        }

        Row(
            Modifier.fillMaxWidth().padding(top = 8.dp),
            horizontalArrangement = Arrangement.spacedBy(8.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            WhiteTextField(input, { input = it }, modifier = Modifier.weight(1f), placeholder = "Ask about your notes")
            RedButton("Send", {}, icon = Icons.AutoMirrored.Filled.Send)
        }
    }
}

@Composable
private fun QuestionBubble(text: String) {
    Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.End) {
        Text(
            text,
            color = Color.White,
            fontSize = 16.sp,
            modifier = Modifier
                .clip(RoundedCornerShape(10.dp))
                .background(BlastRed)
                .padding(horizontal = 12.dp, vertical = 8.dp)
        )
    }
}

@Composable
private fun AnswerBubble(text: String, sources: List<SampleSource>, onOpenNote: (String) -> Unit) {
    Column(
        Modifier
            .fillMaxWidth(0.9f)
            .clip(RoundedCornerShape(10.dp))
            .background(Color.White)
            .padding(16.dp)
    ) {
        Text(text, color = InkDark, fontSize = 16.sp)
        sources.forEach { source ->
            Column(
                Modifier
                    .padding(top = 8.dp)
                    .fillMaxWidth()
                    .clip(RoundedCornerShape(6.dp))
                    .background(Color(0xFFF3F4F6))
                    .clickable { onOpenNote(source.noteId) }
                    .padding(8.dp)
            ) {
                Text(source.title, color = BlastRed, fontSize = 14.sp, fontWeight = FontWeight.Bold)
                Text(source.excerpt, color = InkDark, fontSize = 13.sp)
            }
        }
    }
}

@Composable
private fun AiUnavailableBanner() {
    Text(
        "Chat is unavailable right now. Your notes and photos still work.",
        color = Color.White,
        fontSize = 14.sp,
        modifier = Modifier
            .fillMaxWidth()
            .padding(bottom = 8.dp)
            .clip(RoundedCornerShape(6.dp))
            .background(StatusOrange)
            .padding(10.dp)
    )
}

@Preview(showBackground = true, widthDp = 360, heightDp = 720)
@Composable
private fun ChatPreview() {
    BlastProMobileTheme { ChatScreen(onMenuClick = {}, onOpenNote = {}) }
}

@Preview(showBackground = true, widthDp = 360, heightDp = 720)
@Composable
private fun ChatUnavailablePreview() {
    BlastProMobileTheme { ChatScreen(onMenuClick = {}, onOpenNote = {}, aiAvailable = false) }
}
