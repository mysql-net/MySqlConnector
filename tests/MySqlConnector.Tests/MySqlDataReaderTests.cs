using System;
using System.Data;
using MySqlConnector;
using Xunit;

namespace MySqlConnector.Tests
{
    public class MySqlDataReaderTests
    {
        [Fact]
        public void GetVectorDataType()
        {
            using var connection = new MySqlConnection("your_connection_string");
            connection.Open();

            using var command = new MySqlCommand("SELECT CAST('[1.0, 2.0, 3.0]' AS VECTOR)", connection);
            using var reader = command.ExecuteReader();

            Assert.True(reader.Read());
            var vector = reader.GetValue(0) as float[];
            Assert.NotNull(vector);
            Assert.Equal(new float[] { 1.0f, 2.0f, 3.0f }, vector);
        }

        [Fact]
        public void GetReadOnlySpanFloat()
        {
            using var connection = new MySqlConnection("your_connection_string");
            connection.Open();

            using var command = new MySqlCommand("SELECT CAST('[1.0, 2.0, 3.0]' AS VECTOR)", connection);
            using var reader = command.ExecuteReader();

            Assert.True(reader.Read());
            var span = reader.GetFieldValue<ReadOnlySpan<float>>(0);
            Assert.Equal(new float[] { 1.0f, 2.0f, 3.0f }, span.ToArray());
        }
    }
}
