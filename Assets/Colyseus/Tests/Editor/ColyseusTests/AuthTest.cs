
using System.Threading.Tasks;
using System.Collections;
using NUnit.Framework;
using UnityEngine;

public class AuthTest
{
	class User
	{
		public string email;
		public string name;
		public int errorServerIsStringButClientIsInt;
		public int anonymousId;
		public bool anonymous;
	}

	private Colyseus.ColyseusClient client = new Colyseus.ColyseusClient("http://localhost:2567");

	[SetUp]
	public void Init()
	{
		// Make sure auth token is not cached
		PlayerPrefs.DeleteAll();
	}

	[TearDown]
	public void Dispose()
	{
	}

	[Test]
	public async Task RegisterWithEmailAndPassword()
	{
		var uniqueEmail = $"endel{Time.time.ToString().Replace(".", "")}@colyseus.io";
		string token = "OnChange was not called";

		client.Auth.OnChange((Colyseus.AuthData<User> authData) =>
		{
			token = authData.token;
			if (authData.user != null)
			{
				Debug.Log("email => " + authData.user.email);
				Debug.Log("name => " + authData.user.name);
			}
		});

		//
		// Registering for the first time
		//
		var responseToken = "";
		try
		{
			var registerResponse = await client.Auth.RegisterWithEmailAndPassword(uniqueEmail, "123456");
			responseToken = registerResponse.GetToken();
		} catch (Colyseus.HttpException e)
		{
			Assert.Fail(e.Message + $"({e.StatusCode})");
		}
		Assert.True(responseToken.Length > 0);
		Assert.AreEqual(responseToken, token);

		//
		// Trying to register a second time
		//
		try
		{
			await client.Auth.RegisterWithEmailAndPassword(uniqueEmail, "123456");
			Assert.Fail("Should have failed.");
		}
		catch (Colyseus.HttpException e)
		{
			Assert.AreEqual(e.Message, "email_already_in_use");
		}

		try
		{
			await client.Auth.SignInWithEmailAndPassword(uniqueEmail, "wrong");
			Assert.Fail("Signing in with wrong password should've failed!");
		}
		catch (Colyseus.HttpException e)
		{
			Assert.AreEqual(e.Message, "invalid_credentials");
		}

		var loginResponse = await client.Auth.SignInWithEmailAndPassword<User>(uniqueEmail, "123456");
		Assert.AreEqual(loginResponse.user.email, uniqueEmail);
		Debug.Log(loginResponse.user.name);
		Debug.Log(uniqueEmail.Split("@")[0]);
		Assert.AreEqual(loginResponse.user.name, uniqueEmail.Split("@")[0]);
	}

}