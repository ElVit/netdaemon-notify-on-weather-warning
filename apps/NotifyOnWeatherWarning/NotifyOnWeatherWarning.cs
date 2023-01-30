using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reactive.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NetDaemon.AppModel;
using NetDaemon.Client;
using NetDaemon.Client.HomeAssistant.Extensions;
using NetDaemon.HassModel;
using NetDaemon.HassModel.Entities;

// Use unique namespaces for your apps if you going to share with others to avoid conflicting names
namespace NotifyOnWeatherWarning;

public class NotifyOnWeatherWarningConfig
{
  public string? NotifyTitlePrefix { get; set; }
  public string? NotifyId { get; set; }
  public bool? PersistentNotification { get; set; }
  public string? WeatherWarningEntity { get; set; }
  public IEnumerable<string>? MobileNotifyServices { get; set; }
}

/// <summary>
/// Creates a notification in Home Assistant if a weather warning is available
/// </summary>
[NetDaemonApp]
public class NotifyOnWeatherWarningApp : IAsyncInitializable
{
  private readonly IHaContext mHaContext;
  private readonly IHomeAssistantConnection mHaConnection;
  private readonly ILogger<NotifyOnWeatherWarningApp> mLogger;
  private string mNotifyTitlePrefix;
  private string mNotifyId;
  private bool mPersistentNotification;
  private string mWeatherWarningEntity;
  private IEnumerable<string> mMobileNotifyServices;


  public async Task InitializeAsync(CancellationToken cancellationToken)
  {
    mLogger.LogDebug($"*** InitializeAsync started ***");

    // Check if user defined notify services are valid
    mMobileNotifyServices = await GetServicesOfType("notify", mMobileNotifyServices);

    // Set weather warning notification on startup
    var warningsEntity = new NumericEntity(mHaContext, mWeatherWarningEntity);
    SetNotification(warningsEntity.EntityState);

    mLogger.LogDebug($"*** InitializeAsync finished ***");
  }

  public NotifyOnWeatherWarningApp(IHaContext ha,
                            IHomeAssistantConnection haConnection,
                            IAppConfig<NotifyOnWeatherWarningConfig> config,
                            ILogger<NotifyOnWeatherWarningApp> logger)
  {
    mHaContext = ha;
    mHaConnection = haConnection;
    mLogger = logger;

    mLogger.LogDebug($"*** Constructor started ***");

    // Check options against null and set a default value if true
    mNotifyTitlePrefix = config.Value.NotifyTitlePrefix ?? "";
    mNotifyId = config.Value.NotifyId ?? "weather_warning";
    mPersistentNotification = config.Value.PersistentNotification ?? true;
    mWeatherWarningEntity = config.Value.WeatherWarningEntity ?? "";
    mMobileNotifyServices = config.Value.MobileNotifyServices ?? new List<string>();

    // Check options against empty/invalid values and set a default value if true
    if (String.IsNullOrEmpty(config.Value.NotifyTitlePrefix))
    {
      mLogger.LogWarning($"Option 'NotifyTitlePrefix' not found. No default value is used.");
    }
    if (String.IsNullOrEmpty(config.Value.NotifyId))
    {
      mNotifyId = "weather_warning";
      mLogger.LogWarning($"Option 'NotifyId' not found. Default value '{mNotifyId}' is used.");
    }
    if (config.Value.PersistentNotification == null)
    {
      mLogger.LogWarning("Option 'PersistentNotification' not found. Default value 'true' is used.");
    }
    if (String.IsNullOrEmpty(config.Value.WeatherWarningEntity))
    {
      mLogger.LogError("Option 'WeatherWarningEntity' not found.");
      return;
    }
    else
    {
      var weatherEntity = mHaContext.GetAllEntities().FirstOrDefault(entity => entity.EntityId == mWeatherWarningEntity);
      if (weatherEntity == null)
      {
        mLogger.LogError($"Entity '{mWeatherWarningEntity}' not found.");
        return;
      }
    }

    // Set weather warning notification on state change
    var warningsEntity = new NumericEntity(mHaContext, mWeatherWarningEntity);
    warningsEntity.StateAllChanges().Subscribe(state => SetNotification(state.New));

    mLogger.LogDebug($"*** Constructor finished ***");
  }

