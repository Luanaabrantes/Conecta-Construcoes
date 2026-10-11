# Stage 1: Runtime Base
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000

# Stage 2: SDK Build & Restore
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Otimização de cache do Docker: restaura dependências apenas quando os arquivos .csproj mudarem
COPY ["Conecta.API/Conecta.API.csproj", "Conecta.API/"]
COPY ["Conecta.Application/Conecta.Application.csproj", "Conecta.Application/"]
COPY ["Conecta.Domain/Conecta.Domain.csproj", "Conecta.Domain/"]
COPY ["Conecta.Infrastructure/Conecta.Infrastructure.csproj", "Conecta.Infrastructure/"]
RUN dotnet restore "Conecta.API/Conecta.API.csproj"

# Copia todo o código-fonte (incluindo Database/Migrations/ e scripts SQL)
COPY . .
WORKDIR "/src/Conecta.API"
RUN dotnet build "Conecta.API.csproj" -c Release -o /app/build

# Stage 3: Publish da aplicação
FROM build AS publish
RUN dotnet publish "Conecta.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 4: Imagem final para execução
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Conecta.API.dll"]
