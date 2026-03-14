# Stage 1: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080 

# Stage 2: SDK and Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["OrderAPI.csproj", "./"]
RUN dotnet restore "./OrderAPI.csproj"

COPY . .
RUN dotnet build "OrderAPI.csproj" -c Release -o /app/build

# Stage 3: Publish
FROM build AS publish
RUN dotnet publish "OrderAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 4: Final Assembly
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OrderAPI.dll"]