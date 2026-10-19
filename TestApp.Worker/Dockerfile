# Build context is the solution root, because the csproj is copied by path.

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Manifest first, so the restore layer stays cached until dependencies change.
COPY ["TestApp.Worker/TestApp.Worker.csproj", "TestApp.Worker/"]
RUN dotnet restore "./TestApp.Worker/TestApp.Worker.csproj"

COPY . .
WORKDIR "/src/TestApp.Worker"
RUN dotnet publish "./TestApp.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# runtime, not aspnet: this is a console app. It has no Kestrel, no ports and no
# ingress, so the whole ASP.NET Core stack would be dead weight in the image.
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Non-root. $APP_UID is provided by the .NET base images.
USER $APP_UID

# No EXPOSE: nothing listens on a port.
ENTRYPOINT ["dotnet", "TestApp.Worker.dll"]
