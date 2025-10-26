package com.codex.androidtviptvplayer.network

import okhttp3.ResponseBody
import retrofit2.http.GET
import retrofit2.http.Url

interface ApiService {
    @GET
    suspend fun downloadPlaylist(@Url url: String): ResponseBody
}
