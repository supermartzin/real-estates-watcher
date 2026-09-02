# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY "Directory.Packages.props" "Directory.Packages.props"
COPY "RealEstatesWatcher.UI.Console/Properties/PublishProfiles" "RealEstatesWatcher.UI.Console/Properties/PublishProfiles/"
COPY "RealEstatesWatcher.UI.Console" "RealEstatesWatcher.UI.Console/"
COPY "Filters/RealEstatesWatcher.AdPostsFilters.BasicFilter" "Filters/RealEstatesWatcher.AdPostsFilters.BasicFilter/"
COPY "Filters/RealEstatesWatcher.AdPostsFilters.Contracts/RealEstatesWatcher.AdPostsFilters.Contracts.csproj" "Filters/RealEstatesWatcher.AdPostsFilters.Contracts/"
COPY "RealEstatesWatcher.Models/RealEstatesWatcher.Models.csproj" "RealEstatesWatcher.Models/"
COPY "Handlers/RealEstatesWatcher.AdPostsHandlers.Email/RealEstatesWatcher.AdPostsHandlers.Email.csproj" "Handlers/RealEstatesWatcher.AdPostsHandlers.Email/"
COPY "Handlers/RealEstatesWatcher.AdPostsHandlers.Contracts/RealEstatesWatcher.AdPostsHandlers.Contracts.csproj" "Handlers/RealEstatesWatcher.AdPostsHandlers.Contracts/"
COPY "Handlers/RealEstatesWatcher.AdPostsHandlers.Base.Html/RealEstatesWatcher.AdPostsHandlers.Base.Html.csproj" "Handlers/RealEstatesWatcher.AdPostsHandlers.Base.Html/"
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
COPY "Tools/RealEstatesWatcher.Tools.Attributes/RealEstatesWatcher.Tools.Attributes.csproj" "Tools/RealEstatesWatcher.Tools.Attributes/"
RUN dotnet restore "./RealEstatesWatcher.UI.Console/RealEstatesWatcher.UI.Console.csproj"
COPY . .
WORKDIR "/src/RealEstatesWatcher.UI.Console/"
RUN dotnet build "./RealEstatesWatcher.UI.Console.csproj" -c "$BUILD_CONFIGURATION" -o /app/build --no-restore

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./RealEstatesWatcher.UI.Console.csproj" \
    -c "$BUILD_CONFIGURATION" \
    -r linux-x64 \
    --self-contained true \
    -o /app/publish \
    /p:PublishProfile=Linux-profile

# The official image includes Node.js, Puppeteer 25.4.0, its matching Chrome,
# and the browser's runtime dependencies. The digest makes the supply chain reproducible.
FROM ghcr.io/puppeteer/puppeteer:25.4.0@sha256:d93009ac8e1b8f307d59847b82404b51fd1672ddd6bdd5a4016a1cd9b5afd94d AS final
ENV NODE_PATH=/home/pptruser/node_modules
WORKDIR /app
COPY --from=publish --chown=10042:10042 /app/publish .
COPY --chown=10042:10042 "Tools/scraper/index.js" "Tools/scraper/package.json" "Tools/scraper/package-lock.json" "/app/scraper/"
USER 10042
ENTRYPOINT ["./RealEstatesWatcher.UI.Console", "--e", "configs/engine.ini", "--h", "configs/handlers.ini", "--p", "configs/portals.ini"]
