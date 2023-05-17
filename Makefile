help:
	@egrep "^#" Makefile

# target: up                   - Start rabbit in docker container
up: 
	docker-compose up -d --build

# target: prod                   - Start dotnet worker and rabbit in docker containers
prod: 
	docker compose -f docker-compose.prod.yml up -d --build --force-recreate
