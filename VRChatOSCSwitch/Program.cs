using Newtonsoft.Json;

namespace VRChatOSCSwitch
{
    static class Program
    {
        static Random Rnd = new Random();
        static OSCServer? Host;
        static LogSystem MainLog = new LogSystem("MainPro");
        static string SettingsPath = Directory.GetCurrentDirectory() + "\\settings.json";

        [STAThread]
        static int Main(string[] Args)
        {
            Reload:

            bool FileCheck = File.Exists(SettingsPath);
            if (!FileCheck)
            {
                CreateJSON();

                MainLog.PrintMessage(LogSystem.MsgType.Warning, "The settings.json file was missing. A template has been created.", SettingsPath);
                MainLog.PrintMessage(LogSystem.MsgType.Warning, "Please fill it up with the programs you want the server to handle.");
                MainLog.PrintMessage(LogSystem.MsgType.Information, "Press any key to close the program.");

                Console.ReadKey();
                return 0;
            }

            string? JSON = null;
            using (StreamReader SR = new StreamReader(SettingsPath))
                JSON = SR.ReadToEnd();

            if (JSON != null)
            {
                Host = JsonConvert.DeserializeObject<OSCServer>(JSON);

                if (Host == null)
                {
                    CreateJSON();

                    MainLog.PrintMessage(LogSystem.MsgType.Error, "The settings.json file is invalid and has been recreated.", SettingsPath);
                    MainLog.PrintMessage(LogSystem.MsgType.Warning, "Please fill it up with the programs you want the server to handle.");
                    MainLog.PrintMessage(LogSystem.MsgType.Information, "Press any key to close the program.");

                    Console.ReadKey();
                    return 0;
                }

                Host.PrepareServer();

                bool Quit = false;
                while (!Quit)
                {
                    if (Quit)
                        break;

                    string[] CArgs = Console.ReadLine().ToLower().Split(' ');

                    switch (CArgs[0])
                    {
                        case "quitserver":
                            Quit = true;
                            Console.ForegroundColor = ConsoleColor.Green;
                            MainLog.PrintMessage(LogSystem.MsgType.Information, "Quitting...");
                            break;

                        case "reloadserver":
                            Host.TerminateServer();
                            Console.Clear();
                            goto Reload;

                        case "motivateme":
                            string[] Msgs = Properties.Resources.MotivationalMessages.Split('\n');
                            MainLog.PrintMessage(LogSystem.MsgType.Information, Msgs[Rnd.Next(0, Msgs.Length - 1)]);
                            break;

                        // Test
                        case "vibe":
                        /*
                        
                        HttpClient Client = new HttpClient();
                        var Response = Client.GetStringAsync("http://ip:port/command?v=0&t=id");
                        break;
                        
                        */

                        default:
                            break;
                    }

                    Console.ResetColor();
                };
            }

            return 0;
        }

        static void CreateJSON()
        {
            Host = new OSCServer(9000, 9001, 8000, 8001, true,
                new OSCProgram[1] {
                    new OSCProgram("TargetAppName", true, 10000, 10001, 9000, "C:\\TargetApp.exe", "--osc=$InPort$:127.0.0.1:$OutPort$",
                    new OSCAddress[2] {
                        new OSCAddress("/avatar/parameters", new string[2] { "param1", "param2" } ),
                        new OSCAddress("/something/else", new string[2] { "cpu", "ram" } )}) },
                    new OSCAddressHTTP[1] { 
                        new OSCAddressHTTP("http://sas:420/hitme", 
                            new OSCAddressHTTPItem[2] {
                                new OSCAddressHTTPItem("constsos", "int", "100", 0, 10),
                                new OSCAddressHTTPItem("oscsas", "float", null, 2, 5)
                            })
                    }
                );

            File.WriteAllText(SettingsPath, JsonConvert.SerializeObject(Host, Formatting.Indented));
        }
    }
}