using System;
using System.IO;
using System.IO.Pipes;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;


namespace ProjectScanner.Common
{

    public static class PipeUtils
    {

        public static async Task SendDataAsync<T>(PipeStream pipeStream, T data)
        {

            if (!pipeStream.IsConnected)
            {
                throw new InvalidOperationException("Pipe is not connected.");
            }


            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(ms, data);
                byte[] buffer = ms.ToArray();
                byte[] lengthBytes = BitConverter.GetBytes(buffer.Length);
                await pipeStream.WriteAsync(lengthBytes, 0, lengthBytes.Length);


                // Write the actual data.
                await pipeStream.WriteAsync(buffer, 0, buffer.Length);
                await pipeStream.FlushAsync();
            }
        }


        public static async Task<T> ReceiveDataAsync<T>(PipeStream pipeStream)
        {
            if (!pipeStream.IsConnected)
            {
                throw new InvalidOperationException("Pipe is not connected.");
            }


            byte[] lengthBytes = new byte[4]; int bytesRead = await pipeStream.ReadAsync(lengthBytes, 0, lengthBytes.Length);
            if (bytesRead == 0)
            {
                return default(T);
            }
            int dataLength = BitConverter.ToInt32(lengthBytes, 0);


            // Read the actual data based on the received length.
            byte[] buffer = new byte[dataLength];
            int totalBytesRead = 0;
            while (totalBytesRead < dataLength)
            {
                bytesRead = await pipeStream.ReadAsync(buffer, totalBytesRead, dataLength - totalBytesRead);
                if (bytesRead == 0)
                {
                    throw new EndOfStreamException("End of pipe stream reached prematurely.");
                }
                totalBytesRead += bytesRead;
            }


            using (MemoryStream ms = new MemoryStream(buffer))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                return (T)formatter.Deserialize(ms);
            }
        }
    }
}

