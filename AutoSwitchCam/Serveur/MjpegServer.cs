using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace AutoSwitchCam
{
    class MjpegServer
    {
        private const int _handlerThread = 2;

        private readonly HttpListener _listener;

        private MemoryStream _currentFrame = new MemoryStream();

        public bool IsStarted { get { return _listener.IsListening; } }

        public MjpegServer(string url)
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add(url);
        }

        public void Start()
        {
            if (_listener.IsListening)
                return;

            _listener.Start();

            for (int i = 0; i < _handlerThread; i++)
            {
                _listener.GetContextAsync().ContinueWith(ProcessRequestHandlerAsync);
            }
        }

        public void Stop()
        {
            if (_listener.IsListening)
                _listener.Stop();
        }

        private async Task ProcessRequestHandlerAsync(Task<HttpListenerContext> result)
        {
            if (!_listener.IsListening)
                return;

            var context = result.Result;

            // Start new listener which replace this
            _listener.GetContextAsync().ContinueWith(ProcessRequestHandlerAsync);

            // Read request
            string request = new StreamReader(context.Request.InputStream).ReadToEnd();

            // Prepare response
            Stream output = context.Response.OutputStream;
            MjpegStreamer mjpegStreamer = new MjpegStreamer(context.Response);

            switch (context.Request.RawUrl)
            {
                case "/stream.mjpeg":
                    mjpegStreamer.WriteMJpegHeader();

                    int lastStreamHash = 0;

                    while (_listener.IsListening)
                    {
                        int streamHash = _currentFrame.GetHashCode();

                        if (streamHash == lastStreamHash)
                        {
                            await Task.Delay(50);
                            continue;
                        }

                        lastStreamHash = streamHash;

                        mjpegStreamer.WriteMJpeg(_currentFrame);
                    }

                    break;
                case "/snap.jpg":
                    mjpegStreamer.WriteJpeg(_currentFrame);

                    break;
                default:
                    mjpegStreamer.WriteErrorHeader();
                    break;
            }
        }

        public void NewFrame(MemoryStream newFrame)
        {
            MemoryStream currentFrame = new MemoryStream();
            newFrame.Position = 0;
            newFrame.CopyTo(currentFrame);

            Interlocked.Exchange(ref this._currentFrame, currentFrame);
        }
    }
}
