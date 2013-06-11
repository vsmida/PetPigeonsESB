using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Shared;

namespace PgmTransport
{
    public class FrameStream : Stream
    {
        private List<Frame> _frames;
        private int _currentFrameIndex = 0;
        private int _currentPositionFromFrameStart = 0;
        private int _length;
        private readonly Pool<FrameStream> _frameStreamPool;
        private Frame _currentFrame;

        public FrameStream(List<Frame> frames, Pool<FrameStream> pool = null)
            : this(pool)
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
            for (int i = 0; i < _frames.Count; i++)
            {
                _length += _frames[i].Count;
            }
            if (_frames.Count > 0)
                _currentFrame = _frames[0];
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
            if (_frameStreamPool != null)
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

        //public override int ReadByte()
        //{
        //    var leftBytesFromFrame = _currentFrame.Count - _currentPositionFromFrameStart;
        //    byte res;
        //    if(leftBytesFromFrame > 0)
        //    {
        //        res = _currentFrame.Buffer[_currentPositionFromFrameStart + _currentFrame.Offset];
        //        _currentPositionFromFrameStart++;
        //        if(leftBytesFromFrame -1 == 0 && _currentFrameIndex < _frames.Count - 1) //advance if necessary
        //        {
        //              _currentFrameIndex++;
        //                _currentFrame = _frames[_currentFrameIndex];
        //        }
             
            
        //    }
        //    else
        //    {
        //        if (_currentFrameIndex == _frames.Count - 1)
        //        {
        //            throw new ArgumentException("no longer any byte to read");
        //        }
        //        _currentFrameIndex++;
        //        _currentFrame = _frames[_currentFrameIndex];
        //        return ReadByte();

        //    }
        //    Position++;
        //    return res;
        //}

        public override int Read(byte[] buffer, int offset, int count)
        {
            _currentFrame = _frames[_currentFrameIndex];
            var leftBytesFromFrame = _currentFrame.Count - _currentPositionFromFrameStart;
            var leftBytesToCopyToBuffer = count;
            var currentoffset = offset;
            var copiedBytes = 0;

            while (leftBytesFromFrame - leftBytesToCopyToBuffer <= 0) // should copy all
            {
                Array.Copy(_currentFrame.Buffer, _currentPositionFromFrameStart + _currentFrame.Offset, buffer, currentoffset, leftBytesFromFrame);
                copiedBytes += leftBytesFromFrame;
                if (_currentFrameIndex == _frames.Count - 1)
                    break;
                _currentFrameIndex++;
                _currentFrame = _frames[_currentFrameIndex];
                leftBytesToCopyToBuffer -= leftBytesFromFrame;
                currentoffset += copiedBytes;
                _currentPositionFromFrameStart = 0;
                leftBytesFromFrame = _currentFrame.Count - _currentPositionFromFrameStart;

            }

            //last
            if (leftBytesToCopyToBuffer > 0)
            {
                Array.Copy(_currentFrame.Buffer, _currentPositionFromFrameStart + _currentFrame.Offset, buffer, currentoffset, leftBytesToCopyToBuffer);
                copiedBytes += leftBytesToCopyToBuffer;
                currentoffset += leftBytesToCopyToBuffer;
                //    leftBytesToCopyToBuffer -= leftBytesToCopyToBuffer;
                _currentPositionFromFrameStart += leftBytesToCopyToBuffer;
                if (_currentPositionFromFrameStart - offset == _currentFrame.Count) //  if frame exhausted, point to the next;
                {
                    if (_currentFrameIndex < _frames.Count - 1)
                    {
                        _currentFrameIndex++;
                        _currentFrame = _frames[_currentFrameIndex];
                    }
                }
            }

            Position += copiedBytes;
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