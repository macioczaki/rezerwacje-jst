# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Rezerwacje.sln", "./"]
COPY ["src/Rezerwacje.Domain/Rezerwacje.Domain.csproj", "src/Rezerwacje.Domain/"]
COPY ["src/Rezerwacje.Application/Rezerwacje.Application.csproj", "src/Rezerwacje.Application/"]
COPY ["src/Rezerwacje.Infrastructure/Rezerwacje.Infrastructure.csproj", "src/Rezerwacje.Infrastructure/"]
COPY ["src/Rezerwacje.Api/Rezerwacje.Api.csproj", "src/Rezerwacje.Api/"]
COPY ["tests/Rezerwacje.UnitTests/Rezerwacje.UnitTests.csproj", "tests/Rezerwacje.UnitTests/"]

RUN dotnet restore "Rezerwacje.sln"

COPY . .
WORKDIR "/src/src/Rezerwacje.Api"
RUN dotnet publish "Rezerwacje.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Render podaje PORT w env; ASP.NET Core domyślnie słucha 80
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Rezerwacje.Api.dll"]