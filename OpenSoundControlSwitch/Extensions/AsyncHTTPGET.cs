using System.Collections.Concurrent;

namespace OpenSoundControlSwitch
{
    public class AsyncHTTPGET
    {
        private BlockingCollection<string> MsgQueue = new BlockingCollection<string>();
        private HttpClient httpClient = new HttpClient();

        public AsyncHTTPGET()
        {
            var thread = new Thread(() => {
                while (true)
                {
                    try
                    {
                        string Item = MsgQueue.Take();

                        if (Item != null)
                        {
                            Task<string> Resp = httpClient.GetStringAsync(Item);
                            Resp.Wait(new TimeSpan(0, 0, 0, 0, 200));
                        }
                    }
                    catch { }
                }
            });

            thread.IsBackground = true;
            thread.Start();
        }

        public void AddToQ(string value)
        {
            MsgQueue.Add(value);
        }
    }
}
