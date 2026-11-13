# Real Estates Watcher 🔍
🏦 Simple C# command-line application for periodic watching of selected Real estate advertisement portals and sending notifications on new ads. 🏗
Supports watching adverts for **sells** as well as **leases**.

[![Build and publish .NET commandline script](https://github.com/supermartzin/real-estates-watcher/actions/workflows/dotnet.yml/badge.svg?branch=main)](https://github.com/supermartzin/real-estates-watcher/actions/workflows/dotnet.yml)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=supermartzin_real-estates-watcher&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=supermartzin_real-estates-watcher)

**Frameworks:** .NET 10, Node.js 22.12 or later (for the web scraping script)

**Supported OS:** Windows, macOS, Linux

### 🌐 Currently supported Ads portals:

 - [Bazoš.cz](https://www.bazos.cz/)
 - [Bezrealitky.cz](https://www.bezrealitky.cz)
 - [Bidli.cz](https://www.bidli.cz/)
 - [Bravis.cz](https://www.bravis.cz/)
 - [České Reality.cz](https://www.ceskereality.cz/)
 - [Flatzone.cz](https://www.flatzone.cz/)
 - [M&M Reality.cz](https://www.mmreality.cz/)
 - [Realcity.cz](https://www.realcity.cz/)
 - [Reality.iDNES.cz](https://reality.idnes.cz/)
 - [RE/MAX.cz](https://www.remax-czech.cz/)
 - [Sreality.cz](https://www.sreality.cz/)

___

## 🚀 How to run the application

Run the app by executing the following command

    dotnet RealEstatesWatcher.UI.Console.dll --config configs/watcher.ini
    
##### * Description of all command-line options can be obtained by running the `--help` option

## 🛠️ How to build, publish, deploy and prepare the application

### 🫳 Manually

Perform following steps and commands either from the [**root folder**](https://github.com/supermartzin/real-estates-watcher) (where the solution file is placed) or from the UI Console project in [**RealEstatesWatcher.UI.Console** folder](https://github.com/supermartzin/real-estates-watcher/tree/main/RealEstatesWatcher.UI.Console) in order to prepare the application from execution:

 1. **Restore** dependencies
      
      `dotnet restore`
      
 2. **Build**
      
      `dotnet build --configuration Release`
      
 3. **Publish** the UI Console project for desired platform

      `dotnet publish ./RealEstatesWatcher.UI.Console/RealEstatesWatcher.UI.Console.csproj -c Release`
      
    You can use your own publish parameters or use predefined profiles for Windows and Linux in [**RealEstatesWatcher.UI.Console/Properties/PublishProfiles/** folder](https://github.com/supermartzin/real-estates-watcher/tree/main/RealEstatesWatcher.UI.Console/Properties/PublishProfiles)
 
 4. **Copy Web scraper files** from [Tools/scraper folder](https://github.com/supermartzin/real-estates-watcher/tree/main/Tools/scraper) to **~/publish/scraper** directory.
 
 5. Copy [**config-templates/watcher.ini**](RealEstatesWatcher.UI.Console/config-templates/watcher.ini) to **RealEstatesWatcher.UI.Console/configs/watcher.ini** and edit it for your searches before building or publishing. The configured file is copied to **publish/configs/watcher.ini**. If configuring after publishing, create **publish/configs** and copy the template there as **watcher.ini**.

 6. **Deploy** the whole **publish directory** to server or run locally.
 
 7.  On the target machine, enter `scraper` folder and **install** the locked **Node.js dependencies** with command
 
      `npm ci`
      
      ##### * It's important to do this on the target platform as the dependencies are platform-specific

❗**CHANGE**❗(since **v1.4.3**) 👇

 8.  If an Ad portal requires specific **cookies** to be set during web scraping in order to reach the final ads page (to pass through GDPR consent page, etc.), fill all the cookies to `cookies.json` file as shown in the template. Remember to copy this file to the same **~/publish/scraper** directory as stated above.

     #### * For example *Sreality.cz* Ads portal requires cookies.

### 🐋 Using Docker (recommended)

There is a `Dockerfile` in the [**root**](https://github.com/supermartzin/real-estates-watcher) of the repository that you can use to automatic build and setup Docker image and run the app in containerized environment.
Prepare **watcher.ini** from the [template](RealEstatesWatcher.UI.Console/config-templates/watcher.ini), build the image, and mount your configuration into the container. Replace `/absolute/path/to/watcher.ini` with the absolute path to your file:

    docker build -t real-estates-watcher:latest .
    docker run -itd --mount type=bind,source=/absolute/path/to/watcher.ini,target=/app/configs/watcher.ini,readonly real-estates-watcher:latest

Runtime configs are excluded from the Docker build context by `.dockerignore`, so the mount is required. If you enable file output, set its paths to a writable directory or mounted volume.

## 📝 Configuration

All watcher settings are read once from a single INI file. Pass its path with **`--config`** (or **`-c`**):

```sh
dotnet RealEstatesWatcher.UI.Console.dll --config configs/watcher.ini
```

Start from [**the unified template**](RealEstatesWatcher.UI.Console/config-templates/watcher.ini). Relative file paths resolve from the current working directory. The Node.js executable path must be absolute when specified. `NLog.config` remains the logging configuration, and scraper cookies remain in their JSON data file.

### Migrating existing configurations

1. Copy the sections from `engine.ini`, `handlers.ini`, `filters.ini` and `scraper.ini` into `watcher.ini` without changing their setting names.
2. Convert the plain URL list from `portals.ini` to uniquely numbered entries under `[portals]`, as shown below. Change `//` comments to INI `;` comments.
3. Replace the old per-file command-line arguments with `--config configs/watcher.ini`.
4. Omit or comment out the **whole** `[basic]` or `[nodejs]` section, including its keys, to disable filtering or the scraper respectively. A dynamic portal still requires the Node.js scraper.

### Sections

**`[general]` and `[gcloud]`** - configuration of the watching engine and Google Cloud logging

❗**CHANGE**❗(since **v1.6**) 👇
* Added support for **setting the exact time** of day of the periodic checks and ability to **start** the application **without the initial ads check** if the time is set.
* Added integration for sending the logs to **Google Cloud Logging service**.
* First section name changed from `[settings]` to `[general]`.

<!-- end of the list -->

    [general]
    check_interval_minutes=            # <number> | required | periodic checking interval, minimum 1 minute
    start_periodic_check_at=           # <time>   | optional | exact time of day to start periodic checks (format: HH:mm:ss)
    perform_check_on_startup=          # <bool>   | optional | switch to indicate whether to perform the initial check of all portals on application startup when exact time of day for periodic checks is set (see 'start_periodic_check_at' setting below).
    enable_multiple_portal_instances=  # <bool>   | optional | enable/disable multiple instances of the same portal (in case of watching multiple URLs of the same portal)
    [gcloud]
    enable_cloud_logging=              # <bool>   | optional | switch to enable sending logs to Google Cloud Logging service
    application_id=                    # <string> | optional | identifier of the application instance in Google Cloud Logging service (defaults to randomly generated GUID)
    project_id=                        # <string> | optional | identifier of the Google Cloud project (required when running outside of Google Cloud environment, applicable only in DEBUG mode).
    service_name=                      # <string> | optional | name of the service to be used in Google Cloud Logging service (required when running outside of Google Cloud environment, applicable only in DEBUG mode).


**`[portals]`** - all search URLs to watch

Use a unique numeric key for each URL. INI comments (`;`) disable entries; blank values are ignored. URLs can contain query parameters and equals signs.

```ini
[portals]
0=https://reality.idnes.cz/s/prodej/domy/cena-do-7000000/
1=https://reality.idnes.cz/s/prodej/pozemky/
; 2=https://www.sreality.cz/hledani/prodej/domy/
3=https://reality.bazos.cz/prodam/dum/
```

**`[email]` and `[file]`** - configuration of the classes handling the received Ad posts

❗**CHANGE**❗(since **v1.4.7**)

    [email]         
    enabled=                      # <bool>    | required | enable/disable this handler
    from=                         # <string>  | required | email address for outgoing notifications
    to=                           # <strings> | required | list of email addresses where to send notifications (separated by comma)
    cc=                           # <strings> | optional | list of email CC addresses where to send notifications (separated by comma)
    bcc=                          # <strings> | optional | list of email BCC addresses where to send notifications (separated by comma)
    sender_name=                  # <string>  | required | name of the sending entity for the email_address_from
    username=                     # <string>  | required | login username of sending email account
    password=                     # <string>  | required | login password of sending email account
    smtp_server_host=             # <string>  | required | URL of the SMTP server that handles sending notification emails
    smtp_server_port=             # <string>  | required | port of the SMTP server that handles sending notification emails
    use_secure_connection=        # <bool>    | required | switch for using TLS connection to the SMTP server
    skip_initial_notification=    # <bool>    | required | switch to disable sending the initial list of current Real estate offers
    [file]
    enabled=                      # <bool>    | required | enable/disable this handler
    main_path=                    # <string>  | required | path to the file where to save initial and new Ad posts
    separate_new_posts=           # <bool>    | optional | set 'true' if you want to save new Ad posts in separate file from the initial list
    new_posts_path=               # <string>    | optional | path to the file where to save new Ad posts (required when separate_new_posts=true)
    print_format=                 # <strings> | optional | format of printing parsed Ads to file, (allowed enum values below) (defaults to 'PlainText')
    
**`[basic]`** - optional configuration of the Ads filter (omit the section to disable filtering)

    [basic]
    price_min=          # <number>  | optional | minimal price of Real estate
    price_max=          # <number>  | optional | maximal price of Real estate
    layouts=            # <strings> | optional | layouts of Real estate (allowed enum values below)(separated by comma)
    floor_area_min=     # <number>  | optional | minimal floor area of Real estate
    floor_area_max=     # <number>  | optional | maximal floor area of Real estate
    
 * Leave any of the values empty if you don't want to filter Ad posts by it
 * **Layout** option supported values: `1+1, 1+kk, 2+1, 2+kk, 3+1, 3+kk, 4+1, 4+kk, 5+1, 5+kk`
 * **Format** option supported values: `PlainText, Html` (case-insensitive)

❗**ADDED**❗(since **v1.4.6**) 👇

**`[nodejs]`** - optional configuration of the Web scraper for dynamic portal pages requiring JavaScript (omit the section when no scraper is needed)

    [nodejs]
    path_to_node_executable=          # <string>  | optional | absolute path to Node.js (defaults to the standard platform install path)
    path_to_script=                   # <string>  | required | path to web scraping script
    page_scraping_timeout_seconds=    # <number>  | required | maximum timeout for scraping a single webpage (when set to 0 waits indefinitely)
    path_to_cookies_file=             # <string>  | optional | path to file with cookies that may be needed with some Ad portals (see README)
