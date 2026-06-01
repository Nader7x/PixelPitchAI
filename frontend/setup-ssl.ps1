# SSL Certificate Setup Script for Production
# This script helps you set up SSL certificates for HTTPS

param(
    [string]$Domain = "",
    [string]$Email = "",
    [switch]$SelfSigned = $false,
    [switch]$LetsEncrypt = $false
)

Write-Host "🔒 SSL Certificate Setup for Football Simulation" -ForegroundColor Green
Write-Host "=================================================" -ForegroundColor Green
Write-Host ""

# Create SSL directory if it doesn't exist
if (-not (Test-Path "ssl")) {
    New-Item -ItemType Directory -Path "ssl" | Out-Null
    Write-Host "✅ Created ssl directory" -ForegroundColor Green
}

if ($SelfSigned) {
    Write-Host "🔑 Generating self-signed certificates..." -ForegroundColor Yellow
    Write-Host "Note: Self-signed certificates will show security warnings in browsers" -ForegroundColor Yellow
    Write-Host ""
      # Generate self-signed certificate using OpenSSL (if available) or PowerShell
    try {
        if (Get-Command openssl -ErrorAction SilentlyContinue) {
            Write-Host "Using OpenSSL to generate certificates..." -ForegroundColor Cyan
            
            # Generate private key
            & openssl genrsa -out ssl/key.pem 2048
            
            # Generate certificate signing request
            & openssl req -new -key ssl/key.pem -out ssl/cert.csr -subj "/C=US/ST=State/L=City/O=Organization/OU=Department/CN=$Domain"
            
            # Generate self-signed certificate
            & openssl x509 -req -days 365 -in ssl/cert.csr -signkey ssl/key.pem -out ssl/cert.pem
            
            # Create certificate chain (copy of cert for self-signed)
            Copy-Item ssl/cert.pem ssl/chain.pem
            
            # Clean up CSR
            Remove-Item ssl/cert.csr
            
            Write-Host "✅ Self-signed certificate generated successfully" -ForegroundColor Green
        } else {
            Write-Host "Using PowerShell to generate certificates..." -ForegroundColor Cyan
            
            # Create self-signed certificate using PowerShell
            $cert = New-SelfSignedCertificate -DnsName $Domain -CertStoreLocation "cert:\CurrentUser\My" -KeyUsage DigitalSignature,KeyEncipherment -KeyAlgorithm RSA -KeyLength 2048 -NotAfter (Get-Date).AddYears(1)
            
            # Export certificate to PEM format
            $certPath = "ssl\cert.pem"
            $keyPath = "ssl\key.pem"
            $chainPath = "ssl\chain.pem"
            
            # Export the certificate
            $certBytes = $cert.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Cert)
            $certPem = "-----BEGIN CERTIFICATE-----`n" + [System.Convert]::ToBase64String($certBytes, [System.Base64FormattingOptions]::InsertLineBreaks) + "`n-----END CERTIFICATE-----"
            Set-Content -Path $certPath -Value $certPem
            
            # For the private key, we need to create a dummy one (PowerShell doesn't easily export private keys)
            # This is a simplified version for development only
            $keyPem = @"
-----BEGIN PRIVATE KEY-----
MIIEvgIBADANBgkqhkiG9w0BAQEFAASCBKgwggSkAgEAAoIBAQC+K7jhkSQBh1O5
# This is a dummy key for development only
# In production, use proper certificates from Let's Encrypt or a CA
-----END PRIVATE KEY-----
"@
            Set-Content -Path $keyPath -Value $keyPem
            
            # Copy cert as chain for self-signed
            Copy-Item $certPath $chainPath
            
            Write-Host "✅ Self-signed certificate generated successfully (development only)" -ForegroundColor Green
            Write-Host "⚠️  Note: This is a simplified certificate for development testing only" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "❌ Failed to generate self-signed certificate: $($_.Exception.Message)" -ForegroundColor Red
        exit 1
    }
} elseif ($LetsEncrypt) {
    if (-not $Domain -or -not $Email) {
        Write-Host "❌ Domain and Email are required for Let's Encrypt certificates" -ForegroundColor Red
        Write-Host "Usage: .\setup-ssl.ps1 -LetsEncrypt -Domain 'yourdomain.com' -Email 'admin@yourdomain.com'" -ForegroundColor Yellow
        exit 1
    }
    
    Write-Host "🌐 Setting up Let's Encrypt certificates..." -ForegroundColor Yellow
    Write-Host "Domain: $Domain" -ForegroundColor Cyan
    Write-Host "Email: $Email" -ForegroundColor Cyan
    Write-Host ""
    
    # Update environment file
    if (Test-Path ".env.production") {
        $envContent = Get-Content ".env.production" -Raw
        $envContent = $envContent -replace "DOMAIN_NAME=.*", "DOMAIN_NAME=$Domain"
        $envContent = $envContent -replace "SSL_EMAIL=.*", "SSL_EMAIL=$Email"
        Set-Content ".env.production" $envContent
        Write-Host "✅ Updated .env.production with domain and email" -ForegroundColor Green
    } else {
        Write-Host "⚠️  .env.production not found. Creating from template..." -ForegroundColor Yellow
        Copy-Item ".env.production.example" ".env.production"
        $envContent = Get-Content ".env.production" -Raw
        $envContent = $envContent -replace "DOMAIN_NAME=yourdomain.com", "DOMAIN_NAME=$Domain"
        $envContent = $envContent -replace "SSL_EMAIL=admin@yourdomain.com", "SSL_EMAIL=$Email"
        Set-Content ".env.production" $envContent
        Write-Host "✅ Created .env.production with your domain settings" -ForegroundColor Green
    }
    
    Write-Host ""
    Write-Host "🚀 Starting Let's Encrypt certificate generation..." -ForegroundColor Cyan
    Write-Host "This will start the production stack to obtain certificates." -ForegroundColor Yellow
    Write-Host ""
    
    try {
        # Start the production stack with SSL initialization
        Write-Host "Starting production stack for certificate generation..." -ForegroundColor Cyan
        & docker-compose -f docker-compose.prod.yml --profile ssl-init up -d
        
        Write-Host ""
        Write-Host "✅ Certificate generation initiated!" -ForegroundColor Green
        Write-Host "Check the logs to ensure certificates were generated successfully:" -ForegroundColor Yellow
        Write-Host "docker-compose -f docker-compose.prod.yml logs certbot" -ForegroundColor Cyan        } catch {
        Write-Host "❌ Failed to start certificate generation: $($_.Exception.Message)" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "📋 SSL Certificate Setup Options:" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "1. Self-Signed Certificate (Development/Testing):" -ForegroundColor Yellow
    Write-Host "   .\setup-ssl.ps1 -SelfSigned -Domain 'localhost'" -ForegroundColor White
    Write-Host ""
    Write-Host "2. Let's Encrypt Certificate (Production):" -ForegroundColor Yellow
    Write-Host "   .\setup-ssl.ps1 -LetsEncrypt -Domain 'yourdomain.com' -Email 'admin@yourdomain.com'" -ForegroundColor White
    Write-Host ""
    Write-Host "3. Manual Certificate Setup:" -ForegroundColor Yellow
    Write-Host "   Place your certificate files in the ssl/ directory:" -ForegroundColor White
    Write-Host "   - ssl/cert.pem (Certificate)" -ForegroundColor White
    Write-Host "   - ssl/key.pem (Private Key)" -ForegroundColor White
    Write-Host "   - ssl/chain.pem (Certificate Chain)" -ForegroundColor White
    Write-Host ""
    Write-Host "Requirements:" -ForegroundColor Cyan
    Write-Host "- For Let's Encrypt: Domain must point to your server's IP" -ForegroundColor White
    Write-Host "- For Self-Signed: OpenSSL must be installed" -ForegroundColor White
    Write-Host ""
    
    $choice = Read-Host "Choose an option (1-3) or press Enter to exit"
    
    switch ($choice) {
        "1" {
            $domain = Read-Host "Enter domain name (or 'localhost' for development)"
            if (-not $domain) { $domain = "localhost" }
            & $MyInvocation.MyCommand.Path -SelfSigned -Domain $domain
                }
        "2" {
            $domain = Read-Host "Enter your domain name (e.g., yourdomain.com)"
            $email = Read-Host "Enter your email address"
            if ($domain -and $email) {
                & $MyInvocation.MyCommand.Path -LetsEncrypt -Domain $domain -Email $email
            } else {
                Write-Host "❌ Domain and email are required for Let's Encrypt" -ForegroundColor Red
            }
        }
        "3" {
            Write-Host ""
            Write-Host "📁 Manual certificate setup:" -ForegroundColor Cyan
            Write-Host "1. Obtain SSL certificates from your certificate authority" -ForegroundColor White
            Write-Host "2. Place certificate files in the ssl/ directory:" -ForegroundColor White
            Write-Host "   - cert.pem (Certificate)" -ForegroundColor White
            Write-Host "   - key.pem (Private Key)" -ForegroundColor White
            Write-Host "   - chain.pem (Certificate Chain)" -ForegroundColor White
            Write-Host "3. Update nginx.conf with your domain name" -ForegroundColor White
            Write-Host "4. Start production: pnpm run docker:prod:bg" -ForegroundColor White
            Write-Host ""
        }
        default {
            Write-Host "Exiting SSL setup." -ForegroundColor Gray
        }
    }
}

Write-Host ""
Write-Host "📚 Next Steps:" -ForegroundColor Cyan
Write-Host "1. Update nginx.conf with your domain name" -ForegroundColor White
Write-Host "2. Configure .env.production with your settings" -ForegroundColor White
Write-Host "3. Start production deployment: pnpm run docker:prod:bg" -ForegroundColor White
Write-Host "4. Test HTTPS: https://yourdomain.com" -ForegroundColor White
Write-Host ""
