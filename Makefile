help:
	@egrep "^#" Makefile

# target: up                   - Start rabbit in docker container
up: 
	docker-compose up -d

# target: prod-up|pu           - Start dotnet worker and rabbit in docker containers
pu: prod-up
prod-up: 
	docker compose -f docker-compose.prod.yml up -d --build

# target: prod-stop|ps         - Start dotnet worker and rabbit in docker containers
ps: prod-stop
prod-stop:
	docker compose -f docker-compose.prod.yml down
	
