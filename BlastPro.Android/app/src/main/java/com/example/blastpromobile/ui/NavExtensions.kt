package com.example.blastpromobile.ui

import android.os.Bundle
import androidx.lifecycle.Lifecycle
import androidx.navigation.NavController
import androidx.navigation.NavOptions

/**
 * Ignores taps while a screen change is still settling or the action is not available
 * from the current screen, so rapid taps cannot stack the same screen or crash.
 */
fun NavController.safeNavigate(actionId: Int, args: Bundle? = null) {
    val entry = currentBackStackEntry ?: return
    if (entry.lifecycle.currentState != Lifecycle.State.RESUMED) return
    if (currentDestination?.getAction(actionId) == null) return
    navigate(actionId, args)
}

/**
 * Safe navigate that always creates a NEW instance of the destination.
 * Use this when the destination needs to re-read its arguments every time
 * (e.g. the photo viewer showing a different photo each tap).
 */
fun NavController.safeNavigateFresh(actionId: Int, args: Bundle? = null) {
    val entry = currentBackStackEntry ?: return
    if (entry.lifecycle.currentState != Lifecycle.State.RESUMED) return
    if (currentDestination?.getAction(actionId) == null) return

    val options = NavOptions.Builder()
        .setLaunchSingleTop(false)
        .setPopUpTo(currentDestination!!.id, inclusive = false, saveState = false)
        .build()

    navigate(actionId, args, options)
}

fun NavController.safePop() {
    if (currentBackStackEntry?.lifecycle?.currentState == Lifecycle.State.RESUMED) {
        popBackStack()
    }
}