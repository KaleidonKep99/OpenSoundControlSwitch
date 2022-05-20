using System.Collections.Concurrent;
using System.Diagnostics;

namespace VRChatOSCSwitch
{
    public class LogSystem
    {
        public enum MsgType
        {
            Information,
            Warning,
            Error,
            Fatal
        }

        private string WhoAmI = "Undefined";
        private AsyncConsole AC = new AsyncConsole();

        public LogSystem(string Source)
        {
            WhoAmI = Source;
        }

        public bool PrintMessage(MsgType? Type, string Message, params object[] Values)
        {
            if (Message == null)
                return false;

            ConsoleMsg FinalMsg;
            ConsoleColor MsgCC;
            string Msg = string.Empty;

            switch (Type)
            {
                case MsgType.Warning:
                    MsgCC= ConsoleColor.Yellow;
                    break;

                case MsgType.Error:
                case MsgType.Fatal:
                    MsgCC = ConsoleColor.Red;
                    break;

                case MsgType.Information:
                default:
                    MsgCC= ConsoleColor.White;
                    break;
            }

            Msg += string.Format("[{0}, {1}]", (Type == MsgType.Fatal) ? WhoAmI.ToUpper() : WhoAmI, DateTime.Now.ToString("yyyy-MM-dd h:mm:sst"));

            Msg += string.Format(" >> {0}", Message);

            if (Values.Length > 0)
            {
                Msg += " (Params: ";

                for (int i = 0; i < Values.Length; i++)
                {
                    switch (Values[i])
                    {
                        case Exception:
                            Msg += String.Format("{0}, L{1}{2}", ((Exception)Values[i]).Message, new StackTrace((Exception)Values[i], true).GetFrame(0).GetFileLineNumber(), (i == Values.Length - 1) ? null : ", ");
                            break;
                        default:
                            Msg += String.Format("{0}{1}", Values[i], (i == Values.Length - 1) ? null : ", ");
                            break;
                    }
                }

                Msg += ")";
            }

            Msg += "\n";

            FinalMsg = new ConsoleMsg(Msg, MsgCC);
            AC.WriteLog(FinalMsg);

            Console.ForegroundColor = ConsoleColor.White;

            return true;
        }
    }

    public class ConsoleMsg
    {
        public string Msg { get; }
        public ConsoleColor Color { get; }

        public ConsoleMsg(string M, ConsoleColor C)
        {
            Msg = M;
            Color = C;
        }
    }

    public class AsyncConsole
    {
        private BlockingCollection<ConsoleMsg> MsgQueue = new BlockingCollection<ConsoleMsg>();

        public AsyncConsole()
        {
            var thread = new Thread(() => { 
                while (true) {
                    ConsoleMsg Item = MsgQueue.Take();
                    
                    if (Item != null)
                    {
                        Console.ForegroundColor = Item.Color;
                        Console.Write(Item.Msg);
                    }
                } });

            thread.IsBackground = true;
            thread.Start();
        }

        public void WriteLog(ConsoleMsg value)
        {
            MsgQueue.Add(value);
        }
    }
}
