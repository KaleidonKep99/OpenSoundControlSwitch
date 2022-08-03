using Newtonsoft.Json;
using System.Net.Sockets;

namespace OpenSoundControlSwitch
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
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(ProcExit);

        Reload:
            try
            {
                bool FileCheck = File.Exists(SettingsPath);
                if (!FileCheck)
                {
                    CreateJSON();
                    PrintGenericError(LogSystem.MsgType.Warning, "The settings.json file was missing. A template has been created.", SettingsPath);
                }
                else
                {
                    string? JSON = null;
                    using (StreamReader SR = new StreamReader(SettingsPath))
                        JSON = SR.ReadToEnd();

                    if (JSON != null)
                    {
                        Host = JsonConvert.DeserializeObject<OSCServer>(JSON);

                        if (Host == null)
                        {
                            CreateJSON();
                            PrintGenericError(LogSystem.MsgType.Error, "The settings.json file is invalid and has been recreated.", SettingsPath);
                        }
                        else Host.PrepareServer();
                    }
                }

                bool Quit = false;
                while (!Quit)
                {
                    if (Quit)
                        return 0;

                    string[] CArgs;

                    try { CArgs = Console.ReadLine().ToLower().Split(' '); }
                    catch { CArgs = new string[] { "" }; }

                    switch (CArgs[0])
                    {
                        case "qs":
                        case "quitswitch":
                            Quit = true;
                            Console.ForegroundColor = ConsoleColor.Green;
                            MainLog.PrintMessage(LogSystem.MsgType.Information, "Quitting...");
                            break;

                        case "rs":
                        case "reloadswitch":
                            Host.TerminateServer();
                            Console.Clear();
                            goto Reload;

                        case "mm":
                        case "motivateme":
                            string[] Msgs = Properties.Resources.MotivationalMessages.Split('\n');
                            MainLog.PrintMessage(LogSystem.MsgType.Information, Msgs[Rnd.Next(0, Msgs.Length - 1)]);
                            break;

                        case "d":
                        case "dbg":
                        case "debug":
                            MainLog.PrintMessage(LogSystem.MsgType.Information, String.Format("Requested debug info!\n\n{0}\n\n", JsonConvert.SerializeObject(Host, Formatting.Indented)));
                            break;

                        case "cls":
                        case "clear":
                            Console.Clear();
                            break;

                        default:
                            break;
                    }

                    Console.ResetColor();
                };

                return 0;
            }
            catch (SocketException ex)
            {
                MainLog.PrintMessage(LogSystem.MsgType.Error, "Something went wrong! Press a key to restart the program runtime.", ex);
                Console.ReadKey();
                goto Reload;
            }
        }

        static void PrintGenericError(LogSystem.MsgType Color, string Msg, string Param)
        {
            MainLog.PrintMessage(Color, Msg, Param);
            MainLog.PrintMessage(LogSystem.MsgType.Warning, "Please fill it up with the programs you want the server to handle.");
            MainLog.PrintMessage(LogSystem.MsgType.Information, "Type rs/reloadswitch to reload, once the configuration file is ready to be used.");
        }

        static void CreateJSON()
        {
            Host = new OSCServer(9000, 9001, 8000, "192.168.1.128 8001", true, false,
                new OSCProgram[1] {
                    new OSCProgram("OSCClient", true, 10000, 10001, "C:\\OSCClient.exe", "--osc=$OutPort$:127.0.0.1:$InPort$",
                    new OSCAddress[2] {
                        new OSCAddress("/avatar/parameters", new string[2] { "param1", "param2" } ),
                        new OSCAddress("/something/else", new string[2] { "cpu", "ram" } )}) },
                    new OSCAddressHTTP[1] { 
                        new OSCAddressHTTP("http://testwebsite.com/getme", "/avatar/parameters/sendthis",
                            new OSCAddressHTTPItem[2] {
                                new OSCAddressHTTPItem("constant", "string", "thisisconstant", null, null),
                                new OSCAddressHTTPItem("variable", "int", null, 0, 10)
                            })
                    }
                );

            File.WriteAllText(SettingsPath, JsonConvert.SerializeObject(Host, Formatting.Indented));
        }

        static void ProcExit(object sender, EventArgs e)
        {
            MainLog.PrintMessage(LogSystem.MsgType.Warning, "Terminating...");

            if (Host != null)
                Host.TerminateServer();
        }
    }
}