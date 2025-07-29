# Docker Build and Deployment Scripts

# Build development image
build-dev:
	docker build --target development -t football-simulation:dev .

# Build production image
build-prod:
	docker build --target production -t football-simulation:prod .

# Run development environment
dev-up:
	docker-compose up --build

# Run development environment in background
dev-up-d:
	docker-compose up --build -d

# Stop development environment
dev-down:
	docker-compose down

# Run production environment
prod-up:
	docker-compose -f docker-compose.prod.yml up --build

# Run production environment in background
prod-up-d:
	docker-compose -f docker-compose.prod.yml up --build -d

# Stop production environment
prod-down:
	docker-compose -f docker-compose.prod.yml down

# View logs
logs-dev:
	docker-compose logs -f

logs-prod:
	docker-compose -f docker-compose.prod.yml logs -f

# Clean up Docker resources
clean:
	docker system prune -f
	docker volume prune -f

# Remove all containers and images
clean-all:
	docker-compose down --rmi all --volumes
	docker-compose -f docker-compose.prod.yml down --rmi all --volumes

.PHONY: build-dev build-prod dev-up dev-up-d dev-down prod-up prod-up-d prod-down logs-dev logs-prod clean clean-all
