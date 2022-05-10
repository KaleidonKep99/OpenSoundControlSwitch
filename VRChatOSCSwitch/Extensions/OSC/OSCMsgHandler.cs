using Bespoke.Osc;
using System.Net;

namespace VRChatOSCSwitch
{
    public partial class OSCMsgHandler
    {
        // Log system used to log errors
        private LogSystem OSCMsgHandlerL = new LogSystem("OSCMsgHandler");

        public OscMessage? BuildMsg(string Target, IPEndPoint NetTarget, params object[] Parameters)
        {
            // Prepare the construct for the message
            OscMessage Message = new OscMessage(NetTarget, Target);

            try
            {
                // Since you can pass multiple parameters at once with one message,
                // scan and append each one to the message
                foreach (object Param in Parameters)
                {
                    switch (Param)
                    {
                        // If it's an array of any type, append each value to the message
                        case int[] iArray:
                            foreach (int i in iArray)
                                Message.Append(i);
                            break;

                        case bool[] boArray:
                            foreach (bool bo in boArray)
                                Message.Append(bo);
                            break;

                        case float[] fArray:
                            foreach (float f in fArray)
                                Message.Append(f);
                            break;

                        case byte[] byArray:
                            foreach (byte by in byArray)
                                Message.Append(by);
                            break;

                        case string[] sArray:
                            foreach (string s in sArray)
                                Message.Append(s);
                            break;

                        // If it's a recognized value, just append it to the message
                        case int i:
                        case bool bo:
                        case float f:
                        case byte by:
                        case string s:
                            Message.Append(Param);
                            break;

                        // If it's a message or bundle already, just send them to the target
                        case OscMessage:
                            OscMessage TempM = (OscMessage)Param;
                            TempM.Send(NetTarget);
                            return null;

                        case OscBundle:
                            OscBundle TempB = (OscBundle)Param;
                            TempB.Send(NetTarget);
                            return null;

                        // Not recognized?
                        default:
                            OSCMsgHandlerL.PrintMessage(LogSystem.MsgType.Error, "Unsupported parameter passed to BuildMsg.", Param.GetType());
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                OSCMsgHandlerL.PrintMessage(LogSystem.MsgType.Error, "An error has occured.", ex.ToString());
            }

            return Message;
        }

        public void SendMsg(string Target, IPEndPoint NetTarget, object? Value = null, bool? Silent = false)
        {
            OscMessage Message = BuildMsg(Target, NetTarget, Value);
            Message.Send(NetTarget);

            if (Silent == false)
                OSCMsgHandlerL.PrintMessage(LogSystem.MsgType.Information, "OSC message sent.", Message.Address, NetTarget.Address.ToString(), NetTarget.Port.ToString());
        }

        public void SendBndl(IPEndPoint NetTarget, List<OscMessage>? MsgVector = null, bool? Silent = false)
        {
            OscBundle Bundle = new OscBundle(NetTarget);

            foreach (OscMessage Msg in MsgVector)
                Bundle.Append(Msg);

            Bundle.Send(NetTarget);
            if (Silent == false)
                OSCMsgHandlerL.PrintMessage(LogSystem.MsgType.Information, "OSC bundle sent.", NetTarget.Address.ToString(), NetTarget.Port.ToString());
        }
    }
}
