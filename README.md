# Real Estates Watcher 🔍
🏦 Simple C# command-line application for periodic watching of selected Real estate advertisement portals and sending notifications on new ads. 🏗
Supports watching adverts for **sells** as well as **leases**.

[![Build and publish .NET commandline script](https://github.com/supermartzin/real-estates-watcher/actions/workflows/dotnet.yml/badge.svg?branch=main)](https://github.com/supermartzin/real-estates-watcher/actions/workflows/dotnet.yml)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=supermartzin_real-estates-watcher&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=supermartzin_real-estates-watcher)

**Frameworks:** .NET 8, Node.js (for web scraping script)

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

    dotnet RealEstatesWatcher.UI.Console.dll --h <path to handlers.ini file> --p <path to portals.ini file> --f <path to filters.ini file> --e <path to engine.ini file>
    
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
 
 5. Make sure you have a **/configs** folder in the publish directory with all the configuration files or copy them manually from [**RealEstatesWatcher.UI.Console/configs/** folder](https://github.com/supermartzin/real-estates-watcher/tree/main/RealEstatesWatcher.UI.Console/configs)
    * `handlers.ini`
    * `portals.ini`
    * `filters.ini`
    * `engine.ini`

 7. **Deploy** the whole **publish directory** to server or run locally.
 
 8.  On the target machine, enter `scraper` folder and **install** all required **Node.js dependencies** with command
 
      `npm install`
      
      ##### * It's important to do this on the target platform as the dependencies are platform-specific

### 🐋 Using Docker (recommended)

There is a `Dockerfile` in the [**root**](https://github.com/supermartzin/real-estates-watcher) of the repository that you can use to automatic build and setup Docker image and run the app in containerized environment.
Simply use basic build and run commands:

    docker build -t real-estates-watcher:latest .
    docker run -itd real-estates-watcher:latest

## 📝 Configuration files description

**engine.ini** - configuration of the watching engine (*required* cmd argument `--e` or `-engine`)

    [settings]
    check_interval_minutes=            # <number> | required | periodic checking interval, minimum 1 minute
    enable_multiple_portal_instances=  # <bool>   | optional | enable/disable multiple instances of the same portal (in case of watching multiple URLs of the same portal)
    
    
**portals.\*** - configuration of all Ad portals to watch (*required* cmd argument `--p` or `-portals`)

❗**CHANGE**❗(since **v1.4**)
* Newline separated file with all URLs that you want to be watched.
* If you don't want to watch specific portal, just comment whole line with (`// https://bazos.cz/...`) or remove it completely from the config file.

<pre>
https://reality.idnes.cz/s/prodej/domy/cena-do-7000000/...<b>↩</b>
https://reality.idnes.cz/s/prodej/pozemky/...<b>↩</b>
https://www.sreality.cz/hledani/prodej/domy/...<b>↩</b>
https://reality.bazos.cz/prodam/dum/...<b>↩</b>
https://reality.bazos.cz/prodam/chata/...<b>↩</b>
</pre>

**handlers.ini** - configuration of the classes handling the received Ad posts (*required* cmd argument `--h` or `-handlers`)

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
    new_posts_path=               # <bool>    | optional | path to the file where to save new Ad posts (required when separate_new_posts=true)
    
**filters.ini** - configuration of the Ads filter (*optional* cmd argument `--f` or `-filters`)

    [basic]
    price_min=          # <number>  | optional | minimal price of Real estate
    price_max=          # <number>  | optional | maximal price of Real estate
    layouts=            # <strings> | optional | layouts of Real estate (allowed enum values below)(separated by comma)
    floor_area_min=     # <number>  | optional | minimal floor area of Real estate
    floor_area_max=     # <number>  | optional | maximal floor area of Real estate
    
 * Leave any of the values empty if you don't want to filter Ad posts by it
 * **Layout** option supported values: `1+1, 1+kk, 2+1, 2+kk, 3+1, 3+kk, 4+1, 4+kk, 5+1, 5+kk`
