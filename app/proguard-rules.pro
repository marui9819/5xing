# Add project specific ProGuard rules here.
# By default, the flags in this file are appended to flags specified
# in /tools/proguard/proguard-android.txt
# You can edit the include path and order by changing the proguardFiles
# directive in build.gradle.
#
# For more details, see
#   http://developer.android.com/guide/developing/tools/proguard.html

-keep class com.google.android.exoplayer2.** { *; }
-keep class androidx.room.** { *; }
-keep class androidx.work.** { *; }
