using System;
using System.Collections.Generic;

namespace PgmTransport
{
    class FrameAccumulator
    {
        private int? _length;
        private List<Frame> _frames = new List<Frame>();
        private int _currentFrameLength;
        private bool _ready = false;

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

        public byte[] GetMessage()
        {
            if (!_ready)
                throw new ArgumentException("Message is not ready");
            var buffer = new byte[_length.Value];
            int offset = 0;
            foreach (var frame in _frames)
            {
                var lengthToCopy = Math.Min(_length.Value - offset, frame.Count);
                Array.Copy(frame.Buffer, frame.Offset, buffer, offset, lengthToCopy);
                offset += lengthToCopy;
            }
            return buffer;
        }

    }
}