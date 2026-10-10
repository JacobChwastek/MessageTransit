#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "."]
COPY ["signing.props", "."]
COPY ["MessageTransit.snk", "."]
COPY ["src/", "src/"]
COPY ["tests/MessageTransit.Benchmark/", "tests/MessageTransit.Benchmark/"]

WORKDIR "/src/tests/MessageTransit.Benchmark"
RUN dotnet build "MessageTransit.Benchmark.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MessageTransit.Benchmark.csproj" -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "mtbench.dll"]
