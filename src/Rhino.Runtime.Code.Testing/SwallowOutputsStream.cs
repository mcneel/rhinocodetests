using System;
using System.IO;

namespace Rhino.Runtime.Code.Testing
{
    public sealed class SwallowOutputsStream : Stream
    {
        public override bool CanRead { get; } = false;
        public override bool CanSeek { get; } = false;
        public override bool CanWrite { get; } = true;
        public override long Length { get; } = 0;
        public override long Position { get; set; } = 0;

        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotImplementedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();
        public override void SetLength(long value) => throw new NotImplementedException();

        public override void Write(byte[] buffer, int offset, int count) { }
    }
}
