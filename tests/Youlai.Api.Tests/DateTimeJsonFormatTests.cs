using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Youlai.Api.Tests;

public sealed class DateTimeJsonFormatTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly JsonSerializerOptions _options;

    public DateTimeJsonFormatTests(TestWebApplicationFactory factory)
    {
        _options = factory.Services.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions;
    }

    [Fact]
    public void DateTimeShouldSerializeAsSpaceSeparatedSeconds()
    {
        var json = JsonSerializer.Serialize(new DateTimeHolder { CreateTime = new DateTime(2026, 10, 7, 3, 0, 3) }, _options);

        Assert.Contains("\"createTime\":\"2026-10-07 03:00:03\"", json);
    }

    private sealed class DateTimeHolder
    {
        public DateTime? CreateTime { get; init; }
    }
}
