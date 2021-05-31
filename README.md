# Real Estate Watcher 🔍
🏦 Simple C# command-line application for periodic watching of selected Real estate advertisement portals and sending notifications on new ads. 🏗

[![Build and publish .NET commandline script](https://github.com/supermartzin/real-estates-watcher/actions/workflows/dotnet.yml/badge.svg?branch=main)](https://github.com/supermartzin/real-estates-watcher/actions/workflows/dotnet.yml)

**Frameworks:** .NET 5 & .NET Standard 2.1, Node.js (for web scraping script)

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

## 🙌 How to build, publish, deploy and prepare the application

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

## 📝 Configuration files description

**engine.ini** - configuration of the watching engine (*required* cmd argument `--e` or `-engine`)

    [settings]
    check_interval_minutes=10     # <number> | required | periodic checking interval, minimum 1 minute
    
**portals.ini** - configuration of all Ad portals to watch (*required* cmd argument `--p` or `-portals`)

    [<portal-name>]               # <string> | required | name of the portal name
    url=                          # <string> | required | url to a followed page with Ad posts
   
* Each portal needs to have a separate section in the file with the URL.
* If you don't want to watch specific portal, just comment its section or remove it completely from the config file.

**handlers.ini** - configuration of the classes handling the received Ad posts (*required* cmd argument `--h` or `-handlers`)

    [email]         
    enabled=                      # <bool>    | required | enable/disable this handler
    email_address_from=           # <string>  | required | email address for outgoing notifications
    email_addresses_to=           # <strings> | required | list of email addresses where to send notifications (separated by comma)
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
