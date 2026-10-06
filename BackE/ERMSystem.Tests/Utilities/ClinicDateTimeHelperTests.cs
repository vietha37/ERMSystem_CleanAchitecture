using System;
using ERMSystem.Application.Utilities;
using Xunit;

namespace ERMSystem.Tests.Utilities;

public class ClinicDateTimeHelperTests
{
    [Fact]
    public void ResolveClinicTimeZone_ReturnsNonNullTimeZone()
    {
        var tz = ClinicDateTimeHelper.ResolveClinicTimeZone();
        Assert.NotNull(tz);
    }

    [Fact]
    public void ConvertUtcToClinicLocal_ConvertsUtcToExpectedLocal()
    {
        var utcNow = new DateTime(2026, 10, 6, 12, 0, 0, DateTimeKind.Utc);
        var local = ClinicDateTimeHelper.ConvertUtcToClinicLocal(utcNow);

        // SE Asia Standard Time is UTC+7
        Assert.Equal(19, local.Hour);
        Assert.Equal(utcNow.Minute, local.Minute);
    }

    [Fact]
    public void ConvertUtcToClinicLocal_NullInput_ReturnsNull()
    {
        DateTime? nullUtc = null;
        var result = ClinicDateTimeHelper.ConvertUtcToClinicLocal(nullUtc);

        Assert.Null(result);
    }
}
