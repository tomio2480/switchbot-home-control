using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using SwitchBotHomeControl.Api;
using SwitchBotHomeControl.Monitoring;
using SwitchBotHomeControl.Notifications;
using SwitchBotHomeControl.Tray;
using SwitchBotHomeControl.WebServer.Controllers;

namespace SwitchBotHomeControl;

class Program
{
    private static NotifyIcon? _trayIcon;
    private static WebApplication? _app;
    private static SwitchBotClient? _client;
    private static readonly int Port = 3000;

    // Discomfort index shown by the tray icon, updated by DiscomfortMonitorService every 10 minutes
    private static readonly LatestMeterReadings LatestReadings = new();
    private static readonly Color UnknownLevelColor = Color.Gray;
    private static string? _trayMeterName;
    private static Icon? _levelIcon;
    private static readonly List<ToolStripMenuItem> DiscomfortItems = new();

    [STAThread]
    static void Main(string[] args)
    {
        // Single instance check
        using var mutex = new Mutex(true, "SwitchBotHomeControl_SingleInstance", out bool createdNew);
        if (!createdNew)
        {
            // Another instance is running, open the browser
            OpenBrowser($"http://localhost:{Port}");
            return;
        }

        // Load environment variables
        // Find project root (directory containing .csproj file)
        string projectRoot = FindProjectRoot(AppDomain.CurrentDomain.BaseDirectory);
        var envPath = Path.Combine(projectRoot, ".env");

        if (File.Exists(envPath))
        {
            DotNetEnv.Env.Load(envPath);
        }

        var token = Environment.GetEnvironmentVariable("SWITCHBOT_TOKEN");
        var secret = Environment.GetEnvironmentVariable("SWITCHBOT_SECRET");

        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(secret))
        {
            MessageBox.Show(
                "Error: SWITCHBOT_TOKEN and SWITCHBOT_SECRET must be set in .env file",
                "Configuration Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
            return;
        }

        // Discomfort index notification is enabled only when a webhook URL is configured
        var discordWebhookUrl = Environment.GetEnvironmentVariable("DISCORD_WEBHOOK_URL");
        if (string.IsNullOrWhiteSpace(discordWebhookUrl))
        {
            discordWebhookUrl = null;
        }
        else if (!DiscordWebhookClient.IsValidWebhookUrl(discordWebhookUrl))
        {
            MessageBox.Show(
                "DISCORD_WEBHOOK_URL が Discord の Webhook URL ではありません。\n" +
                "https://discord.com/api/webhooks/... の形式で .env に設定してください。\n" +
                "不快指数の通知は無効のまま起動します。",
                "Configuration Warning",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
            discordWebhookUrl = null;
        }

        // The meter whose discomfort index colors the tray icon (defaults to the first meter)
        _trayMeterName = Environment.GetEnvironmentVariable("TRAY_METER_NAME");
        if (string.IsNullOrWhiteSpace(_trayMeterName))
        {
            _trayMeterName = null;
        }

        // Initialize SwitchBot client
        _client = new SwitchBotClient(token, secret);

        // Start web server in background thread
        var webServerThread = new Thread(() => StartWebServer(token, secret, discordWebhookUrl, projectRoot))
        {
            IsBackground = true
        };
        webServerThread.Start();

        // Wait a moment for server to start
        Thread.Sleep(1000);

        // Create tray icon
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        CreateTrayIcon();

        // Keep the application running
        Application.Run();

        // Cleanup
        _app?.StopAsync().Wait();
        _trayIcon?.Dispose();
        GC.KeepAlive(mutex);
    }

    static void StartWebServer(string token, string secret, string? discordWebhookUrl, string projectRoot)
    {
        try
        {
            var builder = WebApplication.CreateBuilder();

            // Add services
            builder.Services.AddControllers();
            builder.Services.AddSingleton(new SwitchBotClient(token, secret));

            // Meter readings are recorded for the history chart and the tray icon even without Discord
            var dataPath = Path.Combine(projectRoot, "data");
            builder.Services.AddSingleton(new MeterHistoryStore(Path.Combine(dataPath, "meter-history")));
            builder.Services.AddSingleton(new NotificationStateStore(Path.Combine(dataPath, "notification-state.json")));
            builder.Services.AddSingleton(LatestReadings);
            if (discordWebhookUrl != null)
            {
                builder.Services.AddSingleton(new DiscordWebhookClient(discordWebhookUrl));
            }
            builder.Services.AddHostedService<DiscomfortMonitorService>();

            // Configure Kestrel to listen on specific port
            builder.WebHost.UseUrls($"http://localhost:{Port}");

            _app = builder.Build();

            // Configure middleware
            var wwwrootPath = Path.Combine(projectRoot, "wwwroot");
            var fileProvider = new PhysicalFileProvider(wwwrootPath);

            _app.UseDefaultFiles(new DefaultFilesOptions
            {
                FileProvider = fileProvider,
                RequestPath = ""
            });

            _app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = fileProvider,
                RequestPath = ""
            });

            _app.MapControllers();

            Console.WriteLine($"Web server running at http://localhost:{Port}");
            _app.Run();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to start web server: {ex.Message}",
                "Server Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
            Application.Exit();
        }
    }

    static void CreateTrayIcon()
    {
        _levelIcon = CircleIcon.CreateIcon(UnknownLevelColor, SystemInformation.SmallIconSize.Width);
        _trayIcon = new NotifyIcon
        {
            Icon = _levelIcon,
            Text = TrayDiscomfortView.AppName,
            Visible = true
        };

        var contextMenu = new ContextMenuStrip();

        contextMenu.Items.Add("🏠 SwitchBot Home Control").Enabled = false;
        contextMenu.Items.Add(new ToolStripSeparator());

        var openItem = new ToolStripMenuItem("📱 コントロール画面を開く");
        openItem.Click += (s, e) => OpenBrowser($"http://localhost:{Port}");
        contextMenu.Items.Add(openItem);

        contextMenu.Items.Add(new ToolStripSeparator());

        // Discomfort index of every meter; the items are rebuilt on each monitoring cycle
        var discomfortHeader = new ToolStripMenuItem(TrayDiscomfortView.FormatHeader(null)) { Enabled = false };
        contextMenu.Items.Add(discomfortHeader);
        contextMenu.Items.Add(new ToolStripSeparator());

        // Creating the menu installed the WinForms synchronization context on this (UI) thread
        var uiContext = SynchronizationContext.Current
            ?? throw new InvalidOperationException("Tray icon must be created on the UI thread");
        LatestReadings.Updated += snapshot =>
            uiContext.Post(_ => ApplyDiscomfortSnapshot(contextMenu, discomfortHeader, snapshot), null);
        if (LatestReadings.Current is { } firstSnapshot)
        {
            ApplyDiscomfortSnapshot(contextMenu, discomfortHeader, firstSnapshot);
        }

        // Add placeholder for bulb controls
        var loadingItem = new ToolStripMenuItem("💡 電球を読込中...");
        loadingItem.Enabled = false;
        contextMenu.Items.Add(loadingItem);

        // Load devices when menu is opening
        contextMenu.Opening += async (s, e) =>
        {
            // Only load once
            if (contextMenu.Items.Contains(loadingItem))
            {
                try
                {
                    if (_client != null)
                    {
                        var devicesResponse = await _client.GetDevicesAsync();
                        if (devicesResponse.StatusCode == 100)
                        {
                            var bulbs = devicesResponse.Body.DeviceList
                                .Where(d => d.DeviceType.ToLower().Contains("bulb") ||
                                           d.DeviceType.ToLower().Contains("light"))
                                .ToList();

                            // Remove loading indicator
                            contextMenu.Items.Remove(loadingItem);

                            if (bulbs.Any())
                            {
                                foreach (var bulb in bulbs)
                                {
                                    var deviceId = bulb.DeviceId;
                                    var deviceName = bulb.DeviceName;

                                    // Add ON button
                                    var onItem = new ToolStripMenuItem($"💡 {deviceName} - ON");
                                    onItem.Click += (sender, args) => Task.Run(() => ControlDeviceAsync(deviceId, "on", deviceName));
                                    contextMenu.Items.Insert(contextMenu.Items.Count - 2, onItem);

                                    // Add OFF button
                                    var offItem = new ToolStripMenuItem($"💡 {deviceName} - OFF");
                                    offItem.Click += (sender, args) => Task.Run(() => ControlDeviceAsync(deviceId, "off", deviceName));
                                    contextMenu.Items.Insert(contextMenu.Items.Count - 2, offItem);

                                    // Add Toggle button
                                    var toggleItem = new ToolStripMenuItem($"💡 {deviceName} - Toggle");
                                    toggleItem.Click += (sender, args) => Task.Run(() => ControlDeviceAsync(deviceId, "toggle", deviceName));
                                    contextMenu.Items.Insert(contextMenu.Items.Count - 2, toggleItem);
                                }
                            }
                            else
                            {
                                var noDeviceItem = new ToolStripMenuItem("💡 電球デバイスなし");
                                noDeviceItem.Enabled = false;
                                contextMenu.Items.Insert(contextMenu.Items.Count - 2, noDeviceItem);
                            }
                        }
                        else
                        {
                            contextMenu.Items.Remove(loadingItem);
                            var errorItem = new ToolStripMenuItem($"💡 読込失敗: {devicesResponse.Message}");
                            errorItem.Enabled = false;
                            contextMenu.Items.Insert(contextMenu.Items.Count - 2, errorItem);
                        }
                    }
                }
                catch (Exception ex)
                {
                    contextMenu.Items.Remove(loadingItem);
                    var errorItem = new ToolStripMenuItem($"💡 エラー: {ex.Message}");
                    errorItem.Enabled = false;
                    contextMenu.Items.Insert(contextMenu.Items.Count - 2, errorItem);
                }
            }
        };

        contextMenu.Items.Add(new ToolStripSeparator());

        var exitItem = new ToolStripMenuItem("終了");
        exitItem.Click += (s, e) => ExitApplication();
        contextMenu.Items.Add(exitItem);

        _trayIcon.ContextMenuStrip = contextMenu;

        // Show menu on left click
        _trayIcon.MouseClick += (s, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                // Show context menu at cursor position
                var method = typeof(NotifyIcon).GetMethod("ShowContextMenu",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                method?.Invoke(_trayIcon, null);
            }
        };

        _trayIcon.DoubleClick += (s, e) => OpenBrowser($"http://localhost:{Port}");
    }

    /// <summary>
    /// Recolors the tray icon and rebuilds the per-meter menu items. Must run on the UI thread.
    /// </summary>
    static void ApplyDiscomfortSnapshot(ContextMenuStrip contextMenu, ToolStripMenuItem header, MeterSnapshot snapshot)
    {
        if (_trayIcon == null)
        {
            return;
        }

        header.Text = TrayDiscomfortView.FormatHeader(snapshot);

        foreach (var item in DiscomfortItems)
        {
            contextMenu.Items.Remove(item);
            item.Image?.Dispose();
            item.Dispose();
        }
        DiscomfortItems.Clear();

        var menuIconSize = SystemInformation.SmallIconSize.Width;
        if (snapshot.Records.Count == 0)
        {
            DiscomfortItems.Add(new ToolStripMenuItem("取得できませんでした") { Enabled = false });
        }
        foreach (var record in snapshot.Records)
        {
            var item = new ToolStripMenuItem(EscapeMnemonic(TrayDiscomfortView.FormatMenuLine(record)))
            {
                Image = CircleIcon.CreateBitmap(ColorTranslator.FromHtml(record.Level.ToColorHex()), menuIconSize)
            };
            item.Click += (s, e) => OpenBrowser($"http://localhost:{Port}");
            DiscomfortItems.Add(item);
        }

        var insertAt = contextMenu.Items.IndexOf(header) + 1;
        foreach (var item in DiscomfortItems)
        {
            contextMenu.Items.Insert(insertAt++, item);
        }

        var iconMeter = TrayDiscomfortView.SelectIconMeter(snapshot.Records, _trayMeterName);
        var color = iconMeter == null ? UnknownLevelColor : ColorTranslator.FromHtml(iconMeter.Level.ToColorHex());
        var previousIcon = _levelIcon;
        _levelIcon = CircleIcon.CreateIcon(color, SystemInformation.SmallIconSize.Width);
        _trayIcon.Icon = _levelIcon;
        _trayIcon.Text = TrayDiscomfortView.FormatTooltip(iconMeter);
        previousIcon?.Dispose();
    }

    /// <summary>
    /// Menu texts treat "&amp;" as an access key marker; device names must show it literally.
    /// </summary>
    static string EscapeMnemonic(string text) => text.Replace("&", "&&");

    static async Task ControlDeviceAsync(string deviceId, string command, string deviceName)
    {
        if (_client == null) return;

        try
        {
            var response = command switch
            {
                "on" => await _client.TurnOnAsync(deviceId),
                "off" => await _client.TurnOffAsync(deviceId),
                "toggle" => await _client.ToggleAsync(deviceId),
                _ => throw new ArgumentException("Invalid command")
            };

            if (response.StatusCode == 100)
            {
                string commandText = command == "on" ? "ON" : command == "off" ? "OFF" : "トグル"; // Default fallback
                string? actualPowerState = null;

                // Get actual device status to show accurate message for all commands
                for (int retry = 0; retry < 3; retry++)
                {
                    await Task.Delay(retry == 0 ? 1000 : 500); // 1s first, then 500ms retries

                    try
                    {
                        var statusResponse = await _client.GetDeviceStatusAsync(deviceId);
                        if (statusResponse.StatusCode == 100 && statusResponse.Body != null)
                        {
                            // Try to get power state from response
                            if (statusResponse.Body.ContainsKey("power"))
                            {
                                var powerState = statusResponse.Body["power"]?.ToString()?.ToLower();
                                if (!string.IsNullOrEmpty(powerState))
                                {
                                    actualPowerState = powerState;
                                    commandText = powerState == "on" ? "ON" : "OFF";
                                    break; // Success, exit retry loop
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Continue to next retry
                    }
                }

                ShowNotification("SwitchBot", $"{deviceName} を {commandText} にしました", ToolTipIcon.Info);
            }
            else
            {
                ShowNotification("エラー", $"コマンド送信失敗: {response.Message}", ToolTipIcon.Error);
            }
        }
        catch (Exception ex)
        {
            ShowNotification("エラー", $"エラー: {ex.Message}", ToolTipIcon.Error);
        }
    }

    static void ShowNotification(string title, string message, ToolTipIcon icon)
    {
        if (_trayIcon != null && _trayIcon.Visible)
        {
            try
            {
                // ShowBalloonTip can be called from any thread
                _trayIcon.ShowBalloonTip(3000, title, message, icon);
            }
            catch
            {
                // Ignore if tray icon is disposed
            }
        }
    }

    static void OpenBrowser(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to open browser: {ex.Message}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }

    static void ExitApplication()
    {
        try
        {
            _trayIcon?.Dispose();
            _app?.StopAsync().Wait(TimeSpan.FromSeconds(5));
        }
        catch
        {
            // Ignore cleanup errors
        }
        finally
        {
            Environment.Exit(0);
        }
    }

    static string FindProjectRoot(string startDirectory)
    {
        var directory = new DirectoryInfo(startDirectory);
        while (directory != null)
        {
            // Check if this directory contains a .csproj file (project root marker)
            var csprojFiles = directory.GetFiles("*.csproj");
            if (csprojFiles.Length > 0)
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        // If we can't find the project root, return the start directory
        return startDirectory;
    }
}