  private async Task<IEnumerable<string>> GetServicesOfType(string serviceType, IEnumerable<string> definedServices)
  {
    var availableServices = new List<string>();

    mLogger.LogInformation($"{definedServices.Count()} notify service(s) defined.");
    if (definedServices.Count() < 1) return availableServices;

    var allServices = await mHaConnection.GetServicesAsync(CancellationToken.None).ConfigureAwait(false);
    var notifyService = new JsonElement();
    allServices.GetValueOrDefault().TryGetProperty(serviceType, out notifyService);
    var filteredServices = JsonSerializer.Deserialize<Dictionary<string, object>>(notifyService) ?? new Dictionary<string, object>();
    foreach (var definedService in definedServices)
    {
      var service = definedService;
      // If notifyService starts with "notify." then remove this part
      if (service.StartsWith("notify.")) service = service.Substring(7);

      if (filteredServices.ContainsKey(service))
      {
        availableServices.Add(service);
        mLogger.LogInformation($"- Service '{service}' is available");
      }
      else
      {
        mLogger.LogInformation($"- Service '{service}' is NOT available");
      }
    }

    return availableServices;
  }

  /// <summary>
  /// Sets a notification if there are any updates available
  /// </summary>
  private void SetNotification(NumericEntityState? entityState)
  {
    var notifyTitle = "";
    var notifyMessage = "";
    var notifyId = "";

    var attributes = entityState?.Attributes as Dictionary<string, object>;
    if (attributes != null)
    {
      foreach(var attribute in attributes)
      {
        mLogger.LogDebug($"Attribute: {attribute.Key} -> {attribute.Value}");
      }

      var state = entityState?.State ?? 0;
      mLogger.LogDebug($"Count of weather warnings: {state}");

      // Assume there may be max. 5 weather warnings at the same time
      for (int i = 1; i <= 5; i++)
      {
        if (i <= state)
        {
          notifyTitle = mNotifyTitlePrefix + attributes[$"warning_{i}_headline"].ToString();
          notifyMessage = attributes[$"warning_{i}_description"].ToString() ?? "";
        }
        else
        {
          notifyTitle = "";
          notifyMessage = "";
        }
        notifyId =  $"{mNotifyId}_{i}";

        if (mPersistentNotification) SetPersistenNotification(notifyTitle, notifyMessage, notifyId);
        if (mMobileNotifyServices.Any()) SetMobileNotification(mMobileNotifyServices, notifyTitle, notifyMessage, notifyId);
      }
    }
  }

  private void SetPersistenNotification(string title, string message, string id)
  {
    if (!String.IsNullOrEmpty(message))
    {
      mHaContext.CallService("persistent_notification", "create", data: new
        {
          title = title,
          message = message,
          notification_id = id
        });
    }
    else
    {
      mHaContext.CallService("persistent_notification", "dismiss", data: new
        {
          notification_id = id
        });
    }
  }

  private void SetMobileNotification(IEnumerable<string> services, string title, string message, string tag)
  {
    foreach (var service in services)
    {
      if (!String.IsNullOrEmpty(message))
      {
        mHaContext.CallService("notify", service, data: new
          {
            title = title,
            message = message,
            data = new
              {
                tag = tag,
                url = "/config/dashboard",          // iOS URL
                clickAction = "/config/dashboard",  // Android URL
                actions = new List<object>
                {
                  new
                    {
                      action = "URI",
                      title = "Open Addons",
                      uri = "/hassio/dashboard"
                    },
                  new
                    {
                      action = "URI",
                      title = "Open HACS",
                      uri = "/hacs"
                    },
                }
              }
          });
      }
      else
      {
        mHaContext.CallService("notify", service, data: new
          {
            message = "clear_notification",
            data = new
              {
                tag = tag
              }
          });
      }
    }
  }
}
