﻿using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace StackExchange.Utils.Tests
{
    public class HttpTests
    {
        [Fact]
        public async Task BasicCreation()
        {
            var request = Http.Request("https://example.com/")
                              .SendPlaintext("test")
                              .ExpectString();
            Assert.Equal("https://example.com/", request.Inner.Message.RequestUri.ToString());
            var content = Assert.IsType<StringContent>(request.Inner.Message.Content);
            var stringContent = await content.ReadAsStringAsync();
            Assert.Equal("test", stringContent);
        }

        [Fact]
        public async Task BasicGet()
        {
            var guid = Guid.NewGuid().ToString();
            var result = await Http.Request("https://httpbin.org/get")
                                    .ExpectJson<HttpBinResponse>()
                                    .GetAsync();
            Assert.True(result.Success);
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.NotNull(result);
            Assert.Equal("https://httpbin.org/get", result.Data.Url);
            Assert.Equal(Http.DefaultSettings.UserAgent, result.Data.Headers["User-Agent"]);
        }

        [Fact]
        public async Task BasicPost()
        {
            var guid = Guid.NewGuid().ToString();
            var result = await Http.Request("https://httpbin.org/post")
                                    .SendPlaintext(guid)
                                    .ExpectJson<HttpBinResponse>()
                                    .PostAsync();
            Assert.True(result.Success);
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.NotNull(result);
            Assert.True(result.Data.Form.ContainsKey(guid));
            Assert.Equal("https://httpbin.org/post", result.Data.Url);
            Assert.Equal(Http.DefaultSettings.UserAgent, result.Data.Headers["User-Agent"]);
        }

        [Fact]
        public async Task BasicPut()
        {
            var guid = Guid.NewGuid().ToString();
            var result = await Http.Request("https://httpbin.org/put")
                                    .SendPlaintext(guid)
                                    .ExpectJson<HttpBinResponse>()
                                    .PutAsync();
            Assert.True(result.Success);
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.True(result.Data.Form.ContainsKey(guid));
            Assert.Equal("https://httpbin.org/put", result.Data.Url);
            Assert.Equal(Http.DefaultSettings.UserAgent, result.Data.Headers["User-Agent"]);
        }

        [Fact]
        public async Task BasicDelete()
        {
            var result = await Http.Request("https://httpbin.org/delete")
                                    .ExpectJson<HttpBinResponse>()
                                    .DeleteAsync();
            Assert.True(result.Success);
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.Equal("https://httpbin.org/delete", result.Data.Url);
            Assert.Equal(Http.DefaultSettings.UserAgent, result.Data.Headers["User-Agent"]);
        }

        [Fact]
        public async Task ErrorIgnores()
        {
            var settings = new HttpSettings();
            var errorCount = 0;
            settings.Exception += (_, __) => errorCount++;

            var result = await Http.Request("https://httpbin.org/satus/404", settings)
                                   .ExpectHttpSuccess()
                                   .GetAsync();
            Assert.False(result.Success);
            Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
            Assert.Equal(1, errorCount);

            result = await Http.Request("https://httpbin.org/satus/404", settings)
                               .WithoutLogging(HttpStatusCode.NotFound)
                               .ExpectHttpSuccess()
                               .GetAsync();
            Assert.False(result.Success);
            Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
            Assert.Equal(1, errorCount); // didn't go up

            result = await Http.Request("https://httpbin.org/satus/404", settings)
                               .WithoutErrorLogging()
                               .ExpectHttpSuccess()
                               .GetAsync();
            Assert.False(result.Success);
            Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
            Assert.Equal(1, errorCount); // didn't go up
        }

        [Fact]
        public async Task Timeouts()
        {
            var result = await Http.Request("https://httpbin.org/delay/10")
                                   .WithTimeout(TimeSpan.FromSeconds(1))
                                   .ExpectHttpSuccess()
                                   .GetAsync();
            Assert.False(result.Success);
            Assert.NotNull(result.Error);
            Assert.Equal("HttpClient request timed out. Timeout: 1,000ms", result.Error.Message);
            var err = Assert.IsType<HttpClientException>(result.Error);
            Assert.Equal("https://httpbin.org/delay/10", err.Uri.ToString());
            Assert.Null(err.StatusCode);
        }
    }
}
