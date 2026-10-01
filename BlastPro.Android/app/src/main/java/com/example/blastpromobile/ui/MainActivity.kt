package com.example.blastpromobile.ui

import android.graphics.Color
import android.os.Bundle
import android.view.View
import androidx.activity.OnBackPressedCallback
import androidx.activity.SystemBarStyle
import androidx.activity.enableEdgeToEdge
import androidx.appcompat.app.AppCompatActivity
import androidx.core.view.GravityCompat
import androidx.core.view.ViewCompat
import androidx.core.view.WindowInsetsCompat
import androidx.drawerlayout.widget.DrawerLayout
import androidx.navigation.NavController
import androidx.navigation.NavDestination
import androidx.navigation.fragment.NavHostFragment
import com.example.blastpromobile.Config
import com.example.blastpromobile.R
import com.example.blastpromobile.databinding.ActivityMainBinding

/** Hosts the screens, the bottom bar and the slide-out menu. */
class MainActivity : AppCompatActivity() {

    private lateinit var binding: ActivityMainBinding
    private lateinit var navController: NavController

    private var imeVisible = false
    private var showBottomBar = false
    private var navBarInset = 0
    private var imeInset = 0

    override fun onCreate(savedInstanceState: Bundle?) {
        enableEdgeToEdge(
            statusBarStyle = SystemBarStyle.dark(Color.TRANSPARENT),
            navigationBarStyle = SystemBarStyle.dark(Color.TRANSPARENT)
        )
        super.onCreate(savedInstanceState)
        binding = ActivityMainBinding.inflate(layoutInflater)
        setContentView(binding.root)

        navController = (supportFragmentManager.findFragmentById(R.id.nav_host) as NavHostFragment).navController

        setUpInsets()
        setUpBottomBar()
        setUpDrawer()

        navController.addOnDestinationChangedListener { _, destination, _ -> onDestinationChanged(destination) }

        // Back closes the menu first if it is open.
        onBackPressedDispatcher.addCallback(this, object : OnBackPressedCallback(true) {
            override fun handleOnBackPressed() {
                if (binding.drawerLayout.isDrawerOpen(GravityCompat.END)) {
                    binding.drawerLayout.closeDrawer(GravityCompat.END)
                } else {
                    isEnabled = false
                    onBackPressedDispatcher.onBackPressed()
                    isEnabled = true
                }
            }
        })
    }

    fun openMenu() {
        binding.drawerLayout.openDrawer(GravityCompat.END)
    }

    private fun closeMenu() {
        binding.drawerLayout.closeDrawer(GravityCompat.END)
    }

    /** Pads for the status bar, the navigation bar and the keyboard so nothing is hidden behind them. */
    private fun setUpInsets() {
        ViewCompat.setOnApplyWindowInsetsListener(binding.drawerLayout) { _, insets ->
            val bars = insets.getInsets(WindowInsetsCompat.Type.systemBars())
            navBarInset = bars.bottom
            imeInset = insets.getInsets(WindowInsetsCompat.Type.ime()).bottom
            imeVisible = insets.isVisible(WindowInsetsCompat.Type.ime())

            binding.contentRoot.setPadding(0, bars.top, 0, 0)
            binding.drawerPanel.setPadding(
                binding.drawerPanel.paddingLeft, bars.top + dp(16),
                binding.drawerPanel.paddingRight, bars.bottom + dp(16)
            )
            updateBottomArea()
            insets
        }
    }

    /** Shows the bottom bar on the main screens, and hides it while the keyboard is open. */
    private fun updateBottomArea() {
        val barVisible = showBottomBar && !imeVisible
        binding.bottomBar.visibility = if (barVisible) View.VISIBLE else View.GONE
        binding.bottomBar.setPadding(0, 0, 0, navBarInset)
        // When the bar is hidden, keep screen content clear of the navigation bar and the keyboard.
        val bottom = if (barVisible) 0 else maxOf(navBarInset, imeInset)
        binding.navHost.setPadding(0, 0, 0, bottom)
    }

    private fun setUpBottomBar() {
        binding.navNotes.setOnClickListener { goTo(R.id.action_global_notes) }
        binding.navNewNote.setOnClickListener { goTo(R.id.action_global_new_note) }
        binding.navChat.setOnClickListener { goTo(R.id.action_global_chat) }
        if (!Config.CHAT_ENABLED) binding.navChat.visibility = View.INVISIBLE
    }

    private fun setUpDrawer() {
        binding.menuClose.setOnClickListener { closeMenu() }
        binding.menuNewNote.setOnClickListener { closeMenu(); goTo(R.id.action_global_new_note) }
        binding.menuNotes.setOnClickListener { closeMenu(); goTo(R.id.action_global_notes) }
        binding.menuChat.setOnClickListener { closeMenu(); goTo(R.id.action_global_chat) }
        binding.menuSettings.setOnClickListener { closeMenu(); goTo(R.id.action_global_settings) }
        binding.menuSync.setOnClickListener { closeMenu() } // UI only for now
        binding.menuLogout.setOnClickListener { closeMenu(); goTo(R.id.action_global_login) }
        if (!Config.CHAT_ENABLED) binding.menuChat.visibility = View.GONE
    }

    /** Opens a main screen, ignoring taps on the screen the user is already on. */
    private fun goTo(actionId: Int) {
        val target = when (actionId) {
            R.id.action_global_notes -> R.id.notesFragment
            R.id.action_global_chat -> R.id.chatFragment
            R.id.action_global_settings -> R.id.settingsFragment
            else -> null
        }
        if (target != null && navController.currentDestination?.id == target) return
        navController.safeNavigate(actionId)
    }

    private fun onDestinationChanged(destination: NavDestination) {
        val id = destination.id
        showBottomBar = id == R.id.notesFragment || id == R.id.chatFragment || id == R.id.settingsFragment
        updateBottomArea()

        binding.navNotes.isSelected = id == R.id.notesFragment
        binding.navChat.isSelected = id == R.id.chatFragment
        binding.menuNotes.isSelected = id == R.id.notesFragment
        binding.menuChat.isSelected = id == R.id.chatFragment
        binding.menuSettings.isSelected = id == R.id.settingsFragment

        // The menu can be swiped open only on the main screens (not on login or the editor).
        binding.drawerLayout.setDrawerLockMode(
            if (showBottomBar) DrawerLayout.LOCK_MODE_UNLOCKED else DrawerLayout.LOCK_MODE_LOCKED_CLOSED,
            GravityCompat.END
        )
    }

    private fun dp(value: Int) = (value * resources.displayMetrics.density).toInt()
}
