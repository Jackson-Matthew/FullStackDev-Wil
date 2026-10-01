package com.example.blastpromobile.ui

import android.os.Bundle
import androidx.lifecycle.Lifecycle
import androidx.navigation.NavController

/**
 * Ignores taps while a screen change is still settling or the action is not available from the
 * current screen, so rapid taps cannot stack the same screen or crash.
 */
fun NavController.safeNavigate(actionId: Int, args: Bundle? = null) {
    val entry = currentBackStackEntry ?: return
    if (entry.lifecycle.currentState != Lifecycle.State.RESUMED) return
    if (currentDestination?.getAction(actionId) == null) return
    navigate(actionId, args)
}

fun NavController.safePop() {
    if (currentBackStackEntry?.lifecycle?.currentState == Lifecycle.State.RESUMED) {
        popBackStack()
    }
}
