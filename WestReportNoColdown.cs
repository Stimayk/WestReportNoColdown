using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using Newtonsoft.Json;
using WestReportSystemApiReborn;

namespace WestReportNoCooldown
{
    public class WestReportNoCooldown : BasePlugin
    {
        public override string ModuleName => "WestReportNoCooldown";
        public override string ModuleVersion => "v1.0";
        public override string ModuleAuthor => "E!N";
        public override string ModuleDescription => "Module to disable report sending delay for administrators with a certain flag";

        private IWestReportSystemApi? _wrsApi;
        private NoCooldownConfig? _config;

        public override void OnAllPluginsLoaded(bool hotReload)
        {
            InitializeDependencies();

            string configDirectory = GetConfigDirectory();
            EnsureConfigDirectory(configDirectory);
            string configPath = Path.Combine(configDirectory, "NoCooldownConfig.json");
            _config = NoCooldownConfig.Load(configPath);

            if (_wrsApi == null)
            {
                Console.WriteLine($"{ModuleName} | Error: Essential services (WestReportSystem API) are not available.");
                return;
            }

            _wrsApi.OnReportSend += ResetCooldown;
            Console.WriteLine($"{ModuleName} | Successfully subscribed to report send events.");
        }

        private static string GetConfigDirectory()
        {
            return Path.Combine(Server.GameDirectory, "csgo/addons/counterstrikesharp/configs/plugins/WestReportSystem/Modules");
        }

        private void EnsureConfigDirectory(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
                Console.WriteLine($"{ModuleName} | Created configuration directory at: {directoryPath}");
            }
        }

        private void InitializeDependencies()
        {
            _wrsApi = IWestReportSystemApi.Capability.Get();
        }

        private void ResetCooldown(CCSPlayerController sender, CCSPlayerController violator, string reason)
        {
            var players = Utilities.GetPlayers();
            foreach (var player in players)
            {
                if (_config != null || _config?.NoCooldownAdmFlag != null)
                {
                    if (AdminManager.PlayerHasPermissions(player, _config.NoCooldownAdmFlag))
                    {
                        _wrsApi?.WRS_ClearCooldown(player);
                    }
                }
            }
        }

        public override void Unload(bool hotReload)
        {
            if (_wrsApi != null)
            {
                _wrsApi.OnReportSend -= ResetCooldown;
            }
        }

        public class NoCooldownConfig
        {
            public string NoCooldownAdmFlag { get; set; } = "@css/root";

            public static NoCooldownConfig Load(string configPath)
            {
                if (!File.Exists(configPath))
                {
                    NoCooldownConfig defaultConfig = new();
                    File.WriteAllText(configPath, JsonConvert.SerializeObject(defaultConfig, Formatting.Indented));
                    return defaultConfig;
                }

                string json = File.ReadAllText(configPath);
                return JsonConvert.DeserializeObject<NoCooldownConfig>(json) ?? new NoCooldownConfig();
            }
        }
    }
}