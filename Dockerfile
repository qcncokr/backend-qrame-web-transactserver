FROM mcr.microsoft.com/dotnet/core/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 7002

FROM mcr.microsoft.com/dotnet/core/sdk:5.0 AS build
WORKDIR /src
COPY ["Qrame.Web.TransactServer/Qrame.Web.TransactServer.csproj", "Qrame.Web.TransactServer/"]
RUN dotnet restore "Qrame.Web.TransactServer/Qrame.Web.TransactServer.csproj"
COPY . .
WORKDIR "/src/Qrame.Web.TransactServer"
RUN dotnet build "Qrame.Web.TransactServer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Qrame.Web.TransactServer.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Qrame.Web.TransactServer.dll"]