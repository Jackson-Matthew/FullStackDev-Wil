package com.example.blastpromobile.ui.login

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.tooling.preview.Preview
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.blastpromobile.ui.components.BlastProScreen
import com.example.blastpromobile.ui.components.FieldLabel
import com.example.blastpromobile.ui.components.RedButton
import com.example.blastpromobile.ui.components.WhiteTextField
import com.example.blastpromobile.ui.theme.BlastProMobileTheme

/** Dummy login: nothing is checked or sent. Login just opens the app. */
@Composable
fun LoginScreen(onLogin: () -> Unit) {
    var email by remember { mutableStateOf("") }
    var password by remember { mutableStateOf("") }

    BlastProScreen {
        Column(
            Modifier.fillMaxSize(),
            verticalArrangement = Arrangement.Center,
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            Text("Welcome to BlastPro!", color = Color.White, fontSize = 24.sp, fontWeight = FontWeight.Bold)
            Text(
                "Sign in to see your field notes and photos.",
                color = Color.White,
                fontSize = 14.sp,
                textAlign = TextAlign.Center,
                modifier = Modifier.padding(top = 4.dp, bottom = 24.dp)
            )

            Column(Modifier.fillMaxWidth()) {
                FieldLabel("Enter email")
                WhiteTextField(email, { email = it })
                FieldLabel("Enter password")
                WhiteTextField(password, { password = it }, isPassword = true)
            }

            RedButton("Login", onLogin, modifier = Modifier.fillMaxWidth().padding(top = 24.dp))
        }
    }
}

@Preview(showBackground = true, widthDp = 360, heightDp = 720)
@Composable
private fun LoginPreview() {
    BlastProMobileTheme { LoginScreen(onLogin = {}) }
}
