package com.example.blastpromobile.ui.components

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.ColumnScope
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.navigationBarsPadding
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.statusBarsPadding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.BasicTextField
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.ArrowDropDown
import androidx.compose.material.icons.filled.Menu
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.SolidColor
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.text.SpanStyle
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.buildAnnotatedString
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.text.input.VisualTransformation
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.text.withStyle
import androidx.compose.ui.unit.TextUnit
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.blastpromobile.ui.theme.BlastRed
import com.example.blastpromobile.ui.theme.ContourLine
import com.example.blastpromobile.ui.theme.FieldWhite
import com.example.blastpromobile.ui.theme.InkDark
import com.example.blastpromobile.ui.theme.InkMuted
import com.example.blastpromobile.ui.theme.ScreenBackground
import com.example.blastpromobile.ui.theme.TopBarBackground

@Composable
fun BlastProLogo(fontSize: TextUnit = 24.sp) {
    Text(
        text = buildAnnotatedString {
            withStyle(SpanStyle(color = BlastRed, fontWeight = FontWeight.Bold)) { append("Blast") }
            withStyle(SpanStyle(color = Color.White, fontWeight = FontWeight.Bold)) { append("Pro") }
        },
        fontSize = fontSize
    )
}

/** Dark topographic-style background used behind every screen. */
@Composable
fun ContourBackground(modifier: Modifier = Modifier, content: @Composable () -> Unit) {
    Box(modifier.fillMaxSize().background(ScreenBackground)) {
        Canvas(Modifier.fillMaxSize()) {
            val stroke = Stroke(width = 1.5f)
            for (i in 1..16) {
                val w = size.width * (0.35f + i * 0.09f)
                val h = size.height * (0.12f + i * 0.055f)
                drawOval(
                    color = ContourLine,
                    topLeft = Offset(size.width * 0.75f - w / 2, size.height * 0.3f - h / 2),
                    size = Size(w, h),
                    style = stroke
                )
                drawOval(
                    color = ContourLine,
                    topLeft = Offset(size.width * 0.1f - w / 3, size.height * 0.85f - h / 3),
                    size = Size(w * 0.7f, h * 0.7f),
                    style = stroke
                )
            }
        }
        content()
    }
}

@Composable
fun BlastProTopBar(onMenuClick: (() -> Unit)? = null, onBackClick: (() -> Unit)? = null) {
    Row(
        Modifier
            .fillMaxWidth()
            .background(TopBarBackground)
            .statusBarsPadding()
            .height(56.dp)
            .padding(start = if (onBackClick != null) 4.dp else 16.dp, end = 4.dp),
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.SpaceBetween
    ) {
        Row(verticalAlignment = Alignment.CenterVertically) {
            if (onBackClick != null) {
                IconButton(onClick = onBackClick) {
                    Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = "Back", tint = Color.White)
                }
            }
            BlastProLogo()
        }
        if (onMenuClick != null) {
            IconButton(onClick = onMenuClick) {
                Icon(Icons.Default.Menu, contentDescription = "Menu", tint = Color.White)
            }
        }
    }
}

/** Background + top bar + padded content column shared by the main screens. */
@Composable
fun BlastProScreen(
    onMenuClick: (() -> Unit)? = null,
    onBackClick: (() -> Unit)? = null,
    content: @Composable ColumnScope.() -> Unit
) {
    ContourBackground {
        Column(Modifier.fillMaxSize()) {
            BlastProTopBar(onMenuClick, onBackClick)
            Column(
                Modifier
                    .weight(1f)
                    .fillMaxWidth()
                    .padding(horizontal = 20.dp, vertical = 16.dp)
            ) { content() }
        }
    }
}

@Composable
fun ScreenHeading(title: String, subtitle: String) {
    Text(title, color = Color.White, fontSize = 24.sp, fontWeight = FontWeight.Bold)
    Text(subtitle, color = Color.White, fontSize = 13.sp, modifier = Modifier.padding(top = 2.dp, bottom = 12.dp))
}

