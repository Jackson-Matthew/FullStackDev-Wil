package com.example.blastpromobile

object Config {
    /** Emulator alias for the development PC's localhost. A physical phone needs the PC's LAN IP instead. */
    const val DEV_API_BASE_URL = "http://10.0.2.2:5002/"

    /** Chat is a removable module. Set to false to hide the Chat tab and route; nothing else depends on it. */
    const val CHAT_ENABLED = true
}
