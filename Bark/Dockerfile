FROM mcr.microsoft.com/dotnet/sdk:11.0-preview AS build
WORKDIR /src

COPY Bark.slnx Directory.Build.props Directory.Packages.props NuGet.Config global.json ./
COPY src/Bark/Bark.csproj src/Bark/
COPY tests/Bark.Tests/Bark.Tests.csproj tests/Bark.Tests/
RUN dotnet restore src/Bark/Bark.csproj

COPY docs/ docs/
COPY src/Bark/ src/Bark/
RUN dotnet publish src/Bark/Bark.csproj -c Release -o /app --no-self-contained

FROM mcr.microsoft.com/dotnet/aspnet:11.0-preview AS runtime
WORKDIR /app

# only needed for the opt-in Git:Enabled content sync (see GitContentSyncService)
RUN apt-get update && apt-get install -y --no-install-recommends git \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app .

# Ensure wwwroot is writable by the app user so theme files
# (custom.css, custom.js) can be written or bind-mounted.
# Copy built-in wwwroot for CompositeFileProvider fallback when users mount their own wwwroot.
RUN mkdir -p /app/wwwroot/theme /app/wwwroot-default /app/log && cp -r /app/wwwroot/. /app/wwwroot-default/ && chown -R 1654:1654 /app/wwwroot /app/log
USER 1654

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Bark.dll"]
