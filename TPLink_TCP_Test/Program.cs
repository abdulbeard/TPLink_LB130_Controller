using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TPLink_TCP_Test
{

    public class Data
    {
        [JsonProperty("smartlife.iot.smartbulb.lightingservice")]
        public Random transition_light_state { get; set; }
    }

    public class Random
    {        
        public TransitionState transition_light_state { get; set; }
    }

    public class TransitionState
    {
        public int on_off { get; set; }
        public int brightness { get; set; }
        public int hue { get; set; }
        public int saturation { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var colors = new List<Color> { Color.Red, Color.Green, Color.Yellow, Color.Purple, Color.Magenta, Color.Blue };

            var host = "192.168.1.112";
            var tcpclient = new TcpClient(host, 9999);
            var stream = tcpclient.GetStream();
            sendMessage(stream, host, "{\"smartlife.iot.smartbulb.lightingservice\":{\"transition_light_state\":{\"on_off\":0,\"brightness\":50,\"hue\":50,\"saturation\":50}}}", false);
            //var resultDeserialized = JsonConvert.DeserializeObject<Data>(@"{'smartlife.iot.smartbulb.lightingservice':{'transition_light_state':{'on_off':0,'brightness':50,'hue':50,'saturation':50}}}");
            //sendMessage("192.168.1.7", "{\"system\":{\"set_relay_state\":{\"state\": 1 }}}");
            foreach (var color in colors)
            {
                sendMessage(stream, host, JsonConvert.SerializeObject(new Data()
                {
                    transition_light_state = new Random()
                    {
                        transition_light_state = ColorToHSV(color)
                    }
                }), false);
                Thread.Sleep(3000);
            }
            sendMessage(stream, host, "{\"smartlife.iot.smartbulb.lightingservice\":{\"transition_light_state\":{\"on_off\":0,\"brightness\":50,\"hue\":50,\"saturation\":50}}}");
            tcpclient.Close();
            stream.Dispose();
            tcpclient.Dispose();
        }

        private static void sendMessage(NetworkStream stream, string host, string message, bool closeStream = true)
        {
            var byteMessage = encryptWithHeader(message);
            stream.Write(byteMessage, 0, byteMessage.Length);
            if (closeStream) {
                stream.Close();
            };
        }

        private static byte[] encrypt(string message)
        {
            var key = (byte) 0xAB;
            var bytes = Encoding.ASCII.GetBytes(message);
            for (int i = 0; i < bytes.Length; i++){
                bytes[i] = (byte) (bytes[i] ^ key);
                key = bytes[i];
            }
            return bytes;
        }

        private static byte[] encryptWithHeader(string input)
        {
            var bufMsg = encrypt(input);
            var bufLength = new byte[4];
            bufLength = BitConverter.GetBytes(Convert.ToUInt32(bufMsg.Length));

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bufLength);
            }
            var result = new List<byte>();
            result.AddRange(bufLength);
            result.AddRange(bufMsg);
            return result.ToArray();
        }

        public static TransitionState ColorToHSV(Color color)
        {
            int max = Math.Max(color.R, Math.Max(color.G, color.B));
            int min = Math.Min(color.R, Math.Min(color.G, color.B));

            var hue = color.GetHue();
            var saturation = (max == 0) ? 0 : 1d - (1d * min / max);
            var value = max / 255d;

            return new TransitionState()
            {
                brightness = 50,
                hue = (int)hue,
                on_off = 1,
                saturation = (int)saturation
            };
        }

        public static Color ColorFromHSV(double hue, double saturation, double value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value = value * 255;
            int v = Convert.ToInt32(value);
            int p = Convert.ToInt32(value * (1 - saturation));
            int q = Convert.ToInt32(value * (1 - f * saturation));
            int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

            if (hi == 0)
                return Color.FromArgb(255, v, t, p);
            else if (hi == 1)
                return Color.FromArgb(255, q, v, p);
            else if (hi == 2)
                return Color.FromArgb(255, p, v, t);
            else if (hi == 3)
                return Color.FromArgb(255, p, q, v);
            else if (hi == 4)
                return Color.FromArgb(255, t, p, v);
            else
                return Color.FromArgb(255, v, p, q);
        }
    }
}
