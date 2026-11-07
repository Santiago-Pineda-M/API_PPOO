param(
    [string]$Login,
    [System.Security.SecureString]$AdminPassword
)

$ErrorActionPreference = 'Stop'

$baseUrl = $env:API_BASE_URL
if ([string]::IsNullOrWhiteSpace($baseUrl)) {
    $baseUrl = 'http://localhost:5114'
}

$apiUri = [Uri]$baseUrl
if (-not $apiUri.IsLoopback) {
    throw 'Por seguridad, este script solo crea datos en una API local (localhost).'
}

$baseUrl = $baseUrl.TrimEnd('/')
$null = Invoke-RestMethod -Uri "$baseUrl/health" -Method Get

$adminLogin = $null
$plainPassword = $null
try {
    $bootstrap = Invoke-RestMethod -Uri "$baseUrl/api/bootstrap/admin" -Method Post
    $adminLogin = $bootstrap.login
    $plainPassword = $bootstrap.passwordGenerada
    Write-Host "Se inicializo el administrador local: $adminLogin"
}
catch {
    $statusCode = 0
    if ($_.Exception.Response) {
        $statusCode = [int]$_.Exception.Response.StatusCode
    }

    if ($statusCode -ne 403) {
        throw
    }

    if ([string]::IsNullOrWhiteSpace($Login)) {
        if ([Console]::IsInputRedirected) {
            throw 'La base ya esta inicializada. Ejecuta este script en una terminal interactiva o proporciona -Login y -AdminPassword.'
        }

        $Login = Read-Host 'La API ya esta inicializada. Ingresa el login del administrador local'
    }

    $adminLogin = $Login
    if ($null -eq $AdminPassword) {
        if ([Console]::IsInputRedirected) {
            throw 'La base ya esta inicializada. Ejecuta este script en una terminal interactiva o proporciona -Login y -AdminPassword.'
        }

        $AdminPassword = Read-Host 'Contrasena' -AsSecureString
    }

    $passwordPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($AdminPassword)
    try {
        $plainPassword = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($passwordPointer)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($passwordPointer)
    }
}

$loginBody = @{
    login = $adminLogin
    password = $plainPassword
} | ConvertTo-Json
$tokens = Invoke-RestMethod `
    -Uri "$baseUrl/api/auth/login" `
    -Method Post `
    -ContentType 'application/json' `
    -Body $loginBody

if ([string]::IsNullOrWhiteSpace($tokens.accessToken) -or [string]::IsNullOrWhiteSpace($tokens.apiKey)) {
    throw 'El login no devolvio accessToken y apiKey; revisa la respuesta de autenticacion.'
}

$headers = @{
    Authorization = "$($tokens.accessTokenType) $($tokens.accessToken)"
    'X-Api-Key' = $tokens.apiKey
}

$suffix = [Guid]::NewGuid().ToString('N').Substring(0, 8).ToUpperInvariant()
$documentType = Invoke-RestMethod `
    -Uri "$baseUrl/api/tipos-documento" `
    -Method Post `
    -Headers $headers `
    -ContentType 'application/json' `
    -Body (@{
        codigo = "DEMO$suffix"
        nombre = 'SOAT de demostracion'
        tiposVehiculoAplicables = 'A'
        codigoObligatoriedad = 'RA'
        descripcion = 'Documento local de demostracion; contenido ficticio.'
    } | ConvertTo-Json)

$identification = Get-Random -Minimum 1000000000 -Maximum 2147483647
$persona = Invoke-RestMethod `
    -Uri "$baseUrl/api/personas" `
    -Method Post `
    -Headers $headers `
    -ContentType 'application/json' `
    -Body (@{
        tipoIdentificacion = 1
        numeroIdentificacion = "$identification"
        nombres = 'Ana'
        apellidos = "Conductora $suffix"
        correoElectronico = "ana.$($suffix.ToLowerInvariant())@example.test"
        tipoPersona = 2
    } | ConvertTo-Json)

$letters = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ'
$platePrefix = -join (1..3 | ForEach-Object { $letters[(Get-Random -Minimum 0 -Maximum $letters.Length)] })
$plate = $platePrefix + (Get-Random -Minimum 0 -Maximum 1000).ToString('D3')
$issuedAt = [DateTime]::UtcNow.AddDays(-10).ToString('o')
$expiresAt = [DateTime]::UtcNow.AddYears(1).ToString('o')
$documentBytes = [Text.Encoding]::UTF8.GetBytes('%PDF-1.4 Demo document')
$vehicle = Invoke-RestMethod `
    -Uri "$baseUrl/api/vehiculos" `
    -Method Post `
    -Headers $headers `
    -ContentType 'application/json' `
    -Body (@{
        placa = $plate
        tipoVehiculo = 1
        tipoServicio = 2
        tipoCombustible = 1
        capacidadPasajeros = 5
        color = '#336699'
        modelo = 2024
        marca = 'Demo'
        linea = 'Presentacion'
        tipoDocumentoId = $documentType.id
        documentoBase64 = [Convert]::ToBase64String($documentBytes)
        nombreArchivo = 'soat-demo.pdf'
        fechaExpedicion = $issuedAt
        fechaVencimiento = $expiresAt
    } | ConvertTo-Json)

$null = Invoke-RestMethod `
    -Uri "$baseUrl/api/conductores/vehiculos" `
    -Method Post `
    -Headers $headers `
    -ContentType 'application/json' `
    -Body (@{
        personaId = $persona.id
        vehiculoIds = @($vehicle.id)
    } | ConvertTo-Json -Depth 5)

Write-Host ''
Write-Host 'Datos locales de demostracion creados:'
Write-Host "  Persona conductora: $($persona.id)"
Write-Host "  Vehiculo: $($vehicle.placa) ($($vehicle.id))"
Write-Host "  Tipo de documento: $($documentType.codigo) ($($documentType.id))"
Write-Host 'No se guardaron las credenciales del administrador en disco.'
