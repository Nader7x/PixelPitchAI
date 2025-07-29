# Linux/MacOS version of the SSL setup script
#!/bin/bash

# SSL Certificate Setup Script for Production (Linux/MacOS)
# This script helps you set up SSL certificates for HTTPS

DOMAIN=""
EMAIL=""
SELF_SIGNED=false
LETS_ENCRYPT=false

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -d|--domain)
            DOMAIN="$2"
            shift 2
            ;;
        -e|--email)
            EMAIL="$2"
            shift 2
            ;;
        --self-signed)
            SELF_SIGNED=true
            shift
            ;;
        --lets-encrypt)
            LETS_ENCRYPT=true
            shift
            ;;
        -h|--help)
            echo "Usage: $0 [OPTIONS]"
            echo "Options:"
            echo "  -d, --domain DOMAIN     Domain name for the certificate"
            echo "  -e, --email EMAIL       Email address for Let's Encrypt"
            echo "  --self-signed           Generate self-signed certificate"
            echo "  --lets-encrypt          Generate Let's Encrypt certificate"
            echo "  -h, --help              Show this help message"
            exit 0
            ;;
        *)
            echo "Unknown option $1"
            exit 1
            ;;
    esac
done

echo "🔒 SSL Certificate Setup for Football Simulation"
echo "================================================="
echo

# Create SSL directory if it doesn't exist
if [ ! -d "ssl" ]; then
    mkdir ssl
    echo "✅ Created ssl directory"
fi

if [ "$SELF_SIGNED" = true ]; then
    echo "🔑 Generating self-signed certificates..."
    echo "Note: Self-signed certificates will show security warnings in browsers"
    echo
    
    if ! command -v openssl &> /dev/null; then
        echo "❌ OpenSSL not found. Please install OpenSSL first."
        echo "Ubuntu/Debian: sudo apt install openssl"
        echo "CentOS/RHEL: sudo yum install openssl"
        echo "MacOS: brew install openssl"
        exit 1
    fi
    
    # Generate self-signed certificate
    openssl genrsa -out ssl/key.pem 2048
    openssl req -new -key ssl/key.pem -out ssl/cert.csr -subj "/C=US/ST=State/L=City/O=Organization/OU=Department/CN=${DOMAIN:-localhost}"
    openssl x509 -req -days 365 -in ssl/cert.csr -signkey ssl/key.pem -out ssl/cert.pem
    cp ssl/cert.pem ssl/chain.pem
    rm ssl/cert.csr
    
    echo "✅ Self-signed certificate generated successfully"
    
elif [ "$LETS_ENCRYPT" = true ]; then
    if [ -z "$DOMAIN" ] || [ -z "$EMAIL" ]; then
        echo "❌ Domain and Email are required for Let's Encrypt certificates"
        echo "Usage: $0 --lets-encrypt --domain 'yourdomain.com' --email 'admin@yourdomain.com'"
        exit 1
    fi
    
    echo "🌐 Setting up Let's Encrypt certificates..."
    echo "Domain: $DOMAIN"
    echo "Email: $EMAIL"
    echo
    
    # Update environment file
    if [ -f ".env.production" ]; then
        sed -i "s/DOMAIN_NAME=.*/DOMAIN_NAME=$DOMAIN/" .env.production
        sed -i "s/SSL_EMAIL=.*/SSL_EMAIL=$EMAIL/" .env.production
        echo "✅ Updated .env.production with domain and email"
    else
        echo "⚠️  .env.production not found. Creating from template..."
        cp .env.production.example .env.production
        sed -i "s/DOMAIN_NAME=yourdomain.com/DOMAIN_NAME=$DOMAIN/" .env.production
        sed -i "s/SSL_EMAIL=admin@yourdomain.com/SSL_EMAIL=$EMAIL/" .env.production
        echo "✅ Created .env.production with your domain settings"
    fi
    
    echo
    echo "🚀 Starting Let's Encrypt certificate generation..."
    echo "This will start the production stack to obtain certificates."
    echo
    
    # Start the production stack with SSL initialization
    docker-compose -f docker-compose.prod.yml --profile ssl-init up -d
    
    echo
    echo "✅ Certificate generation initiated!"
    echo "Check the logs to ensure certificates were generated successfully:"
    echo "docker-compose -f docker-compose.prod.yml logs certbot"
    
else
    echo "📋 SSL Certificate Setup Options:"
    echo
    echo "1. Self-Signed Certificate (Development/Testing):"
    echo "   ./setup-ssl.sh --self-signed --domain 'localhost'"
    echo
    echo "2. Let's Encrypt Certificate (Production):"
    echo "   ./setup-ssl.sh --lets-encrypt --domain 'yourdomain.com' --email 'admin@yourdomain.com'"
    echo
    echo "3. Manual Certificate Setup:"
    echo "   Place your certificate files in the ssl/ directory:"
    echo "   - ssl/cert.pem (Certificate)"
    echo "   - ssl/key.pem (Private Key)"
    echo "   - ssl/chain.pem (Certificate Chain)"
    echo
fi

echo
echo "📚 Next Steps:"
echo "1. Update nginx.conf with your domain name"
echo "2. Configure .env.production with your settings"
echo "3. Start production deployment: npm run docker:prod:bg"
echo "4. Test HTTPS: https://yourdomain.com"
echo
