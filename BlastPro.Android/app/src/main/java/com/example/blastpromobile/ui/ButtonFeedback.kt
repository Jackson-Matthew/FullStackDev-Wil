package com.example.blastpromobile.ui

import com.example.blastpromobile.R

import android.annotation.SuppressLint
import android.view.HapticFeedbackConstants
import android.view.MotionEvent
import android.view.View
import android.view.ViewGroup
import com.google.android.material.button.MaterialButton

/** Shrinks the view slightly and gives a short vibration while it is pressed. Never consumes the touch. */
@SuppressLint("ClickableViewAccessibility")
fun View.addPressFeedback() {
    setOnTouchListener { v, event ->
        when (event.actionMasked) {
            MotionEvent.ACTION_DOWN -> {
                v.animate().scaleX(0.95f).scaleY(0.95f).setDuration(80).start()
                v.performHapticFeedback(HapticFeedbackConstants.VIRTUAL_KEY)
            }
            MotionEvent.ACTION_UP, MotionEvent.ACTION_CANCEL ->
                v.animate().scaleX(1f).scaleY(1f).setDuration(120).start()
        }
        false
    }
}

/** Adds press feedback to every button under this view. */
fun View.addPressFeedbackToButtons() {
    if (this is MaterialButton) addPressFeedback()
    if (this is ViewGroup) for (i in 0 until childCount) getChildAt(i).addPressFeedbackToButtons()
}

/** Disables the button and swaps its label while work is running, then restores it. */
fun MaterialButton.setBusy(busy: Boolean, busyText: String) {
    if (busy) {
        if (getTag(R.id.tag_idle_text) == null) setTag(R.id.tag_idle_text, text)
        text = busyText
        isEnabled = false
        alpha = 0.7f
    } else {
        (getTag(R.id.tag_idle_text) as? CharSequence)?.let { text = it }
        setTag(R.id.tag_idle_text, null)
        isEnabled = true
        alpha = 1f
    }
}
