using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace StorytellYourself.Protocol;

public static class ProtocolJson
{
	public static byte[] Serialize<T>(T value)
	{
		using MemoryStream memoryStream = new MemoryStream();
		new DataContractJsonSerializer(typeof(T)).WriteObject((Stream)memoryStream, (object)value);
		return memoryStream.ToArray();
	}

	public static string SerializeToString<T>(T value)
	{
		return Encoding.UTF8.GetString(Serialize(value));
	}

	public static T? Deserialize<T>(string json) where T : class
	{
		return Deserialize<T>(Encoding.UTF8.GetBytes(json));
	}

	public static T? Deserialize<T>(byte[] payload) where T : class
	{
		using MemoryStream stream = new MemoryStream(payload);
		return (T)new DataContractJsonSerializer(typeof(T)).ReadObject((Stream)stream);
	}
}
