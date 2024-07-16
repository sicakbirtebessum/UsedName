using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using System;
using UsedName.GUI;
using UsedName.Manager;
// using XivCommon;

namespace UsedName
{
    public sealed class UsedName : IDalamudPlugin
    {

        private readonly WindowSystem windowSystem;


        public UsedName(IDalamudPluginInterface pluginInterface)
        {
            pluginInterface.Create<Service>();

            Service.Configuration = Service.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Service.Configuration.Initialize();

            // Service.Common = new XivCommonBase(pluginInterface);
            

            Service.Loc = new Localization();

            Service.PlayersNamesManager = new PlayersNamesManager();
            Service.GameDataManager = new GameDataManager();
            Service.Commands = new Commands();
            this.windowSystem = new WindowSystem("UsedName");

            Service.MainWindow = new MainWindow();
            Service.ConfigWindow = new ConfigWindow();
            Service.EditingWindow = new EditingWindow();
            Service.SubscriptionWindow = new SubscriptionWindow();

            this.windowSystem.AddWindow(Service.MainWindow);
            this.windowSystem.AddWindow(Service.ConfigWindow);
            this.windowSystem.AddWindow(Service.EditingWindow);
            this.windowSystem.AddWindow(Service.SubscriptionWindow);

            ContextMenu.Enable();
            Service.PluginInterface.UiBuilder.OpenMainUi += DrawMainUI;
            Service.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
            Service.PluginInterface.UiBuilder.Draw += DrawUI;

        }

        public void Dispose()
        {
            Service.Commands.Dispose();
            Service.GameDataManager.Dispose();
            //Service.Common.Dispose();
            ContextMenu.Disable();
            Service.Configuration.Save(storeName: true);

            Service.PluginInterface.UiBuilder.Draw -= DrawUI;
            Service.PluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUI;
            Service.PluginInterface.UiBuilder.OpenMainUi -= DrawMainUI;
#if DEBUG
            Service.Loc.StoreLanguage();
#endif
            GC.SuppressFinalize(this);
        }
        private void DrawUI() => this.windowSystem.Draw();
        private void DrawConfigUI()
        {
            Service.ConfigWindow.IsOpen = true;
        }

        private void DrawMainUI()
        {
            Service.MainWindow.IsOpen = true;
        }

    }
}