@Composable
fun FieldLabel(text: String) {
    Text(
        text = text.uppercase(),
        color = Color.White,
        fontSize = 12.sp,
        letterSpacing = 0.5.sp,
        modifier = Modifier.padding(top = 10.dp, bottom = 4.dp)
    )
}

@Composable
fun WhiteTextField(
    value: String,
    onValueChange: (String) -> Unit,
    modifier: Modifier = Modifier,
    placeholder: String = "",
    singleLine: Boolean = true,
    readOnly: Boolean = false,
    isPassword: Boolean = false,
    minHeight: androidx.compose.ui.unit.Dp = 50.dp
) {
    BasicTextField(
        value = value,
        onValueChange = onValueChange,
        modifier = modifier.fillMaxWidth(),
        singleLine = singleLine,
        readOnly = readOnly,
        visualTransformation = if (isPassword) PasswordVisualTransformation() else VisualTransformation.None,
        keyboardOptions = KeyboardOptions(keyboardType = if (isPassword) KeyboardType.Password else KeyboardType.Text),
        textStyle = TextStyle(color = InkDark, fontSize = 16.sp),
        cursorBrush = SolidColor(BlastRed),
        decorationBox = { inner ->
            Box(
                Modifier
                    .fillMaxWidth()
                    .height(minHeight)
                    .clip(RoundedCornerShape(6.dp))
                    .background(FieldWhite)
                    .padding(horizontal = 12.dp, vertical = if (singleLine) 0.dp else 10.dp),
                contentAlignment = if (singleLine) Alignment.CenterStart else Alignment.TopStart
            ) {
                if (value.isEmpty() && placeholder.isNotEmpty()) {
                    Text(placeholder, color = InkMuted, fontSize = 16.sp)
                }
                inner()
            }
        }
    )
}

/** White dropdown used for the project filter and project picker. UI only: options are sample text. */
@Composable
fun WhiteDropdown(selected: String, options: List<String>, onSelected: (String) -> Unit) {
    var expanded by remember { mutableStateOf(false) }
    Box {
        Row(
            Modifier
                .fillMaxWidth()
                .height(50.dp)
                .clip(RoundedCornerShape(6.dp))
                .background(FieldWhite)
                .clickable { expanded = true }
                .padding(start = 12.dp, end = 6.dp),
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.SpaceBetween
        ) {
            Text(selected, color = InkDark, fontSize = 16.sp)
            Icon(Icons.Default.ArrowDropDown, contentDescription = null, tint = InkMuted)
        }
        DropdownMenu(expanded = expanded, onDismissRequest = { expanded = false }) {
            options.forEach { option ->
                DropdownMenuItem(
                    text = { Text(option, fontSize = 16.sp) },
                    onClick = {
                        onSelected(option)
                        expanded = false
                    }
                )
            }
        }
    }
}

@Composable
fun RedButton(
    text: String,
    onClick: () -> Unit,
    modifier: Modifier = Modifier,
    icon: ImageVector? = null
) {
    Row(
        modifier
            .clip(RoundedCornerShape(4.dp))
            .background(BlastRed)
            .clickable(onClick = onClick)
            .height(50.dp)
            .padding(horizontal = 12.dp),
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.Center
    ) {
        if (icon != null) {
            Icon(icon, contentDescription = null, tint = Color.White, modifier = Modifier.size(20.dp))
            Spacer(Modifier.width(6.dp))
        }
        Text(text, color = Color.White, fontSize = 14.sp, fontWeight = FontWeight.Medium, textAlign = TextAlign.Center)
    }
}

@Composable
fun StatusChip(text: String, color: Color) {
    Text(
        text = text.uppercase(),
        color = Color.White,
        fontSize = 11.sp,
        fontWeight = FontWeight.Bold,
        modifier = Modifier
            .clip(RoundedCornerShape(10.dp))
            .background(color)
            .padding(horizontal = 8.dp, vertical = 3.dp)
    )
}
