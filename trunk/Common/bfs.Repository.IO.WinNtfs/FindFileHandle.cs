using System;
using Microsoft.Win32.SafeHandles;

namespace bfs.Repository.IO.WinNtfs
{
    public class FindFileHandle : SafeHandleZeroOrMinusOneIsInvalid
	{
		public FindFileHandle()
            : base(true)
		{
        }

        protected override bool ReleaseHandle()
		{
            return WindowsNative.FindClose(base.handle);
        }
    }
}
