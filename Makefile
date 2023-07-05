help:
	@egrep "^#" Makefile

# target: up                                     - Start rabbit in docker container
up: 
	docker-compose up -d

# target: pu|prod-up                             - Start dotnet worker and rabbit in docker containers
pu: prod-up
prod-up: 
	docker compose -f docker-compose.prod.yml up -d --build

# target: ps|prod-stop                           - Start dotnet worker and rabbit in docker containers
ps: prod-stop
prod-stop:
	docker compose -f docker-compose.prod.yml down

# target: pbp|prod-build-publisher               - Start dotnet worker and rabbit in docker containers
pbp: prod-build-publisher
prod-build-publisher: 
	docker compose -f docker-compose.prod.yml up -d --no-deps --build publisher

# target: pbc|prod-build-consumer                - Start dotnet worker and rabbit in docker containers
pbc: prod-build-consumer
prod-build-consumer: 
	docker compose -f docker-compose.prod.yml up -d --no-deps --build consumer

# target: bp|build-publisher                     - Start dotnet worker and rabbit in docker containers
bp:build-publisher
build-publisher:
	BUILDKIT_PROGRESS=plain docker compose up -d --no-deps --build publisher

# target: bp|build-consumer                      - Start dotnet worker and rabbit in docker containers
bc:build-consumer
build-consumer:
	BUILDKIT_PROGRESS=plain docker compose up -d --no-deps --build consumer

# target: bn|build-nuget                         - Build shared package nuget 
bn: build-nuget
build-nuget:
	dotnet pack src/Shared -c Release