using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PlayniteMameMetadata
{
    public sealed class MameMetadataPlugin : MetadataPlugin
    {
        private const string NotificationId = "MameMetadata_IndexStatus";
        private static readonly ILogger Logger = LogManager.GetLogger();
        private readonly MameIndexService indexService;
        private readonly MameGameIdentifier identifier = new MameGameIdentifier();

        public override Guid Id { get; } = Guid.Parse("0d873564-ca47-40b3-a77d-fb8b2afe2fdd");

        public override string Name => "MAME DAT Metadata";

        public override List<MetadataField> SupportedFields { get; } = new List<MetadataField>
        {
            MetadataField.Name,
            MetadataField.ReleaseDate,
            MetadataField.Publishers,
            MetadataField.Links,
            MetadataField.Platform
        };

        public MameMetadataPlugin(IPlayniteAPI api) : base(api)
        {
            Properties = new MetadataPluginProperties { HasSettings = false };
            indexService = new MameIndexService(GetPluginUserDataPath());
            try
            {
                var cached = indexService.LoadCachedIndex();
                if (cached != null)
                {
                    Logger.Info($"Loaded MAME DAT {cached.Version} with {cached.Count} machines.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to load cached MAME DAT index.");
            }
        }

        public override OnDemandMetadataProvider GetMetadataProvider(MetadataRequestOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return new MameMetadataProvider(options.GameData, indexService.Current, identifier);
        }

        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
            Logger.Info("MAME DAT Metadata application-start initialization scheduled.");
            if (indexService.Current == null)
            {
                AddNotification(
                    "MAME DAT Metadata is preparing its first local index. Metadata will be available when this notification updates.",
                    NotificationType.Info);
            }

            Task.Run(() => UpdateInBackgroundAsync());
        }

        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
        {
            yield return new MainMenuItem
            {
                MenuSection = "@MAME DAT Metadata",
                Description = "Update MAME DAT index",
                Action = _ => UpdateInteractively()
            };
        }

        private async Task UpdateInBackgroundAsync()
        {
            try
            {
                var result = await indexService.UpdateAsync(
                    false,
                    null,
                    phase => Logger.Info(phase),
                    CancellationToken.None).ConfigureAwait(false);
                if (result.Changed)
                {
                    AddNotification(
                        $"MAME DAT Metadata is ready ({result.Index.Version}, {result.Index.Count:N0} machines).",
                        NotificationType.Info);
                }
                else
                {
                    Logger.Info($"MAME DAT {result.Index.Version} is already current ({result.Index.Count} machines).");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to update the MAME DAT index in the background.");
                if (indexService.Current == null)
                {
                    AddNotification("MAME DAT Metadata could not initialize: " + ex.Message, NotificationType.Error);
                }
            }
        }

        private void UpdateInteractively()
        {
            MameIndexUpdateResult updateResult = null;
            var result = PlayniteApi.Dialogs.ActivateGlobalProgress(async progressArgs =>
            {
                progressArgs.IsIndeterminate = true;
                var downloadProgress = new Progress<double>(value =>
                {
                    progressArgs.IsIndeterminate = false;
                    progressArgs.ProgressMaxValue = 100;
                    progressArgs.CurrentProgressValue = value;
                });
                updateResult = await indexService.UpdateAsync(
                    true,
                    downloadProgress,
                    phase =>
                    {
                        progressArgs.Text = phase;
                        if (phase.IndexOf("Downloading", StringComparison.OrdinalIgnoreCase) < 0)
                        {
                            progressArgs.IsIndeterminate = true;
                        }
                    },
                    progressArgs.CancelToken).ConfigureAwait(false);
            }, new GlobalProgressOptions("Updating the MAME DAT index...", true));

            if (result.Error != null)
            {
                Logger.Error(result.Error, "Failed to update the MAME DAT index.");
                PlayniteApi.Dialogs.ShowErrorMessage(result.Error.Message, "MAME DAT Metadata");
            }
            else if (!result.Canceled && updateResult?.Index != null)
            {
                PlayniteApi.Dialogs.ShowMessage(
                    $"MAME DAT {updateResult.Index.Version} is ready with {updateResult.Index.Count:N0} machines.",
                    "MAME DAT Metadata");
            }
        }

        private void AddNotification(string text, NotificationType type)
        {
            PlayniteApi.MainView.UIDispatcher.BeginInvoke(new Action(() =>
            {
                PlayniteApi.Notifications.Add(new NotificationMessage(NotificationId, text, type));
            }));
        }
    }
}
