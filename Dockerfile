FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY Directory.Build.props ./
COPY Directory.Build.targets ./
COPY Directory.Packages.props ./
COPY *.sln ./

COPY ["src/Monoling0.NotificationService.Abstractions/Monoling0.NotificationService.Abstractions.csproj", "src/Monoling0.NotificationService.Abstractions/"]
COPY ["src/Monoling0.NotificationService.Application/Monoling0.NotificationService.Application.csproj", "src/Monoling0.NotificationService.Application/"]
COPY ["src/Monoling0.NotificationService.Contracts/Monoling0.NotificationService.Contracts.csproj", "src/Monoling0.NotificationService.Contracts/"]
COPY ["src/Monoling0.NotificationService.Persistence/Monoling0.NotificationService.Persistence.csproj", "src/Monoling0.NotificationService.Persistence/"]
COPY ["src/Presentation/Monoling0.NotificationService.Presentation/Monoling0.NotificationService.Presentation.csproj", "src/Presentation/Monoling0.NotificationService.Presentation/"]
COPY ["src/Presentation/Monoling0.NotificationService.Presentation.Email/Monoling0.NotificationService.Presentation.Email.csproj", "src/Presentation/Monoling0.NotificationService.Presentation.Email/"]
COPY ["src/Presentation/Monoling0.NotificationService.Presentation.Kafka/Monoling0.NotificationService.Presentation.Kafka.csproj", "src/Presentation/Monoling0.NotificationService.Presentation.Kafka/"]

RUN dotnet restore "src/Presentation/Monoling0.NotificationService.Presentation/Monoling0.NotificationService.Presentation.csproj"

COPY . .
WORKDIR "/src/src/Presentation/Monoling0.NotificationService.Presentation"
RUN dotnet build "Monoling0.NotificationService.Presentation.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Monoling0.NotificationService.Presentation.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Monoling0.NotificationService.Presentation.dll"]
