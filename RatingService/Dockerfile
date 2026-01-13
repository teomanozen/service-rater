# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["RatingService/RatingService.csproj", "RatingService/"]
RUN dotnet restore "RatingService/RatingService.csproj"

# Copy source code
COPY . .
WORKDIR "/src/RatingService"

# Build
RUN dotnet build "RatingService.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "RatingService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 5082

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RatingService.dll"]