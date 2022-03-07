using NUnit.Framework;
using UnityEngine;

public class ColyseusRequestTest
{

	[SetUp]
	public void Init()
	{

	}

	[TearDown]
	public void Dispose()
	{

	}

	[Test]
	public void UnsecureRootPathWithPortTest()
	{
		var settings = new Colyseus.ColyseusSettings();
		settings.colyseusServerAddress = "localhost";
		settings.colyseusServerPort = "2567";
		settings.useSecureProtocol = false;

		var request = new Colyseus.ColyseusRequest(settings);
		Assert.AreEqual("http://localhost:2567/", request.GetUriBuilder("").ToString());
	}

	[Test]
	public void UnsecureChildPathWithPortTest()
	{
		var settings = new Colyseus.ColyseusSettings();
		settings.colyseusServerAddress = "localhost/path";
		settings.colyseusServerPort = "2567";
		settings.useSecureProtocol = false;

		var request = new Colyseus.ColyseusRequest(settings);
		Assert.AreEqual("http://localhost:2567/path/", request.GetUriBuilder("").ToString());
	}

	[Test]
	public void UnsecureChildPathNoPortTest()
	{
		var settings = new Colyseus.ColyseusSettings();
		settings.colyseusServerAddress = "localhost/path";
		settings.colyseusServerPort = "80";
		settings.useSecureProtocol = false;

		var request = new Colyseus.ColyseusRequest(settings);
		Assert.AreEqual("http://localhost/path/", request.GetUriBuilder("").ToString());
	}


	[Test]
	public void SecureChildPathNoPortTest()
	{
		var settings = new Colyseus.ColyseusSettings();
		settings.colyseusServerAddress = "localhost/path";
		settings.colyseusServerPort = "443";
		settings.useSecureProtocol = true;

		var request = new Colyseus.ColyseusRequest(settings);
		Assert.AreEqual("https://localhost/path/", request.GetUriBuilder("").ToString());
	}

	[Test]
	public void SecureChildPathWithPortTest()
	{
		var settings = new Colyseus.ColyseusSettings();
		settings.colyseusServerAddress = "localhost";
		settings.colyseusServerPort = "8080";
		settings.useSecureProtocol = true;

		var request = new Colyseus.ColyseusRequest(settings);
		Assert.AreEqual("https://localhost:8080/", request.GetUriBuilder("").ToString());
	}


}