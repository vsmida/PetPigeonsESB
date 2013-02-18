using System;
using System.Collections.Generic;
using System.IO;
using Shared;

namespace PgmTransport
{
    class FrameAccumulator
    {
        private int? _length;
        private readonly List<Frame> _frames = new List<Frame>();
        private int _currentFrameLength;
        private bool _ready = false;
        private static readonly Pool<FrameStream> _streamPool = new Pool<FrameStream>(() => new FrameStream(_streamPool));

        public void SetLength(int length)
        {
            if (_length.HasValue)
            {
                Clear();
            }
            _length = length;
        }

        public bool AddFrame(Frame frame)
        {
            if (!_length.HasValue || _ready)
            {
                Clear();
                return false;
            }

            _frames.Add(frame);
            _currentFrameLength += frame.Count;

            if (_length <= _currentFrameLength)
            {
                _ready = true;
                return true;
            }

            return false;
        }

        private void Clear()
        {
            _frames.Clear();
            _currentFrameLength = 0;
            _ready = false;
        }

        public Stream GetMessage()
        {
            if (!_ready)
                throw new ArgumentException("Message is not ready");
            var stream = _streamPool.GetItem();
            stream.SetFrames(_frames);
            return stream;
        }

    }
}