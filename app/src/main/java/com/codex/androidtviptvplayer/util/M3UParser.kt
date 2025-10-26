package com.codex.androidtviptvplayer.util

import java.io.BufferedReader

data class M3UChannel(
    val name: String,
    val group: String?,
    val logo: String?,
    val url: String
)

object M3UParser {
    private val EXTINF_REGEX = Regex("#EXTINF:-?\\d+.*?,(.*)")
    private val GROUP_REGEX = Regex("group-title=\"(.*?)\"")
    private val LOGO_REGEX = Regex("tvg-logo=\"(.*?)\"")

    fun parse(reader: BufferedReader): List<M3UChannel> {
        val channels = mutableListOf<M3UChannel>()
        var currentName: String? = null
        var currentGroup: String? = null
        var currentLogo: String? = null

        reader.useLines { lines ->
            lines.forEach { line ->
                val trimmed = line.trim()
                if (trimmed.startsWith("#EXTINF", ignoreCase = true)) {
                    val name = EXTINF_REGEX.find(trimmed)?.groupValues?.getOrNull(1)?.trim()
                    currentName = name
                    currentGroup = GROUP_REGEX.find(trimmed)?.groupValues?.getOrNull(1)
                    currentLogo = LOGO_REGEX.find(trimmed)?.groupValues?.getOrNull(1)
                } else if (trimmed.isNotEmpty() && !trimmed.startsWith("#")) {
                    val url = trimmed
                    val channelName = currentName ?: url
                    channels += M3UChannel(
                        name = channelName,
                        group = currentGroup,
                        logo = currentLogo,
                        url = url
                    )
                    currentName = null
                    currentGroup = null
                    currentLogo = null
                }
            }
        }
        return channels
    }
}
