using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Text;

namespace LumenXEditor.Utilities
{
	public static class Serializer
	{
		public static void ToFile<T>(T intstance, string path)
		{
			try
			{
				using var fs = new FileStream(path, FileMode.Create);
				var serializer = new DataContractSerializer(typeof(T));
				serializer.WriteObject(fs, intstance);
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Failed to serialize object of type {typeof(T).FullName} to file {path}.", ex);
			}
		}

		public static T FromFile<T>(string path)
		{
			try
			{
				using var fs = new FileStream(path, FileMode.Open);
				var serializer = new DataContractSerializer(typeof(T));
				T instance = (T)serializer.ReadObject(fs);
				return instance;
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Failed to deserialize object of type {typeof(T).FullName} from file {path}.", ex);
				return default(T);
			}
		}
	}
}
