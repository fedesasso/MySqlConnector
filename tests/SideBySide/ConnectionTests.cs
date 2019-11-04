using System;
using System.Data;
using Dapper;
using MySql.Data.MySqlClient;
using Xunit;

namespace SideBySide
{
	public class ConnectionTests : IClassFixture<DatabaseFixture>
	{
		public ConnectionTests(DatabaseFixture database)
		{
		}

		[Fact]
		public void GotInfoMessageForNonExistentTable()
		{
			using var connection = new MySqlConnection(AppConfig.ConnectionString);
			connection.Open();

			var gotEvent = false;
			connection.InfoMessage += (s, a) =>
			{
				gotEvent = true;
				Assert.Single(a.errors);
				Assert.Equal((int) MySqlErrorCode.BadTable, a.errors[0].Code);
			};

			connection.Execute(@"drop table if exists table_does_not_exist;");
			Assert.True(gotEvent);
		}

		[Fact]
		public void NoInfoMessageWhenNotLastStatementInBatch()
		{
			using var connection = new MySqlConnection(AppConfig.ConnectionString);
			connection.Open();

			var gotEvent = false;
			connection.InfoMessage += (s, a) =>
			{
				gotEvent = true;

				// seeming bug in Connector/NET raises an event with no errors
				Assert.Empty(a.errors);
			};

			connection.Execute(@"drop table if exists table_does_not_exist; select 1;");
#if BASELINE
			Assert.True(gotEvent);
#else
			Assert.False(gotEvent);
#endif
		}

		[Fact]
		public void DefaultConnectionStringIsEmpty()
		{
			using var connection = new MySqlConnection();
			Assert.Equal("", connection.ConnectionString);
		}

		[Fact]
		public void InitializeWithNullConnectionString()
		{
			using var connection = new MySqlConnection(default(string));
			Assert.Equal("", connection.ConnectionString);
		}

		[Fact]
		public void SetConnectionStringToNull()
		{
			using var connection = new MySqlConnection();
			connection.ConnectionString = null;
			Assert.Equal("", connection.ConnectionString);
		}

		[Fact]
		public void SetConnectionStringToEmptyString()
		{
			using var connection = new MySqlConnection();
			connection.ConnectionString = "";
			Assert.Equal("", connection.ConnectionString);
		}

		[SkippableFact(Baseline = "Throws NullReferenceException")]
		public void ServerVersionThrows()
		{
			using var connection = new MySqlConnection();
			Assert.Throws<InvalidOperationException>(() => connection.ServerVersion);
		}

		[SkippableFact(Baseline = "Throws NullReferenceException")]
		public void ServerThreadThrows()
		{
			using var connection = new MySqlConnection();
			Assert.Throws<InvalidOperationException>(() => connection.ServerThread);
		}

		[Fact]
		public void DatabaseIsEmptyString()
		{
			using var connection = new MySqlConnection();
			Assert.Equal("", connection.Database);
		}

		[Fact]
		public void DataSourceIsEmptyString()
		{
			using var connection = new MySqlConnection();
			Assert.Equal("", connection.DataSource);
		}

		[Fact]
		public void ConnectionTimeoutDefaultValue()
		{
			using var connection = new MySqlConnection();
			Assert.Equal(15, connection.ConnectionTimeout);
		}

		[Fact]
		public void ConnectionTimeoutDefaultValueAfterOpen()
		{
			using var connection = new MySqlConnection(AppConfig.ConnectionString);
			connection.Open();
			Assert.Equal(15, connection.ConnectionTimeout);
		}

		[Fact]
		public void ConnectionTimeoutExplicitValue()
		{
			using var connection = new MySqlConnection("Connection Timeout=30");
			Assert.Equal(30, connection.ConnectionTimeout);
		}

		[Fact]
		public void ConnectionTimeoutExplicitValueAfterOpen()
		{
			var csb = AppConfig.CreateConnectionStringBuilder();
			csb.ConnectionTimeout = 30;
			using var connection = new MySqlConnection(csb.ConnectionString);
			connection.Open();
			Assert.Equal(30, connection.ConnectionTimeout);
		}

		[Fact]
		public void CloneClonesConnectionString()
		{
			using var connection = new MySqlConnection(AppConfig.ConnectionString);
			using var connection2 = (MySqlConnection) connection.Clone();
			Assert.Equal(connection.ConnectionString, connection2.ConnectionString);
#if !BASELINE
			Assert.Equal(AppConfig.ConnectionString, connection2.ConnectionString);
#endif
		}

		[Fact]
		public void CloneIsClosed()
		{
			using var connection = new MySqlConnection(AppConfig.ConnectionString);
			connection.Open();
			using var connection2 = (MySqlConnection) connection.Clone();
			Assert.Equal(ConnectionState.Closed, connection2.State);
		}

		[SkippableFact(Baseline = "https://bugs.mysql.com/bug.php?id=97473")]
		public void CloneDoesNotDisclosePassword()
		{
			using var connection = new MySqlConnection(AppConfig.ConnectionString);
			connection.Open();
			using var connection2 = (MySqlConnection) connection.Clone();
			Assert.Equal(connection.ConnectionString, connection2.ConnectionString);
			Assert.DoesNotContain("password", connection2.ConnectionString, StringComparison.OrdinalIgnoreCase);
		}
	}
}
