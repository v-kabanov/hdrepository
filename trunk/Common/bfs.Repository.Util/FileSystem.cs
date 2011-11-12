using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace bfs.Repository.Util
{
	public static class FileSystem
	{
		/// <summary>
		///		Given a path to a directory or file return its last component
		///		which would be directory or file name.
		/// </summary>
		/// <param name="directoryPath">
		///		Any valid path such as "c:\foo\poo\".
		/// </param>
		/// <returns>
		///		Last path component, such as "poo" (for "c:\foo\poo\")
		/// </returns>
		public static string GetLastPathComponent(string path)
		{
			Util.Check.Require(!string.IsNullOrEmpty(path));

			char[] separators = new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
			path = path.TrimEnd(separators);
			int lastIndex = path.LastIndexOfAny(separators);
			if (lastIndex >= 0)
			{
				path = path.Substring(lastIndex + 1);
			}
			return path;
		}
	}
}
