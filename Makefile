help:
	@egrep "^#" Makefile

# target: prod-start-docker|psd                   - Start dotnet worker and rabbit in docker containers
psd: prod-start-docker
prod-start-docker:
	docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d