using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Pipes;
using System.Text;

namespace OpenSoundControlSwitch
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
        private AsyncConsole AC;

        public LogSystem(string Source)
        {
            WhoAmI = Source;
            AC = new AsyncConsole(WhoAmI);
        }

        public bool PrintMessage(MsgType? Type, string Message, params object[] Values)
        {
            try
            {
                if (Message == null)
                    return false;

                ConsoleMsg FinalMsg;
                ConsoleColor MsgCC;
                string Msg = string.Empty;

                switch (Type)
                {
                    case MsgType.Warning:
                        MsgCC = ConsoleColor.Yellow;
                        break;

                    case MsgType.Error:
                    case MsgType.Fatal:
                        MsgCC = ConsoleColor.Red;
                        break;

                    case MsgType.Information:
                    default:
                        MsgCC = ConsoleColor.White;
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
            }
            catch { }

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

        public AsyncConsole(string Source)
        {

            // NamedPipeServerStream PipeSrv = new NamedPipeServerStream(String.Format("VRCOSCS{0}", Source), PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            // PipeString PipeSt = new PipeString(PipeSrv);
            // new Thread(() => { if (PipeSrv != null) PipeSrv.WaitForConnection(); }).Start();

            var thread = new Thread(() => {
            YesLetsGo:
                try
                {
                    while (true)
                    {
                        ConsoleMsg Item = MsgQueue.Take();

                        if (Item != null)
                        {
                            Console.ForegroundColor = Item.Color;
                            Console.Write(Item.Msg);

                            // if (PipeSrv.IsConnected)
                            //     PipeSt.WriteToPipe(Item.Msg);
                        }
                    }
                }
                catch { goto YesLetsGo; }
            });

            thread.IsBackground = true;
            thread.Start();
        }

        public void WriteLog(ConsoleMsg value)
        {
            MsgQueue.Add(value);
        }
    }

    /*
    public class PipeString
    {
        private Stream IOStream;
        private UnicodeEncoding UE;

        public PipeString(Stream TheStream)
        {
            IOStream = TheStream;
            UE = new UnicodeEncoding();
        }

        public string ReadFromPipe()
        {
            int len;
            len = (IOStream.ReadByte() * 256) + IOStream.ReadByte();

            byte[] InputBuf = new byte[len];
            IOStream.Read(InputBuf, 0, len);

            return UE.GetString(InputBuf);
        }

        public int WriteToPipe(string Output)
        {
            byte[] OutputBuf = UE.GetBytes(Output);
            int len = OutputBuf.Length > UInt16.MaxValue ? UInt16.MaxValue : OutputBuf.Length;

            IOStream.WriteByte((byte)(len / 256));
            IOStream.WriteByte((byte)(len & 255));

            IOStream.Write(OutputBuf, 0, len);
            IOStream.Flush();

            return OutputBuf.Length + 2;
        }
    }
    */
}
