# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["Bingo.ApiService/Bingo.ApiService.csproj", "Bingo.ApiService/"]
RUN dotnet restore "Bingo.ApiService/Bingo.ApiService.csproj"

# Copy the rest of the code
COPY . .
WORKDIR "/src/Bingo.ApiService"

# Build and publish
RUN dotnet build "Bingo.ApiService.csproj" -c Release -o /app/build
RUN dotnet publish "Bingo.ApiService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 80
EXPOSE 443

ENTRYPOINT ["dotnet", "Bingo.ApiService.dll"] 