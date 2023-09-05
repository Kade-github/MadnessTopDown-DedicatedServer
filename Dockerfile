FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["DedicatedServerCore/DedicatedServerCore.csproj", "DedicatedServerCore/"]
RUN dotnet restore "DedicatedServerCore/DedicatedServerCore.csproj"
COPY . .
WORKDIR "/src/DedicatedServerCore"
RUN dotnet build "DedicatedServerCore.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DedicatedServerCore.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DedicatedServerCore.dll"]