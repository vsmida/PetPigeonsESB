using System;
using System.Collections.Generic;
using System.IO;
using Shared;
using log4net;

namespace PgmTransport
{
    class PartialMessage
    {
        private int? _messageLength;
        private static readonly Pool<FrameStream> _streamPool = new Pool<FrameStream>(() => new FrameStream(_streamPool), 10000);
        private int _readMesssageLength;
        private readonly List<Frame> _frames = new List<Frame>(10);
        private Frame _lengthPrefix = new Frame(new byte[4], 0, 4);
        private int _lengthPrefixReadCount = 0;

        public bool Ready { get; private set; }


        public int AddFrame(Frame frame)
        {

            if (Ready)
                throw new ArgumentException("cannot add frame to this message, already ready");

            int readFrameBytesForLength = 0;
            if (!_messageLength.HasValue)
            {
                var lengthPrefixSizeToRead = Math.Min(4 - _lengthPrefixReadCount, frame.Count);
                Array.Copy(frame.Buffer, frame.Offset, _lengthPrefix.Buffer, _lengthPrefix.Offset + _lengthPrefixReadCount, lengthPrefixSizeToRead);
                _lengthPrefixReadCount += lengthPrefixSizeToRead;
                //for (int i = 0; i < lengthPrefixSizeToRead; i++)
                //{
                //    _lengthPrefix.Buffer[_lengthPrefix.Offset+_lengthPrefix.Count] = frame.Buffer[frame.Offset + i];
                //    _lengthPrefix.Count++;
                //}
                if (_lengthPrefixReadCount == 4)
                {
                    _messageLength = BitConverter.ToInt32(_lengthPrefix.Buffer, _lengthPrefix.Offset);
                    if (_messageLength == 0) //null delimiter?
                        Ready = true;
                }
                else
                    return lengthPrefixSizeToRead;

                readFrameBytesForLength += lengthPrefixSizeToRead;
            }

            //size has value
            var lengthToRead = Math.Min(frame.Count - readFrameBytesForLength, _messageLength.Value - _readMesssageLength);
            if (lengthToRead > 0)
            {
                _frames.Add(new Frame(frame.Buffer, frame.Offset + readFrameBytesForLength, lengthToRead, frame.BufferPool));
                _readMesssageLength += lengthToRead;
                if (_readMesssageLength == _messageLength)
                    Ready = true;
                return lengthToRead + readFrameBytesForLength;
            }

            return readFrameBytesForLength;
        }

        public Stream GetMessage()
        {
            if (!Ready)
                throw new ArgumentException("Cannot get message");

            var stream = _streamPool.GetItem();
            stream.SetFrames(_frames ?? new List<Frame>());
         //   stream.SetFrames(_frames == null? new List<Frame>():new List<Frame>(_frames));
            return stream;
        }


        public void Clear()
        {
            _lengthPrefixReadCount = 0;
            _readMesssageLength = 0;
            _messageLength = null;
            _frames.Clear();
            Ready = false;
        }
    }


    class FrameAccumulator
    {
        private readonly ILog _logger = LogManager.GetLogger(typeof(FrameAccumulator));
        private PartialMessage _currentPartialMessage = new PartialMessage();
        public event Action<Stream> MessageReceived = delegate{};

        public void AddFrame(Frame frame)
        {
            var count = frame.Count;
            var offset = frame.Offset;
            while (offset < frame.Count +frame.Offset)
            {
                var readFromFrame = _currentPartialMessage.AddFrame(new Frame(frame.Buffer, offset, count));

                if (_currentPartialMessage.Ready)
                {
                    MessageReceived(_currentPartialMessage.GetMessage()); 
                    _currentPartialMessage.Clear();
                }
                offset += readFromFrame;
                count -= readFromFrame;

            }
        }

    }
}