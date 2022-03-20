using System;
using System.IO;
using System.Net;
using System.Text;

namespace AutoSwitchCam
{
    public class MjpegStreamer
    {
        public HttpListenerResponse _response = null;
        public const string _boundary = "AutoSwitchCam_boundary";

        public MjpegStreamer(HttpListenerResponse response)
        {
            _response = response;
        }

        public void WriteMJpegHeader()
        {
            _response.ContentType = "multipart/x-mixed-replace; boundary=\"" + _boundary + "\"\r\n";
            _response.OutputStream.Flush();
        }

        public void WriteErrorHeader()
        {
            Stream output = this._response.OutputStream;

            WriteLine("HTTP/1.0 404 NotFound");

            output.Flush();
            output.Close();
        }

        public void WriteMJpeg(MemoryStream imageStream)
        {
            imageStream.Position = 0;
            byte[] buffer = imageStream.ToArray();

            WriteLine("--" + _boundary);
            WriteLine("Content-Type:image/jpeg");
            WriteLine("Content-Length:" + buffer.Length);
            WriteLine(string.Empty);

            Stream output = this._response.OutputStream;
            output.Write(buffer, 0, buffer.Length);

            WriteLine(string.Empty);

            output.Flush();
        }

        public void WriteJpeg(MemoryStream imageStream)
        {
            _response.ContentType = "image/jpeg";
            _response.ContentLength64 = imageStream.Length;

            imageStream.Position = 0;
            byte[] buffer = imageStream.ToArray();

            Stream output = this._response.OutputStream;
            output.Write(buffer, 0, buffer.Length);

            output.Flush();
            output.Close();
        }

        private void WriteLine(string text)
        {
            byte[] data = BytesOf(text + "\r\n");
            _response.OutputStream.Write(data, 0, data.Length);
        }

        private static byte[] BytesOf(string text)
        {
            return Encoding.ASCII.GetBytes(text);
        }
    }
}
