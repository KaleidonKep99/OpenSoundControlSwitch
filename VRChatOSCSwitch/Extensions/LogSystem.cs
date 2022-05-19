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
            string MsgC = "";
            ConsoleColor MsgCC = ConsoleColor.White;

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

            MsgC += string.Format("({0}) {1} >> {2}", (Type == MsgType.Fatal) ? WhoAmI.ToUpper() : WhoAmI, DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss:fff"), Message);

            if (Values.Length > 0)
            {
                MsgC += " (Params: ";

                for (int i = 0; i < Values.Length; i++)
                {
                    switch (Values[i])
                    {
                        case Exception:
                            MsgC += String.Format("{0}, L{1}{2}", ((Exception)Values[i]).Message, new StackTrace((Exception)Values[i], true).GetFrame(0).GetFileLineNumber(), (i == Values.Length - 1) ? null : ", ");
                            break;
                        default:
                            MsgC += String.Format("{0}{1}", Values[i], (i == Values.Length - 1) ? null : ", ");
                            break;
                    }
                }

                MsgC += ")";
            }

            Console.ForegroundColor = ConsoleColor.White;
            MsgC += "\n";

            FinalMsg = new ConsoleMsg(MsgC, MsgCC);
            AC.WriteLog(FinalMsg);

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
                        Console.ForegroundColor = ConsoleColor.White;
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
