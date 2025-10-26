#!/usr/bin/env sh

APP_HOME=$(cd "$(dirname "$0")"; pwd -P)
WRAPPER_JAR="$APP_HOME/gradle/wrapper/gradle-wrapper.jar"
DIST_URL="https://services.gradle.org/distributions/gradle-8.2.2-bin.zip"

if [ ! -f "$WRAPPER_JAR" ]; then
  echo "Gradle wrapper JAR not found. Attempting to download..." >&2
  if command -v curl >/dev/null 2>&1 && command -v unzip >/dev/null 2>&1; then
    mkdir -p "$(dirname "$WRAPPER_JAR")"
    curl -fsSL "$DIST_URL" |
      unzip -p - gradle-8.2.2/lib/gradle-launcher-8.2.2.jar > "$WRAPPER_JAR" 2>/dev/null || true
  fi
fi

JAVA_CMD=""
if [ -n "$JAVA_HOME" ]; then
  JAVA_CMD="$JAVA_HOME/bin/java"
elif command -v java >/dev/null 2>&1; then
  JAVA_CMD="$(command -v java)"
fi

if [ -n "$JAVA_CMD" ] && [ -f "$WRAPPER_JAR" ]; then
  CLASSPATH="$WRAPPER_JAR"
  exec "$JAVA_CMD" ${JAVA_OPTS:-} \
    -classpath "$CLASSPATH" org.gradle.wrapper.GradleWrapperMain "$@"
fi

if command -v gradle >/dev/null 2>&1; then
  echo "Falling back to system gradle executable." >&2
  exec gradle "$@"
fi

echo "Unable to run Gradle. Install Gradle 8.2.2 or provide the wrapper JAR." >&2
exit 1
