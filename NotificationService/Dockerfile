# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Build image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["NotificationService/NotificationService.csproj", "NotificationService/"]
RUN dotnet restore "NotificationService/NotificationService.csproj"

COPY . .
WORKDIR "/src/NotificationService"
RUN dotnet publish "NotificationService.csproj" -c Release -o /app/publish

# Final image
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "NotificationService.dll"]
