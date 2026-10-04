package com.example.blastpromobile.data

import android.content.Context
import android.net.Uri
import com.example.blastpromobile.BuildConfig
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import org.json.JSONArray
import org.json.JSONObject
import java.io.File
import java.net.HttpURLConnection
import java.net.URL

data class RemoteProject(val id: Int, val name: String, val siteLocation: String, val blastType: String)
data class RemotePhoto(val id: Int, val fileName: String)
data class RemoteComment(val id: Int, val body: String, val authorName: String)
data class RemoteNote(
    val id: Int, val projectId: Int, val title: String, val body: String,
    val authorName: String, val updatedAtUtc: String,
    val photos: List<RemotePhoto>, val comments: List<RemoteComment>
)

class ApiFailure(message: String, val status: Int = 0) : Exception(message)

/** The session exists only in memory. Closing the app requires a new sign in. */
object MobileSession {
    var token: String? = null
        private set
    var userId: String? = null
        private set
    fun signIn(value: String, user: String) { token = value; userId = user }
    fun signOut() { token = null; userId = null }
}

class RemoteRepository(private val context: Context) {
    private val base = BuildConfig.API_BASE_URL.trimEnd('/')

    private suspend fun call(method: String, path: String, body: JSONObject? = null,
                             authenticated: Boolean = true): String = withContext(Dispatchers.IO) {
        val connection = (URL("$base$path").openConnection() as HttpURLConnection).apply {
            requestMethod = method
            connectTimeout = 10_000
            readTimeout = 20_000
            setRequestProperty("Accept", "application/json")
            if (authenticated) setRequestProperty("Authorization", "Bearer ${MobileSession.token ?: throw ApiFailure("Please sign in again.", 401)}")
            if (body != null) {
                doOutput = true
                setRequestProperty("Content-Type", "application/json; charset=utf-8")
            }
        }
        try {
            if (body != null) connection.outputStream.use { it.write(body.toString().toByteArray(Charsets.UTF_8)) }
            val status = connection.responseCode
            val response = (if (status in 200..299) connection.inputStream else connection.errorStream)
                ?.bufferedReader()?.use { it.readText() }.orEmpty()
            if (status !in 200..299) {
                if (status == 401) MobileSession.signOut()
                val message = runCatching { JSONObject(response).optString("message") }.getOrNull()
                    ?.takeIf { it.isNotBlank() }
                    ?: when (status) {
                        401 -> "Sign in failed or your session expired."
                        404 -> "This project or note is no longer available."
                        else -> "The server could not complete the request ($status)."
                    }
                throw ApiFailure(message, status)
            }
            response
        } finally { connection.disconnect() }
    }

    suspend fun login(email: String, password: String) {
        val response = JSONObject(call("POST", "/api/auth/login", JSONObject()
            .put("email", email).put("password", password), false))
        MobileSession.signIn(response.getString("token"), response.getString("userId"))
        // A token must also pass the API's current account and company checks.
        try { call("GET", "/api/auth/me") }
        catch (error: Exception) { MobileSession.signOut(); throw error }
    }

    /** True when the server answers at all; any HTTP response counts, only a failed connection is offline. */
    suspend fun isOnline(): Boolean = withContext(Dispatchers.IO) {
        runCatching {
            val connection = (URL(base).openConnection() as HttpURLConnection).apply {
                requestMethod = "GET"
                connectTimeout = 5_000
                readTimeout = 5_000
            }
            try { connection.responseCode; true } finally { connection.disconnect() }
        }.getOrDefault(false)
    }

    suspend fun projects(): List<RemoteProject> = JSONArray(call("GET", "/api/projects")).let { array ->
        (0 until array.length()).map { i -> array.getJSONObject(i).let { p ->
            RemoteProject(p.getInt("id"), p.getString("name"), p.getString("siteLocation"), p.getString("blastType"))
        } }
    }

    suspend fun createProject(name: String, site: String, blastType: String): RemoteProject =
        JSONObject(call("POST", "/api/projects", JSONObject()
            .put("name", name).put("siteLocation", site).put("blastType", blastType))).let { p ->
            RemoteProject(p.getInt("id"), p.getString("name"), p.getString("siteLocation"), p.getString("blastType"))
        }

    private fun parseNote(n: JSONObject): RemoteNote {
        val photos = n.getJSONArray("photos").let { a -> (0 until a.length()).map { i ->
            a.getJSONObject(i).let { p -> RemotePhoto(p.getInt("id"), p.getString("fileName")) }
        } }
        val comments = n.getJSONArray("comments").let { a -> (0 until a.length()).map { i ->
            a.getJSONObject(i).let { c -> RemoteComment(c.getInt("id"), c.getString("body"), c.getString("authorName")) }
        } }
        return RemoteNote(n.getInt("id"), n.getInt("projectId"), n.getString("title"),
            n.getString("body"), n.getString("authorName"), n.getString("updatedAtUtc"), photos, comments)
    }

