# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
USER app
WORKDIR /app

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY "RealEstatesWatcher.UI.Console/Properties/PublishProfiles" "RealEstatesWatcher.UI.Console/Properties/PublishProfiles/"
COPY "RealEstatesWatcher.UI.Console" "RealEstatesWatcher.UI.Console/"
COPY "Filters/RealEstatesWatcher.AdPostsFilters.BasicFilter" "Filters/RealEstatesWatcher.AdPostsFilters.BasicFilter/"
COPY "Filters/RealEstatesWatcher.AdPostsFilters.Contracts/RealEstatesWatcher.AdPostsFilters.Contracts.csproj" "Filters/RealEstatesWatcher.AdPostsFilters.Contracts/"
COPY "RealEstatesWatcher.Models/RealEstatesWatcher.Models.csproj" "RealEstatesWatcher.Models/"
COPY "Handlers/RealEstatesWatcher.AdPostsHandlers.Email/RealEstatesWatcher.AdPostsHandlers.Email.csproj" "Handlers/RealEstatesWatcher.AdPostsHandlers.Email/"
COPY "Handlers/RealEstatesWatcher.AdPostsHandlers.Contracts/RealEstatesWatcher.AdPostsHandlers.Contracts.csproj" "Handlers/RealEstatesWatcher.AdPostsHandlers.Contracts/"
COPY "Handlers/RealEstatesWatcher.AdPostsHandlers.File/RealEstatesWatcher.AdPostsHandlers.File.csproj" "Handlers/RealEstatesWatcher.AdPostsHandlers.File/"
COPY "Portals/RealEstatesWatcher.AdsPortals.BazosCz/RealEstatesWatcher.AdsPortals.BazosCz.csproj" "Portals/RealEstatesWatcher.AdsPortals.BazosCz/"
COPY "Portals/RealEstatesWatcher.AdsPortals.Base/RealEstatesWatcher.AdsPortals.Base.csproj" "Portals/RealEstatesWatcher.AdsPortals.Base/"
COPY "Portals/RealEstatesWatcher.AdsPortals.Contracts/RealEstatesWatcher.AdsPortals.Contracts.csproj" "Portals/RealEstatesWatcher.AdsPortals.Contracts/"
COPY "Scrapers/RealEstatesWatcher.Scrapers.Contracts/RealEstatesWatcher.Scrapers.Contracts.csproj" "Scrapers/RealEstatesWatcher.Scrapers.Contracts/"
COPY "Portals/RealEstatesWatcher.AdsPortals.BidliCz/RealEstatesWatcher.AdsPortals.BidliCz.csproj" "Portals/RealEstatesWatcher.AdsPortals.BidliCz/"
COPY "Portals/RealEstatesWatcher.AdsPortals.BravisCz/RealEstatesWatcher.AdsPortals.BravisCz.csproj" "Portals/RealEstatesWatcher.AdsPortals.BravisCz/"
COPY "Portals/RealEstatesWatcher.AdsPortals.CeskeRealityCz/RealEstatesWatcher.AdsPortals.CeskeRealityCz.csproj" "Portals/RealEstatesWatcher.AdsPortals.CeskeRealityCz/"
COPY "Portals/RealEstatesWatcher.AdsPortals.FlatZoneCz/RealEstatesWatcher.AdsPortals.FlatZoneCz.csproj" "Portals/RealEstatesWatcher.AdsPortals.FlatZoneCz/"
COPY "Portals/RealEstatesWatcher.AdsPortals.MMRealityCz/RealEstatesWatcher.AdsPortals.MMRealityCz.csproj" "Portals/RealEstatesWatcher.AdsPortals.MMRealityCz/"
COPY "Portals/RealEstatesWatcher.AdsPortals.RealcityCz/RealEstatesWatcher.AdsPortals.RealcityCz.csproj" "Portals/RealEstatesWatcher.AdsPortals.RealcityCz/"
COPY "Portals/RealEstatesWatcher.AdsPortals.RealityIdnesCz/RealEstatesWatcher.AdsPortals.RealityIdnesCz.csproj" "Portals/RealEstatesWatcher.AdsPortals.RealityIdnesCz/"
COPY "Portals/RealEstatesWatcher.AdsPortals.RemaxCz/RealEstatesWatcher.AdsPortals.RemaxCz.csproj" "Portals/RealEstatesWatcher.AdsPortals.RemaxCz/"
COPY "Portals/RealEstatesWatcher.AdsPortals.SrealityCz/RealEstatesWatcher.AdsPortals.SrealityCz.csproj" "Portals/RealEstatesWatcher.AdsPortals.SrealityCz/"
COPY "Portals/RealEstatesWatcher.AdsPortals.BezrealitkyCz/RealEstatesWatcher.AdsPortals.BezrealitkyCz.csproj" "Portals/RealEstatesWatcher.AdsPortals.BezrealitkyCz/"
COPY "RealEstatesWatcher.Core/RealEstatesWatcher.Core.csproj" "RealEstatesWatcher.Core/"
COPY "Scrapers/RealEstatesWatcher.Scrapers/RealEstatesWatcher.Scrapers.csproj" "Scrapers/RealEstatesWatcher.Scrapers/"
RUN dotnet restore "./RealEstatesWatcher.UI.Console/RealEstatesWatcher.UI.Console.csproj"
COPY . .
WORKDIR "/src/RealEstatesWatcher.UI.Console/"
RUN dotnet build "./RealEstatesWatcher.UI.Console.csproj" -c $BUILD_CONFIGURATION -o /app/build --no-restore

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
COPY "Tools/scraper/" "/app/publish/scraper/"
RUN dotnet publish "./RealEstatesWatcher.UI.Console.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:PublishProfile=Linux-profile
# install Node.js runtime for external Puppeteer web scraper
WORKDIR /app/publish/scraper
RUN apt-get update -yq && apt-get upgrade -yq && apt-get install -yq curl git nano
RUN curl -sL https://deb.nodesource.com/setup_23.x | bash - && apt-get install -yq nodejs build-essential
RUN npm install -g npm
RUN npm install

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RealEstatesWatcher.UI.Console.dll", "--e", "configs/engine.ini", "--h", "configs/handlers.ini", "--p", "configs/portals.ini"]