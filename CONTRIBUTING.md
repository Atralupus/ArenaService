docker compose -f docker-compose.test.yml run --build --rm test-runner

# Migration
```
$ dotnet ef database update --project ArenaService.Migrations
```