    suspend fun notes(projectId: Int): List<RemoteNote> =
        JSONArray(call("GET", "/api/projects/$projectId/notes")).let { a ->
            (0 until a.length()).map { parseNote(a.getJSONObject(it)) }
        }

    suspend fun note(projectId: Int, noteId: Int): RemoteNote =
        parseNote(JSONObject(call("GET", "/api/projects/$projectId/notes/$noteId")))

    suspend fun createNote(projectId: Int, title: String, body: String): RemoteNote =
        parseNote(JSONObject(call("POST", "/api/projects/$projectId/notes", JSONObject()
            .put("title", title).put("body", body))))

    suspend fun updateNote(projectId: Int, noteId: Int, title: String, body: String) {
        call("PUT", "/api/projects/$projectId/notes/$noteId", JSONObject()
            .put("title", title).put("body", body))
    }

    suspend fun deleteNote(projectId: Int, noteId: Int) {
        call("DELETE", "/api/projects/$projectId/notes/$noteId")
    }

    suspend fun addComment(projectId: Int, noteId: Int, body: String) {
        call("POST", "/api/projects/$projectId/notes/$noteId/comments", JSONObject().put("body", body))
    }

    suspend fun deletePhoto(projectId: Int, noteId: Int, photoId: Int) {
        call("DELETE", "/api/projects/$projectId/notes/$noteId/photos/$photoId")
    }

    suspend fun uploadPhoto(projectId: Int, noteId: Int, uri: Uri) = withContext(Dispatchers.IO) {
        val data = context.contentResolver.openInputStream(uri)?.use { it.readBytes() }
            ?: throw ApiFailure("Could not read the selected photo.")
        if (data.size > 5_000_000) throw ApiFailure("Choose a photo smaller than 5 MB.")
        val mime = when {
            data.size >= 8 && data.copyOfRange(0, 8).contentEquals(byteArrayOf(137.toByte(), 80, 78, 71, 13, 10, 26, 10)) -> "image/png"
            data.size >= 3 && data[0] == 0xff.toByte() && data[1] == 0xd8.toByte() && data[2] == 0xff.toByte() -> "image/jpeg"
            data.size >= 12 && String(data, 0, 4, Charsets.US_ASCII) == "RIFF" &&
                String(data, 8, 4, Charsets.US_ASCII) == "WEBP" -> "image/webp"
            else -> throw ApiFailure("Choose a JPEG, PNG, or WebP image.")
        }
        val extension = when (mime) { "image/png" -> "png"; "image/webp" -> "webp"; else -> "jpg" }
        val boundary = "BlastPro${System.currentTimeMillis()}"
        val connection = (URL("$base/api/projects/$projectId/notes/$noteId/photos").openConnection() as HttpURLConnection).apply {
            requestMethod = "POST"
            connectTimeout = 10_000
            readTimeout = 30_000
            doOutput = true
            setRequestProperty("Authorization", "Bearer ${MobileSession.token ?: throw ApiFailure("Please sign in again.", 401)}")
            setRequestProperty("Content-Type", "multipart/form-data; boundary=$boundary")
        }
        try {
            connection.outputStream.use { output ->
                output.write("--$boundary\r\nContent-Disposition: form-data; name=\"file\"; filename=\"field-photo.$extension\"\r\nContent-Type: $mime\r\n\r\n".toByteArray())
                output.write(data)
                output.write("\r\n--$boundary--\r\n".toByteArray())
            }
            if (connection.responseCode !in 200..299)
                throw ApiFailure("Photo upload failed (${connection.responseCode}).", connection.responseCode)
        } finally { connection.disconnect() }
    }

    suspend fun photoFile(projectId: Int, noteId: Int, photoId: Int): File = withContext(Dispatchers.IO) {
        val account = MobileSession.userId ?: throw ApiFailure("Please sign in again.", 401)
        val file = File(context.cacheDir, "${account}-project-${projectId}-note-${noteId}-photo-${photoId}.img")
        if (!file.exists()) {
            val connection = (URL("$base/api/projects/$projectId/notes/$noteId/photos/$photoId").openConnection() as HttpURLConnection).apply {
                connectTimeout = 10_000
                readTimeout = 20_000
                setRequestProperty("Authorization", "Bearer ${MobileSession.token ?: throw ApiFailure("Please sign in again.", 401)}")
            }
            try {
                if (connection.responseCode !in 200..299) throw ApiFailure("Photo unavailable.", connection.responseCode)
                connection.inputStream.use { input -> file.outputStream().use { input.copyTo(it) } }
            } finally { connection.disconnect() }
        }
        file
    }
}
