FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["src/Muddlr.Api/Muddlr.Api.csproj", "src/Muddlr.Api/"]
RUN dotnet restore "src/Muddlr.Api/Muddlr.Api.csproj"
COPY . .
WORKDIR "/src/src/Muddlr.Api"
RUN dotnet build "Muddlr.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Muddlr.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Muddlr.Api.dll"]
