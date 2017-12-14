using System;
using System.Data;
using AdoNet.Specification.Tests;

namespace Conformance.Tests
{
	public class GetValueConversionTests : GetValueConversionTestBase<SelectValueFixture>
	{
		public GetValueConversionTests(SelectValueFixture fixture)
			: base(fixture)
		{
		}

		// GetBoolean allows conversions from any integral type for backwards compatibility
		public override void GetBoolean_throws_for_maximum_Byte() => TestGetValue(DbType.Byte, ValueKind.Maximum, x => x.GetBoolean(0), true);
		public override void GetBoolean_throws_for_maximum_Int16() => TestGetValue(DbType.Int16, ValueKind.Maximum, x => x.GetBoolean(0), true);
		public override void GetBoolean_throws_for_maximum_Int32() => TestGetValue(DbType.Int32, ValueKind.Maximum, x => x.GetBoolean(0), true);
		public override void GetBoolean_throws_for_maximum_Int64() => TestGetValue(DbType.Int64, ValueKind.Maximum, x => x.GetBoolean(0), true);
		public override void GetBoolean_throws_for_maximum_SByte() => TestGetValue(DbType.SByte, ValueKind.Maximum, x => x.GetBoolean(0), true);
		public override void GetBoolean_throws_for_maximum_UInt16() => TestGetValue(DbType.UInt16, ValueKind.Maximum, x => x.GetBoolean(0), true);
		public override void GetBoolean_throws_for_maximum_UInt32() => TestGetValue(DbType.UInt32, ValueKind.Maximum, x => x.GetBoolean(0), true);
		public override void GetBoolean_throws_for_maximum_UInt64() => TestGetValue(DbType.UInt64, ValueKind.Maximum, x => x.GetBoolean(0), true);
		public override void GetBoolean_throws_for_minimum_Byte() => TestGetValue(DbType.Byte, ValueKind.Minimum, x => x.GetBoolean(0), false);
		public override void GetBoolean_throws_for_minimum_Int16() => TestGetValue(DbType.Int16, ValueKind.Minimum, x => x.GetBoolean(0), true);
		public override void GetBoolean_throws_for_minimum_Int32() => TestGetValue(DbType.Int32, ValueKind.Minimum, x => x.GetBoolean(0), true);
		public override void GetBoolean_throws_for_minimum_Int64() => TestGetValue(DbType.Int64, ValueKind.Minimum, x => x.GetBoolean(0), true);
		public override void GetBoolean_throws_for_minimum_SByte() => TestGetValue(DbType.SByte, ValueKind.Minimum, x => x.GetBoolean(0), true);
		public override void GetBoolean_throws_for_minimum_UInt16() => TestGetValue(DbType.UInt16, ValueKind.Minimum, x => x.GetBoolean(0), false);
		public override void GetBoolean_throws_for_minimum_UInt32() => TestGetValue(DbType.UInt32, ValueKind.Minimum, x => x.GetBoolean(0), false);
		public override void GetBoolean_throws_for_minimum_UInt64() => TestGetValue(DbType.UInt64, ValueKind.Minimum, x => x.GetBoolean(0), false);
		public override void GetBoolean_throws_for_one_Byte() => TestGetValue(DbType.Byte, ValueKind.One, x => x.GetBoolean(0), true);
		public override void GetBoolean_throws_for_one_Int16() => TestGetValue(DbType.Int16, ValueKind.One, x => x.GetBoolean(0), true);
		public override void GetBoolean_throws_for_one_Int32() => TestGetValue(DbType.Int32, ValueKind.One, x => x.GetBoolean(0), true);
		public override void GetBoolean_throws_for_one_Int64() => TestGetValue(DbType.Int64, ValueKind.One, x => x.GetBoolean(0), true);
		public override void GetBoolean_throws_for_one_SByte() => TestGetValue(DbType.SByte, ValueKind.One, x => x.GetBoolean(0), true);
		public override void GetBoolean_throws_for_one_UInt16() => TestGetValue(DbType.UInt16, ValueKind.One, x => x.GetBoolean(0), true);
		public override void GetBoolean_throws_for_one_UInt32() => TestGetValue(DbType.UInt32, ValueKind.One, x => x.GetBoolean(0), true);
		public override void GetBoolean_throws_for_one_UInt64() => TestGetValue(DbType.UInt64, ValueKind.One, x => x.GetBoolean(0), true);
		public override void GetBoolean_throws_for_zero_Byte() => TestGetValue(DbType.Byte, ValueKind.Zero, x => x.GetBoolean(0), false);
		public override void GetBoolean_throws_for_zero_Int16() => TestGetValue(DbType.Int16, ValueKind.Zero, x => x.GetBoolean(0), false);
		public override void GetBoolean_throws_for_zero_Int32() => TestGetValue(DbType.Int32, ValueKind.Zero, x => x.GetBoolean(0), false);
		public override void GetBoolean_throws_for_zero_Int64() => TestGetValue(DbType.Int64, ValueKind.Zero, x => x.GetBoolean(0), false);
		public override void GetBoolean_throws_for_zero_SByte() => TestGetValue(DbType.SByte, ValueKind.Zero, x => x.GetBoolean(0), false);
		public override void GetBoolean_throws_for_zero_UInt16() => TestGetValue(DbType.UInt16, ValueKind.Zero, x => x.GetBoolean(0), false);
		public override void GetBoolean_throws_for_zero_UInt32() => TestGetValue(DbType.UInt32, ValueKind.Zero, x => x.GetBoolean(0), false);
		public override void GetBoolean_throws_for_zero_UInt64() => TestGetValue(DbType.UInt64, ValueKind.Zero, x => x.GetBoolean(0), false);

		// the minimum date permitted by MySQL is 1000-01-01; override the minimum value for DateTime tests
		public override void GetDateTime_for_minimum_Date() => TestGetValue(DbType.Date, ValueKind.Minimum, x => x.GetDateTime(0), new DateTime(1000, 1, 1));
		public override void GetDateTime_for_minimum_DateTime() => TestGetValue(DbType.Date, ValueKind.Minimum, x => x.GetDateTime(0), new DateTime(1000, 1, 1));
	}
}
