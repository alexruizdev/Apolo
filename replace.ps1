$pathEn = "c:\Users\aruiz\source\repos\alexruizdev\Apolo\Apolo\Strings\en-US\Resources.resw"
$lines = Get-Content -Encoding UTF8 $pathEn
for($i=0; $i -lt $lines.Length; $i++) {
    if ($lines[$i] -match '<value>') {
        $lines[$i] = $lines[$i] -creplace 'Students', 'Clients'
        $lines[$i] = $lines[$i] -creplace 'students', 'clients'
        $lines[$i] = $lines[$i] -creplace 'Student', 'Client'
        $lines[$i] = $lines[$i] -creplace 'student', 'client'
    }
}
Set-Content -Path $pathEn -Value $lines -Encoding UTF8

$pathEs = "c:\Users\aruiz\source\repos\alexruizdev\Apolo\Apolo\Strings\es-ES\Resources.resw"
$linesEs = Get-Content -Encoding UTF8 $pathEs
for($i=0; $i -lt $linesEs.Length; $i++) {
    if ($linesEs[$i] -match '<value>') {
        $linesEs[$i] = $linesEs[$i] -creplace 'Alumnos', 'Clientes'
        $linesEs[$i] = $linesEs[$i] -creplace 'alumnos', 'clientes'
        $linesEs[$i] = $linesEs[$i] -creplace 'Alumno', 'Cliente'
        $linesEs[$i] = $linesEs[$i] -creplace 'alumno', 'cliente'
    }
}
Set-Content -Path $pathEs -Value $linesEs -Encoding UTF8
