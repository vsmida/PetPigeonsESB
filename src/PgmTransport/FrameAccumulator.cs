using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Shared;

namespace PgmTransport
{
    public interface IMutableStreamProvider
    {
        MutableMemoryStream GetStream();
    }


    class FrameAccumulator
    {

        private class SimpleStreamProvider : IMutableStreamProvider
        {
            private readonly MutableMemoryStream _stream = new MutableMemoryStream();
            public MutableMemoryStream GetStream()
            {
                return _stream;
            }
        }

        public event Action<Stream> MessageReceived;
        private readonly IMutableStreamProvider _streamProvider;
        private byte[] _spareBuffer;
        private int _spareBufferCount;
        private int _spareLengthBufferCount;
        private readonly byte[] _spareLengthBuffer = new byte[4];
        private int _copiedMessageLength = -1;

        public FrameAccumulator(IMutableStreamProvider streamProvider = null)
        {
            _streamProvider = streamProvider ?? new SimpleStreamProvider();
            _spareBuffer = new byte[1024 * 16];
        }


        public void AddFrame(byte[] buffer, int originalOffset, int originalCount)
        {
            var offset = originalOffset;
            var count = originalCount;

            while (offset < originalCount + originalOffset)
            {

                if (_spareLengthBufferCount != 0) // get length if necesary
                {
                    while (count > 0 && _spareLengthBufferCount != 4)
                    {
                        _spareLengthBuffer[_spareLengthBufferCount] = buffer[offset];
                        count--;
                        offset++;
                        _spareLengthBufferCount++;
                    }

                    if (_spareLengthBufferCount != 4)//could not get full length again
                        return;

                   // _copiedMessageLength = BitConverter.ToInt32(_spareLengthBuffer, 0);
                    _copiedMessageLength = ByteUtils.ReadInt(_spareLengthBuffer, 0);
                    _spareLengthBufferCount = 0;

                    GetFullMessageOrCopyToSpareBuffer(buffer, ref count, _copiedMessageLength, ref offset);
                }
                else if (_copiedMessageLength != -1) //we already have a size
                {
                    var lengthLeftToCopyForMessage = _copiedMessageLength - _spareBufferCount;
                    if (count >= lengthLeftToCopyForMessage)
                    {
                        Array.Copy(buffer, offset, _spareBuffer, _spareBufferCount, lengthLeftToCopyForMessage); //finish copying to spare buffer
                        var stream = _streamProvider.GetStream();
                        stream.SetBuffer(_spareBuffer, 0, _copiedMessageLength);
                        MessageReceived(stream);
                        offset += lengthLeftToCopyForMessage;
                        count -= lengthLeftToCopyForMessage;
                        _copiedMessageLength = -1;
                        _spareBufferCount = 0;
                    }
                    else
                    {
                        Array.Copy(buffer, offset, _spareBuffer, _spareBufferCount, count);
                        _spareBufferCount += count;
                        offset += count;
                        count = 0;
                    }
                }

                else
                {
                    if (count >= 4) //fast path can at least read lentgth
                    {
                       // var messageLength = BitConverter.ToInt32(buffer, offset);
                        var messageLength = ByteUtils.ReadInt(buffer, offset);
                        count -= 4;
                        offset += 4;
                        GetFullMessageOrCopyToSpareBuffer(buffer, ref count, messageLength, ref offset);
                    }

                    else //cant read full length
                    {
                        //copy to buffer
                        Array.Copy(buffer, offset, _spareLengthBuffer, _spareLengthBufferCount, count);
                        _spareLengthBufferCount += count;
                        offset = +count;
                        count = 0;
                    }
                }

            }


        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GetFullMessageOrCopyToSpareBuffer(byte[] buffer, ref int count, int messageLength, ref int offset)
        {
            if (count >= messageLength - _spareBufferCount) //fast path
            {
                var stream = _streamProvider.GetStream();
                stream.SetBuffer(buffer, offset, messageLength);
                MessageReceived(stream);
                _copiedMessageLength = -1;
                offset += messageLength;
                count -= messageLength;

            }
            else //end of fast path
            {
                _copiedMessageLength = messageLength;
                if (messageLength > _spareBuffer.Length)
                    _spareBuffer = new byte[messageLength];

                Array.Copy(buffer, offset, _spareBuffer, _spareBufferCount, count);
                _spareBufferCount += count;
                offset += count;
                count = 0;
            }
        }
    }
}