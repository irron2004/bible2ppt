# Build stage
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY . .
RUN set -e; \
    SOLUTION_PATH=$(find . -maxdepth 2 -name 'Bible2PPT.sln' | head -n 1); \
    if [ -z "$SOLUTION_PATH" ]; then \
        echo 'Bible2PPT.sln not found in build context' >&2; \
        exit 1; \
    fi; \
    SOLUTION_DIR=$(dirname "$SOLUTION_PATH"); \
    echo "$SOLUTION_DIR" > /tmp/bible2ppt_solution_dir; \
    dotnet restore "$SOLUTION_PATH"
RUN set -e; \
    SOLUTION_DIR=$(cat /tmp/bible2ppt_solution_dir); \
    PROJECT_PATH="$SOLUTION_DIR/Bible2PPT.Web/Bible2PPT.Web.csproj"; \
    if [ ! -f "$PROJECT_PATH" ]; then \
        echo "Project file $PROJECT_PATH not found" >&2; \
        exit 1; \
    fi; \
    dotnet publish "$PROJECT_PATH" -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080}
EXPOSE 8080
ENTRYPOINT ["dotnet", "Bible2PPT.Web.dll"]
