help:
	@egrep "^#" Makefile

# target: up                        - Start rabbit in docker container
up: 
	docker-compose up -d

# target: pu|prod-up                - Start dotnet worker and rabbit in docker containers
pu: prod-up
prod-up: 
	docker compose -f docker-compose.prod.yml up -d --build

# target: ps|prod-stop              - Start dotnet worker and rabbit in docker containers
ps: prod-stop
prod-stop:
	docker compose -f docker-compose.prod.yml down
	
# target: bp|build-publisher        - Start dotnet worker and rabbit in docker containers
bp:build-publisher
build-publisher:
	docker compose -f docker-compose.prod.yml up -d --no-deps --build dotnet