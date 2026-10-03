$pathEn = "c:\Users\aruiz\source\repos\alexruizdev\Apolo\Apolo\Strings\en-US\Resources.resw"
$lines = Get-Content -Encoding UTF8 $pathEn
for($i=0; $i -lt $lines.Length; $i++) {
    if ($lines[$i] -match '<value>') {
        $lines[$i] = $lines[$i] -creplace 'Lessons', 'Sessions'
        $lines[$i] = $lines[$i] -creplace 'lessons', 'sessions'
        $lines[$i] = $lines[$i] -creplace 'Lesson', 'Session'
        $lines[$i] = $lines[$i] -creplace 'lesson', 'session'
    }
}
Set-Content -Path $pathEn -Value $lines -Encoding UTF8

$pathEs = "c:\Users\aruiz\source\repos\alexruizdev\Apolo\Apolo\Strings\es-ES\Resources.resw"
$linesEs = Get-Content -Encoding UTF8 $pathEs
for($i=0; $i -lt $linesEs.Length; $i++) {
    if ($linesEs[$i] -match '<value>') {
        $linesEs[$i] = $linesEs[$i] -creplace 'Clases', 'Sesiones'
        $linesEs[$i] = $linesEs[$i] -creplace 'clases', 'sesiones'
        $linesEs[$i] = $linesEs[$i] -creplace 'Clase', 'Sesión'
        $linesEs[$i] = $linesEs[$i] -creplace 'clase', 'sesión'
    }
}
Set-Content -Path $pathEs -Value $linesEs -Encoding UTF8
