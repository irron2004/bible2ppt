# Build stage
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore Bible2PPT/Bible2PPT.sln
RUN dotnet publish Bible2PPT.Web/Bible2PPT.Web.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080}
EXPOSE 8080
ENTRYPOINT ["dotnet", "Bible2PPT.Web.dll"]
