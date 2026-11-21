using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DiscordPlugin.Discord
{
    /// <summary>
    /// Minimal client for the local Discord RPC named pipe (\\.\pipe\discord-ipc-N).
    /// Only implements what Rich Presence needs: handshake, SET_ACTIVITY and ping/pong.
    /// </summary>
    internal sealed class DiscordIpcClient : IDisposable
    {
        private const int OpHandshake = 0;
        private const int OpFrame = 1;
        private const int OpClose = 2;
        private const int OpPing = 3;
        private const int OpPong = 4;

        private const int MaxFrameBytes = 64 * 1024;

        private readonly string _clientId;
        private readonly object _writeLock = new object();

        private NamedPipeClientStream _pipe;
        private volatile bool _ready;

        public DiscordIpcClient(string clientId)
        {
            _clientId = clientId;
        }

        /// <summary>True once Discord has accepted the handshake and the pipe is still open.</summary>
        public bool IsReady => _ready && _pipe != null && _pipe.IsConnected;

        public bool IsConnected => _pipe != null && _pipe.IsConnected;

        /// <summary>Tries each Discord pipe in turn and sends the handshake. Readiness arrives asynchronously.</summary>
        public bool TryConnect()
        {
            Disconnect();

            for (var i = 0; i < 10; i++)
            {
                // Asynchronous (overlapped) so the reader thread's blocking Read doesn't serialise our Writes.
                var pipe = new NamedPipeClientStream(".", $"discord-ipc-{i}", PipeDirection.InOut, PipeOptions.Asynchronous);
                try
                {
                    pipe.Connect(200);
                }
                catch (Exception)
                {
                    pipe.Dispose();
                    continue;
                }

                _pipe = pipe;
                try
                {
                    Write(OpHandshake, new JObject { ["v"] = 1, ["client_id"] = _clientId });
                }
                catch (Exception ex)
                {
                    Log($"Handshake failed on discord-ipc-{i}: {ex.Message}");
                    Disconnect();
                    continue;
                }

                var reader = new Thread(() => ReadLoop(pipe)) { IsBackground = true, Name = "Discord IPC reader" };
                reader.Start();
                return true;
            }

            return false;
        }

        /// <summary>Sets the activity, or clears it when <paramref name="activity"/> is null.</summary>
        public void SetActivity(JObject activity)
        {
            var payload = new JObject
            {
                ["cmd"] = "SET_ACTIVITY",
                ["args"] = new JObject
                {
                    ["pid"] = Process.GetCurrentProcess().Id,
                    ["activity"] = activity ?? (JToken)JValue.CreateNull(),
                },
                ["nonce"] = Guid.NewGuid().ToString(),
            };

            Write(OpFrame, payload);
        }

        public void Disconnect()
        {
            _ready = false;
            var pipe = _pipe;
            _pipe = null;
            if (pipe == null) return;

            try
            {
                if (pipe.IsConnected) WriteFrame(pipe, OpClose, "{}");
            }
            catch (Exception) { }

            pipe.Dispose();
        }

        public void Dispose() => Disconnect();

        private void ReadLoop(NamedPipeClientStream pipe)
        {
            try
            {
                while (pipe.IsConnected)
                {
                    var header = ReadExactly(pipe, 8);
                    if (header == null) break;

                    var opcode = BitConverter.ToInt32(header, 0);
                    var length = BitConverter.ToInt32(header, 4);
                    if (length < 0 || length > MaxFrameBytes) break;

                    var body = length == 0 ? new byte[0] : ReadExactly(pipe, length);
                    if (body == null) break;

                    var json = Encoding.UTF8.GetString(body);

                    switch (opcode)
                    {
                        case OpFrame:
                            HandleFrame(json);
                            break;
                        case OpPing:
                            lock (_writeLock) WriteFrame(pipe, OpPong, json);
                            break;
                        case OpClose:
                            Log($"Discord closed the connection: {json}");
                            return;
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Reader stopped: {ex.Message}");
            }
            finally
            {
                if (ReferenceEquals(pipe, _pipe)) _ready = false;
                pipe.Dispose();
            }
        }

        private void HandleFrame(string json)
        {
            JObject frame;
            try { frame = JObject.Parse(json); }
            catch (JsonException) { return; }

            var evt = (string)frame["evt"];
            if (evt == "READY")
            {
                _ready = true;
                Log("Connected to Discord.");
            }
            else if (evt == "ERROR")
            {
                Log($"Discord error: {frame["data"]}");
            }
        }

        private void Write(int opcode, JObject payload)
        {
            var pipe = _pipe ?? throw new InvalidOperationException("Not connected to Discord.");
            lock (_writeLock) WriteFrame(pipe, opcode, payload.ToString(Formatting.None));
        }

        private static void WriteFrame(Stream stream, int opcode, string json)
        {
            var body = Encoding.UTF8.GetBytes(json);
            var frame = new byte[8 + body.Length];
            BitConverter.GetBytes(opcode).CopyTo(frame, 0);
            BitConverter.GetBytes(body.Length).CopyTo(frame, 4);
            body.CopyTo(frame, 8);
            stream.Write(frame, 0, frame.Length);
            stream.Flush();
        }

        private static byte[] ReadExactly(Stream stream, int count)
        {
            var buffer = new byte[count];
            var offset = 0;
            while (offset < count)
            {
                var read = stream.Read(buffer, offset, count - offset);
                if (read <= 0) return null;
                offset += read;
            }
            return buffer;
        }

        private static void Log(string message) => Debug.WriteLine($"[DiscordPlugin] {message}");
    }
}
