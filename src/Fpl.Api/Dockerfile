#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["Fpl.Api/Fpl.Api.csproj", "Fpl.Api/"]
RUN dotnet restore "Fpl.Api/Fpl.Api.csproj"
COPY . .
WORKDIR "/src/Fpl.Api"
RUN dotnet build "Fpl.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Fpl.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Fpl.Api.dll"]