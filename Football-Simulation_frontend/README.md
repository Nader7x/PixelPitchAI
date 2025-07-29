# ⚽ Football Simulation Frontend

A modern, feature-rich Next.js application for football match simulation with real-time updates, 3D visualizations, and comprehensive management features.

## 🚀 Features

- **Real-time Match Simulation** with SignalR integration
- **3D Stadium Visualizations** using Three.js and React Three Fiber
- **Multi-language Support** (English, Spanish, French) with next-intl
- **Dark/Light Theme** with system preference detection
- **User Authentication** with role-based access control
- **Responsive Design** optimized for all devices
- **Progressive Web App** capabilities
- **Real-time Notifications** system
- **Advanced Settings Management** with persistence

## 🐳 Docker Deployment

This project supports Docker deployment for both development and production environments with HTTPS security.

### Quick Start with Docker

1. **Prerequisites**: Install [Docker Desktop](https://www.docker.com/products/docker-desktop/)

2. **First-time setup**:

   ```powershell
   # Run the automated setup script
   .\setup-docker.ps1
   ```

3. **Development environment**:

   ```powershell
   pnpm run docker:dev
   # Access: http://localhost:3000
   ```

4. **Production environment with HTTPS**:

   ```powershell
   # Configure SSL certificates
   .\setup-ssl.ps1 -LetsEncrypt -Domain "yourdomain.com" -Email "admin@yourdomain.com"

   # Deploy to production
   .\deploy-production.ps1 -Domain "yourdomain.com" -Email "admin@yourdomain.com"
   # Access: https://yourdomain.com
   ```

### Available Docker Commands

```powershell
# Development
pnpm run docker:dev        # Start development environment
pnpm run docker:dev:bg     # Start development in background
pnpm run docker:dev:down   # Stop development environment

# Production
pnpm run docker:prod       # Start production environment
pnpm run docker:prod:bg    # Start production in background
pnpm run docker:prod:down  # Stop production environment

# SSL Setup
pnpm run ssl:setup         # Interactive SSL setup
pnpm run ssl:letsencrypt   # Let's Encrypt certificates
pnpm run ssl:selfsigned    # Self-signed certificates

# Monitoring & Maintenance
pnpm run docker:prod:monitoring  # Start with monitoring stack
pnpm run docker:logs:prod        # View production logs
pnpm run docker:clean            # Clean Docker resources
pnpm run validate:docker         # Validate Docker configuration
```

## 🔒 Security & HTTPS

The production deployment includes enterprise-grade security features:

- **HTTPS/TLS encryption** with Let's Encrypt or custom certificates
- **Security headers** (HSTS, CSP, XSS protection, etc.)
- **Rate limiting** for API endpoints and authentication
- **Container security** with non-root users and minimal privileges
- **Network isolation** with Docker networks
- **Automated SSL certificate renewal**

## 🌐 Hosting Options

### Self-Hosting

- **VPS/Cloud Server**: DigitalOcean, Linode, Vultr ($10-30/month)
- **Home Server**: Raspberry Pi or dedicated hardware
- **Enterprise**: Kubernetes clusters with auto-scaling

### Cloud Platforms

- **Vercel**: Zero-config deployment with global CDN
- **Netlify**: JAMstack optimized with serverless functions
- **AWS**: ECS/Fargate for containerized deployments
- **Google Cloud**: Cloud Run for serverless containers
- **Azure**: Container Instances with Application Gateway

For detailed hosting instructions, see [HTTPS_HOSTING_GUIDE.md](./HTTPS_HOSTING_GUIDE.md).

## 🔧 Development

### Local Development Setup

1. **Clone the repository**:

   ```bash
   git clone <repository-url>
   cd Football-Simulation_frontend
   ```

2. **Install dependencies**:

   ```bash
   pnpm install
   ```

3. **Set up environment variables**:

   ```bash
   cp .env.example .env.local
   # Edit .env.local with your configuration
   ```

4. **Start the development server**:
   ```bash
   pnpm dev
   # Access: http://localhost:3000
   ```

### Technology Stack

- **Framework**: Next.js 15.3.2 with React 19
- **Language**: TypeScript with strict type checking
- **Styling**: Tailwind CSS v4 with DaisyUI components
- **3D Graphics**: Three.js with React Three Fiber
- **Real-time**: SignalR for live updates
- **Authentication**: NextAuth.js with JWT
- **Internationalization**: next-intl with multi-language support
- **Theme**: next-themes with dark/light mode
- **Package Manager**: pnpm for fast, efficient installs

### Development Tools

```bash
# Code formatting
pnpm format              # Format code with Prettier
pnpm format:check        # Check code formatting

# Linting
pnpm lint               # Run ESLint checks

# Type checking
npx tsc --noEmit        # TypeScript type checking

# Testing
pnpm test               # Run test suite (if configured)
```

## 📁 Project Structure

```
Football-Simulation_frontend/
├── app/                          # Next.js App Router
│   ├── Components/              # Shared React components
│   ├── api/                     # API routes
│   ├── contexts/                # React Context providers
│   ├── dashboard/               # Dashboard pages
│   ├── login/                   # Authentication pages
│   ├── settings/                # User settings
│   └── ...                      # Other feature pages
├── components/                   # UI components library
│   ├── ui/                      # Reusable UI components
│   ├── Scene3D.tsx             # 3D visualization components
│   └── ...
├── Services/                     # Business logic services
├── types/                       # TypeScript type definitions
├── messages/                    # Internationalization files
├── public/                      # Static assets
├── docker-compose.yml           # Development Docker setup
├── docker-compose.prod.yml      # Production Docker setup
├── Dockerfile                   # Multi-stage Docker build
├── nginx.conf                   # Production web server config
└── ...                          # Configuration files
```

## 🎯 Key Features Breakdown

### 🏟️ Match Simulation

- Real-time match visualization with 3D stadium
- Live event tracking and commentary
- Player positioning and movement
- Match statistics and analytics

### 👤 User Management

- Role-based authentication (Admin, Coach, Player)
- User profiles with customizable settings
- Team and player management
- Permission-based access control

### 🌍 Internationalization

- Multi-language support (English, Spanish, French)
- RTL language support ready
- Timezone-aware date/time handling
- Localized number and currency formatting

### 🎨 Theme System

- Dark and light mode themes
- System preference detection
- Custom theme creation capability
- Smooth theme transitions

### 📱 Responsive Design

- Mobile-first responsive layout
- Touch-friendly interactions
- Progressive Web App features
- Offline capability planning

## 🔧 Configuration

### Environment Variables

The application uses different environment files for different environments:

- `.env.local` - Local development
- `.env.production` - Production deployment
- `.env.example` - Template with all available variables

Key configuration options:

```bash
# API Configuration
NEXT_PUBLIC_API_URL=http://localhost:5000
NEXT_PUBLIC_SIGNALR_URL=http://localhost:5000/hubs

# Authentication
NEXTAUTH_SECRET=your-super-secure-secret
NEXTAUTH_URL=http://localhost:3000

# Features
NEXT_PUBLIC_ENABLE_3D=true
NEXT_PUBLIC_ENABLE_NOTIFICATIONS=true
```

### Customization

#### Adding New Languages

1. Create translation files in `messages/[locale].json`
2. Update `i18n.ts` configuration
3. Add locale to middleware configuration

#### Theme Customization

1. Update `tailwind.config.js` with custom colors
2. Modify theme definitions in `app/globals.css`
3. Update theme provider configuration

#### Adding New Features

1. Create feature components in `app/Components/`
2. Add API routes in `app/api/`
3. Update navigation and routing as needed

## 🚀 Deployment Options

### Quick Production Deployment

```powershell
# Automated production deployment with HTTPS
.\deploy-production.ps1 -Domain "yourdomain.com" -Email "admin@yourdomain.com" -Monitoring -WithRedis
```

### Manual Deployment Steps

1. **Configure environment**:

   ```bash
   cp .env.production.example .env.production
   # Edit with your production values
   ```

2. **Set up SSL certificates**:

   ```bash
   # For Let's Encrypt (recommended)
   .\setup-ssl.ps1 -LetsEncrypt -Domain "yourdomain.com" -Email "admin@yourdomain.com"

   # Or for self-signed (testing)
   .\setup-ssl.ps1 -SelfSigned -Domain "localhost"
   ```

3. **Deploy with Docker**:

   ```bash
   # Build and start production containers
   docker-compose -f docker-compose.prod.yml up --build -d
   ```

4. **Verify deployment**:

   ```bash
   # Check health
   curl https://yourdomain.com/api/health

   # View logs
   docker-compose -f docker-compose.prod.yml logs -f
   ```

## 📊 Monitoring and Maintenance

### Built-in Monitoring

The production deployment includes optional monitoring stack:

- **Health Checks**: Application and container health monitoring
- **Prometheus**: Metrics collection and alerting
- **Nginx**: Access logs and performance metrics
- **Redis**: Session and cache monitoring (when enabled)

### Log Management

```bash
# View application logs
pnpm run docker:logs:prod

# View specific service logs
docker-compose -f docker-compose.prod.yml logs nginx
docker-compose -f docker-compose.prod.yml logs football-frontend-prod

# Follow logs in real-time
docker-compose -f docker-compose.prod.yml logs -f --tail=100
```

### Backup and Recovery

```bash
# Backup application data
docker run --rm -v football_app_logs:/data -v $(pwd)/backups:/backup alpine tar czf /backup/app-backup-$(date +%Y%m%d).tar.gz /data

# Backup SSL certificates
tar czf backups/ssl-backup-$(date +%Y%m%d).tar.gz ssl/

# Create full deployment backup
tar czf backups/full-backup-$(date +%Y%m%d).tar.gz --exclude=node_modules --exclude=.git .
```

## 🛠️ Troubleshooting

### Common Issues

1. **Port conflicts**:

   ```bash
   # Check what's using port 3000
   netstat -ano | findstr :3000
   # Kill the process if needed
   taskkill /PID <PID> /F
   ```

2. **SSL certificate issues**:

   ```bash
   # Validate certificate
   openssl x509 -in ssl/cert.pem -text -noout
   # Test SSL connection
   openssl s_client -connect yourdomain.com:443
   ```

3. **Docker issues**:
   ```bash
   # Check Docker daemon
   docker ps
   # Restart Docker if needed
   docker system prune -f
   ```

### Getting Help

- **Documentation**: Check the comprehensive guides in `/Docs/`
- **Health Check**: `https://yourdomain.com/api/health`
- **Logs**: Use `pnpm run docker:logs:prod` for debugging
- **Configuration**: Validate setup with `pnpm run validate:docker`

## 📚 Documentation

Comprehensive documentation is available in the `/Docs/` directory:

- [Docker Deployment Guide](./DOCKER_DEPLOYMENT_GUIDE.md)
- [HTTPS & Hosting Guide](./HTTPS_HOSTING_GUIDE.md)
- [Settings System Guide](./Docs/SETTINGS_SYSTEM_COMPLETION_REPORT.md)
- [Internationalization Setup](./Docs/NEXT_INTL_TIMEZONE_FIX.md)
- [Theme Implementation](./Docs/DARK_MODE_FINAL_COMPLETION_REPORT.md)

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/amazing-feature`
3. Commit changes: `git commit -m 'Add amazing feature'`
4. Push to branch: `git push origin feature/amazing-feature`
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Next.js team for the amazing framework
- Vercel for hosting and deployment tools
- The open-source community for excellent packages and tools

---

**Built with ❤️ for football simulation enthusiasts**
