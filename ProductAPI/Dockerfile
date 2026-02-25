# Stage 1: The lightweight runtime environment (Updated to .NET 10)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

# Stage 2: The heavy SDK environment (Updated to .NET 10)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["ProductAPI.csproj", "./"]
RUN dotnet restore "./ProductAPI.csproj"

COPY . .
RUN dotnet build "ProductAPI.csproj" -c Release -o /app/build

# Stage 3: Publish the compiled code
FROM build AS publish
RUN dotnet publish "ProductAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 4: Create the final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ProductAPI.dll"]