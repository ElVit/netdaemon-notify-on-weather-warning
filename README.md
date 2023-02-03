[![hacs_badge](https://img.shields.io/badge/HACS-Custom-41BDF5.svg)](https://github.com/hacs/integration)
[![netdaemon_badge](https://img.shields.io/badge/NetDaemon-v3-pink)](https://netdaemon.xyz/docs/v3)

# NetDaemonApp: Notify on Weather Warning
A NetDaemon App that will notify you if there is a weather warning available in Home Assistant.  
This notification can be set as a persistent notification or send to your mobile devices if you are using the [companion app](https://companion.home-assistant.io/).  

## Installation
1. Install the [NetDaemon V3.X](https://netdaemon.xyz/docs/v3/started/installation) Addon
2. Change the addon option "app_config_folder" to "/config/netdaemon"
3. Install [HACS](https://hacs.xyz/docs/setup/download)
4. In Home Assistant go to HACS -> Automation -> Add repository -> Search for "Notify on Weather Warning"
5. Download this repository with HACS
6. Restart the NetDaemon V3 Addon

## Configuration  

Example configuration:

```yaml
NotifyOnWeatherWarning.NotifyOnWeatherWarningConfig:
  NotifyTitlePrefix: ⛅
  NotifyId: weather_warning
  WeatherWarningEntity: sensor.dwd_weather_warnings_current_warning_level
  WeatherWarningFilter:
    - *FROST*
    - *HAGEL*
  PersistentNotification: true
  MobileNotifyServices:
    - notify.mobile_app_myphone
```

https://www.dwd.de/DE/leistungen/opendata/help/warnungen/warning_codes_pdf.pdf?__blob=publicationFile&v=5

## Options:

### Option: `NotifyTitlePrefix`

Defines the prefix title of the notification.  
Then the actual title is depending on the current weather warning.  
*Example title:* &nbsp;&nbsp;&nbsp;&nbsp; `⛅ Official WIND GUST WARNING`  
*Default value:* &nbsp;&nbsp;&nbsp; `⛅`

### Option: `NotifyId`

Defines the ID of the notification so it can be updated.  
Since there can be multipe weather warning at the same time an index is added to the NotifyId.  
*Example ID:* &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; `weather_warning_1`  
*Default value:* &nbsp;&nbsp;&nbsp; `weather_warning`  

### Option: `WeatherWarningFilter`

Defines a filter of `warning_x_name`'s where a notification shall be showed.  
It is also possible to set wildecards like `*` (for any multipe characters) and `?` (for any single character).  
For DWD Weather Warnings you can find all possible `warning_x_name`'s [here](https://www.dwd.de/DE/leistungen/opendata/help/warnungen/warning_codes_pdf.pdf?__blob=publicationFile&v=5).  

### Option: `WeatherWarningEntity`

Defines the entity for the weather warning sensor.  
This sensor has to be added manually through a Home Assistant Integration.  
A possible sensor could be e.g. `sensor.dwd_weather_warnings_current_warning_level` added through the [DWD Weather Warning](https://www.home-assistant.io/integrations/dwd_weather_warnings/) integration.  
*Default value:* &nbsp;&nbsp;&nbsp; `sensor.dwd_weather_warnings_current_warning_level`  

__NOTE:__ &nbsp; At the moment this app is only tested with the [DWD Weather Warning](https://www.home-assistant.io/integrations/dwd_weather_warnings/) integration.  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; If you want more integrations supported please create an [issue](https://github.com/ElVit/netdaemon-notify-on-weather-warning/issues).  

### Option: `PersistentNotification`

The persistent notification can be disabled if only mobile notifications are preferred.  
*Default value:* &nbsp;&nbsp;&nbsp; `true`  

### Option: `MobileNotifyServices`

A list of notify services for mobile apps like the iOS or Android [companion app](https://companion.home-assistant.io/).  
If the notify service is valid then a notify message will be sent to your mobile device as soon as there is an update available.  
The notify service can be definded like "notify.mobile_app_myphone" or just "mobile_app_myphone".  
*Default value:* &nbsp;&nbsp;&nbsp; `none`  

## Contribution
This App was developed with help of this Home Assistant Community Thread:  
https://community.home-assistant.io/t/update-notifications-core-hacs-supervisor-and-addons/182295  

Also a special thanks to [FrankBakkerNl](https://github.com/FrankBakkerNl) and [helto4real](https://github.com/helto4real) for their support during development via [discord](https://discord.gg/K3xwfcX).
