using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace bfs.Repository.Util
{
	public static class SystemInfo
	{
		public static bool IsAnyWindows
		{
			get
			{
				return Environment.OSVersion.Platform == PlatformID.Win32Windows
					|| Environment.OSVersion.Platform == PlatformID.Win32S
					|| Environment.OSVersion.Platform == PlatformID.Win32NT;
			}
		}

		public static bool IsUnix
		{
			get { return Environment.OSVersion.Platform == PlatformID.Unix; }
		}

		public static bool IsMacOS
		{
			get { return Environment.OSVersion.Platform == PlatformID.MacOSX; }
		}
	}
}
