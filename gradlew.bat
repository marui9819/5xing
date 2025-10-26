@ECHO OFF
SETLOCAL

SET APP_HOME=%~dp0
SET WRAPPER_JAR=%APP_HOME%\gradle\wrapper\gradle-wrapper.jar

IF EXIST "%WRAPPER_JAR%" GOTO runWrapper

ECHO Gradle wrapper JAR not found. Please download gradle-8.2.2-bin.zip and extract gradle-launcher-8.2.2.jar to %WRAPPER_JAR%.
IF EXIST "%JAVA_HOME%" GOTO fallback
IF NOT "%JAVA_HOME%"=="" GOTO fallback
GOTO fail

:runWrapper
SET CLASSPATH=%WRAPPER_JAR%
"%JAVA_HOME%\bin\java.exe" -classpath "%CLASSPATH%" org.gradle.wrapper.GradleWrapperMain %*
GOTO end

:fallback
IF EXIST "%ProgramFiles%\Gradle\gradle-8.2.2\bin\gradle.bat" (
  CALL "%ProgramFiles%\Gradle\gradle-8.2.2\bin\gradle.bat" %*
  GOTO end
)
ECHO Please install Gradle 8.2.2 and retry.
GOTO fail

:fail
EXIT /B 1

:end
ENDLOCAL
