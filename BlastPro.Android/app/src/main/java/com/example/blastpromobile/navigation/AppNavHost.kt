package com.example.blastpromobile.navigation

import androidx.compose.animation.EnterTransition
import androidx.compose.animation.ExitTransition
import androidx.compose.animation.core.tween
import androidx.compose.animation.fadeIn
import androidx.compose.animation.fadeOut
import androidx.compose.animation.slideInHorizontally
import androidx.compose.animation.slideOutHorizontally
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.WindowInsets
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.ime
import androidx.compose.foundation.layout.imePadding
import androidx.compose.foundation.layout.navigationBarsPadding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.DrawerValue
import androidx.compose.material3.ModalDrawerSheet
import androidx.compose.material3.ModalNavigationDrawer
import androidx.compose.material3.rememberDrawerState
import androidx.compose.runtime.Composable
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.getValue
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.platform.LocalLayoutDirection
import androidx.compose.ui.unit.LayoutDirection
import androidx.compose.ui.unit.dp
import androidx.lifecycle.Lifecycle
import androidx.navigation.NavHostController
import androidx.navigation.NavOptionsBuilder
import androidx.navigation.NavType
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.currentBackStackEntryAsState
import androidx.navigation.compose.rememberNavController
import androidx.navigation.navArgument
import com.example.blastpromobile.Config
import com.example.blastpromobile.ui.chat.ChatScreen
import com.example.blastpromobile.ui.components.BottomNavBar
import com.example.blastpromobile.ui.login.LoginScreen
import com.example.blastpromobile.ui.menu.MenuDrawerContent
import com.example.blastpromobile.ui.notes.NoteEditorScreen
import com.example.blastpromobile.ui.notes.NotesHomeScreen
import com.example.blastpromobile.ui.photos.PhotoViewerScreen
import com.example.blastpromobile.ui.settings.SettingsScreen
import com.example.blastpromobile.ui.theme.ScreenBackground
import kotlinx.coroutines.launch

