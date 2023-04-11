using Godot;
using System;
using System.Collections.Generic;

namespace HuntersHaul.Scripts;
public partial class CustomTools : Node
{
	public static List<string> DirContent(string path)
	{
		using var dir = DirAccess.Open(path);
		var result = new List<string>();
		if (dir != null)
		{
			dir.ListDirBegin();
			string fileName = dir.GetNext();
			while (fileName != "")
			{
				if (!dir.CurrentIsDir())
				{
					if (fileName.EndsWith(".tscn") || fileName.EndsWith(".tscn.remap"))
					{
						result.Add(fileName.Remove(fileName.IndexOf(".tscn", StringComparison.Ordinal)));
						
					}
				}
				fileName = dir.GetNext();
			}
		}
		else
		{
			GD.Print("An error occurred when trying to access the path.");
		}

		return result;
	}
}
