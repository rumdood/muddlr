FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /app/out
WORKDIR /src
COPY . .
WORKDIR /src/Muddlr.Api

RUN dotnet restore "Muddlr.Api.csproj"
RUN dotnet build "Muddlr.Api.csproj" -c Release
RUN dotnet publish -c Release -o /app/out
WORKDIR /app/out

FROM base AS final
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "Muddlr.Api.dll"]