@Composable
fun AppNavHost(navController: NavHostController = rememberNavController()) {
    val drawerState = rememberDrawerState(DrawerValue.Closed)
    val scope = rememberCoroutineScope()
    val openMenu: () -> Unit = { scope.launch { drawerState.open() } }
    val closeMenu: () -> Unit = { scope.launch { drawerState.close() } }
    val openNewNote = { navController.safeNavigate(Routes.noteEditor(Routes.NEW_NOTE_ID)) }

    val currentRoute = navController.currentBackStackEntryAsState().value?.destination?.route
    val showBottomBar = currentRoute in listOf(Routes.NOTES, Routes.CHAT, Routes.SETTINGS)
    val keyboardOpen = WindowInsets.ime.getBottom(LocalDensity.current) > 0

    // The drawer is laid out right-to-left so it slides in from the right, under the hamburger icon.
    // Everything inside is switched back to left-to-right.
    CompositionLocalProvider(LocalLayoutDirection provides LayoutDirection.Rtl) {
        ModalNavigationDrawer(
            drawerState = drawerState,
            gesturesEnabled = showBottomBar || drawerState.isOpen,
            drawerContent = {
                CompositionLocalProvider(LocalLayoutDirection provides LayoutDirection.Ltr) {
                    ModalDrawerSheet(
                        modifier = Modifier.width(300.dp),
                        drawerShape = RoundedCornerShape(topStart = 16.dp, bottomStart = 16.dp),
                        drawerContainerColor = ScreenBackground,
                        windowInsets = WindowInsets(0)
                    ) {
                        MenuDrawerContent(
                            currentRoute = currentRoute,
                            onClose = closeMenu,
                            onNewNote = { closeMenu(); openNewNote() },
                            onNotes = { closeMenu(); navController.navigateTopLevel(Routes.NOTES) },
                            onChat = { closeMenu(); navController.navigateTopLevel(Routes.CHAT) },
                            onSettings = { closeMenu(); navController.navigateTopLevel(Routes.SETTINGS) },
                            onSync = { closeMenu() },
                            onLogout = {
                                closeMenu()
                                navController.safeNavigate(Routes.LOGIN) { popUpTo(0) { inclusive = true } }
                            }
                        )
                    }
                }
            }
        ) {
            CompositionLocalProvider(LocalLayoutDirection provides LayoutDirection.Ltr) {
                Column(Modifier.fillMaxSize().background(ScreenBackground).imePadding()) {
                    Box(Modifier.weight(1f)) {
                        NavHost(
                            navController = navController,
                            startDestination = Routes.LOGIN,
                            enterTransition = {
                                if (targetState.destination.route in detailRoutes) {
                                    slideInHorizontally(tween(220)) { it / 10 } + fadeIn(tween(220))
                                } else {
                                    fadeIn(tween(150))
                                }
                            },
                            exitTransition = { ExitTransition.None },
                            popEnterTransition = { EnterTransition.None },
                            popExitTransition = {
                                if (initialState.destination.route in detailRoutes) {
                                    slideOutHorizontally(tween(200)) { it / 10 } + fadeOut(tween(200))
                                } else {
                                    ExitTransition.None
                                }
                            }
                        ) {
                            composable(Routes.LOGIN) {
                                LoginScreen(
                                    onLogin = {
                                        navController.safeNavigate(Routes.NOTES) { popUpTo(Routes.LOGIN) { inclusive = true } }
                                    }
                                )
                            }
                            composable(Routes.NOTES) {
                                NotesHomeScreen(
                                    onMenuClick = openMenu,
                                    onOpenNote = { navController.safeNavigate(Routes.noteEditor(it)) }
                                )
                            }
                            composable(
                                route = Routes.NOTE_EDITOR,
                                arguments = listOf(navArgument("noteId") { type = NavType.StringType })
                            ) { entry ->
                                val noteId = entry.arguments?.getString("noteId")?.takeIf { it != Routes.NEW_NOTE_ID }
                                NoteEditorScreen(
                                    noteId = noteId,
                                    onBack = { navController.safePop() },
                                    onOpenPhoto = { navController.safeNavigate(Routes.PHOTO_VIEWER) }
                                )
                            }
                            composable(Routes.PHOTO_VIEWER) {
                                PhotoViewerScreen(onBack = { navController.safePop() })
                            }
                            composable(Routes.SETTINGS) {
                                SettingsScreen(onMenuClick = openMenu, onBack = { navController.safePop() })
                            }
                            // Chat is an optional module: remove this block, the Chat package and Config.CHAT_ENABLED to drop it.
                            if (Config.CHAT_ENABLED) {
                                composable(Routes.CHAT) {
                                    ChatScreen(
                                        onMenuClick = openMenu,
                                        onOpenNote = { navController.safeNavigate(Routes.noteEditor(it)) }
                                    )
                                }
                            }
                        }
                    }

                    if (showBottomBar && !keyboardOpen) {
                        BottomNavBar(
                            notesSelected = currentRoute == Routes.NOTES,
                            chatSelected = currentRoute == Routes.CHAT,
                            onNotes = { navController.navigateTopLevel(Routes.NOTES) },
                            onNewNote = { openNewNote() },
                            onChat = { navController.navigateTopLevel(Routes.CHAT) }
                        )
                    } else {
                        // Keep content clear of the system navigation bar when the bottom bar is hidden.
                        Box(Modifier.navigationBarsPadding())
                    }
                }
            }
        }
    }
}

/** Screens pushed on top of the main tabs. They slide in gently; tab switches only fade in. */
private val detailRoutes = listOf(Routes.NOTE_EDITOR, Routes.PHOTO_VIEWER)

/** Lands on a top-level screen, keeping Notes as the bottom of the back stack. */
private fun NavHostController.navigateTopLevel(route: String) {
    if (currentDestination?.route == route) return
    safeNavigate(route) {
        popUpTo(Routes.NOTES) { inclusive = false }
        launchSingleTop = true
    }
}

/** Ignores taps while a transition is settling or another navigation is already under way, so rapid taps cannot stack screens. */
private fun NavHostController.safeNavigate(route: String, builder: NavOptionsBuilder.() -> Unit = {}) {
    if (currentBackStackEntry?.lifecycle?.currentState == Lifecycle.State.RESUMED) {
        navigate(route, builder)
    }
}

private fun NavHostController.safePop() {
    if (currentBackStackEntry?.lifecycle?.currentState == Lifecycle.State.RESUMED) {
        popBackStack()
    }
}
