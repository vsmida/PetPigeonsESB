using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Shared;

namespace PgmTransport
{
    class FrameStream : Stream
    {
        private List<Frame> _frames;
        private int _currentFrameIndex = 0;
        private int _currentPositionFromFrameStart = 0;
        private int _length;
        private readonly Pool<FrameStream> _frameStreamPool;

        public FrameStream(List<Frame> frames, Pool<FrameStream> pool = null) : this(pool)
        {
            SetFrames(frames);
        }

        public FrameStream(Pool<FrameStream> pool = null)
        {
            _frameStreamPool = pool;
        }

        public void SetFrames(List<Frame> frames)
        {
            _frames = frames;
            for (int i = 0; i < _frames.Count(); i++)
            {
                _length += _frames[i].Count;
            }
        }

        protected override void Dispose(bool disposing)
        {
            for (int i = 0; i < _frames.Count(); i++)
            {
                _frames[i].Dispose();
            }
            _frames = null;
            _currentFrameIndex = 0;
            _currentPositionFromFrameStart = 0;
            _length = 0;
            if(_frameStreamPool != null)
                _frameStreamPool.PutBackItem(this);

        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var currentFrame = _frames[_currentFrameIndex];
            var leftBytesFromFrame = currentFrame.Count - _currentPositionFromFrameStart;
            var leftBytesToCopyToBuffer = count;
            var currentoffset = offset;
            var copiedBytes = 0;

            while (leftBytesFromFrame - leftBytesToCopyToBuffer < 0) // should copy all
            {
                Array.Copy(currentFrame.Buffer, _currentPositionFromFrameStart, buffer, currentoffset, leftBytesFromFrame);
                copiedBytes += leftBytesFromFrame;
                _currentFrameIndex++;
                currentFrame = _frames[_currentFrameIndex];
                leftBytesToCopyToBuffer -= leftBytesFromFrame;
                currentoffset += leftBytesFromFrame;
                _currentPositionFromFrameStart = 0;
                leftBytesFromFrame = currentFrame.Count - _currentPositionFromFrameStart;
                
            }

            //last
            if(leftBytesToCopyToBuffer > 0)
            {
                Array.Copy(currentFrame.Buffer, _currentPositionFromFrameStart, buffer, currentoffset, leftBytesToCopyToBuffer);
                copiedBytes += leftBytesToCopyToBuffer;
                leftBytesToCopyToBuffer -= leftBytesToCopyToBuffer;
                currentoffset += leftBytesToCopyToBuffer;
                _currentPositionFromFrameStart += leftBytesToCopyToBuffer;
                if (_currentPositionFromFrameStart - offset + 1 == currentFrame.Count) //  if frame exhausted, point to the next;
                    _currentFrameIndex++;
            }

            return copiedBytes;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override long Length
        {
            get { return _length; }
        }

        public override long Position { get; set; }


    }
}