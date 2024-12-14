FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["YukiBot.csproj", "YukiBot/"]
RUN dotnet restore "YukiBot/YukiBot.csproj"
WORKDIR "/src/YukiBot"
COPY . .
RUN dotnet build "YukiBot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet build "YukiBot.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDir /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "YukiBot.dll"]
