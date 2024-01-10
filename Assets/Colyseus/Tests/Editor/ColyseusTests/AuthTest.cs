
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

		string tokenFromCallback = "OnChange was not called";
		string emailFromCallback = "";
		string nameFromCallback = "";

		client.Auth.OnChange((Colyseus.AuthData<User> authData) =>
		{
			tokenFromCallback = authData.token;
			if (authData.user != null)
			{
				emailFromCallback = authData.user.email;
				nameFromCallback = authData.user.name;
			}
		});

		//
		// Registering for the first time
		//
		Colyseus.IAuthData response = null;
		try
		{
			response = await client.Auth.RegisterWithEmailAndPassword(uniqueEmail, "123456");
		} catch (Colyseus.HttpException e)
		{
			Assert.Fail(e.Message + $"({e.StatusCode})");
		}
		Assert.True(response.Token.Length > 0);
		Assert.AreEqual(response.Token, tokenFromCallback);

		object responseEmail = "";
		object responseName = "";
		response.RawUser.TryGetValue("email", out responseEmail);
		response.RawUser.TryGetValue("name", out responseName);
		Assert.AreEqual(responseEmail, emailFromCallback);
		Assert.AreEqual(responseName, nameFromCallback);

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
		Assert.AreEqual(loginResponse.user.name, uniqueEmail.Split("@")[0]);
	}

}